<template>
  <div class="dashboard-container">
    <!-- Header compacto -->
    <div class="dashboard-header compact">
      <div class="header-main">
        <h1 class="title">Gestión de Usuarios - {{ tenantName }}</h1>
        <div class="user-badge">
          <span class="user-email">{{ currentUserEmail }}</span>
          <span class="tenant-badge">{{ tenantName }}</span>
        </div>
      </div>
    </div>

    <!-- Alertas compactas -->
    <div v-if="mensajeAlerta.show" :class="['alert', mensajeAlerta.type, 'compact']">
      <span class="alert-icon">{{ mensajeAlerta.type === 'success' ? '✓' : '⚠' }}</span>
      {{ mensajeAlerta.text }}
      <button class="alert-close" @click="mensajeAlerta.show = false">×</button>
    </div>

    <!-- Panel de Control compacto -->
    <div class="control-panel compact">
      <div class="control-group">
        <button 
          @click="cargarUsuarios" 
          class="btn btn-primary btn-icon btn-small" 
          :disabled="cargando"
        >
          <span class="btn-icon">↻</span>
          {{ cargando ? 'Cargando...' : 'Actualizar' }}
        </button>
        
        <button 
          @click="mostrarModalCrear = true" 
          class="btn btn-success btn-icon btn-small"
        >
          <span class="btn-icon">+</span>
          Crear Usuario
        </button>
      </div>
      
      <!-- Filtros por Rol -->
      <div class="filters">
        <div class="filter-group">
          <label class="filter-label">Filtrar por Rol:</label>
          <select v-model="filtroRol" @change="aplicarFiltroRol" class="filter-select">
            <option value="">Todos los roles</option>
            <option value="Estudiante">Estudiante</option>
            <option value="Profesor">Profesor</option>
            <option value="Director">Director</option>
          </select>
        </div>
        
        <div class="search-group">
          <input 
            v-model="filtroBusqueda" 
            placeholder="Buscar por nombre, email..." 
            class="search-input compact"
          >
        </div>
      </div>

      <!-- Stats compactas -->
      <div class="stats compact">
        <div class="stat-card compact" @click="filtroRol = ''; aplicarFiltroRol()" :class="{ active: !filtroRol }">
          <span class="stat-number">{{ usuarios.length }}</span>
          <span class="stat-label">Total</span>
        </div>
        <div class="stat-card compact" @click="filtroRol = 'Estudiante'; aplicarFiltroRol()" :class="{ active: filtroRol === 'Estudiante' }">
          <span class="stat-number">{{ contarPorRol('Estudiante') }}</span>
          <span class="stat-label">Estudiantes</span>
        </div>
        <div class="stat-card compact" @click="filtroRol = 'Profesor'; aplicarFiltroRol()" :class="{ active: filtroRol === 'Profesor' }">
          <span class="stat-number">{{ contarPorRol('Profesor') }}</span>
          <span class="stat-label">Profesores</span>
        </div>
        <div class="stat-card compact" @click="filtroRol = 'Director'; aplicarFiltroRol()" :class="{ active: filtroRol === 'Director' }">
          <span class="stat-number">{{ contarPorRol('Director') }}</span>
          <span class="stat-label">Directores</span>
        </div>
      </div>
    </div>

    <!-- Estado de Carga compacto -->
    <div v-if="cargando" class="loading-state compact">
      <div class="spinner small"></div>
      <p>Cargando usuarios...</p>
    </div>

    <div v-if="error && !cargando" class="error-state compact">
      <div class="error-icon">⚠</div>
      <p>{{ error }}</p>
      <button @click="cargarUsuarios" class="btn btn-outline btn-small">Reintentar</button>
    </div>

    <!-- Tabla de Usuarios compacta -->
    <div v-if="!cargando && usuariosFiltrados.length > 0" class="table-container compact">
      <div class="table-header compact">
        <h3>Usuarios ({{ usuariosFiltrados.length }})</h3>
        <div class="table-info">
          <span v-if="filtroRol" class="filter-indicator">
            Filtrado por: <strong>{{ filtroRol }}</strong>
            <button @click="filtroRol = ''; aplicarFiltroRol()" class="clear-filter">×</button>
          </span>
        </div>
      </div>

      <div class="table-responsive compact">
        <table class="users-table compact">
          <thead>
            <tr>
              <th class="col-id">ID</th>
              <th class="col-name">Nombre</th>
              <th class="col-email">Email</th>
              <th class="col-role">Rol Actual</th>
              <th class="col-new-role">Nuevo Rol</th>
              <th class="col-actions">Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="usuario in usuariosFiltrados" :key="usuario.id" class="user-row">
              <td class="user-id">{{ usuario.id }}</td>
              <td class="user-name">
                <div class="name-display">
                  <strong>{{ usuario.nombre || 'N/A' }} {{ usuario.apellido || '' }}</strong>
                </div>
              </td>
              <td class="user-email compact">{{ usuario.email }}</td>
              <td class="user-role">
                <span :class="['role-badge', `role-${usuario.rol?.toLowerCase()}`]">
                  {{ usuario.rol }}
                </span>
              </td>
              <td class="user-new-role">
                <select 
                  v-model="usuario.nuevoRol" 
                  class="role-select compact"
                  :class="{ changed: usuario.nuevoRol !== usuario.rol }"
                >
                  <option value="Estudiante">Estudiante</option>
                  <option value="Profesor">Profesor</option>
                  <option value="Director">Director</option>
                </select>
              </td>
              <td class="user-actions">
                <button 
                  @click="actualizarRol(usuario)" 
                  class="btn btn-update btn-small"
                  :disabled="usuario.nuevoRol === usuario.rol || actualizandoId === usuario.id"
                  :class="{ loading: actualizandoId === usuario.id }"
                >
                  <span v-if="actualizandoId === usuario.id" class="btn-spinner small"></span>
                  {{ actualizandoId === usuario.id ? '' : 'Actualizar' }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Estado Sin Datos compacto -->
    <div v-if="!cargando && usuariosFiltrados.length === 0" class="empty-state compact">
      <div class="empty-icon">👥</div>
      <h3>No hay usuarios</h3>
      <p v-if="filtroRol">No se encontraron usuarios con rol "{{ filtroRol }}"</p>
      <p v-else>No se encontraron usuarios en {{ tenantName }}</p>
      <button @click="mostrarModalCrear = true" class="btn btn-success btn-small">
        Crear Usuario
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
          <button @click="cerrarModal" class="btn btn-outline btn-small">Cancelar</button>
          <button 
            @click="crearUsuario" 
            :disabled="creandoUsuario || (nuevoUsuario.email && !validarEmailDominio(nuevoUsuario.email))" 
            class="btn btn-success btn-small"
          >
            <span v-if="creandoUsuario" class="btn-spinner small"></span>
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
    const filtroRol = ref('')
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
      let filtered = usuarios.value
      
      // Aplicar filtro de búsqueda
      if (filtroBusqueda.value) {
        const search = filtroBusqueda.value.toLowerCase()
        filtered = filtered.filter(usuario => 
          usuario.nombre?.toLowerCase().includes(search) ||
          usuario.apellido?.toLowerCase().includes(search) ||
          usuario.email?.toLowerCase().includes(search) ||
          usuario.rol?.toLowerCase().includes(search)
        )
      }
      
      return filtered
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

    const aplicarFiltroRol = async () => {
      cargando.value = true
      error.value = ''
      
      try {
        const { data: { session } } = await supabase.auth.getSession()
        if (!session?.user?.email) {
          throw new Error('No hay usuario autenticado')
        }

        let url = `${API_BASE_URL}/mi-tenant`
        if (filtroRol.value) {
          url = `${API_BASE_URL}/mi-tenant/filtrar?rol=${encodeURIComponent(filtroRol.value)}`
        }

        const response = await fetch(url, {
          headers: { 'X-User-Email': session.user.email }
        })
        
        if (!response.ok) throw new Error(`Error HTTP: ${response.status}`)
        
        const data = await response.json()
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

    const cargarUsuarios = async () => {
      // Resetear filtros al cargar
      filtroRol.value = ''
      await aplicarFiltroRol()
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
          throw new Error(`Error HTTP: ${response.status} - ${errorText}`)
        }

        const resultado = await response.json()
        mostrarAlerta('Usuario creado correctamente', 'success')
        
        // Cerrar modal y recargar usuarios
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
      // Resetear el formulario
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
      filtroRol,
      mensajeAlerta,
      nuevoUsuario,
      cargarUsuarios,
      actualizarRol,
      crearUsuario,
      cerrarModal,
      contarPorRol,
      mostrarAlerta,
      validarEmailDominio,
      aplicarFiltroRol
    }
  }
}
</script>

