<template>
  <div class="director-dashboard">
    <h1 class="dashboard-title">🎯 Panel de Control - Director</h1>

    <!-- Loading State -->
    <div v-if="loading" class="loading">
      <div class="spinner"></div>
      <p>Cargando datos del sistema...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="error-message">
      {{ error }}
    </div>

    <!-- Dashboard Content -->
    <div v-else class="dashboard-content">
      <!-- Main KPI Cards -->
      <div class="kpi-grid">
        <div class="kpi-card primary">
          <div class="kpi-icon">📚</div>
          <div class="kpi-content">
            <h3>{{ dashboardData.total_cursos }}</h3>
            <p>Cursos Totales</p>
            <span class="kpi-trend positive">+{{ dashboardData.cursos_activos }} activos</span>
          </div>
        </div>

        <div class="kpi-card success">
          <div class="kpi-icon">👥</div>
          <div class="kpi-content">
            <h3>{{ dashboardData.total_estudiantes }}</h3>
            <p>Estudiantes Matriculados</p>
            <span class="kpi-trend">En {{ dashboardData.total_cursos }} cursos</span>
          </div>
        </div>

        <div class="kpi-card warning">
          <div class="kpi-icon">👨‍🏫</div>
          <div class="kpi-content">
            <h3>{{ dashboardData.total_profesores }}</h3>
            <p>Profesores Activos</p>
            <span class="kpi-trend">Sistema completo</span>
          </div>
        </div>

        <div class="kpi-card info">
          <div class="kpi-icon">📊</div>
          <div class="kpi-content">
            <h3>{{ dashboardData.promedio_sistema?.toFixed(2) }}</h3>
            <p>Promedio del Sistema</p>
            <span class="kpi-trend" :class="dashboardData.promedio_sistema >= 70 ? 'positive' : 'negative'">
              {{ dashboardData.promedio_sistema >= 70 ? '✓ Saludable' : '⚠ Revisar' }}
            </span>
          </div>
        </div>
      </div>

      <!-- Performance Overview -->
      <div class="overview-cards">
        <div class="overview-card">
          <h3>✅ Tasa de Aprobación General</h3>
          <div class="big-number">{{ dashboardData.tasa_aprobacion_general?.toFixed(1) }}%</div>
          <div class="progress-bar">
            <div class="progress-fill" :style="{ width: dashboardData.tasa_aprobacion_general + '%' }"></div>
          </div>
        </div>

        <div class="overview-card">
          <h3>📈 Estudiantes Aprobados</h3>
          <div class="big-number success-color">{{ dashboardData.total_aprobados }}</div>
          <p class="small-text">de {{ dashboardData.total_estudiantes }} estudiantes</p>
        </div>

        <div class="overview-card">
          <h3>⚠️ Estudiantes Reprobados</h3>
          <div class="big-number warning-color">{{ dashboardData.total_reprobados }}</div>
          <p class="small-text">Requieren seguimiento</p>
        </div>
      </div>

      <!-- Charts Row 1 -->
      <div class="charts-row">
        <!-- Course Performance Comparison -->
        <div class="chart-card large">
          <h3 class="chart-title">📊 Rendimiento por Curso</h3>
          <div class="chart-wrapper">
            <Bar :data="coursePerformanceData" :options="coursePerformanceOptions" />
          </div>
        </div>

        <!-- Pass Rate by Course -->
        <div class="chart-card">
          <h3 class="chart-title">🎯 Tasa de Aprobación por Curso</h3>
          <div class="chart-wrapper">
            <Doughnut :data="passRateData" :options="passRateOptions" />
          </div>
        </div>
      </div>

      <!-- Charts Row 2 -->
      <div class="charts-row">
        <!-- Grade Distribution -->
        <div class="chart-card">
          <h3 class="chart-title">📉 Distribución General de Calificaciones</h3>
          <div class="chart-wrapper">
            <Line :data="gradeDistributionData" :options="gradeDistributionOptions" />
          </div>
        </div>

        <!-- Students per Course -->
        <div class="chart-card">
          <h3 class="chart-title">👨‍🎓 Matrícula por Curso</h3>
          <div class="chart-wrapper">
            <Bar :data="enrollmentData" :options="enrollmentOptions" />
          </div>
        </div>
      </div>

      <!-- Alerts and Insights -->
      <div class="insights-section">
        <div class="insight-card alert" v-if="dashboardData.cursos_riesgo?.length > 0">
          <h3>🚨 Cursos que Requieren Atención</h3>
          <div class="alert-list">
            <div v-for="curso in dashboardData.cursos_riesgo" :key="curso.id" class="alert-item">
              <span class="alert-icon">⚠️</span>
              <div class="alert-content">
                <strong>{{ curso.nombre }}</strong>
                <p>Tasa de aprobación: {{ curso.tasa_aprobacion?.toFixed(1) }}% (Profesor: {{ curso.profesor }})</p>
              </div>
            </div>
          </div>
        </div>

        <div class="insight-card success" v-if="dashboardData.mejores_cursos?.length > 0">
          <h3>🏆 Cursos Destacados</h3>
          <div class="success-list">
            <div v-for="curso in dashboardData.mejores_cursos" :key="curso.id" class="success-item">
              <span class="success-icon">✨</span>
              <div class="success-content">
                <strong>{{ curso.nombre }}</strong>
                <p>Promedio: {{ curso.promedio?.toFixed(2) }} | Aprobación: {{ curso.tasa_aprobacion?.toFixed(1) }}%</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Detailed Table -->
      <div class="table-card">
        <h3 class="section-title">📋 Resumen Detallado por Curso</h3>
        <div class="table-controls">
          <input type="text" v-model="searchQuery" placeholder="Buscar curso..." class="search-input" />
        </div>
        <table class="data-table">
          <thead>
            <tr>
              <th>Curso</th>
              <th>Código</th>
              <th>Profesor</th>
              <th>Estudiantes</th>
              <th>Promedio</th>
              <th>Aprobados</th>
              <th>Reprobados</th>
              <th>Tasa Aprobación</th>
              <th>Estado</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="curso in filteredCursos" :key="curso.id">
              <td><strong>{{ curso.nombre }}</strong></td>
              <td>{{ curso.codigo }}</td>
              <td>{{ curso.profesor }}</td>
              <td>{{ curso.total_estudiantes }}</td>
              <td>{{ curso.promedio?.toFixed(2) }}</td>
              <td class="approved">{{ curso.aprobados }}</td>
              <td class="failed">{{ curso.reprobados }}</td>
              <td>{{ curso.tasa_aprobacion?.toFixed(1) }}%</td>
              <td>
                <span :class="['status-badge', getStatusClass(curso.tasa_aprobacion)]">
                  {{ getStatusText(curso.tasa_aprobacion) }}
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
const searchQuery = ref('')
const dashboardData = ref({
  total_cursos: 0,
  cursos_activos: 0,
  total_estudiantes: 0,
  total_profesores: 0,
  promedio_sistema: 0,
  tasa_aprobacion_general: 0,
  total_aprobados: 0,
  total_reprobados: 0,
  cursos_detalle: [],
  cursos_riesgo: [],
  mejores_cursos: []
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

    const response = await fetch(`${REPORTS_API}/api/reports/director-dashboard`, {
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

// Filtered courses for table
const filteredCursos = computed(() => {
  if (!searchQuery.value) return dashboardData.value.cursos_detalle || []
  
  const query = searchQuery.value.toLowerCase()
  return dashboardData.value.cursos_detalle?.filter(curso =>
    curso.nombre.toLowerCase().includes(query) ||
    curso.codigo.toLowerCase().includes(query) ||
    curso.profesor?.toLowerCase().includes(query)
  ) || []
})

// Course Performance Chart
const coursePerformanceData = computed(() => ({
  labels: dashboardData.value.cursos_detalle?.slice(0, 10).map(c => c.codigo) || [],
  datasets: [
    {
      label: 'Promedio',
      data: dashboardData.value.cursos_detalle?.slice(0, 10).map(c => c.promedio) || [],
      backgroundColor: 'rgba(52, 152, 219, 0.7)',
      borderColor: 'rgba(52, 152, 219, 1)',
      borderWidth: 2
    },
    {
      label: 'Tasa Aprobación',
      data: dashboardData.value.cursos_detalle?.slice(0, 10).map(c => c.tasa_aprobacion) || [],
      backgroundColor: 'rgba(46, 204, 113, 0.7)',
      borderColor: 'rgba(46, 204, 113, 1)',
      borderWidth: 2
    }
  ]
}))

const coursePerformanceOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { position: 'top' }
  },
  scales: {
    y: {
      beginAtZero: true,
      max: 100
    }
  }
}

// Pass Rate Doughnut
const passRateData = computed(() => ({
  labels: ['Aprobados', 'Reprobados'],
  datasets: [{
    data: [dashboardData.value.total_aprobados || 0, dashboardData.value.total_reprobados || 0],
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
}))

const passRateOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { position: 'bottom' }
  }
}

// Grade Distribution
const gradeDistributionData = computed(() => ({
  labels: ['0-50', '51-60', '61-70', '71-80', '81-90', '91-100'],
  datasets: [{
    label: 'Cantidad de Estudiantes',
    data: dashboardData.value.distribucion_notas || [0, 0, 0, 0, 0, 0],
    borderColor: 'rgba(155, 89, 182, 1)',
    backgroundColor: 'rgba(155, 89, 182, 0.2)',
    tension: 0.4,
    fill: true
  }]
}))

const gradeDistributionOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false }
  }
}

