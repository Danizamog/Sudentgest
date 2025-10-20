<template>
  <transition name="fade">
    <section class="wrap">
      <h1>Registro de Asistencia</h1>
      <p class="subtitle">Gestión de asistencias para profesores</p>

      <div v-if="error" class="error-box">{{ error }}</div>

      <!-- Mensaje si no hay cursos -->
      <div v-if="!loadingCourses && cursos.length === 0" class="empty-state">
        <p>📚 No tienes cursos asignados.</p>
        <p class="hint">Contacta al director para que te asigne cursos.</p>
      </div>

      <template v-else>
        <!-- Selector de curso y fecha -->
        <div class="course-selector">
          <div class="form-group">
            <label>Selecciona un curso</label>
            <select v-model="selectedCursoId" @change="loadStudentsByCourse" :disabled="loadingCourses">
              <option value="">{{ loadingCourses ? 'Cargando...' : '-- Selecciona un curso --' }}</option>
              <option v-for="curso in cursos" :key="curso.id" :value="curso.id">
                {{ curso.codigo }} - {{ curso.nombre }}
              </option>
            </select>
          </div>

          <div class="form-group">
            <label>Fecha</label>
            <input v-model="selectedDate" type="date" required />
          </div>
        </div>

        <!-- Tabla CRUD de estudiantes -->
        <div v-if="selectedCursoId && students.length > 0" class="table-container">
          <div class="table-header">
            <h2>Estudiantes ({{ students.length }})</h2>
            <button @click="saveAllAttendance" class="btn-save" :disabled="saving">
              {{ saving ? 'Guardando...' : '💾 Guardar Todo' }}
            </button>
          </div>

          <div class="table-wrapper">
            <table class="attendance-table">
              <thead>
                <tr>
                  <th class="col-student">Estudiante</th>
                  <th class="col-email">Email</th>
                  <th class="col-status">Estado</th>
                  <th class="col-obs">Observaciones</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="student in students" :key="student.id">
                  <td class="student-name">
                    <div class="name-cell">
                      {{ student.nombre }} {{ student.apellido }}
                    </div>
                  </td>
                  <td class="student-email">{{ student.email }}</td>
                  <td class="status-cell">
                    <select 
                      v-model="attendanceRecords[student.id].estado" 
                      class="status-select"
                      :class="`status-${attendanceRecords[student.id].estado}`"
                    >
                      <option value="presente">✓ Presente</option>
                      <option value="ausente">✗ Ausente</option>
                      <option value="tardanza">⏰ Tardanza</option>
                    </select>
                  </td>
                  <td class="obs-cell">
                    <input 
                      v-model="attendanceRecords[student.id].observaciones" 
                      type="text" 
                      placeholder="Observaciones..."
                      class="obs-input"
                      maxlength="200"
                    />
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div v-else-if="selectedCursoId && !loadingStudents && students.length === 0" class="empty">
          📋 No hay estudiantes inscritos en este curso
        </div>

        <div v-if="loadingStudents" class="loading">
          <div class="spinner"></div>
          Cargando estudiantes...
        </div>
      </template>
    </section>
  </transition>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { supabase } from '../supabase'

const cursos = ref([])
const students = ref([])
const selectedCursoId = ref('')
const selectedDate = ref(new Date().toISOString().split('T')[0])
const attendanceRecords = reactive({})
const loadingCourses = ref(true)
const loadingStudents = ref(false)
const saving = ref(false)
const error = ref(null)

// 🔹 URLs dinámicas
const getCoursesAPI = () => window.location.hostname === 'localhost' ? 'http://localhost:5008' : window.location.origin
const getAttendanceAPI = () => window.location.hostname === 'localhost' ? 'http://localhost:5004' : window.location.origin
const getAuthAPI = () => window.location.hostname === 'localhost' ? 'http://localhost:5002' : window.location.origin

function getTenantFromEmail(email) {
  if (email.endsWith('@ucb.edu.bo')) return 'ucb.edu.bo'
  if (email.endsWith('@upb.edu.bo')) return 'upb.edu.bo'
  if (email.endsWith('@gmail.com')) return 'gmail.com'
  return null
}

