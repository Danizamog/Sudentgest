from typing import Dict, List, Optional
from utils import supabase_request, get_tenant_schema
from utils.pdf_generator import PDFReportGenerator
from utils.excel_generator import ExcelReportGenerator
from datetime import datetime


pdf_generator = PDFReportGenerator()
excel_generator = ExcelReportGenerator()


async def get_course_grade_report(tenant_schema: str, curso_id: int, format: str = "pdf") -> bytes:
    """Generate course grade report with all students and statistics"""
    
    # Get course info
    curso_endpoint = f"{tenant_schema}_cursos?id=eq.{curso_id}&select=*"
    cursos = await supabase_request("GET", curso_endpoint)
    if not cursos:
        raise ValueError(f"Course {curso_id} not found")
    course_data = cursos[0]
    
    # Get grade configuration
    config_endpoint = f"{tenant_schema}_configuracion_notas?curso_id=eq.{curso_id}&select=*"
    configs = await supabase_request("GET", config_endpoint)
    if not configs:
        raise ValueError(f"No grade configuration found for course {curso_id}")
    config = configs[0]
    
    # Get weights
    weights_endpoint = f"{tenant_schema}_pesos_parciales?configuracion_id=eq.{config['id']}&select=*&order=numero_parcial.asc"
    weights = await supabase_request("GET", weights_endpoint)
    
    # Get enrollments
    inscripciones_endpoint = f"{tenant_schema}_inscripciones?curso_id=eq.{curso_id}&select=*"
    inscripciones = await supabase_request("GET", inscripciones_endpoint)
    
    students_data = []
    notas_finales = []
    
    for inscripcion in inscripciones:
        # Get student info
        usuario_endpoint = f"{tenant_schema}_usuarios?id=eq.{inscripcion['usuario_id']}&select=*"
        usuarios = await supabase_request("GET", usuario_endpoint)
        if not usuarios:
            continue
        usuario = usuarios[0]
        
        # Get student grades
        notas_endpoint = f"{tenant_schema}_notas?inscripcion_id=eq.{inscripcion['id']}&select=*&order=numero_parcial.asc"
        notas = await supabase_request("GET", notas_endpoint)
        
        notas_dict = {nota['numero_parcial']: nota['nota'] for nota in notas}
        
        # Calculate final grade
        nota_final = 0.0
        notas_parciales = []
        for weight in weights:
            nota = notas_dict.get(weight['numero_parcial'], 0.0)
            notas_parciales.append(nota)
            nota_final += nota * (weight['peso'] / 100.0)
        
        estado = "APROBADO" if nota_final >= config['nota_aprobacion'] else "REPROBADO"
        notas_finales.append(nota_final)
        
        students_data.append({
            'nombre': f"{usuario['nombre']} {usuario.get('apellido', '')}",
            'email': usuario['email'],
            'notas_parciales': notas_parciales,
            'nota_final': round(nota_final, 2),
            'estado': estado
        })
    
    # Calculate statistics
    if notas_finales:
        statistics = {
            'nota_aprobacion': config['nota_aprobacion'],
            'promedio': sum(notas_finales) / len(notas_finales),
            'nota_maxima': max(notas_finales),
            'nota_minima': min(notas_finales),
            'aprobados': sum(1 for n in notas_finales if n >= config['nota_aprobacion']),
            'reprobados': sum(1 for n in notas_finales if n < config['nota_aprobacion']),
        }
        statistics['tasa_aprobacion'] = (statistics['aprobados'] / len(notas_finales)) * 100
    else:
        statistics = {
            'nota_aprobacion': config['nota_aprobacion'],
            'promedio': 0,
            'nota_maxima': 0,
            'nota_minima': 0,
            'aprobados': 0,
            'reprobados': 0,
            'tasa_aprobacion': 0
        }
    
    # Generate report
    if format == "pdf":
        return pdf_generator.create_course_grade_report(course_data, students_data, statistics)
    else:
        return excel_generator.create_course_grade_report(course_data, students_data, statistics)


