from fastapi import APIRouter, Header, HTTPException
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
    """Obtener todos los inscritos en un curso con datos completos (directores/admin)"""
    try:
        user = await get_current_user(authorization)
        result = await CourseController.get_course_enrollments(curso_id, user["email"])
        print(f"✅ Enrollments obtenidos para curso {curso_id}: {len(result.get('inscripciones', []))} registros")
        return result
    except Exception as e:
        print(f"❌ Error en get_course_enrollments para curso {curso_id}: {e}")
        raise HTTPException(status_code=500, detail=f"Error al obtener inscripciones: {str(e)}")

@router.get("/{curso_id}/students")
async def get_course_students_for_attendance(curso_id: int, authorization: str = Header(None)):
    """Obtener SOLO estudiantes de un curso para tomar asistencia (profesores)"""
    try:
        user = await get_current_user(authorization)
        result = await CourseController.get_course_students_for_attendance(curso_id, user["email"])
        print(f"✅ Estudiantes obtenidos para curso {curso_id}: {len(result.get('inscripciones', []))} registros")
        return result
    except Exception as e:
        print(f"❌ Error en get_course_students_for_attendance para curso {curso_id}: {e}")
        raise HTTPException(status_code=500, detail=f"Error al obtener estudiantes: {str(e)}")

@router.delete("/enrollments/{inscripcion_id}")
async def delete_enrollment(inscripcion_id: int, authorization: str = Header(None)):
    """Eliminar inscripción de un curso (solo directores/admin)"""
    user = await get_current_user(authorization)
    return await CourseController.delete_enrollment(inscripcion_id, user["email"])

@router.put("/{curso_id}")
async def update_course(curso_id: int, course: Course, authorization: str = Header(None)):
    """Actualizar curso (solo directores/admin)"""
    user = await get_current_user(authorization)
    return await CourseController.update_course(curso_id, course, user["email"])

@router.delete("/{curso_id}")
async def delete_course(curso_id: int, authorization: str = Header(None)):
    """Eliminar curso (solo directores/admin)"""
    user = await get_current_user(authorization)
    return await CourseController.delete_course(curso_id, user["email"])

@router.post("/{curso_id}/assign-teacher")
async def assign_teacher(curso_id: int, profesor_id: int, authorization: str = Header(None)):
    """Asignar profesor a un curso (solo directores/admin)"""
    user = await get_current_user(authorization)
    return await CourseController.assign_teacher(curso_id, profesor_id, user["email"])
