from typing import List, Optional
from fastapi import HTTPException
from datetime import datetime
import httpx
import os

from models.contact import ContactMessage, ContactMessageResponse, ContactMessageCreate, ContactStats
from utils.supabase import (
    get_tenant_from_email, 
    get_tenant_table_prefix, 
    create_contact_message,
    get_contact_messages_by_tenant,
    get_user_by_email
)

class ContactController:
    def __init__(self):
        self.supabase_url = os.getenv("SUPABASE_URL")
        self.supabase_service_key = os.getenv("SUPABASE_SERVICE_ROLE_KEY")

    async def create_contact_message(self, message: ContactMessage, user_agent: str = None, ip_address: str = None) -> dict:
        """Crear un nuevo mensaje de contacto"""
        try:
            # Obtener tenant del email
            tenant = get_tenant_from_email(message.email)
            if tenant == "unknown":
                raise HTTPException(
                    status_code=400, 
                    detail=f"Dominio no soportado: {message.email.split('@')[1]}"
                )

            # Verificar si el usuario existe (opcional)
            user = await get_user_by_email(message.email, tenant)
            user_id = user.get("id") if user else None

            # Preparar datos para insertar
            contact_data = {
                "nombre": message.nombre,
                "email": message.email,
                "mensaje": message.mensaje,
                "asunto": message.asunto or "Consulta general",
                "telefono": message.telefono,
                "tenant": tenant,
                "status": "pendiente"
            }
            
            # Agregar campos opcionales solo si están disponibles
            if user_id:
                contact_data["user_id"] = user_id
            if user_agent:
                contact_data["user_agent"] = user_agent
            if ip_address:
                contact_data["ip_address"] = ip_address

            # Crear el mensaje en la base de datos
            result = await create_contact_message(contact_data, tenant)
            
            if not result:
                raise HTTPException(status_code=500, detail="Error al crear el mensaje de contacto")

            return {
                "success": True,
                "message": "Mensaje de contacto enviado exitosamente",
                "data": result,
                "tenant": tenant
            }

        except HTTPException:
            raise
        except Exception as e:
            raise HTTPException(status_code=500, detail="Error interno del servidor")

    async def get_contact_messages(self, tenant: str = None, limit: int = 50) -> List[dict]:
        """Obtener mensajes de contacto por tenant"""
        try:
            if not tenant:
                raise HTTPException(status_code=400, detail="Tenant requerido")

            messages = await get_contact_messages_by_tenant(tenant, limit)
            
            return {
                "success": True,
                "data": messages,
                "total": len(messages),
                "tenant": tenant
            }

        except HTTPException:
            raise
        except Exception as e:
            raise HTTPException(status_code=500, detail="Error interno del servidor")

    async def get_contact_stats(self, tenant: str) -> ContactStats:
        """Obtener estadísticas de contactos por tenant"""
        try:
            table_prefix = get_tenant_table_prefix(tenant)
            table_name = f"{table_prefix}_contact_messages"
            
            async with httpx.AsyncClient(timeout=10.0) as client:
                headers = {
                    "apikey": self.supabase_service_key,
                    "Authorization": f"Bearer {self.supabase_service_key}"
                }
                
                # Obtener estadísticas
                response = await client.get(
                    f"{self.supabase_url}/rest/v1/{table_name}?select=status,created_at",
                    headers=headers
                )
                
                if response.status_code == 200:
                    messages = response.json()
                    total = len(messages)
                    pendientes = len([m for m in messages if m.get("status") == "pendiente"])
                    respondidos = len([m for m in messages if m.get("status") == "respondido"])
                    
                    ultimo_mensaje = None
                    if messages:
                        ultimo_mensaje = max([m.get("created_at") for m in messages if m.get("created_at")])
                        ultimo_mensaje = datetime.fromisoformat(ultimo_mensaje.replace('Z', '+00:00'))
                    
                    return ContactStats(
                        total_mensajes=total,
                        mensajes_pendientes=pendientes,
                        mensajes_respondidos=respondidos,
                        tenant=tenant,
                        ultimo_mensaje=ultimo_mensaje
                    )
                else:
                    raise HTTPException(status_code=500, detail="Error obteniendo estadísticas")

        except HTTPException:
            raise
        except Exception as e:
            raise HTTPException(status_code=500, detail="Error interno del servidor")

    async def update_message_status(self, message_id: int, tenant: str, status: str, respuesta: str = None, atendido_por: str = None) -> dict:
        """Actualizar estado de un mensaje de contacto"""
        try:
            table_prefix = get_tenant_table_prefix(tenant)
            table_name = f"{table_prefix}_contact_messages"
            
            update_data = {
                "status": status,
                "updated_at": datetime.utcnow().isoformat()
            }
            
            if respuesta:
                update_data["respuesta"] = respuesta
            if atendido_por:
                update_data["atendido_por"] = atendido_por

            async with httpx.AsyncClient(timeout=10.0) as client:
                headers = {
                    "apikey": self.supabase_service_key,
                    "Authorization": f"Bearer {self.supabase_service_key}",
                    "Content-Type": "application/json"
                }
                
                response = await client.patch(
                    f"{self.supabase_url}/rest/v1/{table_name}?id=eq.{message_id}",
                    headers=headers,
                    json=update_data
                )
                
                if response.status_code == 204:
                    return {
                        "success": True,
                        "message": "Estado del mensaje actualizado exitosamente"
                    }
                else:
                    raise HTTPException(status_code=500, detail="Error actualizando el mensaje")

        except HTTPException:
            raise
        except Exception as e:
            raise HTTPException(status_code=500, detail="Error interno del servidor")