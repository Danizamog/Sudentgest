<template>
  <transition name="fade">
    <section class="wrap">
      <header class="header">
        <h1>Cursos Disponibles</h1>
        <div class="header-actions">
          <button v-if="canManage" @click="showEnrollModal = true" class="btn-outline">
            Inscribir Usuario
          </button>
          <button v-if="canManage" @click="showModal = true" class="btn-primary">
            + Crear Curso
          </button>
        </div>
      </header>

      <div v-if="loading" class="loading">Cargando cursos...</div>
      <div v-else-if="error" class="error">{{ error }}</div>
      
      <div v-else class="grid">
        <article 
          v-for="curso in cursos" 
          :key="curso.id" 
          class="card"
          :class="{ clickable: canManage }"
          @click="canManage && viewCourseDetail(curso.id)"
        >
          <div class="card-header">
            <h3>{{ curso.nombre }}</h3>
            <span class="badge">{{ curso.codigo }}</span>
          </div>
          <p class="descripcion">{{ curso.descripcion || 'Sin descripción' }}</p>
          <div class="info">
            <span><strong>Créditos:</strong> {{ curso.creditos }}</span>
            <span v-if="curso.horario"><strong>Horario:</strong> {{ curso.horario }}</span>
          </div>
          <div v-if="canManage" class="card-hint">
            Click para ver detalles
          </div>
        </article>
      </div>

      <!-- Modal para crear curso -->
      <div v-if="showModal" class="modal" @click.self="showModal = false">
        <div class="modal-content">
          <h2>Crear Nuevo Curso</h2>
          <form @submit.prevent="createCourse" class="form">
            <input v-model="newCourse.nombre" type="text" placeholder="Nombre del curso" required />
            <input v-model="newCourse.codigo" type="text" placeholder="Código (ej: ARQ-101)" required />
            <textarea v-model="newCourse.descripcion" rows="3" placeholder="Descripción"></textarea>
            <input v-model.number="newCourse.creditos" type="number" placeholder="Créditos" min="1" max="10" />
            <input v-model="newCourse.horario" type="text" placeholder="Horario (ej: Lun-Mie 10:00-12:00)" />
            
            <div class="modal-actions">
              <button type="button" @click="showModal = false" class="btn-outline">Cancelar</button>
              <button type="submit" class="btn-primary" :disabled="creating">
                {{ creating ? 'Creando...' : 'Crear Curso' }}
              </button>
            </div>
          </form>
        </div>
      </div>

      <!-- Modal para inscribir usuario -->
      <div v-if="showEnrollModal" class="modal" @click.self="showEnrollModal = false">
        <div class="modal-content">
          <h2>Inscribir Usuario en Curso</h2>
          <div v-if="enrollError" class="error-box">{{ enrollError }}</div>
          <form @submit.prevent="enrollUser" class="form">
            <div class="form-group">
              <label>Seleccionar Usuario</label>
              <select v-model="enrollment.usuario_id" required>
                <option value="">-- Selecciona un usuario --</option>
                <option v-for="user in usuarios" :key="user.id" :value="user.id">
                  {{ user.nombre }} {{ user.apellido }} ({{ user.email }}) - {{ user.rol }}
                </option>
              </select>
            </div>

            <div class="form-group">
              <label>Seleccionar Curso</label>
              <select v-model="enrollment.curso_id" required>
                <option value="">-- Selecciona un curso --</option>
                <option v-for="curso in cursos" :key="curso.id" :value="curso.id">
                  {{ curso.codigo }} - {{ curso.nombre }}
                </option>
              </select>
            </div>
            
            <div class="modal-actions">
              <button type="button" @click="showEnrollModal = false" class="btn-outline">Cancelar</button>
              <button type="submit" class="btn-primary" :disabled="enrolling">
                {{ enrolling ? 'Inscribiendo...' : 'Inscribir' }}
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
import { supabase } from '../supabase'
import { useRouter } from 'vue-router'

