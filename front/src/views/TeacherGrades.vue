<template>
  <div class="teacher-grades">
    <div class="container">
      <!-- Header -->
      <div class="page-header">
        <h1>📊 Gestión de Notas</h1>
        <p class="subtitle">Administra las calificaciones de tus cursos</p>
      </div>

      <!-- My Courses Selection -->
      <div class="courses-section">
        <h2>Mis Cursos</h2>
        <div v-if="loadingCourses" class="loading">Cargando cursos...</div>
        <div v-else-if="courses.length === 0" class="no-data">
          No tienes cursos asignados
        </div>
        <div v-else class="courses-grid">
          <div 
            v-for="course in courses" 
            :key="course.id"
            class="course-card"
            :class="{ active: selectedCourse?.id === course.id }"
            @click="selectCourse(course)"
          >
            <h3>{{ course.nombre }}</h3>
            <p class="course-code">{{ course.codigo }}</p>
            <p class="course-info">
              {{ course.descripcion || 'Click para ver estudiantes' }}
            </p>
          </div>
        </div>
      </div>

      <!-- Course Details and Grades -->
      <div v-if="selectedCourse" class="grades-section">
        <!-- Configuration Panel -->
        <div class="config-panel">
          <div class="config-header">
            <h2>{{ selectedCourse.nombre }}</h2>
            <button @click="showConfigModal = true" class="btn btn-primary">
              ⚙️ Configurar Parciales
            </button>
          </div>

          <div v-if="courseConfig" class="config-info">
            <div class="config-item">
              <span class="label">Número de Parciales:</span>
              <span class="value">{{ courseConfig.numero_parciales }}</span>
            </div>
            <div class="config-item">
              <span class="label">Nota de Aprobación:</span>
              <span class="value">{{ courseConfig.nota_aprobacion }}</span>
            </div>
            <div v-if="weights.length > 0" class="weights-list">
              <span class="label">Pesos:</span>
              <div class="weights">
                <span v-for="weight in weights" :key="weight.id" class="weight-badge">
                  {{ weight.nombre || `Parcial ${weight.numero_parcial}` }}: {{ weight.peso }}%
                </span>
              </div>
            </div>
          </div>
          <div v-else class="config-warning">
            ⚠️ Este curso no tiene configuración de notas. Haz clic en "Configurar Parciales" para empezar.
          </div>
        </div>

        <!-- Students Table -->
        <div v-if="courseConfig && studentList" class="students-table-container">
          <div class="table-header">
            <h3>Lista de Estudiantes ({{ studentList.estudiantes?.length || 0 }})</h3>
            <div class="table-actions">
              <input 
                v-model="searchQuery" 
                type="text" 
                placeholder="Buscar estudiante..." 
                class="search-input"
              />
            </div>
          </div>

          <div v-if="loadingStudents" class="loading">Cargando estudiantes...</div>
          <div v-else-if="filteredStudents.length === 0" class="no-data">
            No hay estudiantes inscritos
          </div>
          <div v-else class="table-wrapper">
            <table class="grades-table">
              <thead>
                <tr>
                  <th class="sticky-col">Estudiante</th>
                  <th class="sticky-col">Email</th>
                  <th v-for="weight in weights" :key="weight.id" class="grade-col">
                    {{ weight.nombre || `Parcial ${weight.numero_parcial}` }}
                    <br />
                    <span class="weight-label">({{ weight.peso }}%)</span>
                  </th>
                  <th class="final-col">Nota Final</th>
                  <th class="status-col">Estado</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="student in filteredStudents" :key="student.inscripcion_id">
                  <td class="sticky-col student-name">
                    {{ student.estudiante_nombre }} {{ student.estudiante_apellido }}
                  </td>
                  <td class="sticky-col">{{ student.estudiante_email }}</td>
                  <td 
                    v-for="parcial in student.parciales" 
                    :key="parcial.numero_parcial"
                    class="grade-cell"
                    @click="openGradeModal(student, parcial.numero_parcial)"
                  >
                    <input 
                      type="number" 
                      :value="parcial.nota"
                      @input="(e) => updateGradeLocally(student, parcial.numero_parcial, e.target.value)"
                      @blur="() => saveGrade(student, parcial.numero_parcial)"
                      min="0" 
                      max="100"
                      step="0.01"
                      class="grade-input"
                    />
                  </td>
                  <td class="final-grade" :class="{ passed: student.nota_final >= courseConfig.nota_aprobacion }">
                    {{ student.nota_final.toFixed(2) }}
                  </td>
                  <td class="status-cell">
                    <span class="status-badge" :class="student.estado.toLowerCase()">
                      {{ student.estado }}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- Configuration Modal -->
      <div v-if="showConfigModal" class="modal-overlay" @click="showConfigModal = false">
        <div class="modal-content" @click.stop>
          <div class="modal-header">
            <h2>{{ courseConfig ? 'Actualizar' : 'Configurar' }} Parciales</h2>
            <button @click="showConfigModal = false" class="close-btn">×</button>
          </div>

          <form @submit.prevent="saveConfiguration" class="config-form">
            <div class="form-group">
              <label>Número de Parciales (1-8):</label>
              <input 
                v-model.number="configForm.numero_parciales" 
                type="number" 
                min="1" 
                max="8"
                required
                @input="generateWeights"
              />
            </div>

            <div class="form-group">
              <label>Nota de Aprobación:</label>
              <input 
                v-model.number="configForm.nota_aprobacion" 
                type="number" 
                min="1" 
                max="100"
                required
              />
            </div>

            <div class="weights-config">
              <h3>Pesos de Parciales (Total debe ser 100%)</h3>
              <div v-for="(weight, index) in configForm.pesos" :key="index" class="weight-item">
                <input 
                  v-model="weight.nombre" 
                  type="text" 
                  :placeholder="`Nombre Parcial ${index + 1}`"
                  class="weight-name"
                />
                <input 
                  v-model.number="weight.peso" 
                  type="number" 
                  min="0" 
                  max="100"
                  step="0.01"
                  required
                  class="weight-peso"
                />
                <span class="percent">%</span>
              </div>
              <div class="weight-total" :class="{ invalid: totalWeight !== 100 }">
                Total: {{ totalWeight.toFixed(2) }}%
                <span v-if="totalWeight !== 100" class="error-text">
                  (Debe ser 100%)
                </span>
              </div>
            </div>

            <div v-if="formError" class="form-error">{{ formError }}</div>

            <div class="modal-actions">
              <button type="button" @click="showConfigModal = false" class="btn btn-secondary">
                Cancelar
              </button>
              <button type="submit" class="btn btn-primary" :disabled="submitting || totalWeight !== 100">
                {{ submitting ? 'Guardando...' : 'Guardar Configuración' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { supabase } from '../supabase'

// State
const courses = ref([])
const selectedCourse = ref(null)
const courseConfig = ref(null)
const weights = ref([])
const studentList = ref(null)
const searchQuery = ref('')

const loadingCourses = ref(false)
const loadingStudents = ref(false)
const showConfigModal = ref(false)
const submitting = ref(false)
const formError = ref(null)

const configForm = ref({
  numero_parciales: 3,
  nota_aprobacion: 60,
  pesos: []
})

// API Base URL
const GRADES_API = import.meta.env.VITE_GRADES_API || 'http://localhost:5013'
const COURSES_API = import.meta.env.VITE_COURSES_API || 'http://localhost:5008'

// Computed
const totalWeight = computed(() => {
  return configForm.value.pesos.reduce((sum, w) => sum + (parseFloat(w.peso) || 0), 0)
})

const filteredStudents = computed(() => {
  if (!studentList.value?.estudiantes) return []
  
  if (!searchQuery.value) return studentList.value.estudiantes
  
  const query = searchQuery.value.toLowerCase()
  return studentList.value.estudiantes.filter(s => 
    s.estudiante_nombre.toLowerCase().includes(query) ||
    s.estudiante_apellido.toLowerCase().includes(query) ||
    s.estudiante_email.toLowerCase().includes(query)
  )
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

async function loadMyCourses() {
  loadingCourses.value = true
  try {
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) throw new Error('No estás autenticado')

    const response = await fetch(`${COURSES_API}/api/courses/my-courses`, {
      headers: { 
        'Authorization': `Bearer ${session.access_token}`
      }
    })

    if (!response.ok) throw new Error('Error al cargar cursos')
    
    const data = await response.json()
    // El endpoint retorna { usuario, rol, cursos: [...] }
    courses.value = data.cursos || []
  } catch (err) {
    console.error('Error loading courses:', err)
    alert('Error al cargar cursos: ' + err.message)
  } finally {
    loadingCourses.value = false
  }
}

async function selectCourse(course) {
  selectedCourse.value = course
  courseConfig.value = null
  weights.value = []
  studentList.value = null
  
  await loadCourseConfig()
  if (courseConfig.value) {
    await loadWeights()
    await loadStudentList()
  }
}

async function loadCourseConfig() {
  try {
    const email = await getUserEmail()
    const response = await fetch(
      `${GRADES_API}/api/grades/config/${selectedCourse.value.id}`,
      { headers: { 'X-User-Email': email } }
    )
    
    if (response.ok) {
      courseConfig.value = await response.json()
    } else if (response.status === 404) {
      courseConfig.value = null
    }
  } catch (err) {
    console.error('Error loading config:', err)
  }
}

async function loadWeights() {
  try {
    const email = await getUserEmail()
    const response = await fetch(
      `${GRADES_API}/api/grades/weights/${courseConfig.value.id}`,
      { headers: { 'X-User-Email': email } }
    )
    
    if (response.ok) {
      weights.value = await response.json()
    }
  } catch (err) {
    console.error('Error loading weights:', err)
  }
}

async function loadStudentList() {
  loadingStudents.value = true
  try {
    const email = await getUserEmail()
    const response = await fetch(
      `${GRADES_API}/api/grades/course/${selectedCourse.value.id}/students`,
      { headers: { 'X-User-Email': email } }
    )
    
    if (response.ok) {
      studentList.value = await response.json()
    }
  } catch (err) {
    console.error('Error loading students:', err)
  } finally {
    loadingStudents.value = false
  }
}

function generateWeights() {
  const numParciales = configForm.value.numero_parciales
  const equalWeight = (100 / numParciales).toFixed(2)
  
  configForm.value.pesos = Array.from({ length: numParciales }, (_, i) => ({
    numero_parcial: i + 1,
    peso: parseFloat(equalWeight),
    nombre: `Parcial ${i + 1}`
  }))
  
  // Adjust last weight to ensure total is exactly 100
  const currentTotal = configForm.value.pesos.reduce((sum, w) => sum + w.peso, 0)
  if (currentTotal !== 100) {
    configForm.value.pesos[numParciales - 1].peso += (100 - currentTotal)
  }
}

async function saveConfiguration() {
  if (totalWeight.value !== 100) {
    formError.value = 'Los pesos deben sumar exactamente 100%'
    return
  }
  
  submitting.value = true
  formError.value = null
  
  try {
    const email = await getUserEmail()
    const payload = {
      curso_id: selectedCourse.value.id,
      numero_parciales: configForm.value.numero_parciales,
      nota_aprobacion: configForm.value.nota_aprobacion,
      pesos: configForm.value.pesos
    }
    
    const response = await fetch(`${GRADES_API}/api/grades/config-complete`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-User-Email': email
      },
      body: JSON.stringify(payload)
    })
    
    if (!response.ok) {
      const error = await response.json()
      throw new Error(error.detail || 'Error al guardar configuración')
    }
    
    showConfigModal.value = false
    await loadCourseConfig()
    await loadWeights()
    await loadStudentList()
  } catch (err) {
    formError.value = err.message
  } finally {
    submitting.value = false
  }
}

function updateGradeLocally(student, numero_parcial, value) {
  const parcial = student.parciales.find(p => p.numero_parcial === numero_parcial)
  if (parcial) {
    parcial.nota = parseFloat(value) || 0
  }
}

async function saveGrade(student, numero_parcial) {
  const parcial = student.parciales.find(p => p.numero_parcial === numero_parcial)
  if (!parcial) return
  
  try {
    const email = await getUserEmail()
    const payload = {
      inscripcion_id: student.inscripcion_id,
      curso_id: selectedCourse.value.id,
      usuario_id: student.usuario_id,
      numero_parcial: numero_parcial,
      nota: parcial.nota
    }
    
    const response = await fetch(`${GRADES_API}/api/grades/grade`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-User-Email': email
      },
      body: JSON.stringify(payload)
    })
    
    if (!response.ok) throw new Error('Error al guardar nota')
    
    // Reload to recalculate final grade
    await loadStudentList()
  } catch (err) {
    console.error('Error saving grade:', err)
    alert('Error al guardar nota: ' + err.message)
  }
}

