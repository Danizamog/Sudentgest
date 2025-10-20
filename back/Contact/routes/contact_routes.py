from fastapi import APIRouter, HTTPException, Request, Header, Depends
from fastapi.exceptions import RequestValidationError
from typing import Optional, List
import httpx

from models.contact import ContactMessage, ContactMessageResponse, ContactStats, ContactMessageUpdate
from controllers.contact_controller import ContactController
from utils.supabase import get_tenant_from_email, get_current_user

router = APIRouter(prefix="/api/contact", tags=["Contact"])

# Instancia del controlador
contact_controller = ContactController()

@router.post("/send")
async def send_contact_message(request: Request):
    """
    Enviar un mensaje de contacto
    El tenant se determina automáticamente por el dominio del email
    """
    try:
        # Get the raw JSON body
        body = await request.json()
        
        # Validate manually and show detailed errors
        try:
            message = ContactMessage(**body)
        except Exception as val_error:
            return {
                "success": False,
                "error": "Validation error",
                "details": str(val_error),
                "received_body": body
            }
        
        # Obtener información adicional de la request
        user_agent = request.headers.get("user-agent")
        ip_address = request.client.host if request.client else None
        
        result = await contact_controller.create_contact_message(
            message=message,
            user_agent=user_agent,
            ip_address=ip_address
        )
        
        return result
        
    except Exception as e:
        return {
            "success": False,
            "error": f"Server error: {str(e)}"
        }

@router.get("/messages/{tenant}", response_model=dict)
async def get_contact_messages(
    tenant: str,
    limit: int = 50,
    authorization: str = Header(None)
):
    """
    Obtener mensajes de contacto por tenant (requiere autenticación)
    """
    try:
        # Verificar autenticación
        if authorization:
            user = await get_current_user(authorization)
            user_tenant = get_tenant_from_email(user["email"])
            
            # Verificar que el usuario pertenezca al tenant solicitado
            if user_tenant != tenant:
                raise HTTPException(
                    status_code=403, 
                    detail="No tienes permisos para acceder a este tenant"
                )
        else:
            raise HTTPException(status_code=401, detail="Autenticación requerida")
        
        result = await contact_controller.get_contact_messages(tenant, limit)
        return result
        
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail="Error obteniendo mensajes")

@router.get("/stats/{tenant}", response_model=ContactStats)
async def get_contact_statistics(
    tenant: str,
    authorization: str = Header(None)
):
    """
    Obtener estadísticas de contactos por tenant (requiere autenticación)
    """
    try:
        # Verificar autenticación
        if authorization:
            user = await get_current_user(authorization)
            user_tenant = get_tenant_from_email(user["email"])
            
            # Verificar que el usuario pertenezca al tenant solicitado
            if user_tenant != tenant:
                raise HTTPException(
                    status_code=403, 
                    detail="No tienes permisos para acceder a este tenant"
                )
        else:
            raise HTTPException(status_code=401, detail="Autenticación requerida")
        
        stats = await contact_controller.get_contact_stats(tenant)
        return stats
        
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail="Error obteniendo estadísticas")

@router.patch("/messages/{message_id}/status", response_model=dict)
async def update_message_status(
    message_id: int,
    update: ContactMessageUpdate,
    authorization: str = Header(None)
):
    """
    Actualizar el estado de un mensaje de contacto (requiere autenticación)
    """
    try:
        # Verificar autenticación
        if not authorization:
            raise HTTPException(status_code=401, detail="Autenticación requerida")
        
        user = await get_current_user(authorization)
        tenant = get_tenant_from_email(user["email"])
        
        if not update.status:
            raise HTTPException(status_code=400, detail="Estado requerido")
        
        result = await contact_controller.update_message_status(
            message_id=message_id,
            tenant=tenant,
            status=update.status,
            respuesta=update.respuesta,
            atendido_por=user["email"]
        )
        
        return result
        
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail="Error actualizando mensaje")

@router.get("/tenants", response_model=dict)
async def get_supported_tenants():
    """
    Obtener lista de tenants soportados
    """
    return {
        "success": True,
        "tenants": [
            {
                "domain": "ucb.edu.bo",
                "name": "Universidad Católica Boliviana",
                "description": "Correos institucionales UCB"
            },
            {
                "domain": "upb.edu.bo", 
                "name": "Universidad Privada Boliviana",
                "description": "Correos institucionales UPB"
            },
            {
                "domain": "gmail.com",
                "name": "Gmail",
                "description": "Correos personales Gmail"
            }
        ]
    }

@router.get("/health", response_model=dict)
async def health_check():
    """Health check del servicio de contacto"""
    return {
        "service": "Contact Service",
        "status": "healthy",
        "version": "1.0.0"
    }