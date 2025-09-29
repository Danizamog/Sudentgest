<template>
  <div class="home-container">
    <h1>Bienvenido, {{ username }}</h1>
    <p>Tenant actual: {{ tenant }}</p>

    <button class="btn-logout" @click="handleLogout">
      Cerrar sesión
    </button>

    <div v-if="userData">
      <h2>Datos del usuario</h2>
      <pre>{{ userData }}</pre>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { supabase } from '../supabase'

const router = useRouter()
const username = ref('')
const tenant = ref('')
const userData = ref(null)

// Obtener token del localStorage
const token = localStorage.getItem('token')
const BACKEND_URL = import.meta.env.VITE_BACKEND_URL || 'http://localhost:5002'


// Función para obtener tenant desde el email
function getTenantFromEmail(email) {
  const domain = email.split('@')[1]?.toLowerCase()
  if (domain === 'gmail.com') return 'tenant_upb'
  if (domain === 'ucb.edu.bo') return 'tenant_ucb'
  return null
}

onMounted(async () => {
  try {
    // Obtener sesión actual
    const { data: { session } } = await supabase.auth.getSession()
    if (!session?.user) {
      router.push('/signin')
      return
    }

    // Nombre y tenant
    username.value = session.user.email
    tenant.value = getTenantFromEmail(username.value)

    // Obtener datos del usuario desde el backend según tenant
    if (tenant.value) {
      const res = await fetch(`${BACKEND_URL}/api/users/${tenant.value}/me`, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      })
      if (res.ok) {
        userData.value = await res.json()
      } else {
        console.warn('No se pudo obtener datos del usuario')
      }
    }
  } catch (e) {
    console.error('Error al cargar home:', e)
  }
})

async function handleLogout() {
  try {
    await supabase.auth.signOut()
    localStorage.removeItem('token')
    router.push('/signin')
  } catch (e) {
    console.error('Error al cerrar sesión:', e)
  }
}
</script>

<style scoped>
.home-container {
  text-align: center;
  margin-top: 3rem;
}
.btn-logout {
  margin-top: 2rem;
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 0.75rem;
  background-color: #ef4444;
  color: white;
  font-weight: bold;
  cursor: pointer;
  transition: background 0.3s ease;
}
.btn-logout:hover {
  background-color: #b91c1c;
}
</style>
