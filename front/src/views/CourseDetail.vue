<template>
  <transition name="fade">
    <section class="wrap">
      <div class="header">
        <button @click="$router.back()" class="btn-back">← Volver</button>
        <h1>{{ curso?.nombre || 'Cargando...' }}</h1>
        <button @click="viewAssignments" class="btn-assignments">
          📝 Ver Tareas
        </button>
      </div>

      <div v-if="loading" class="loading">Cargando información...</div>
      <div v-else-if="error" class="error">{{ error }}</div>

      <div v-else>
        <!-- Información del curso -->
        <div class="course-info">
          <div class="info-item">
            <strong>Código:</strong> {{ curso.codigo }}
          </div>
          <div class="info-item">
            <strong>Créditos:</strong> {{ curso.creditos }}
          </div>
          <div class="info-item" v-if="curso.horario">
            <strong>Horario:</strong> {{ curso.horario }}
          </div>
          <div class="info-item full" v-if="curso.descripcion">
            <strong>Descripción:</strong><br>{{ curso.descripcion }}
          </div>
        </div>

        <!-- Profesor asignado -->
        <div class="profesor-section">
          <h2 style="display: inline-flex; align-items: center; gap: .5rem;">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
              <circle cx="12" cy="7" r="4"></circle>
            </svg>
            Profesor Asignado
          </h2>
          <div v-if="profesorAsignado" class="profesor-card">
            <div class="profesor-info">
              <h3>{{ profesorAsignado.nombre }} {{ profesorAsignado.apellido }}</h3>
              <p class="email">{{ profesorAsignado.email }}</p>
            </div>
            <button @click="showProfesorModal = true" class="btn-change">
              Cambiar Profesor
            </button>
          </div>
          <div v-else class="profesor-empty">
            <p>No hay profesor asignado a este curso</p>
            <button @click="showProfesorModal = true" class="btn-primary">
              Asignar Profesor
            </button>
          </div>
        </div>

        <!-- Lista de inscritos -->
        <div class="inscritos-section">
          <h2>Estudiantes Inscritos ({{ inscritos.length }})</h2>
          
          <div v-if="inscritos.length === 0" class="empty">
            No hay estudiantes inscritos en este curso.
          </div>

          <div v-else class="inscritos-grid">
            <article v-for="inscrito in inscritos" :key="inscrito.inscripcion_id" class="inscrito-card">
              <div class="inscrito-info">
                <h3>{{ inscrito.nombre }} {{ inscrito.apellido }}</h3>
                <p class="email">{{ inscrito.email }}</p>
                <span class="badge" :class="inscrito.rol">{{ inscrito.rol }}</span>
              </div>
              <button 
                @click="confirmDelete(inscrito)" 
                class="btn-delete"
                :disabled="deleting"
              >
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <polyline points="3 6 5 6 21 6"></polyline>
                  <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                  <line x1="10" y1="11" x2="10" y2="17"></line>
                  <line x1="14" y1="11" x2="14" y2="17"></line>
                </svg>
                Eliminar
              </button>
            </article>
          </div>
        </div>
      </div>

      <!-- Modal de confirmación -->
      <div v-if="showConfirm" class="modal" @click.self="showConfirm = false">
        <div class="modal-content confirm">
          <h2 style="display: inline-flex; align-items: center; gap: .5rem;">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#ef4444" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path>
              <line x1="12" y1="9" x2="12" y2="13"></line>
              <line x1="12" y1="17" x2="12.01" y2="17"></line>
            </svg>
            Confirmar eliminación
          </h2>
          <p>¿Estás seguro de eliminar a <strong>{{ inscritoToDelete?.nombre }} {{ inscritoToDelete?.apellido }}</strong> del curso?</p>
          <div class="modal-actions">
            <button @click="showConfirm = false" class="btn-outline">Cancelar</button>
            <button @click="deleteInscrito" class="btn-danger" :disabled="deleting">
              {{ deleting ? 'Eliminando...' : 'Eliminar' }}
            </button>
          </div>
        </div>
      </div>

      <!-- Modal de asignación de profesor -->
      <div v-if="showProfesorModal" class="modal" @click.self="showProfesorModal = false">
        <div class="modal-content">
          <h2>Asignar Profesor al Curso</h2>
          <div v-if="profesorError" class="error-box">{{ profesorError }}</div>
          <form @submit.prevent="assignProfesor" class="form">
            <div class="form-group">
              <label>Seleccionar Profesor</label>
              <select v-model="selectedProfesorId" required>
                <option value="">-- Selecciona un profesor --</option>
                <option v-for="prof in profesores" :key="prof.id" :value="prof.id">
                  {{ prof.nombre }} {{ prof.apellido }} - {{ prof.email }}
                </option>
              </select>
            </div>
            
            <div class="modal-actions">
              <button type="button" @click="showProfesorModal = false" class="btn-outline">Cancelar</button>
              <button type="submit" class="btn-primary" :disabled="assigning">
                {{ assigning ? 'Asignando...' : 'Asignar' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </section>
  </transition>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { supabase } from '../supabase'

const route = useRoute()
const router = useRouter()
const curso = ref(null)
const inscritos = ref([])
const profesores = ref([])
const profesorAsignado = ref(null)
const loading = ref(true)
const error = ref(null)
const profesorError = ref(null)
const deleting = ref(false)
const assigning = ref(false)
const showConfirm = ref(false)
const showProfesorModal = ref(false)
const inscritoToDelete = ref(null)
const selectedProfesorId = ref('')
const tenantDomain = ref(null)

const COURSES_API = import.meta.env.VITE_COURSES_API || 'http://localhost:5008'
const AUTH_API = 'http://localhost:5002'

function getTenantFromEmail(email) {
  if (email.endsWith('@ucb.edu.bo')) return 'ucb.edu.bo'
  if (email.endsWith('@upb.edu.bo')) return 'upb.edu.bo'
  if (email.endsWith('@gmail.com')) return 'gmail.com'
  return null
}

async function fetchProfesores() {
  try {
    if (!tenantDomain.value) return
    
    const response = await fetch(`${AUTH_API}/api/usuarios/${tenantDomain.value}`)
    
    if (response.ok) {
      const data = await response.json()
      profesores.value = data.usuarios.filter(u => u.rol === 'Profesor') || []
    }
  } catch (e) {
    console.error('Error fetching profesores:', e)
  }
}

async function fetchCourseDetail() {
  try {
    loading.value = true
    error.value = null
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) {
      error.value = 'No estás autenticado'
      return
    }

    // Obtener tenant del usuario
    const { data: { user } } = await supabase.auth.getUser()
    if (user?.email) {
      tenantDomain.value = getTenantFromEmail(user.email)
    }

    const cursoId = route.params.id

    // Obtener datos del curso
    const coursesResponse = await fetch(`${COURSES_API}/api/courses`, {
      headers: { 'Authorization': `Bearer ${session.access_token}` }
    })

    if (coursesResponse.ok) {
      const data = await coursesResponse.json()
      curso.value = data.cursos.find(c => c.id === parseInt(cursoId))
      
      // Si hay profesor asignado, obtener sus datos
      if (curso.value?.profesor_id) {
        await fetchProfesorAsignado(curso.value.profesor_id)
      }
    }

    // Obtener inscritos
    const enrollmentsResponse = await fetch(`${COURSES_API}/api/courses/${cursoId}/enrollments`, {
      headers: { 'Authorization': `Bearer ${session.access_token}` }
    })

    if (enrollmentsResponse.ok) {
      const data = await enrollmentsResponse.json()
      inscritos.value = data.inscritos || []
    } else {
      error.value = 'Error al cargar inscritos'
    }

    // Cargar lista de profesores
    await fetchProfesores()
  } catch (e) {
    console.error('Error fetching course detail:', e)
    error.value = 'Error de conexión'
  } finally {
    loading.value = false
  }
}

async function fetchProfesorAsignado(profesorId) {
  try {
    if (!tenantDomain.value) return
    
    const response = await fetch(`${AUTH_API}/api/usuarios/${tenantDomain.value}`)
    
    if (response.ok) {
      const data = await response.json()
      profesorAsignado.value = data.usuarios.find(u => u.id === profesorId)
    }
  } catch (e) {
    console.error('Error fetching profesor asignado:', e)
  }
}

function confirmDelete(inscrito) {
  inscritoToDelete.value = inscrito
  showConfirm.value = true
}

async function deleteInscrito() {
  try {
    deleting.value = true
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const response = await fetch(`${COURSES_API}/api/courses/enrollments/${inscritoToDelete.value.inscripcion_id}`, {
      method: 'DELETE',
      headers: { 'Authorization': `Bearer ${session.access_token}` }
    })

    if (response.ok) {
      inscritos.value = inscritos.value.filter(i => i.inscripcion_id !== inscritoToDelete.value.inscripcion_id)
      showConfirm.value = false
      inscritoToDelete.value = null
    } else {
      const data = await response.json()
      alert(data.detail || 'Error al eliminar inscripción')
    }
  } catch (e) {
    console.error('Error deleting enrollment:', e)
    alert('Error al eliminar inscripción')
  } finally {
    deleting.value = false
  }
}

async function assignProfesor() {
  try {
    assigning.value = true
    profesorError.value = null
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) {
      profesorError.value = 'No estás autenticado'
      return
    }

    const response = await fetch(
      `${COURSES_API}/api/courses/${route.params.id}/assign-teacher?profesor_id=${selectedProfesorId.value}`,
      {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${session.access_token}` }
      }
    )

    if (response.ok) {
      showProfesorModal.value = false
      selectedProfesorId.value = ''
      // Actualizar datos del profesor asignado
      await fetchProfesorAsignado(parseInt(selectedProfesorId.value))
      // Recargar datos del curso
      await fetchCourseDetail()
    } else {
      const data = await response.json()
      profesorError.value = data.detail || 'Error al asignar profesor'
    }
  } catch (e) {
    console.error('Error assigning profesor:', e)
    profesorError.value = 'Error al asignar profesor'
  } finally {
    assigning.value = false
  }
}

onMounted(() => {
  fetchCourseDetail()
})
</script>

<style scoped>
.wrap { max-width: 1100px; margin: 2rem auto; padding: 0 1rem; animation: slideIn .7s; }
.header { display: flex; align-items: center; gap: 1rem; margin-bottom: 1.5rem; }
.btn-back { background: #fff; color: #2a4dd0; border: 2px solid #2a4dd0; padding: .5rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; }
.btn-assignments { background: #10b981; color: #fff; border: none; padding: .5rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; margin-left: auto; transition: background .2s; }
.btn-assignments:hover { background: #059669; }
.loading, .error, .empty { text-align: center; padding: 2rem; }
.error { color: #ef4444; }
.error-box { background: #fee; color: #c00; padding: .75rem; border-radius: 8px; margin-bottom: 1rem; border: 1px solid #fcc; }
.empty { color: #666; }

.course-info { display: grid; grid-template-columns: repeat(3, 1fr); gap: 1rem; background: #fff; border-radius: 12px; padding: 1.5rem; box-shadow: 0 2px 8px rgba(0,0,0,.06); margin-bottom: 2rem; }
.info-item { padding: .5rem 0; }
.info-item.full { grid-column: 1 / -1; }
.info-item strong { color: #2a4dd0; }

.profesor-section { background: #fff; border-radius: 12px; padding: 1.5rem; box-shadow: 0 2px 8px rgba(0,0,0,.06); margin-bottom: 2rem; }
.profesor-section h2 { margin: 0 0 1rem; color: #2a4dd0; font-size: 1.25rem; }
.profesor-card { display: flex; justify-content: space-between; align-items: center; background: #f9fafb; border-radius: 8px; padding: 1rem; }
.profesor-info h3 { margin: 0 0 .25rem; color: #222; }
.profesor-empty { text-align: center; padding: 1rem; }
.profesor-empty p { color: #666; margin-bottom: 1rem; }
.btn-change { background: #f59e0b; color: #fff; border: none; padding: .5rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; transition: background .2s; }
.btn-change:hover { background: #d97706; }
.btn-primary { background: #2a4dd0; color: #fff; border: none; padding: .6rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; transition: transform .15s, box-shadow .2s; }
.btn-primary:hover { transform: translateY(-1px); box-shadow: 0 6px 20px rgba(42,77,208,.2); }
.btn-primary:disabled { opacity: .5; cursor: not-allowed; transform: none; }

.inscritos-section h2 { margin-bottom: 1rem; color: #2a4dd0; }
.inscritos-grid { display: grid; gap: 1rem; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); }
.inscrito-card { background: #fff; border-radius: 12px; padding: 1rem; box-shadow: 0 2px 8px rgba(0,0,0,.06); display: flex; justify-content: space-between; align-items: center; transition: transform .18s ease, box-shadow .2s ease; }
.inscrito-card:hover { transform: translateY(-2px); box-shadow: 0 4px 12px rgba(0,0,0,.08); }
.inscrito-info h3 { margin: 0 0 .25rem; color: #222; }
.email { margin: .25rem 0; color: #666; font-size: .9rem; }
.badge { display: inline-block; padding: .25rem .5rem; border-radius: 6px; font-size: .85rem; font-weight: 600; }
.badge.estudiante { background: #eef2ff; color: #2a4dd0; }
.badge.profesor { background: #fef3c7; color: #d97706; }
.badge.director { background: #fee; color: #c00; }
.badge.Estudiante { background: #eef2ff; color: #2a4dd0; }
.badge.Profesor { background: #fef3c7; color: #d97706; }
.badge.Director { background: #fee; color: #c00; }
.btn-delete { background: #ef4444; color: #fff; border: none; padding: .5rem .75rem; border-radius: 8px; cursor: pointer; font-size: .9rem; transition: background .2s; display: inline-flex; align-items: center; gap: .3rem; }
.btn-delete svg { flex-shrink: 0; }
.btn-delete:hover { background: #dc2626; }
.btn-delete:disabled { opacity: .5; cursor: not-allowed; }

/* Modal */
.modal { position: fixed; inset: 0; background: rgba(0,0,0,.5); display: grid; place-items: center; z-index: 100; animation: fadeIn .3s; }
.modal-content { background: #fff; border-radius: 16px; padding: 1.5rem; max-width: 500px; width: 90%; animation: scaleIn .3s; }
.modal-content.confirm { max-width: 400px; text-align: center; }
.modal-content h2 { margin-bottom: .75rem; }
.modal-content p { margin-bottom: 1.5rem; color: #555; }
.form { display: flex; flex-direction: column; gap: .75rem; }
.form-group { display: flex; flex-direction: column; gap: .25rem; }
.form-group label { font-weight: 600; color: #555; font-size: .9rem; }
.form select { border: 1px solid #e5e7eb; border-radius: 8px; padding: .75rem; font: inherit; transition: border-color .15s, box-shadow .2s; }
.form select:focus { outline: none; border-color: #2a4dd0; box-shadow: 0 0 0 3px rgba(42,77,208,.1); }
.modal-actions { display: flex; gap: .5rem; justify-content: center; }
.btn-outline { background: #fff; color: #2a4dd0; border: 2px solid #2a4dd0; padding: .6rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; }
.btn-danger { background: #ef4444; color: #fff; border: none; padding: .6rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; }
.btn-danger:disabled { opacity: .5; cursor: not-allowed; }

.fade-enter-active, .fade-leave-active { transition: opacity .4s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
@keyframes slideIn { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
@keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
@keyframes scaleIn { from { transform: scale(.95); opacity: 0; } to { transform: scale(1); opacity: 1; } }

@media (max-width: 900px) {
  .course-info { grid-template-columns: repeat(2, 1fr); }
  .header { flex-wrap: wrap; }
  .btn-assignments { margin-left: 0; }
}
@media (max-width: 600px) {
  .course-info { grid-template-columns: 1fr; }
  .header { flex-direction: column; align-items: flex-start; }
}
</style>