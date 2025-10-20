"""Dashboard data aggregation controllers"""
from typing import Dict, List
from utils import supabase_request


async def get_teacher_dashboard_data(tenant_schema: str, teacher_email: str) -> Dict:
    """Get aggregated dashboard data for a teacher"""
    
    # Get teacher's user ID
    teacher_endpoint = f"{tenant_schema}_usuarios?email=eq.{teacher_email}&select=id,nombre,apellido"
    teachers = await supabase_request("GET", teacher_endpoint)
    if not teachers:
        raise ValueError("Teacher not found")
    teacher = teachers[0]
    teacher_id = teacher['id']
    
    # Get teacher's courses
    cursos_endpoint = f"{tenant_schema}_cursos?profesor_id=eq.{teacher_id}&select=*"
    cursos = await supabase_request("GET", cursos_endpoint)
    
    total_estudiantes = 0
    total_aprobados = 0
    total_reprobados = 0
    all_promedios = []
    cursos_detalle = []
    all_students = []
    
    for curso in cursos:
        # Get course configuration
        config_endpoint = f"{tenant_schema}_configuracion_notas?curso_id=eq.{curso['id']}&select=*"
        configs = await supabase_request("GET", config_endpoint)
        if not configs:
            continue
        config = configs[0]
        
        # Get weights
        weights_endpoint = f"{tenant_schema}_pesos_parciales?configuracion_id=eq.{config['id']}&select=*&order=numero_parcial.asc"
        weights = await supabase_request("GET", weights_endpoint)
        
        # Get enrollments
        inscripciones_endpoint = f"{tenant_schema}_inscripciones?curso_id=eq.{curso['id']}&select=*"
        inscripciones = await supabase_request("GET", inscripciones_endpoint)
        
        curso_aprobados = 0
        curso_reprobados = 0
        curso_promedios = []
        
        for inscripcion in inscripciones:
            # Get student info
            usuario_endpoint = f"{tenant_schema}_usuarios?id=eq.{inscripcion['usuario_id']}&select=*"
            usuarios = await supabase_request("GET", usuario_endpoint)
            if not usuarios:
                continue
            usuario = usuarios[0]
            
            # Get grades
            notas_endpoint = f"{tenant_schema}_notas?inscripcion_id=eq.{inscripcion['id']}&select=*"
            notas = await supabase_request("GET", notas_endpoint)
            notas_dict = {nota['numero_parcial']: nota['nota'] for nota in notas}
            
            # Calculate final grade
            nota_final = 0.0
            for weight in weights:
                nota = notas_dict.get(weight['numero_parcial'], 0.0)
                nota_final += nota * (weight['peso'] / 100.0)
            
            curso_promedios.append(nota_final)
            all_promedios.append(nota_final)
            
            if nota_final >= config['nota_aprobacion']:
                curso_aprobados += 1
            else:
                curso_reprobados += 1
            
            # Track students for top/risk lists
            all_students.append({
                'id': usuario['id'],
                'nombre': f"{usuario['nombre']} {usuario.get('apellido', '')}",
                'email': usuario['email'],
                'promedio': nota_final,
                'curso': curso['nombre']
            })
        
        total_estudiantes += len(inscripciones)
        total_aprobados += curso_aprobados
        total_reprobados += curso_reprobados
        
        curso_promedio = sum(curso_promedios) / len(curso_promedios) if curso_promedios else 0
        curso_tasa = (curso_aprobados / len(inscripciones) * 100) if inscripciones else 0
        
        cursos_detalle.append({
            'id': curso['id'],
            'nombre': curso['nombre'],
            'codigo': curso['codigo'],
            'total_estudiantes': len(inscripciones),
            'promedio': curso_promedio,
            'aprobados': curso_aprobados,
            'reprobados': curso_reprobados,
            'tasa_aprobacion': curso_tasa
        })
    
    # Calculate overall statistics
    promedio_general = sum(all_promedios) / len(all_promedios) if all_promedios else 0
    tasa_aprobacion = (total_aprobados / total_estudiantes * 100) if total_estudiantes > 0 else 0
    
    # Get top students (top 5)
    top_estudiantes = sorted(all_students, key=lambda x: x['promedio'], reverse=True)[:5]
    
    # Get at-risk students (bottom 5 with promedio < 60)
    estudiantes_riesgo = sorted(
        [s for s in all_students if s['promedio'] < 60],
        key=lambda x: x['promedio']
    )[:5]
    
    return {
        'total_cursos': len(cursos),
        'total_estudiantes': total_estudiantes,
        'promedio_general': promedio_general,
        'tasa_aprobacion': tasa_aprobacion,
        'cursos_detalle': cursos_detalle,
        'top_estudiantes': top_estudiantes,
        'estudiantes_riesgo': estudiantes_riesgo
    }


