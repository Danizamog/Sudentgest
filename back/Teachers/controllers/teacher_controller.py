from fastapi import HTTPException
import httpx
import os
from typing import List, Dict, Optional
from models.teacher import Teacher, TeacherCreate, TeacherUpdate
from utils.supabase import get_tenant_from_email, get_tenant_info, get_user_by_email, get_schema_from_domain

SUPABASE_URL = os.getenv("SUPABASE_URL")
SUPABASE_SERVICE_ROLE_KEY = os.getenv("SUPABASE_SERVICE_ROLE_KEY")

class TeacherController:
    @staticmethod
    async def get_all_teachers(user_email: str) -> List[Dict]:
        """Obtener todos los profesores del tenant (solo para directores)"""
        # Verificar permisos
        tenant_domain = get_tenant_from_email(user_email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Dominio de tenant no válido")
        
        schema = get_schema_from_domain(tenant_domain)
        current_user = await get_user_by_email(user_email, schema)
        
        if not current_user or current_user.get("rol") != "director":
            raise HTTPException(status_code=403, detail="Solo los directores pueden ver la lista de profesores")
        
        try:
            async with httpx.AsyncClient(timeout=10.0) as client:
                headers = {
                    "apikey": SUPABASE_SERVICE_ROLE_KEY,
                    "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}"
                }
                
                table_name = f"{schema}_usuarios"
                response = await client.get(
                    f"{SUPABASE_URL}/rest/v1/{table_name}?rol=eq.profesor&select=*",
                    headers=headers
                )
                
                if response.status_code == 200:
                    return response.json()
                else:
                    raise HTTPException(status_code=response.status_code, detail="Error al obtener profesores")
                    
        except Exception as e:
            print(f"❌ Error obteniendo profesores: {e}")
            raise HTTPException(status_code=500, detail=f"Error interno del servidor: {str(e)}")

    @staticmethod
    async def create_teacher(teacher: TeacherCreate, user_email: str) -> Dict:
        """Crear un nuevo profesor (solo para directores)"""
        # Verificar permisos
        tenant_domain = get_tenant_from_email(user_email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Dominio de tenant no válido")
        
        schema = get_schema_from_domain(tenant_domain)
        current_user = await get_user_by_email(user_email, schema)
        
        if not current_user or current_user.get("rol") != "director":
            raise HTTPException(status_code=403, detail="Solo los directores pueden crear profesores")
        
        # Verificar que el email del nuevo profesor pertenezca al mismo tenant
        teacher_domain = get_tenant_from_email(teacher.email)
        if teacher_domain != tenant_domain:
            raise HTTPException(status_code=400, detail="El email del profesor debe pertenecer al mismo dominio institucional")
        
        try:
            async with httpx.AsyncClient(timeout=10.0) as client:
                headers = {
                    "apikey": SUPABASE_SERVICE_ROLE_KEY,
                    "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}",
                    "Content-Type": "application/json",
                    "Prefer": "return=representation"
                }
                
                table_name = f"{schema}_usuarios"
                teacher_data = {
                    "nombre": teacher.nombre,
                    "apellido": teacher.apellido,
                    "email": teacher.email,
                    "rol": "profesor"
                }
                
                # Agregar campos opcionales si están presentes
                if teacher.telefono:
                    teacher_data["telefono"] = teacher.telefono
                if teacher.direccion:
                    teacher_data["direccion"] = teacher.direccion
                if teacher.fecha_nacimiento:
                    teacher_data["fecha_nacimiento"] = teacher.fecha_nacimiento
                
                response = await client.post(
                    f"{SUPABASE_URL}/rest/v1/{table_name}",
                    headers=headers,
                    json=teacher_data
                )
                
                if response.status_code == 201:
                    try:
                        response_data = response.json()
                        if response_data and len(response_data) > 0:
                            return response_data[0]
                        else:
                            # Si no hay datos en la respuesta, retornar los datos que enviamos
                            return {**teacher_data, "id": None, "created_at": None}
                    except Exception as parse_error:
                        print(f"⚠️ Error parsing response JSON: {parse_error}")
                        # Retornar los datos que enviamos si no podemos parsear la respuesta
                        return {**teacher_data, "id": None, "created_at": None}
                else:
                    error_detail = "Error al crear profesor"
                    try:
                        error_response = response.json()
                        if "message" in error_response:
                            error_detail = error_response["message"]
                    except:
                        pass
                    raise HTTPException(status_code=response.status_code, detail=error_detail)
                    
        except Exception as e:
            print(f"❌ Error creando profesor: {e}")
            raise HTTPException(status_code=500, detail=f"Error interno del servidor: {str(e)}")

    @staticmethod
    async def get_teacher_by_id(teacher_id: int, user_email: str) -> Dict:
        """Obtener un profesor por ID (solo para directores)"""
        # Verificar permisos
        tenant_domain = get_tenant_from_email(user_email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Dominio de tenant no válido")
        
        schema = get_schema_from_domain(tenant_domain)
        current_user = await get_user_by_email(user_email, schema)
        
        if not current_user or current_user.get("rol") != "director":
            raise HTTPException(status_code=403, detail="Solo los directores pueden ver detalles de profesores")
        
        try:
            async with httpx.AsyncClient(timeout=10.0) as client:
                headers = {
                    "apikey": SUPABASE_SERVICE_ROLE_KEY,
                    "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}"
                }
                
                table_name = f"{schema}_usuarios"
                response = await client.get(
                    f"{SUPABASE_URL}/rest/v1/{table_name}?id=eq.{teacher_id}&rol=eq.profesor&select=*",
                    headers=headers
                )
                
                if response.status_code == 200:
                    teachers = response.json()
                    if not teachers:
                        raise HTTPException(status_code=404, detail="Profesor no encontrado")
                    return teachers[0]
                else:
                    raise HTTPException(status_code=response.status_code, detail="Error al obtener profesor")
                    
        except Exception as e:
            print(f"❌ Error obteniendo profesor: {e}")
            raise HTTPException(status_code=500, detail=f"Error interno del servidor: {str(e)}")

    @staticmethod
    async def update_teacher(teacher_id: int, teacher_update: TeacherUpdate, user_email: str) -> Dict:
        """Actualizar un profesor (solo para directores)"""
        # Verificar permisos
        tenant_domain = get_tenant_from_email(user_email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Dominio de tenant no válido")
        
        schema = get_schema_from_domain(tenant_domain)
        current_user = await get_user_by_email(user_email, schema)
        
        if not current_user or current_user.get("rol") != "director":
            raise HTTPException(status_code=403, detail="Solo los directores pueden actualizar profesores")
        
        # Si se está actualizando el email, verificar que pertenezca al mismo tenant
        if teacher_update.email:
            teacher_domain = get_tenant_from_email(teacher_update.email)
            if teacher_domain != tenant_domain:
                raise HTTPException(status_code=400, detail="El email del profesor debe pertenecer al mismo dominio institucional")
        
        try:
            # Crear el objeto de actualización excluyendo campos None
            update_data = {}
            if teacher_update.nombre is not None:
                update_data["nombre"] = teacher_update.nombre
            if teacher_update.apellido is not None:
                update_data["apellido"] = teacher_update.apellido
            if teacher_update.email is not None:
                update_data["email"] = teacher_update.email
            if teacher_update.telefono is not None:
                update_data["telefono"] = teacher_update.telefono
            if teacher_update.direccion is not None:
                update_data["direccion"] = teacher_update.direccion
            if teacher_update.fecha_nacimiento is not None:
                update_data["fecha_nacimiento"] = teacher_update.fecha_nacimiento
            if teacher_update.rol is not None:
                update_data["rol"] = teacher_update.rol
            
            if not update_data:
                raise HTTPException(status_code=400, detail="No hay campos para actualizar")
            
            async with httpx.AsyncClient(timeout=10.0) as client:
                headers = {
                    "apikey": SUPABASE_SERVICE_ROLE_KEY,
                    "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}",
                    "Content-Type": "application/json"
                }
                
                table_name = f"{schema}_usuarios"
                response = await client.patch(
                    f"{SUPABASE_URL}/rest/v1/{table_name}?id=eq.{teacher_id}&rol=eq.profesor",
                    headers=headers,
                    json=update_data
                )
                
                if response.status_code == 200 or response.status_code == 204:
                    # Obtener el profesor actualizado
                    return await TeacherController.get_teacher_by_id(teacher_id, user_email)
                else:
                    raise HTTPException(status_code=response.status_code, detail="Error al actualizar profesor")
                    
        except Exception as e:
            print(f"❌ Error actualizando profesor: {e}")
            raise HTTPException(status_code=500, detail=f"Error interno del servidor: {str(e)}")

    @staticmethod
    async def delete_teacher(teacher_id: int, user_email: str) -> Dict:
        """Eliminar un profesor (solo para directores)"""
        # Verificar permisos
        tenant_domain = get_tenant_from_email(user_email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Dominio de tenant no válido")
        
        schema = get_schema_from_domain(tenant_domain)
        current_user = await get_user_by_email(user_email, schema)
        
        if not current_user or current_user.get("rol") != "director":
            raise HTTPException(status_code=403, detail="Solo los directores pueden eliminar profesores")
        
        try:
            async with httpx.AsyncClient(timeout=10.0) as client:
                headers = {
                    "apikey": SUPABASE_SERVICE_ROLE_KEY,
                    "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}"
                }
                
                table_name = f"{schema}_usuarios"
                response = await client.delete(
                    f"{SUPABASE_URL}/rest/v1/{table_name}?id=eq.{teacher_id}&rol=eq.profesor",
                    headers=headers
                )
                
                if response.status_code == 200 or response.status_code == 204:
                    return {"message": "Profesor eliminado exitosamente"}
                else:
                    raise HTTPException(status_code=response.status_code, detail="Error al eliminar profesor")
                    
        except Exception as e:
            print(f"❌ Error eliminando profesor: {e}")
            raise HTTPException(status_code=500, detail=f"Error interno del servidor: {str(e)}")