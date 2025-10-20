<template>
  <div class="teacher-dashboard">
    <h1 class="dashboard-title">📊 Panel de Control - Profesor</h1>

    <!-- Loading State -->
    <div v-if="loading" class="loading">
      <div class="spinner"></div>
      <p>Cargando datos...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="error-message">
      {{ error }}
    </div>

    <!-- Dashboard Content -->
    <div v-else class="dashboard-content">
      <!-- KPI Cards -->
      <div class="kpi-section">
        <div class="kpi-card">
          <div class="kpi-icon">📚</div>
          <div class="kpi-content">
            <h3>{{ dashboardData.total_cursos }}</h3>
            <p>Cursos Activos</p>
          </div>
        </div>

        <div class="kpi-card">
          <div class="kpi-icon">👥</div>
          <div class="kpi-content">
            <h3>{{ dashboardData.total_estudiantes }}</h3>
            <p>Estudiantes Totales</p>
          </div>
        </div>

        <div class="kpi-card">
          <div class="kpi-icon">✅</div>
          <div class="kpi-content">
            <h3>{{ dashboardData.tasa_aprobacion?.toFixed(1) }}%</h3>
            <p>Tasa de Aprobación</p>
          </div>
        </div>

        <div class="kpi-card">
          <div class="kpi-icon">📈</div>
          <div class="kpi-content">
            <h3>{{ dashboardData.promedio_general?.toFixed(2) }}</h3>
            <p>Promedio General</p>
          </div>
        </div>
      </div>

      <!-- Charts Row 1 -->
      <div class="charts-row">
        <!-- Course Performance Comparison -->
        <div class="chart-card">
          <h3 class="chart-title">📊 Comparación de Promedios por Curso</h3>
          <div class="chart-wrapper">
            <Bar :data="coursesChartData" :options="coursesChartOptions" />
          </div>
        </div>

        <!-- Pass/Fail Distribution -->
        <div class="chart-card">
          <h3 class="chart-title">🎯 Distribución Aprobados/Reprobados</h3>
          <div class="chart-wrapper">
            <Doughnut :data="passFailChartData" :options="passFailChartOptions" />
          </div>
        </div>
      </div>

      <!-- Charts Row 2 -->
      <div class="charts-row">
        <!-- Grade Distribution -->
        <div class="chart-card">
          <h3 class="chart-title">📉 Distribución de Calificaciones</h3>
          <div class="chart-wrapper">
            <Line :data="gradeDistributionData" :options="gradeDistributionOptions" />
          </div>
        </div>

        <!-- Students per Course -->
        <div class="chart-card">
          <h3 class="chart-title">👨‍🎓 Estudiantes por Curso</h3>
          <div class="chart-wrapper">
            <Bar :data="studentsPerCourseData" :options="studentsPerCourseOptions" />
          </div>
        </div>
      </div>

      <!-- Performance Lists -->
      <div class="performance-section">
        <!-- Top Performers -->
        <div class="performance-card">
          <h3 class="section-title">🏆 Mejores Estudiantes</h3>
          <div class="student-list">
            <div v-for="student in dashboardData.top_estudiantes" :key="student.id" class="student-item top">
              <span class="student-name">{{ student.nombre }}</span>
              <span class="student-score">{{ student.promedio?.toFixed(2) }}</span>
            </div>
          </div>
        </div>

        <!-- At-Risk Students -->
        <div class="performance-card">
          <h3 class="section-title">⚠️ Estudiantes en Riesgo</h3>
          <div class="student-list">
            <div v-for="student in dashboardData.estudiantes_riesgo" :key="student.id" class="student-item risk">
              <span class="student-name">{{ student.nombre }}</span>
              <span class="student-score">{{ student.promedio?.toFixed(2) }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Course Details Table -->
      <div class="table-card">
        <h3 class="section-title">📋 Detalle por Curso</h3>
        <table class="data-table">
          <thead>
            <tr>
              <th>Curso</th>
              <th>Estudiantes</th>
              <th>Promedio</th>
              <th>Aprobados</th>
              <th>Reprobados</th>
              <th>Tasa Aprobación</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="curso in dashboardData.cursos_detalle" :key="curso.id">
              <td><strong>{{ curso.nombre }}</strong></td>
              <td>{{ curso.total_estudiantes }}</td>
              <td>{{ curso.promedio?.toFixed(2) }}</td>
              <td class="approved">{{ curso.aprobados }}</td>
              <td class="failed">{{ curso.reprobados }}</td>
              <td>
                <span :class="['badge', curso.tasa_aprobacion >= 70 ? 'badge-success' : 'badge-warning']">
                  {{ curso.tasa_aprobacion?.toFixed(1) }}%
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { Bar, Doughnut, Line } from 'vue-chartjs'
import { Chart as ChartJS, CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend, ArcElement, PointElement, LineElement } from 'chart.js'
import { supabase } from '../supabase'

// Register Chart.js components
ChartJS.register(CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend, ArcElement, PointElement, LineElement)

// API Configuration
const REPORTS_API = import.meta.env.VITE_REPORTS_API || 'http://localhost:5014'

// State
const loading = ref(true)
const error = ref('')
const dashboardData = ref({
  total_cursos: 0,
  total_estudiantes: 0,
  tasa_aprobacion: 0,
  promedio_general: 0,
  cursos_detalle: [],
  top_estudiantes: [],
  estudiantes_riesgo: []
})

// Get user email
async function getUserEmail() {
  const { data: { user } } = await supabase.auth.getUser()
  return user?.email
}

// Fetch dashboard data
async function fetchDashboardData() {
  loading.value = true
  error.value = ''

  try {
    const email = await getUserEmail()
    if (!email) throw new Error('No se pudo obtener el email del usuario')

    const response = await fetch(`${REPORTS_API}/api/reports/teacher-dashboard`, {
      method: 'GET',
      headers: {
        'X-User-Email': email
      }
    })

    if (!response.ok) {
      const errorData = await response.json()
      throw new Error(errorData.detail || 'Error al cargar datos del dashboard')
    }

    dashboardData.value = await response.json()
  } catch (err) {
    console.error('Error fetching dashboard data:', err)
    error.value = err.message
  } finally {
    loading.value = false
  }
}

// Course Performance Chart
const coursesChartData = computed(() => ({
  labels: dashboardData.value.cursos_detalle?.map(c => c.nombre) || [],
  datasets: [{
    label: 'Promedio del Curso',
    data: dashboardData.value.cursos_detalle?.map(c => c.promedio) || [],
    backgroundColor: [
      'rgba(52, 152, 219, 0.7)',
      'rgba(46, 204, 113, 0.7)',
      'rgba(155, 89, 182, 0.7)',
      'rgba(241, 196, 15, 0.7)',
      'rgba(230, 126, 34, 0.7)',
    ],
    borderColor: [
      'rgba(52, 152, 219, 1)',
      'rgba(46, 204, 113, 1)',
      'rgba(155, 89, 182, 1)',
      'rgba(241, 196, 15, 1)',
      'rgba(230, 126, 34, 1)',
    ],
    borderWidth: 2
  }]
}))

const coursesChartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false },
    title: { display: false }
  },
  scales: {
    y: {
      beginAtZero: true,
      max: 100
    }
  }
}