<style scoped>
/* Estilos mejorados y responsive */
.dashboard-container {
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 16px;
}

/* Header compacto */
.dashboard-header.compact {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  flex-wrap: wrap;
  gap: 16px;
}

.header-main {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
}

.title {
  color: white;
  font-size: 1.75rem;
  font-weight: 700;
  margin: 0;
  text-shadow: 0 2px 4px rgba(0,0,0,0.3);
}

.user-badge {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.user-email {
  color: #2d3748;
  font-weight: 500;
  background: rgba(255,255,255,0.9);
  padding: 6px 12px;
  border-radius: 6px;
  backdrop-filter: blur(10px);
  font-size: 0.875rem;
}

.tenant-badge {
  background: rgba(255,255,255,0.2);
  color: white;
  padding: 6px 12px;
  border-radius: 16px;
  font-weight: 600;
  backdrop-filter: blur(10px);
  font-size: 0.875rem;
}

/* Alertas compactas */
.alert.compact {
  display: flex;
  align-items: center;
  padding: 12px 16px;
  border-radius: 8px;
  margin-bottom: 16px;
  font-weight: 500;
  gap: 8px;
  font-size: 0.875rem;
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
  font-size: 1.1rem;
  cursor: pointer;
  opacity: 0.7;
}

.alert-close:hover {
  opacity: 1;
}

/* Panel de Control compacto */
.control-panel.compact {
  background: white;
  border-radius: 12px;
  padding: 20px;
  margin-bottom: 20px;
  box-shadow: 0 4px 16px rgba(0,0,0,0.1);
}

.control-group {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  margin-bottom: 16px;
}

/* Filtros */
.filters {
  display: flex;
  gap: 16px;
  align-items: center;
  margin-bottom: 16px;
  flex-wrap: wrap;
}

.filter-group {
  display: flex;
  align-items: center;
  gap: 8px;
}

.filter-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #2d3748;
}