async function fetchMyCourses() {
  try {
    loadingCourses.value = true
    error.value = null
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) {
      error.value = 'No estás autenticado'
      return
    }

    const response = await fetch(`${getCoursesAPI()}/api/courses/my-courses`, {
      headers: { 'Authorization': `Bearer ${session.access_token}` },
      credentials: 'include'
    })

    if (response.ok) {
      const data = await response.json()
      cursos.value = data.cursos || []
    } else {
      const errorData = await response.json()
      error.value = errorData.detail || 'Error al cargar cursos'
    }
  } catch (e) {
    console.error('Error fetching courses:', e)
    error.value = 'Error de conexión'
  } finally {
    loadingCourses.value = false
  }
}

async function loadStudentsByCourse() {
  if (!selectedCursoId.value) {
    students.value = []
    return
  }

  try {
    loadingStudents.value = true
    error.value = null
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const tenant = getTenantFromEmail(session.user.email)
    if (!tenant) return

    const studentsResponse = await fetch(
      `${getCoursesAPI()}/api/courses/${selectedCursoId.value}/students`,
      { 
        headers: { 'Authorization': `Bearer ${session.access_token}` },
        credentials: 'include'
      }
    )

    if (!studentsResponse.ok) {
      error.value = 'Error al obtener estudiantes del curso'
      students.value = []
      return
    }

    const studentsData = await studentsResponse.json()
    const enrolledIds = studentsData.inscripciones?.map(i => i.usuario_id) || []

    if (enrolledIds.length === 0) {
      students.value = []
      return
    }

    const usersResponse = await fetch(`${getAuthAPI()}/api/usuarios/${tenant}`, {
      credentials: 'include'
    })
    
    if (usersResponse.ok) {
      const usersData = await usersResponse.json()
      const allUsers = usersData.usuarios || []

      students.value = allUsers.filter(u => 
        enrolledIds.includes(u.id) && 
        (u.rol?.toLowerCase() === 'estudiante')
      ).sort((a, b) => 
        `${a.apellido} ${a.nombre}`.localeCompare(`${b.apellido} ${b.nombre}`)
      )

      students.value.forEach(student => {
        attendanceRecords[student.id] = {
          estado: 'presente',
          observaciones: ''
        }
      })
    }
  } catch (e) {
    console.error('Error loading students:', e)
    error.value = 'Error al cargar estudiantes'
  } finally {
    loadingStudents.value = false
  }
}