// Pass/Fail Chart
const passFailChartData = computed(() => {
  const totalAprobados = dashboardData.value.cursos_detalle?.reduce((sum, c) => sum + c.aprobados, 0) || 0
  const totalReprobados = dashboardData.value.cursos_detalle?.reduce((sum, c) => sum + c.reprobados, 0) || 0
  
  return {
    labels: ['Aprobados', 'Reprobados'],
    datasets: [{
      data: [totalAprobados, totalReprobados],
      backgroundColor: [
        'rgba(46, 204, 113, 0.8)',
        'rgba(231, 76, 60, 0.8)'
      ],
      borderColor: [
        'rgba(46, 204, 113, 1)',
        'rgba(231, 76, 60, 1)'
      ],
      borderWidth: 2
    }]
  }
})

const passFailChartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: 'bottom'
    }
  }
}

// Grade Distribution Chart
const gradeDistributionData = computed(() => {
  const ranges = ['0-50', '51-60', '61-70', '71-80', '81-90', '91-100']
  const counts = [0, 0, 0, 0, 0, 0]

  dashboardData.value.cursos_detalle?.forEach(curso => {
    // This is simplified - in real implementation, you'd get individual grades
    const avg = curso.promedio
    if (avg <= 50) counts[0]++
    else if (avg <= 60) counts[1]++
    else if (avg <= 70) counts[2]++
    else if (avg <= 80) counts[3]++
    else if (avg <= 90) counts[4]++
    else counts[5]++
  })

  return {
    labels: ranges,
    datasets: [{
      label: 'Número de Estudiantes',
      data: counts,
      borderColor: 'rgba(52, 152, 219, 1)',
      backgroundColor: 'rgba(52, 152, 219, 0.2)',
      tension: 0.4,
      fill: true
    }]
  }
})

const gradeDistributionOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false }
  },
  scales: {
    y: {
      beginAtZero: true,
      ticks: { stepSize: 1 }
    }
  }
}

