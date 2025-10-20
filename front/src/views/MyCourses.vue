<template>
  <transition name="fade">
    <section class="wrap">
      <h1>Mis Cursos</h1>
      <p class="subtitle">Cursos en los que estás inscrito</p>

      <div v-if="loading" class="loading">Cargando tus cursos...</div>
      <div v-else-if="error" class="error">{{ error }}</div>
      
      <div v-else-if="cursos.length === 0" class="empty">
        <p>No estás inscrito en ningún curso aún.</p>
        <router-link to="/courses" class="btn-primary">Ver cursos disponibles</router-link>
      </div>

      <div v-else class="grid">
        <article v-for="curso in cursos" :key="curso.id" class="card">
          <div class="card-header">
            <h3>{{ curso.nombre }}</h3>
            <span class="badge">{{ curso.codigo }}</span>
          </div>
          <p class="descripcion">{{ curso.descripcion || 'Sin descripción' }}</p>
          <div class="info">
            <span><strong>Créditos:</strong> {{ curso.creditos }}</span>
            <span v-if="curso.horario"><strong>Horario:</strong> {{ curso.horario }}</span>
          </div>
          <div class="actions">
            <button @click="viewAttendance(curso)" class="btn-outline">
              📊 Ver Asistencias
            </button>
          </div>
        </article>
      </div>

      <!-- Modal de asistencias POR CURSO -->
      <div v-if="attendanceModal.show" class="modal" @click.self="closeAttendanceModal">
        <div class="modal-content">
          <div class="modal-header">
            <div class="course-info">
              <h2>{{ attendanceModal.cursoNombre }}</h2>
              <span class="course-badge">{{ attendanceModal.cursoCodigo }}</span>
            </div>
            <button @click="closeAttendanceModal" class="close-btn">×</button>
          </div>
          
          <div v-if="attendanceModal.loading" class="loading-modal">
            <div class="spinner"></div>
            Cargando asistencias...
          </div>
          <div v-else-if="attendanceModal.error" class="error-box">{{ attendanceModal.error }}</div>
          <div v-else-if="attendanceModal.records.length === 0" class="empty-modal">
            No hay registros de asistencia en este curso
          </div>
          
          <div v-else>
            <!-- Resumen de asistencias -->
            <div class="summary">
              <div class="summary-item presente">
                <span class="count">{{ getCountByStatus('presente') }}</span>
                <span class="label">Presente</span>
              </div>
              <div class="summary-item ausente">
                <span class="count">{{ getCountByStatus('ausente') }}</span>
                <span class="label">Ausente</span>
              </div>
              <div class="summary-item tardanza">
                <span class="count">{{ getCountByStatus('tardanza') }}</span>
                <span class="label">Tardanza</span>
              </div>
              <div class="summary-item total">
                <span class="count">{{ attendanceModal.records.length }}</span>
                <span class="label">Total</span>
              </div>
            </div>

            <!-- Tabla de registros -->
            <div class="table-wrapper">
              <table class="attendance-table">
                <thead>
                  <tr>
                    <th>Fecha</th>
                    <th>Estado</th>
                    <th>Observaciones</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="record in attendanceModal.records" :key="record.id">
                    <td class="date-cell">{{ formatDate(record.fecha) }}</td>
                    <td>
                      <span :class="['status-badge', record.estado]">
                        {{ getStatusLabel(record.estado) }}
                      </span>
                    </td>
                    <td class="obs-cell">{{ record.observaciones || '-' }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </section>
  </transition>
</template>

<script setup>
import { ref, onMounted, reactive } from 'vue'
import { supabase } from '../supabase'
import { useRouter } from 'vue-router'

const router = useRouter()
const cursos = ref([])
const loading = ref(true)
const error = ref(null)
const userInfo = ref(null)

const attendanceModal = reactive({
  show: false,
  loading: false,
  error: null,
  records: [],
  cursoId: null,
  cursoNombre: '',
  cursoCodigo: ''
})

// 🔹 URLs dinámicas
const getCoursesAPI = () => window.location.hostname === 'localhost' ? 'http://localhost:5008' : window.location.origin
const getAttendanceAPI = () => window.location.hostname === 'localhost' ? 'http://localhost:5004' : window.location.origin
const getAuthAPI = () => window.location.hostname === 'localhost' ? 'http://localhost:5002' : window.location.origin

async function fetchMyCourses() {
  try {
    loading.value = true
    error.value = null
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) {
      error.value = 'No estás autenticado'
      return
    }

    const response = await fetch(`${getCoursesAPI()}/api/courses/my-courses`, {
      headers: { 'Authorization': `Bearer ${session.access_token}` },
      credentials: 'include'
    })

    if (response.ok) {
      const data = await response.json()
      cursos.value = data.cursos || []
      userInfo.value = { usuario: data.usuario, rol: data.rol }
    } else {
      const errorData = await response.json()
      error.value = errorData.detail || 'Error al cargar tus cursos'
    }
  } catch (e) {
    console.error('Error fetching my courses:', e)
    error.value = 'Error de conexión con el servidor'
  } finally {
    loading.value = false
  }
}