.filter-select {
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 0.875rem;
  background: white;
  color: #2d3748;
}

.search-group {
  display: flex;
  align-items: center;
}

.search-input.compact {
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 0.875rem;
  width: 200px;
  color: #2d3748;
}

.search-input.compact:focus {
  outline: none;
  border-color: #667eea;
}

/* Stats compactas */
.stats.compact {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

.stat-card.compact {
  background: #f8f9fa;
  padding: 12px 16px;
  border-radius: 8px;
  text-align: center;
  min-width: 80px;
  cursor: pointer;
  transition: all 0.2s ease;
  border: 2px solid transparent;
}

.stat-card.compact:hover {
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.stat-card.compact.active {
  background-color: #e3f2fd;
  border-color: #2196f3;
}

.stat-number {
  display: block;
  font-size: 1.25rem;
  font-weight: 700;
  color: #667eea;
}

.stat-label {
  font-size: 0.75rem;
  color: #6c757d;
  font-weight: 500;
}

/* Botones compactos */
.btn-small {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  border: none;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  text-decoration: none;
  font-size: 0.8rem;
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
  transform: translateY(-1px);
}

.btn-success {
  background: #28a745;
  color: white;
}

.btn-success:hover:not(:disabled) {
  background: #218838;
  transform: translateY(-1px);
}

.btn-update {
  background: #17a2b8;
  color: white;
  padding: 6px 12px;
  font-size: 0.75rem;
}

.btn-update:hover:not(:disabled) {
  background: #138496;
}

.btn-outline {
  background: transparent;
  border: 1px solid #6c757d;
  color: #6c757d;
}

.btn-outline:hover {
  background: #6c757d;
  color: white;
}

.btn-icon {
  font-size: 0.9rem;
}

.btn-spinner.small {
  width: 12px;
  height: 12px;
  border: 2px solid transparent;
  border-top: 2px solid currentColor;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Estados compactos */
.loading-state.compact, .error-state.compact, .empty-state.compact {
  text-align: center;
  padding: 40px 20px;
  background: white;
  border-radius: 12px;
  box-shadow: 0 4px 16px rgba(0,0,0,0.1);
  margin: 0 16px;
}

.spinner.small {
  width: 32px;
  height: 32px;
  border: 3px solid #f3f3f3;
  border-top: 3px solid #667eea;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin: 0 auto 16px;
}

.error-icon, .empty-icon {
  font-size: 2.5rem;
  margin-bottom: 16px;
}

/* Tabla compacta */
.table-container.compact {
  background: white;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 4px 16px rgba(0,0,0,0.1);
  margin: 0 16px;
}

.table-header.compact {
  padding: 16px 20px;
  border-bottom: 1px solid #e9ecef;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
}

.table-header.compact h3 {
  margin: 0;
  color: #2d3748;
  font-size: 1.1rem;
}

.filter-indicator {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.8rem;
  background: #e3f2fd;
  padding: 4px 12px;
  border-radius: 12px;
  color: #1976d2;
}

.clear-filter {
  background: none;
  border: none;
  cursor: pointer;
  font-size: 1.1rem;
  line-height: 1;
  color: #666;
  padding: 0;
  width: 20px;
  height: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.clear-filter:hover {
  color: #333;
}

.table-responsive.compact {
  overflow-x: auto;
}

.users-table.compact {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.8rem;
}

.users-table.compact th {
  background: #f8f9fa;
  padding: 12px 8px;
  text-align: left;
  font-weight: 600;
  color: #2d3748;
  border-bottom: 1px solid #e9ecef;
  white-space: nowrap;
}

.users-table.compact td {
  padding: 10px 8px;
  border-bottom: 1px solid #e9ecef;
  color: #2d3748;
  vertical-align: middle;
}

.users-table.compact tr:hover {
  background: #f8f9fa;
}

/* Columnas compactas */
.col-id { width: 60px; }
.col-name { width: 140px; }
.col-email { width: 180px; }
.col-role { width: 100px; }
.col-new-role { width: 120px; }
.col-actions { width: 90px; }

.user-row {
  height: 44px;
}

.name-display {
  line-height: 1.2;
}

.user-email.compact {
  font-size: 0.75rem;
  color: #666;
}

/* Badges de rol compactos */
.role-badge {
  padding: 4px 8px;
  border-radius: 12px;
  font-size: 0.7rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.3px;
  display: inline-block;
  text-align: center;
  min-width: 70px;
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

/* Select de roles compacto */
.role-select.compact {
  padding: 6px 8px;
  border: 1px solid #e2e8f0;
  border-radius: 4px;
  font-size: 0.75rem;
  transition: all 0.3s ease;
  background: white;
  color: #2d3748;
  width: 100%;
}

.role-select.compact:focus {
  outline: none;
  border-color: #667eea;
}

.role-select.compact.changed {
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
  padding: 16px;
}

.modal {
  background: white;
  border-radius: 12px;
  width: 100%;
  max-width: 450px;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 16px 48px rgba(0,0,0,0.3);
}

.modal-header {
  padding: 20px;
  border-bottom: 1px solid #e9ecef;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.modal-header h3 {
  margin: 0;
  color: #2d3748;
  font-size: 1.2rem;
}

.modal-close {
  background: none;
  border: none;
  font-size: 1.3rem;
  cursor: pointer;
  color: #6c757d;
  padding: 0;
  width: 28px;
  height: 28px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.modal-close:hover {
  color: #495057;
}

.modal-body {
  padding: 20px;
}

.modal-footer {
  padding: 20px;
  border-top: 1px solid #e9ecef;
  display: flex;
  gap: 10px;
  justify-content: flex-end;
}

/* Formulario */
.user-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.form-group label {
  font-weight: 600;
  color: #2d3748;
  font-size: 0.8rem;
}

.form-input, .form-select {
  padding: 10px 12px;
  border: 1px solid #e2e8f0;
  border-radius: 6px;
  font-size: 0.8rem;
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
  font-size: 0.7rem;
  margin-top: 2px;
}

/* Estilos para validación */
.form-input.invalid {
  border-color: #e53e3e;
  background-color: #fed7d7;
}

.form-error {
  color: #e53e3e;
  font-size: 0.7rem;
  margin-top: 2px;
  display: block;
}

/* Responsive */
@media (max-width: 768px) {
  .dashboard-header.compact {
    flex-direction: column;
    align-items: flex-start;
  }
  
  .header-main {
    flex-direction: column;
    align-items: flex-start;
    gap: 12px;
  }
  
  .control-panel.compact {
    flex-direction: column;
    align-items: stretch;
  }
  
  .filters {
    flex-direction: column;
    align-items: stretch;
  }
  
  .filter-group {
    justify-content: space-between;
  }
  
  .search-input.compact {
    width: 100%;
  }
  
  .stats.compact {
    justify-content: center;
  }
  
  .table-header.compact {
    flex-direction: column;
    align-items: stretch;
  }
  
  .users-table.compact {
    font-size: 0.75rem;
  }
  
  .users-table.compact th,
  .users-table.compact td {
    padding: 8px 6px;
  }
  
  .modal {
    margin: 16px;
    width: calc(100% - 32px);
  }
  
  .modal-footer {
    flex-direction: column;
  }
}

@media (max-width: 480px) {
  .dashboard-container {
    padding: 12px;
  }
  
  .title {
    font-size: 1.5rem;
  }
  
  .control-group {
    flex-direction: column;
    width: 100%;
  }
  
  .btn-small {
    width: 100%;
    justify-content: center;
  }
  
  .stats.compact {
    flex-direction: column;
  }
  
  .stat-card.compact {
    width: 100%;
  }
  
  .col-id { width: 50px; }
  .col-name { width: 120px; }
  .col-email { width: 150px; }
  .col-role { width: 80px; }
  .col-new-role { width: 100px; }
  .col-actions { width: 80px; }
}
</style>