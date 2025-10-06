from fastapi import APIRouter, Header
import sys
import os

# Añadir path para imports
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from models.course import Course, CourseEnrollment
from controllers.course_controller import CourseController
from utils.supabase import get_current_user

router = APIRouter(prefix="/api/courses", tags=["Courses"])

@router.get("/")
async def list_courses(authorization: str = Header(None)):
    """Listar todos los cursos del tenant"""
    user = await get_current_user(authorization)
    return await CourseController.list_courses(user["email"])

@router.post("/")
async def create_course(course: Course, authorization: str = Header(None)):
    """Crear nuevo curso (solo directores/admin)"""
    user = await get_current_user(authorization)
    return await CourseController.create_course(course, user["email"])

@router.post("/enroll")
async def enroll_course(enrollment: CourseEnrollment, authorization: str = Header(None)):
    """Inscribir estudiante/profesor en curso"""
    user = await get_current_user(authorization)
    return await CourseController.enroll_course(enrollment, user["email"])

@router.get("/my-courses")
async def get_my_courses(authorization: str = Header(None)):
    """Obtener cursos del usuario actual"""
    user = await get_current_user(authorization)
    return await CourseController.get_my_courses(user["email"])

@router.get("/{curso_id}/enrollments")
async def get_course_enrollments(curso_id: int, authorization: str = Header(None)):
    """Obtener estudiantes inscritos en un curso (solo directores/admin)"""
    user = await get_current_user(authorization)
    return await CourseController.get_course_enrollments(curso_id, user["email"])

@router.delete("/enrollments/{inscripcion_id}")
async def delete_enrollment(inscripcion_id: int, authorization: str = Header(None)):
    """Eliminar inscripción de un curso (solo directores/admin)"""
    user = await get_current_user(authorization)
    return await CourseController.delete_enrollment(inscripcion_id, user["email"])