// Enrollment Chart
const enrollmentData = computed(() => ({
  labels: dashboardData.value.cursos_detalle?.map(c => c.codigo) || [],
  datasets: [{
    label: 'Estudiantes Matriculados',
    data: dashboardData.value.cursos_detalle?.map(c => c.total_estudiantes) || [],
    backgroundColor: 'rgba(230, 126, 34, 0.7)',
    borderColor: 'rgba(230, 126, 34, 1)',
    borderWidth: 2
  }]
}))

const enrollmentOptions = {
  responsive: true,
  maintainAspectRatio: false,
  indexAxis: 'y',
  plugins: {
    legend: { display: false }
  }
}

// Status helpers
function getStatusClass(tasa) {
  if (tasa >= 80) return 'excellent'
  if (tasa >= 70) return 'good'
  if (tasa >= 60) return 'acceptable'
  return 'critical'
}

function getStatusText(tasa) {
  if (tasa >= 80) return 'Excelente'
  if (tasa >= 70) return 'Bueno'
  if (tasa >= 60) return 'Aceptable'
  return 'Crítico'
}

onMounted(() => {
  fetchDashboardData()
})
</script>

<style scoped>
.director-dashboard {
  padding: 2rem;
  max-width: 1600px;
  margin: 0 auto;
  background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%);
  min-height: 100vh;
}