const cursos = ref([])
const usuarios = ref([])
const loading = ref(true)
const error = ref(null)
const enrollError = ref(null)
const showModal = ref(false)
const showEnrollModal = ref(false)
const creating = ref(false)
const enrolling = ref(false)
const canManage = ref(false)
const tenantDomain = ref(null)

const newCourse = ref({
  nombre: '',
  codigo: '',
  descripcion: '',
  creditos: 3,
  horario: ''
})

const enrollment = ref({
  usuario_id: '',
  curso_id: ''
})

const COURSES_API = import.meta.env.VITE_COURSES_API || 'http://localhost:5008'
const AUTH_API = 'http://localhost:5002'
const router = useRouter()

function getTenantFromEmail(email) {
  if (email.endsWith('@ucb.edu.bo')) return 'ucb.edu.bo'
  if (email.endsWith('@upb.edu.bo')) return 'upb.edu.bo'
  if (email.endsWith('@gmail.com')) return 'gmail.com'
  return null
}

async function fetchCourses() {
  try {
    loading.value = true
    error.value = null
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) {
      error.value = 'No estás autenticado'
      return
    }

    const response = await fetch(`${COURSES_API}/api/courses`, {
      headers: {
        'Authorization': `Bearer ${session.access_token}`
      }
    })

    if (response.ok) {
      const data = await response.json()
      cursos.value = data.cursos || []
    } else {
      error.value = 'Error al cargar cursos'
    }
  } catch (e) {
    console.error('Error fetching courses:', e)
    error.value = 'Error de conexión'
  } finally {
    loading.value = false
  }
}

async function fetchUsuarios() {
  try {
    const { data: { session } } = await supabase.auth.getSession()
    if (!session || !tenantDomain.value) return

    const response = await fetch(`${AUTH_API}/api/usuarios/${tenantDomain.value}`)
    
    if (response.ok) {
      const data = await response.json()
      usuarios.value = data.usuarios || []
    }
  } catch (e) {
    console.error('Error fetching usuarios:', e)
  }
}

async function checkPermissions() {
  try {
    const token = localStorage.getItem('token')
    if (!token) return

    const response = await fetch(`${AUTH_API}/api/auth/user-profile`, {
      headers: { 'Authorization': `Bearer ${token}` }
    })

    if (response.ok) {
      const profile = await response.json()
      canManage.value = ['Director', 'admin'].includes(profile.rol)
      tenantDomain.value = getTenantFromEmail(profile.email)
      
      if (canManage.value) {
        await fetchUsuarios()
      }
    }
  } catch (e) {
    console.error('Error checking permissions:', e)
  }
}

async function createCourse() {
  try {
    creating.value = true
    error.value = null
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const response = await fetch(`${COURSES_API}/api/courses`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${session.access_token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(newCourse.value)
    })

    if (response.ok) {
      showModal.value = false
      newCourse.value = { nombre: '', codigo: '', descripcion: '', creditos: 3, horario: '' }
      await fetchCourses()
    } else {
      const data = await response.json()
      error.value = data.detail || 'Error al crear curso'
    }
  } catch (e) {
    console.error('Error creating course:', e)
    error.value = 'Error al crear curso'
  } finally {
    creating.value = false
  }
}

async function enrollUser() {
  try {
    enrolling.value = true
    enrollError.value = null
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) {
      enrollError.value = 'No estás autenticado'
      return
    }

    const response = await fetch(`${COURSES_API}/api/courses/enroll`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${session.access_token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        usuario_id: parseInt(enrollment.value.usuario_id),
        curso_id: parseInt(enrollment.value.curso_id)
      })
    })

    if (response.ok) {
      showEnrollModal.value = false
      enrollment.value = { usuario_id: '', curso_id: '' }
      alert('Usuario inscrito exitosamente')
    } else {
      const data = await response.json()
      enrollError.value = data.detail || 'Error al inscribir usuario'
    }
  } catch (e) {
    console.error('Error enrolling user:', e)
    enrollError.value = 'Error al inscribir usuario'
  } finally {
    enrolling.value = false
  }
}

