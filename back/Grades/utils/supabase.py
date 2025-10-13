import httpx
import os
from typing import Optional, Dict
from fastapi import HTTPException, Header
import jwt

# Variables de entorno
SUPABASE_URL = os.getenv("SUPABASE_URL")
SUPABASE_ANON_KEY = os.getenv("SUPABASE_ANON_KEY")
SUPABASE_SERVICE_ROLE_KEY = os.getenv("SUPABASE_SERVICE_ROLE_KEY")
SUPABASE_JWT_SECRET = os.getenv("SUPABASE_JWT_SECRET", SUPABASE_ANON_KEY)

# =========================================================
# FUNCIONES DE UTILIDAD
# =========================================================

def get_tenant_from_email(email: str) -> Optional[str]:
    """Determina el dominio del tenant a partir del email institucional."""
    if not email:
        return None
    email = email.lower()
    if email.endswith("@ucb.edu.bo"):
        return "ucb.edu.bo"
    elif email.endswith("@upb.edu.bo"):
        return "upb.edu.bo"
    elif email.endswith("@gmail.com"):
        return "gmail.com"
    return None


def get_schema_from_domain(domain: str) -> str:
    """Devuelve el schema correspondiente según el dominio."""
    domain_mapping = {
        "ucb.edu.bo": "tenant_ucb",
        "upb.edu.bo": "tenant_upb",
        "gmail.com": "tenant_gmail"
    }
    return domain_mapping.get(domain, "tenant_default")


# =========================================================
# VALIDACIÓN DEL USUARIO AUTENTICADO
# =========================================================

async def get_current_user(authorization: str = Header(None)) -> Dict:
    """Extrae y valida el usuario actual a partir del token JWT."""
    if not authorization or not authorization.startswith("Bearer "):
        raise HTTPException(status_code=401, detail="Token no proporcionado")
    
    token = authorization.split(" ")[1]
    try:
        payload = jwt.decode(
            token,
            SUPABASE_JWT_SECRET,
            algorithms=["HS256"],
            audience="authenticated",
            options={"verify_aud": True}
        )
        email = payload.get("email")
        if not email:
            raise HTTPException(status_code=401, detail="Email no encontrado en token")
        return {"email": email, "user_id": payload.get("sub")}
    except jwt.ExpiredSignatureError:
        raise HTTPException(status_code=401, detail="Token expirado")
    except jwt.InvalidTokenError as e:
        print(f"❌ Token inválido: {e}")
        raise HTTPException(status_code=401, detail="Token inválido")


# =========================================================
# CONSULTAS A SUPABASE
# =========================================================

async def get_tenant_info(domain: str) -> Optional[Dict]:
    """Obtiene la información del tenant (institución) según su dominio."""
    try:
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_ANON_KEY,
                "Authorization": f"Bearer {SUPABASE_ANON_KEY}"
            }
            response = await client.get(
                f"{SUPABASE_URL}/rest/v1/tenants?domain=eq.{domain}&select=*",
                headers=headers
            )
            if response.status_code == 200:
                tenants = response.json()
                return tenants[0] if tenants else None
    except Exception as e:
        print(f"❌ Error obteniendo tenant info: {e}")
    return None


async def get_user_by_email(email: str, schema: str) -> Optional[Dict]:
    """Obtiene los datos del usuario autenticado desde el schema correspondiente."""
    try:
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_SERVICE_ROLE_KEY,
                "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}"
            }
            table_name = f"{schema}_usuarios"
            response = await client.get(
                f"{SUPABASE_URL}/rest/v1/{table_name}?email=eq.{email}&select=*",
                headers=headers
            )
            if response.status_code == 200:
                users = response.json()
                return users[0] if users else None
    except Exception as e:
        print(f"❌ Error obteniendo usuario: {e}")
    return None


async def get_grades_from_course(schema: str, course_id: int) -> Optional[list]:
    """Obtiene todas las notas de un curso específico desde el schema correspondiente."""
    try:
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_SERVICE_ROLE_KEY,
                "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}"
            }
            table_name = f"{schema}_notas"
            response = await client.get(
                f"{SUPABASE_URL}/rest/v1/{table_name}?course_id=eq.{course_id}&select=*",
                headers=headers
            )
            if response.status_code == 200:
                return response.json()
    except Exception as e:
        print(f"❌ Error obteniendo notas del curso: {e}")
    return None
