<template>
  <transition name="fade">
    <section class="wrap">
      <header class="header">
        <h1>Cursos Disponibles</h1>
        <div class="header-actions">
          <button v-if="canManage" @click="openEnrollModal" class="btn-outline">
            Inscribir Estudiante
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
        >
          <div class="card-header">
            <h3 @click="canManage && viewCourseDetail(curso.id)" style="cursor: pointer; flex: 1;">
              {{ curso.nombre }}
            </h3>
            <span class="badge">{{ curso.codigo }}</span>
          </div>
          <p class="descripcion">{{ curso.descripcion || 'Sin descripción' }}</p>
          <div class="info">
            <span><strong>Créditos:</strong> {{ curso.creditos }}</span>
            <span v-if="curso.horario"><strong>Horario:</strong> {{ curso.horario }}</span>
          </div>
          <div v-if="canManage" class="card-actions">
            <button @click="openAssignTeacherModal(curso)" class="btn-small btn-primary" title="Asignar Profesor">
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
                <circle cx="12" cy="7" r="4"></circle>
              </svg>
              Profesor
            </button>
            <button @click="viewCourseDetail(curso.id)" class="btn-small btn-info">
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                <circle cx="12" cy="12" r="3"></circle>
              </svg>
              Ver
            </button>
            <button @click="openEditModal(curso)" class="btn-small btn-edit">
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
              </svg>
              Editar
            </button>
            <button @click="confirmDeleteCourse(curso)" class="btn-small btn-danger-small">
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <polyline points="3 6 5 6 21 6"></polyline>
                <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                <line x1="10" y1="11" x2="10" y2="17"></line>
                <line x1="14" y1="11" x2="14" y2="17"></line>
              </svg>
              Eliminar
            </button>
          </div>
        </article>
      </div>

      <!-- Modal para crear/editar curso -->
      <div v-if="showModal" class="modal" @click.self="closeModal">
        <div class="modal-content modal-large">
          <h2>{{ editingCourse ? 'Editar Curso' : 'Crear Nuevo Curso' }}</h2>
          <form @submit.prevent="editingCourse ? updateCourse() : createCourse()" class="form">
            <input v-model="newCourse.nombre" type="text" placeholder="Nombre del curso" required />
            <input v-model="newCourse.codigo" type="text" placeholder="Código (ej: ARQ-101)" required />
            <textarea v-model="newCourse.descripcion" rows="3" placeholder="Descripción"></textarea>
            <input v-model.number="newCourse.creditos" type="number" placeholder="Créditos" min="1" max="10" />
            
            <!-- Componente de horario interactivo -->
            <div class="horario-section">
              <label class="section-label">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="vertical-align: middle; margin-right: 4px;">
                  <rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
                  <line x1="16" y1="2" x2="16" y2="6"></line>
                  <line x1="8" y1="2" x2="8" y2="6"></line>
                  <line x1="3" y1="10" x2="21" y2="10"></line>
                </svg>
                Horario del Curso
              </label>
              
              <div class="horario-builder">
                <!-- Botones para agregar horarios -->
                <div class="horario-actions">
                  <button type="button" @click="addHorarioSlot" class="btn-add-horario">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <line x1="12" y1="5" x2="12" y2="19"></line>
                      <line x1="5" y1="12" x2="19" y2="12"></line>
                    </svg>
                    Agregar Horario
                  </button>
                  <span class="hint-text">Puedes agregar múltiples horarios con horas diferentes</span>
                </div>

                <!-- Lista de horarios -->
                <div v-if="horariosSlots.length === 0" class="empty-horarios">
                  No hay horarios configurados. Haz clic en "Agregar Horario" para comenzar.
                </div>

                <div v-for="(slot, index) in horariosSlots" :key="index" class="horario-slot">
                  <div class="slot-header">
                    <span class="slot-number">Horario #{{ index + 1 }}</span>
                    <button 
                      type="button" 
                      @click="removeHorarioSlot(index)" 
                      class="btn-remove-slot"
                      :disabled="horariosSlots.length === 1"
                    >
                      <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <polyline points="3 6 5 6 21 6"></polyline>
                        <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                        <line x1="10" y1="11" x2="10" y2="17"></line>
                        <line x1="14" y1="11" x2="14" y2="17"></line>
                      </svg>
                    </button>
                  </div>

                  <!-- Selección de días para este slot -->
                  <div class="form-group">
                    <label>Días de clase</label>
                    <div class="dias-grid">
                      <label 
                        v-for="dia in diasSemana" 
                        :key="dia.value" 
                        class="dia-checkbox"
                        :class="{ 
                          active: slot.dias.includes(dia.value),
                          disabled: isDiaUsedInOtherSlot(dia.value, index)
                        }"
                      >
                        <input 
                          type="checkbox" 
                          :value="dia.value" 
                          v-model="slot.dias"
                          :disabled="isDiaUsedInOtherSlot(dia.value, index)"
                        />
                        <span>{{ dia.label }}</span>
                      </label>
                    </div>
                    <span v-if="slot.dias.length === 0" class="validation-hint">
                      Selecciona al menos un día
                    </span>
                  </div>

                  <!-- Hora de inicio y fin para este slot -->
                  <div class="horas-grid">
                    <div class="form-group">
                      <label>Hora inicio</label>
                      <select v-model="slot.horaInicio" required @change="validateHoraFin(index)">
                        <option value="">--</option>
                        <option v-for="hora in horasDisponibles" :key="hora" :value="hora">
                          {{ hora }}
                        </option>
                      </select>
                    </div>

                    <div class="form-group">
                      <label>Hora fin</label>
                      <select 
                        v-model="slot.horaFin" 
                        required
                        :disabled="!slot.horaInicio"
                        @change="validateHoraFin(index)"
                      >
                        <option value="">--</option>
                        <option 
                          v-for="hora in getHorasFinDisponibles(slot.horaInicio)" 
                          :key="hora" 
                          :value="hora"
                        >
                          {{ hora }}
                        </option>
                      </select>
                      <span v-if="slot.horaInicio && !slot.horaFin" class="validation-hint">
                        Selecciona una hora posterior a {{ slot.horaInicio }}
                      </span>
                      <span v-if="slot.horaError" class="error-hint">
                        {{ slot.horaError }}
                      </span>
                    </div>
                  </div>

                  <!-- Preview individual del slot -->
                  <div v-if="getSlotPreview(slot)" class="slot-preview">
                    {{ getSlotPreview(slot) }}
                  </div>
                </div>

                <!-- Preview global del horario -->
                <div v-if="horarioPreviewGlobal" class="horario-preview-global">
                  <strong style="display: inline-flex; align-items: center; gap: .3rem;">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
                      <polyline points="14 2 14 8 20 8"></polyline>
                      <line x1="16" y1="13" x2="8" y2="13"></line>
                      <line x1="16" y1="17" x2="8" y2="17"></line>
                      <polyline points="10 9 9 9 8 9"></polyline>
                    </svg>
                    Horario completo:
                  </strong>
                  <div class="preview-content">{{ horarioPreviewGlobal }}</div>
                </div>
              </div>
            </div>
            
            <div class="modal-actions">
              <button type="button" @click="closeModal" class="btn-outline">Cancelar</button>
              <button type="submit" class="btn-primary" :disabled="creating">
                {{ creating ? (editingCourse ? 'Actualizando...' : 'Creando...') : (editingCourse ? 'Actualizar Curso' : 'Crear Curso') }}
              </button>
            </div>
          </form>
        </div>
      </div>

      <!-- Modal para inscribir usuario -->
      <div v-if="showEnrollModal" class="modal" @click.self="showEnrollModal = false">
        <div class="modal-content">
          <h2>Inscribir Estudiante en Curso</h2>
          <div v-if="enrollError" class="error-box">{{ enrollError }}</div>
          <form @submit.prevent="enrollUser" class="form">
            <div class="form-group">
              <label>Buscar Estudiante</label>
              <input 
                v-model="searchQuery" 
                type="text" 
                placeholder="Buscar por nombre, apellido o email..." 
                class="search-input"
              />
            </div>

            <div class="form-group">
              <label>Seleccionar Estudiante</label>
              <select v-model="enrollment.usuario_id" required size="8" class="select-large">
                <option value="">-- Selecciona un estudiante --</option>
                <option v-for="user in filteredEstudiantes" :key="user.id" :value="user.id">
                  {{ user.nombre }} {{ user.apellido }} - {{ user.email }}
                </option>
              </select>
              <div class="select-hint">{{ filteredEstudiantes.length }} estudiante(s) encontrado(s)</div>
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

      <!-- Modal de confirmación de eliminación -->
      <div v-if="showDeleteConfirm" class="modal" @click.self="showDeleteConfirm = false">
        <div class="modal-content confirm">
          <h2 style="display: inline-flex; align-items: center; gap: .5rem;">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#ef4444" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path>
              <line x1="12" y1="9" x2="12" y2="13"></line>
              <line x1="12" y1="17" x2="12.01" y2="17"></line>
            </svg>
            Confirmar Eliminación
          </h2>
          <p>¿Estás seguro de eliminar el curso <strong>{{ courseToDelete?.nombre }}</strong>?</p>
          <p class="warning-text">Esta acción eliminará también todas las inscripciones asociadas.</p>
          <div class="modal-actions">
            <button @click="showDeleteConfirm = false" class="btn-outline">Cancelar</button>
            <button @click="deleteCourse" class="btn-danger" :disabled="deleting">
              {{ deleting ? 'Eliminando...' : 'Eliminar' }}
            </button>
          </div>
        </div>
      </div>

      <!-- Modal de asignación de profesor -->
      <div v-if="showAssignTeacherModal" class="modal" @click.self="closeAssignTeacherModal">
        <div class="modal-content">
          <h2 style="display: inline-flex; align-items: center; gap: .5rem;">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
              <circle cx="12" cy="7" r="4"></circle>
            </svg>
            Asignar Profesor a Curso
          </h2>
          
          <div v-if="assignTeacherError" class="error-box" style="margin-bottom: 1rem;">
            {{ assignTeacherError }}
          </div>
          
          <div v-if="assignTeacherSuccess" class="success-box" style="margin-bottom: 1rem;">
            {{ assignTeacherSuccess }}
          </div>

          <div class="course-info-box" style="background: var(--color-bg-secondary); padding: 1rem; border-radius: 8px; margin-bottom: 1.5rem;">
            <h3 style="margin: 0 0 0.5rem 0; color: var(--color-primary);">{{ selectedCourseForTeacher?.nombre }}</h3>
            <p style="margin: 0; color: var(--color-text-secondary); font-size: 0.9rem;">{{ selectedCourseForTeacher?.codigo }}</p>
          </div>

          <form @submit.prevent="assignTeacher" class="form">
            <div class="form-group">
              <label>Seleccionar Profesor</label>
              <select v-model="selectedTeacherId" required>
                <option value="">-- Selecciona un profesor --</option>
                <option v-for="profesor in profesores" :key="profesor.id" :value="profesor.id">
                  {{ profesor.nombre }} {{ profesor.apellido }} - {{ profesor.email }}
                </option>
              </select>
              <div class="select-hint" v-if="profesores.length === 0">
                No hay profesores disponibles
              </div>
              <div class="select-hint" v-else>
                {{ profesores.length }} profesor(es) disponible(s)
              </div>
            </div>
            
            <div class="modal-actions">
              <button type="button" @click="closeAssignTeacherModal" class="btn-outline">Cancelar</button>
              <button type="submit" class="btn-primary" :disabled="assigning || !selectedTeacherId">
                {{ assigning ? 'Asignando...' : 'Asignar Profesor' }}
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
import { supabase } from '../supabase'
import { useRouter } from 'vue-router'

