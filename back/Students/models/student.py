from pydantic import BaseModel
from typing import Optional

class Student(BaseModel):
    nombre: str
    apellido: str
    email: str
    rol: str = "estudiante"

class StudentUpdate(BaseModel):
    nombre: Optional[str] = None
    apellido: Optional[str] = None
    email: Optional[str] = None
    telefono: Optional[str] = None
    direccion: Optional[str] = None
    fecha_nacimiento: Optional[str] = None
    rol: Optional[str] = None

class StudentResponse(BaseModel):
    id: int
    nombre: str
    apellido: str
    email: str
    rol: str
    created_at: str
    updated_at: str

class StudentCreate(BaseModel):
    nombre: str
    apellido: str
    email: str
    telefono: Optional[str] = None
    direccion: Optional[str] = None
    fecha_nacimiento: Optional[str] = None
    rol: str = "estudiante"