from pydantic import BaseModel
from typing import Optional, List
from datetime import date


class ReportRequest(BaseModel):
    """Base report request"""
    format: str = "pdf"  # "pdf" or "excel"
    

class CourseGradeReportRequest(ReportRequest):
    """Request for course grade report"""
    include_statistics: bool = True


class StudentPerformanceReportRequest(ReportRequest):
    """Request for individual student performance report"""
    curso_id: Optional[int] = None  # If None, all courses


class CourseStatisticsRequest(ReportRequest):
    """Request for course statistics report"""
    curso_id: int


class SystemOverviewRequest(ReportRequest):
    """Request for system-wide overview (Director only)"""
    start_date: Optional[str] = None
    end_date: Optional[str] = None


class TeacherPerformanceRequest(ReportRequest):
    """Request for teacher performance report (Director only)"""
    profesor_id: Optional[int] = None  # If None, all teachers


class AttendanceReportRequest(ReportRequest):
    """Request for attendance report"""
    curso_id: Optional[int] = None
    start_date: Optional[str] = None
    end_date: Optional[str] = None
    usuario_id: Optional[int] = None  # For student-specific report


# Response Models
class ReportResponse(BaseModel):
    """Response containing report data"""
    filename: str
    content_type: str
    data: bytes
    
    class Config:
        arbitrary_types_allowed = True