async function saveAllAttendance() {
  if (!confirm(`¿Guardar asistencia de ${students.value.length} estudiantes?`)) {
    return
  }

  try {
    saving.value = true
    error.value = null

    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const promises = students.value.map(student => {
      return fetch(`${getAttendanceAPI()}/api/attendance/register`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${session.access_token}`,
          'Content-Type': 'application/json'
        },
        credentials: 'include',
        body: JSON.stringify({
          estudiante_id: student.id,
          curso_id: parseInt(selectedCursoId.value),
          fecha: selectedDate.value,
          estado: attendanceRecords[student.id].estado,
          observaciones: attendanceRecords[student.id].observaciones || null
        })
      })
    })

    const results = await Promise.allSettled(promises)
    const successful = results.filter(r => r.status === 'fulfilled' && r.value.ok).length
    const failed = results.length - successful

    if (failed === 0) {
      alert(`✅ ${successful} asistencias guardadas exitosamente`)
      students.value.forEach(student => {
        attendanceRecords[student.id].observaciones = ''
      })
    } else {
      alert(`⚠️ ${successful} guardadas, ${failed} fallaron`)
      error.value = `${failed} asistencias no se pudieron guardar`
    }
  } catch (e) {
    console.error('Error saving attendance:', e)
    error.value = 'Error al guardar asistencias'
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  fetchMyCourses()
})
</script>

<style scoped>
.wrap { max-width: 1400px; margin: 2rem auto; padding: 0 1rem; animation: slideIn .7s; }
.subtitle { color: #666; margin-bottom: 1.5rem; }
.error-box { background: #fee; color: #c00; padding: .75rem; border-radius: 8px; margin-bottom: 1rem; border: 1px solid #fcc; }
.loading { text-align: center; padding: 2rem; color: #666; display: flex; align-items: center; justify-content: center; gap: 1rem; }
.empty { text-align: center; padding: 2rem; color: #666; background: #f8fafc; border-radius: 12px; }
.empty-state { background: #f8fafc; border: 2px dashed #cbd5e1; border-radius: 12px; padding: 3rem; text-align: center; }
.empty-state p { font-size: 1.1rem; color: #475569; margin: .5rem 0; }
.empty-state .hint { font-size: .95rem; color: #94a3b8; }

.spinner { border: 3px solid #f3f3f3; border-top: 3px solid #2a4dd0; border-radius: 50%; width: 24px; height: 24px; animation: spin 1s linear infinite; }
@keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }

.course-selector { 
  background: #fff; 
  border-radius: 12px; 
  padding: 1.5rem; 
  box-shadow: 0 2px 8px rgba(0,0,0,.06); 
  margin-bottom: 1.5rem;
  display: grid;
  grid-template-columns: 2fr 1fr;
  gap: 1rem;
}

.form-group { display: flex; flex-direction: column; gap: .5rem; }
.form-group label { font-weight: 600; color: #1e293b; font-size: .95rem; }
.form-group input, .form-group select { 
  border: 2px solid #e2e8f0; 
  border-radius: 8px; 
  padding: .75rem; 
  font: inherit; 
  transition: all .2s;
  background: #fff;
}
.form-group input:focus, .form-group select:focus { 
  outline: none; 
  border-color: #2a4dd0; 
  box-shadow: 0 0 0 3px rgba(42,77,208,.1); 
}

.table-container { background: #fff; border-radius: 12px; padding: 1.5rem; box-shadow: 0 2px 12px rgba(0,0,0,.08); }
.table-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
.table-header h2 { margin: 0; color: #1e293b; font-size: 1.3rem; }

.btn-save { 
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  color: #fff; 
  border: none; 
  padding: .75rem 1.5rem; 
  border-radius: 8px; 
  font-weight: 600; 
  cursor: pointer;
  transition: all .2s;
  box-shadow: 0 4px 12px rgba(16,185,129,.3);
}
.btn-save:hover:not(:disabled) { 
  transform: translateY(-2px);
  box-shadow: 0 6px 20px rgba(16,185,129,.4);
}
.btn-save:disabled { opacity: 0.6; cursor: not-allowed; transform: none; }

.table-wrapper { overflow-x: auto; }
.attendance-table { width: 100%; border-collapse: collapse; }
.attendance-table thead { background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%); }
.attendance-table th { 
  text-align: left; 
  padding: 1rem; 
  font-weight: 700; 
  color: #1e293b; 
  border-bottom: 3px solid #e2e8f0;
  font-size: .9rem;
  text-transform: uppercase;
  letter-spacing: .5px;
}
.attendance-table td { padding: 1rem; border-bottom: 1px solid #f1f5f9; }
.attendance-table tbody tr { transition: background .2s; }
.attendance-table tbody tr:hover { background: #f8fafc; }

.col-student { width: 25%; }
.col-email { width: 25%; }
.col-status { width: 20%; }
.col-obs { width: 30%; }

.name-cell { font-weight: 600; color: #1e293b; }
.student-email { color: #64748b; font-size: .9rem; }

.status-select { 
  width: 100%; 
  padding: .6rem; 
  border: 2px solid #e2e8f0; 
  border-radius: 6px; 
  font: inherit;
  font-weight: 600;
  cursor: pointer;
  transition: all .2s;
  background: #fff;
}
.status-select:focus { outline: none; border-color: #2a4dd0; box-shadow: 0 0 0 3px rgba(42,77,208,.1); }
.status-select.status-presente { border-color: #10b981; color: #065f46; background: #d1fae5; }
.status-select.status-ausente { border-color: #ef4444; color: #991b1b; background: #fee2e2; }
.status-select.status-tardanza { border-color: #f59e0b; color: #92400e; background: #fef3c7; }

.obs-input { 
  width: 100%; 
  padding: .6rem; 
  border: 2px solid #e2e8f0; 
  border-radius: 6px; 
  font: inherit;
  transition: all .2s;
}
.obs-input:focus { outline: none; border-color: #2a4dd0; box-shadow: 0 0 0 3px rgba(42,77,208,.1); }

.fade-enter-active, .fade-leave-active { transition: opacity .4s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
@keyframes slideIn { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }

@media (max-width: 1024px) {
  .course-selector { grid-template-columns: 1fr; }
  .table-wrapper { overflow-x: auto; }
  .attendance-table { min-width: 900px; }
}
</style>
