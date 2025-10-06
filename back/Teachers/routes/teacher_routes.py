from fastapi import APIRouter, Header
import sys
import os

# Añadir path para imports
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from models.teacher import TeacherCreate, TeacherUpdate
from controllers.teacher_controller import TeacherController
from utils.supabase import get_current_user

router = APIRouter(prefix="/api/teachers", tags=["Teachers"])

@router.get("/")
async def get_all_teachers(authorization: str = Header(None)):
    """Obtener todos los profesores del tenant (solo para directores)"""
    user = await get_current_user(authorization)
    return await TeacherController.get_all_teachers(user["email"])

@router.post("/")
async def create_teacher(teacher: TeacherCreate, authorization: str = Header(None)):
    """Crear un nuevo profesor (solo para directores)"""
    user = await get_current_user(authorization)
    return await TeacherController.create_teacher(teacher, user["email"])

@router.get("/{teacher_id}")
async def get_teacher_by_id(teacher_id: int, authorization: str = Header(None)):
    """Obtener un profesor por ID (solo para directores)"""
    user = await get_current_user(authorization)
    return await TeacherController.get_teacher_by_id(teacher_id, user["email"])

@router.put("/{teacher_id}")
async def update_teacher(teacher_id: int, teacher_update: TeacherUpdate, authorization: str = Header(None)):
    """Actualizar un profesor (solo para directores)"""
    user = await get_current_user(authorization)
    return await TeacherController.update_teacher(teacher_id, teacher_update, user["email"])

@router.delete("/{teacher_id}")
async def delete_teacher(teacher_id: int, authorization: str = Header(None)):
    """Eliminar un profesor (solo para directores)"""
    user = await get_current_user(authorization)
    return await TeacherController.delete_teacher(teacher_id, user["email"])