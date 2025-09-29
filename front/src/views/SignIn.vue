<template>
  <div class="signin-container">
    <h2 class="title">Iniciar sesión</h2>

    <form @submit.prevent="handleSignIn" class="signin-form">
      <div class="input-group">
        <label for="email">Correo</label>
        <input id="email" v-model="email" type="email" placeholder="correo@ejemplo.com" required />
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

      <button type="button" class="btn-google" @click="handleGoogleSignIn">
        <img src="https://www.svgrepo.com/show/355037/google.svg" alt="Google" />
        Iniciar con Google
      </button>

      <p v-if="error" class="error">{{ error }}</p>
    </form>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { supabase } from '../supabase'

const email = ref('')
const password = ref('')
const error = ref(null)
const loading = ref(false)
const router = useRouter()
const BACKEND_URL = import.meta.env.VITE_BACKEND_URL || 'http://auth:5000'

// Obtener tenant según email
function getTenantFromEmail(email) {
  if (email.endsWith('@ucb.edu.bo')) return 'ucb.edu.bo'
  if (email.endsWith('@upb.edu.bo')) return 'upb.edu.bo'
  if (email.endsWith('@gmail.com')) return 'gmail.com'
  return null
}

// Procesar sesión y sincronizar usuario
async function processSession(session) {
  const token = session.access_token
  localStorage.setItem('token', token)

  const userEmail = session.user?.email
  const tenant = getTenantFromEmail(userEmail)
  if (!tenant) {
    error.value = 'Dominio de correo no permitido.'
    return
  }

  try {
    const res = await fetch(`${BACKEND_URL}/api/auth/sync-user`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ tenant })
    })

    if (!res.ok) {
      const backendError = await res.json()
      console.warn('Sync-user falló:', backendError)
    }
  } catch (e) {
    console.warn('No se pudo sincronizar con backend:', e)
  }

  router.push('/home')
}

// Login con email/password
async function handleSignIn() {
  error.value = null
  loading.value = true
  try {
    const { data, error: err } = await supabase.auth.signInWithPassword({
      email: email.value,
      password: password.value
    })
    if (err) throw err
    if (!data.session?.access_token) throw new Error('No se recibió token de Supabase.')
    await processSession(data.session)
  } catch (e) {
    error.value = e.message || 'Error inesperado. Intenta nuevamente.'
  } finally {
    loading.value = false
  }
}

// Login con Google OAuth
async function handleGoogleSignIn() {
  try {
    const { error: err } = await supabase.auth.signInWithOAuth({
      provider: 'google',
      options: { redirectTo: window.location.origin }
    })
    if (err) throw err
    // El redirect se encargará de disparar onAuthStateChange
  } catch (e) {
    error.value = 'Error al iniciar sesión con Google.'
    console.error(e)
  }
}

// Escuchar cambios de sesión (incluye redirect OAuth)
onMounted(async () => {
  const { data: { session } } = await supabase.auth.getSession()
  if (session?.access_token) {
    await processSession(session)
  }

  supabase.auth.onAuthStateChange(async (_event, session) => {
    if (session?.access_token) {
      await processSession(session)
    }
  })
})
</script>


<style scoped>
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
.title { font-size: 1.75rem; font-weight: 700; margin-bottom: 2rem; color: #1f2937; }
.signin-form .input-group { margin-bottom: 1.5rem; text-align: left; }
.signin-form label { display: block; margin-bottom: 0.4rem; font-weight: 600; color: #374151; }
.signin-form input { width: 100%; padding: 0.75rem; border-radius: 0.75rem; border: 1px solid #d1d5db; background: #f9fafb; transition: border 0.2s ease; }
.signin-form input:focus { border-color: #4f46e5; outline: none; background: #fff; }
.btn-primary { width: 100%; padding: 0.85rem; border: none; border-radius: 0.75rem; background-color: #4f46e5; color: white; font-weight: 600; cursor: pointer; transition: background 0.3s ease; margin-top: 0.5rem; }
.btn-primary:disabled { background-color: #a5b4fc; cursor: not-allowed; }
.btn-primary:hover:not(:disabled) { background-color: #3730a3; }
.divider { display: flex; align-items: center; margin: 1.5rem 0; color: #6b7280; font-size: 0.9rem; }
.divider::before, .divider::after { content: ''; flex: 1; height: 1px; background: #e5e7eb; }
.divider span { margin: 0 0.75rem; }
.btn-google { width: 100%; padding: 0.85rem; border: 1px solid #d1d5db; border-radius: 0.75rem; background-color: #fff; font-weight: 600; color: #374151; display: flex; align-items: center; justify-content: center; gap: 0.75rem; cursor: pointer; transition: background 0.2s ease, border 0.2s ease; }
.btn-google:hover { background-color: #f9fafb; border-color: #9ca3af; }
.btn-google img { width: 20px; height: 20px; }
.error { margin-top: 1rem; color: #dc2626; font-weight: bold; font-size: 0.9rem; }
</style>
