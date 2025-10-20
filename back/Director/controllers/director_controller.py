import httpx
import os
import secrets
import string
from typing import List, Optional
from models.usuario import UsuarioCreate, UsuarioUpdate, UsuarioResponse
from utils.supabase import supabase_request, get_tenant_schema

SUPABASE_URL = os.getenv("SUPABASE_URL")
SUPABASE_SERVICE_ROLE_KEY = os.getenv("SUPABASE_SERVICE_ROLE_KEY")

def generate_password(length: int = 12) -> str:
    """Generate a secure random password"""
    characters = string.ascii_letters + string.digits + "!@#$%^&*"
    return ''.join(secrets.choice(characters) for _ in range(length))

async def create_supabase_auth_user(email: str, password: str) -> dict:
    """Create a user in Supabase Auth using Admin API"""
    url = f"{SUPABASE_URL}/auth/v1/admin/users"
    headers = {
        "apikey": SUPABASE_SERVICE_ROLE_KEY,
        "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}",
        "Content-Type": "application/json"
    }
    
    payload = {
        "email": email,
        "password": password,
        "email_confirm": True,  # Auto-confirm email
        "user_metadata": {
            "created_by": "director_panel"
        }
    }
    
    async with httpx.AsyncClient() as client:
        response = await client.post(url, headers=headers, json=payload)
        response.raise_for_status()
        return response.json()

async def delete_supabase_auth_user(user_id: str) -> bool:
    """Delete a user from Supabase Auth"""
    url = f"{SUPABASE_URL}/auth/v1/admin/users/{user_id}"
    headers = {
        "apikey": SUPABASE_SERVICE_ROLE_KEY,
        "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}"
    }
    
    async with httpx.AsyncClient() as client:
        response = await client.delete(url, headers=headers)
        return response.status_code == 200

async def get_all_users(tenant_schema: str) -> List[UsuarioResponse]:
    """Get all users from a tenant's usuarios table"""
    table_name = f"{tenant_schema}_usuarios"
    endpoint = f"{table_name}?select=*&order=created_at.desc"
    
    try:
        data = await supabase_request("GET", endpoint)
        return [UsuarioResponse(**user) for user in data]
    except Exception as e:
        print(f"❌ Error getting users: {e}")
        raise

async def get_user_by_id(tenant_schema: str, user_id: int) -> Optional[UsuarioResponse]:
    """Get a specific user by ID"""
    table_name = f"{tenant_schema}_usuarios"
    endpoint = f"{table_name}?id=eq.{user_id}&select=*"
    
    try:
        data = await supabase_request("GET", endpoint)
        if data and len(data) > 0:
            return UsuarioResponse(**data[0])
        return None
    except Exception as e:
        print(f"❌ Error getting user: {e}")
        raise

async def get_user_by_email(tenant_schema: str, email: str) -> Optional[UsuarioResponse]:
    """Get a user by email"""
    table_name = f"{tenant_schema}_usuarios"
    endpoint = f"{table_name}?email=eq.{email}&select=*"
    
    try:
        data = await supabase_request("GET", endpoint)
        if data and len(data) > 0:
            return UsuarioResponse(**data[0])
        return None
    except Exception as e:
        print(f"❌ Error getting user by email: {e}")
        raise

async def create_user(tenant_schema: str, user_data: UsuarioCreate) -> dict:
    """
    Create a new user in both Supabase Auth and the tenant's usuarios table
    Returns: dict with user info and generated password
    """
    table_name = f"{tenant_schema}_usuarios"
    
    # Check if user already exists
    existing = await get_user_by_email(tenant_schema, user_data.email)
    if existing:
        raise ValueError(f"User with email {user_data.email} already exists")
    
    # Generate password for Auth
    password = generate_password()
    
    # Create Auth user first
    try:
        auth_user = await create_supabase_auth_user(user_data.email, password)
        print(f"✅ Created Auth user: {auth_user.get('id')}")
    except Exception as e:
        print(f"❌ Error creating Auth user: {e}")
        raise ValueError(f"Failed to create Auth user: {str(e)}")
    
    # Create database record
    db_data = {
        "nombre": user_data.nombre,
        "apellido": user_data.apellido,
        "email": user_data.email,
        "rol": user_data.rol
    }
    
    try:
        endpoint = f"{table_name}"
        created_users = await supabase_request("POST", endpoint, db_data)
        
        if created_users and len(created_users) > 0:
            created_user = created_users[0]
            return {
                "user": UsuarioResponse(**created_user),
                "password": password,
                "auth_user_id": auth_user.get('id')
            }
        else:
            raise ValueError("No user was created in database")
    except Exception as e:
        # Rollback: delete the Auth user if DB creation failed
        print(f"❌ Error creating DB user, rolling back Auth user: {e}")
        await delete_supabase_auth_user(auth_user.get('id'))
        raise

async def update_user(tenant_schema: str, user_id: int, user_data: UsuarioUpdate) -> UsuarioResponse:
    """Update a user's information"""
    table_name = f"{tenant_schema}_usuarios"
    
    # Build update payload (only non-None fields)
    update_data = {k: v for k, v in user_data.dict().items() if v is not None}
    
    if not update_data:
        raise ValueError("No fields to update")
    
    endpoint = f"{table_name}?id=eq.{user_id}"
    
    try:
        updated_users = await supabase_request("PATCH", endpoint, update_data)
        
        if updated_users and len(updated_users) > 0:
            return UsuarioResponse(**updated_users[0])
        else:
            raise ValueError(f"User with ID {user_id} not found")
    except Exception as e:
        print(f"❌ Error updating user: {e}")
        raise

async def delete_user(tenant_schema: str, user_id: int) -> bool:
    """Delete a user from the tenant's usuarios table"""
    table_name = f"{tenant_schema}_usuarios"
    
    # Get user first to get email for Auth deletion
    user = await get_user_by_id(tenant_schema, user_id)
    if not user:
        raise ValueError(f"User with ID {user_id} not found")
    
    endpoint = f"{table_name}?id=eq.{user_id}"
    
    try:
        await supabase_request("DELETE", endpoint)
        print(f"✅ Deleted user {user_id} from database")
        
        # Note: We can't easily delete from Auth without the Auth user ID
        # For production, you'd want to store auth_user_id in the usuarios table
        # For now, we just delete from the database
        
        return True
    except Exception as e:
        print(f"❌ Error deleting user: {e}")
        raise
