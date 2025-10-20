from reportlab.lib import colors
from reportlab.lib.pagesizes import letter, A4
from reportlab.platypus import SimpleDocTemplate, Table, TableStyle, Paragraph, Spacer, PageBreak
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import inch
from reportlab.lib.enums import TA_CENTER, TA_LEFT, TA_RIGHT
from io import BytesIO
from datetime import datetime
from typing import List, Dict, Any


class PDFReportGenerator:
    """Generate PDF reports using ReportLab"""
    
    def __init__(self):
        self.styles = getSampleStyleSheet()
        self.title_style = ParagraphStyle(
            'CustomTitle',
            parent=self.styles['Heading1'],
            fontSize=24,
            textColor=colors.HexColor('#2C3E50'),
            spaceAfter=30,
            alignment=TA_CENTER
        )
        self.heading_style = ParagraphStyle(
            'CustomHeading',
            parent=self.styles['Heading2'],
            fontSize=16,
            textColor=colors.HexColor('#34495E'),
            spaceAfter=12
        )
        self.normal_style = self.styles['Normal']
    
    def create_course_grade_report(self, course_data: Dict, students_data: List[Dict], 
                                   statistics: Dict) -> bytes:
        """Generate course grade report PDF"""
        buffer = BytesIO()
        doc = SimpleDocTemplate(buffer, pagesize=letter, 
                               rightMargin=72, leftMargin=72,
                               topMargin=72, bottomMargin=18)
        
        story = []
        
        # Title
        title = Paragraph(f"Reporte de Calificaciones<br/>{course_data['nombre']}", self.title_style)
        story.append(title)
        story.append(Spacer(1, 12))
        
        # Course Info
        info = [
            [f"Código: {course_data['codigo']}", f"Fecha: {datetime.now().strftime('%d/%m/%Y')}"],
            [f"Total Estudiantes: {len(students_data)}", 
             f"Nota Aprobación: {statistics.get('nota_aprobacion', 60)}"]
        ]
        info_table = Table(info, colWidths=[3*inch, 3*inch])
        info_table.setStyle(TableStyle([
            ('FONTNAME', (0, 0), (-1, -1), 'Helvetica-Bold'),
            ('FONTSIZE', (0, 0), (-1, -1), 10),
            ('TEXTCOLOR', (0, 0), (-1, -1), colors.HexColor('#2C3E50')),
            ('ALIGN', (0, 0), (-1, -1), 'LEFT'),
            ('VALIGN', (0, 0), (-1, -1), 'TOP'),
        ]))
        story.append(info_table)
        story.append(Spacer(1, 20))
        
        # Statistics Section
        if statistics:
            stats_title = Paragraph("Estadísticas del Curso", self.heading_style)
            story.append(stats_title)
            
            stats_data = [
                ['Métrica', 'Valor'],
                ['Promedio General', f"{statistics.get('promedio', 0):.2f}"],
                ['Nota Más Alta', f"{statistics.get('nota_maxima', 0):.2f}"],
                ['Nota Más Baja', f"{statistics.get('nota_minima', 0):.2f}"],
                ['Estudiantes Aprobados', f"{statistics.get('aprobados', 0)} ({statistics.get('tasa_aprobacion', 0):.1f}%)"],
                ['Estudiantes Reprobados', f"{statistics.get('reprobados', 0)}"],
            ]
            
            stats_table = Table(stats_data, colWidths=[3*inch, 2*inch])
            stats_table.setStyle(TableStyle([
                ('BACKGROUND', (0, 0), (-1, 0), colors.HexColor('#3498DB')),
                ('TEXTCOLOR', (0, 0), (-1, 0), colors.whitesmoke),
                ('ALIGN', (0, 0), (-1, -1), 'CENTER'),
                ('FONTNAME', (0, 0), (-1, 0), 'Helvetica-Bold'),
                ('FONTSIZE', (0, 0), (-1, 0), 12),
                ('BOTTOMPADDING', (0, 0), (-1, 0), 12),
                ('BACKGROUND', (0, 1), (-1, -1), colors.beige),
                ('GRID', (0, 0), (-1, -1), 1, colors.black),
                ('FONTNAME', (0, 1), (-1, -1), 'Helvetica'),
                ('FONTSIZE', (0, 1), (-1, -1), 10),
            ]))
            story.append(stats_table)
            story.append(Spacer(1, 20))
        
        # Students Grades Table
        grades_title = Paragraph("Calificaciones por Estudiante", self.heading_style)
        story.append(grades_title)
        
        # Build table headers dynamically based on parciales
        max_parciales = max([len(s.get('notas_parciales', [])) for s in students_data], default=0)
        headers = ['#', 'Estudiante', 'Email']
        for i in range(1, max_parciales + 1):
            headers.append(f'P{i}')
        headers.extend(['Final', 'Estado'])
        
        table_data = [headers]
        
        for idx, student in enumerate(students_data, 1):
            row = [
                str(idx),
                student.get('nombre', ''),
                student.get('email', '')
            ]
            
            # Add parcial grades
            notas = student.get('notas_parciales', [])
            for i in range(max_parciales):
                nota = notas[i] if i < len(notas) else '-'
                row.append(f"{nota:.1f}" if isinstance(nota, (int, float)) else str(nota))
            
            # Add final grade and status
            row.append(f"{student.get('nota_final', 0):.2f}")
            estado = student.get('estado', 'N/A')
            row.append(estado)
            
            table_data.append(row)
        
        # Calculate column widths
        col_widths = [0.3*inch, 1.8*inch, 1.8*inch]
        parcial_width = (6.5 - 3.9) / max(max_parciales + 2, 1)
        col_widths.extend([parcial_width*inch] * max_parciales)
        col_widths.extend([0.7*inch, 0.7*inch])
        
        grades_table = Table(table_data, colWidths=col_widths)
        
        table_style = [
            ('BACKGROUND', (0, 0), (-1, 0), colors.HexColor('#2ECC71')),
            ('TEXTCOLOR', (0, 0), (-1, 0), colors.whitesmoke),
            ('ALIGN', (0, 0), (-1, -1), 'CENTER'),
            ('FONTNAME', (0, 0), (-1, 0), 'Helvetica-Bold'),
            ('FONTSIZE', (0, 0), (-1, 0), 10),
            ('BOTTOMPADDING', (0, 0), (-1, 0), 12),
            ('GRID', (0, 0), (-1, -1), 1, colors.black),
            ('FONTNAME', (0, 1), (-1, -1), 'Helvetica'),
            ('FONTSIZE', (0, 1), (-1, -1), 9),
            ('VALIGN', (0, 0), (-1, -1), 'MIDDLE'),
        ]
        
        # Color rows based on pass/fail
        for i, student in enumerate(students_data, 1):
            if student.get('estado') == 'APROBADO':
                table_style.append(('BACKGROUND', (0, i), (-1, i), colors.HexColor('#D5F4E6')))
            else:
                table_style.append(('BACKGROUND', (0, i), (-1, i), colors.HexColor('#FADBD8')))
        
        grades_table.setStyle(TableStyle(table_style))
        story.append(grades_table)
        
        # Footer
        story.append(Spacer(1, 30))
        footer = Paragraph(
            f"<i>Generado por StudentGest - {datetime.now().strftime('%d/%m/%Y %H:%M')}</i>",
            self.normal_style
        )
        story.append(footer)
        
        doc.build(story)
        buffer.seek(0)
        return buffer.getvalue()
    
    def create_student_performance_report(self, student_data: Dict, courses_data: List[Dict]) -> bytes:
        """Generate individual student performance report PDF"""
        buffer = BytesIO()
        doc = SimpleDocTemplate(buffer, pagesize=letter)
        
        story = []
        
        # Title
        title = Paragraph(f"Reporte de Desempeño<br/>{student_data['nombre']} {student_data.get('apellido', '')}", 
                         self.title_style)
        story.append(title)
        story.append(Spacer(1, 12))
        
        # Student Info
        info_data = [
            ['Email:', student_data.get('email', 'N/A')],
            ['Fecha:', datetime.now().strftime('%d/%m/%Y')],
            ['Total Cursos:', str(len(courses_data))],
        ]
        
        info_table = Table(info_data, colWidths=[2*inch, 4*inch])
        info_table.setStyle(TableStyle([
            ('FONTNAME', (0, 0), (0, -1), 'Helvetica-Bold'),
            ('FONTNAME', (1, 0), (1, -1), 'Helvetica'),
            ('FONTSIZE', (0, 0), (-1, -1), 11),
            ('TEXTCOLOR', (0, 0), (-1, -1), colors.HexColor('#2C3E50')),
        ]))
        story.append(info_table)
        story.append(Spacer(1, 20))
        
        # Courses Performance
        for course in courses_data:
            course_title = Paragraph(f"<b>{course['nombre']}</b> ({course['codigo']})", self.heading_style)
            story.append(course_title)
            
            parciales = course.get('parciales', [])
            if parciales:
                parciales_data = [['Parcial', 'Nota', 'Peso', 'Contribución']]
                for p in parciales:
                    parciales_data.append([
                        p.get('nombre', f"Parcial {p['numero']}"),
                        f"{p['nota']:.2f}",
                        f"{p['peso']}%",
                        f"{p['nota'] * p['peso'] / 100:.2f}"
                    ])
                
                parciales_data.append(['', '', 'Total:', f"{course['nota_final']:.2f}"])
                
                p_table = Table(parciales_data, colWidths=[2*inch, 1*inch, 1*inch, 1.5*inch])
                p_table.setStyle(TableStyle([
                    ('BACKGROUND', (0, 0), (-1, 0), colors.HexColor('#3498DB')),
                    ('TEXTCOLOR', (0, 0), (-1, 0), colors.whitesmoke),
                    ('FONTNAME', (0, 0), (-1, 0), 'Helvetica-Bold'),
                    ('GRID', (0, 0), (-1, -2), 1, colors.grey),
                    ('FONTNAME', (0, -1), (-1, -1), 'Helvetica-Bold'),
                    ('LINEABOVE', (0, -1), (-1, -1), 2, colors.black),
                    ('ALIGN', (1, 1), (-1, -1), 'CENTER'),
                ]))
                story.append(p_table)
            
            # Status
            estado_text = f"<b>Estado:</b> {course['estado']}"
            if course['estado'] == 'APROBADO':
                estado_text = f'<font color="green">{estado_text}</font>'
            else:
                estado_text = f'<font color="red">{estado_text}</font>'
            
            story.append(Paragraph(estado_text, self.normal_style))
            story.append(Spacer(1, 20))
        
        # Summary
        aprobados = sum(1 for c in courses_data if c['estado'] == 'APROBADO')
        promedio_general = sum(c['nota_final'] for c in courses_data) / len(courses_data) if courses_data else 0
        
        summary = Paragraph(f"<b>Resumen General:</b> {aprobados}/{len(courses_data)} cursos aprobados | "
                           f"Promedio: {promedio_general:.2f}", self.heading_style)
        story.append(summary)
        
        doc.build(story)
        buffer.seek(0)
        return buffer.getvalue()
    
    def create_system_overview_report(self, overview_data: Dict) -> bytes:
        """Generate system overview report for directors"""
        buffer = BytesIO()
        doc = SimpleDocTemplate(buffer, pagesize=letter)
        
        story = []
        
        title = Paragraph("Reporte General del Sistema", self.title_style)
        story.append(title)
        story.append(Spacer(1, 12))
        
        # System Stats
        stats_data = [
            ['Métrica', 'Valor'],
            ['Total Cursos', str(overview_data.get('total_cursos', 0))],
            ['Total Estudiantes', str(overview_data.get('total_estudiantes', 0))],
            ['Total Profesores', str(overview_data.get('total_profesores', 0))],
            ['Promedio General', f"{overview_data.get('promedio_general', 0):.2f}"],
            ['Tasa de Aprobación', f"{overview_data.get('tasa_aprobacion', 0):.1f}%"],
        ]
        
        stats_table = Table(stats_data, colWidths=[3*inch, 2*inch])
        stats_table.setStyle(TableStyle([
            ('BACKGROUND', (0, 0), (-1, 0), colors.HexColor('#9B59B6')),
            ('TEXTCOLOR', (0, 0), (-1, 0), colors.whitesmoke),
            ('ALIGN', (0, 0), (-1, -1), 'CENTER'),
            ('FONTNAME', (0, 0), (-1, 0), 'Helvetica-Bold'),
            ('GRID', (0, 0), (-1, -1), 1, colors.black),
            ('FONTNAME', (0, 1), (-1, -1), 'Helvetica'),
            ('BACKGROUND', (0, 1), (-1, -1), colors.beige),
        ]))
        story.append(stats_table)
        story.append(Spacer(1, 20))
        
        # Courses Performance
        if 'cursos_performance' in overview_data:
            perf_title = Paragraph("Desempeño por Curso", self.heading_style)
            story.append(perf_title)
            
            perf_data = [['Curso', 'Estudiantes', 'Promedio', 'Aprobación %']]
            for curso in overview_data['cursos_performance']:
                perf_data.append([
                    curso['nombre'],
                    str(curso['total_estudiantes']),
                    f"{curso['promedio']:.2f}",
                    f"{curso['tasa_aprobacion']:.1f}%"
                ])
            
            perf_table = Table(perf_data, colWidths=[2.5*inch, 1.2*inch, 1.2*inch, 1.2*inch])
            perf_table.setStyle(TableStyle([
                ('BACKGROUND', (0, 0), (-1, 0), colors.HexColor('#E74C3C')),
                ('TEXTCOLOR', (0, 0), (-1, 0), colors.whitesmoke),
                ('FONTNAME', (0, 0), (-1, 0), 'Helvetica-Bold'),
                ('GRID', (0, 0), (-1, -1), 1, colors.black),
                ('ALIGN', (1, 1), (-1, -1), 'CENTER'),
            ]))
            story.append(perf_table)
        
        doc.build(story)
        buffer.seek(0)
        return buffer.getvalue()
