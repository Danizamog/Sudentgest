from pydantic import BaseModel
from typing import Optional

class GradeBase(BaseModel):
    student_id: int
    course_id: int
    grade: float

class GradeCreate(GradeBase):
    pass

class GradeUpdate(BaseModel):
    grade: Optional[float] = None

class GradeResponse(GradeBase):
    id: int
    class Config:
        orm_mode = True
