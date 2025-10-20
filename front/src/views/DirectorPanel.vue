<template>
  <div class="director-panel">
    <div class="container">
      <div class="panel-header">
        <h1>Panel de Director</h1>
        <p class="subtitle">Gestión de Estudiantes y Profesores</p>
      </div>

      <!-- Actions Bar -->
      <div class="actions-bar">
        <div class="filters">
          <select v-model="roleFilter" class="filter-select">
            <option value="">Todos los roles</option>
            <option value="Estudiante">Estudiantes</option>
            <option value="Profesor">Profesores</option>
            <option value="Director">Directores</option>
          </select>
          <input 
            v-model="searchQuery" 
            type="text" 
            placeholder="Buscar por nombre o email..." 
            class="search-input"
          />
        </div>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="loading">
        <p>Cargando usuarios...</p>
      </div>

      <!-- Error State -->
      <div v-else-if="error" class="error-message">
        <p>❌ {{ error }}</p>
        <button @click="loadUsers" class="btn btn-secondary">Reintentar</button>
      </div>

      <!-- Users Table -->
      <div v-else class="table-container">
        <table class="users-table">
          <thead>
            <tr>
              <th>ID</th>
              <th>Nombre</th>
              <th>Apellido</th>
              <th>Email</th>
              <th>Rol</th>
              <th>Fecha Creación</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="user in filteredUsers" :key="user.id">
              <td>{{ user.id }}</td>
              <td>{{ user.nombre }}</td>
              <td>{{ user.apellido }}</td>
              <td>{{ user.email }}</td>
              <td>
                <span :class="['role-badge', user.rol.toLowerCase()]">
                  {{ user.rol }}
                </span>
              </td>
              <td>{{ formatDate(user.created_at) }}</td>
              <td class="actions-cell">
                <button @click="openEditModal(user)" class="btn-icon btn-edit" title="Editar">
                  ✏️
                </button>
                <button @click="confirmDelete(user)" class="btn-icon btn-delete" title="Eliminar">
                  🗑️
                </button>
              </td>
            </tr>
            <tr v-if="filteredUsers.length === 0">
              <td colspan="7" class="no-data">
                No se encontraron usuarios
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Create/Edit Modal -->
      <div v-if="showModal" class="modal-overlay" @click="closeModal">
        <div class="modal-content" @click.stop>
          <div class="modal-header">
            <h2>Editar Usuario</h2>
            <button @click="closeModal" class="close-btn">×</button>
          </div>
          
          <form @submit.prevent="submitForm" class="modal-form">
            <div class="form-group">
              <label>Nombre *</label>
              <input v-model="formData.nombre" type="text" required />
            </div>
            
            <div class="form-group">
              <label>Apellido *</label>
              <input v-model="formData.apellido" type="text" required />
            </div>
            
            <div class="form-group">
              <label>Email *</label>
              <input 
                v-model="formData.email" 
                type="email" 
                required 
                disabled
              />
            </div>
            
            <div class="form-group">
              <label>Rol *</label>
              <select v-model="formData.rol" required>
                <option value="">Seleccionar rol</option>
                <option value="Estudiante">Estudiante</option>
                <option value="Profesor">Profesor</option>
                <option value="Director">Director</option>
              </select>
            </div>

            <div v-if="submitting" class="form-message">
              Procesando...
            </div>

            <div v-if="formError" class="form-error">
              {{ formError }}
            </div>

            <div class="modal-actions">
              <button type="button" @click="closeModal" class="btn btn-secondary">
                Cancelar
              </button>
              <button type="submit" class="btn btn-primary" :disabled="submitting">
                Guardar Cambios
              </button>
            </div>
          </form>
        </div>
      </div>

      <!-- Delete Confirmation Modal -->
      <div v-if="showDeleteModal" class="modal-overlay" @click="closeDeleteModal">
        <div class="modal-content delete-modal" @click.stop>
          <div class="modal-header">
            <h2>⚠️ Confirmar Eliminación</h2>
            <button @click="closeDeleteModal" class="close-btn">×</button>
          </div>
          
          <div class="delete-content">
            <p>¿Estás seguro de que deseas eliminar al usuario?</p>
            <div class="user-info-box">
              <p><strong>{{ userToDelete?.nombre }} {{ userToDelete?.apellido }}</strong></p>
              <p>{{ userToDelete?.email }}</p>
              <p>Rol: {{ userToDelete?.rol }}</p>
            </div>
            <p class="delete-warning">Esta acción no se puede deshacer.</p>
          </div>

          <div class="modal-actions">
            <button @click="closeDeleteModal" class="btn btn-secondary">
              Cancelar
            </button>
            <button @click="deleteUser" class="btn btn-danger" :disabled="submitting">
              {{ submitting ? 'Eliminando...' : 'Eliminar Usuario' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()

// Data
const users = ref([])
const loading = ref(false)
const error = ref(null)
const roleFilter = ref('')
const searchQuery = ref('')

// Modal states
const showModal = ref(false)
const showDeleteModal = ref(false)
const submitting = ref(false)
const formError = ref(null)

// Form data
const formData = ref({
  nombre: '',
  apellido: '',
  email: '',
  rol: ''
})

const editingUserId = ref(null)
const userToDelete = ref(null)

// User context
const userEmail = ref('')
const userDomain = ref('')

// API Base URL
const DIRECTOR_API = import.meta.env.VITE_DIRECTOR_API || 'http://localhost:5012'

// Computed
const filteredUsers = computed(() => {
  let filtered = users.value

  // Filter by role
  if (roleFilter.value) {
    filtered = filtered.filter(u => u.rol === roleFilter.value)
  }

  // Filter by search query
  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase()
    filtered = filtered.filter(u => 
      u.nombre.toLowerCase().includes(query) ||
      u.apellido.toLowerCase().includes(query) ||
      u.email.toLowerCase().includes(query)
    )
  }

  return filtered
})

const emailPlaceholder = computed(() => {
  if (userDomain.value) {
    return `ejemplo${userDomain.value}`
  }
  return 'ejemplo@institucion.edu'
})

// Methods
async function getUserEmail() {
  try {
    const response = await fetch('/auth/user-profile', {
      credentials: 'include'
    })
    if (response.ok) {
      const profile = await response.json()
      userEmail.value = profile.email
      return profile.email
    }
  } catch (err) {
    console.error('Error getting user email:', err)
  }
  return null
}

async function loadUsers() {
  loading.value = true
  error.value = null
  
  try {
    const email = userEmail.value || await getUserEmail()
    
    if (!email) {
      throw new Error('No se pudo obtener el email del usuario')
    }

    const response = await fetch(`${DIRECTOR_API}/api/director/users`, {
      headers: {
        'X-User-Email': email
      }
    })

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}))
      throw new Error(errorData.detail || 'Error al cargar usuarios')
    }

    users.value = await response.json()
  } catch (err) {
    console.error('Error loading users:', err)
    error.value = err.message
  } finally {
    loading.value = false
  }
}