async def get_director_dashboard_data(tenant_schema: str) -> Dict:
    """Get aggregated dashboard data for director (system-wide)"""
    
    # Get all courses
    cursos_endpoint = f"{tenant_schema}_cursos?select=*"
    cursos = await supabase_request("GET", cursos_endpoint)
    
    # Get all users count by role
    usuarios_endpoint = f"{tenant_schema}_usuarios?select=id,rol"
    usuarios = await supabase_request("GET", usuarios_endpoint)
    
    total_estudiantes = sum(1 for u in usuarios if u.get('rol') == 'Estudiante')
    total_profesores = sum(1 for u in usuarios if u.get('rol') == 'Profesor')
    
    total_aprobados = 0
    total_reprobados = 0
    all_promedios = []
    cursos_detalle = []
    cursos_riesgo = []
    mejores_cursos = []
    
    for curso in cursos:
        # Get professor name
        profesor_endpoint = f"{tenant_schema}_usuarios?id=eq.{curso['profesor_id']}&select=nombre,apellido"
        profesores = await supabase_request("GET", profesor_endpoint)
        profesor_nombre = f"{profesores[0]['nombre']} {profesores[0].get('apellido', '')}" if profesores else "N/A"
        
        # Get configuration
        config_endpoint = f"{tenant_schema}_configuracion_notas?curso_id=eq.{curso['id']}&select=*"
        configs = await supabase_request("GET", config_endpoint)
        if not configs:
            continue
        config = configs[0]
        
        # Get weights
        weights_endpoint = f"{tenant_schema}_pesos_parciales?configuracion_id=eq.{config['id']}&select=*"
        weights = await supabase_request("GET", weights_endpoint)
        
        # Get enrollments
        inscripciones_endpoint = f"{tenant_schema}_inscripciones?curso_id=eq.{curso['id']}&select=*"
        inscripciones = await supabase_request("GET", inscripciones_endpoint)
        
        if not inscripciones:
            continue
        
        curso_aprobados = 0
        curso_reprobados = 0
        curso_promedios = []
        
        for inscripcion in inscripciones:
            # Get grades
            notas_endpoint = f"{tenant_schema}_notas?inscripcion_id=eq.{inscripcion['id']}&select=*"
            notas = await supabase_request("GET", notas_endpoint)
            notas_dict = {nota['numero_parcial']: nota['nota'] for nota in notas}
            
            # Calculate final grade
            nota_final = 0.0
            for weight in weights:
                nota = notas_dict.get(weight['numero_parcial'], 0.0)
                nota_final += nota * (weight['peso'] / 100.0)
            
            curso_promedios.append(nota_final)
            all_promedios.append(nota_final)
            
            if nota_final >= config['nota_aprobacion']:
                curso_aprobados += 1
            else:
                curso_reprobados += 1
        
        total_aprobados += curso_aprobados
        total_reprobados += curso_reprobados
        
        curso_promedio = sum(curso_promedios) / len(curso_promedios) if curso_promedios else 0
        curso_tasa = (curso_aprobados / len(inscripciones) * 100) if inscripciones else 0
        
        curso_data = {
            'id': curso['id'],
            'nombre': curso['nombre'],
            'codigo': curso['codigo'],
            'profesor': profesor_nombre,
            'total_estudiantes': len(inscripciones),
            'promedio': curso_promedio,
            'aprobados': curso_aprobados,
            'reprobados': curso_reprobados,
            'tasa_aprobacion': curso_tasa
        }
        
        cursos_detalle.append(curso_data)
        
        # Flag at-risk courses (< 60% approval rate)
        if curso_tasa < 60:
            cursos_riesgo.append(curso_data)
        
        # Track best courses (>= 85% approval rate)
        if curso_tasa >= 85:
            mejores_cursos.append(curso_data)
    
    # Calculate system statistics
    promedio_sistema = sum(all_promedios) / len(all_promedios) if all_promedios else 0
    tasa_aprobacion_general = (total_aprobados / (total_aprobados + total_reprobados) * 100) if (total_aprobados + total_reprobados) > 0 else 0
    
    # Sort best courses by approval rate
    mejores_cursos = sorted(mejores_cursos, key=lambda x: x['tasa_aprobacion'], reverse=True)[:5]
    
    # Grade distribution (simplified - would need individual student grades for accuracy)
    distribucion_notas = [0, 0, 0, 0, 0, 0]  # 0-50, 51-60, 61-70, 71-80, 81-90, 91-100
    for promedio in all_promedios:
        if promedio <= 50:
            distribucion_notas[0] += 1
        elif promedio <= 60:
            distribucion_notas[1] += 1
        elif promedio <= 70:
            distribucion_notas[2] += 1
        elif promedio <= 80:
            distribucion_notas[3] += 1
        elif promedio <= 90:
            distribucion_notas[4] += 1
        else:
            distribucion_notas[5] += 1
    
    return {
        'total_cursos': len(cursos),
        'cursos_activos': len(cursos),  # All courses are considered active
        'total_estudiantes': total_estudiantes,
        'total_profesores': total_profesores,
        'promedio_sistema': promedio_sistema,
        'tasa_aprobacion_general': tasa_aprobacion_general,
        'total_aprobados': total_aprobados,
        'total_reprobados': total_reprobados,
        'cursos_detalle': cursos_detalle,
        'cursos_riesgo': cursos_riesgo,
        'mejores_cursos': mejores_cursos,
        'distribucion_notas': distribucion_notas
    }