const cursos = ref([])
const usuarios = ref([])
const loading = ref(true)
const error = ref(null)
const enrollError = ref(null)
const showModal = ref(false)
const showEnrollModal = ref(false)
const showDeleteConfirm = ref(false)
const showAssignTeacherModal = ref(false)
const creating = ref(false)
const enrolling = ref(false)
const deleting = ref(false)
const assigning = ref(false)
const canManage = ref(false)
const tenantDomain = ref(null)
const editingCourse = ref(null)
const courseToDelete = ref(null)
const selectedCourseForTeacher = ref(null)
const selectedTeacherId = ref('')
const assignTeacherError = ref(null)
const assignTeacherSuccess = ref(null)
const searchQuery = ref('')

// Configuración de horario
const diasSemana = [
  { label: 'Lun', value: 'Lun' },
  { label: 'Mar', value: 'Mar' },
  { label: 'Mié', value: 'Mie' },
  { label: 'Jue', value: 'Jue' },
  { label: 'Vie', value: 'Vie' },
  { label: 'Sáb', value: 'Sab' },
  { label: 'Dom', value: 'Dom' }
]

const horasDisponibles = [
  '07:00', '07:30', '08:00', '08:30', '09:00', '09:30',
  '10:00', '10:30', '11:00', '11:30', '12:00', '12:30',
  '13:00', '13:30', '14:00', '14:30', '15:00', '15:30',
  '16:00', '16:30', '17:00', '17:30', '18:00', '18:30',
  '19:00', '19:30', '20:00', '20:30', '21:00'
]

