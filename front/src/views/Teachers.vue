<template>
  <div class="teachers-container">
    <div class="header-section">
      <h1 class="page-title">
        <i class="fas fa-chalkboard-teacher"></i>
        Gestión de Profesores
      </h1>
      <p class="page-subtitle">Administra los profesores de tu institución</p>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="loading-container">
      <div class="loading-spinner"></div>
      <p>Cargando profesores...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="error-container">
      <i class="fas fa-exclamation-triangle"></i>
      <h3>Error al cargar profesores</h3>
      <p>{{ error }}</p>
      <button @click="fetchTeachers" class="retry-btn">
        <i class="fas fa-refresh"></i>
        Reintentar
      </button>
    </div>

    <!-- Main Content -->
    <div v-else class="main-content">
      <!-- Actions Bar -->
      <div class="actions-bar">
        <div class="search-section">
          <div class="search-box">
            <i class="fas fa-search"></i>
            <input
              v-model="searchTerm"
              type="text"
              placeholder="Buscar profesores..."
              class="search-input"
            />
          </div>
        </div>
        
        <button
          v-if="userProfile?.rol === 'director'"
          @click="showCreateModal = true"
          class="create-btn"
        >
          <i class="fas fa-plus"></i>
          Nuevo Profesor
        </button>
      </div>

      <!-- Teachers Grid -->
      <div v-if="filteredTeachers.length > 0" class="teachers-grid">
        <div
          v-for="teacher in filteredTeachers"
          :key="teacher.id"
          class="teacher-card"
        >
          <div class="teacher-header">
            <div class="teacher-avatar">
              <i class="fas fa-user-tie"></i>
            </div>
            <div class="teacher-info">
              <h3 class="teacher-name">{{ teacher.nombre }} {{ teacher.apellido }}</h3>
              <p class="teacher-email">{{ teacher.email }}</p>
              <span class="teacher-role">{{ teacher.rol }}</span>
            </div>
          </div>
          
          <div class="teacher-meta">
            <div class="meta-item">
              <i class="fas fa-calendar-plus"></i>
              <span>Creado: {{ formatDate(teacher.created_at) }}</span>
            </div>
          </div>

          <div v-if="userProfile?.rol === 'director'" class="teacher-actions">
            <button
              @click="editTeacher(teacher)"
              class="action-btn edit-btn"
              title="Editar profesor"
            >
              <i class="fas fa-edit"></i>
            </button>
            <button
              @click="deleteTeacher(teacher)"
              class="action-btn delete-btn"
              title="Eliminar profesor"
            >
              <i class="fas fa-trash"></i>
            </button>
          </div>
        </div>
      </div>

      <!-- Empty State -->
      <div v-else class="empty-state">
        <i class="fas fa-chalkboard-teacher"></i>
        <h3>No hay profesores</h3>
        <p v-if="searchTerm">No se encontraron profesores que coincidan con "{{ searchTerm }}"</p>
        <p v-else>Aún no hay profesores registrados en tu institución.</p>
        <button
          v-if="userProfile?.rol === 'director' && !searchTerm"
          @click="showCreateModal = true"
          class="create-btn"
        >
          <i class="fas fa-plus"></i>
          Agregar Primer Profesor
        </button>
      </div>
    </div>

    <!-- Create/Edit Teacher Modal -->
    <div v-if="showCreateModal || showEditModal" class="modal-overlay" @click="closeModals">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h2>
            <i class="fas fa-user-tie"></i>
            {{ showEditModal ? 'Editar' : 'Nuevo' }} Profesor
          </h2>
          <button @click="closeModals" class="close-btn">
            <i class="fas fa-times"></i>
          </button>
        </div>

        <form @submit.prevent="showEditModal ? updateTeacher() : createTeacher()" class="teacher-form">
          <div class="form-group">
            <label for="nombre">Nombre *</label>
            <input
              id="nombre"
              v-model="teacherForm.nombre"
              type="text"
              required
              class="form-input"
              placeholder="Nombre del profesor"
            />
          </div>

          <div class="form-group">
            <label for="apellido">Apellido *</label>
            <input
              id="apellido"
              v-model="teacherForm.apellido"
              type="text"
              required
              class="form-input"
              placeholder="Apellido del profesor"
            />
          </div>

          <div class="form-group">
            <label for="email">Email Institucional *</label>
            <input
              id="email"
              v-model="teacherForm.email"
              type="email"
              required
              class="form-input"
              placeholder="profesor@institución.edu"
            />
          </div>

          <div class="form-actions">
            <button type="button" @click="closeModals" class="cancel-btn">
              Cancelar
            </button>
            <button type="submit" :disabled="submitting" class="submit-btn">
              <i v-if="submitting" class="fas fa-spinner fa-spin"></i>
              <i v-else class="fas fa-save"></i>
              {{ submitting ? 'Guardando...' : (showEditModal ? 'Actualizar' : 'Crear') }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { supabase } from '../supabase'

// Reactive state
const loading = ref(true)
const error = ref(null)
const teachers = ref([])
const searchTerm = ref('')
const userProfile = ref(null)

// Modal states
const showCreateModal = ref(false)
const showEditModal = ref(false)
const submitting = ref(false)
const editingTeacher = ref(null)

// Form data
const teacherForm = ref({
  nombre: '',
  apellido: '',
  email: ''
})

// Computed
const filteredTeachers = computed(() => {
  if (!searchTerm.value) return teachers.value
  
  const term = searchTerm.value.toLowerCase()
  return teachers.value.filter(teacher =>
    teacher.nombre.toLowerCase().includes(term) ||
    teacher.apellido.toLowerCase().includes(term) ||
    teacher.email.toLowerCase().includes(term)
  )
})

// Methods
const fetchTeachers = async () => {
  try {
    loading.value = true
    error.value = null
    
    const token = localStorage.getItem('token')
    if (!token) {
      throw new Error('No hay token de autenticación')
    }

    const backendUrl = window.location.hostname === 'localhost' ? 'http://localhost:5009' : '/api/teachers'
    
    const response = await fetch(`${backendUrl}/api/teachers/`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    })

    if (!response.ok) {
      throw new Error(`Error ${response.status}: ${response.statusText}`)
    }

    const data = await response.json()
    teachers.value = data
  } catch (err) {
    console.error('Error fetching teachers:', err)
    error.value = err.message
  } finally {
    loading.value = false
  }
}

