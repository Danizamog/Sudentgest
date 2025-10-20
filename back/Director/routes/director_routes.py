from fastapi import APIRouter, HTTPException, Header, status
from typing import Optional, List
from models.usuario import UsuarioCreate, UsuarioUpdate, UsuarioResponse
from controllers.director_controller import (
    get_all_users,
    get_user_by_id,
    create_user,
    update_user,
    delete_user
)
from utils.supabase import get_tenant_schema

router = APIRouter(prefix="/api/director", tags=["Director"])

def extract_tenant_from_header(x_user_email: Optional[str]) -> str:
    """Extract and validate tenant from user email header"""
    if not x_user_email:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="X-User-Email header is required"
        )
    
    try:
        return get_tenant_schema(x_user_email)
    except ValueError as e:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=str(e)
        )

@router.get("/users", response_model=List[UsuarioResponse])
async def list_users(x_user_email: Optional[str] = Header(None)):
    """
    Get all users (students and teachers) in the director's tenant
    Requires X-User-Email header to determine tenant
    """
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    try:
        users = await get_all_users(tenant_schema)
        return users
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error retrieving users: {str(e)}"
        )

@router.get("/users/{user_id}", response_model=UsuarioResponse)
async def get_user(user_id: int, x_user_email: Optional[str] = Header(None)):
    """Get a specific user by ID"""
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    try:
        user = await get_user_by_id(tenant_schema, user_id)
        if not user:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail=f"User with ID {user_id} not found"
            )
        return user
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error retrieving user: {str(e)}"
        )

@router.post("/users", status_code=status.HTTP_201_CREATED)
async def create_new_user(user_data: UsuarioCreate, x_user_email: Optional[str] = Header(None)):
    """
    Create a new user (student or teacher) in the director's tenant
    Also creates a Supabase Auth user with a generated password
    Returns the created user and the password to share with them
    """
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    # Validate role
    if user_data.rol not in ["Estudiante", "Profesor"]:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Rol must be 'Estudiante' or 'Profesor'"
        )
    
    # Validate email domain matches director's tenant
    try:
        user_tenant = get_tenant_schema(user_data.email)
        if user_tenant != tenant_schema:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail=f"User email domain must match your institution. Expected domain for {tenant_schema}"
            )
    except ValueError as e:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=str(e)
        )
    
    try:
        result = await create_user(tenant_schema, user_data)
        return {
            "message": "User created successfully",
            "user": result["user"],
            "password": result["password"],
            "auth_user_id": result["auth_user_id"]
        }
    except ValueError as e:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=str(e)
        )
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error creating user: {str(e)}"
        )

@router.put("/users/{user_id}", response_model=UsuarioResponse)
async def update_existing_user(
    user_id: int, 
    user_data: UsuarioUpdate, 
    x_user_email: Optional[str] = Header(None)
):
    """Update a user's information"""
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    # Validate role if provided
    if user_data.rol and user_data.rol not in ["Estudiante", "Profesor", "Director"]:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Rol must be 'Estudiante', 'Profesor', or 'Director'"
        )
    
    try:
        updated_user = await update_user(tenant_schema, user_id, user_data)
        return updated_user
    except ValueError as e:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=str(e)
        )
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error updating user: {str(e)}"
        )

@router.delete("/users/{user_id}", status_code=status.HTTP_204_NO_CONTENT)
async def delete_existing_user(user_id: int, x_user_email: Optional[str] = Header(None)):
    """Delete a user from the tenant"""
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    try:
        await delete_user(tenant_schema, user_id)
        return None
    except ValueError as e:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=str(e)
        )
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error deleting user: {str(e)}"
        )
