from fastapi import APIRouter, HTTPException, Header, status
from typing import Optional, List
from models.grades import (
    GradeConfigCreate, GradeConfigUpdate, GradeConfigResponse,
    GradeWeightCreate, GradeWeightResponse,
    GradeCreate, GradeUpdate, GradeResponse,
    ConfigWithWeights, BulkGradeUpdate, CourseStudentList
)
from controllers.grades_controller import (
    get_course_config, create_course_config, update_course_config,
    get_course_weights, create_weight, update_weight, create_config_with_weights,
    get_student_grades, create_or_update_grade, delete_grade,
    get_course_students_with_grades, get_student_own_grades
)
from utils.supabase import get_tenant_schema

router = APIRouter(prefix="/api/grades", tags=["Grades"])


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


# ==================== Grade Configuration Endpoints ====================

@router.get("/config/{curso_id}", response_model=GradeConfigResponse)
async def get_config(curso_id: int, x_user_email: Optional[str] = Header(None)):
    """Get grade configuration for a course"""
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    try:
        config = await get_course_config(tenant_schema, curso_id)
        if not config:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail=f"No configuration found for course {curso_id}"
            )
        return config
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error retrieving configuration: {str(e)}"
        )


@router.post("/config", response_model=GradeConfigResponse, status_code=status.HTTP_201_CREATED)
async def create_config(config_data: GradeConfigCreate, x_user_email: Optional[str] = Header(None)):
    """Create grade configuration for a course (Teachers only)"""
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    try:
        config = await create_course_config(tenant_schema, config_data)
        return config
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error creating configuration: {str(e)}"
        )


@router.put("/config/{config_id}", response_model=GradeConfigResponse)
async def update_config(
    config_id: int, 
    config_data: GradeConfigUpdate, 
    x_user_email: Optional[str] = Header(None)
):
    """Update grade configuration (Teachers only)"""
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    try:
        config = await update_course_config(tenant_schema, config_id, config_data)
        return config
    except ValueError as e:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=str(e)
        )
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error updating configuration: {str(e)}"
        )


# ==================== Grade Weights Endpoints ====================

@router.get("/weights/{config_id}", response_model=List[GradeWeightResponse])
async def get_weights(config_id: int, x_user_email: Optional[str] = Header(None)):
    """Get all weights for a grade configuration"""
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    try:
        weights = await get_course_weights(tenant_schema, config_id)
        return weights
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error retrieving weights: {str(e)}"
        )


@router.post("/weights/{config_id}", response_model=GradeWeightResponse, status_code=status.HTTP_201_CREATED)
async def add_weight(
    config_id: int, 
    weight_data: GradeWeightCreate, 
    x_user_email: Optional[str] = Header(None)
):
    """Add a weight for a parcial (Teachers only)"""
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    try:
        weight = await create_weight(tenant_schema, config_id, weight_data)
        return weight
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error creating weight: {str(e)}"
        )


@router.put("/weights/{weight_id}")
async def update_weight_endpoint(
    weight_id: int,
    peso: float,
    nombre: Optional[str] = None,
    x_user_email: Optional[str] = Header(None)
):
    """Update a weight (Teachers only)"""
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    try:
        weight = await update_weight(tenant_schema, weight_id, peso, nombre)
        return weight
    except ValueError as e:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=str(e)
        )
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error updating weight: {str(e)}"
        )


# ==================== Complete Config (Config + Weights) ====================

@router.post("/config-complete", status_code=status.HTTP_201_CREATED)
async def create_complete_config(
    config_data: ConfigWithWeights, 
    x_user_email: Optional[str] = Header(None)
):
    """Create configuration with weights in one call (Teachers only)"""
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    # Validate weights sum to 100
    total_peso = sum(w.peso for w in config_data.pesos)
    if abs(total_peso - 100.0) > 0.01:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"Weights must sum to 100. Current sum: {total_peso}"
        )
    
    # Validate number of weights matches numero_parciales
    if len(config_data.pesos) != config_data.numero_parciales:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"Number of weights ({len(config_data.pesos)}) must match numero_parciales ({config_data.numero_parciales})"
        )
    
    try:
        result = await create_config_with_weights(tenant_schema, config_data)
        return result
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error creating configuration: {str(e)}"
        )