// Array de slots de horarios (múltiples horarios con días diferentes)
const horariosSlots = ref([
  { dias: [], horaInicio: '', horaFin: '', horaError: '' }
])

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

// Computed property para preview del horario completo
const horarioPreviewGlobal = computed(() => {
  const validSlots = horariosSlots.value.filter(
    slot => slot.dias.length > 0 && slot.horaInicio && slot.horaFin
  )
  
  if (validSlots.length === 0) return ''
  
  return validSlots.map(slot => {
    const diasText = slot.dias.join('-')
    return `${diasText} ${slot.horaInicio}-${slot.horaFin}`
  }).join(', ')
})

// Computed property para filtrar profesores
const profesores = computed(() => {
  return usuarios.value.filter(u => u.rol === 'Profesor')
})

// Computed property para estudiantes (usado en modal de inscripción)
const filteredEstudiantes = computed(() => {
  let estudiantes = usuarios.value.filter(u => u.rol === 'Estudiante')
  
  if (searchQuery.value.trim()) {
    const query = searchQuery.value.toLowerCase()
    estudiantes = estudiantes.filter(u => 
      u.nombre.toLowerCase().includes(query) ||
      u.apellido.toLowerCase().includes(query) ||
      u.email.toLowerCase().includes(query)
    )
  }
  
  return estudiantes
})

