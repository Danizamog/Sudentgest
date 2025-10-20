from fastapi import APIRouter, Header, HTTPException, Request
import sys
import os

# Añadir path para imports
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from models.course import Course, CourseEnrollment
from controllers.course_controller import CourseController
from utils.supabase import get_current_user

router = APIRouter(prefix="/api/courses", tags=["Courses"])

# 🔹 ACTUALIZADO: Request añadido a todos los endpoints
@router.get("/")
async def list_courses(request: Request, authorization: str = Header(None)):
    """Listar todos los cursos del tenant"""
    user = await get_current_user(request, authorization)
    return await CourseController.list_courses(user["email"])

@router.post("/")
async def create_course(request: Request, course: Course, authorization: str = Header(None)):
    """Crear nuevo curso (solo directores/admin)"""
    user = await get_current_user(request, authorization)
    return await CourseController.create_course(course, user["email"])

@router.get("/my-courses")
async def get_my_courses(request: Request, authorization: str = Header(None)):
    """Obtener cursos del usuario actual"""
    user = await get_current_user(request, authorization)
    return await CourseController.get_my_courses(user["email"])

@router.get("/{curso_id}/enrollments")
async def get_course_enrollments(request: Request, curso_id: int, authorization: str = Header(None)):
    """Obtener todos los inscritos en un curso"""
    user = await get_current_user(request, authorization)
    return await CourseController.get_course_enrollments(curso_id, user["email"])

@router.get("/{curso_id}/students")
async def get_course_students_for_attendance(request: Request, curso_id: int, authorization: str = Header(None)):
    """Obtener SOLO estudiantes de un curso para asistencia"""
    user = await get_current_user(request, authorization)
    return await CourseController.get_course_students_for_attendance(curso_id, user["email"])

@router.post("/enroll")
async def enroll_course(request: Request, enrollment: CourseEnrollment, authorization: str = Header(None)):
    """Inscribir estudiante/profesor en curso"""
    user = await get_current_user(request, authorization)
    return await CourseController.enroll_course(enrollment, user["email"])

@router.delete("/enroll/{inscripcion_id}")
async def delete_enrollment(request: Request, inscripcion_id: int, authorization: str = Header(None)):
    """Eliminar inscripción"""
    user = await get_current_user(request, authorization)
    return await CourseController.delete_enrollment(inscripcion_id, user["email"])

@router.put("/{curso_id}")
async def update_course(request: Request, curso_id: int, course: Course, authorization: str = Header(None)):
    """Actualizar curso (solo directores/admin)"""
    user = await get_current_user(request, authorization)
    return await CourseController.update_course(curso_id, course, user["email"])

@router.delete("/{curso_id}")
async def delete_course(request: Request, curso_id: int, authorization: str = Header(None)):
    """Eliminar curso (solo directores/admin)"""
    user = await get_current_user(request, authorization)
    return await CourseController.delete_course(curso_id, user["email"])

@router.post("/{curso_id}/assign-teacher")
async def assign_teacher(request: Request, curso_id: int, profesor_id: int, authorization: str = Header(None)):
    """Asignar profesor a un curso (solo directores/admin)"""
    user = await get_current_user(request, authorization)
    return await CourseController.assign_teacher(curso_id, profesor_id, user["email"])
