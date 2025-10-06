<template>
  <div class="bd-container">
    <h1>Gestión de Usuarios - {{ tenantName }}</h1>
    
    <div class="user-info">
      <p><strong>Usuario:</strong> {{ currentUserEmail }}</p>
      <p><strong>Tenant:</strong> {{ tenantName }}</p>
    </div>

    <div class="controls">
      <button @click="cargarUsuarios" class="btn btn-primary" :disabled="cargando">
        {{ cargando ? 'Cargando...' : 'Actualizar Lista' }}
      </button>
    </div>

    <div v-if="cargando" class="loading">
      Cargando usuarios de {{ tenantName }}...
    </div>

    <div v-if="error" class="error">
      {{ error }}
    </div>

    <!-- Tabla del tenant del usuario -->
    <div v-if="usuarios.length > 0" class="table-section">
      <h2>Usuarios de {{ tenantName }} ({{ usuarios.length }})</h2>
      <table class="data-table">
        <thead>
          <tr>
            <th>ID</th>
            <th>Nombre</th>
            <th>Apellido</th>
            <th>Email</th>
            <th>Rol</th>
            <th>Acciones</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="usuario in usuarios" :key="usuario.id">
            <td>{{ usuario.id }}</td>
            <td>{{ usuario.nombre || 'N/A' }}</td>
            <td>{{ usuario.apellido || 'N/A' }}</td>
            <td>{{ usuario.email }}</td>
            <td>
              <select :value="usuario.rol" @change="actualizarRol(usuario.id, $event.target.value)">
                <option value="Estudiante">Estudiante</option>
                <option value="Profesor">Profesor</option>
                <option value="Director">Director</option>
              </select>
            </td>
            <td>
              <button @click="confirmarActualizacion(usuario.id, usuario.rol)" 
                      class="btn btn-small">
                Actualizar
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="!cargando && usuarios.length === 0" class="no-data">
      No hay usuarios registrados en {{ tenantName }}.
    </div>
  </div>
</template>

<script>
import { ref, onMounted } from 'vue'
import { supabase } from '../supabase'

export default {
  name: 'BDView',
  setup() {
    const usuarios = ref([])
    const currentUserEmail = ref('')
    const tenantName = ref('')
    const cargando = ref(false)
    const error = ref('')

    const API_BASE_URL = 'http://localhost:5009/api/usuarios'

    // Función para obtener tenant del email (igual que el backend)
    const getTenantFromEmail = (email) => {
      if (email.endsWith('@ucb.edu.bo')) return 'ucb.edu.bo'
      if (email.endsWith('@upb.edu.bo')) return 'upb.edu.bo'
      if (email.endsWith('@gmail.com')) return 'gmail.com'
      return 'unknown'
    }

    // Obtener nombre amigable del tenant
    const getTenantDisplayName = (tenant) => {
      switch (tenant) {
        case 'ucb.edu.bo': return 'UCB'
        case 'upb.edu.bo': return 'UPB'
        case 'gmail.com': return 'Gmail'
        default: return tenant
      }
    }

    const cargarUsuarios = async () => {
      cargando.value = true
      error.value = ''
      
      try {
        // Obtener el usuario actual de Supabase
        const { data: { session } } = await supabase.auth.getSession()
        if (!session?.user?.email) {
          throw new Error('No hay usuario autenticado')
        }

        currentUserEmail.value = session.user.email
        const tenant = getTenantFromEmail(session.user.email)
        tenantName.value = getTenantDisplayName(tenant)

        // Llamar al backend con el email del usuario en los headers
        const response = await fetch(`${API_BASE_URL}/mi-tenant`, {
          headers: {
            'X-User-Email': session.user.email
          }
        })
        
        if (!response.ok) {
          throw new Error(`Error HTTP: ${response.status}`)
        }
        
        const data = await response.json()
        usuarios.value = data.usuarios || []
        
        console.log(`✅ Cargados ${usuarios.value.length} usuarios de ${tenantName.value}`)
      } catch (err) {
        error.value = `Error al cargar usuarios: ${err.message}`
        console.error('Error:', err)
      } finally {
        cargando.value = false
      }
    }

    const actualizarRol = async (usuarioId, nuevoRol) => {
      try {
        console.log(`Actualizando rol del usuario ${usuarioId} a ${nuevoRol}`)
        
        const { data: { session } } = await supabase.auth.getSession()
        if (!session?.user?.email) {
          throw new Error('No hay usuario autenticado')
        }

        const response = await fetch(`${API_BASE_URL}/${usuarioId}/rol`, {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json',
            'X-User-Email': session.user.email
          },
          body: JSON.stringify({ rol: nuevoRol })
        })

        if (!response.ok) {
          throw new Error(`Error HTTP: ${response.status}`)
        }

        const resultado = await response.json()
        console.log('Rol actualizado:', resultado)
        
        // Recargar la lista
        await cargarUsuarios()
        
        alert(`Rol actualizado correctamente para el usuario ${usuarioId}`)
      } catch (err) {
        console.error('Error actualizando rol:', err)
        error.value = `Error al actualizar rol: ${err.message}`
        alert('Error al actualizar el rol')
      }
    }

    const confirmarActualizacion = (usuarioId, rolActual) => {
      if (confirm(`¿Estás seguro de que quieres actualizar el rol del usuario ${usuarioId}?`)) {
        const select = document.querySelector(`select[data-usuario="${usuarioId}"]`)
        if (select) {
          actualizarRol(usuarioId, select.value)
        }
      }
    }

    onMounted(() => {
      cargarUsuarios()
    })

    return {
      usuarios,
      currentUserEmail,
      tenantName,
      cargando,
      error,
      cargarUsuarios,
      actualizarRol,
      confirmarActualizacion
    }
  }
}
</script>

<style scoped>
.bd-container {
  padding: 20px;
  max-width: 1200px;
  margin: 0 auto;
}

.user-info {
  background-color: #e8f5e8;
  padding: 15px;
  border-radius: 8px;
  margin-bottom: 20px;
  border-left: 4px solid #4caf50;
}

.user-info p {
  margin: 5px 0;
  color: #2e7d32;
}

.controls {
  margin-bottom: 20px;
}

.btn {
  padding: 10px 15px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-primary {
  background-color: #007bff;
  color: white;
}

.btn-small {
  padding: 5px 10px;
  font-size: 0.8em;
  background-color: #28a745;
  color: white;
}

.btn:hover:not(:disabled) {
  opacity: 0.8;
}

.loading, .error, .no-data {
  padding: 20px;
  text-align: center;
  margin: 20px 0;
  border-radius: 4px;
}

.loading {
  background-color: #e3f2fd;
  color: #1976d2;
}

.error {
  background-color: #ffebee;
  color: #c62828;
}

.no-data {
  background-color: #f5f5f5;
  color: #666;
}

.table-section {
  margin-bottom: 30px;
}

.table-section h2 {
  color: #333;
  border-bottom: 2px solid #007bff;
  padding-bottom: 5px;
}

.data-table {
  width: 100%;
  border-collapse: collapse;
  margin-top: 10px;
}

.data-table th,
.data-table td {
  padding: 12px;
  text-align: left;
  border-bottom: 1px solid #ddd;
}

.data-table th {
  background-color: #f8f9fa;
  font-weight: bold;
}

.data-table tr:hover {
  background-color: #f5f5f5;
}

select {
  padding: 5px;
  border: 1px solid #ddd;
  border-radius: 4px;
}
</style>