<template>
  <transition name="fade">
    <section class="wrap">
      <h1>Historial de Asistencias</h1>
      <p class="subtitle">Consulta y edita registros de asistencia</p>

      <div v-if="error" class="error-box">{{ error }}</div>

      <!-- Filtros -->
      <div class="filters">
        <div class="form-group">
          <label>Curso</label>
          <select v-model="filters.curso_id" @change="fetchHistory">
            <option value="">-- Selecciona un curso --</option>
            <option v-for="curso in cursos" :key="curso.id" :value="curso.id">
              {{ curso.codigo }} - {{ curso.nombre }}
            </option>
          </select>
        </div>
        <div class="form-group">
          <label>Fecha Inicio</label>
          <input v-model="filters.fecha_inicio" type="date" @change="fetchHistory" />
        </div>
        <div class="form-group">
          <label>Fecha Fin</label>
          <input v-model="filters.fecha_fin" type="date" @change="fetchHistory" />
        </div>
      </div>

      <!-- Tabla de asistencias -->
      <div v-if="asistencias.length > 0" class="table-container">
        <table class="table">
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Estudiante ID</th>
              <th>Estado</th>
              <th>Observaciones</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="asistencia in asistencias" :key="asistencia.id">
              <td>{{ formatDate(asistencia.fecha) }}</td>
              <td>{{ asistencia.estudiante_id }}</td>
              <td>
                <span :class="['status-badge', asistencia.estado]">
                  {{ getStatusLabel(asistencia.estado) }}
                </span>
              </td>
              <td>{{ asistencia.observaciones || '-' }}</td>
              <td class="actions">
                <button @click="editAsistencia(asistencia)" class="btn-edit">✏️</button>
                <button v-if="isDirector" @click="deleteAsistencia(asistencia.id)" class="btn-delete">🗑️</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <div v-else-if="!loading && filters.curso_id" class="empty">
        No hay registros de asistencia
      </div>

      <!-- Modal de edición -->
      <div v-if="editModal.show" class="modal" @click.self="editModal.show = false">
        <div class="modal-content">
          <h2>Editar Asistencia</h2>
          <div class="form-group">
            <label>Estado</label>
            <select v-model="editModal.estado">
              <option value="presente">Presente</option>
              <option value="ausente">Ausente</option>
              <option value="tardanza">Tardanza</option>
            </select>
          </div>
          <div class="form-group">
            <label>Observaciones</label>
            <textarea v-model="editModal.observaciones" rows="3"></textarea>
          </div>
          <div class="modal-actions">
            <button @click="editModal.show = false" class="btn-outline">Cancelar</button>
            <button @click="saveEdit" class="btn-primary">Guardar</button>
          </div>
        </div>
      </div>
    </section>
  </transition>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { supabase } from '../supabase'

const cursos = ref([])
const asistencias = ref([])
const loading = ref(false)
const error = ref(null)
const isDirector = ref(false)

const filters = reactive({
  curso_id: '',
  fecha_inicio: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
  fecha_fin: new Date().toISOString().split('T')[0]
})

const editModal = reactive({
  show: false,
  id: null,
  estado: '',
  observaciones: ''
})

const ATTENDANCE_API = import.meta.env.VITE_ATTENDANCE_API || 'http://localhost:5004'
const COURSES_API = import.meta.env.VITE_COURSES_API || 'http://localhost:5008'

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

async function fetchCourses() {
  const { data: { session } } = await supabase.auth.getSession()
  if (!session) return
  const response = await fetch(`${COURSES_API}/api/courses/my-courses`, {
    headers: { 'Authorization': `Bearer ${session.access_token}` }
  })
  if (response.ok) {
    const data = await response.json()
    cursos.value = data.cursos || []
  }
}

async function fetchHistory() {
  if (!filters.curso_id) return
  try {
    loading.value = true
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const response = await fetch(
      `${ATTENDANCE_API}/api/attendance/history/${filters.curso_id}?fecha_inicio=${filters.fecha_inicio}&fecha_fin=${filters.fecha_fin}`,
      { headers: { 'Authorization': `Bearer ${session.access_token}` } }
    )

    if (response.ok) {
      const data = await response.json()
      asistencias.value = data.asistencias || []
    }
  } catch (e) {
    error.value = 'Error al cargar historial'
  } finally {
    loading.value = false
  }
}

