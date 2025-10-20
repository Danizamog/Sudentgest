<template>
  <transition name="fade">
    <section class="wrap">
      <h1>Mis Excusas</h1>
      <p class="subtitle">Excusas que has creado</p>

      <div v-if="loading" class="loading">
        <div class="spinner"></div>
        Cargando tus excusas...
      </div>

      <div v-else-if="error" class="error-box">{{ error }}</div>

      <div v-else-if="excuses.length === 0" class="empty">
        📋 No has creado ninguna excusa aún
      </div>

      <div v-else class="table-container">
        <!-- Filtros -->
        <div class="filters">
          <select v-model="filterStatus" class="filter-select">
            <option value="">Todos los estados</option>
            <option value="pendiente">Pendientes</option>
            <option value="aprobada">Aprobadas</option>
            <option value="rechazada">Rechazadas</option>
          </select>
          <span class="count">{{ filteredExcuses.length }} excusa(s)</span>
        </div>

        <!-- Tabla -->
        <div class="table-wrapper">
          <table class="excuses-table">
            <thead>
              <tr>
                <th>Fecha Excusa</th>
                <th>Estudiante</th>
                <th>Motivo</th>
                <th>Estado</th>
                <th>Fecha Creación</th>
                <th v-if="userRole === 'Profesor'">Acciones</th>
              </tr>
            </thead>
            <tbody>
                <tr v-for="excuse in filteredExcuses" :key="excuse.id">
                <td class="date-cell">{{ formatDate(excuse.fecha_inicio) }} | {{ formatDate(excuse.fecha_fin) }}</td>
                <td class="student-cell">{{ excuse.estudiante_nombre || 'Cargando...' }}</td>
                <td class="motivo-cell">{{ excuse.motivo }}</td>
                <td>
                    <span :class="['status-badge', excuse.estado]">
                    {{ getStatusLabel(excuse.estado) }}
                  </span>
                </td>
                <td class="created-cell">{{ formatDateTime(excuse.created_at) }}</td>
                <td v-if="userRole === 'Profesor'" class="actions-cell">
                  <button 
                    v-if="excuse.estado === 'pendiente'" 
                    @click="deleteExcuse(excuse.id)"
                    class="btn-delete"
                    title="Eliminar excusa"
                  >
                    🗑️
                  </button>
                  <span v-else class="no-action">-</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </section>
  </transition>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { supabase } from '../supabase'

const excuses = ref([])
const loading = ref(true)
const error = ref(null)
const filterStatus = ref('')
const userRole = ref('')

const getAttendanceAPI = () => window.location.hostname === 'localhost' ? 'http://localhost:5004' : window.location.origin
const getAuthAPI = () => window.location.hostname === 'localhost' ? 'http://localhost:5002' : window.location.origin

const filteredExcuses = computed(() => {
  if (!filterStatus.value) return excuses.value
  return excuses.value.filter(e => e.estado === filterStatus.value)
})

function getTenantFromEmail(email) {
  if (email.endsWith('@ucb.edu.bo')) return 'ucb.edu.bo'
  if (email.endsWith('@upb.edu.bo')) return 'upb.edu.bo'
  if (email.endsWith('@gmail.com')) return 'gmail.com'
  return null
}

async function loadMyExcuses() {
  try {
    loading.value = true
    error.value = null

    const { data: { session } } = await supabase.auth.getSession()
    if (!session) {
      error.value = 'No estás autenticado'
      return
    }

    // Obtener perfil del usuario
    const profileResponse = await fetch(`${getAuthAPI()}/api/auth/user-profile`, {
      headers: { 'Authorization': `Bearer ${session.access_token}` },
      credentials: 'include'
    })

    if (!profileResponse.ok) {
      error.value = 'Error al obtener perfil'
      return
    }

    const profile = await profileResponse.json()
    userRole.value = profile.rol

    // Obtener excusas creadas por el usuario actual
    const response = await fetch(`${getAttendanceAPI()}/api/attendance/excuses/my`, {
      headers: { 'Authorization': `Bearer ${session.access_token}` },
      credentials: 'include'
    })

    if (!response.ok) {
      const errorData = await response.json()
      error.value = errorData.detail || 'Error al cargar excusas'
      return
    }

    const data = await response.json()
    let excusasData = data.excusas || []

    // Obtener tenant para buscar nombres de estudiantes
    const tenant = getTenantFromEmail(profile.email)
    if (!tenant) {
      error.value = 'Tenant no identificado'
      return
    }

    // Obtener todos los usuarios del tenant
    const usersResponse = await fetch(`${getAuthAPI()}/api/usuarios/${tenant}`, {
      credentials: 'include'
    })

    if (!usersResponse.ok) {
      error.value = 'Error al obtener usuarios'
      return
    }

    const usersData = await usersResponse.json()
    const usuarios = usersData.usuarios || []

    // Crear mapa de usuarios por ID
    const usuariosMap = {}
    usuarios.forEach(u => {
      usuariosMap[u.id] = `${u.nombre} ${u.apellido}`
    })

    // Enriquecer excusas con nombres de estudiantes
    excusasData = excusasData.map(excuse => ({
      ...excuse,
      estudiante_nombre: usuariosMap[excuse.estudiante_id] || 'Desconocido'
    }))

    excuses.value = excusasData

  } catch (e) {
    console.error('Error loading my excuses:', e)
    error.value = 'Error de conexión'
  } finally {
    loading.value = false
  }
}

