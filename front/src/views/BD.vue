<template>
  <div class="bd-container">
    <h1>Gestión de Usuarios - Base de Datos</h1>
    
    <div class="controls">
      <button @click="cargarTodasTablas" class="btn btn-primary">
        Cargar Todas las Tablas
      </button>
      <button @click="cargarUcb" class="btn btn-secondary">
        Solo UCB
      </button>
      <button @click="cargarUpb" class="btn btn-secondary">
        Solo UPB
      </button>
      <button @click="cargarGmail" class="btn btn-secondary">
        Solo Gmail
      </button>
    </div>

    <div v-if="cargando" class="loading">
      Cargando datos...
    </div>

    <div v-if="error" class="error">
      {{ error }}
    </div>

    <!-- Tabla UCB -->
    <div v-if="usuariosUcb.length > 0" class="table-section">
      <h2>Usuarios UCB ({{ usuariosUcb.length }})</h2>
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
          <tr v-for="usuario in usuariosUcb" :key="'ucb-' + usuario.id">
            <td>{{ usuario.id }}</td>
            <td>{{ usuario.nombre || 'N/A' }}</td>
            <td>{{ usuario.apellido || 'N/A' }}</td>
            <td>{{ usuario.email }}</td>
            <td>
              <select :value="usuario.rol" @change="actualizarRol('ucb', usuario.id, $event.target.value)">
                <option value="Estudiante">Estudiante</option>
                <option value="Profesor">Profesor</option>
                <option value="Director">Director</option>
              </select>
            </td>
            <td>
              <button @click="confirmarActualizacion('ucb', usuario.id, usuario.rol)" 
                      class="btn btn-small">
                Actualizar
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Repite la misma estructura para UPB y Gmail -->
    <!-- Tabla UPB -->
    <div v-if="usuariosUpb.length > 0" class="table-section">
      <h2>Usuarios UPB ({{ usuariosUpb.length }})</h2>
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
          <tr v-for="usuario in usuariosUpb" :key="'upb-' + usuario.id">
            <td>{{ usuario.id }}</td>
            <td>{{ usuario.nombre || 'N/A' }}</td>
            <td>{{ usuario.apellido || 'N/A' }}</td>
            <td>{{ usuario.email }}</td>
            <td>
              <select :value="usuario.rol" @change="actualizarRol('upb', usuario.id, $event.target.value)">
                <option value="Estudiante">Estudiante</option>
                <option value="Profesor">Profesor</option>
                <option value="Director">Director</option>
              </select>
            </td>
            <td>
              <button @click="confirmarActualizacion('upb', usuario.id, usuario.rol)" 
                      class="btn btn-small">
                Actualizar
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Tabla Gmail -->
    <div v-if="usuariosGmail.length > 0" class="table-section">
      <h2>Usuarios Gmail ({{ usuariosGmail.length }})</h2>
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
          <tr v-for="usuario in usuariosGmail" :key="'gmail-' + usuario.id">
            <td>{{ usuario.id }}</td>
            <td>{{ usuario.nombre || 'N/A' }}</td>
            <td>{{ usuario.apellido || 'N/A' }}</td>
            <td>{{ usuario.email }}</td>
            <td>
              <select :value="usuario.rol" @change="actualizarRol('gmail', usuario.id, $event.target.value)">
                <option value="Estudiante">Estudiante</option>
                <option value="Profesor">Profesor</option>
                <option value="Director">Director</option>
              </select>
            </td>
            <td>
              <button @click="confirmarActualizacion('gmail', usuario.id, usuario.rol)" 
                      class="btn btn-small">
                Actualizar
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="!cargando && usuariosUcb.length === 0 && usuariosUpb.length === 0 && usuariosGmail.length === 0" 
         class="no-data">
      No hay datos para mostrar. Haz clic en uno de los botones para cargar la información.
    </div>
  </div>
</template>

<script>
import { ref } from 'vue'

export default {
  name: 'BDView',
  setup() {
    const usuariosUcb = ref([])
    const usuariosUpb = ref([])
    const usuariosGmail = ref([])
    const cargando = ref(false)
    const error = ref('')

    const API_BASE_URL = 'http://localhost:5009/api/usuarios'

    const cargarTodasTablas = async () => {
      await cargarDatos(`${API_BASE_URL}/todas-tablas`, 'todas')
    }

    const cargarUcb = async () => {
      await cargarDatos(`${API_BASE_URL}/ucb`, 'ucb')
    }

    const cargarUpb = async () => {
      await cargarDatos(`${API_BASE_URL}/upb`, 'upb')
    }

    const cargarGmail = async () => {
      await cargarDatos(`${API_BASE_URL}/gmail`, 'gmail')
    }

    const cargarDatos = async (url, tipo) => {
      cargando.value = true
      error.value = ''
      
      try {
        const response = await fetch(url)
        
        if (!response.ok) {
          throw new Error(`Error HTTP: ${response.status}`)
        }
        
        const data = await response.json()
        
        if (tipo === 'todas') {
          usuariosUcb.value = data.ucbUsuarios || []
          usuariosUpb.value = data.upbUsuarios || []
          usuariosGmail.value = data.gmailUsuarios || []
        } else if (tipo === 'ucb') {
          usuariosUcb.value = data
          usuariosUpb.value = []
          usuariosGmail.value = []
        } else if (tipo === 'upb') {
          usuariosUpb.value = data
          usuariosUcb.value = []
          usuariosGmail.value = []
        } else if (tipo === 'gmail') {
          usuariosGmail.value = data
          usuariosUcb.value = []
          usuariosUpb.value = []
        }
      } catch (err) {
        error.value = `Error al cargar datos: ${err.message}`
        console.error('Error:', err)
      } finally {
        cargando.value = false
      }
    }

    const actualizarRol = async (tabla, usuarioId, nuevoRol) => {
      try {
        console.log(`Actualizando rol: ${tabla}, usuario: ${usuarioId}, nuevo rol: ${nuevoRol}`)
        
        const response = await fetch(`${API_BASE_URL}/${tabla}/${usuarioId}/rol`, {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({ rol: nuevoRol })
        })

        if (!response.ok) {
          throw new Error(`Error HTTP: ${response.status}`)
        }

        const resultado = await response.json()
        console.log('Rol actualizado:', resultado)
        
        // Recargar los datos para reflejar el cambio
        await cargarTodasTablas()
        
      } catch (err) {
        console.error('Error actualizando rol:', err)
        error.value = `Error al actualizar rol: ${err.message}`
      }
    }

    const confirmarActualizacion = (tabla, usuarioId, rolActual) => {
      if (confirm(`¿Estás seguro de que quieres actualizar el rol del usuario ${usuarioId} en ${tabla}?`)) {
        // Encuentra el select y obtiene el valor actual
        const select = document.querySelector(`select[data-tabla="${tabla}"][data-usuario="${usuarioId}"]`)
        if (select) {
          actualizarRol(tabla, usuarioId, select.value)
        }
      }
    }

    return {
      usuariosUcb,
      usuariosUpb,
      usuariosGmail,
      cargando,
      error,
      cargarTodasTablas,
      cargarUcb,
      cargarUpb,
      cargarGmail,
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

.controls {
  margin-bottom: 20px;
}

.btn {
  padding: 10px 15px;
  margin-right: 10px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.btn-primary {
  background-color: #007bff;
  color: white;
}

.btn-secondary {
  background-color: #6c757d;
  color: white;
}

.btn:hover {
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
.btn-small {
  padding: 5px 10px;
  font-size: 0.8em;
}

select {
  padding: 5px;
  border: 1px solid #ddd;
  border-radius: 4px;
}
</style>