# ==================== Grades Endpoints ====================

@router.get("/student/{inscripcion_id}", response_model=List[GradeResponse])
async def get_grades(inscripcion_id: int, x_user_email: Optional[str] = Header(None)):
    """Get all grades for a student enrollment"""
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    try:
        grades = await get_student_grades(tenant_schema, inscripcion_id)
        return grades
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error retrieving grades: {str(e)}"
        )


@router.post("/grade", response_model=GradeResponse, status_code=status.HTTP_201_CREATED)
async def register_grade(
    grade_data: GradeCreate, 
    x_user_email: Optional[str] = Header(None)
):
    """Register or update a grade (Teachers only)"""
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    # Get teacher ID from email (simplified - in production, extract from JWT)
    # For now, we'll use a placeholder
    teacher_id = 1  # TODO: Extract from authenticated user context
    
    try:
        grade = await create_or_update_grade(tenant_schema, grade_data, teacher_id)
        return grade
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error registering grade: {str(e)}"
        )


@router.post("/grades/bulk", status_code=status.HTTP_200_OK)
async def register_grades_bulk(
    grades: List[BulkGradeUpdate],
    curso_id: int,
    x_user_email: Optional[str] = Header(None)
):
    """Register multiple grades at once (Teachers only)"""
    tenant_schema = extract_tenant_from_header(x_user_email)
    teacher_id = 1  # TODO: Extract from authenticated user
    
    results = []
    errors = []
    
    for grade_data in grades:
        try:
            # Get usuario_id from inscripcion_id
            inscripcion_endpoint = f"{tenant_schema}_inscripciones?id=eq.{grade_data.inscripcion_id}&select=usuario_id"
            # Simplified - would need to import supabase_request
            
            grade = await create_or_update_grade(
                tenant_schema,
                GradeCreate(
                    inscripcion_id=grade_data.inscripcion_id,
                    curso_id=curso_id,
                    usuario_id=0,  # Would be fetched
                    numero_parcial=grade_data.numero_parcial,
                    nota=grade_data.nota
                ),
                teacher_id
            )
            results.append(grade)
        except Exception as e:
            errors.append({
                "inscripcion_id": grade_data.inscripcion_id,
                "error": str(e)
            })
    
    return {
        "success": len(results),
        "errors": len(errors),
        "results": results,
        "error_details": errors
    }


@router.delete("/grade/{grade_id}", status_code=status.HTTP_204_NO_CONTENT)
async def remove_grade(grade_id: int, x_user_email: Optional[str] = Header(None)):
    """Delete a grade (Teachers only)"""
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    try:
        await delete_grade(tenant_schema, grade_id)
        return None
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error deleting grade: {str(e)}"
        )


# ==================== Student List with Grades ====================

@router.get("/course/{curso_id}/students", response_model=CourseStudentList)
async def get_course_students(curso_id: int, x_user_email: Optional[str] = Header(None)):
    """Get all students in a course with their grades (Teachers only)"""
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    try:
        student_list = await get_course_students_with_grades(tenant_schema, curso_id)
        return student_list
    except ValueError as e:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=str(e)
        )
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error retrieving student list: {str(e)}"
        )


# ==================== Student's Own Grades ====================

@router.get("/my-grades")
async def get_my_grades(x_user_email: Optional[str] = Header(None)):
    """Get all grades for the authenticated student"""
    tenant_schema = extract_tenant_from_header(x_user_email)
    
    # TODO: Get user ID from authenticated context
    # For now, would need to query user by email
    usuario_id = 1  # Placeholder
    
    try:
        grades = await get_student_own_grades(tenant_schema, usuario_id)
        return grades
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error retrieving grades: {str(e)}"
        )