// Funciones para gestionar slots de horario
function addHorarioSlot() {
  horariosSlots.value.push({ dias: [], horaInicio: '', horaFin: '', horaError: '' })
}

function removeHorarioSlot(index) {
  if (horariosSlots.value.length > 1) {
    horariosSlots.value.splice(index, 1)
  }
}

function getSlotPreview(slot) {
  if (slot.dias.length === 0 || !slot.horaInicio || !slot.horaFin) {
    return ''
  }
  
  const diasText = slot.dias.join('-')
  return `${diasText} ${slot.horaInicio}-${slot.horaFin}`
}

function isDiaUsedInOtherSlot(dia, currentIndex) {
  return horariosSlots.value.some((slot, index) => 
    index !== currentIndex && slot.dias.includes(dia)
  )
}

// Obtener horas disponibles para la hora fin (solo horas posteriores a hora inicio)
function getHorasFinDisponibles(horaInicio) {
  if (!horaInicio) return horasDisponibles
  
  const indexInicio = horasDisponibles.indexOf(horaInicio)
  if (indexInicio === -1) return horasDisponibles
  
  // Retornar solo las horas posteriores a la hora de inicio
  return horasDisponibles.slice(indexInicio + 1)
}

// Validar que hora fin sea mayor que hora inicio
function validateHoraFin(index) {
  const slot = horariosSlots.value[index]
  
  if (!slot.horaInicio || !slot.horaFin) {
    slot.horaError = ''
    return
  }
  
  const indexInicio = horasDisponibles.indexOf(slot.horaInicio)
  const indexFin = horasDisponibles.indexOf(slot.horaFin)
  
  if (indexFin <= indexInicio) {
    slot.horaError = 'La hora fin debe ser posterior a la hora inicio'
    slot.horaFin = '' // Limpiar hora fin inválida
  } else {
    slot.horaError = ''
  }
}

