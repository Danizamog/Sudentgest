<template>
  <div class="home-container">
    <h1>Bienvenido, {{ username }}</h1>
    <p>Tenant actual: {{ tenant }}</p>

    <div class="usuarios">
      <h2>Usuarios en tenant_ucb</h2>
      <pre>{{ JSON.stringify(usuariosUcb, null, 2) }}</pre>

      <h2>Usuarios en tenant_upb</h2>
      <pre>{{ JSON.stringify(usuariosUpb, null, 2) }}</pre>
      
      <h2>Usuarios en tenant_gmail</h2>
      <pre>{{ JSON.stringify(usuariosGmail, null, 2) }}</pre>
    </div>

    <button class="btn-logout" @click="handleLogout">
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

// Configuración de tenants
const TENANT_CONFIG = {
  '@ucb.edu.bo': {
    name: 'tenant_ucb',
    rpcFunction: 'get_usuarios_ucb'
  },
  '@upb.edu.bo': {
    name: 'tenant_upb',
    rpcFunction: 'get_usuarios_upb'
  },
  '@gmail.com': {
    name: 'tenant_gmail',
    rpcFunction: 'get_usuarios_gmail'
  }
}

function getTenantFromEmail(email) {
  for (const [domain, config] of Object.entries(TENANT_CONFIG)) {
    if (email.endsWith(domain)) {
      return config.name
    }
  }
  return null
}

async function fetchUsuariosPorTenant(tenantKey) {
  const config = Object.values(TENANT_CONFIG).find(t => t.name === tenantKey)
  if (!config) return null
  
  const { data, error } = await supabase.rpc(config.rpcFunction)
  if (error) {
    console.error(`Error consultando ${tenantKey}:`, error)
    return null
  }
  return data
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

    // Consultar usuarios de todos los tenants en paralelo
    const [ucb, upb, gmail] = await Promise.all([
      fetchUsuariosPorTenant('tenant_ucb'),
      fetchUsuariosPorTenant('tenant_upb'),
      fetchUsuariosPorTenant('tenant_gmail')
    ])

    usuariosUcb.value = ucb || []
    usuariosUpb.value = upb || []
    usuariosGmail.value = gmail || []

  } catch (error) {
    console.error('Error al cargar Home:', error)
  } finally {
    loading.value = false
  }
})

async function handleLogout() {
  try {
    await supabase.auth.signOut()
    localStorage.removeItem('token')
    router.push('/signin')
  } catch (error) {
    console.error('Error al cerrar sesión:', error)
  }
}
</script>

<style scoped>
.home-container {
  text-align: center;
  margin-top: 3rem;
  padding: 1rem;
}

.usuarios {
  margin-top: 2rem;
  text-align: left;
  display: inline-block;
  max-width: 90%;
}

pre {
  background: #f3f4f6;
  padding: 1rem;
  border-radius: 0.5rem;
  overflow-x: auto;
  max-height: 300px;
  overflow-y: auto;
  font-size: 0.875rem;
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
}

.btn-logout:hover {
  background-color: #dc2626;
}

.loading {
  margin-top: 2rem;
  color: #6b7280;
}
</style>