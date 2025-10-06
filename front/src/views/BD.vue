<template>
  <div class="dashboard-container">
    <!-- Header -->
    <div class="dashboard-header">
      <h1 class="title">Gestión de Usuarios - {{ tenantName }}</h1>
      <div class="user-badge">
        <span class="user-email">{{ currentUserEmail }}</span>
        <span class="tenant-badge">{{ tenantName }}</span>
      </div>
    </div>

    <!-- Alertas -->
    <div v-if="mensajeAlerta.show" :class="['alert', mensajeAlerta.type]">
      <span class="alert-icon">{{ mensajeAlerta.type === 'success' ? '✓' : '⚠' }}</span>
      {{ mensajeAlerta.text }}
      <button class="alert-close" @click="mensajeAlerta.show = false">×</button>
    </div>

    <!-- Panel de Control -->
    <div class="control-panel">
      <div class="control-group">
        <button 
          @click="cargarUsuarios" 
          class="btn btn-primary btn-icon" 
          :disabled="cargando"
        >
          <span class="btn-icon">↻</span>
          {{ cargando ? 'Cargando...' : 'Actualizar Lista' }}
        </button>
        
        <button 
          @click="mostrarModalCrear = true" 
          class="btn btn-success btn-icon"
        >
          <span class="btn-icon">+</span>
          Crear Usuario
        </button>
      </div>
      
      <div class="stats">
        <div class="stat-card">
          <span class="stat-number">{{ usuarios.length }}</span>
          <span class="stat-label">Usuarios Totales</span>
        </div>
        <div class="stat-card">
          <span class="stat-number">{{ contarPorRol('Estudiante') }}</span>
          <span class="stat-label">Estudiantes</span>
        </div>
        <div class="stat-card">
          <span class="stat-number">{{ contarPorRol('Profesor') }}</span>
          <span class="stat-label">Profesores</span>
        </div>
        <div class="stat-card">
          <span class="stat-number">{{ contarPorRol('Director') }}</span>
          <span class="stat-label">Directores</span>
        </div>
      </div>
    </div>

    <!-- Estado de Carga y Error -->
    <div v-if="cargando" class="loading-state">
      <div class="spinner"></div>
      <p>Cargando usuarios de {{ tenantName }}...</p>
    </div>

    <div v-if="error && !cargando" class="error-state">
      <div class="error-icon">⚠</div>
      <p>{{ error }}</p>
      <button @click="cargarUsuarios" class="btn btn-outline">Reintentar</button>
    </div>

    <!-- Tabla de Usuarios -->
    <div v-if="!cargando && usuarios.length > 0" class="table-container">
      <div class="table-header">
        <h2>Lista de Usuarios</h2>
        <div class="table-actions">
          <input 
            v-model="filtroBusqueda" 
            placeholder="Buscar usuarios..." 
            class="search-input"
          >
        </div>
      </div>

      <div class="table-responsive">
        <table class="users-table">
          <thead>
            <tr>
              <th>ID</th>
              <th>Nombre</th>
              <th>Apellido</th>
              <th>Email</th>
              <th>Rol Actual</th>
              <th>Nuevo Rol</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="usuario in usuariosFiltrados" :key="usuario.id">
              <td class="user-id">{{ usuario.id }}</td>
              <td class="user-name">{{ usuario.nombre || 'N/A' }}</td>
              <td class="user-lastname">{{ usuario.apellido || 'N/A' }}</td>
              <td class="user-email">{{ usuario.email }}</td>
              <td>
                <span :class="['role-badge', `role-${usuario.rol?.toLowerCase()}`]">
                  {{ usuario.rol }}
                </span>
              </td>
              <td>
                <select 
                  v-model="usuario.nuevoRol" 
                  class="role-select"
                  :class="{ changed: usuario.nuevoRol !== usuario.rol }"
                >
                  <option value="Estudiante">Estudiante</option>
                  <option value="Profesor">Profesor</option>
                  <option value="Director">Director</option>
                </select>
              </td>
              <td>
                <button 
                  @click="actualizarRol(usuario)" 
                  class="btn btn-update"
                  :disabled="usuario.nuevoRol === usuario.rol || actualizandoId === usuario.id"
                  :class="{ loading: actualizandoId === usuario.id }"
                >
                  <span v-if="actualizandoId === usuario.id" class="btn-spinner"></span>
                  {{ actualizandoId === usuario.id ? 'Actualizando...' : 'Actualizar' }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Estado Sin Datos -->
    <div v-if="!cargando && usuarios.length === 0" class="empty-state">
      <div class="empty-icon">👥</div>
      <h3>No hay usuarios registrados</h3>
      <p>No se encontraron usuarios en {{ tenantName }}.</p>
      <button @click="mostrarModalCrear = true" class="btn btn-success">
        Crear Primer Usuario
      </button>
    </div>

    <!-- Modal Crear Usuario -->
    <div v-if="mostrarModalCrear" class="modal-overlay">
      <div class="modal">
        <div class="modal-header">
          <h3>Crear Nuevo Usuario</h3>
          <button @click="cerrarModal" class="modal-close">×</button>
        </div>
        
        <div class="modal-body">
          <form @submit.prevent="crearUsuario" class="user-form">
            <div class="form-group">
              <label for="nombre">Nombre *</label>
              <input 
                id="nombre"
                v-model="nuevoUsuario.nombre" 
                type="text" 
                required
                placeholder="Ingresa el nombre"
                class="form-input"
              >
            </div>
            
            <div class="form-group">
              <label for="apellido">Apellido *</label>
              <input 
                id="apellido"
                v-model="nuevoUsuario.apellido" 
                type="text" 
                required
                placeholder="Ingresa el apellido"
                class="form-input"
              >
            </div>
            
            <div class="form-group">
              <label for="email">Email *</label>
              <input 
                id="email"
                v-model="nuevoUsuario.email" 
                type="email" 
                required
                :placeholder="`Ej: usuario@${tenantDomain}`"
                class="form-input"
                :class="{ invalid: nuevoUsuario.email && !validarEmailDominio(nuevoUsuario.email) }"
              >
              <small class="form-hint">Debe ser un email de {{ tenantName }}</small>
              <small v-if="nuevoUsuario.email && !validarEmailDominio(nuevoUsuario.email)" 
                     class="form-error">
                El email debe pertenecer a {{ tenantName }}
              </small>
            </div>
            
            <div class="form-group">
              <label for="rol">Rol *</label>
              <select 
                id="rol"
                v-model="nuevoUsuario.rol" 
                required
                class="form-select"
              >
                <option value="Estudiante">Estudiante</option>
                <option value="Profesor">Profesor</option>
                <option value="Director">Director</option>
              </select>
            </div>
          </form>
        </div>
        
        <div class="modal-footer">
          <button @click="cerrarModal" class="btn btn-outline">Cancelar</button>
          <button 
            @click="crearUsuario" 
            :disabled="creandoUsuario || (nuevoUsuario.email && !validarEmailDominio(nuevoUsuario.email))" 
            class="btn btn-success"
          >
            <span v-if="creandoUsuario" class="btn-spinner"></span>
            {{ creandoUsuario ? 'Creando...' : 'Crear Usuario' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, onMounted, computed } from 'vue'
import { supabase } from '../supabase'

export default {
  name: 'UsersDashboard',
  setup() {
    // Estados reactivos
    const usuarios = ref([])
    const currentUserEmail = ref('')
    const tenantName = ref('')
    const tenantDomain = ref('')
    const cargando = ref(false)
    const error = ref('')
    const actualizandoId = ref(null)
    const creandoUsuario = ref(false)
    const mostrarModalCrear = ref(false)
    const filtroBusqueda = ref('')
    const mensajeAlerta = ref({
      show: false,
      text: '',
      type: 'success'
    })

    const API_BASE_URL = 'http://localhost:5009/api/usuarios'

    // Nuevo usuario
    const nuevoUsuario = ref({
      nombre: '',
      apellido: '',
      email: '',
      rol: 'Estudiante'
    })

    // Computed
    const usuariosFiltrados = computed(() => {
      if (!filtroBusqueda.value) return usuarios.value
      
      const search = filtroBusqueda.value.toLowerCase()
      return usuarios.value.filter(usuario => 
        usuario.nombre?.toLowerCase().includes(search) ||
        usuario.apellido?.toLowerCase().includes(search) ||
        usuario.email?.toLowerCase().includes(search) ||
        usuario.rol?.toLowerCase().includes(search)
      )
    })

    // Funciones
    const getTenantFromEmail = (email) => {
      if (email.endsWith('@ucb.edu.bo')) return 'ucb.edu.bo'
      if (email.endsWith('@upb.edu.bo')) return 'upb.edu.bo'
      if (email.endsWith('@gmail.com')) return 'gmail.com'
      return 'unknown'
    }

    const getTenantDisplayName = (tenant) => {
      switch (tenant) {
        case 'ucb.edu.bo': return 'UCB'
        case 'upb.edu.bo': return 'UPB'
        case 'gmail.com': return 'Gmail'
        default: return tenant
      }
    }

    const getTenantDomain = (tenant) => {
      switch (tenant) {
        case 'ucb.edu.bo': return 'ucb.edu.bo'
        case 'upb.edu.bo': return 'upb.edu.bo'
        case 'gmail.com': return 'gmail.com'
        default: return tenant
      }
    }

    const validarEmailDominio = (email) => {
      if (!currentUserEmail.value) return true
      const userTenant = getTenantFromEmail(currentUserEmail.value)
      const emailTenant = getTenantFromEmail(email)
      return userTenant === emailTenant
    }

    const mostrarAlerta = (text, type = 'success') => {
      mensajeAlerta.value = { show: true, text, type }
      setTimeout(() => {
        mensajeAlerta.value.show = false
      }, 5000)
    }

    const contarPorRol = (rol) => {
      return usuarios.value.filter(u => u.rol === rol).length
    }

    const cargarUsuarios = async () => {
      cargando.value = true
      error.value = ''
      
      try {
        const { data: { session } } = await supabase.auth.getSession()
        if (!session?.user?.email) {
          throw new Error('No hay usuario autenticado')
        }

        currentUserEmail.value = session.user.email
        const tenant = getTenantFromEmail(session.user.email)
        tenantName.value = getTenantDisplayName(tenant)
        tenantDomain.value = getTenantDomain(tenant)

        const response = await fetch(`${API_BASE_URL}/mi-tenant`, {
          headers: { 'X-User-Email': session.user.email }
        })
        
        if (!response.ok) throw new Error(`Error HTTP: ${response.status}`)
        
        const data = await response.json()
        // Inicializar nuevoRol para cada usuario
        usuarios.value = data.usuarios.map(u => ({
          ...u,
          nuevoRol: u.rol
        }))
        
      } catch (err) {
        error.value = `Error al cargar usuarios: ${err.message}`
        console.error('Error:', err)
      } finally {
        cargando.value = false
      }
    }

    const actualizarRol = async (usuario) => {
      if (usuario.nuevoRol === usuario.rol) return
      
      actualizandoId.value = usuario.id
      
      try {
        const { data: { session } } = await supabase.auth.getSession()
        if (!session?.user?.email) {
          throw new Error('No hay usuario autenticado')
        }

        const response = await fetch(`${API_BASE_URL}/${usuario.id}/rol`, {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json',
            'X-User-Email': session.user.email
          },
          body: JSON.stringify({ rol: usuario.nuevoRol })
        })

        if (!response.ok) throw new Error(`Error HTTP: ${response.status}`)

        // Actualizar el rol localmente
        usuario.rol = usuario.nuevoRol
        mostrarAlerta(`Rol actualizado correctamente para ${usuario.nombre}`, 'success')
        
      } catch (err) {
        console.error('Error actualizando rol:', err)
        // Revertir el cambio en caso de error
        usuario.nuevoRol = usuario.rol
        mostrarAlerta(`Error al actualizar rol: ${err.message}`, 'error')
      } finally {
        actualizandoId.value = null
      }
    }

    const crearUsuario = async () => {
  if (!nuevoUsuario.value.nombre || !nuevoUsuario.value.apellido || !nuevoUsuario.value.email) {
    mostrarAlerta('Todos los campos son requeridos', 'error')
    return
  }

  // Validar dominio del email
  if (!validarEmailDominio(nuevoUsuario.value.email)) {
    mostrarAlerta(`El email debe pertenecer al dominio ${tenantDomain.value}`, 'error')
    return
  }

  creandoUsuario.value = true
  
  try {
    const { data: { session } } = await supabase.auth.getSession()
    if (!session?.user?.email) {
      throw new Error('No hay usuario autenticado')
    }

    // ✅ CORREGIDO: Ahora usa /api/crear
    const response = await fetch(`http://localhost:5009/api/crear`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-User-Email': session.user.email
      },
      body: JSON.stringify(nuevoUsuario.value)
    })

    if (!response.ok) {
      const errorText = await response.text()
      console.error('Error response:', errorText)
      throw new Error(`Error HTTP: ${response.status} - ${errorText}`)
    }

    const resultado = await response.json()
    mostrarAlerta('Usuario creado correctamente', 'success')
    cerrarModal()
    await cargarUsuarios()
    
  } catch (err) {
    console.error('Error creando usuario:', err)
    mostrarAlerta(`Error al crear usuario: ${err.message}`, 'error')
  } finally {
    creandoUsuario.value = false
  }
}
    const cerrarModal = () => {
      mostrarModalCrear.value = false
      nuevoUsuario.value = {
        nombre: '',
        apellido: '',
        email: '',
        rol: 'Estudiante'
      }
    }

    onMounted(() => {
      cargarUsuarios()
    })

    return {
      usuarios,
      usuariosFiltrados,
      currentUserEmail,
      tenantName,
      tenantDomain,
      cargando,
      error,
      actualizandoId,
      creandoUsuario,
      mostrarModalCrear,
      filtroBusqueda,
      mensajeAlerta,
      nuevoUsuario,
      cargarUsuarios,
      actualizarRol,
      crearUsuario,
      cerrarModal,
      contarPorRol,
      mostrarAlerta,
      validarEmailDominio
    }
  }
}
</script>

