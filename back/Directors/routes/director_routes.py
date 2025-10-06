from fastapi import APIRouter, Header
from pydantic import BaseModel
import sys
import os

# Añadir path para imports
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from models.director import DirectorCreate, DirectorUpdate
from controllers.director_controller import DirectorController
from utils.supabase import get_current_user

class RoleUpdate(BaseModel):
    new_role: str

router = APIRouter(prefix="/api/directors", tags=["Directors"])

@router.get("/overview")
async def get_institution_overview(authorization: str = Header(None)):
    """Obtener resumen completo de la institución (solo para directores)"""
    user = await get_current_user(authorization)
    return await DirectorController.get_institution_overview(user["email"])

@router.get("/all-users")
async def get_all_users(authorization: str = Header(None)):
    """Obtener todos los usuarios organizados por rol (solo para directores)"""
    user = await get_current_user(authorization)
    return await DirectorController.get_all_users(user["email"])

@router.get("/")
async def get_all_directors(authorization: str = Header(None)):
    """Obtener todos los directores del tenant (solo para directores)"""
    user = await get_current_user(authorization)
    return await DirectorController.get_all_directors(user["email"])

@router.post("/")
async def create_director(director: DirectorCreate, authorization: str = Header(None)):
    """Crear un nuevo director (solo para directores existentes)"""
    user = await get_current_user(authorization)
    return await DirectorController.create_director(director, user["email"])

@router.patch("/users/{user_id}/role")
async def update_user_role(user_id: int, role_update: RoleUpdate, authorization: str = Header(None)):
    """Actualizar el rol de un usuario (solo para directores)"""
    user = await get_current_user(authorization)
    return await DirectorController.update_user_role(user_id, role_update.new_role, user["email"])