async function viewAttendance(curso) {
  attendanceModal.show = true
  attendanceModal.loading = true
  attendanceModal.error = null
  attendanceModal.records = []
  attendanceModal.cursoId = curso.id
  attendanceModal.cursoNombre = curso.nombre
  attendanceModal.cursoCodigo = curso.codigo

  try {
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const profileResponse = await fetch(`${getAuthAPI()}/api/auth/user-profile`, {
      headers: { 'Authorization': `Bearer ${session.access_token}` },
      credentials: 'include'
    })

    if (!profileResponse.ok) {
      attendanceModal.error = 'Error al obtener perfil de usuario'
      return
    }

    const profile = await profileResponse.json()
    
    const tenant = getTenantFromEmail(profile.email)
    if (!tenant) {
      attendanceModal.error = 'Tenant no identificado'
      return
    }

    const usersResponse = await fetch(`${getAuthAPI()}/api/usuarios/${tenant}`, {
      credentials: 'include'
    })
    
    if (!usersResponse.ok) {
      attendanceModal.error = 'Error al obtener usuarios'
      return
    }

    const usersData = await usersResponse.json()
    const currentUser = usersData.usuarios.find(u => u.email === profile.email)
    
    if (!currentUser) {
      attendanceModal.error = 'Usuario no encontrado'
      return
    }

    const response = await fetch(
      `${getAttendanceAPI()}/api/attendance/student/${currentUser.id}?curso_id=${curso.id}`,
      {
        headers: { 'Authorization': `Bearer ${session.access_token}` },
        credentials: 'include'
      }
    )

    if (response.ok) {
      const data = await response.json()
      attendanceModal.records = data.asistencias || []
    } else {
      attendanceModal.error = 'Error al cargar asistencias'
    }
  } catch (e) {
    console.error('Error fetching attendance:', e)
    attendanceModal.error = 'Error de conexión'
  } finally {
    attendanceModal.loading = false
  }
}

function goToTareas(curso) {
  router.push(`/courses/${curso.id}/assignments`)
}

function getTenantFromEmail(email) {
  if (email.endsWith('@ucb.edu.bo')) return 'ucb.edu.bo'
  if (email.endsWith('@upb.edu.bo')) return 'upb.edu.bo'
  if (email.endsWith('@gmail.com')) return 'gmail.com'
  return null
}

function closeAttendanceModal() {
  attendanceModal.show = false
  attendanceModal.records = []
  attendanceModal.error = null
  attendanceModal.cursoId = null
}