function viewCourseDetail(cursoId) {
  router.push(`/courses/${cursoId}`)
}

onMounted(async () => {
  await Promise.all([fetchCourses(), checkPermissions()])
})
</script>

<style scoped>
.wrap { max-width: 1100px; margin: 2rem auto; padding: 0 1rem; animation: slideIn .7s; }
.header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; flex-wrap: wrap; gap: .5rem; }
.header-actions { display: flex; gap: .5rem; }
.loading, .error { text-align: center; padding: 2rem; }
.error { color: #ef4444; }
.error-box { background: #fee; color: #c00; padding: .75rem; border-radius: 8px; margin-bottom: 1rem; border: 1px solid #fcc; }
.grid { display: grid; gap: 1rem; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); }
.card { background: #fff; border-radius: 12px; padding: 1.25rem; box-shadow: 0 2px 8px rgba(0,0,0,.06); transition: transform .18s ease, box-shadow .2s ease; }
.card:hover { transform: translateY(-4px); box-shadow: 0 10px 24px rgba(0,0,0,.08); }
.card-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: .75rem; }
.card h3 { color: #2a4dd0; margin: 0; }
.badge { background: #eef2ff; color: #2a4dd0; padding: .25rem .5rem; border-radius: 6px; font-size: .85rem; font-weight: 600; }
.descripcion { color: #666; margin: .5rem 0; }
.info { display: flex; flex-direction: column; gap: .25rem; font-size: .9rem; color: #555; }
.btn-primary { background: #2a4dd0; color: #fff; border: none; padding: .6rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; transition: transform .15s, box-shadow .2s; }
.btn-primary:hover { transform: translateY(-1px); box-shadow: 0 6px 20px rgba(42,77,208,.2); }
.btn-outline { background: #fff; color: #2a4dd0; border: 2px solid #2a4dd0; padding: .6rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; transition: background .2s, color .2s; }
.btn-outline:hover { background: #eef2ff; }

/* Modal */
.modal { position: fixed; inset: 0; background: rgba(0,0,0,.5); display: grid; place-items: center; z-index: 100; animation: fadeIn .3s; }
.modal-content { background: #fff; border-radius: 16px; padding: 1.5rem; max-width: 500px; width: 90%; animation: scaleIn .3s; }
.modal-content h2 { margin-bottom: 1rem; color: #2a4dd0; }
.form { display: flex; flex-direction: column; gap: .75rem; }
.form-group { display: flex; flex-direction: column; gap: .25rem; }
.form-group label { font-weight: 600; color: #555; font-size: .9rem; }
.form input, .form textarea, .form select { border: 1px solid #e5e7eb; border-radius: 8px; padding: .75rem; font: inherit; transition: border-color .15s, box-shadow .2s; }
.form input:focus, .form textarea:focus, .form select:focus { outline: none; border-color: #2a4dd0; box-shadow: 0 0 0 3px rgba(42,77,208,.1); }
.modal-actions { display: flex; gap: .5rem; justify-content: flex-end; margin-top: .5rem; }

.card.clickable { cursor: pointer; }
.card.clickable:hover { transform: translateY(-6px); box-shadow: 0 12px 28px rgba(0,0,0,.1); }
.card-hint { margin-top: .75rem; padding-top: .75rem; border-top: 1px solid #e5e7eb; font-size: .85rem; color: #2a4dd0; text-align: center; }

@keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
@keyframes scaleIn { from { transform: scale(.95); opacity: 0; } to { transform: scale(1); opacity: 1; } }

.fade-enter-active, .fade-leave-active { transition: opacity .4s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
@keyframes slideIn { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }

@media (max-width: 600px) {
  .header { flex-direction: column; align-items: stretch; }
  .header-actions { flex-direction: column; }
}
</style>