function getTenantFromEmail(email) {
  if (email.endsWith('@ucb.edu.bo')) return 'ucb.edu.bo'
  if (email.endsWith('@upb.edu.bo')) return 'upb.edu.bo'
  if (email.endsWith('@gmail.com')) return 'gmail.com'
  return null
}

function parseHorario(horarioString) {
  if (!horarioString) {
    return [{ dias: [], horaInicio: '', horaFin: '', horaError: '' }]
  }
  
  // Formato nuevo con múltiples slots: "Lun 10:00-12:00 | Mar-Jue 14:00-16:00"
  // Formato viejo compatible: "Lun-Mie 10:00-12:00"
  
  const slots = horarioString.split('|').map(slot => slot.trim())
  const parsedSlots = []
  
  for (const slot of slots) {
    const match = slot.match(/^([A-Za-z-]+)\s+(\d{2}:\d{2})-(\d{2}:\d{2})$/)
    
    if (match) {
      const dias = match[1].split('-')
      parsedSlots.push({
        dias: dias,
        horaInicio: match[2],
        horaFin: match[3],
        horaError: ''
      })
    }
  }
  
  return parsedSlots.length > 0 ? parsedSlots : [{ dias: [], horaInicio: '', horaFin: '', horaError: '' }]
}

function buildHorarioString() {
  const validSlots = horariosSlots.value.filter(
    slot => slot.dias.length > 0 && slot.horaInicio && slot.horaFin
  )
  
  if (validSlots.length === 0) return ''
  
  return validSlots.map(slot => {
    const diasText = slot.dias.join('-')
    return `${diasText} ${slot.horaInicio}-${slot.horaFin}`
  }).join(' | ')
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
    // Use cookie-based authentication (same as AppLayout)
    const response = await fetch('/auth/user-profile', {
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include'
    })

    if (response.ok) {
      const profile = await response.json()
      console.log('User profile:', profile) // Debug log
      canManage.value = ['Director', 'admin'].includes(profile.rol)
      tenantDomain.value = getTenantFromEmail(profile.email)
      
      console.log('canManage:', canManage.value, 'rol:', profile.rol) // Debug log
      
      if (canManage.value) {
        await fetchUsuarios()
      }
    }
  } catch (e) {
    console.error('Error checking permissions:', e)
  }
}

function openEnrollModal() {
  searchQuery.value = ''
  enrollment.value = { usuario_id: '', curso_id: '' }
  enrollError.value = null
  showEnrollModal.value = true
}

function closeModal() {
  showModal.value = false
  editingCourse.value = null
  newCourse.value = { nombre: '', codigo: '', descripcion: '', creditos: 3, horario: '' }
  horariosSlots.value = [{ dias: [], horaInicio: '', horaFin: '', horaError: '' }]
}

function openEditModal(curso) {
  editingCourse.value = curso
  newCourse.value = {
    nombre: curso.nombre,
    codigo: curso.codigo,
    descripcion: curso.descripcion || '',
    creditos: curso.creditos,
    horario: curso.horario || ''
  }
  
  // Parsear horario existente
  horariosSlots.value = parseHorario(curso.horario)
  
  showModal.value = true
}

function confirmDeleteCourse(curso) {
  courseToDelete.value = curso
  showDeleteConfirm.value = true
}

