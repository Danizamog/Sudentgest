from pydantic import BaseModel, EmailStr, Field
from typing import Optional
from datetime import datetime

from pydantic import BaseModel, Field, EmailStr
from typing import Optional
from datetime import datetime

from pydantic import BaseModel
from typing import Optional
from datetime import datetime

class ContactMessage(BaseModel):
    """Modelo para mensaje de contacto - Completamente permisivo"""
    nombre: str
    email: str 
    mensaje: str
    asunto: Optional[str] = None
    telefono: Optional[str] = None
    
    class Config:
        str_strip_whitespace = True  # Auto-strip whitespace
        json_schema_extra = {
            "example": {
                "nombre": "Juan Pérez",
                "email": "juan.perez@ucb.edu.bo",
                "mensaje": "Necesito información sobre el sistema de gestión de estudiantes.",
                "asunto": "Consulta sobre StudentGest",
                "telefono": "+591 70123456"
            }
        }

class ContactMessageResponse(BaseModel):
    """Modelo de respuesta para mensaje de contacto"""
    id: int
    nombre: str
    email: str
    mensaje: str
    asunto: Optional[str]
    telefono: Optional[str]
    tenant: str
    status: str
    created_at: datetime
    updated_at: Optional[datetime]

class ContactMessageCreate(BaseModel):
    """Modelo para crear mensaje de contacto con datos adicionales"""
    nombre: str
    email: str
    mensaje: str
    asunto: Optional[str] = None
    telefono: Optional[str] = None
    user_agent: Optional[str] = None
    ip_address: Optional[str] = None

class ContactMessageUpdate(BaseModel):
    """Modelo para actualizar mensaje de contacto"""
    status: Optional[str] = None
    respuesta: Optional[str] = None
    atendido_por: Optional[str] = None

class ContactStats(BaseModel):
    """Estadísticas de contactos por tenant"""
    total_mensajes: int
    mensajes_pendientes: int
    mensajes_respondidos: int
    tenant: str
    ultimo_mensaje: Optional[datetime]