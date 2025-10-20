from pydantic import BaseModel
from typing import Optional, Literal
from datetime import date, datetime

class AttendanceRecord(BaseModel):
    estudiante_id: int
    curso_id: int
    fecha: date
    estado: Literal["presente", "ausente", "tardanza"]
    observaciones: Optional[str] = None

class Excuse(BaseModel):
    estudiante_id: int
    curso_id: int
    fecha_inicio: date
    fecha_fin: date
    motivo: str
    documento_url: Optional[str] = None

class ExcuseApproval(BaseModel):
    excuse_id: int
    estado: Literal["aprobada", "rechazada"]
    comentario_director: Optional[str] = None
