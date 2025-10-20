import httpx
import os
from typing import List, Optional, Dict
from models.grades import (
    GradeConfigCreate, GradeConfigUpdate, GradeConfigResponse,
    GradeWeightCreate, GradeWeightResponse,
    GradeCreate, GradeUpdate, GradeResponse,
    ConfigWithWeights, StudentListItem, CourseStudentList, StudentGrade
)
from utils.supabase import supabase_request

SUPABASE_URL = os.getenv("SUPABASE_URL")
SUPABASE_SERVICE_ROLE_KEY = os.getenv("SUPABASE_SERVICE_ROLE_KEY")


# ==================== Grade Configuration ====================

async def get_course_config(tenant_schema: str, curso_id: int) -> Optional[GradeConfigResponse]:
    """Get grade configuration for a course"""
    table_name = f"{tenant_schema}_configuracion_notas"
    endpoint = f"{table_name}?curso_id=eq.{curso_id}&select=*"
    
    try:
        data = await supabase_request("GET", endpoint)
        if data and len(data) > 0:
            return GradeConfigResponse(**data[0])
        return None
    except Exception as e:
        print(f"❌ Error getting course config: {e}")
        raise


async def create_course_config(tenant_schema: str, config_data: GradeConfigCreate) -> GradeConfigResponse:
    """Create grade configuration for a course"""
    table_name = f"{tenant_schema}_configuracion_notas"
    endpoint = f"{table_name}"
    
    data = {
        "curso_id": config_data.curso_id,
        "numero_parciales": config_data.numero_parciales,
        "nota_aprobacion": config_data.nota_aprobacion
    }
    
    try:
        result = await supabase_request("POST", endpoint, data)
        if result and len(result) > 0:
            return GradeConfigResponse(**result[0])
        raise ValueError("Failed to create configuration")
    except Exception as e:
        print(f"❌ Error creating course config: {e}")
        raise


async def update_course_config(tenant_schema: str, config_id: int, config_data: GradeConfigUpdate) -> GradeConfigResponse:
    """Update grade configuration"""
    table_name = f"{tenant_schema}_configuracion_notas"
    endpoint = f"{table_name}?id=eq.{config_id}"
    
    update_data = {k: v for k, v in config_data.dict().items() if v is not None}
    
    try:
        result = await supabase_request("PATCH", endpoint, update_data)
        if result and len(result) > 0:
            return GradeConfigResponse(**result[0])
        raise ValueError("Configuration not found")
    except Exception as e:
        print(f"❌ Error updating course config: {e}")
        raise


# ==================== Grade Weights ====================

async def get_course_weights(tenant_schema: str, config_id: int) -> List[GradeWeightResponse]:
    """Get all weights for a course configuration"""
    table_name = f"{tenant_schema}_pesos_parciales"
    endpoint = f"{table_name}?configuracion_id=eq.{config_id}&select=*&order=numero_parcial.asc"
    
    try:
        data = await supabase_request("GET", endpoint)
        return [GradeWeightResponse(**weight) for weight in data]
    except Exception as e:
        print(f"❌ Error getting weights: {e}")
        raise


async def create_weight(tenant_schema: str, config_id: int, weight_data: GradeWeightCreate) -> GradeWeightResponse:
    """Create a weight for a parcial"""
    table_name = f"{tenant_schema}_pesos_parciales"
    endpoint = f"{table_name}"
    
    data = {
        "configuracion_id": config_id,
        "numero_parcial": weight_data.numero_parcial,
        "peso": weight_data.peso,
        "nombre": weight_data.nombre
    }
    
    try:
        result = await supabase_request("POST", endpoint, data)
        if result and len(result) > 0:
            return GradeWeightResponse(**result[0])
        raise ValueError("Failed to create weight")
    except Exception as e:
        print(f"❌ Error creating weight: {e}")
        raise


async def update_weight(tenant_schema: str, weight_id: int, peso: float, nombre: Optional[str] = None) -> GradeWeightResponse:
    """Update a weight"""
    table_name = f"{tenant_schema}_pesos_parciales"
    endpoint = f"{table_name}?id=eq.{weight_id}"
    
    data = {"peso": peso}
    if nombre is not None:
        data["nombre"] = nombre
    
    try:
        result = await supabase_request("PATCH", endpoint, data)
        if result and len(result) > 0:
            return GradeWeightResponse(**result[0])
        raise ValueError("Weight not found")
    except Exception as e:
        print(f"❌ Error updating weight: {e}")
        raise


