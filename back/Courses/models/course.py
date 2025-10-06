from pydantic import BaseModel
from typing import Optional

class Course(BaseModel):
    nombre: str
    codigo: str
    descripcion: Optional[str] = None
    creditos: int = 3
    horario: Optional[str] = None

class CourseEnrollment(BaseModel):
    curso_id: int
    usuario_id: int

class CourseResponse(BaseModel):
    id: int
    nombre: str
    codigo: str
    descripcion: Optional[str] = None
    creditos: int
    horario: Optional[str] = None
    created_at: str