function openEditModal(user) {
  editingUserId.value = user.id
  formData.value = {
    nombre: user.nombre,
    apellido: user.apellido,
    email: user.email,
    rol: user.rol
  }
  formError.value = null
  showModal.value = true
}

function closeModal() {
  showModal.value = false
  formData.value = {
    nombre: '',
    apellido: '',
    email: '',
    rol: ''
  }
  editingUserId.value = null
  formError.value = null
}

function confirmDelete(user) {
  userToDelete.value = user
  showDeleteModal.value = true
}

function closeDeleteModal() {
  showDeleteModal.value = false
  userToDelete.value = null
}

async function submitForm() {
  submitting.value = true
  formError.value = null

  try {
    const email = userEmail.value

    // Update user
    const response = await fetch(
      `${DIRECTOR_API}/api/director/users/${editingUserId.value}`,
      {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'X-User-Email': email
        },
        body: JSON.stringify(formData.value)
      }
    )

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}))
      throw new Error(errorData.detail || 'Error al actualizar usuario')
    }

    await loadUsers()
    closeModal()
  } catch (err) {
    console.error('Error submitting form:', err)
    formError.value = err.message
  } finally {
    submitting.value = false
  }
}

async function deleteUser() {
  submitting.value = true

  try {
    const email = userEmail.value
    const response = await fetch(
      `${DIRECTOR_API}/api/director/users/${userToDelete.value.id}`,
      {
        method: 'DELETE',
        headers: {
          'X-User-Email': email
        }
      }
    )

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}))
      throw new Error(errorData.detail || 'Error al eliminar usuario')
    }

    await loadUsers()
    closeDeleteModal()
  } catch (err) {
    console.error('Error deleting user:', err)
    alert('Error al eliminar usuario: ' + err.message)
  } finally {
    submitting.value = false
  }
}