// Students per Course Chart
const studentsPerCourseData = computed(() => ({
  labels: dashboardData.value.cursos_detalle?.map(c => c.nombre) || [],
  datasets: [{
    label: 'Estudiantes',
    data: dashboardData.value.cursos_detalle?.map(c => c.total_estudiantes) || [],
    backgroundColor: 'rgba(155, 89, 182, 0.7)',
    borderColor: 'rgba(155, 89, 182, 1)',
    borderWidth: 2
  }]
}))

const studentsPerCourseOptions = {
  responsive: true,
  maintainAspectRatio: false,
  indexAxis: 'y',
  plugins: {
    legend: { display: false }
  },
  scales: {
    x: {
      beginAtZero: true,
      ticks: { stepSize: 1 }
    }
  }
}

onMounted(() => {
  fetchDashboardData()
})
</script>

<style scoped>
.teacher-dashboard {
  padding: 2rem;
  max-width: 1400px;
  margin: 0 auto;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  min-height: 100vh;
}

.dashboard-title {
  color: white;
  font-size: 2.5rem;
  margin-bottom: 2rem;
  text-align: center;
  text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.2);
}

.loading {
  text-align: center;
  color: white;
  font-size: 1.2rem;
  padding: 3rem;
}

.spinner {
  border: 4px solid rgba(255, 255, 255, 0.3);
  border-top: 4px solid white;
  border-radius: 50%;
  width: 50px;
  height: 50px;
  animation: spin 1s linear infinite;
  margin: 0 auto 1rem;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.error-message {
  background: rgba(231, 76, 60, 0.9);
  color: white;
  padding: 1rem;
  border-radius: 8px;
  text-align: center;
}

.dashboard-content {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

/* KPI Cards */
.kpi-section {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
}

.kpi-card {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  display: flex;
  align-items: center;
  gap: 1rem;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  transition: transform 0.3s ease, box-shadow 0.3s ease;
}

.kpi-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 8px 12px rgba(0, 0, 0, 0.2);
}

.kpi-icon {
  font-size: 3rem;
}

.kpi-content h3 {
  font-size: 2rem;
  margin: 0;
  color: #2c3e50;
}

.kpi-content p {
  margin: 0;
  color: #7f8c8d;
  font-size: 0.9rem;
}

/* Charts */
.charts-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(500px, 1fr));
  gap: 1.5rem;
}

.chart-card {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  display: flex;
  flex-direction: column;
}

.chart-title {
  margin: 0 0 1rem 0;
  color: #2c3e50;
  font-size: 1.2rem;
  flex-shrink: 0;
}

.chart-wrapper {
  flex: 1;
  min-height: 0;
  position: relative;
  height: 320px;
}

/* Performance Lists */
.performance-section {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
  gap: 1.5rem;
}

.performance-card {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}

.section-title {
  margin: 0 0 1rem 0;
  color: #2c3e50;
  font-size: 1.2rem;
}

.student-list {
  display: flex;
  flex-direction: column;
  gap: 0.8rem;
}

.student-item {
  display: flex;
  justify-content: space-between;
  padding: 0.8rem;
  border-radius: 8px;
  transition: transform 0.2s ease;
}

.student-item:hover {
  transform: translateX(5px);
}

.student-item.top {
  background: rgba(46, 204, 113, 0.1);
  border-left: 4px solid #2ecc71;
}

.student-item.risk {
  background: rgba(231, 76, 60, 0.1);
  border-left: 4px solid #e74c3c;
}

.student-name {
  font-weight: 600;
  color: #2c3e50;
}

.student-score {
  font-weight: bold;
  color: #3498db;
}

/* Table */
.table-card {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  overflow-x: auto;
}

.data-table {
  width: 100%;
  border-collapse: collapse;
}

.data-table th {
  background: #3498db;
  color: white;
  padding: 1rem;
  text-align: left;
  font-weight: 600;
}

.data-table td {
  padding: 1rem;
  border-bottom: 1px solid #ecf0f1;
}

.data-table tbody tr:hover {
  background: #f8f9fa;
}

.data-table .approved {
  color: #2ecc71;
  font-weight: 600;
}

.data-table .failed {
  color: #e74c3c;
  font-weight: 600;
}

.badge {
  padding: 0.3rem 0.8rem;
  border-radius: 20px;
  font-size: 0.9rem;
  font-weight: 600;
}

.badge-success {
  background: rgba(46, 204, 113, 0.2);
  color: #27ae60;
}

.badge-warning {
  background: rgba(230, 126, 34, 0.2);
  color: #d68910;
}

@media (max-width: 768px) {
  .teacher-dashboard {
    padding: 1rem;
  }

  .charts-row {
    grid-template-columns: 1fr;
  }

  .performance-section {
    grid-template-columns: 1fr;
  }

  .chart-wrapper {
    height: 280px;
  }
}
</style>