<style scoped>
/* Estilos mejorados y responsive */
.dashboard-container {
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 20px;
}

.dashboard-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 30px;
  flex-wrap: wrap;
  gap: 20px;
}

.title {
  color: white;
  font-size: 2.5rem;
  font-weight: 700;
  margin: 0;
  text-shadow: 0 2px 4px rgba(0,0,0,0.3);
}

.user-badge {
  display: flex;
  align-items: center;
  gap: 15px;
  flex-wrap: wrap;
}

.user-email {
  color: #2d3748; /* CORREGIDO: Ahora es negro/dark gray */
  font-weight: 500;
  background: rgba(255,255,255,0.9);
  padding: 8px 16px;
  border-radius: 8px;
  backdrop-filter: blur(10px);
}

.tenant-badge {
  background: rgba(255,255,255,0.2);
  color: white;
  padding: 8px 16px;
  border-radius: 20px;
  font-weight: 600;
  backdrop-filter: blur(10px);
}

/* Alertas */
.alert {
  display: flex;
  align-items: center;
  padding: 16px 20px;
  border-radius: 12px;
  margin-bottom: 24px;
  font-weight: 500;
  gap: 12px;
}

.alert.success {
  background: #d4edda;
  color: #155724;
  border: 1px solid #c3e6cb;
}