function formatDate(dateString) {
  if (!dateString) return 'N/A'
  const date = new Date(dateString)
  return date.toLocaleDateString('es-ES', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}

onMounted(async () => {
  await getUserEmail()
  await loadUsers()
})
</script>

<style scoped>
.director-panel {
  min-height: calc(100vh - 120px);
  padding: 2rem 0;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.container {
  max-width: 1400px;
  margin: 0 auto;
  padding: 0 1rem;
}

.panel-header {
  text-align: center;
  margin-bottom: 2rem;
  color: white;
}

.panel-header h1 {
  font-size: 2.5rem;
  margin-bottom: 0.5rem;
}

.subtitle {
  font-size: 1.1rem;
  opacity: 0.9;
}

.actions-bar {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  margin-bottom: 1.5rem;
  gap: 1rem;
  flex-wrap: wrap;
}

.filters {
  display: flex;
  gap: 1rem;
  flex: 1;
  max-width: 800px;
}

.filter-select,
.search-input {
  padding: 0.75rem 1rem;
  border: 1px solid #ddd;
  border-radius: 8px;
  font-size: 1rem;
}

.filter-select {
  min-width: 180px;
}

.search-input {
  flex: 1;
}

.btn {
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
}

.btn-primary {
  background: #4CAF50;
  color: white;
}

.btn-primary:hover {
  background: #45a049;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(76, 175, 80, 0.3);
}

.btn-secondary {
  background: #6c757d;
  color: white;
}

.btn-secondary:hover {
  background: #5a6268;
}

.btn-danger {
  background: #dc3545;
  color: white;
}

.btn-danger:hover {
  background: #c82333;
}

.table-container {
  background: white;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
}

.users-table {
  width: 100%;
  border-collapse: collapse;
}

.users-table thead {
  background: #667eea;
  color: white;
}

.users-table th,
.users-table td {
  padding: 1rem;
  text-align: left;
}

.users-table tbody tr {
  border-bottom: 1px solid #eee;
  transition: background 0.2s;
}

.users-table tbody tr:hover {
  background: #f8f9fa;
}

.role-badge {
  padding: 0.25rem 0.75rem;
  border-radius: 20px;
  font-size: 0.875rem;
  font-weight: 600;
}

.role-badge.estudiante {
  background: #e3f2fd;
  color: #1976d2;
}

.role-badge.profesor {
  background: #f3e5f5;
  color: #7b1fa2;
}

.role-badge.director {
  background: #fff3e0;
  color: #e65100;
}

.actions-cell {
  display: flex;
  gap: 0.5rem;
}

.btn-icon {
  background: none;
  border: none;
  font-size: 1.25rem;
  cursor: pointer;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  transition: background 0.2s;
}

.btn-icon:hover {
  background: #e0e0e0;
}

.no-data {
  text-align: center;
  padding: 3rem;
  color: #999;
  font-style: italic;
}

.loading,
.error-message {
  text-align: center;
  padding: 3rem;
  background: white;
  border-radius: 12px;
  color: #333;
}

.error-message {
  color: #dc3545;
}

/* Modal Styles */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.7);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
  padding: 1rem;
}

.modal-content {
  background: white;
  border-radius: 12px;
  max-width: 500px;
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  border-bottom: 1px solid #eee;
}

.modal-header h2 {
  margin: 0;
  font-size: 1.5rem;
}

.close-btn {
  background: none;
  border: none;
  font-size: 2rem;
  cursor: pointer;
  color: #999;
  line-height: 1;
  padding: 0;
  width: 30px;
  height: 30px;
}

.close-btn:hover {
  color: #333;
}

.modal-form {
  padding: 1.5rem;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 600;
  color: #333;
}

.form-group input,
.form-group select {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 8px;
  font-size: 1rem;
}

.form-group input:disabled {
  background: #f5f5f5;
  cursor: not-allowed;
}

.form-help {
  display: block;
  margin-top: 0.25rem;
  font-size: 0.875rem;
  color: #666;
}

.form-error {
  padding: 0.75rem;
  background: #ffebee;
  color: #c62828;
  border-radius: 8px;
  margin-bottom: 1rem;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  padding: 1.5rem;
  border-top: 1px solid #eee;
}

.delete-content {
  padding: 1.5rem;
}

.user-info-box {
  padding: 1rem;
  background: #f5f5f5;
  border-radius: 8px;
  margin: 1rem 0;
}

.user-info-box p {
  margin: 0.25rem 0;
}

.delete-warning {
  color: #dc3545;
  font-weight: 600;
  margin-top: 1rem;
}

@media (max-width: 768px) {
  .actions-bar {
    flex-direction: column;
    align-items: stretch;
  }

  .filters {
    flex-direction: column;
    max-width: 100%;
  }

  .users-table {
    font-size: 0.875rem;
  }

  .users-table th,
  .users-table td {
    padding: 0.5rem;
  }

  .panel-header h1 {
    font-size: 1.75rem;
  }
}
</style>
