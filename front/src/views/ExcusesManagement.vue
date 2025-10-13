<template>
  <transition name="fade">
    <section class="wrap">
      <h1>Gestión de Excusas Pendientes</h1>
      <p class="subtitle">Aprobar o rechazar excusas como director</p>

      <div v-if="loading" class="loading">Cargando excusas...</div>
      <div v-else-if="error" class="error-box">{{ error }}</div>
      <div v-else-if="excuses.length === 0" class="empty">No hay excusas pendientes</div>

      <div v-else class="grid">
        <article v-for="excuse in excuses" :key="excuse.id" class="card">
          <div class="card-header">
            <span class="badge pending">Pendiente</span>
            <small class="date">{{ formatDate(excuse.created_at) }}</small>
          </div>
          
          <div class="info">
            <p><strong>Estudiante ID:</strong> {{ excuse.estudiante_id }}</p>
            <p><strong>Curso ID:</strong> {{ excuse.curso_id }}</p>
            <p><strong>Periodo:</strong> {{ excuse.fecha_inicio }} al {{ excuse.fecha_fin }}</p>
            <p><strong>Motivo:</strong> {{ excuse.motivo }}</p>
            <p v-if="excuse.documento_url"><strong>Documento:</strong> <a :href="excuse.documento_url" target="_blank">Ver documento</a></p>
          </div>

          <div class="actions">
            <button @click="approveExcuse(excuse.id, 'aprobada')" class="btn-approve">Aprobar</button>
            <button @click="openRejectModal(excuse.id)" class="btn-reject">Rechazar</button>
          </div>
        </article>
      </div>

      <!-- Modal para rechazar con comentario -->
      <div v-if="rejectModal.show" class="modal" @click.self="rejectModal.show = false">
        <div class="modal-content">
          <h2>Rechazar Excusa</h2>
          <textarea v-model="rejectModal.comentario" rows="4" placeholder="Comentario (opcional)"></textarea>
          <div class="modal-actions">
            <button @click="rejectModal.show = false" class="btn-outline">Cancelar</button>
            <button @click="confirmReject" class="btn-reject">Rechazar</button>
          </div>
        </div>
      </div>
    </section>
  </transition>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { supabase } from '../supabase'

const excuses = ref([])
const loading = ref(true)
const error = ref(null)
const rejectModal = ref({ show: false, excuseId: null, comentario: '' })

const ATTENDANCE_API = import.meta.env.VITE_ATTENDANCE_API || 'http://localhost:5004'

async function fetchPendingExcuses() {
  try {
    loading.value = true
    error.value = null

    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const response = await fetch(`${ATTENDANCE_API}/api/attendance/excuses/pending`, {
      headers: { 'Authorization': `Bearer ${session.access_token}` }
    })

    if (response.ok) {
      const data = await response.json()
      excuses.value = data.excusas || []
    } else {
      error.value = 'Error al cargar excusas pendientes'
    }
  } catch (e) {
    console.error('Error fetching excuses:', e)
    error.value = 'Error de conexión'
  } finally {
    loading.value = false
  }
}

async function approveExcuse(excuseId, estado) {
  try {
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const response = await fetch(`${ATTENDANCE_API}/api/attendance/excuse/approve`, {
      method: 'PATCH',
      headers: {
        'Authorization': `Bearer ${session.access_token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        excuse_id: excuseId,
        estado: estado,
        comentario_director: null
      })
    })

    if (response.ok) {
      alert('Excusa aprobada exitosamente')
      await fetchPendingExcuses()
    } else {
      error.value = 'Error al aprobar excusa'
    }
  } catch (e) {
    console.error('Error approving excuse:', e)
    error.value = 'Error de conexión'
  }
}

function openRejectModal(excuseId) {
  rejectModal.value = { show: true, excuseId, comentario: '' }
}

async function confirmReject() {
  try {
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const response = await fetch(`${ATTENDANCE_API}/api/attendance/excuse/approve`, {
      method: 'PATCH',
      headers: {
        'Authorization': `Bearer ${session.access_token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        excuse_id: rejectModal.value.excuseId,
        estado: 'rechazada',
        comentario_director: rejectModal.value.comentario || null
      })
    })

    if (response.ok) {
      alert('Excusa rechazada')
      rejectModal.value = { show: false, excuseId: null, comentario: '' }
      await fetchPendingExcuses()
    } else {
      error.value = 'Error al rechazar excusa'
    }
  } catch (e) {
    console.error('Error rejecting excuse:', e)
    error.value = 'Error de conexión'
  }
}

function formatDate(dateString) {
  return new Date(dateString).toLocaleDateString('es-ES', { year: 'numeric', month: 'short', day: 'numeric' })
}

onMounted(() => {
  fetchPendingExcuses()
})
</script>

<style scoped>
.wrap { max-width: 1100px; margin: 2rem auto; padding: 0 1rem; animation: slideIn .7s; }
.subtitle { color: #666; margin-bottom: 1.5rem; }
.loading, .error-box, .empty { text-align: center; padding: 2rem; }
.error-box { background: #fee; color: #c00; border-radius: 8px; }
.grid { display: grid; gap: 1rem; grid-template-columns: repeat(auto-fill, minmax(350px, 1fr)); }
.card { background: #fff; border-radius: 12px; padding: 1.25rem; box-shadow: 0 2px 8px rgba(0,0,0,.06); }
.card-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: .75rem; }
.badge { padding: .25rem .5rem; border-radius: 6px; font-size: .85rem; font-weight: 600; }
.badge.pending { background: #fef3c7; color: #92400e; }
.date { color: #999; font-size: .85rem; }
.info p { margin: .5rem 0; color: #555; font-size: .9rem; }
.info a { color: #2a4dd0; text-decoration: underline; }
.actions { display: flex; gap: .5rem; margin-top: 1rem; }
.btn-approve { background: #10b981; color: #fff; border: none; padding: .6rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; flex: 1; }
.btn-reject { background: #ef4444; color: #fff; border: none; padding: .6rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; flex: 1; }
.btn-outline { background: #fff; color: #2a4dd0; border: 2px solid #2a4dd0; padding: .6rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; }

/* Modal */
.modal { position: fixed; inset: 0; background: rgba(0,0,0,.5); display: grid; place-items: center; z-index: 100; }
.modal-content { background: #fff; border-radius: 16px; padding: 1.5rem; max-width: 500px; width: 90%; }
.modal-content h2 { margin-bottom: 1rem; color: #2a4dd0; }
.modal-content textarea { width: 100%; border: 1px solid #e5e7eb; border-radius: 8px; padding: .75rem; font: inherit; margin-bottom: 1rem; }
.modal-actions { display: flex; gap: .5rem; justify-content: flex-end; }

.fade-enter-active, .fade-leave-active { transition: opacity .4s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
@keyframes slideIn { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
</style>