function openGradeModal(student, numero_parcial) {
  // Optional: Open a modal for editing with observations
}

onMounted(() => {
  loadMyCourses()
  generateWeights()
})
</script>

<style scoped>
.teacher-grades {
  min-height: calc(100vh - 120px);
  padding: 2rem 0;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.container {
  max-width: 1600px;
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

/* Courses Section */
.courses-section {
  background: white;
  border-radius: 12px;
  padding: 2rem;
  margin-bottom: 2rem;
}

.courses-section h2 {
  margin-top: 0;
  color: #333;
}

.courses-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 1rem;
  margin-top: 1rem;
}

.course-card {
  background: #f8f9fa;
  border: 2px solid #e0e0e0;
  border-radius: 8px;
  padding: 1.5rem;
  cursor: pointer;
  transition: all 0.3s;
}

.course-card:hover {
  border-color: #667eea;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.2);
}

.course-card.active {
  border-color: #667eea;
  background: #e8eaf6;
}

.course-card h3 {
  margin: 0 0 0.5rem 0;
  color: #333;
  font-size: 1.1rem;
}

.course-code {
  color: #666;
  font-size: 0.9rem;
  margin: 0.25rem 0;
}

.course-info {
  color: #999;
  font-size: 0.85rem;
  margin: 0.5rem 0 0 0;
}

/* Grades Section */
.grades-section {
  background: white;
  border-radius: 12px;
  padding: 2rem;
}

.config-panel {
  margin-bottom: 2rem;
  padding-bottom: 2rem;
  border-bottom: 2px solid #e0e0e0;
}

.config-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.config-header h2 {
  margin: 0;
  color: #333;
}

.config-info {
  display: flex;
  gap: 2rem;
  flex-wrap: wrap;
  margin-top: 1rem;
}

.config-item {
  display: flex;
  gap: 0.5rem;
}

.config-item .label {
  font-weight: 600;
  color: #666;
}

.config-item .value {
  color: #333;
}

.weights-list {
  display: flex;
  gap: 0.5rem;
  align-items: flex-start;
}

.weights {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.weight-badge {
  background: #e3f2fd;
  color: #1976d2;
  padding: 0.25rem 0.75rem;
  border-radius: 12px;
  font-size: 0.85rem;
}

.config-warning {
  padding: 1rem;
  background: #fff3e0;
  color: #e65100;
  border-radius: 8px;
  margin-top: 1rem;
}

/* Students Table */
.students-table-container {
  margin-top: 2rem;
}

.table-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.table-header h3 {
  margin: 0;
  color: #333;
}

.search-input {
  padding: 0.5rem 1rem;
  border: 1px solid #ddd;
  border-radius: 8px;
  width: 300px;
}

.table-wrapper {
  overflow-x: auto;
}

.grades-table {
  width: 100%;
  border-collapse: collapse;
  min-width: 800px;
}

.grades-table thead {
  background: #667eea;
  color: white;
}

.grades-table th,
.grades-table td {
  padding: 0.75rem;
  text-align: left;
  border: 1px solid #ddd;
}

.grades-table th {
  font-weight: 600;
  white-space: nowrap;
}

.grades-table tbody tr:hover {
  background: #f8f9fa;
}

.sticky-col {
  position: sticky;
  background: white;
  z-index: 1;
}

.sticky-col:first-child {
  left: 0;
}

.sticky-col:nth-child(2) {
  left: 200px;
}

.student-name {
  font-weight: 600;
  color: #333;
}

.grade-cell {
  text-align: center;
  padding: 0.25rem;
}

.grade-input {
  width: 80px;
  padding: 0.5rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  text-align: center;
  font-size: 1rem;
}

.grade-input:focus {
  outline: none;
  border-color: #667eea;
}

.weight-label {
  font-size: 0.75rem;
  color: #e0e0e0;
}

.final-grade {
  font-weight: 700;
  font-size: 1.1rem;
  text-align: center;
  color: #d32f2f;
}

.final-grade.passed {
  color: #388e3c;
}

.status-cell {
  text-align: center;
}

.status-badge {
  padding: 0.25rem 0.75rem;
  border-radius: 12px;
  font-size: 0.85rem;
  font-weight: 600;
}

.status-badge.aprobado {
  background: #c8e6c9;
  color: #2e7d32;
}

.status-badge.reprobado {
  background: #ffcdd2;
  color: #c62828;
}

/* Modal */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.7);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
  padding: 1rem;
}

