<template>
  <div class="signin-container">
    <h2 class="title">Iniciar sesión</h2>

    <form @submit.prevent="handleSignIn" class="signin-form">
      <div class="input-group">
        <label for="email">Correo</label>
        <input id="email" v-model="email" type="email" placeholder="correo@ucb.edu.bo o correo@gmail.com" required />
      </div>

      <div class="input-group">
        <label for="password">Contraseña</label>
        <input id="password" v-model="password" type="password" placeholder="••••••••" required />
      </div>

      <button type="submit" class="btn-primary" :disabled="loading">
        <span v-if="loading">Ingresando...</span>
        <span v-else>Entrar</span>
      </button>

      <div class="divider"><span>o</span></div>

      <button type="button" class="btn-google" @click="handleGoogleSignIn" :disabled="loading">
        <img src="https://www.svgrepo.com/show/355037/google.svg" alt="Google" />
        Iniciar con Google
      </button>

      <div class="domain-info">
        <p>🔐 Dominios soportados:</p>
        <ul>
          <li>@ucb.edu.bo - Base de datos UCB</li>
          <li>@gmail.com - Base de datos Gmail</li>
        </ul>
        <p class="auto-create">✅ Los usuarios se crean automáticamente al primer inicio de sesión</p>
      </div>

      <p v-if="error" class="error">{{ error }}</p>
    </form>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { getSupabaseClient, supabase } from '../supabase'

const email = ref('')
const password = ref('')
const error = ref(null)
const loading = ref(false)
const router = useRouter()

const getBackendUrl = () => {
  if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
    return 'http://localhost:5002'
  }
  return '/api/auth'
}

function getTenantFromEmail(email) {
  if (email.endsWith('@ucb.edu.bo')) return 'ucb.edu.bo'
  if (email.endsWith('@gmail.com')) return 'gmail.com'
  return null
}

async function processSession(session, userEmail) {
  console.log('🔹 ProcessSession iniciado', userEmail)
  
  const token = session.access_token
  localStorage.setItem('token', token)

  if (!userEmail) {
    error.value = 'No se pudo obtener el email del usuario.'
    return
  }

  const tenant = getTenantFromEmail(userEmail)
  if (!tenant) {
    error.value = 'Dominio de correo no permitido. Use @ucb.edu.bo o @gmail.com'
    return
  }

  try {
    const backendUrl = getBackendUrl()
    console.log('🔹 Sincronizando usuario con backend...', { email: userEmail, tenant })
    
    const res = await fetch(`${backendUrl}/api/auth/sync-user`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ tenant })
    })

    if (res.ok) {
      const result = await res.json()
      console.log('✅ Sync-user exitoso:', result)
      if (result.isNewUser) {
        console.log('🎉 Nuevo usuario creado automáticamente en', tenant)
      }
    } else {
      const backendError = await res.text()
      console.warn('⚠️ Sync-user falló:', backendError)
    }
  } catch (e) {
    console.warn('⚠️ No se pudo sincronizar con backend:', e)
  }

  // Redirigir al home independientemente del resultado del sync
  router.push('/home')
}

// Login con email/password
async function handleSignIn() {
  error.value = null
  loading.value = true
  try {
    const userEmail = email.value
    const tenant = getTenantFromEmail(userEmail)
    
    if (!tenant) {
      throw new Error('Dominio no permitido. Use @ucb.edu.bo o @gmail.com')
    }

    console.log('🔹 Iniciando sesión con:', userEmail, 'Tenant:', tenant)
    
    // Usar el cliente Supabase correcto según el dominio
    const supabaseClient = getSupabaseClient(userEmail)
    
    const { data, error: err } = await supabaseClient.auth.signInWithPassword({
      email: userEmail,
      password: password.value
    })
    
    if (err) throw err
    if (!data.session?.access_token) throw new Error('No se recibió token de Supabase.')
    
    await processSession(data.session, userEmail)
  } catch (e) {
    error.value = e.message || 'Error inesperado. Intenta nuevamente.'
    console.error('❌ Error en login:', e)
  } finally {
    loading.value = false
  }
}