.alert.error {
  background: #f8d7da;
  color: #721c24;
  border: 1px solid #f5c6cb;
}

.alert-icon {
  font-weight: bold;
}

.alert-close {
  margin-left: auto;
  background: none;
  border: none;
  font-size: 1.2rem;
  cursor: pointer;
  opacity: 0.7;
}

.alert-close:hover {
  opacity: 1;
}

/* Panel de Control */
.control-panel {
  background: white;
  border-radius: 16px;
  padding: 24px;
  margin-bottom: 24px;
  box-shadow: 0 8px 32px rgba(0,0,0,0.1);
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 24px;
}

.control-group {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

.stats {
  display: flex;
  gap: 16px;
  flex-wrap: wrap;
}

.stat-card {
  background: #f8f9fa;
  padding: 16px 20px;
  border-radius: 12px;
  text-align: center;
  min-width: 100px;
}

.stat-number {
  display: block;
  font-size: 1.5rem;
  font-weight: 700;
  color: #667eea;
}

.stat-label {
  font-size: 0.875rem;
  color: #6c757d;
}

/* Botones */
.btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 12px 20px;
  border: none;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  text-decoration: none;
  font-size: 0.875rem;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-primary {
  background: #667eea;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: #5a6fd8;
  transform: translateY(-2px);
}

.btn-success {
  background: #28a745;
  color: white;
}

.btn-success:hover:not(:disabled) {
  background: #218838;
  transform: translateY(-2px);
}

.btn-update {
  background: #17a2b8;
  color: white;
  padding: 8px 16px;
  font-size: 0.8rem;
}

.btn-update:hover:not(:disabled) {
  background: #138496;
}

.btn-outline {
  background: transparent;
  border: 2px solid #6c757d;
  color: #6c757d;
}

.btn-outline:hover {
  background: #6c757d;
  color: white;
}

.btn-icon {
  font-size: 1rem;
}

.btn-spinner {
  width: 16px;
  height: 16px;
  border: 2px solid transparent;
  border-top: 2px solid currentColor;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Estados */
.loading-state, .error-state, .empty-state {
  text-align: center;
  padding: 60px 20px;
  background: white;
  border-radius: 16px;
  box-shadow: 0 8px 32px rgba(0,0,0,0.1);
}

.spinner {
  width: 40px;
  height: 40px;
  border: 4px solid #f3f3f3;
  border-top: 4px solid #667eea;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin: 0 auto 20px;
}

.error-icon, .empty-icon {
  font-size: 3rem;
  margin-bottom: 20px;
}

/* Tabla */
.table-container {
  background: white;
  border-radius: 16px;
  overflow: hidden;
  box-shadow: 0 8px 32px rgba(0,0,0,0.1);
}

.table-header {
  padding: 24px;
  border-bottom: 1px solid #e9ecef;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 16px;
}

.table-header h2 {
  margin: 0;
  color: #2d3748;
}

.search-input {
  padding: 10px 16px;
  border: 2px solid #e2e8f0;
  border-radius: 8px;
  font-size: 0.875rem;
  transition: border-color 0.3s ease;
  min-width: 250px;
  color: #2d3748;
}

.search-input:focus {
  outline: none;
  border-color: #667eea;
}

.table-responsive {
  overflow-x: auto;
}

.users-table {
  width: 100%;
  border-collapse: collapse;
}

.users-table th {
  background: #f8f9fa;
  padding: 16px;
  text-align: left;
  font-weight: 600;
  color: #2d3748;
  border-bottom: 2px solid #e9ecef;
}

.users-table td {
  padding: 16px;
  border-bottom: 1px solid #e9ecef;
  color: #2d3748; /* Asegurar texto oscuro */
}

.users-table tr:hover {
  background: #f8f9fa;
}

/* CORREGIDO: Texto oscuro para emails y nombres */
.user-id, .user-name, .user-lastname, .user-email {
  color: #2d3748 !important;
  font-weight: 500;
}

/* Badges de rol */
.role-badge {
  padding: 6px 12px;
  border-radius: 20px;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.role-estudiante {
  background: #e3f2fd;
  color: #1976d2;
}

.role-profesor {
  background: #f3e5f5;
  color: #7b1fa2;
}

.role-director {
  background: #e8f5e8;
  color: #2e7d32;
}

/* Select de roles */
.role-select {
  padding: 8px 12px;
  border: 2px solid #e2e8f0;
  border-radius: 6px;
  font-size: 0.875rem;
  transition: all 0.3s ease;
  background: white;
  color: #2d3748;
}

.role-select:focus {
  outline: none;
  border-color: #667eea;
}

.role-select.changed {
  border-color: #ffc107;
  background: #fffbf0;
}

/* Modal */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0,0,0,0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 20px;
}

.modal {
  background: white;
  border-radius: 16px;
  width: 100%;
  max-width: 500px;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 20px 60px rgba(0,0,0,0.3);
}

.modal-header {
  padding: 24px;
  border-bottom: 1px solid #e9ecef;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.modal-header h3 {
  margin: 0;
  color: #2d3748;
}

.modal-close {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: #6c757d;
  padding: 0;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.modal-close:hover {
  color: #495057;
}

.modal-body {
  padding: 24px;
}

.modal-footer {
  padding: 24px;
  border-top: 1px solid #e9ecef;
  display: flex;
  gap: 12px;
  justify-content: flex-end;
}

/* Formulario */
.user-form {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.form-group label {
  font-weight: 600;
  color: #2d3748;
  font-size: 0.875rem;
}

.form-input, .form-select {
  padding: 12px 16px;
  border: 2px solid #e2e8f0;
  border-radius: 8px;
  font-size: 0.875rem;
  transition: border-color 0.3s ease;
  color: #2d3748;
}

.form-input:focus, .form-select:focus {
  outline: none;
  border-color: #667eea;
}

.form-input::placeholder {
  color: #a0aec0;
}

.form-hint {
  color: #6c757d;
  font-size: 0.75rem;
  margin-top: 4px;
}

/* Estilos para validación */
.form-input.invalid {
  border-color: #e53e3e;
  background-color: #fed7d7;
}

.form-error {
  color: #e53e3e;
  font-size: 0.75rem;
  margin-top: 4px;
  display: block;
}

/* Responsive */
@media (max-width: 768px) {
  .dashboard-header {
    flex-direction: column;
    align-items: flex-start;
  }
  
  .control-panel {
    flex-direction: column;
    align-items: stretch;
  }
  
  .stats {
    justify-content: center;
  }
  
  .table-header {
    flex-direction: column;
    align-items: stretch;
  }
  
  .search-input {
    min-width: auto;
    width: 100%;
  }
  
  .users-table {
    font-size: 0.875rem;
  }
  
  .users-table th,
  .users-table td {
    padding: 12px 8px;
  }
  
  .modal {
    margin: 20px;
    width: calc(100% - 40px);
  }
  
  .modal-footer {
    flex-direction: column;
  }
}

@media (max-width: 480px) {
  .dashboard-container {
    padding: 10px;
  }
  
  .title {
    font-size: 2rem;
  }
  
  .control-group {
    flex-direction: column;
    width: 100%;
  }
  
  .btn {
    width: 100%;
    justify-content: center;
  }
  
  .stats {
    flex-direction: column;
  }
  
  .stat-card {
    width: 100%;
  }
}
</style>