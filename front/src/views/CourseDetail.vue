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
                🗑️ Eliminar
              </button>
            </article>
          </div>
        </div>
      </div>

      <!-- Modal de confirmación -->
      <div v-if="showConfirm" class="modal" @click.self="showConfirm = false">
        <div class="modal-content confirm">
          <h2>⚠️ Confirmar eliminación</h2>
          <p>¿Estás seguro de eliminar a <strong>{{ inscritoToDelete?.nombre }} {{ inscritoToDelete?.apellido }}</strong> del curso?</p>
          <div class="modal-actions">
            <button @click="showConfirm = false" class="btn-outline">Cancelar</button>
            <button @click="deleteInscrito" class="btn-danger" :disabled="deleting">
              {{ deleting ? 'Eliminando...' : 'Eliminar' }}
            </button>
          </div>
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
const loading = ref(true)
const error = ref(null)
const deleting = ref(false)
const showConfirm = ref(false)
const inscritoToDelete = ref(null)

const COURSES_API = import.meta.env.VITE_COURSES_API || 'http://localhost:5008'

function viewAssignments() {
  const cursoId = route.params.id
  router.push(`/courses/${cursoId}/assignments`)
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

    const cursoId = route.params.id

    // Obtener datos del curso
    const coursesResponse = await fetch(`${COURSES_API}/api/courses`, {
      headers: { 'Authorization': `Bearer ${session.access_token}` }
    })

    if (coursesResponse.ok) {
      const data = await coursesResponse.json()
      curso.value = data.cursos.find(c => c.id === parseInt(cursoId))
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
  } catch (e) {
    console.error('Error fetching course detail:', e)
    error.value = 'Error de conexión'
  } finally {
    loading.value = false
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
.empty { color: #666; }

.course-info { display: grid; grid-template-columns: repeat(3, 1fr); gap: 1rem; background: #fff; border-radius: 12px; padding: 1.5rem; box-shadow: 0 2px 8px rgba(0,0,0,.06); margin-bottom: 2rem; }
.info-item { padding: .5rem 0; }
.info-item.full { grid-column: 1 / -1; }
.info-item strong { color: #2a4dd0; }

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
.btn-delete { background: #ef4444; color: #fff; border: none; padding: .5rem .75rem; border-radius: 8px; cursor: pointer; font-size: .9rem; transition: background .2s; }
.btn-delete:hover { background: #dc2626; }
.btn-delete:disabled { opacity: .5; cursor: not-allowed; }

/* Modal */
.modal { position: fixed; inset: 0; background: rgba(0,0,0,.5); display: grid; place-items: center; z-index: 100; animation: fadeIn .3s; }
.modal-content.confirm { background: #fff; border-radius: 16px; padding: 1.5rem; max-width: 400px; width: 90%; text-align: center; }
.modal-content h2 { margin-bottom: .75rem; }
.modal-content p { margin-bottom: 1.5rem; color: #555; }
.modal-actions { display: flex; gap: .5rem; justify-content: center; }
.btn-outline { background: #fff; color: #2a4dd0; border: 2px solid #2a4dd0; padding: .6rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; }
.btn-danger { background: #ef4444; color: #fff; border: none; padding: .6rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; }
.btn-danger:disabled { opacity: .5; cursor: not-allowed; }

.fade-enter-active, .fade-leave-active { transition: opacity .4s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
@keyframes slideIn { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
@keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }

@media (max-width: 900px) {
  .course-info { grid-template-columns: repeat(2, 1fr); }
  .header { flex-wrap: wrap; }
  .btn-assignments { margin-left: 0; }
}
@media (max-width: 600px) {
  .course-info { grid-template-columns: 1fr; }
  .header { flex-direction: column; align-items: flex-start; gap: 1rem; }
  .btn-assignments { width: 100%; }
}
</style>