from fastapi import APIRouter, Header, UploadFile, File, HTTPException
import sys, os

# Ajuste de path para imports relativos (igual que en otros routers)
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from controllers.grades_controller import GradeController
from utils.supabase import get_current_user

router = APIRouter(prefix="/api/grades", tags=["Grades"])

@router.get("/")
async def get_all_grades(authorization: str = Header(None)):
    """
    Listar todas las notas del tenant del usuario autenticado.
    """
    user = await get_current_user(authorization)
    try:
        data = await GradeController.list_all_grades(user["email"])
        return data
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@router.get("/course/{course_id}")
async def get_grades_by_course(course_id: int, authorization: str = Header(None)):
    user = await get_current_user(authorization)
    try:
        data = await GradeController.list_grades_by_course(user["email"], course_id)
        return data
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@router.post("/")
async def create_grades(grades: list, authorization: str = Header(None)):
    """
    Espera un JSON array con objetos: { student_id, course_id, grade, subject? }
    """
    user = await get_current_user(authorization)
    try:
        res = await GradeController.create_grades_bulk(user["email"], grades)
        return res
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@router.post("/upload")
async def upload_grades_file(file: UploadFile = File(...), authorization: str = Header(None)):
    """
    Subir un archivo CSV o XLSX con notas. El archivo debe contener columnas:
    student_id, course_id, grade (subject opcional).
    """
    user = await get_current_user(authorization)
    if not file:
        raise HTTPException(status_code=400, detail="Archivo no proporcionado")
    try:
        res = await GradeController.upload_file_and_create(file, user["email"])
        return res
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@router.put("/{grade_id}")
async def update_grade(grade_id: int, payload: dict, authorization: str = Header(None)):
    user = await get_current_user(authorization)
    try:
        res = await GradeController.update_grade(user["email"], grade_id, payload)
        return res
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@router.delete("/{grade_id}")
async def delete_grade(grade_id: int, authorization: str = Header(None)):
    user = await get_current_user(authorization)
    try:
        res = await GradeController.delete_grade(user["email"], grade_id)
        return res
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
