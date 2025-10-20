<template>
  <div class="student-grades">
    <div class="container">
      <!-- Header -->
      <div class="page-header">
        <h1>📚 Mis Calificaciones</h1>
        <p class="subtitle">Consulta tus notas en todos tus cursos</p>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="loading">
        <p>Cargando calificaciones...</p>
      </div>

      <!-- No Data -->
      <div v-else-if="grades.length === 0" class="no-data">
        <p>📭 No tienes calificaciones registradas aún</p>
      </div>

      <!-- Grades Cards -->
      <div v-else class="grades-container">
        <div v-for="course in grades" :key="course.curso_id" class="course-card">
          <!-- Course Header -->
          <div class="course-header">
            <div class="course-info">
              <h2>{{ course.curso_nombre }}</h2>
              <p class="course-code">{{ course.curso_codigo }}</p>
            </div>
            <div class="final-grade-box" :class="{ passed: course.nota_final >= course.nota_aprobacion }">
              <div class="final-grade-label">Nota Final</div>
              <div class="final-grade-value">{{ course.nota_final.toFixed(2) }}</div>
              <div class="status-badge" :class="course.estado.toLowerCase()">
                {{ course.estado }}
              </div>
            </div>
          </div>

          <!-- Parciales Table -->
          <div class="parciales-container">
            <table class="parciales-table">
              <thead>
                <tr>
                  <th>Parcial</th>
                  <th>Nota</th>
                  <th>Peso</th>
                  <th>Contribución</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="parcial in course.parciales" :key="parcial.numero_parcial">
                  <td class="parcial-name">
                    {{ parcial.nombre || `Parcial ${parcial.numero_parcial}` }}
                  </td>
                  <td class="parcial-nota" :class="{ 'nota-zero': parcial.nota === 0 }">
                    {{ parcial.nota.toFixed(2) }}
                  </td>
                  <td class="parcial-peso">
                    {{ parcial.peso }}%
                  </td>
                  <td class="parcial-contribution">
                    {{ (parcial.nota * parcial.peso / 100).toFixed(2) }}
                  </td>
                </tr>
              </tbody>
              <tfoot>
                <tr class="total-row">
                  <td colspan="3"><strong>Total (Nota de Aprobación: {{ course.nota_aprobacion }})</strong></td>
                  <td><strong>{{ course.nota_final.toFixed(2) }}</strong></td>
                </tr>
              </tfoot>
            </table>
          </div>

          <!-- Progress Bar -->
          <div class="progress-section">
            <div class="progress-label">
              Progreso hacia la aprobación
            </div>
            <div class="progress-bar">
              <div 
                class="progress-fill" 
                :class="{ passed: course.nota_final >= course.nota_aprobacion }"
                :style="{ width: Math.min((course.nota_final / 100) * 100, 100) + '%' }"
              ></div>
              <div 
                class="passing-line" 
                :style="{ left: course.nota_aprobacion + '%' }"
              >
                <span class="passing-label">{{ course.nota_aprobacion }}</span>
              </div>
            </div>
            <div class="progress-info">
              <span>0</span>
              <span>{{ course.nota_final.toFixed(2) }} / 100</span>
            </div>
          </div>
        </div>

        <!-- Summary Card -->
        <div class="summary-card">
          <h2>📊 Resumen General</h2>
          <div class="summary-stats">
            <div class="stat-item">
              <div class="stat-label">Total de Cursos</div>
              <div class="stat-value">{{ grades.length }}</div>
            </div>
            <div class="stat-item">
              <div class="stat-label">Aprobados</div>
              <div class="stat-value passed">{{ approvedCount }}</div>
            </div>
            <div class="stat-item">
              <div class="stat-label">Reprobados</div>
              <div class="stat-value failed">{{ failedCount }}</div>
            </div>
            <div class="stat-item">
              <div class="stat-label">Promedio General</div>
              <div class="stat-value">{{ generalAverage.toFixed(2) }}</div>
            </div>
          </div>

          <!-- Performance Chart (text-based) -->
          <div class="performance-section">
            <h3>Rendimiento por Curso</h3>
            <div class="performance-bars">
              <div v-for="course in sortedGrades" :key="course.curso_id" class="performance-item">
                <div class="performance-label">{{ course.curso_codigo }}</div>
                <div class="performance-bar">
                  <div 
                    class="performance-fill"
                    :class="{ passed: course.nota_final >= course.nota_aprobacion }"
                    :style="{ width: (course.nota_final / 100 * 100) + '%' }"
                  >
                    <span class="performance-value">{{ course.nota_final.toFixed(1) }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'

// State
const grades = ref([])
const loading = ref(false)

// API Base URL
const GRADES_API = import.meta.env.VITE_GRADES_API || 'http://localhost:5013'

// Computed
const approvedCount = computed(() => {
  return grades.value.filter(g => g.estado === 'APROBADO').length
})

const failedCount = computed(() => {
  return grades.value.filter(g => g.estado === 'REPROBADO').length
})

const generalAverage = computed(() => {
  if (grades.value.length === 0) return 0
  const sum = grades.value.reduce((acc, g) => acc + g.nota_final, 0)
  return sum / grades.value.length
})

const sortedGrades = computed(() => {
  return [...grades.value].sort((a, b) => b.nota_final - a.nota_final)
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

async function loadMyGrades() {
  loading.value = true
  try {
    const email = await getUserEmail()
    if (!email) throw new Error('No se pudo obtener el email del usuario')

    const response = await fetch(`${GRADES_API}/api/grades/my-grades`, {
      headers: { 'X-User-Email': email }
    })

    if (!response.ok) throw new Error('Error al cargar calificaciones')
    
    grades.value = await response.json()
  } catch (err) {
    console.error('Error loading grades:', err)
    alert('Error al cargar calificaciones: ' + err.message)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadMyGrades()
})
</script>

<style scoped>
.student-grades {
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

.loading,
.no-data {
  background: white;
  border-radius: 12px;
  padding: 3rem;
  text-align: center;
  color: #666;
}

.no-data p {
  font-size: 1.2rem;
  margin: 0;
}

/* Grades Container */
.grades-container {
  display: grid;
  gap: 1.5rem;
}

/* Course Card */
.course-card {
  background: white;
  border-radius: 12px;
  padding: 2rem;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.course-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1.5rem;
  padding-bottom: 1.5rem;
  border-bottom: 2px solid #e0e0e0;
}

.course-info h2 {
  margin: 0 0 0.5rem 0;
  color: #333;
  font-size: 1.5rem;
}

.course-code {
  color: #666;
  margin: 0;
  font-size: 0.9rem;
}

.final-grade-box {
  text-align: center;
  padding: 1.5rem;
  background: #ffebee;
  border-radius: 12px;
  min-width: 150px;
}

.final-grade-box.passed {
  background: #e8f5e9;
}

.final-grade-label {
  font-size: 0.85rem;
  color: #666;
  margin-bottom: 0.5rem;
  text-transform: uppercase;
}

.final-grade-value {
  font-size: 2.5rem;
  font-weight: 700;
  color: #c62828;
  margin-bottom: 0.5rem;
}

.final-grade-box.passed .final-grade-value {
  color: #2e7d32;
}

.status-badge {
  display: inline-block;
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

/* Parciales Table */
.parciales-container {
  overflow-x: auto;
  margin-bottom: 1.5rem;
}

.parciales-table {
  width: 100%;
  border-collapse: collapse;
}

.parciales-table thead {
  background: #f5f5f5;
}

.parciales-table th,
.parciales-table td {
  padding: 0.75rem;
  text-align: left;
  border-bottom: 1px solid #e0e0e0;
}

.parciales-table th {
  font-weight: 600;
  color: #666;
  font-size: 0.9rem;
  text-transform: uppercase;
}

.parcial-name {
  font-weight: 500;
  color: #333;
}

.parcial-nota {
  font-weight: 700;
  font-size: 1.1rem;
  color: #2e7d32;
}

.parcial-nota.nota-zero {
  color: #c62828;
}

.parcial-peso {
  color: #666;
}

.parcial-contribution {
  font-weight: 600;
  color: #333;
}

.total-row {
  background: #f5f5f5;
  font-size: 1.1rem;
}

.total-row td {
  padding: 1rem 0.75rem;
  border-top: 2px solid #333;
}

/* Progress Section */
.progress-section {
  margin-top: 1.5rem;
}

.progress-label {
  font-weight: 600;
  color: #666;
  margin-bottom: 0.5rem;
  font-size: 0.9rem;
}

.progress-bar {
  position: relative;
  height: 40px;
  background: #e0e0e0;
  border-radius: 20px;
  overflow: visible;
  margin-bottom: 0.5rem;
}

.progress-fill {
  height: 100%;
  background: linear-gradient(90deg, #ef5350, #e53935);
  border-radius: 20px;
  transition: width 0.5s ease;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  padding-right: 1rem;
  color: white;
  font-weight: 700;
}

.progress-fill.passed {
  background: linear-gradient(90deg, #66bb6a, #43a047);
}

.passing-line {
  position: absolute;
  top: -5px;
  bottom: -5px;
  width: 3px;
  background: #ff9800;
  z-index: 1;
}

.passing-label {
  position: absolute;
  top: -25px;
  left: 50%;
  transform: translateX(-50%);
  background: #ff9800;
  color: white;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: 600;
  white-space: nowrap;
}

.progress-info {
  display: flex;
  justify-content: space-between;
  font-size: 0.85rem;
  color: #666;
}

/* Summary Card */
.summary-card {
  background: white;
  border-radius: 12px;
  padding: 2rem;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.summary-card h2 {
  margin-top: 0;
  color: #333;
}

.summary-stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  gap: 1rem;
  margin-bottom: 2rem;
}

.stat-item {
  text-align: center;
  padding: 1.5rem;
  background: #f5f5f5;
  border-radius: 8px;
}

.stat-label {
  font-size: 0.85rem;
  color: #666;
  margin-bottom: 0.5rem;
  text-transform: uppercase;
}

.stat-value {
  font-size: 2rem;
  font-weight: 700;
  color: #333;
}

.stat-value.passed {
  color: #2e7d32;
}

.stat-value.failed {
  color: #c62828;
}

/* Performance Section */
.performance-section {
  margin-top: 2rem;
  padding-top: 2rem;
  border-top: 2px solid #e0e0e0;
}

.performance-section h3 {
  margin-top: 0;
  margin-bottom: 1rem;
  color: #333;
}

.performance-bars {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.performance-item {
  display: grid;
  grid-template-columns: 100px 1fr;
  align-items: center;
  gap: 1rem;
}

.performance-label {
  font-weight: 600;
  color: #666;
  font-size: 0.9rem;
}

.performance-bar {
  height: 30px;
  background: #e0e0e0;
  border-radius: 15px;
  overflow: hidden;
  position: relative;
}

.performance-fill {
  height: 100%;
  background: #ef5350;
  transition: width 0.5s ease;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  padding-right: 0.75rem;
  min-width: 50px;
}

.performance-fill.passed {
  background: #66bb6a;
}

.performance-value {
  color: white;
  font-weight: 700;
  font-size: 0.85rem;
}

@media (max-width: 768px) {
  .course-header {
    flex-direction: column;
    gap: 1rem;
  }

  .final-grade-box {
    width: 100%;
  }

  .summary-stats {
    grid-template-columns: repeat(2, 1fr);
  }

  .performance-item {
    grid-template-columns: 80px 1fr;
    gap: 0.5rem;
  }
}
</style>
