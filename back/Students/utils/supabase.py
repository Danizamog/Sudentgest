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

def get_tenant_from_email(email: str) -> Optional[str]:
    """Obtener dominio del tenant según el email"""
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

async def get_tenant_info(domain: str) -> Optional[Dict]:
    """Obtener información del tenant desde Supabase"""
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

async def get_current_user(authorization: str = Header(None)) -> Dict:
    """Extraer y validar usuario del token JWT"""
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

async def get_user_by_email(email: str, schema: str) -> Optional[Dict]:
    """Obtener datos del usuario por email"""
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

def get_schema_from_domain(domain: str) -> str:
    """Obtener el nombre del schema desde el dominio"""
    domain_mapping = {
        "ucb.edu.bo": "tenant_ucb",
        "upb.edu.bo": "tenant_upb", 
        "gmail.com": "tenant_gmail"
    }
    return domain_mapping.get(domain, "tenant_default")