function editAsistencia(asistencia) {
  editModal.id = asistencia.id
  editModal.estado = asistencia.estado
  editModal.observaciones = asistencia.observaciones || ''
  editModal.show = true
}

async function saveEdit() {
  try {
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const response = await fetch(
      `${ATTENDANCE_API}/api/attendance/update/${editModal.id}?estado=${editModal.estado}&observaciones=${encodeURIComponent(editModal.observaciones)}`,
      {
        method: 'PATCH',
        headers: { 'Authorization': `Bearer ${session.access_token}` }
      }
    )

    if (response.ok) {
      editModal.show = false
      await fetchHistory()
    }
  } catch (e) {
    error.value = 'Error al actualizar'
  }
}

async function deleteAsistencia(id) {
  if (!confirm('¿Eliminar este registro?')) return
  try {
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const response = await fetch(`${ATTENDANCE_API}/api/attendance/delete/${id}`, {
      method: 'DELETE',
      headers: { 'Authorization': `Bearer ${session.access_token}` }
    })

    if (response.ok) {
      await fetchHistory()
    }
  } catch (e) {
    error.value = 'Error al eliminar'
  }
}

function formatDate(dateString) {
  return new Date(dateString).toLocaleDateString('es-ES')
}

function getStatusLabel(estado) {
  const labels = { presente: '✓ Presente', ausente: '✗ Ausente', tardanza: '⏰ Tardanza' }
  return labels[estado] || estado
}

onMounted(() => {
  checkRole()
  fetchCourses()
})
</script>

<style scoped>
.wrap { max-width: 1200px; margin: 2rem auto; padding: 0 1rem; }
.filters { display: grid; grid-template-columns: 2fr 1fr 1fr; gap: 1rem; background: #fff; padding: 1.5rem; border-radius: 12px; margin-bottom: 1.5rem; }
.form-group { display: flex; flex-direction: column; gap: .5rem; }
.form-group label { font-weight: 600; font-size: .9rem; }
.form-group input, .form-group select, .form-group textarea { border: 1px solid #e5e7eb; border-radius: 8px; padding: .75rem; }
.table-container { overflow-x: auto; background: #fff; border-radius: 12px; padding: 1.5rem; }
.table { width: 100%; border-collapse: collapse; }
.table th { text-align: left; padding: 1rem; border-bottom: 2px solid #e2e8f0; font-weight: 700; }
.table td { padding: 1rem; border-bottom: 1px solid #f1f5f9; }
.status-badge { padding: .25rem .5rem; border-radius: 6px; font-size: .85rem; font-weight: 600; }
.status-badge.presente { background: #d1fae5; color: #065f46; }
.status-badge.ausente { background: #fee2e2; color: #991b1b; }
.status-badge.tardanza { background: #fef3c7; color: #92400e; }
.actions { display: flex; gap: .5rem; }
.btn-edit, .btn-delete { background: none; border: none; cursor: pointer; font-size: 1.2rem; }
.error-box { background: #fee; color: #c00; padding: .75rem; border-radius: 8px; margin-bottom: 1rem; }
.empty { text-align: center; padding: 2rem; color: #666; }
.modal { position: fixed; inset: 0; background: rgba(0,0,0,.5); display: grid; place-items: center; z-index: 100; }
.modal-content { background: #fff; border-radius: 16px; padding: 1.5rem; width: 500px; }
.modal-actions { display: flex; gap: .5rem; justify-content: flex-end; margin-top: 1rem; }
.btn-primary { background: #2a4dd0; color: #fff; border: none; padding: .6rem 1rem; border-radius: 8px; cursor: pointer; }
.btn-outline { background: #fff; color: #2a4dd0; border: 2px solid #2a4dd0; padding: .6rem 1rem; border-radius: 8px; cursor: pointer; }
.fade-enter-active, .fade-leave-active { transition: opacity .4s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
</style>