async def create_config_with_weights(tenant_schema: str, config_data: ConfigWithWeights) -> Dict:
    """Create configuration and weights in one transaction"""
    # Create config
    config = await create_course_config(tenant_schema, GradeConfigCreate(
        curso_id=config_data.curso_id,
        numero_parciales=config_data.numero_parciales,
        nota_aprobacion=config_data.nota_aprobacion
    ))
    
    # Create weights
    weights = []
    for weight_data in config_data.pesos:
        weight = await create_weight(tenant_schema, config.id, weight_data)
        weights.append(weight)
    
    return {
        "config": config,
        "weights": weights
    }


# ==================== Grades CRUD ====================

async def get_student_grades(tenant_schema: str, inscripcion_id: int) -> List[GradeResponse]:
    """Get all grades for a student enrollment"""
    table_name = f"{tenant_schema}_notas"
    endpoint = f"{table_name}?inscripcion_id=eq.{inscripcion_id}&select=*&order=numero_parcial.asc"
    
    try:
        data = await supabase_request("GET", endpoint)
        return [GradeResponse(**grade) for grade in data]
    except Exception as e:
        print(f"❌ Error getting student grades: {e}")
        raise


async def create_or_update_grade(tenant_schema: str, grade_data: GradeCreate, teacher_id: int) -> GradeResponse:
    """Create or update a grade (upsert)"""
    table_name = f"{tenant_schema}_notas"
    
    # Check if grade already exists
    check_endpoint = f"{table_name}?inscripcion_id=eq.{grade_data.inscripcion_id}&numero_parcial=eq.{grade_data.numero_parcial}&select=id"
    
    try:
        existing = await supabase_request("GET", check_endpoint)
        
        if existing and len(existing) > 0:
            # Update existing grade
            grade_id = existing[0]["id"]
            update_endpoint = f"{table_name}?id=eq.{grade_id}"
            update_data = {
                "nota": grade_data.nota,
                "observaciones": grade_data.observaciones
            }
            result = await supabase_request("PATCH", update_endpoint, update_data)
        else:
            # Create new grade
            endpoint = f"{table_name}"
            data = {
                "inscripcion_id": grade_data.inscripcion_id,
                "curso_id": grade_data.curso_id,
                "usuario_id": grade_data.usuario_id,
                "numero_parcial": grade_data.numero_parcial,
                "nota": grade_data.nota,
                "observaciones": grade_data.observaciones,
                "created_by": teacher_id
            }
            result = await supabase_request("POST", endpoint, data)
        
        if result and len(result) > 0:
            return GradeResponse(**result[0])
        raise ValueError("Failed to create/update grade")
    except Exception as e:
        print(f"❌ Error creating/updating grade: {e}")
        raise


async def delete_grade(tenant_schema: str, grade_id: int) -> bool:
    """Delete a grade"""
    table_name = f"{tenant_schema}_notas"
    endpoint = f"{table_name}?id=eq.{grade_id}"
    
    try:
        await supabase_request("DELETE", endpoint)
        return True
    except Exception as e:
        print(f"❌ Error deleting grade: {e}")
        raise


# ==================== Student List with Grades ====================

