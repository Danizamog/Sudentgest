<template>
  <div class="teacher-reports">
    <div class="container">
      <!-- Header -->
      <div class="page-header">
        <h1>📊 Reportes</h1>
        <p class="subtitle">Genera reportes detallados de tus cursos</p>
      </div>

      <!-- Report Type Selection -->
      <div class="report-types">
        <div 
          class="report-type-card"
          :class="{ active: selectedType === 'course' }"
          @click="selectedType = 'course'"
        >
          <div class="icon">📈</div>
          <h3>Reporte de Curso</h3>
          <p>Calificaciones de todos los estudiantes de un curso con estadísticas</p>
        </div>

        <div 
          class="report-type-card"
          :class="{ active: selectedType === 'student' }"
          @click="selectedType = 'student'"
        >
          <div class="icon">👤</div>
          <h3>Reporte de Estudiante</h3>
          <p>Desempeño detallado de un estudiante específico</p>
        </div>
      </div>

      <!-- Course Report Form -->
      <div v-if="selectedType === 'course'" class="report-form">
        <h2>Generar Reporte de Curso</h2>
        
        <div class="form-group">
          <label>Selecciona el Curso:</label>
          <select v-model="courseReportForm.curso_id" class="form-select">
            <option value="">-- Selecciona un curso --</option>
            <option v-for="course in courses" :key="course.id" :value="course.id">
              {{ course.nombre }} ({{ course.codigo }})
            </option>
          </select>
        </div>

        <div class="form-group">
          <label>Formato:</label>
          <div class="format-options">
            <label class="radio-option">
              <input type="radio" v-model="courseReportForm.format" value="pdf" />
              <span>📄 PDF</span>
            </label>
            <label class="radio-option">
              <input type="radio" v-model="courseReportForm.format" value="excel" />
              <span>📊 Excel</span>
            </label>
          </div>
        </div>

        <button 
          @click="generateCourseReport" 
          :disabled="!courseReportForm.curso_id || generating"
          class="btn btn-primary"
        >
          {{ generating ? 'Generando...' : 'Generar Reporte' }}
        </button>
      </div>

      <!-- Student Report Form -->
      <div v-if="selectedType === 'student'" class="report-form">
        <h2>Generar Reporte de Estudiante</h2>
        
        <div class="form-group">
          <label>Selecciona el Curso:</label>
          <select v-model="studentReportForm.curso_id" @change="loadStudentsForCourse" class="form-select">
            <option value="">-- Selecciona un curso --</option>
            <option v-for="course in courses" :key="course.id" :value="course.id">
              {{ course.nombre }} ({{ course.codigo }})
            </option>
          </select>
        </div>

        <div v-if="studentReportForm.curso_id" class="form-group">
          <label>Selecciona el Estudiante:</label>
          <select v-model="studentReportForm.usuario_id" class="form-select">
            <option value="">-- Selecciona un estudiante --</option>
            <option v-for="student in students" :key="student.usuario_id" :value="student.usuario_id">
              {{ student.estudiante_nombre }} {{ student.estudiante_apellido }} - {{ student.estudiante_email }}
            </option>
          </select>
        </div>

        <div class="form-group">
          <label>Formato:</label>
          <div class="format-options">
            <label class="radio-option">
              <input type="radio" v-model="studentReportForm.format" value="pdf" />
              <span>📄 PDF</span>
            </label>
            <label class="radio-option">
              <input type="radio" v-model="studentReportForm.format" value="excel" />
              <span>📊 Excel</span>
            </label>
          </div>
        </div>

        <button 
          @click="generateStudentReport" 
          :disabled="!studentReportForm.usuario_id || generating"
          class="btn btn-primary"
        >
          {{ generating ? 'Generando...' : 'Generar Reporte' }}
        </button>
      </div>

      <!-- Error Message -->
      <div v-if="errorMessage" class="error-message">
        {{ errorMessage }}
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { supabase } from '../supabase'

// API Base URLs
const COURSES_API = import.meta.env.VITE_COURSES_API || 'http://localhost:5008'
const GRADES_API = import.meta.env.VITE_GRADES_API || 'http://localhost:5013'
const REPORTS_API = import.meta.env.VITE_REPORTS_API || 'http://localhost:5014'

// State
const selectedType = ref('course')
const courses = ref([])
const students = ref([])
const generating = ref(false)
const errorMessage = ref('')

const courseReportForm = ref({
  curso_id: '',
  format: 'pdf'
})

const studentReportForm = ref({
  curso_id: '',
  usuario_id: '',
  format: 'pdf'
})

// Methods
async function getUserEmail() {
  try {
    const response = await fetch('/auth/user-profile', { credentials: 'include' })
    if (response.ok) {
      const profile = await response.json()
      return profile.email
    }
  } catch (err) {
    console.error('Error getting user email:', err)
  }
  return null
}

async function loadCourses() {
  try {
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) throw new Error('No autenticado')

    const response = await fetch(`${COURSES_API}/api/courses/my-courses`, {
      headers: { 'Authorization': `Bearer ${session.access_token}` }
    })

    if (!response.ok) throw new Error('Error al cargar cursos')
    
    const data = await response.json()
    courses.value = data.cursos || []
  } catch (err) {
    console.error('Error loading courses:', err)
    errorMessage.value = 'Error al cargar cursos: ' + err.message
  }
}

