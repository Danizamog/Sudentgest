from fastapi import APIRouter, HTTPException, Header, status, Response
from typing import Optional
from models import (
    CourseGradeReportRequest,
    StudentPerformanceReportRequest,
    SystemOverviewRequest
)
from controllers import (
    get_course_grade_report,
    get_student_performance_report,
    get_system_overview_report
)
from utils import get_tenant_schema

router = APIRouter(prefix="/api/reports", tags=["Reports"])


def extract_tenant_from_header(x_user_email: Optional[str]) -> str:
    """Extract and validate tenant from user email header"""
    if not x_user_email:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="X-User-Email header is required"
        )
    
    try:
        return get_tenant_schema(x_user_email)
    except ValueError as e:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=str(e)
        )


@router.post("/course-grades/{curso_id}")
async def generate_course_grade_report(
    curso_id: int,
    request: CourseGradeReportRequest,
    x_user_email: Optional[str] = Header(None)
):
    """Generate course grade report (PDF or Excel)"""
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    try:
        report_data = await get_course_grade_report(
            tenant_schema, 
            curso_id, 
            format=request.format
        )
        
        content_type = "application/pdf" if request.format == "pdf" else \
                      "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        
        ext = "pdf" if request.format == "pdf" else "xlsx"
        filename = f"curso_{curso_id}_calificaciones.{ext}"
        
        return Response(
            content=report_data,
            media_type=content_type,
            headers={
                "Content-Disposition": f"attachment; filename={filename}"
            }
        )
    except ValueError as e:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error generating report: {str(e)}"
        )


@router.post("/student-performance/{usuario_id}")
async def generate_student_performance_report(
    usuario_id: int,
    request: StudentPerformanceReportRequest,
    x_user_email: Optional[str] = Header(None)
):
    """Generate student performance report (PDF or Excel)"""
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    try:
        report_data = await get_student_performance_report(
            tenant_schema,
            usuario_id,
            curso_id=request.curso_id,
            format=request.format
        )
        
        content_type = "application/pdf" if request.format == "pdf" else \
                      "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        
        ext = "pdf" if request.format == "pdf" else "xlsx"
        filename = f"estudiante_{usuario_id}_desempeno.{ext}"
        
        return Response(
            content=report_data,
            media_type=content_type,
            headers={
                "Content-Disposition": f"attachment; filename={filename}"
            }
        )
    except ValueError as e:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error generating report: {str(e)}"
        )


@router.post("/system-overview")
async def generate_system_overview_report(
    request: SystemOverviewRequest,
    x_user_email: Optional[str] = Header(None)
):
    """Generate system overview report for directors (PDF or Excel)"""
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    try:
        report_data = await get_system_overview_report(
            tenant_schema,
            format=request.format
        )
        
        content_type = "application/pdf" if request.format == "pdf" else \
                      "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        
        ext = "pdf" if request.format == "pdf" else "xlsx"
        filename = f"sistema_resumen.{ext}"
        
        return Response(
            content=report_data,
            media_type=content_type,
            headers={
                "Content-Disposition": f"attachment; filename={filename}"
            }
        )
    except ValueError as e:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error generating report: {str(e)}"
        )


@router.get("/teacher-dashboard")
async def get_teacher_dashboard_data(
    x_user_email: Optional[str] = Header(None)
):
    """Get aggregated dashboard data for teacher"""
    from controllers.dashboard import get_teacher_dashboard_data as get_dashboard
    
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    try:
        # Get teacher email to filter their courses
        dashboard_data = await get_dashboard(tenant_schema, x_user_email)
        return dashboard_data
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error fetching dashboard data: {str(e)}"
        )


@router.get("/director-dashboard")
async def get_director_dashboard_data(
    x_user_email: Optional[str] = Header(None)
):
    """Get aggregated dashboard data for director"""
    from controllers.dashboard import get_director_dashboard_data as get_dashboard
    
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    try:
        dashboard_data = await get_dashboard(tenant_schema)
        return dashboard_data
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error fetching dashboard data: {str(e)}"
        )