.modal-content {
  background: white;
  border-radius: 12px;
  max-width: 700px;
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  border-bottom: 1px solid #eee;
}

.modal-header h2 {
  margin: 0;
}

.close-btn {
  background: none;
  border: none;
  font-size: 2rem;
  cursor: pointer;
  color: #999;
}

.close-btn:hover {
  color: #333;
}

.config-form {
  padding: 1.5rem;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 600;
  color: #333;
}

.form-group input {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 8px;
  font-size: 1rem;
}

.weights-config {
  margin: 2rem 0;
  padding: 1.5rem;
  background: #f8f9fa;
  border-radius: 8px;
}

.weights-config h3 {
  margin-top: 0;
  color: #333;
}

.weight-item {
  display: flex;
  gap: 1rem;
  align-items: center;
  margin-bottom: 1rem;
}

.weight-name {
  flex: 1;
  padding: 0.5rem;
  border: 1px solid #ddd;
  border-radius: 4px;
}

.weight-peso {
  width: 100px;
  padding: 0.5rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  text-align: right;
}

.percent {
  color: #666;
  font-weight: 600;
}

.weight-total {
  margin-top: 1rem;
  padding: 1rem;
  background: white;
  border-radius: 8px;
  font-weight: 700;
  font-size: 1.2rem;
  color: #388e3c;
  text-align: center;
}

.weight-total.invalid {
  background: #ffebee;
  color: #c62828;
}

.error-text {
  font-size: 0.9rem;
  margin-left: 0.5rem;
}

.form-error {
  padding: 1rem;
  background: #ffebee;
  color: #c62828;
  border-radius: 8px;
  margin-bottom: 1rem;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  padding: 1.5rem;
  border-top: 1px solid #eee;
}

.btn {
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
}

.btn-primary {
  background: #667eea;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: #5568d3;
}

.btn-primary:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.btn-secondary {
  background: #6c757d;
  color: white;
}

.btn-secondary:hover {
  background: #5a6268;
}

.loading,
.no-data {
  text-align: center;
  padding: 2rem;
  color: #999;
}

@media (max-width: 768px) {
  .courses-grid {
    grid-template-columns: 1fr;
  }
  
  .config-header {
    flex-direction: column;
    gap: 1rem;
  }
  
  .search-input {
    width: 100%;
  }
}
</style>