.dashboard-title {
  color: white;
  font-size: 2.8rem;
  margin-bottom: 2rem;
  text-align: center;
  text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.3);
}

.loading, .error-message {
  text-align: center;
  color: white;
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

.dashboard-content {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

/* KPI Cards */
.kpi-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 1.5rem;
}

.kpi-card {
  background: white;
  border-radius: 16px;
  padding: 1.5rem;
  display: flex;
  align-items: center;
  gap: 1.5rem;
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.15);
  transition: all 0.3s ease;
  position: relative;
  overflow: hidden;
}

.kpi-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  width: 4px;
  height: 100%;
}

.kpi-card.primary::before { background: #3498db; }
.kpi-card.success::before { background: #2ecc71; }
.kpi-card.warning::before { background: #f39c12; }
.kpi-card.info::before { background: #9b59b6; }

.kpi-card:hover {
  transform: translateY(-8px);
  box-shadow: 0 12px 24px rgba(0, 0, 0, 0.2);
}

.kpi-icon {
  font-size: 3.5rem;
}

.kpi-content h3 {
  font-size: 2.5rem;
  margin: 0;
  color: #2c3e50;
  font-weight: 700;
}

.kpi-content p {
  margin: 0.3rem 0;
  color: #7f8c8d;
  font-size: 1rem;
  font-weight: 500;
}

.kpi-trend {
  font-size: 0.85rem;
  color: #95a5a6;
}

.kpi-trend.positive { color: #27ae60; }
.kpi-trend.negative { color: #e74c3c; }

/* Overview Cards */
.overview-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 1.5rem;
}

.overview-card {
  background: white;
  border-radius: 16px;
  padding: 2rem;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
  text-align: center;
}

.overview-card h3 {
  margin: 0 0 1rem 0;
  color: #2c3e50;
  font-size: 1.1rem;
}

.big-number {
  font-size: 3.5rem;
  font-weight: 700;
  color: #3498db;
  margin: 1rem 0;
}

.success-color { color: #2ecc71; }
.warning-color { color: #e67e22; }

.small-text {
  color: #95a5a6;
  font-size: 0.9rem;
}

.progress-bar {
  width: 100%;
  height: 12px;
  background: #ecf0f1;
  border-radius: 6px;
  overflow: hidden;
  margin-top: 1rem;
}

.progress-fill {
  height: 100%;
  background: linear-gradient(90deg, #3498db, #2ecc71);
  transition: width 0.5s ease;
}

/* Charts */
.charts-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(500px, 1fr));
  gap: 1.5rem;
}

.chart-card {
  background: white;
  border-radius: 16px;
  padding: 1.5rem;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
  display: flex;
  flex-direction: column;
}

.chart-card.large {
  grid-column: span 2;
}

.chart-title {
  margin: 0 0 1rem 0;
  color: #2c3e50;
  font-size: 1.2rem;
  font-weight: 600;
  flex-shrink: 0;
}

.chart-wrapper {
  flex: 1;
  min-height: 0;
  position: relative;
  height: 320px;
}

/* Insights */
.insights-section {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(500px, 1fr));
  gap: 1.5rem;
}

.insight-card {
  background: white;
  border-radius: 16px;
  padding: 1.5rem;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
}

.insight-card h3 {
  margin: 0 0 1rem 0;
  font-size: 1.2rem;
}

.alert-list, .success-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.alert-item, .success-item {
  display: flex;
  gap: 1rem;
  padding: 1rem;
  border-radius: 8px;
  background: rgba(231, 76, 60, 0.05);
}

.success-item {
  background: rgba(46, 204, 113, 0.05);
}

.alert-icon, .success-icon {
  font-size: 1.5rem;
}

.alert-content strong, .success-content strong {
  display: block;
  color: #2c3e50;
  margin-bottom: 0.3rem;
}

.alert-content p, .success-content p {
  margin: 0;
  color: #7f8c8d;
  font-size: 0.9rem;
}

/* Table */
.table-card {
  background: white;
  border-radius: 16px;
  padding: 1.5rem;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
}

.section-title {
  margin: 0 0 1rem 0;
  color: #2c3e50;
  font-size: 1.3rem;
  font-weight: 600;
}

.table-controls {
  margin-bottom: 1rem;
}

.search-input {
  width: 100%;
  max-width: 400px;
  padding: 0.8rem 1rem;
  border: 2px solid #ecf0f1;
  border-radius: 8px;
  font-size: 1rem;
  transition: border-color 0.3s ease;
}

.search-input:focus {
  outline: none;
  border-color: #3498db;
}

.data-table {
  width: 100%;
  border-collapse: collapse;
  overflow-x: auto;
  display: block;
}

.data-table thead, .data-table tbody, .data-table tr {
  display: table;
  width: 100%;
  table-layout: fixed;
}

.data-table th {
  background: #3498db;
  color: white;
  padding: 1rem;
  text-align: left;
  font-weight: 600;
  position: sticky;
  top: 0;
}

.data-table td {
  padding: 1rem;
  border-bottom: 1px solid #ecf0f1;
}

.data-table tbody {
  display: block;
  max-height: 500px;
  overflow-y: auto;
}

.data-table tbody tr:hover {
  background: #f8f9fa;
}

.approved { color: #2ecc71; font-weight: 600; }
.failed { color: #e74c3c; font-weight: 600; }

.status-badge {
  padding: 0.4rem 0.9rem;
  border-radius: 20px;
  font-size: 0.85rem;
  font-weight: 600;
}

.status-badge.excellent { background: rgba(46, 204, 113, 0.2); color: #27ae60; }
.status-badge.good { background: rgba(52, 152, 219, 0.2); color: #2980b9; }
.status-badge.acceptable { background: rgba(241, 196, 15, 0.2); color: #f39c12; }
.status-badge.critical { background: rgba(231, 76, 60, 0.2); color: #c0392b; }

@media (max-width: 1200px) {
  .chart-card.large {
    grid-column: span 1;
  }
}

@media (max-width: 768px) {
  .director-dashboard {
    padding: 1rem;
  }

  .charts-row, .insights-section {
    grid-template-columns: 1fr;
  }

  .chart-wrapper {
    height: 280px;
  }
}
</style>