const createTeacher = async () => {
  try {
    submitting.value = true
    
    const token = localStorage.getItem('token')
    const backendUrl = window.location.hostname === 'localhost' ? 'http://localhost:5009' : '/api/teachers'
    
    const response = await fetch(`${backendUrl}/api/teachers/`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(teacherForm.value)
    })

    if (!response.ok) {
      throw new Error(`Error ${response.status}: ${response.statusText}`)
    }

    await fetchTeachers()
    closeModals()
    
    // Show success message (you might want to add a notification system)
    alert('Profesor creado exitosamente')
  } catch (err) {
    console.error('Error creating teacher:', err)
    alert(`Error al crear profesor: ${err.message}`)
  } finally {
    submitting.value = false
  }
}

const editTeacher = (teacher) => {
  editingTeacher.value = teacher
  teacherForm.value = {
    nombre: teacher.nombre,
    apellido: teacher.apellido,
    email: teacher.email
  }
  showEditModal.value = true
}

const updateTeacher = async () => {
  try {
    submitting.value = true
    
    const token = localStorage.getItem('token')
    const backendUrl = window.location.hostname === 'localhost' ? 'http://localhost:5009' : '/api/teachers'
    
    const response = await fetch(`${backendUrl}/api/teachers/${editingTeacher.value.id}`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(teacherForm.value)
    })

    if (!response.ok) {
      throw new Error(`Error ${response.status}: ${response.statusText}`)
    }

    await fetchTeachers()
    closeModals()
    
    alert('Profesor actualizado exitosamente')
  } catch (err) {
    console.error('Error updating teacher:', err)
    alert(`Error al actualizar profesor: ${err.message}`)
  } finally {
    submitting.value = false
  }
}

