from pydantic import BaseModel
from typing import Optional, List, Dict

class Director(BaseModel):
    nombre: str
    apellido: str
    email: str
    rol: str = "director"

class DirectorUpdate(BaseModel):
    nombre: Optional[str] = None
    apellido: Optional[str] = None
    email: Optional[str] = None
    rol: Optional[str] = None

class DirectorResponse(BaseModel):
    id: int
    nombre: str
    apellido: str
    email: str
    rol: str
    created_at: str
    updated_at: str

class DirectorCreate(BaseModel):
    nombre: str
    apellido: str
    email: str
    rol: str = "director"

class InstitutionStats(BaseModel):
    total_students: int
    total_teachers: int
    total_courses: int
    total_enrollments: int

class InstitutionOverview(BaseModel):
    institution_name: str
    domain: str
    stats: InstitutionStats
    recent_enrollments: List[Dict]
    recent_users: List[Dict]