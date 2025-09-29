<template>
  <div class="home-container">
    <h1>Bienvenido, {{ username }}</h1>
    <p>Tenant actual: {{ tenant }}</p>

    <div class="usuarios">
      <h2>Usuarios en tenant_ucb</h2>
      <pre>{{ usuariosUcb }}</pre>

      <h2>Usuarios en tenant_upb</h2>
      <pre>{{ usuariosUpb }}</pre>
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

function getTenantFromEmail(email) {
  if (email.endsWith('@ucb.edu.bo')) return 'tenant_ucb'
  if (email.endsWith('@upb.edu.bo')) return 'tenant_upb'
  if (email.endsWith('@gmail.com')) return 'tenant_gmail'
  return null
}

onMounted(async () => {
  try {
    // Sesión actual
    const { data: { session } } = await supabase.auth.getSession()
    if (!session?.user) {
      router.push('/signin')
      return
    }

    username.value = session.user.email
    tenant.value = getTenantFromEmail(username.value)

    // 🔹 Obtener usuarios de tenant_ucb
    const { data: ucb, error: errUcb } = await supabase.rpc('get_usuarios_ucb')
    if (errUcb) console.error('Error consultando tenant_ucb:', errUcb)
    else usuariosUcb.value = ucb

    // 🔹 Obtener usuarios de tenant_upb
    const { data: upb, error: errUpb } = await supabase.rpc('get_usuarios_upb')
    if (errUpb) console.error('Error consultando tenant_upb:', errUpb)
    else usuariosUpb.value = upb
   
    const { data: gmail, error: errGmail } = await supabase.rpc('get_usuarios_gmail')
    if (errGmail) console.error('Error consultando tenant_gmail:', errGmail)
    else console.log('Usuarios en tenant_gmail:', gmail)

  } catch (e) {
    console.error('Error al cargar Home:', e)
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
.usuarios {
  margin-top: 2rem;
  text-align: left;
  display: inline-block;
}
pre {
  background: #f3f4f6;
  padding: 1rem;
  border-radius: 0.5rem;
  overflow-x: auto;
}
.btn-logout {
  margin-top: 2rem;
  padding: 0.75rem 1.5rem;
  background-color: #ef4444;
  color: white;
  border: none;
  border-radius: 0.375rem;
  cursor: pointer;
}
</style>
