from pydantic import BaseModel
from typing import Optional, List
from decimal import Decimal

# ==================== Grade Configuration Models ====================

class GradeConfigCreate(BaseModel):
    curso_id: int
    numero_parciales: int  # 1-8
    nota_aprobacion: int = 60  # Default passing grade

class GradeConfigUpdate(BaseModel):
    numero_parciales: Optional[int] = None
    nota_aprobacion: Optional[int] = None

class GradeConfigResponse(BaseModel):
    id: int
    curso_id: int
    numero_parciales: int
    nota_aprobacion: int
    created_at: Optional[str] = None
    updated_at: Optional[str] = None


# ==================== Grade Weight Models ====================

class GradeWeightCreate(BaseModel):
    numero_parcial: int  # 1-8
    peso: float  # 0-100 (percentage)
    nombre: Optional[str] = None  # e.g., "Primer Parcial"

class GradeWeightUpdate(BaseModel):
    peso: Optional[float] = None
    nombre: Optional[str] = None

class GradeWeightResponse(BaseModel):
    id: int
    configuracion_id: int
    numero_parcial: int
    peso: float
    nombre: Optional[str] = None
    created_at: Optional[str] = None


# ==================== Grade Models ====================

class GradeCreate(BaseModel):
    inscripcion_id: int
    curso_id: int
    usuario_id: int  # student ID
    numero_parcial: int  # 1-8
    nota: float  # 0-100
    observaciones: Optional[str] = None

class GradeUpdate(BaseModel):
    nota: Optional[float] = None
    observaciones: Optional[str] = None

class GradeResponse(BaseModel):
    id: int
    inscripcion_id: int
    curso_id: int
    usuario_id: int
    numero_parcial: int
    nota: float
    observaciones: Optional[str] = None
    created_at: Optional[str] = None
    updated_at: Optional[str] = None
    created_by: Optional[int] = None


# ==================== Bulk/Batch Models ====================

class BulkGradeUpdate(BaseModel):
    inscripcion_id: int
    numero_parcial: int
    nota: float

class ConfigWithWeights(BaseModel):
    curso_id: int
    numero_parciales: int
    nota_aprobacion: int = 60
    pesos: List[GradeWeightCreate]  # Array of weights


# ==================== Student List Models ====================

class StudentGrade(BaseModel):
    numero_parcial: int
    nota: float
    peso: float
    nombre: Optional[str] = None

class StudentListItem(BaseModel):
    inscripcion_id: int
    usuario_id: int
    estudiante_nombre: str
    estudiante_apellido: str
    estudiante_email: str
    parciales: List[StudentGrade]
    nota_final: float
    estado: str  # "APROBADO" / "REPROBADO"

class CourseStudentList(BaseModel):
    curso_id: int
    curso_nombre: str
    curso_codigo: str
    numero_parciales: int
    nota_aprobacion: int
    pesos: List[GradeWeightResponse]
    estudiantes: List[StudentListItem]
