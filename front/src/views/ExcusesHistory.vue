<template>
  <transition name="fade">
    <section class="wrap">
      <h1>{{ isDirector ? 'Historial de Excusas' : 'Mis Excusas' }}</h1>
      <p class="subtitle">{{ isDirector ? 'Gestiona todas las excusas' : 'Consulta tus excusas enviadas' }}</p>

      <!-- Filtro (solo director) -->
      <div v-if="isDirector" class="filters">
        <select v-model="estadoFilter" @change="fetchExcuses">
          <option value="">Todos los estados</option>
          <option value="pendiente">Pendientes</option>
          <option value="aprobada">Aprobadas</option>
          <option value="rechazada">Rechazadas</option>
        </select>
      </div>

      <div v-if="loading" class="loading">Cargando...</div>
      <div v-else-if="excuses.length === 0" class="empty">No hay excusas</div>

      <div v-else class="grid">
        <article v-for="excuse in excuses" :key="excuse.id" class="card">
          <div class="card-header">
            <span :class="['status-badge', excuse.estado]">{{ excuse.estado }}</span>
            <small>{{ formatDate(excuse.created_at) }}</small>
          </div>
          <p><strong>Estudiante ID:</strong> {{ excuse.estudiante_id }}</p>
          <p><strong>Periodo:</strong> {{ excuse.fecha_inicio }} - {{ excuse.fecha_fin }}</p>
          <p><strong>Motivo:</strong> {{ excuse.motivo }}</p>
          <p v-if="excuse.comentario_director"><strong>Comentario:</strong> {{ excuse.comentario_director }}</p>
        </article>
      </div>
    </section>
  </transition>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { supabase } from '../supabase'

const excuses = ref([])
const loading = ref(true)
const isDirector = ref(false)
const estadoFilter = ref('')

const ATTENDANCE_API = import.meta.env.VITE_ATTENDANCE_API || 'http://localhost:5004'

async function checkRole() {
  const token = localStorage.getItem('token')
  if (!token) return
  const response = await fetch('http://localhost:5002/api/auth/user-profile', {
    headers: { 'Authorization': `Bearer ${token}` }
  })
  if (response.ok) {
    const profile = await response.json()
    isDirector.value = profile.rol === 'Director'
  }
}

async function fetchExcuses() {
  try {
    loading.value = true
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const endpoint = isDirector.value
      ? `${ATTENDANCE_API}/api/attendance/excuses/all${estadoFilter.value ? `?estado=${estadoFilter.value}` : ''}`
      : `${ATTENDANCE_API}/api/attendance/excuses/my`

    const response = await fetch(endpoint, {
      headers: { 'Authorization': `Bearer ${session.access_token}` }
    })

    if (response.ok) {
      const data = await response.json()
      excuses.value = data.excusas || []
    }
  } catch (e) {
    console.error(e)
  } finally {
    loading.value = false
  }
}

function formatDate(dateString) {
  return new Date(dateString).toLocaleDateString('es-ES')
}

onMounted(async () => {
  await checkRole()
  await fetchExcuses()
})
</script>

<style scoped>
.wrap { max-width: 1100px; margin: 2rem auto; padding: 0 1rem; }
.filters { background: #fff; padding: 1rem; border-radius: 12px; margin-bottom: 1.5rem; }
.filters select { border: 1px solid #e5e7eb; border-radius: 8px; padding: .75rem; width: 100%; }
.grid { display: grid; gap: 1rem; grid-template-columns: repeat(auto-fill, minmax(350px, 1fr)); }
.card { background: #fff; border-radius: 12px; padding: 1.25rem; box-shadow: 0 2px 8px rgba(0,0,0,.06); }
.card-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: .75rem; }
.status-badge { padding: .25rem .5rem; border-radius: 6px; font-size: .85rem; font-weight: 600; }
.status-badge.pendiente { background: #fef3c7; color: #92400e; }
.status-badge.aprobada { background: #d1fae5; color: #065f46; }
.status-badge.rechazada { background: #fee2e2; color: #991b1b; }
.loading, .empty { text-align: center; padding: 2rem; }
.fade-enter-active, .fade-leave-active { transition: opacity .4s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
</style>
