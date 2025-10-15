import httpx
from fastapi import HTTPException
from datetime import datetime
from typing import Dict
import sys
import os

# Añadir path para imports
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


from models.course import Course, CourseEnrollment
from utils.supabase import (
    SUPABASE_URL, SUPABASE_ANON_KEY, SUPABASE_SERVICE_ROLE_KEY,
    get_tenant_from_email, get_tenant_info, get_user_by_email
)

class CourseController:
    
    @staticmethod
    async def list_courses(email: str) -> Dict:
        """Listar todos los cursos del tenant"""
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
            table_name = f"{schema}_cursos"
            response = await client.get(
                f"{SUPABASE_URL}/rest/v1/{table_name}?select=*&order=nombre.asc",
                headers=headers
            )
            if response.status_code == 200:
                return {"tenant": schema, "cursos": response.json()}
            else:
                raise HTTPException(status_code=500, detail="Error al obtener cursos")
    
    @staticmethod
    async def create_course(course: Course, email: str) -> Dict:
        """Crear nuevo curso (solo directores/admin)"""
        tenant_domain = get_tenant_from_email(email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Tenant no identificado")
        
        tenant_info = await get_tenant_info(tenant_domain)
        if not tenant_info:
            raise HTTPException(status_code=404, detail="Tenant no encontrado")
        
        schema = tenant_info["schema_name"]
        
        user_data = await get_user_by_email(email, schema)
        if not user_data or user_data.get("rol") not in ["Director", "admin"]:
            raise HTTPException(status_code=403, detail="No tienes permisos para crear cursos")
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_SERVICE_ROLE_KEY,
                "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}",
                "Content-Type": "application/json",
                "Prefer": "return=representation"
            }
            table_name = f"{schema}_cursos"
            payload = {
                "nombre": course.nombre,
                "codigo": course.codigo,
                "descripcion": course.descripcion,
                "creditos": course.creditos,
                "horario": course.horario,
                "created_at": datetime.utcnow().isoformat()
            }
            response = await client.post(
                f"{SUPABASE_URL}/rest/v1/{table_name}",
                json=payload,
                headers=headers
            )
            if response.status_code in [200, 201]:
                return {"success": True, "curso": response.json()}
            else:
                raise HTTPException(status_code=500, detail=f"Error al crear curso: {response.text}")
    
    @staticmethod
    async def enroll_course(enrollment: CourseEnrollment, email: str) -> Dict:
        """Inscribir estudiante/profesor en curso"""
        tenant_domain = get_tenant_from_email(email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Tenant no identificado")
        
        tenant_info = await get_tenant_info(tenant_domain)
        if not tenant_info:
            raise HTTPException(status_code=404, detail="Tenant no encontrado")
        
        schema = tenant_info["schema_name"]
        user_data = await get_user_by_email(email, schema)
        if not user_data or user_data.get("rol") not in ["Director", "admin"]:
            raise HTTPException(status_code=403, detail="No tienes permisos para inscribir")
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_SERVICE_ROLE_KEY,
                "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}",
                "Content-Type": "application/json",
                "Prefer": "return=representation"
            }
            table_name = f"{schema}_inscripciones"
            payload = {
                "curso_id": enrollment.curso_id,
                "usuario_id": enrollment.usuario_id,
                "created_at": datetime.utcnow().isoformat()
            }
            response = await client.post(
                f"{SUPABASE_URL}/rest/v1/{table_name}",
                json=payload,
                headers=headers
            )
            if response.status_code in [200, 201]:
                return {"success": True, "inscripcion": response.json()}
            else:
                raise HTTPException(status_code=500, detail=f"Error al inscribir: {response.text}")
    
    @staticmethod
    async def get_my_courses(email: str) -> Dict:
        """Obtener cursos del usuario actual (estudiantes Y profesores)"""
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
        user_rol = user_data.get("rol", "Estudiante")
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_ANON_KEY,
                "Authorization": f"Bearer {SUPABASE_ANON_KEY}"
            }
            
            cursos_table = f"{schema}_cursos"
            
            # Si es profesor, obtener cursos donde está asignado
            if user_rol.lower() == "profesor":
                cursos_response = await client.get(
                    f"{SUPABASE_URL}/rest/v1/{cursos_table}?profesor_id=eq.{user_id}&select=*&order=nombre.asc",
                    headers=headers
                )
                
                if cursos_response.status_code == 200:
                    cursos = cursos_response.json()
                    return {"usuario": email, "rol": user_rol, "cursos": cursos}
                else:
                    raise HTTPException(status_code=500, detail="Error al obtener cursos del profesor")
            
            # Si es estudiante, obtener cursos por inscripciones
            inscripciones_table = f"{schema}_inscripciones"
            
            response = await client.get(
                f"{SUPABASE_URL}/rest/v1/{inscripciones_table}?usuario_id=eq.{user_id}&select=*",
                headers=headers
            )
            
            if response.status_code == 200:
                inscripciones = response.json()
                curso_ids = [insc["curso_id"] for insc in inscripciones]
                
                if not curso_ids:
                    return {"usuario": email, "rol": user_rol, "cursos": []}
                
                ids_query = ",".join(map(str, curso_ids))
                
                cursos_response = await client.get(
                    f"{SUPABASE_URL}/rest/v1/{cursos_table}?id=in.({ids_query})&select=*",
                    headers=headers
                )
                
                if cursos_response.status_code == 200:
                    cursos = cursos_response.json()
                    return {"usuario": email, "rol": user_rol, "cursos": cursos}
                else:
                    raise HTTPException(status_code=500, detail="Error al obtener cursos")
            else:
                raise HTTPException(status_code=500, detail="Error al obtener inscripciones")
    
    @staticmethod
    async def get_course_enrollments(curso_id: int, email: str) -> Dict:
        """Obtener estudiantes inscritos en un curso con sus datos completos"""
        tenant_domain = get_tenant_from_email(email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Tenant no identificado")
        
        tenant_info = await get_tenant_info(tenant_domain)
        if not tenant_info:
            raise HTTPException(status_code=404, detail="Tenant no encontrado")
        
        schema = tenant_info["schema_name"]
        
        # Verificar que el usuario tenga permisos (profesor del curso o director)
        user_data = await get_user_by_email(email, schema)
        if not user_data or user_data.get("rol") not in ["director", "admin"]:
            raise HTTPException(status_code=403, detail="No tienes permisos")
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_ANON_KEY,
                "Authorization": f"Bearer {SUPABASE_ANON_KEY}"
            }
            
            # Si es profesor, verificar que esté inscrito en el curso
            if user_rol == "profesor":
                inscripciones_table = f"{schema}_inscripciones"
                check_response = await client.get(
                    f"{SUPABASE_URL}/rest/v1/{inscripciones_table}?curso_id=eq.{curso_id}&usuario_id=eq.{user_id}&select=id",
                    headers=headers
                )
                
                if check_response.status_code != 200 or not check_response.json():
                    raise HTTPException(
                        status_code=403, 
                        detail="No tienes permiso para ver los estudiantes de este curso"
                    )
            
            # Si es director o profesor autorizado, obtener inscripciones
            inscripciones_table = f"{schema}_inscripciones"
            usuarios_table = f"{schema}_usuarios"
            
            # Obtener inscripciones del curso
            inscripciones_response = await client.get(
                f"{SUPABASE_URL}/rest/v1/{inscripciones_table}?curso_id=eq.{curso_id}&select=usuario_id",
                headers=headers
            )
            
            if inscripciones_response.status_code == 200:
                inscripciones = inscripciones_response.json()
                usuario_ids = [insc["usuario_id"] for insc in inscripciones]
                
                if not usuario_ids:
                    return {"curso_id": curso_id, "inscritos": []}
                
                # Obtener datos de usuarios
                usuarios_table = f"{schema}_usuarios"
                ids_query = ",".join(map(str, usuario_ids))
                
                usuarios_response = await client.get(
                    f"{SUPABASE_URL}/rest/v1/{usuarios_table}?id=in.({ids_query})&select=id,nombre,apellido,email,rol",
                    headers=headers
                )
                
                if usuarios_response.status_code == 200:
                    usuarios = usuarios_response.json()
                    # Combinar inscripciones con datos de usuario
                    inscritos = []
                    for insc in inscripciones:
                        usuario = next((u for u in usuarios if u["id"] == insc["usuario_id"]), None)
                        if usuario:
                            inscritos.append({
                                "inscripcion_id": insc["id"],
                                "usuario_id": usuario["id"],
                                "nombre": usuario["nombre"],
                                "apellido": usuario["apellido"],
                                "email": usuario["email"],
                                "rol": usuario["rol"],
                                "fecha_inscripcion": insc.get("created_at")
                            })
                    return {"curso_id": curso_id, "total": len(inscritos), "inscritos": inscritos}
                else:
                    raise HTTPException(status_code=500, detail="Error al obtener usuarios")
            else:
                raise HTTPException(status_code=500, detail="Error al obtener inscripciones")
    
    @staticmethod
    async def delete_enrollment(inscripcion_id: int, email: str) -> Dict:
        """Eliminar inscripción (solo directores/admin)"""
        tenant_domain = get_tenant_from_email(email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Tenant no identificado")
        
        tenant_info = await get_tenant_info(tenant_domain)
        if not tenant_info:
            raise HTTPException(status_code=404, detail="Tenant no encontrado")
        
        schema = tenant_info["schema_name"]
        
        # Verificar permisos
        user_data = await get_user_by_email(email, schema)
        if not user_data or user_data.get("rol") not in ["Director", "admin"]:
            raise HTTPException(status_code=403, detail="No tienes permisos para eliminar inscripciones")
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_SERVICE_ROLE_KEY,
                "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}"
            }
            table_name = f"{schema}_inscripciones"
            response = await client.delete(
                f"{SUPABASE_URL}/rest/v1/{table_name}?id=eq.{inscripcion_id}",
                headers=headers
            )
            if response.status_code in [200, 204]:
                return {"success": True, "message": "Inscripción eliminada"}
            else:
                raise HTTPException(status_code=500, detail=f"Error al eliminar inscripción: {response.text}")
    
    @staticmethod
    async def update_course(curso_id: int, course: Course, email: str) -> Dict:
        """Actualizar curso (solo directores/admin)"""
        tenant_domain = get_tenant_from_email(email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Tenant no identificado")
        
        tenant_info = await get_tenant_info(tenant_domain)
        if not tenant_info:
            raise HTTPException(status_code=404, detail="Tenant no encontrado")
        
        schema = tenant_info["schema_name"]
        
        # Verificar permisos
        user_data = await get_user_by_email(email, schema)
        if not user_data or user_data.get("rol") not in ["Director", "admin"]:
            raise HTTPException(status_code=403, detail="No tienes permisos para actualizar cursos")
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_SERVICE_ROLE_KEY,
                "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}",
                "Content-Type": "application/json",
                "Prefer": "return=representation"
            }
            table_name = f"{schema}_cursos"
            payload = {
                "nombre": course.nombre,
                "codigo": course.codigo,
                "descripcion": course.descripcion,
                "creditos": course.creditos,
                "horario": course.horario
            }
            response = await client.patch(
                f"{SUPABASE_URL}/rest/v1/{table_name}?id=eq.{curso_id}",
                json=payload,
                headers=headers
            )
            if response.status_code == 200:
                return {"success": True, "curso": response.json()}
            else:
                raise HTTPException(status_code=500, detail=f"Error al actualizar curso: {response.text}")
    
    @staticmethod
    async def delete_course(curso_id: int, email: str) -> Dict:
        """Eliminar curso (solo directores/admin)"""
        tenant_domain = get_tenant_from_email(email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Tenant no identificado")
        
        tenant_info = await get_tenant_info(tenant_domain)
        if not tenant_info:
            raise HTTPException(status_code=404, detail="Tenant no encontrado")
        
        schema = tenant_info["schema_name"]
        
        # Verificar permisos
        user_data = await get_user_by_email(email, schema)
        if not user_data or user_data.get("rol") not in ["Director", "admin"]:
            raise HTTPException(status_code=403, detail="No tienes permisos para eliminar cursos")
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_SERVICE_ROLE_KEY,
                "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}"
            }
            
            # Primero eliminar inscripciones relacionadas
            inscripciones_table = f"{schema}_inscripciones"
            await client.delete(
                f"{SUPABASE_URL}/rest/v1/{inscripciones_table}?curso_id=eq.{curso_id}",
                headers=headers
            )
            
            # Luego eliminar el curso
            cursos_table = f"{schema}_cursos"
            response = await client.delete(
                f"{SUPABASE_URL}/rest/v1/{cursos_table}?id=eq.{curso_id}",
                headers=headers
            )
            
            if response.status_code in [200, 204]:
                return {"success": True, "message": "Curso eliminado exitosamente"}
            else:
                raise HTTPException(status_code=500, detail=f"Error al eliminar curso: {response.text}")
    
    @staticmethod
    async def assign_teacher(curso_id: int, profesor_id: int, email: str) -> Dict:
        """Asignar profesor a un curso (solo directores/admin)"""
        tenant_domain = get_tenant_from_email(email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Tenant no identificado")
        
        tenant_info = await get_tenant_info(tenant_domain)
        if not tenant_info:
            raise HTTPException(status_code=404, detail="Tenant no encontrado")
        
        schema = tenant_info["schema_name"]
        
        # Verificar permisos
        user_data = await get_user_by_email(email, schema)
        if not user_data or user_data.get("rol") not in ["Director", "admin"]:
            raise HTTPException(status_code=403, detail="No tienes permisos para asignar profesores")
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_SERVICE_ROLE_KEY,
                "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}",
                "Content-Type": "application/json",
                "Prefer": "return=representation"
            }
            
            # Verificar que el usuario sea profesor
            usuarios_table = f"{schema}_usuarios"
            user_response = await client.get(
                f"{SUPABASE_URL}/rest/v1/{usuarios_table}?id=eq.{profesor_id}&select=id,rol",
                headers=headers
            )
            
            if user_response.status_code == 200:
                users = user_response.json()
                if not users or users[0].get("rol") != "Profesor":
                    raise HTTPException(status_code=400, detail="El usuario seleccionado no es un profesor")
            else:
                raise HTTPException(status_code=404, detail="Usuario no encontrado")
            
            # Actualizar el curso con el profesor asignado
            cursos_table = f"{schema}_cursos"
            payload = {"profesor_id": profesor_id}
            
            response = await client.patch(
                f"{SUPABASE_URL}/rest/v1/{cursos_table}?id=eq.{curso_id}",
                json=payload,
                headers=headers
            )
            
            if response.status_code == 200:
                return {"success": True, "message": "Profesor asignado exitosamente"}
            else:
                raise HTTPException(status_code=500, detail=f"Error al asignar profesor: {response.text}")
    
    @staticmethod
    async def get_course_students_for_attendance(curso_id: int, email: str) -> Dict:
        """Obtener SOLO estudiantes inscritos en un curso para tomar asistencia (profesores)"""
        tenant_domain = get_tenant_from_email(email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Tenant no identificado")
        
        tenant_info = await get_tenant_info(tenant_domain)
        if not tenant_info:
            raise HTTPException(status_code=404, detail="Tenant no encontrado")
        
        schema = tenant_info["schema_name"]
        
        # Verificar que el usuario tenga permisos
        user_data = await get_user_by_email(email, schema)
        if not user_data:
            raise HTTPException(status_code=404, detail="Usuario no encontrado")
        
        user_rol = user_data.get("rol", "")
        user_id = user_data["id"]
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_ANON_KEY,
                "Authorization": f"Bearer {SUPABASE_ANON_KEY}"
            }
            
            # Si es profesor, verificar que esté asignado al curso (profesor_id en tabla cursos)
            if user_rol.lower() == "profesor":
                cursos_table = f"{schema}_cursos"
                check_response = await client.get(
                    f"{SUPABASE_URL}/rest/v1/{cursos_table}?id=eq.{curso_id}&profesor_id=eq.{user_id}&select=id",
                    headers=headers
                )
                
                if check_response.status_code != 200 or not check_response.json():
                    raise HTTPException(
                        status_code=403, 
                        detail="No tienes permiso para ver estudiantes de este curso"
                    )
            elif user_rol.lower() not in ["director", "admin"]:
                raise HTTPException(status_code=403, detail="No tienes permisos suficientes")
            
            # Obtener inscripciones del curso (solo estudiantes)
            inscripciones_table = f"{schema}_inscripciones"
            inscripciones_response = await client.get(
                f"{SUPABASE_URL}/rest/v1/{inscripciones_table}?curso_id=eq.{curso_id}&select=usuario_id",
                headers=headers
            )
            
            if inscripciones_response.status_code != 200:
                raise HTTPException(status_code=500, detail="Error al obtener inscripciones")
            
            inscripciones = inscripciones_response.json()
            usuario_ids = [insc["usuario_id"] for insc in inscripciones]
            
            if not usuario_ids:
                return {"inscripciones": []}
            
            # Obtener datos de usuarios inscritos
            usuarios_table = f"{schema}_usuarios"
            ids_query = ",".join(map(str, usuario_ids))
            
            usuarios_response = await client.get(
                f"{SUPABASE_URL}/rest/v1/{usuarios_table}?id=in.({ids_query})&select=id,nombre,apellido,email,rol",
                headers=headers
            )
            
            if usuarios_response.status_code == 200:
                usuarios = usuarios_response.json()
                # Filtrar SOLO estudiantes
                estudiantes = [
                    {"usuario_id": u["id"]}
                    for u in usuarios 
                    if u.get("rol", "").lower() == "estudiante"
                ]
                return {"inscripciones": estudiantes}
            else:
                raise HTTPException(status_code=500, detail="Error al obtener usuarios")
