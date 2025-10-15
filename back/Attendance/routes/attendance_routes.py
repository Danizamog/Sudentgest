from fastapi import APIRouter, Header, Query
import sys
import os

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from models.attendance import AttendanceRecord, Excuse, ExcuseApproval
from controllers.attendance_controller import AttendanceController
from utils.supabase import get_current_user

router = APIRouter(prefix="/api/attendance", tags=["Attendance"])

@router.post("/register")
async def register_attendance(attendance: AttendanceRecord, authorization: str = Header(None)):
    """Registrar asistencia (solo profesores/director)"""
    user = await get_current_user(authorization)
    return await AttendanceController.register_attendance(attendance, user["email"])

@router.post("/excuse")
async def create_excuse(excuse: Excuse, authorization: str = Header(None)):
    """Crear excusa (padres, profesores, personal)"""
    user = await get_current_user(authorization)
    return await AttendanceController.create_excuse(excuse, user["email"])

@router.patch("/excuse/approve")
async def approve_excuse(approval: ExcuseApproval, authorization: str = Header(None)):
    """Aprobar/rechazar excusa (solo director)"""
    user = await get_current_user(authorization)
    return await AttendanceController.approve_excuse(approval, user["email"])

@router.get("/student/{estudiante_id}")
async def get_student_attendance(
    estudiante_id: int,
    curso_id: int = Query(...),
    authorization: str = Header(None)
):
    """Obtener asistencias de un estudiante"""
    user = await get_current_user(authorization)
    return await AttendanceController.get_student_attendance(estudiante_id, curso_id, user["email"])

@router.get("/excuses/pending")
async def get_pending_excuses(authorization: str = Header(None)):
    """Obtener excusas pendientes (solo director)"""
    user = await get_current_user(authorization)
    return await AttendanceController.get_pending_excuses(user["email"])

@router.get("/history/{curso_id}")
async def get_attendance_history(
    curso_id: int,
    fecha_inicio: str = Query(...),
    fecha_fin: str = Query(...),
    authorization: str = Header(None)
):
    """Obtener historial de asistencias de un curso"""
    user = await get_current_user(authorization)
    return await AttendanceController.get_attendance_history(curso_id, fecha_inicio, fecha_fin, user["email"])

@router.patch("/update/{asistencia_id}")
async def update_attendance(
    asistencia_id: int,
    estado: str = Query(...),
    observaciones: str = Query(default=""),
    authorization: str = Header(None)
):
    """Actualizar asistencia existente (profesores/directores)"""
    user = await get_current_user(authorization)
    return await AttendanceController.update_attendance(asistencia_id, estado, observaciones, user["email"])

@router.delete("/delete/{asistencia_id}")
async def delete_attendance(asistencia_id: int, authorization: str = Header(None)):
    """Eliminar una asistencia (solo director)"""
    user = await get_current_user(authorization)
    return await AttendanceController.delete_attendance(asistencia_id, user["email"])

@router.get("/excuses/all")
async def get_all_excuses(
    estado: str = Query(default=None),
    authorization: str = Header(None)
):
    """Obtener todas las excusas (director)"""
    user = await get_current_user(authorization)
    return await AttendanceController.get_all_excuses(user["email"], estado)

@router.get("/excuses/my")
async def get_my_excuses(authorization: str = Header(None)):
    """Obtener mis excusas creadas"""
    user = await get_current_user(authorization)
    return await AttendanceController.get_my_excuses(user["email"])
