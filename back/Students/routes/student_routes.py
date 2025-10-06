from fastapi import APIRouter, Header
import sys
import os

# Añadir path para imports
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from models.student import StudentCreate, StudentUpdate
from controllers.student_controller import StudentController
from utils.supabase import get_current_user

router = APIRouter(prefix="/api/students", tags=["Students"])

@router.get("/")
async def get_all_students(authorization: str = Header(None)):
    """Obtener todos los estudiantes del tenant (para directores y profesores)"""
    user = await get_current_user(authorization)
    return await StudentController.get_all_students(user["email"])

@router.post("/")
async def create_student(student: StudentCreate, authorization: str = Header(None)):
    """Crear un nuevo estudiante (solo para directores)"""
    user = await get_current_user(authorization)
    return await StudentController.create_student(student, user["email"])

@router.get("/{student_id}")
async def get_student_by_id(student_id: int, authorization: str = Header(None)):
    """Obtener un estudiante por ID (para directores y profesores)"""
    user = await get_current_user(authorization)
    return await StudentController.get_student_by_id(student_id, user["email"])

@router.put("/{student_id}")
async def update_student(student_id: int, student_update: StudentUpdate, authorization: str = Header(None)):
    """Actualizar un estudiante (solo para directores)"""
    user = await get_current_user(authorization)
    return await StudentController.update_student(student_id, student_update, user["email"])

@router.delete("/{student_id}")
async def delete_student(student_id: int, authorization: str = Header(None)):
    """Eliminar un estudiante (solo para directores)"""
    user = await get_current_user(authorization)
    return await StudentController.delete_student(student_id, user["email"])

@router.get("/course/{curso_id}")
async def get_students_by_course(curso_id: int, authorization: str = Header(None)):
    """Obtener estudiantes inscritos en un curso específico (para directores y profesores)"""
    user = await get_current_user(authorization)
    return await StudentController.get_students_by_course(curso_id, user["email"])