function formatDate(dateString) {
  return new Date(dateString).toLocaleDateString('es-ES', {
    weekday: 'short',
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}

function getStatusLabel(estado) {
  const labels = {
    presente: '✓ Presente',
    ausente: '✗ Ausente',
    tardanza: '⏰ Tardanza'
  }
  return labels[estado] || estado
}

function getCountByStatus(status) {
  return attendanceModal.records.filter(r => r.estado === status).length
}

onMounted(() => {
  fetchMyCourses()
})
</script>

<style scoped>
.wrap { max-width: 1100px; margin: 2rem auto; padding: 0 1rem; animation: slideIn .7s; }
.subtitle { color: #666; margin-bottom: 1.5rem; }
.loading, .error, .empty { text-align: center; padding: 2rem; }
.error { color: #ef4444; }
.empty p { margin-bottom: 1rem; color: #666; }
.grid { display: grid; gap: 1rem; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); }
.card { background: #fff; border-radius: 12px; padding: 1.25rem; box-shadow: 0 2px 8px rgba(0,0,0,.06); transition: transform .18s ease, box-shadow .2s ease; }
.card:hover { transform: translateY(-4px); box-shadow: 0 10px 24px rgba(0,0,0,.08); }
.card-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: .75rem; }
.card h3 { color: #2a4dd0; margin: 0; }
.badge { background: #eef2ff; color: #2a4dd0; padding: .25rem .5rem; border-radius: 6px; font-size: .85rem; font-weight: 600; }
.descripcion { color: #666; margin: .5rem 0; }
.info { display: flex; flex-direction: column; gap: .25rem; font-size: .9rem; color: #555; margin-bottom: .75rem; }
.actions { display: flex; gap: .5rem; }
.btn-primary { background: #2a4dd0; color: #fff; border: none; padding: .6rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; text-decoration: none; display: inline-block; transition: transform .15s, box-shadow .2s; }
.btn-primary:hover { transform: translateY(-1px); box-shadow: 0 6px 20px rgba(42,77,208,.2); }
.btn-outline { background: #fff; color: #2a4dd0; border: 2px solid #2a4dd0; padding: .5rem .75rem; border-radius: 8px; font-weight: 600; cursor: pointer; font-size: .9rem; transition: background .2s, color .2s; }
.btn-outline:hover { background: #eef2ff; }

/* Modal */
.modal { position: fixed; inset: 0; background: rgba(0,0,0,.5); display: grid; place-items: center; z-index: 100; animation: fadeIn .3s; }
.modal-content { background: #fff; border-radius: 16px; padding: 1.5rem; max-width: 900px; width: 90%; max-height: 85vh; overflow-y: auto; animation: scaleIn .3s; }
.modal-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 1.5rem; }
.course-info { flex: 1; }
.course-info h2 { margin: 0 0 .25rem; color: #2a4dd0; font-size: 1.4rem; }
.course-badge { background: #eef2ff; color: #2a4dd0; padding: .35rem .6rem; border-radius: 6px; font-size: .9rem; font-weight: 600; }
.close-btn { background: none; border: none; font-size: 2rem; color: #666; cursor: pointer; padding: 0; line-height: 1; transition: color .2s; }
.close-btn:hover { color: #2a4dd0; }
.error-box { background: #fee; color: #c00; padding: .75rem; border-radius: 8px; margin-bottom: 1rem; border: 1px solid #fcc; }
.empty-modal { text-align: center; padding: 3rem 2rem; color: #666; }
.loading-modal { text-align: center; padding: 3rem; display: flex; align-items: center; justify-content: center; gap: 1rem; color: #666; }

.spinner { border: 3px solid #f3f3f3; border-top: 3px solid #2a4dd0; border-radius: 50%; width: 28px; height: 28px; animation: spin 1s linear infinite; }
@keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }

/* Summary */
.summary { display: grid; grid-template-columns: repeat(4, 1fr); gap: 1rem; margin-bottom: 1.5rem; }
.summary-item { background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%); border-radius: 10px; padding: 1rem; text-align: center; }
.summary-item.presente { background: linear-gradient(135deg, #d1fae5 0%, #a7f3d0 100%); }
.summary-item.ausente { background: linear-gradient(135deg, #fee2e2 0%, #fecaca 100%); }
.summary-item.tardanza { background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%); }
.summary-item .count { display: block; font-size: 1.8rem; font-weight: 700; color: #1e293b; }
.summary-item .label { display: block; font-size: .85rem; color: #64748b; margin-top: .25rem; font-weight: 600; text-transform: uppercase; }

/* Table */
.table-wrapper { overflow-x: auto; }
.attendance-table { width: 100%; border-collapse: collapse; }
.attendance-table thead { background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%); }
.attendance-table th { text-align: left; padding: .75rem; font-weight: 600; color: #475569; border-bottom: 2px solid #e2e8f0; font-size: .85rem; text-transform: uppercase; }
.attendance-table td { padding: .75rem; border-bottom: 1px solid #f1f5f9; }
.attendance-table tbody tr:hover { background: #f8fafc; }

.date-cell { font-weight: 600; color: #1e293b; white-space: nowrap; }
.status-badge { padding: .35rem .6rem; border-radius: 6px; font-size: .85rem; font-weight: 600; }
.status-badge.presente { background: #d1fae5; color: #065f46; }
.status-badge.ausente { background: #fee2e2; color: #991b1b; }
.status-badge.tardanza { background: #fef3c7; color: #92400e; }
.obs-cell { color: #64748b; font-size: .9rem; }

@keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
@keyframes scaleIn { from { transform: scale(.95); opacity: 0; } to { transform: scale(1); opacity: 1; } }

.fade-enter-active, .fade-leave-active { transition: opacity .4s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
@keyframes slideIn { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }

@media (max-width: 768px) {
  .summary { grid-template-columns: repeat(2, 1fr); }
  .modal-content { width: 95%; padding: 1rem; }
  .course-info h2 { font-size: 1.1rem; }
}
</style>