async function createCourse() {
  try {
    creating.value = true
    error.value = null
    
    // Validar que todos los slots tengan horas válidas
    const hasInvalidHoras = horariosSlots.value.some(slot => {
      if (slot.dias.length > 0 && slot.horaInicio && slot.horaFin) {
        const indexInicio = horasDisponibles.indexOf(slot.horaInicio)
        const indexFin = horasDisponibles.indexOf(slot.horaFin)
        return indexFin <= indexInicio
      }
      return false
    })
    
    if (hasInvalidHoras) {
      error.value = 'La hora fin debe ser posterior a la hora inicio en todos los horarios'
      creating.value = false
      return
    }
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    // Construir el horario desde horariosSlots
    newCourse.value.horario = buildHorarioString()

    const response = await fetch(`${COURSES_API}/api/courses`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${session.access_token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(newCourse.value)
    })

    if (response.ok) {
      closeModal()
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

async function updateCourse() {
  try {
    creating.value = true
    error.value = null
    
    // Validar que todos los slots tengan horas válidas
    const hasInvalidHoras = horariosSlots.value.some(slot => {
      if (slot.dias.length > 0 && slot.horaInicio && slot.horaFin) {
        const indexInicio = horasDisponibles.indexOf(slot.horaInicio)
        const indexFin = horasDisponibles.indexOf(slot.horaFin)
        return indexFin <= indexInicio
      }
      return false
    })
    
    if (hasInvalidHoras) {
      error.value = 'La hora fin debe ser posterior a la hora inicio en todos los horarios'
      creating.value = false
      return
    }
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    // Construir el horario desde horariosSlots
    newCourse.value.horario = buildHorarioString()

    const response = await fetch(`${COURSES_API}/api/courses/${editingCourse.value.id}`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${session.access_token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(newCourse.value)
    })

    if (response.ok) {
      closeModal()
      await fetchCourses()
    } else {
      const data = await response.json()
      error.value = data.detail || 'Error al actualizar curso'
    }
  } catch (e) {
    console.error('Error updating course:', e)
    error.value = 'Error al actualizar curso'
  } finally {
    creating.value = false
  }
}

async function deleteCourse() {
  try {
    deleting.value = true
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const response = await fetch(`${COURSES_API}/api/courses/${courseToDelete.value.id}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${session.access_token}`
      }
    })

    if (response.ok) {
      showDeleteConfirm.value = false
      courseToDelete.value = null
      await fetchCourses()
    } else {
      const data = await response.json()
      alert(data.detail || 'Error al eliminar curso')
    }
  } catch (e) {
    console.error('Error deleting course:', e)
    alert('Error al eliminar curso')
  } finally {
    deleting.value = false
  }
}

function openAssignTeacherModal(curso) {
  selectedCourseForTeacher.value = curso
  selectedTeacherId.value = ''
  assignTeacherError.value = null
  assignTeacherSuccess.value = null
  showAssignTeacherModal.value = true
}

function closeAssignTeacherModal() {
  showAssignTeacherModal.value = false
  selectedCourseForTeacher.value = null
  selectedTeacherId.value = ''
  assignTeacherError.value = null
  assignTeacherSuccess.value = null
}

async function assignTeacher() {
  try {
    assigning.value = true
    assignTeacherError.value = null
    assignTeacherSuccess.value = null
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) {
      assignTeacherError.value = 'No estás autenticado'
      return
    }

    const response = await fetch(
      `${COURSES_API}/api/courses/${selectedCourseForTeacher.value.id}/assign-teacher?profesor_id=${selectedTeacherId.value}`, 
      {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${session.access_token}`,
          'Content-Type': 'application/json'
        }
      }
    )

    if (response.ok) {
      assignTeacherSuccess.value = '✅ Profesor asignado exitosamente'
      
      // Refresh courses to show updated teacher
      await fetchCourses()
      
      // Close modal after 1.5 seconds
      setTimeout(() => {
        closeAssignTeacherModal()
      }, 1500)
    } else {
      const data = await response.json()
      assignTeacherError.value = data.detail || 'Error al asignar profesor'
    }
  } catch (e) {
    console.error('Error assigning teacher:', e)
    assignTeacherError.value = 'Error de conexión al asignar profesor'
  } finally {
    assigning.value = false
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
      searchQuery.value = ''
      alert('Estudiante inscrito exitosamente')
    } else {
      const data = await response.json()
      enrollError.value = data.detail || 'Error al inscribir estudiante'
    }
  } catch (e) {
    console.error('Error enrolling user:', e)
    enrollError.value = 'Error al inscribir estudiante'
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
.card:hover { transform: translateY(-2px); box-shadow: 0 6px 16px rgba(0,0,0,.08); }
.card-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: .75rem; gap: .5rem; }
.card h3 { color: #2a4dd0; margin: 0; }
.badge { background: #eef2ff; color: #2a4dd0; padding: .25rem .5rem; border-radius: 6px; font-size: .85rem; font-weight: 600; white-space: nowrap; }
.descripcion { color: #666; margin: .5rem 0; }
.info { display: flex; flex-direction: column; gap: .25rem; font-size: .9rem; color: #555; margin-bottom: .75rem; }
.btn-primary { background: #2a4dd0; color: #fff; border: none; padding: .6rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; transition: transform .15s, box-shadow .2s; }
.btn-primary:hover { transform: translateY(-1px); box-shadow: 0 6px 20px rgba(42,77,208,.2); }
.btn-primary:disabled { opacity: .5; cursor: not-allowed; transform: none; }
.btn-outline { background: #fff; color: #2a4dd0; border: 2px solid #2a4dd0; padding: .6rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; transition: background .2s, color .2s; }
.btn-outline:hover { background: #eef2ff; }

/* Botones de tarjeta */
.card-actions { display: flex; gap: .4rem; margin-top: .75rem; padding-top: .75rem; border-top: 1px solid #e5e7eb; flex-wrap: wrap; }
.btn-small { padding: .4rem .6rem; border-radius: 6px; font-size: .85rem; font-weight: 600; cursor: pointer; border: none; transition: all .2s; display: inline-flex; align-items: center; gap: .3rem; }
.btn-small svg { vertical-align: middle; }
.btn-info { background: #3b82f6; color: #fff; }
.btn-info:hover { background: #2563eb; transform: translateY(-1px); }
.btn-edit { background: #f59e0b; color: #fff; }
.btn-edit:hover { background: #d97706; transform: translateY(-1px); }
.btn-danger-small { background: #ef4444; color: #fff; }
.btn-danger-small:hover { background: #dc2626; transform: translateY(-1px); }
.btn-danger { background: #ef4444; color: #fff; border: none; padding: .6rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; }
.btn-danger:hover { background: #dc2626; }
.btn-danger:disabled { opacity: .5; cursor: not-allowed; }

/* Modal */
.modal { position: fixed; inset: 0; background: rgba(0,0,0,.5); display: grid; place-items: center; z-index: 100; animation: fadeIn .3s; }
.modal-content { background: #fff; border-radius: 16px; padding: 1.5rem; max-width: 500px; width: 90%; max-height: 90vh; overflow-y: auto; animation: scaleIn .3s; }
.modal-content.modal-large { max-width: 650px; }
.modal-content.confirm { max-width: 400px; text-align: center; }
.modal-content h2 { margin-bottom: 1rem; color: #2a4dd0; }
.form { display: flex; flex-direction: column; gap: .75rem; }
.form-group { display: flex; flex-direction: column; gap: .25rem; }
.form-group label { font-weight: 600; color: #555; font-size: .9rem; }
.form input, .form textarea, .form select { border: 1px solid #e5e7eb; border-radius: 8px; padding: .75rem; font: inherit; transition: border-color .15s, box-shadow .2s; }
.form input:focus, .form textarea:focus, .form select:focus { outline: none; border-color: #2a4dd0; box-shadow: 0 0 0 3px rgba(42,77,208,.1); }
.search-input { background: #f9fafb; }
.select-large { min-height: 200px; }
.select-hint { font-size: .85rem; color: #666; margin-top: .25rem; }
.modal-actions { display: flex; gap: .5rem; justify-content: flex-end; margin-top: .5rem; }
.warning-text { color: #d97706; font-size: .9rem; margin-top: .5rem; }

/* Horario interactivo */
.horario-section { background: #f9fafb; border-radius: 12px; padding: 1rem; border: 2px dashed #e5e7eb; }
.section-label { display: inline-flex; align-items: center; font-weight: 700; color: #2a4dd0; font-size: 1rem; margin-bottom: .75rem; }
.section-label svg { flex-shrink: 0; }
.horario-builder { display: flex; flex-direction: column; gap: 1rem; }

/* Acciones de horario */
.horario-actions { display: flex; flex-direction: column; gap: .5rem; align-items: flex-start; }
.btn-add-horario { background: #10b981; color: #fff; border: none; padding: .6rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; transition: all .2s; font-size: .9rem; display: inline-flex; align-items: center; gap: .4rem; }
.btn-add-horario svg { flex-shrink: 0; }
.btn-add-horario:hover { background: #059669; transform: translateY(-1px); }
.hint-text { font-size: .85rem; color: #666; font-style: italic; }
.empty-horarios { text-align: center; padding: 1.5rem; color: #999; background: #fff; border-radius: 8px; border: 2px dashed #e5e7eb; }

/* Slots de horario */
.horario-slot { background: #fff; border-radius: 10px; padding: 1rem; border: 2px solid #e5e7eb; position: relative; }
.slot-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: .75rem; padding-bottom: .5rem; border-bottom: 1px solid #e5e7eb; }
.slot-number { font-weight: 700; color: #2a4dd0; font-size: .95rem; }
.btn-remove-slot { background: #ef4444; color: #fff; border: none; padding: .3rem .6rem; border-radius: 6px; cursor: pointer; font-size: .85rem; transition: background .2s; display: inline-flex; align-items: center; justify-content: center; }
.btn-remove-slot svg { display: block; }
.btn-remove-slot:hover:not(:disabled) { background: #dc2626; }
.btn-remove-slot:disabled { opacity: .3; cursor: not-allowed; }

/* Días */
.dias-grid { display: grid; grid-template-columns: repeat(7, 1fr); gap: .5rem; }
.dia-checkbox { display: flex; align-items: center; justify-content: center; padding: .5rem; background: #fff; border: 2px solid #e5e7eb; border-radius: 8px; cursor: pointer; transition: all .2s; text-align: center; }
.dia-checkbox input { display: none; }
.dia-checkbox span { font-weight: 600; font-size: .85rem; color: #666; }
.dia-checkbox:hover:not(.disabled) { border-color: #2a4dd0; background: #eef2ff; }
.dia-checkbox.active { border-color: #2a4dd0; background: #2a4dd0; }
.dia-checkbox.active span { color: #fff; }
.dia-checkbox.disabled { opacity: .4; cursor: not-allowed; background: #f5f5f5; }
.dia-checkbox.disabled span { color: #999; }
.validation-hint { font-size: .8rem; color: #ef4444; margin-top: .25rem; display: block; }
.error-hint { font-size: .8rem; color: #dc2626; margin-top: .25rem; display: block; font-weight: 600; }

/* Horas */
.horas-grid { display: grid; grid-template-columns: 1fr 1fr; gap: .75rem; }

/* Previews */
.slot-preview { background: #eef2ff; border: 1px solid #2a4dd0; border-radius: 6px; padding: .5rem; text-align: center; color: #2a4dd0; font-weight: 600; font-size: .9rem; margin-top: .5rem; }
.horario-preview-global { background: #fff; border: 2px solid #10b981; border-radius: 10px; padding: 1rem; }
.horario-preview-global strong { color: #10b981; display: block; margin-bottom: .5rem; }
.preview-content { background: #f0fdf4; padding: .75rem; border-radius: 6px; color: #065f46; font-weight: 600; word-break: break-word; line-height: 1.6; }

.card.clickable { cursor: default; }

/* Teacher Assignment Styles */
.success-box {
  background: #d1fae5;
  color: #065f46;
  padding: 1rem;
  border-radius: 8px;
  border-left: 4px solid #10b981;
  font-weight: 500;
}

.error-box {
  background: #fee2e2;
  color: #991b1b;
  padding: 1rem;
  border-radius: 8px;
  border-left: 4px solid #ef4444;
  font-weight: 500;
}

.course-info-box h3 {
  margin: 0 0 0.5rem 0;
  font-size: 1.1rem;
}

.course-info-box p {
  margin: 0;
  font-size: 0.9rem;
  opacity: 0.8;
}

@keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
@keyframes scaleIn { from { transform: scale(.95); opacity: 0; } to { transform: scale(1); opacity: 1; } }

.fade-enter-active, .fade-leave-active { transition: opacity .4s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
@keyframes slideIn { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }

@media (max-width: 600px) {
  .header { flex-direction: column; align-items: stretch; }
  .header-actions { flex-direction: column; }
  .card-actions { justify-content: center; flex-wrap: wrap; }
  .dias-grid { grid-template-columns: repeat(4, 1fr); }
  .horas-grid { grid-template-columns: 1fr; }
}
</style>