async function deleteExcuse(excuseId) {
  if (!confirm('¿Estás seguro de eliminar esta excusa?')) return

  try {
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const response = await fetch(`${getAttendanceAPI()}/api/attendance/excuses/${excuseId}`, {
      method: 'DELETE',
      headers: { 'Authorization': `Bearer ${session.access_token}` },
      credentials: 'include'
    })

    if (response.ok) {
      alert('✅ Excusa eliminada exitosamente')
      await loadMyExcuses()
    } else {
      const errorData = await response.json()
      alert(`❌ Error: ${errorData.detail || 'No se pudo eliminar la excusa'}`)
    }
  } catch (e) {
    console.error('Error deleting excuse:', e)
    alert('❌ Error al eliminar excusa')
  }
}

function formatDate(dateString) {
  if (!dateString) return '-'
  
  try {
    const date = new Date(dateString)
    if (isNaN(date.getTime())) return dateString
    
    return date.toLocaleDateString('es-ES', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    })
  } catch (e) {
    return dateString
  }
}

function formatDateTime(dateString) {
  if (!dateString) return '-'
  
  try {
    const date = new Date(dateString)
    if (isNaN(date.getTime())) return dateString
    
    return date.toLocaleDateString('es-ES', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  } catch (e) {
    return dateString
  }
}

function getStatusLabel(estado) {
  const labels = {
    pendiente: 'Pendiente',
    aprobada: '✓ Aprobada',
    rechazada: '✗ Rechazada'
  }
  return labels[estado] || estado
}

onMounted(() => {
  loadMyExcuses()
})
</script>

<style scoped>
.wrap { max-width: 1400px; margin: 2rem auto; padding: 0 1rem; animation: slideIn .7s; }
.subtitle { color: #666; margin-bottom: 1.5rem; }
.loading { text-align: center; padding: 3rem; display: flex; align-items: center; justify-content: center; gap: 1rem; color: #666; }
.spinner { border: 3px solid #f3f3f3; border-top: 3px solid #2a4dd0; border-radius: 50%; width: 28px; height: 28px; animation: spin 1s linear infinite; }
@keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }
.error-box { background: #fee; color: #c00; padding: .75rem; border-radius: 8px; margin-bottom: 1rem; border: 1px solid #fcc; }
.empty { text-align: center; padding: 3rem; color: #666; background: #f8fafc; border-radius: 12px; }

.table-container { background: #fff; border-radius: 12px; padding: 1.5rem; box-shadow: 0 2px 12px rgba(0,0,0,.08); }

.filters { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
.filter-select { border: 2px solid #e2e8f0; border-radius: 8px; padding: .6rem; font: inherit; background: #fff; }
.filter-select:focus { outline: none; border-color: #2a4dd0; }
.count { color: #64748b; font-weight: 600; font-size: .9rem; }

.table-wrapper { overflow-x: auto; }
.excuses-table { width: 100%; border-collapse: collapse; }
.excuses-table thead { background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%); }
.excuses-table th { text-align: left; padding: 1rem; font-weight: 700; color: #1e293b; border-bottom: 3px solid #e2e8f0; font-size: .9rem; text-transform: uppercase; letter-spacing: .5px; white-space: nowrap; }
.excuses-table td { padding: 1rem; border-bottom: 1px solid #f1f5f9; }
.excuses-table tbody tr:hover { background: #f8fafc; }

.date-cell { font-weight: 600; color: #1e293b; white-space: nowrap; }
.student-cell { font-weight: 600; color: #2a4dd0; }
.motivo-cell { max-width: 300px; color: #475569; }
.created-cell { color: #94a3b8; font-size: .85rem; white-space: nowrap; }

.status-badge { padding: .35rem .6rem; border-radius: 6px; font-size: .85rem; font-weight: 600; white-space: nowrap; }
.status-badge.pendiente { background: #fef3c7; color: #92400e; }
.status-badge.aprobada { background: #d1fae5; color: #065f46; }
.status-badge.rechazada { background: #fee2e2; color: #991b1b; }

.actions-cell { text-align: center; }
.btn-delete { background: #ef4444; color: #fff; border: none; padding: .4rem .6rem; border-radius: 6px; cursor: pointer; font-size: 1rem; transition: all .2s; }
.btn-delete:hover { background: #dc2626; transform: scale(1.1); }
.no-action { color: #cbd5e1; }

.fade-enter-active, .fade-leave-active { transition: opacity .4s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
@keyframes slideIn { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }

@media (max-width: 1024px) {
  .table-wrapper { overflow-x: auto; }
  .excuses-table { min-width: 1000px; }
}
</style>