// Login con Google - NECESITA SER CONFIGURADO EN AMBAS BASES
async function handleGoogleSignIn() {
  try {
    error.value = null
    loading.value = true

    // Para Google OAuth, necesitamos determinar a qué base de datos redirigir
    // Por ahora usamos la base de Gmail como predeterminada
    // Más adelante podemos implementar una selección
    
    const { error: err } = await supabase.auth.signInWithOAuth({
      provider: 'google',
      options: { 
        redirectTo: `${window.location.origin}/auth/callback`,
        queryParams: {
          prompt: 'select_account'
        }
      }
    })
    
    if (err) throw err
    
  } catch (e) {
    error.value = 'Error al iniciar sesión con Google.'
    console.error('Google OAuth error:', e)
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  const { data: { session } } = await supabase.auth.getSession()
  if (session?.access_token) {
    router.push('/home')
  }
})
</script>

<style scoped>
/* Tus estilos se mantienen igual */
.signin-container {
  max-width: 400px;
  margin: 5rem auto;
  padding: 2.5rem;
  border-radius: 1.5rem;
  box-shadow: 0 8px 24px rgba(0,0,0,0.1);
  background-color: #fff;
  font-family: 'Inter', sans-serif;
  text-align: center;
}

.title { 
  font-size: 1.75rem; 
  font-weight: 700; 
  margin-bottom: 2rem; 
  color: #1f2937; 
}

.signin-form .input-group { 
  margin-bottom: 1.5rem; 
  text-align: left; 
}

.signin-form label { 
  display: block; 
  margin-bottom: 0.4rem; 
  font-weight: 600; 
  color: #374151; 
}

.signin-form input { 
  width: 100%; 
  padding: 0.75rem; 
  border-radius: 0.75rem; 
  border: 1px solid #d1d5db; 
  background: #f9fafb; 
  transition: border 0.2s ease; 
}

.signin-form input:focus { 
  border-color: #4f46e5; 
  outline: none; 
  background: #fff; 
}

.btn-primary { 
  width: 100%; 
  padding: 0.85rem; 
  border: none; 
  border-radius: 0.75rem; 
  background-color: #4f46e5; 
  color: white; 
  font-weight: 600; 
  cursor: pointer; 
  transition: background 0.3s ease; 
  margin-top: 0.5rem; 
}

.btn-primary:disabled { 
  background-color: #a5b4fc; 
  cursor: not-allowed; 
}

.btn-primary:hover:not(:disabled) { 
  background-color: #3730a3; 
}

.divider { 
  display: flex; 
  align-items: center; 
  margin: 1.5rem 0; 
  color: #6b7280; 
  font-size: 0.9rem; 
}

.divider::before, .divider::after { 
  content: ''; 
  flex: 1; 
  height: 1px; 
  background: #e5e7eb; 
}

.divider span { 
  margin: 0 0.75rem; 
}

.btn-google { 
  width: 100%; 
  padding: 0.85rem; 
  border: 1px solid #d1d5db; 
  border-radius: 0.75rem; 
  background-color: #fff; 
  font-weight: 600; 
  color: #374151; 
  display: flex; 
  align-items: center; 
  justify-content: center; 
  gap: 0.75rem; 
  cursor: pointer; 
  transition: background 0.2s ease, border 0.2s ease; 
}

.btn-google:hover { 
  background-color: #f9fafb; 
  border-color: #9ca3af; 
}

.btn-google img { 
  width: 20px; 
  height: 20px; 
}

.domain-info {
  margin-top: 1.5rem;
  padding: 1rem;
  background: #f8fafc;
  border-radius: 0.75rem;
  text-align: left;
  font-size: 0.9rem;
}

.domain-info p {
  margin: 0 0 0.5rem 0;
  font-weight: 600;
  color: #374151;
}

.domain-info ul {
  margin: 0;
  padding-left: 1.2rem;
  color: #6b7280;
}

.domain-info li {
  margin-bottom: 0.25rem;
}

.auto-create {
  margin-top: 0.5rem !important;
  color: #059669 !important;
  font-size: 0.8rem !important;
  font-weight: 500 !important;
}

.error { 
  margin-top: 1rem; 
  color: #dc2626; 
  font-weight: bold; 
  font-size: 0.9rem; 
}
</style>