<template>
  <div class="home-container">
    <h1>Bienvenido, {{ username }}</h1>
    <p>Tenant actual: {{ tenant }}</p>

    <div v-if="loading" class="loading">
      <p>Cargando usuarios...</p>
    </div>

    <div v-else class="usuarios">
      <div class="tenant-section">
        <h2>Usuarios en tenant_ucb</h2>
        <div v-if="usuariosUcb.length === 0" class="no-data">
          No hay usuarios registrados
        </div>
        <pre v-else>{{ JSON.stringify(usuariosUcb, null, 2) }}</pre>
      </div>

      <div class="tenant-section">
        <h2>Usuarios en tenant_upb</h2>
        <div v-if="usuariosUpb.length === 0" class="no-data">
          No hay usuarios registrados
        </div>
        <pre v-else>{{ JSON.stringify(usuariosUpb, null, 2) }}</pre>
      </div>
      
      <div class="tenant-section">
        <h2>Usuarios en tenant_gmail</h2>
        <div v-if="usuariosGmail.length === 0" class="no-data">
          No hay usuarios registrados
        </div>
        <pre v-else>{{ JSON.stringify(usuariosGmail, null, 2) }}</pre>
      </div>
    </div>

    <button class="btn-logout" @click="handleLogout" :disabled="loading">
      Cerrar sesión
    </button>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { supabase } from '../supabase'

const router = useRouter()
const username = ref('')
const tenant = ref('')
const usuariosUcb = ref([])
const usuariosUpb = ref([])
const usuariosGmail = ref([])
const loading = ref(false)

function getTenantFromEmail(email) {
  if (email.endsWith('@ucb.edu.bo')) return 'tenant_ucb'
  if (email.endsWith('@upb.edu.bo')) return 'tenant_upb'
  if (email.endsWith('@gmail.com')) return 'tenant_gmail'
  return null
}

// 🔹 NUEVA FUNCIÓN: Obtener usuarios desde el backend (REST API)
async function fetchUsuariosDesdeBackend(tenantDomain) {
  try {
    const token = localStorage.getItem('token')
    if (!token) {
      console.error('No hay token disponible')
      return []
    }

    const backendUrl = getBackendUrl()
    const response = await fetch(`${backendUrl}/api/usuarios/${tenantDomain}`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    })

    if (!response.ok) {
      const errorText = await response.text()
      console.error(`Error consultando ${tenantDomain}:`, errorText)
      return []
    }

    const data = await response.json()
    return data.usuarios || []
  } catch (error) {
    console.error(`Error fetch ${tenantDomain}:`, error)
    return []
  }
}

// 🔹 FUNCIÓN: Obtener URL del backend
function getBackendUrl() {
  if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
    return 'http://localhost:5002'
  }
  return '/api/auth'
}

onMounted(async () => {
  try {
    loading.value = true
    
    // Verificar sesión
    const { data: { session } } = await supabase.auth.getSession()
    if (!session?.user) {
      router.push('/signin')
      return
    }

    username.value = session.user.email
    tenant.value = getTenantFromEmail(username.value)

    // 🔹 CONSULTAR USUARIOS DESDE EL BACKEND (REST API)
    console.log('🔍 Cargando usuarios desde backend REST API...')
    
    const [ucb, upb, gmail] = await Promise.all([
      fetchUsuariosDesdeBackend('ucb.edu.bo'),
      fetchUsuariosDesdeBackend('upb.edu.bo'),
      fetchUsuariosDesdeBackend('gmail.com')
    ])

    usuariosUcb.value = ucb
    usuariosUpb.value = upb
    usuariosGmail.value = gmail

    console.log('✅ Usuarios cargados via REST API:', {
      ucb: usuariosUcb.value.length,
      upb: usuariosUpb.value.length, 
      gmail: usuariosGmail.value.length
    })

  } catch (error) {
    console.error('Error al cargar Home:', error)
  } finally {
    loading.value = false
  }
})

async function handleLogout() {
  try {
    loading.value = true
    await supabase.auth.signOut()
    localStorage.removeItem('token')
    router.push('/signin')
  } catch (error) {
    console.error('Error al cerrar sesión:', error)
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.home-container {
  text-align: center;
  margin-top: 3rem;
  padding: 1rem;
  max-width: 1200px;
  margin-left: auto;
  margin-right: auto;
}

.usuarios {
  margin-top: 2rem;
  text-align: left;
}

.tenant-section {
  margin-bottom: 2rem;
  padding: 1.5rem;
  border: 1px solid #e5e7eb;
  border-radius: 0.75rem;
  background: white;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.tenant-section h2 {
  margin-top: 0;
  color: #374151;
  border-bottom: 2px solid #4f46e5;
  padding-bottom: 0.5rem;
}

.no-data {
  text-align: center;
  color: #6b7280;
  font-style: italic;
  padding: 2rem;
}

pre {
  background: #f8fafc;
  padding: 1rem;
  border-radius: 0.5rem;
  overflow-x: auto;
  max-height: 400px;
  overflow-y: auto;
  font-size: 0.875rem;
  border: 1px solid #e2e8f0;
}

.loading {
  margin-top: 2rem;
  color: #6b7280;
  font-size: 1.125rem;
}

.btn-logout {
  margin-top: 2rem;
  padding: 0.75rem 1.5rem;
  background-color: #ef4444;
  color: white;
  border: none;
  border-radius: 0.375rem;
  cursor: pointer;
  transition: background-color 0.2s;
  font-size: 1rem;
}

.btn-logout:hover:not(:disabled) {
  background-color: #dc2626;
}

.btn-logout:disabled {
  background-color: #9ca3af;
  cursor: not-allowed;
}

@media (max-width: 768px) {
  .home-container {
    margin-top: 1rem;
    padding: 0.5rem;
  }
  
  .tenant-section {
    padding: 1rem;
  }
  
  pre {
    font-size: 0.75rem;
    padding: 0.75rem;
  }
}
</style>