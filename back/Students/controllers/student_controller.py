from fastapi import HTTPException
import httpx
import os
from typing import List, Dict, Optional
from models.student import Student, StudentCreate, StudentUpdate
from utils.supabase import get_tenant_from_email, get_tenant_info, get_user_by_email, get_schema_from_domain

SUPABASE_URL = os.getenv("SUPABASE_URL")
SUPABASE_SERVICE_ROLE_KEY = os.getenv("SUPABASE_SERVICE_ROLE_KEY")

class StudentController:
    @staticmethod
    async def get_all_students(user_email: str) -> List[Dict]:
        """Obtener todos los estudiantes del tenant (para directores y profesores)"""
        # Verificar permisos
        tenant_domain = get_tenant_from_email(user_email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Dominio de tenant no válido")
        
        schema = get_schema_from_domain(tenant_domain)
        current_user = await get_user_by_email(user_email, schema)
        
        if not current_user or current_user.get("rol") not in ["director", "profesor"]:
            raise HTTPException(status_code=403, detail="Solo los directores y profesores pueden ver la lista de estudiantes")
        
        try:
            async with httpx.AsyncClient(timeout=10.0) as client:
                headers = {
                    "apikey": SUPABASE_SERVICE_ROLE_KEY,
                    "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}"
                }
                
                table_name = f"{schema}_usuarios"
                response = await client.get(
                    f"{SUPABASE_URL}/rest/v1/{table_name}?rol=eq.estudiante&select=*",
                    headers=headers
                )
                
                if response.status_code == 200:
                    return response.json()
                else:
                    raise HTTPException(status_code=response.status_code, detail="Error al obtener estudiantes")
                    
        except Exception as e:
            print(f"❌ Error obteniendo estudiantes: {e}")
            raise HTTPException(status_code=500, detail=f"Error interno del servidor: {str(e)}")

    @staticmethod
    async def create_student(student: StudentCreate, user_email: str) -> Dict:
        """Crear un nuevo estudiante (solo para directores)"""
        # Verificar permisos
        tenant_domain = get_tenant_from_email(user_email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Dominio de tenant no válido")
        
        schema = get_schema_from_domain(tenant_domain)
        current_user = await get_user_by_email(user_email, schema)
        
        if not current_user or current_user.get("rol") != "director":
            raise HTTPException(status_code=403, detail="Solo los directores pueden crear estudiantes")
        
        # Verificar que el email del nuevo estudiante pertenezca al mismo tenant
        student_domain = get_tenant_from_email(student.email)
        if student_domain != tenant_domain:
            raise HTTPException(status_code=400, detail="El email del estudiante debe pertenecer al mismo dominio institucional")
        
        try:
            async with httpx.AsyncClient(timeout=10.0) as client:
                headers = {
                    "apikey": SUPABASE_SERVICE_ROLE_KEY,
                    "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}",
                    "Content-Type": "application/json",
                    "Prefer": "return=representation"
                }
                
                table_name = f"{schema}_usuarios"
                student_data = {
                    "nombre": student.nombre,
                    "apellido": student.apellido,
                    "email": student.email,
                    "rol": "estudiante"
                }
                
                # Agregar campos opcionales si están presentes
                if student.telefono:
                    student_data["telefono"] = student.telefono
                if student.direccion:
                    student_data["direccion"] = student.direccion
                if student.fecha_nacimiento:
                    student_data["fecha_nacimiento"] = student.fecha_nacimiento
                
                response = await client.post(
                    f"{SUPABASE_URL}/rest/v1/{table_name}",
                    headers=headers,
                    json=student_data
                )
                
                if response.status_code == 201:
                    try:
                        response_data = response.json()
                        if response_data and len(response_data) > 0:
                            return response_data[0]
                        else:
                            # Si no hay datos en la respuesta, retornar los datos que enviamos
                            return {**student_data, "id": None, "created_at": None}
                    except Exception as parse_error:
                        print(f"⚠️ Error parsing response JSON: {parse_error}")
                        # Retornar los datos que enviamos si no podemos parsear la respuesta
                        return {**student_data, "id": None, "created_at": None}
                else:
                    error_detail = "Error al crear estudiante"
                    try:
                        error_response = response.json()
                        if "message" in error_response:
                            error_detail = error_response["message"]
                    except:
                        pass
                    raise HTTPException(status_code=response.status_code, detail=error_detail)
                    
        except Exception as e:
            print(f"❌ Error creando estudiante: {e}")
            raise HTTPException(status_code=500, detail=f"Error interno del servidor: {str(e)}")

    @staticmethod
    async def get_student_by_id(student_id: int, user_email: str) -> Dict:
        """Obtener un estudiante por ID (para directores y profesores)"""
        # Verificar permisos
        tenant_domain = get_tenant_from_email(user_email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Dominio de tenant no válido")
        
        schema = get_schema_from_domain(tenant_domain)
        current_user = await get_user_by_email(user_email, schema)
        
        if not current_user or current_user.get("rol") not in ["director", "profesor"]:
            raise HTTPException(status_code=403, detail="Solo los directores y profesores pueden ver detalles de estudiantes")
        
        try:
            async with httpx.AsyncClient(timeout=10.0) as client:
                headers = {
                    "apikey": SUPABASE_SERVICE_ROLE_KEY,
                    "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}"
                }
                
                table_name = f"{schema}_usuarios"
                response = await client.get(
                    f"{SUPABASE_URL}/rest/v1/{table_name}?id=eq.{student_id}&rol=eq.estudiante&select=*",
                    headers=headers
                )
                
                if response.status_code == 200:
                    students = response.json()
                    if not students:
                        raise HTTPException(status_code=404, detail="Estudiante no encontrado")
                    return students[0]
                else:
                    raise HTTPException(status_code=response.status_code, detail="Error al obtener estudiante")
                    
        except Exception as e:
            print(f"❌ Error obteniendo estudiante: {e}")
            raise HTTPException(status_code=500, detail=f"Error interno del servidor: {str(e)}")

    @staticmethod
    async def update_student(student_id: int, student_update: StudentUpdate, user_email: str) -> Dict:
        """Actualizar un estudiante (solo para directores)"""
        # Verificar permisos
        tenant_domain = get_tenant_from_email(user_email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Dominio de tenant no válido")
        
        schema = get_schema_from_domain(tenant_domain)
        current_user = await get_user_by_email(user_email, schema)
        
        if not current_user or current_user.get("rol") != "director":
            raise HTTPException(status_code=403, detail="Solo los directores pueden actualizar estudiantes")
        
        # Si se está actualizando el email, verificar que pertenezca al mismo tenant
        if student_update.email:
            student_domain = get_tenant_from_email(student_update.email)
            if student_domain != tenant_domain:
                raise HTTPException(status_code=400, detail="El email del estudiante debe pertenecer al mismo dominio institucional")
        
        try:
            # Crear el objeto de actualización excluyendo campos None
            update_data = {}
            if student_update.nombre is not None:
                update_data["nombre"] = student_update.nombre
            if student_update.apellido is not None:
                update_data["apellido"] = student_update.apellido
            if student_update.email is not None:
                update_data["email"] = student_update.email
            if student_update.telefono is not None:
                update_data["telefono"] = student_update.telefono
            if student_update.direccion is not None:
                update_data["direccion"] = student_update.direccion
            if student_update.fecha_nacimiento is not None:
                update_data["fecha_nacimiento"] = student_update.fecha_nacimiento
            if student_update.rol is not None:
                update_data["rol"] = student_update.rol
            
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
                    f"{SUPABASE_URL}/rest/v1/{table_name}?id=eq.{student_id}&rol=eq.estudiante",
                    headers=headers,
                    json=update_data
                )
                
                if response.status_code == 200 or response.status_code == 204:
                    # Obtener el estudiante actualizado
                    return await StudentController.get_student_by_id(student_id, user_email)
                else:
                    raise HTTPException(status_code=response.status_code, detail="Error al actualizar estudiante")
                    
        except Exception as e:
            print(f"❌ Error actualizando estudiante: {e}")
            raise HTTPException(status_code=500, detail=f"Error interno del servidor: {str(e)}")

    @staticmethod
    async def delete_student(student_id: int, user_email: str) -> Dict:
        """Eliminar un estudiante (solo para directores)"""
        # Verificar permisos
        tenant_domain = get_tenant_from_email(user_email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Dominio de tenant no válido")
        
        schema = get_schema_from_domain(tenant_domain)
        current_user = await get_user_by_email(user_email, schema)
        
        if not current_user or current_user.get("rol") != "director":
            raise HTTPException(status_code=403, detail="Solo los directores pueden eliminar estudiantes")
        
        try:
            async with httpx.AsyncClient(timeout=10.0) as client:
                headers = {
                    "apikey": SUPABASE_SERVICE_ROLE_KEY,
                    "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}"
                }
                
                table_name = f"{schema}_usuarios"
                response = await client.delete(
                    f"{SUPABASE_URL}/rest/v1/{table_name}?id=eq.{student_id}&rol=eq.estudiante",
                    headers=headers
                )
                
                if response.status_code == 200 or response.status_code == 204:
                    return {"message": "Estudiante eliminado exitosamente"}
                else:
                    raise HTTPException(status_code=response.status_code, detail="Error al eliminar estudiante")
                    
        except Exception as e:
            print(f"❌ Error eliminando estudiante: {e}")
            raise HTTPException(status_code=500, detail=f"Error interno del servidor: {str(e)}")

    @staticmethod
    async def get_students_by_course(curso_id: int, user_email: str) -> List[Dict]:
        """Obtener estudiantes inscritos en un curso específico (para directores y profesores)"""
        # Verificar permisos
        tenant_domain = get_tenant_from_email(user_email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Dominio de tenant no válido")
        
        schema = get_schema_from_domain(tenant_domain)
        current_user = await get_user_by_email(user_email, schema)
        
        if not current_user or current_user.get("rol") not in ["director", "profesor"]:
            raise HTTPException(status_code=403, detail="Solo los directores y profesores pueden ver estudiantes por curso")
        
        try:
            async with httpx.AsyncClient(timeout=10.0) as client:
                headers = {
                    "apikey": SUPABASE_SERVICE_ROLE_KEY,
                    "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}"
                }
                
                # Obtener las inscripciones del curso
                inscripciones_table = f"{schema}_inscripciones"
                usuarios_table = f"{schema}_usuarios"
                
                # Primero obtener las inscripciones del curso
                response = await client.get(
                    f"{SUPABASE_URL}/rest/v1/{inscripciones_table}?curso_id=eq.{curso_id}&select=usuario_id",
                    headers=headers
                )
                
                if response.status_code == 200:
                    inscripciones = response.json()
                    if not inscripciones:
                        return []
                    
                    # Obtener los IDs de los usuarios inscritos
                    usuario_ids = [str(inscripcion["usuario_id"]) for inscripcion in inscripciones]
                    
                    # Obtener los datos de los estudiantes
                    usuarios_query = f"id=in.({','.join(usuario_ids)})&rol=eq.estudiante"
                    response = await client.get(
                        f"{SUPABASE_URL}/rest/v1/{usuarios_table}?{usuarios_query}&select=*",
                        headers=headers
                    )
                    
                    if response.status_code == 200:
                        return response.json()
                    else:
                        raise HTTPException(status_code=response.status_code, detail="Error al obtener datos de estudiantes")
                else:
                    raise HTTPException(status_code=response.status_code, detail="Error al obtener inscripciones")
                    
        except Exception as e:
            print(f"❌ Error obteniendo estudiantes por curso: {e}")
            raise HTTPException(status_code=500, detail=f"Error interno del servidor: {str(e)}")