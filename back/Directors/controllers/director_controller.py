from fastapi import HTTPException
import httpx
import os
from typing import List, Dict, Optional
from models.director import DirectorCreate, DirectorUpdate, InstitutionStats, InstitutionOverview
from utils.supabase import (
    get_tenant_from_email, get_tenant_info, get_user_by_email, 
    get_schema_from_domain, get_institution_name_from_domain
)

SUPABASE_URL = os.getenv("SUPABASE_URL")
SUPABASE_SERVICE_ROLE_KEY = os.getenv("SUPABASE_SERVICE_ROLE_KEY")

class DirectorController:
    @staticmethod
    async def get_institution_overview(user_email: str) -> InstitutionOverview:
        """Obtener resumen completo de la institución (solo para directores)"""
        # Verificar permisos
        tenant_domain = get_tenant_from_email(user_email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Dominio de tenant no válido")
        
        schema = get_schema_from_domain(tenant_domain)
        current_user = await get_user_by_email(user_email, schema)
        
        if not current_user or current_user.get("rol") != "director":
            raise HTTPException(status_code=403, detail="Solo los directores pueden ver el resumen institucional")
        
        try:
            async with httpx.AsyncClient(timeout=10.0) as client:
                headers = {
                    "apikey": SUPABASE_SERVICE_ROLE_KEY,
                    "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}"
                }
                
                # Obtener estadísticas
                usuarios_table = f"{schema}_usuarios"
                cursos_table = f"{schema}_cursos"
                inscripciones_table = f"{schema}_inscripciones"
                
                # Contar estudiantes
                response = await client.get(
                    f"{SUPABASE_URL}/rest/v1/{usuarios_table}?rol=eq.estudiante&select=id",
                    headers={**headers, "Prefer": "count=exact"}
                )
                total_students = int(response.headers.get("Content-Range", "0").split("/")[-1]) if response.status_code == 200 else 0
                
                # Contar profesores
                response = await client.get(
                    f"{SUPABASE_URL}/rest/v1/{usuarios_table}?rol=eq.profesor&select=id",
                    headers={**headers, "Prefer": "count=exact"}
                )
                total_teachers = int(response.headers.get("Content-Range", "0").split("/")[-1]) if response.status_code == 200 else 0
                
                # Contar cursos
                response = await client.get(
                    f"{SUPABASE_URL}/rest/v1/{cursos_table}?select=id",
                    headers={**headers, "Prefer": "count=exact"}
                )
                total_courses = int(response.headers.get("Content-Range", "0").split("/")[-1]) if response.status_code == 200 else 0
                
                # Contar inscripciones
                response = await client.get(
                    f"{SUPABASE_URL}/rest/v1/{inscripciones_table}?select=id",
                    headers={**headers, "Prefer": "count=exact"}
                )
                total_enrollments = int(response.headers.get("Content-Range", "0").split("/")[-1]) if response.status_code == 200 else 0
                
                # Obtener inscripciones recientes
                response = await client.get(
                    f"{SUPABASE_URL}/rest/v1/{inscripciones_table}?select=*&order=created_at.desc&limit=5",
                    headers=headers
                )
                recent_enrollments = response.json() if response.status_code == 200 else []
                
                # Obtener usuarios recientes
                response = await client.get(
                    f"{SUPABASE_URL}/rest/v1/{usuarios_table}?select=*&order=created_at.desc&limit=5",
                    headers=headers
                )
                recent_users = response.json() if response.status_code == 200 else []
                
                stats = InstitutionStats(
                    total_students=total_students,
                    total_teachers=total_teachers,
                    total_courses=total_courses,
                    total_enrollments=total_enrollments
                )
                
                return InstitutionOverview(
                    institution_name=get_institution_name_from_domain(tenant_domain),
                    domain=tenant_domain,
                    stats=stats,
                    recent_enrollments=recent_enrollments,
                    recent_users=recent_users
                )
                
        except Exception as e:
            print(f"❌ Error obteniendo resumen institucional: {e}")
            raise HTTPException(status_code=500, detail=f"Error interno del servidor: {str(e)}")

    @staticmethod
    async def get_all_users(user_email: str) -> Dict:
        """Obtener todos los usuarios organizados por rol (solo para directores)"""
        # Verificar permisos
        tenant_domain = get_tenant_from_email(user_email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Dominio de tenant no válido")
        
        schema = get_schema_from_domain(tenant_domain)
        current_user = await get_user_by_email(user_email, schema)
        
        if not current_user or current_user.get("rol") != "director":
            raise HTTPException(status_code=403, detail="Solo los directores pueden ver todos los usuarios")
        
        try:
            async with httpx.AsyncClient(timeout=10.0) as client:
                headers = {
                    "apikey": SUPABASE_SERVICE_ROLE_KEY,
                    "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}"
                }
                
                usuarios_table = f"{schema}_usuarios"
                
                # Obtener todos los usuarios
                response = await client.get(
                    f"{SUPABASE_URL}/rest/v1/{usuarios_table}?select=*&order=created_at.desc",
                    headers=headers
                )
                
                if response.status_code == 200:
                    all_users = response.json()
                    
                    # Organizar por roles
                    users_by_role = {
                        "directores": [user for user in all_users if user.get("rol") == "director"],
                        "profesores": [user for user in all_users if user.get("rol") == "profesor"],
                        "estudiantes": [user for user in all_users if user.get("rol") == "estudiante"],
                        "otros": [user for user in all_users if user.get("rol") not in ["director", "profesor", "estudiante"]]
                    }
                    
                    return {
                        "institution": get_institution_name_from_domain(tenant_domain),
                        "domain": tenant_domain,
                        "total_users": len(all_users),
                        "users_by_role": users_by_role,
                        "role_counts": {
                            "directores": len(users_by_role["directores"]),
                            "profesores": len(users_by_role["profesores"]),
                            "estudiantes": len(users_by_role["estudiantes"]),
                            "otros": len(users_by_role["otros"])
                        }
                    }
                else:
                    raise HTTPException(status_code=response.status_code, detail="Error al obtener usuarios")
                    
        except Exception as e:
            print(f"❌ Error obteniendo usuarios: {e}")
            raise HTTPException(status_code=500, detail=f"Error interno del servidor: {str(e)}")

    @staticmethod
    async def create_director(director: DirectorCreate, user_email: str) -> Dict:
        """Crear un nuevo director (solo para directores existentes)"""
        # Verificar permisos
        tenant_domain = get_tenant_from_email(user_email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Dominio de tenant no válido")
        
        schema = get_schema_from_domain(tenant_domain)
        current_user = await get_user_by_email(user_email, schema)
        
        if not current_user or current_user.get("rol") != "director":
            raise HTTPException(status_code=403, detail="Solo los directores pueden crear otros directores")
        
        # Verificar que el email del nuevo director pertenezca al mismo tenant
        director_domain = get_tenant_from_email(director.email)
        if director_domain != tenant_domain:
            raise HTTPException(status_code=400, detail="El email del director debe pertenecer al mismo dominio institucional")
        
        try:
            async with httpx.AsyncClient(timeout=10.0) as client:
                headers = {
                    "apikey": SUPABASE_SERVICE_ROLE_KEY,
                    "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}",
                    "Content-Type": "application/json"
                }
                
                table_name = f"{schema}_usuarios"
                director_data = {
                    "nombre": director.nombre,
                    "apellido": director.apellido,
                    "email": director.email,
                    "rol": "director"
                }
                
                response = await client.post(
                    f"{SUPABASE_URL}/rest/v1/{table_name}",
                    headers=headers,
                    json=director_data
                )
                
                if response.status_code == 201:
                    return response.json()[0] if response.json() else director_data
                else:
                    raise HTTPException(status_code=response.status_code, detail="Error al crear director")
                    
        except Exception as e:
            print(f"❌ Error creando director: {e}")
            raise HTTPException(status_code=500, detail=f"Error interno del servidor: {str(e)}")

    @staticmethod
    async def get_all_directors(user_email: str) -> List[Dict]:
        """Obtener todos los directores del tenant (solo para directores)"""
        # Verificar permisos
        tenant_domain = get_tenant_from_email(user_email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Dominio de tenant no válido")
        
        schema = get_schema_from_domain(tenant_domain)
        current_user = await get_user_by_email(user_email, schema)
        
        if not current_user or current_user.get("rol") != "director":
            raise HTTPException(status_code=403, detail="Solo los directores pueden ver la lista de directores")
        
        try:
            async with httpx.AsyncClient(timeout=10.0) as client:
                headers = {
                    "apikey": SUPABASE_SERVICE_ROLE_KEY,
                    "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}"
                }
                
                table_name = f"{schema}_usuarios"
                response = await client.get(
                    f"{SUPABASE_URL}/rest/v1/{table_name}?rol=eq.director&select=*&order=created_at.desc",
                    headers=headers
                )
                
                if response.status_code == 200:
                    return response.json()
                else:
                    raise HTTPException(status_code=response.status_code, detail="Error al obtener directores")
                    
        except Exception as e:
            print(f"❌ Error obteniendo directores: {e}")
            raise HTTPException(status_code=500, detail=f"Error interno del servidor: {str(e)}")

    @staticmethod
    async def update_user_role(user_id: int, new_role: str, director_email: str) -> Dict:
        """Actualizar el rol de un usuario (solo para directores)"""
        # Verificar permisos
        tenant_domain = get_tenant_from_email(director_email)
        if not tenant_domain:
            raise HTTPException(status_code=400, detail="Dominio de tenant no válido")
        
        schema = get_schema_from_domain(tenant_domain)
        current_user = await get_user_by_email(director_email, schema)
        
        if not current_user or current_user.get("rol") != "director":
            raise HTTPException(status_code=403, detail="Solo los directores pueden actualizar roles")
        
        # Validar rol
        valid_roles = ["estudiante", "profesor", "director"]
        if new_role not in valid_roles:
            raise HTTPException(status_code=400, detail=f"Rol no válido. Debe ser uno de: {valid_roles}")
        
        try:
            async with httpx.AsyncClient(timeout=10.0) as client:
                headers = {
                    "apikey": SUPABASE_SERVICE_ROLE_KEY,
                    "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}",
                    "Content-Type": "application/json"
                }
                
                table_name = f"{schema}_usuarios"
                response = await client.patch(
                    f"{SUPABASE_URL}/rest/v1/{table_name}?id=eq.{user_id}",
                    headers=headers,
                    json={"rol": new_role}
                )
                
                if response.status_code == 200 or response.status_code == 204:
                    # Obtener el usuario actualizado
                    response = await client.get(
                        f"{SUPABASE_URL}/rest/v1/{table_name}?id=eq.{user_id}&select=*",
                        headers=headers
                    )
                    
                    if response.status_code == 200:
                        users = response.json()
                        if users:
                            return users[0]
                    
                    return {"message": "Rol actualizado exitosamente"}
                else:
                    raise HTTPException(status_code=response.status_code, detail="Error al actualizar rol")
                    
        except Exception as e:
            print(f"❌ Error actualizando rol: {e}")
            raise HTTPException(status_code=500, detail=f"Error interno del servidor: {str(e)}")