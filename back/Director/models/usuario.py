from pydantic import BaseModel, EmailStr
from typing import Optional
from datetime import datetime

class Usuario(BaseModel):
    id: Optional[int] = None
    nombre: str
    apellido: str
    email: EmailStr
    rol: str
    created_at: Optional[datetime] = None
    updated_at: Optional[datetime] = None

class UsuarioCreate(BaseModel):
    nombre: str
    apellido: str
    email: EmailStr
    rol: str  # "Estudiante" or "Profesor"

class UsuarioUpdate(BaseModel):
    nombre: Optional[str] = None
    apellido: Optional[str] = None
    email: Optional[EmailStr] = None
    rol: Optional[str] = None

class UsuarioResponse(BaseModel):
    id: int
    nombre: str
    apellido: str
    email: str
    rol: str
    created_at: Optional[str] = None
    updated_at: Optional[str] = None
