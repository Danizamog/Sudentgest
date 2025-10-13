import httpx
from fastapi import HTTPException
from datetime import datetime
from typing import Dict, List
import sys
import os

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from models.attendance import AttendanceRecord, Excuse, ExcuseApproval
from utils.supabase import (
    SUPABASE_URL, SUPABASE_ANON_KEY, SUPABASE_SERVICE_ROLE_KEY,
    get_tenant_from_email, get_tenant_info, get_user_by_email
)

class AttendanceController:
    
    @staticmethod
    async def register_attendance(attendance: AttendanceRecord, email: str) -> Dict:
        """Registrar asistencia (solo profesores/director)"""
        tenant_domain = get_tenant_from_email(email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Tenant no identificado")
        
        tenant_info = await get_tenant_info(tenant_domain)
        if not tenant_info:
            raise HTTPException(status_code=404, detail="Tenant no encontrado")
        
        schema = tenant_info["schema_name"]
        
        user_data = await get_user_by_email(email, schema)
        if not user_data or user_data.get("rol") not in ["Profesor", "Director", "Admin"]:
            raise HTTPException(status_code=403, detail="No tienes permisos para registrar asistencia")
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_SERVICE_ROLE_KEY,
                "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}",
                "Content-Type": "application/json",
                "Prefer": "return=representation"
            }
            table_name = f"{schema}_asistencias"
            payload = {
                "estudiante_id": attendance.estudiante_id,
                "curso_id": attendance.curso_id,
                "fecha": attendance.fecha.isoformat(),
                "estado": attendance.estado,
                "observaciones": attendance.observaciones,
                "registrado_por": user_data["id"],
                "created_at": datetime.utcnow().isoformat()
            }
            response = await client.post(
                f"{SUPABASE_URL}/rest/v1/{table_name}",
                json=payload,
                headers=headers
            )
            if response.status_code in [200, 201]:
                return {"success": True, "asistencia": response.json()}
            else:
                raise HTTPException(status_code=500, detail=f"Error al registrar asistencia: {response.text}")
    
    @staticmethod
    async def create_excuse(excuse: Excuse, email: str) -> Dict:
        """Crear excusa (padres, profesores, personal)"""
        tenant_domain = get_tenant_from_email(email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Tenant no identificado")
        
        tenant_info = await get_tenant_info(tenant_domain)
        if not tenant_info:
            raise HTTPException(status_code=404, detail="Tenant no encontrado")
        
        schema = tenant_info["schema_name"]
        user_data = await get_user_by_email(email, schema)
        if not user_data:
            raise HTTPException(status_code=404, detail="Usuario no encontrado")
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_SERVICE_ROLE_KEY,
                "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}",
                "Content-Type": "application/json",
                "Prefer": "return=representation"
            }
            table_name = f"{schema}_excusas"
            payload = {
                "estudiante_id": excuse.estudiante_id,
                "curso_id": excuse.curso_id,
                "fecha_inicio": excuse.fecha_inicio.isoformat(),
                "fecha_fin": excuse.fecha_fin.isoformat(),
                "motivo": excuse.motivo,
                "documento_url": excuse.documento_url,
                "creado_por": user_data["id"],
                "estado": "pendiente",
                "created_at": datetime.utcnow().isoformat()
            }
            response = await client.post(
                f"{SUPABASE_URL}/rest/v1/{table_name}",
                json=payload,
                headers=headers
            )
            if response.status_code in [200, 201]:
                return {"success": True, "excusa": response.json()}
            else:
                raise HTTPException(status_code=500, detail=f"Error al crear excusa: {response.text}")
    
    @staticmethod
    async def approve_excuse(approval: ExcuseApproval, email: str) -> Dict:
        """Aprobar/rechazar excusa (solo director)"""
        tenant_domain = get_tenant_from_email(email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Tenant no identificado")
        
        tenant_info = await get_tenant_info(tenant_domain)
        if not tenant_info:
            raise HTTPException(status_code=404, detail="Tenant no encontrado")
        
        schema = tenant_info["schema_name"]
        user_data = await get_user_by_email(email, schema)
        if not user_data or user_data.get("rol") not in ["Director", "Admin"]:
            raise HTTPException(status_code=403, detail="Solo el director puede aprobar excusas")
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_SERVICE_ROLE_KEY,
                "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}",
                "Content-Type": "application/json",
                "Prefer": "return=representation"
            }
            table_name = f"{schema}_excusas"
            payload = {
                "estado": approval.estado,
                "comentario_director": approval.comentario_director,
                "aprobado_por": user_data["id"],
                "fecha_aprobacion": datetime.utcnow().isoformat()
            }
            response = await client.patch(
                f"{SUPABASE_URL}/rest/v1/{table_name}?id=eq.{approval.excuse_id}",
                json=payload,
                headers=headers
            )
            if response.status_code == 200:
                return {"success": True, "excusa": response.json()}
            else:
                raise HTTPException(status_code=500, detail=f"Error al aprobar excusa: {response.text}")
    
    @staticmethod
    async def get_student_attendance(estudiante_id: int, curso_id: int, email: str) -> Dict:
        """Obtener asistencias de un estudiante"""
        tenant_domain = get_tenant_from_email(email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Tenant no identificado")
        
        tenant_info = await get_tenant_info(tenant_domain)
        if not tenant_info:
            raise HTTPException(status_code=404, detail="Tenant no encontrado")
        
        schema = tenant_info["schema_name"]
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_ANON_KEY,
                "Authorization": f"Bearer {SUPABASE_ANON_KEY}"
            }
            table_name = f"{schema}_asistencias"
            response = await client.get(
                f"{SUPABASE_URL}/rest/v1/{table_name}?estudiante_id=eq.{estudiante_id}&curso_id=eq.{curso_id}&select=*&order=fecha.desc",
                headers=headers
            )
            if response.status_code == 200:
                return {"asistencias": response.json()}
            else:
                raise HTTPException(status_code=500, detail="Error al obtener asistencias")
    
    @staticmethod
    async def get_pending_excuses(email: str) -> Dict:
        """Obtener excusas pendientes (solo director)"""
        tenant_domain = get_tenant_from_email(email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Tenant no identificado")
        
        tenant_info = await get_tenant_info(tenant_domain)
        if not tenant_info:
            raise HTTPException(status_code=404, detail="Tenant no encontrado")
        
        schema = tenant_info["schema_name"]
        user_data = await get_user_by_email(email, schema)
        if not user_data or user_data.get("rol") not in ["Director", "Admin"]:
            raise HTTPException(status_code=403, detail="Solo el director puede ver excusas pendientes")
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_ANON_KEY,
                "Authorization": f"Bearer {SUPABASE_ANON_KEY}"
            }
            table_name = f"{schema}_excusas"
            response = await client.get(
                f"{SUPABASE_URL}/rest/v1/{table_name}?estado=eq.pendiente&select=*&order=created_at.desc",
                headers=headers
            )
            if response.status_code == 200:
                return {"excusas": response.json()}
            else:
                raise HTTPException(status_code=500, detail="Error al obtener excusas")
    
    @staticmethod
    async def get_attendance_history(curso_id: int, fecha_inicio: str, fecha_fin: str, email: str) -> Dict:
        """Obtener historial de asistencias de un curso (profesores/director)"""
        tenant_domain = get_tenant_from_email(email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Tenant no identificado")
        
        tenant_info = await get_tenant_info(tenant_domain)
        if not tenant_info:
            raise HTTPException(status_code=404, detail="Tenant no encontrado")
        
        schema = tenant_info["schema_name"]
        user_data = await get_user_by_email(email, schema)
        if not user_data or user_data.get("rol") not in ["profesor", "director", "admin", "Profesor", "Director"]:
            raise HTTPException(status_code=403, detail="No tienes permisos para ver el historial")
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_ANON_KEY,
                "Authorization": f"Bearer {SUPABASE_ANON_KEY}"
            }
            table_name = f"{schema}_asistencias"
            url = f"{SUPABASE_URL}/rest/v1/{table_name}?curso_id=eq.{curso_id}&fecha=gte.{fecha_inicio}&fecha=lte.{fecha_fin}&select=*&order=fecha.desc,estudiante_id.asc"
            
            response = await client.get(url, headers=headers)
            if response.status_code == 200:
                return {"asistencias": response.json()}
            else:
                raise HTTPException(status_code=500, detail="Error al obtener historial")
    
    @staticmethod
    async def update_attendance(asistencia_id: int, estado: str, observaciones: str, email: str) -> Dict:
        """Actualizar una asistencia existente (profesores/directores)"""
        tenant_domain = get_tenant_from_email(email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Tenant no identificado")
        
        tenant_info = await get_tenant_info(tenant_domain)
        if not tenant_info:
            raise HTTPException(status_code=404, detail="Tenant no encontrado")
        
        schema = tenant_info["schema_name"]
        
        user_data = await get_user_by_email(email, schema)
        if not user_data or user_data.get("rol") not in ["profesor", "director", "admin"]:
            raise HTTPException(status_code=403, detail="No tienes permisos para actualizar asistencia")
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_SERVICE_ROLE_KEY,
                "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}",
                "Content-Type": "application/json",
                "Prefer": "return=representation"
            }
            table_name = f"{schema}_asistencias"
            payload = {
                "estado": estado,
                "observaciones": observaciones
            }
            response = await client.patch(
                f"{SUPABASE_URL}/rest/v1/{table_name}?id=eq.{asistencia_id}",
                json=payload,
                headers=headers
            )
            if response.status_code == 200:
                return {"success": True, "asistencia": response.json()}
            else:
                raise HTTPException(status_code=500, detail=f"Error al actualizar asistencia: {response.text}")
    
    @staticmethod
    async def delete_attendance(asistencia_id: int, email: str) -> Dict:
        """Eliminar una asistencia (solo director)"""
        tenant_domain = get_tenant_from_email(email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Tenant no identificado")
        
        tenant_info = await get_tenant_info(tenant_domain)
        if not tenant_info:
            raise HTTPException(status_code=404, detail="Tenant no encontrado")
        
        schema = tenant_info["schema_name"]
        user_data = await get_user_by_email(email, schema)
        if not user_data or user_data.get("rol") not in ["director", "admin", "Director"]:
            raise HTTPException(status_code=403, detail="Solo el director puede eliminar asistencias")
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_SERVICE_ROLE_KEY,
                "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}"
            }
            table_name = f"{schema}_asistencias"
            response = await client.delete(
                f"{SUPABASE_URL}/rest/v1/{table_name}?id=eq.{asistencia_id}",
                headers=headers
            )
            if response.status_code in [200, 204]:
                return {"success": True, "message": "Asistencia eliminada"}
            else:
                raise HTTPException(status_code=500, detail=f"Error al eliminar asistencia: {response.text}")
    
    @staticmethod
    async def get_all_excuses(email: str, estado: str = None) -> Dict:
        """Obtener todas las excusas con filtro opcional por estado (director)"""
        tenant_domain = get_tenant_from_email(email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Tenant no identificado")
        
        tenant_info = await get_tenant_info(tenant_domain)
        if not tenant_info:
            raise HTTPException(status_code=404, detail="Tenant no encontrado")
        
        schema = tenant_info["schema_name"]
        user_data = await get_user_by_email(email, schema)
        if not user_data or user_data.get("rol") not in ["director", "admin", "Director"]:
            raise HTTPException(status_code=403, detail="Solo el director puede ver todas las excusas")
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_ANON_KEY,
                "Authorization": f"Bearer {SUPABASE_ANON_KEY}"
            }
            table_name = f"{schema}_excusas"
            url = f"{SUPABASE_URL}/rest/v1/{table_name}?select=*&order=created_at.desc"
            if estado:
                url += f"&estado=eq.{estado}"
            
            response = await client.get(url, headers=headers)
            if response.status_code == 200:
                return {"excusas": response.json()}
            else:
                raise HTTPException(status_code=500, detail="Error al obtener excusas")
    
    @staticmethod
    async def get_my_excuses(email: str) -> Dict:
        """Obtener excusas creadas por el usuario actual (profesores/padres)"""
        tenant_domain = get_tenant_from_email(email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Tenant no identificado")
        
        tenant_info = await get_tenant_info(tenant_domain)
        if not tenant_info:
            raise HTTPException(status_code=404, detail="Tenant no encontrado")
        
        schema = tenant_info["schema_name"]
        user_data = await get_user_by_email(email, schema)
        if not user_data:
            raise HTTPException(status_code=404, detail="Usuario no encontrado")
        
        user_id = user_data["id"]
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_ANON_KEY,
                "Authorization": f"Bearer {SUPABASE_ANON_KEY}"
            }
            table_name = f"{schema}_excusas"
            url = f"{SUPABASE_URL}/rest/v1/{table_name}?creado_por=eq.{user_id}&select=*&order=created_at.desc"
            
            response = await client.get(url, headers=headers)
            if response.status_code == 200:
                return {"excusas": response.json()}
            else:
                raise HTTPException(status_code=500, detail="Error al obtener tus excusas")