async def get_student_performance_report(tenant_schema: str, usuario_id: int, 
                                        curso_id: Optional[int] = None, format: str = "pdf") -> bytes:
    """Generate individual student performance report"""
    
    # Get student info
    usuario_endpoint = f"{tenant_schema}_usuarios?id=eq.{usuario_id}&select=*"
    usuarios = await supabase_request("GET", usuario_endpoint)
    if not usuarios:
        raise ValueError(f"Student {usuario_id} not found")
    student_data = usuarios[0]
    
    # Get enrollments
    if curso_id:
        inscripciones_endpoint = f"{tenant_schema}_inscripciones?usuario_id=eq.{usuario_id}&curso_id=eq.{curso_id}&select=*"
    else:
        inscripciones_endpoint = f"{tenant_schema}_inscripciones?usuario_id=eq.{usuario_id}&select=*"
    
    inscripciones = await supabase_request("GET", inscripciones_endpoint)
    
    courses_data = []
    
    for inscripcion in inscripciones:
        # Get course info
        curso_endpoint = f"{tenant_schema}_cursos?id=eq.{inscripcion['curso_id']}&select=*"
        cursos = await supabase_request("GET", curso_endpoint)
        if not cursos:
            continue
        curso = cursos[0]
        
        # Get configuration
        config_endpoint = f"{tenant_schema}_configuracion_notas?curso_id=eq.{inscripcion['curso_id']}&select=*"
        configs = await supabase_request("GET", config_endpoint)
        if not configs:
            continue
        config = configs[0]
        
        # Get weights
        weights_endpoint = f"{tenant_schema}_pesos_parciales?configuracion_id=eq.{config['id']}&select=*&order=numero_parcial.asc"
        weights = await supabase_request("GET", weights_endpoint)
        
        # Get grades
        notas_endpoint = f"{tenant_schema}_notas?inscripcion_id=eq.{inscripcion['id']}&select=*&order=numero_parcial.asc"
        notas = await supabase_request("GET", notas_endpoint)
        notas_dict = {nota['numero_parcial']: nota['nota'] for nota in notas}
        
        # Calculate final grade
        nota_final = 0.0
        parciales = []
        for weight in weights:
            nota = notas_dict.get(weight['numero_parcial'], 0.0)
            parciales.append({
                'numero': weight['numero_parcial'],
                'nombre': weight.get('nombre', f"Parcial {weight['numero_parcial']}"),
                'nota': nota,
                'peso': weight['peso']
            })
            nota_final += nota * (weight['peso'] / 100.0)
        
        estado = "APROBADO" if nota_final >= config['nota_aprobacion'] else "REPROBADO"
        
        courses_data.append({
            'nombre': curso['nombre'],
            'codigo': curso['codigo'],
            'parciales': parciales,
            'nota_final': round(nota_final, 2),
            'estado': estado
        })
    
    # Generate report
    if format == "pdf":
        return pdf_generator.create_student_performance_report(student_data, courses_data)
    else:
        return excel_generator.create_student_performance_report(student_data, courses_data)


async def get_system_overview_report(tenant_schema: str, format: str = "pdf") -> bytes:
    """Generate system overview report for directors"""
    
    # Get total courses
    cursos_endpoint = f"{tenant_schema}_cursos?select=*"
    cursos = await supabase_request("GET", cursos_endpoint)
    total_cursos = len(cursos)
    
    # Get total students (rol = Estudiante)
    estudiantes_endpoint = f"{tenant_schema}_usuarios?rol=eq.Estudiante&select=id"
    estudiantes = await supabase_request("GET", estudiantes_endpoint)
    total_estudiantes = len(estudiantes)
    
    # Get total teachers (rol = Profesor)
    profesores_endpoint = f"{tenant_schema}_usuarios?rol=eq.Profesor&select=id"
    profesores = await supabase_request("GET", profesores_endpoint)
    total_profesores = len(profesores)
    
    # Get all configurations and calculate averages
    configs_endpoint = f"{tenant_schema}_configuracion_notas?select=*"
    configs = await supabase_request("GET", configs_endpoint)
    
    cursos_performance = []
    all_finals = []
    all_passed = 0
    all_total = 0
    
    for curso in cursos:
        config = next((c for c in configs if c['curso_id'] == curso['id']), None)
        if not config:
            continue
        
        # Get enrollments for this course
        inscripciones_endpoint = f"{tenant_schema}_inscripciones?curso_id=eq.{curso['id']}&select=id"
        inscripciones = await supabase_request("GET", inscripciones_endpoint)
        
        if not inscripciones:
            continue
        
        # Get weights
        weights_endpoint = f"{tenant_schema}_pesos_parciales?configuracion_id=eq.{config['id']}&select=*"
        weights = await supabase_request("GET", weights_endpoint)
        
        curso_finals = []
        curso_passed = 0
        
        for inscripcion in inscripciones:
            # Get grades
            notas_endpoint = f"{tenant_schema}_notas?inscripcion_id=eq.{inscripcion['id']}&select=*"
            notas = await supabase_request("GET", notas_endpoint)
            notas_dict = {nota['numero_parcial']: nota['nota'] for nota in notas}
            
            # Calculate final
            nota_final = sum(notas_dict.get(w['numero_parcial'], 0) * (w['peso'] / 100) for w in weights)
            curso_finals.append(nota_final)
            all_finals.append(nota_final)
            
            if nota_final >= config['nota_aprobacion']:
                curso_passed += 1
                all_passed += 1
            all_total += 1
        
        if curso_finals:
            cursos_performance.append({
                'nombre': curso['nombre'],
                'total_estudiantes': len(inscripciones),
                'promedio': sum(curso_finals) / len(curso_finals),
                'tasa_aprobacion': (curso_passed / len(curso_finals)) * 100
            })
    
    overview_data = {
        'total_cursos': total_cursos,
        'total_estudiantes': total_estudiantes,
        'total_profesores': total_profesores,
        'promedio_general': sum(all_finals) / len(all_finals) if all_finals else 0,
        'tasa_aprobacion': (all_passed / all_total * 100) if all_total > 0 else 0,
        'cursos_performance': cursos_performance
    }
    
    # Generate report
    if format == "pdf":
        return pdf_generator.create_system_overview_report(overview_data)
    else:
        return excel_generator.create_system_overview_report(overview_data)