async def get_course_students_with_grades(tenant_schema: str, curso_id: int) -> CourseStudentList:
    """Get all students enrolled in a course with their grades"""
    
    # Get course info
    curso_endpoint = f"{tenant_schema}_cursos?id=eq.{curso_id}&select=id,nombre,codigo"
    curso_data = await supabase_request("GET", curso_endpoint)
    if not curso_data or len(curso_data) == 0:
        raise ValueError(f"Course {curso_id} not found")
    curso = curso_data[0]
    
    # Get configuration
    config = await get_course_config(tenant_schema, curso_id)
    if not config:
        raise ValueError(f"No grade configuration found for course {curso_id}")
    
    # Get weights
    weights = await get_course_weights(tenant_schema, config.id)
    
    # Get enrollments with student info
    inscripciones_endpoint = f"{tenant_schema}_inscripciones?curso_id=eq.{curso_id}&select=id,usuario_id"
    inscripciones = await supabase_request("GET", inscripciones_endpoint)
    
    estudiantes = []
    for inscripcion in inscripciones:
        # Get student info
        usuario_endpoint = f"{tenant_schema}_usuarios?id=eq.{inscripcion['usuario_id']}&select=id,nombre,apellido,email"
        usuario_data = await supabase_request("GET", usuario_endpoint)
        if not usuario_data or len(usuario_data) == 0:
            continue
        usuario = usuario_data[0]
        
        # Get student grades
        notas_endpoint = f"{tenant_schema}_notas?inscripcion_id=eq.{inscripcion['id']}&select=*&order=numero_parcial.asc"
        notas_data = await supabase_request("GET", notas_endpoint)
        
        # Build grades dictionary
        notas_dict = {nota['numero_parcial']: nota['nota'] for nota in notas_data}
        
        # Calculate parciales with weights
        parciales = []
        nota_final = 0.0
        for weight in weights:
            nota = notas_dict.get(weight.numero_parcial, 0.0)
            parciales.append(StudentGrade(
                numero_parcial=weight.numero_parcial,
                nota=nota,
                peso=weight.peso,
                nombre=weight.nombre
            ))
            nota_final += nota * (weight.peso / 100.0)
        
        # Determine pass/fail status
        estado = "APROBADO" if nota_final >= config.nota_aprobacion else "REPROBADO"
        
        estudiantes.append(StudentListItem(
            inscripcion_id=inscripcion['id'],
            usuario_id=usuario['id'],
            estudiante_nombre=usuario['nombre'],
            estudiante_apellido=usuario['apellido'],
            estudiante_email=usuario['email'],
            parciales=parciales,
            nota_final=round(nota_final, 2),
            estado=estado
        ))
    
    return CourseStudentList(
        curso_id=curso['id'],
        curso_nombre=curso['nombre'],
        curso_codigo=curso['codigo'],
        numero_parciales=config.numero_parciales,
        nota_aprobacion=config.nota_aprobacion,
        pesos=weights,
        estudiantes=estudiantes
    )


# ==================== Student's Own Grades ====================

async def get_student_own_grades(tenant_schema: str, usuario_id: int) -> List[Dict]:
    """Get all grades for a student across all their enrolled courses"""
    
    # Get all enrollments for the student
    inscripciones_endpoint = f"{tenant_schema}_inscripciones?usuario_id=eq.{usuario_id}&select=id,curso_id"
    inscripciones = await supabase_request("GET", inscripciones_endpoint)
    
    result = []
    for inscripcion in inscripciones:
        # Get course info
        curso_endpoint = f"{tenant_schema}_cursos?id=eq.{inscripcion['curso_id']}&select=id,nombre,codigo"
        curso_data = await supabase_request("GET", curso_endpoint)
        if not curso_data:
            continue
        curso = curso_data[0]
        
        # Get configuration
        config = await get_course_config(tenant_schema, inscripcion['curso_id'])
        if not config:
            continue
        
        # Get weights
        weights = await get_course_weights(tenant_schema, config.id)
        
        # Get grades
        notas_endpoint = f"{tenant_schema}_notas?inscripcion_id=eq.{inscripcion['id']}&select=*&order=numero_parcial.asc"
        notas_data = await supabase_request("GET", notas_endpoint)
        notas_dict = {nota['numero_parcial']: nota['nota'] for nota in notas_data}
        
        # Calculate final grade
        parciales = []
        nota_final = 0.0
        for weight in weights:
            nota = notas_dict.get(weight.numero_parcial, 0.0)
            parciales.append({
                "numero_parcial": weight.numero_parcial,
                "nombre": weight.nombre,
                "nota": nota,
                "peso": weight.peso
            })
            nota_final += nota * (weight.peso / 100.0)
        
        estado = "APROBADO" if nota_final >= config.nota_aprobacion else "REPROBADO"
        
        result.append({
            "curso_id": curso['id'],
            "curso_nombre": curso['nombre'],
            "curso_codigo": curso['codigo'],
            "inscripcion_id": inscripcion['id'],
            "numero_parciales": config.numero_parciales,
            "nota_aprobacion": config.nota_aprobacion,
            "parciales": parciales,
            "nota_final": round(nota_final, 2),
            "estado": estado
        })
    
    return result
