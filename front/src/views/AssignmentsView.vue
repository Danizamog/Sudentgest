<template>
  <transition name="fade">
    <section class="wrap">
      <div class="header">
        <button @click="$router.back()" class="btn-back">← Volver al Curso</button>
        <h1>Tareas - {{ curso?.nombre }}</h1>
        <button v-if="userRole === 'teacher'" @click="showCreateModal = true" class="btn-primary">
          ➕ Nueva Tarea
        </button>
      </div>

      <div v-if="loading" class="loading">Cargando tareas...</div>
      <div v-else-if="error" class="error">{{ error }}</div>

      <div v-else>
        <!-- Estadísticas para profesores -->
        <div v-if="userRole === 'teacher'" class="stats-grid">
          <div class="stat-card">
            <h3>Total Tareas</h3>
            <p class="stat-number">{{ assignments.length }}</p>
          </div>
          <div class="stat-card">
            <h3>Completadas</h3>
            <p class="stat-number">{{ completedAssignments }}</p>
          </div>
          <div class="stat-card">
            <h3>Pendientes</h3>
            <p class="stat-number">{{ pendingAssignments }}</p>
          </div>
        </div>

        <!-- Lista de tareas -->
        <div class="assignments-section">
          <h2 v-if="userRole === 'teacher'">Tareas del Curso</h2>
          <h2 v-else>Mis Tareas</h2>
          
          <div v-if="assignments.length === 0" class="empty">
            <p v-if="userRole === 'teacher'">No hay tareas creadas para este curso.</p>
            <p v-else>No hay tareas asignadas para este curso.</p>
          </div>

          <div v-else class="assignments-grid">
            <article v-for="item in assignments" :key="item.assignment.id" class="assignment-card">
              <div class="assignment-info">
                <h3>{{ item.assignment.title }}</h3>
                <p class="description">{{ item.assignment.description }}</p>
                
                <div class="assignment-meta">
                  <span class="meta-item" v-if="item.assignment.due_date">
                    📅 {{ formatDate(item.assignment.due_date) }}
                  </span>
                  <span class="meta-item" v-if="item.assignment.points">
                    ⭐ {{ item.assignment.points }} puntos
                  </span>
                  <span class="meta-item">
                    🏷️ {{ item.assignment.assignment_type }}
                  </span>
                </div>

                <!-- Estado para estudiantes -->
                <div v-if="userRole === 'student'" class="completion-status">
                  <span v-if="item.completion" class="status completed">
                    ✅ Completada - {{ formatDate(item.completion.completed_at) }}
                  </span>
                  <span v-else class="status pending">
                    ⏳ Pendiente
                  </span>
                </div>

                <!-- Estadísticas para profesores -->
                <div v-if="userRole === 'teacher' && item.completions" class="completion-stats">
                  <span class="stats">
                    📊 {{ item.completions.completed }}/{{ item.completions.total }} estudiantes completaron
                  </span>
                </div>
              </div>

              <div class="assignment-actions">
                <button 
                  v-if="userRole === 'student' && !item.completion"
                  @click="completeAssignment(item.assignment.id)"
                  class="btn-complete"
                  :disabled="completing"
                >
                  ✅ Marcar como Completada
                </button>
                
                <button 
                  v-if="userRole === 'teacher'"
                  @click="viewAssignmentDetails(item.assignment.id)"
                  class="btn-outline"
                >
                  👀 Ver Detalles
                </button>
              </div>
            </article>
          </div>
        </div>
      </div>

      <!-- Modal crear tarea -->
      <div v-if="showCreateModal" class="modal" @click.self="showCreateModal = false">
        <div class="modal-content">
          <h2>➕ Crear Nueva Tarea</h2>
          <form @submit.prevent="createAssignment">
            <div class="form-group">
              <label>Título *</label>
              <input v-model="newAssignment.title" type="text" required>
            </div>
            
            <div class="form-group">
              <label>Descripción</label>
              <textarea v-model="newAssignment.description" rows="3"></textarea>
            </div>
            
            <div class="form-row">
              <div class="form-group">
                <label>Fecha Límite</label>
                <input v-model="newAssignment.due_date" type="datetime-local">
              </div>
              
              <div class="form-group">
                <label>Puntos</label>
                <input v-model="newAssignment.points" type="number" step="0.1" min="0">
              </div>
            </div>
            
            <div class="form-group">
              <label>Tipo de Tarea *</label>
              <select v-model="newAssignment.assignment_type" required>
                <option value="tarea">Tarea</option>
                <option value="examen">Examen</option>
                <option value="proyecto">Proyecto</option>
                <option value="quiz">Quiz</option>
                <option value="presentacion">Presentación</option>
              </select>
            </div>
            
            <div class="modal-actions">
              <button type="button" @click="showCreateModal = false" class="btn-outline">
                Cancelar
              </button>
              <button type="submit" class="btn-primary" :disabled="creating">
                {{ creating ? 'Creando...' : 'Crear Tarea' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </section>
  </transition>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { supabase } from '../supabase'

const route = useRoute()
const router = useRouter()

const curso = ref(null)
const assignments = ref([])
const userRole = ref('')
const loading = ref(true)
const error = ref(null)
const creating = ref(false)
const completing = ref(false)
const showCreateModal = ref(false)

const newAssignment = ref({
  title: '',
  description: '',
  due_date: '',
  points: null,
  assignment_type: 'tarea'
})

const TAREAS_API = import.meta.env.VITE_TAREAS_API || 'http://localhost:5011'

// Computed
const completedAssignments = computed(() => {
  return assignments.value.filter(a => a.completions?.completed > 0).length
})

const pendingAssignments = computed(() => {
  return assignments.value.length - completedAssignments.value
})

// Methods
function formatDate(dateString) {
  return new Date(dateString).toLocaleDateString('es-ES', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

async function fetchAssignments() {
  try {
    loading.value = true
    error.value = null
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) {
      error.value = 'No estás autenticado'
      return
    }

    const courseId = route.params.id

    // Obtener datos del curso primero
    const coursesResponse = await fetch(`${import.meta.env.VITE_COURSES_API || 'http://localhost:5008'}/api/courses`, {
      headers: { 'Authorization': `Bearer ${session.access_token}` }
    })

    if (coursesResponse.ok) {
      const data = await coursesResponse.json()
      curso.value = data.cursos.find(c => c.id === parseInt(courseId))
    }

    // Obtener tareas
    const assignmentsResponse = await fetch(`${TAREAS_API}/api/courses/${courseId}/assignments?email=${session.user.email}`, {
      headers: { 'Authorization': `Bearer ${session.access_token}` }
    })

    if (assignmentsResponse.ok) {
      const data = await assignmentsResponse.json()
      assignments.value = data.assignments || []
      userRole.value = data.userRole
    } else {
      error.value = 'Error al cargar tareas'
    }
  } catch (e) {
    console.error('Error fetching assignments:', e)
    error.value = 'Error de conexión'
  } finally {
    loading.value = false
  }
}

async function createAssignment() {
  try {
    creating.value = true
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const courseId = route.params.id
    const assignmentData = {
      ...newAssignment.value,
      cursoId: parseInt(courseId),
      createdBy: session.user.email
    }

    const response = await fetch(`${TAREAS_API}/api/courses/${courseId}/assignments?email=${session.user.email}`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${session.access_token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(assignmentData)
    })

    if (response.ok) {
      const data = await response.json()
      assignments.value.unshift({
        assignment: data.assignment,
        completions: { total: 0, completed: 0 }
      })
      showCreateModal.value = false
      resetNewAssignment()
    } else {
      const data = await response.json()
      alert(data.detail || 'Error al crear tarea')
    }
  } catch (e) {
    console.error('Error creating assignment:', e)
    alert('Error al crear tarea')
  } finally {
    creating.value = false
  }
}

async function completeAssignment(assignmentId) {
  try {
    completing.value = true
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const response = await fetch(`${TAREAS_API}/api/assignments/${assignmentId}/complete?email=${session.user.email}`, {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${session.access_token}` }
    })

    if (response.ok) {
      // Actualizar estado local
      const assignment = assignments.value.find(a => a.assignment.id === assignmentId)
      if (assignment) {
        assignment.completion = {
          completed_at: new Date().toISOString(),
          status: 'completed'
        }
      }
    } else {
      const data = await response.json()
      alert(data.detail || 'Error al completar tarea')
    }
  } catch (e) {
    console.error('Error completing assignment:', e)
    alert('Error al completar tarea')
  } finally {
    completing.value = false
  }
}

function viewAssignmentDetails(assignmentId) {
  // Navegar a vista de detalles de tarea
  router.push(`/courses/${route.params.id}/assignments/${assignmentId}`)
}

function resetNewAssignment() {
  newAssignment.value = {
    title: '',
    description: '',
    due_date: '',
    points: null,
    assignment_type: 'tarea'
  }
}

onMounted(() => {
  fetchAssignments()
})
</script>

<style scoped>
.wrap { max-width: 1100px; margin: 2rem auto; padding: 0 1rem; animation: slideIn .7s; }
.header { display: flex; align-items: center; gap: 1rem; margin-bottom: 1.5rem; }
.btn-back { background: #fff; color: #2a4dd0; border: 2px solid #2a4dd0; padding: .5rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; }
.btn-primary { background: #10b981; color: #fff; border: none; padding: .5rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; margin-left: auto; transition: background .2s; }
.btn-primary:hover { background: #059669; }
.loading, .error, .empty { text-align: center; padding: 2rem; }
.error { color: #ef4444; }
.empty { color: #666; }

/* Stats */
.stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 1rem; margin-bottom: 2rem; }
.stat-card { background: #fff; border-radius: 12px; padding: 1.5rem; text-align: center; box-shadow: 0 2px 8px rgba(0,0,0,.06); }
.stat-number { font-size: 2rem; font-weight: bold; color: #2a4dd0; margin: .5rem 0 0; }

/* Assignments */
.assignments-section h2 { margin-bottom: 1rem; color: #2a4dd0; }
.assignments-grid { display: grid; gap: 1rem; }
.assignment-card { background: #fff; border-radius: 12px; padding: 1.5rem; box-shadow: 0 2px 8px rgba(0,0,0,.06); display: flex; justify-content: space-between; align-items: flex-start; transition: transform .18s ease, box-shadow .2s ease; }
.assignment-card:hover { transform: translateY(-2px); box-shadow: 0 4px 12px rgba(0,0,0,.08); }
.assignment-info { flex: 1; }
.assignment-info h3 { margin: 0 0 .5rem; color: #222; font-size: 1.2rem; }
.description { margin: .5rem 0; color: #555; line-height: 1.4; }
.assignment-meta { display: flex; gap: 1rem; margin: .75rem 0; flex-wrap: wrap; }
.meta-item { background: #f8fafc; padding: .25rem .5rem; border-radius: 6px; font-size: .85rem; color: #64748b; }

/* Status */
.completion-status, .completion-stats { margin-top: .75rem; }
.status { padding: .25rem .75rem; border-radius: 20px; font-size: .9rem; font-weight: 600; }
.status.completed { background: #d1fae5; color: #065f46; }
.status.pending { background: #fef3c7; color: #92400e; }
.stats { background: #e0f2fe; color: #075985; padding: .25rem .75rem; border-radius: 20px; font-size: .9rem; }

/* Actions */
.assignment-actions { margin-left: 1rem; }
.btn-complete { background: #10b981; color: #fff; border: none; padding: .5rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; font-size: .9rem; transition: background .2s; }
.btn-complete:hover { background: #059669; }
.btn-complete:disabled { opacity: .5; cursor: not-allowed; }
.btn-outline { background: #fff; color: #2a4dd0; border: 2px solid #2a4dd0; padding: .5rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; font-size: .9rem; }

/* Modal */
.modal { position: fixed; inset: 0; background: rgba(0,0,0,.5); display: grid; place-items: center; z-index: 100; animation: fadeIn .3s; }
.modal-content { background: #fff; border-radius: 16px; padding: 2rem; max-width: 500px; width: 90%; max-height: 90vh; overflow-y: auto; }
.modal-content h2 { margin-bottom: 1.5rem; color: #2a4dd0; }

/* Form */
.form-group { margin-bottom: 1rem; }
.form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
.form-group label { display: block; margin-bottom: .5rem; font-weight: 600; color: #374151; }
.form-group input, .form-group textarea, .form-group select { width: 100%; padding: .75rem; border: 2px solid #e5e7eb; border-radius: 8px; font-size: 1rem; transition: border-color .2s; }
.form-group input:focus, .form-group textarea:focus, .form-group select:focus { outline: none; border-color: #2a4dd0; }
.modal-actions { display: flex; gap: .5rem; justify-content: flex-end; margin-top: 1.5rem; }

.fade-enter-active, .fade-leave-active { transition: opacity .4s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
@keyframes slideIn { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
@keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }

@media (max-width: 768px) {
  .header { flex-wrap: wrap; }
  .btn-primary { margin-left: 0; width: 100%; }
  .assignment-card { flex-direction: column; }
  .assignment-actions { margin-left: 0; margin-top: 1rem; width: 100%; }
  .assignment-actions button { width: 100%; }
  .form-row { grid-template-columns: 1fr; }
}
</style>