async function loadStudentsForCourse() {
  if (!studentReportForm.value.curso_id) return
  
  try {
    const email = await getUserEmail()
    const response = await fetch(
      `${GRADES_API}/api/grades/course/${studentReportForm.value.curso_id}/students`,
      { headers: { 'X-User-Email': email } }
    )

    if (!response.ok) throw new Error('Error al cargar estudiantes')
    
    const data = await response.json()
    students.value = data.estudiantes || []
  } catch (err) {
    console.error('Error loading students:', err)
    errorMessage.value = 'Error al cargar estudiantes: ' + err.message
  }
}

async function generateCourseReport() {
  generating.value = true
  errorMessage.value = ''
  
  try {
    const email = await getUserEmail()
    if (!email) throw new Error('No se pudo obtener el email del usuario')

    const response = await fetch(
      `${REPORTS_API}/api/reports/course-grades/${courseReportForm.value.curso_id}`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-User-Email': email
        },
        body: JSON.stringify({
          format: courseReportForm.value.format,
          include_statistics: true
        })
      }
    )

    if (!response.ok) {
      const error = await response.json()
      throw new Error(error.detail || 'Error al generar reporte')
    }

    // Download file
    const blob = await response.blob()
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    const ext = courseReportForm.value.format === 'pdf' ? 'pdf' : 'xlsx'
    a.download = `curso_${courseReportForm.value.curso_id}_calificaciones.${ext}`
    document.body.appendChild(a)
    a.click()
    window.URL.revokeObjectURL(url)
    document.body.removeChild(a)
  } catch (err) {
    console.error('Error generating report:', err)
    errorMessage.value = err.message
  } finally {
    generating.value = false
  }
}

async function generateStudentReport() {
  generating.value = true
  errorMessage.value = ''
  
  try {
    const email = await getUserEmail()
    if (!email) throw new Error('No se pudo obtener el email del usuario')

    const response = await fetch(
      `${REPORTS_API}/api/reports/student-performance/${studentReportForm.value.usuario_id}`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-User-Email': email
        },
        body: JSON.stringify({
          curso_id: parseInt(studentReportForm.value.curso_id),
          format: studentReportForm.value.format
        })
      }
    )

    if (!response.ok) {
      const error = await response.json()
      throw new Error(error.detail || 'Error al generar reporte')
    }

    // Download file
    const blob = await response.blob()
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    const ext = studentReportForm.value.format === 'pdf' ? 'pdf' : 'xlsx'
    a.download = `estudiante_${studentReportForm.value.usuario_id}_desempeno.${ext}`
    document.body.appendChild(a)
    a.click()
    window.URL.revokeObjectURL(url)
    document.body.removeChild(a)
  } catch (err) {
    console.error('Error generating report:', err)
    errorMessage.value = err.message
  } finally {
    generating.value = false
  }
}

onMounted(() => {
  loadCourses()
})
</script>

<style scoped>
.teacher-reports {
  min-height: calc(100vh - 120px);
  padding: 2rem 0;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 1rem;
}

.page-header {
  text-align: center;
  margin-bottom: 2rem;
  color: white;
}

.page-header h1 {
  font-size: 2.5rem;
  margin-bottom: 0.5rem;
}

.subtitle {
  font-size: 1.1rem;
  opacity: 0.9;
}

/* Report Types */
.report-types {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}

.report-type-card {
  background: white;
  border-radius: 12px;
  padding: 2rem;
  cursor: pointer;
  transition: all 0.3s ease;
  border: 3px solid transparent;
}

.report-type-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.2);
}

.report-type-card.active {
  border-color: #667eea;
  box-shadow: 0 8px 20px rgba(102, 126, 234, 0.3);
}

.report-type-card .icon {
  font-size: 3rem;
  margin-bottom: 1rem;
}

.report-type-card h3 {
  margin: 0 0 0.5rem 0;
  color: #333;
}

.report-type-card p {
  margin: 0;
  color: #666;
  font-size: 0.9rem;
}

/* Report Form */
.report-form {
  background: white;
  border-radius: 12px;
  padding: 2rem;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.report-form h2 {
  margin-top: 0;
  color: #333;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 600;
  color: #555;
}

.form-select {
  width: 100%;
  padding: 0.75rem;
  border: 2px solid #ddd;
  border-radius: 8px;
  font-size: 1rem;
  transition: border-color 0.3s;
}

.form-select:focus {
  outline: none;
  border-color: #667eea;
}

.format-options {
  display: flex;
  gap: 1rem;
}

.radio-option {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1.5rem;
  border: 2px solid #ddd;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.3s;
}

.radio-option:hover {
  border-color: #667eea;
  background: #f8f9ff;
}

.radio-option input[type="radio"] {
  width: 18px;
  height: 18px;
}

.radio-option input[type="radio"]:checked + span {
  font-weight: 600;
  color: #667eea;
}

.btn {
  padding: 0.75rem 2rem;
  border: none;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
}

.btn-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.btn-primary:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 6px 20px rgba(102, 126, 234, 0.4);
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.error-message {
  margin-top: 1.5rem;
  padding: 1rem;
  background: #ffebee;
  border-left: 4px solid #f44336;
  border-radius: 4px;
  color: #c62828;
}

@media (max-width: 768px) {
  .report-types {
    grid-template-columns: 1fr;
  }
  
  .format-options {
    flex-direction: column;
  }
}
</style>
