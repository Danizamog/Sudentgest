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
    """Obtener dominio del tenant según el email - Similar al C# Program.cs"""
    if not email:
        return None
    
    email = email.lower().strip()
    
    if email.endswith("@ucb.edu.bo"):
        return "ucb.edu.bo"
    elif email.endswith("@upb.edu.bo"):
        return "upb.edu.bo"
    elif email.endswith("@gmail.com"):
        return "gmail.com"
    
    return "unknown"

def get_tenant_table_prefix(tenant: str) -> str:
    """Obtener prefijo de tabla según el tenant"""
    tenant_mapping = {
        "ucb.edu.bo": "tenant_ucb",
        "upb.edu.bo": "tenant_upb", 
        "gmail.com": "tenant_gmail"
    }
    return tenant_mapping.get(tenant, "tenant_unknown")

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
        raise HTTPException(status_code=401, detail="Token inválido")

async def get_user_by_email(email: str, tenant: str) -> Optional[Dict]:
    """Obtener datos del usuario por email según el tenant"""
    try:
        table_prefix = get_tenant_table_prefix(tenant)
        table_name = f"{table_prefix}_usuarios"
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_SERVICE_ROLE_KEY,
                "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}"
            }
            response = await client.get(
                f"{SUPABASE_URL}/rest/v1/{table_name}?email=eq.{email}&select=*",
                headers=headers
            )
            if response.status_code == 200:
                users = response.json()
                return users[0] if users else None
    except Exception as e:
        return None

async def create_contact_message(contact_data: Dict, tenant: str) -> Optional[Dict]:
    """Crear mensaje de contacto en la tabla correspondiente al tenant"""
    try:
        table_prefix = get_tenant_table_prefix(tenant)
        table_name = f"{table_prefix}_contact_messages"
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_SERVICE_ROLE_KEY,
                "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}",
                "Content-Type": "application/json",
                "Prefer": "return=representation"
            }
            
            response = await client.post(
                f"{SUPABASE_URL}/rest/v1/{table_name}",
                headers=headers,
                json=contact_data
            )
            
            if response.status_code == 201:
                try:
                    result = response.json()
                    return result
                except Exception as json_error:
                    # Return a basic success response if JSON parsing fails
                    return {"success": True, "message": "Contact message created"}
            elif response.status_code == 409:
                return None
            elif response.status_code == 400:
                return None
            else:
                return None
                
    except Exception as e:
        return None

async def get_contact_messages_by_tenant(tenant: str, limit: int = 50) -> list:
    """Obtener mensajes de contacto del tenant"""
    try:
        table_prefix = get_tenant_table_prefix(tenant)
        table_name = f"{table_prefix}_contact_messages"
        
        async with httpx.AsyncClient(timeout=10.0) as client:
            headers = {
                "apikey": SUPABASE_SERVICE_ROLE_KEY,
                "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}"
            }
            response = await client.get(
                f"{SUPABASE_URL}/rest/v1/{table_name}?order=created_at.desc&limit={limit}",
                headers=headers
            )
            if response.status_code == 200:
                return response.json()
    except Exception as e:
        return []