const deleteTeacher = async (teacher) => {
  if (!confirm(`¿Estás seguro de que quieres eliminar a ${teacher.nombre} ${teacher.apellido}?`)) {
    return
  }

  try {
    const token = localStorage.getItem('token')
    const backendUrl = window.location.hostname === 'localhost' ? 'http://localhost:5009' : '/api/teachers'
    
    const response = await fetch(`${backendUrl}/api/teachers/${teacher.id}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    })

    if (!response.ok) {
      throw new Error(`Error ${response.status}: ${response.statusText}`)
    }

    await fetchTeachers()
    alert('Profesor eliminado exitosamente')
  } catch (err) {
    console.error('Error deleting teacher:', err)
    alert(`Error al eliminar profesor: ${err.message}`)
  }
}

const closeModals = () => {
  showCreateModal.value = false
  showEditModal.value = false
  editingTeacher.value = null
  teacherForm.value = {
    nombre: '',
    apellido: '',
    email: ''
  }
}

const getUserProfile = async () => {
  try {
    const { data: { user } } = await supabase.auth.getUser()
    if (!user) return

    const token = localStorage.getItem('token')
    if (!token) return

    const backendUrl = window.location.hostname === 'localhost' ? 'http://localhost:5002' : '/api/auth'
    
    const response = await fetch(`${backendUrl}/api/auth/user-profile`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    })

    if (response.ok) {
      const profileData = await response.json()
      userProfile.value = profileData
    }
  } catch (error) {
    console.error('Error getting user profile:', error)
  }
}

const formatDate = (dateString) => {
  return new Date(dateString).toLocaleDateString('es-ES', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}

// Lifecycle
onMounted(async () => {
  await Promise.all([
    getUserProfile(),
    fetchTeachers()
  ])
})
</script>

<style scoped>
.teachers-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
}

.header-section {
  text-align: center;
  margin-bottom: 3rem;
}

.page-title {
  font-size: 2.5rem;
  color: #2c3e50;
  margin-bottom: 0.5rem;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 1rem;
}

.page-subtitle {
  font-size: 1.1rem;
  color: #7f8c8d;
}

.loading-container, .error-container {
  text-align: center;
  padding: 3rem;
}

.loading-spinner {
  width: 40px;
  height: 40px;
  border: 4px solid #ecf0f1;
  border-top: 4px solid #3498db;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin: 0 auto 1rem;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.error-container {
  color: #e74c3c;
}

.retry-btn {
  background: #3498db;
  color: white;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 8px;
  cursor: pointer;
  margin-top: 1rem;
}

.actions-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  gap: 1rem;
  flex-wrap: wrap;
}

.search-section {
  flex: 1;
  max-width: 400px;
}

.search-box {
  position: relative;
}

.search-box i {
  position: absolute;
  left: 1rem;
  top: 50%;
  transform: translateY(-50%);
  color: #7f8c8d;
}

.search-input {
  width: 100%;
  padding: 0.75rem 1rem 0.75rem 3rem;
  border: 2px solid #ecf0f1;
  border-radius: 8px;
  font-size: 1rem;
  transition: border-color 0.3s;
}

.search-input:focus {
  outline: none;
  border-color: #3498db;
}

.create-btn {
  background: #27ae60;
  color: white;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  transition: background-color 0.3s;
}

.create-btn:hover {
  background: #219a52;
}

.teachers-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}

.teacher-card {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
  transition: transform 0.2s, box-shadow 0.2s;
  border: 1px solid #ecf0f1;
}

.teacher-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
}

.teacher-header {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1rem;
}

.teacher-avatar {
  width: 60px;
  height: 60px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-size: 1.5rem;
}

.teacher-info {
  flex: 1;
}

.teacher-name {
  font-size: 1.3rem;
  color: #2c3e50;
  margin: 0 0 0.25rem 0;
}

.teacher-email {
  color: #7f8c8d;
  margin: 0 0 0.5rem 0;
  font-size: 0.9rem;
}

.teacher-role {
  background: #3498db;
  color: white;
  padding: 0.25rem 0.75rem;
  border-radius: 20px;
  font-size: 0.8rem;
  text-transform: capitalize;
}

.teacher-meta {
  margin-bottom: 1rem;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #7f8c8d;
  font-size: 0.9rem;
}

.teacher-actions {
  display: flex;
  gap: 0.5rem;
  justify-content: flex-end;
}

.action-btn {
  width: 36px;
  height: 36px;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background-color 0.3s;
}

.edit-btn {
  background: #f39c12;
  color: white;
}

.edit-btn:hover {
  background: #e67e22;
}

.delete-btn {
  background: #e74c3c;
  color: white;
}

.delete-btn:hover {
  background: #c0392b;
}

.empty-state {
  text-align: center;
  padding: 4rem 2rem;
  color: #7f8c8d;
}

.empty-state i {
  font-size: 4rem;
  margin-bottom: 1rem;
  color: #bdc3c7;
}

.empty-state h3 {
  font-size: 1.5rem;
  margin-bottom: 1rem;
  color: #5a6c7d;
}

.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background: white;
  border-radius: 12px;
  width: 90%;
  max-width: 500px;
  max-height: 90vh;
  overflow-y: auto;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  border-bottom: 1px solid #ecf0f1;
}

.modal-header h2 {
  margin: 0;
  color: #2c3e50;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.close-btn {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: #7f8c8d;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  transition: background-color 0.3s;
}

.close-btn:hover {
  background: #ecf0f1;
}

.teacher-form {
  padding: 1.5rem;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  color: #2c3e50;
  font-weight: 500;
}

.form-input {
  width: 100%;
  padding: 0.75rem;
  border: 2px solid #ecf0f1;
  border-radius: 8px;
  font-size: 1rem;
  transition: border-color 0.3s;
}

.form-input:focus {
  outline: none;
  border-color: #3498db;
}

.form-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
  margin-top: 2rem;
}

.cancel-btn {
  background: #ecf0f1;
  color: #7f8c8d;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 500;
}

.submit-btn {
  background: #3498db;
  color: white;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.submit-btn:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

@media (max-width: 768px) {
  .teachers-container {
    padding: 1rem;
  }
  
  .page-title {
    font-size: 2rem;
  }
  
  .actions-bar {
    flex-direction: column;
    align-items: stretch;
  }
  
  .search-section {
    max-width: none;
  }
  
  .teachers-grid {
    grid-template-columns: 1fr;
  }
  
  .modal-content {
    margin: 1rem;
    width: calc(100% - 2rem);
  }
  
  .form-actions {
    flex-direction: column;
  }
}
</style>