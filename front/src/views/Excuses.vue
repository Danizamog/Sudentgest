<template>
  <transition name="fade">
    <section class="wrap">
      <h1>Gestión de Excusas</h1>
      <p class="subtitle">Crear y consultar excusas para ausencias</p>

      <div v-if="error" class="error-box">{{ error }}</div>

      <form @submit.prevent="createExcuse" class="form-card">
        <div class="form-group">
          <label>Curso</label>
          <select v-model="excuse.curso_id" required @change="loadStudents">
            <option value="">-- Selecciona un curso --</option>
            <option v-for="curso in cursos" :key="curso.id" :value="curso.id">
              {{ curso.codigo }} - {{ curso.nombre }}
            </option>
          </select>
        </div>

        <div class="form-group">
          <label>Estudiante</label>
          <select v-model="excuse.estudiante_id" required>
            <option value="">-- Selecciona un estudiante --</option>
            <option v-for="student in students" :key="student.id" :value="student.id">
              {{ student.nombre }} {{ student.apellido }}
            </option>
          </select>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>Fecha Inicio</label>
            <input v-model="excuse.fecha_inicio" type="date" required />
          </div>

          <div class="form-group">
            <label>Fecha Fin</label>
            <input v-model="excuse.fecha_fin" type="date" required />
          </div>
        </div>

        <div class="form-group">
          <label>Motivo</label>
          <textarea v-model="excuse.motivo" rows="4" required placeholder="Describe el motivo de la excusa..."></textarea>
        </div>

        <div class="form-group">
          <label>Documento (URL opcional)</label>
          <input v-model="excuse.documento_url" type="url" placeholder="https://ejemplo.com/documento.pdf" />
        </div>

        <button type="submit" class="btn-primary" :disabled="loading">
          {{ loading ? 'Enviando...' : 'Enviar Excusa' }}
        </button>
      </form>
    </section>
  </transition>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { supabase } from '../supabase'

const cursos = ref([])
const students = ref([])
const loading = ref(false)
const error = ref(null)

const excuse = ref({
  curso_id: '',
  estudiante_id: '',
  fecha_inicio: '',
  fecha_fin: '',
  motivo: '',
  documento_url: ''
})

const ATTENDANCE_API = import.meta.env.VITE_ATTENDANCE_API || 'http://localhost:5004'
const COURSES_API = import.meta.env.VITE_COURSES_API || 'http://localhost:5008'
const AUTH_API = 'http://localhost:5002'

async function fetchCourses() {
  try {
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const response = await fetch(`${COURSES_API}/api/courses`, {
      headers: { 'Authorization': `Bearer ${session.access_token}` }
    })

    if (response.ok) {
      const data = await response.json()
      cursos.value = data.cursos || []
    }
  } catch (e) {
    console.error('Error fetching courses:', e)
  }
}

async function loadStudents() {
  if (!excuse.value.curso_id) return
  
  try {
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const tenant = getTenantFromEmail(session.user.email)
    if (!tenant) return

    const response = await fetch(`${AUTH_API}/api/usuarios/${tenant}`)
    
    if (response.ok) {
      const data = await response.json()
      students.value = (data.usuarios || []).filter(u => u.rol === 'Estudiante')
    }
  } catch (e) {
    console.error('Error loading students:', e)
  }
}

function getTenantFromEmail(email) {
  if (email.endsWith('@ucb.edu.bo')) return 'ucb.edu.bo'
  if (email.endsWith('@upb.edu.bo')) return 'upb.edu.bo'
  if (email.endsWith('@gmail.com')) return 'gmail.com'
  return null
}

async function createExcuse() {
  try {
    loading.value = true
    error.value = null

    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const response = await fetch(`${ATTENDANCE_API}/api/attendance/excuse`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${session.access_token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        estudiante_id: parseInt(excuse.value.estudiante_id),
        curso_id: parseInt(excuse.value.curso_id),
        fecha_inicio: excuse.value.fecha_inicio,
        fecha_fin: excuse.value.fecha_fin,
        motivo: excuse.value.motivo,
        documento_url: excuse.value.documento_url || null
      })
    })

    if (response.ok) {
      alert('Excusa enviada exitosamente. Pendiente de aprobación del director.')
      excuse.value = { curso_id: '', estudiante_id: '', fecha_inicio: '', fecha_fin: '', motivo: '', documento_url: '' }
    } else {
      const data = await response.json()
      error.value = data.detail || 'Error al enviar excusa'
    }
  } catch (e) {
    console.error('Error creating excuse:', e)
    error.value = 'Error de conexión'
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  fetchCourses()
})
</script>

<style scoped>
.wrap { max-width: 700px; margin: 2rem auto; padding: 0 1rem; animation: slideIn .7s; }
.subtitle { color: #666; margin-bottom: 1.5rem; }
.error-box { background: #fee; color: #c00; padding: .75rem; border-radius: 8px; margin-bottom: 1rem; border: 1px solid #fcc; }
.form-card { background: #fff; border-radius: 12px; padding: 1.5rem; box-shadow: 0 2px 8px rgba(0,0,0,.06); }
.form-group { display: flex; flex-direction: column; gap: .25rem; margin-bottom: 1rem; }
.form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
.form-group label { font-weight: 600; color: #555; font-size: .9rem; }
.form-group input, .form-group select, .form-group textarea { border: 1px solid #e5e7eb; border-radius: 8px; padding: .75rem; font: inherit; transition: border-color .15s, box-shadow .2s; }
.form-group input:focus, .form-group select:focus, .form-group textarea:focus { outline: none; border-color: #2a4dd0; box-shadow: 0 0 0 3px rgba(42,77,208,.1); }
.btn-primary { background: #2a4dd0; color: #fff; border: none; padding: .75rem 1.5rem; border-radius: 8px; font-weight: 600; cursor: pointer; width: 100%; }
.btn-primary:hover:not(:disabled) { background: #1e3a8a; }
.btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }

.fade-enter-active, .fade-leave-active { transition: opacity .4s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
@keyframes slideIn { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
@media (max-width: 600px) { .form-row { grid-template-columns: 1fr; } }
</style>
