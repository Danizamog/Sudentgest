<template>
  <section class="auth">
    <div class="card">
      <h1>Iniciar sesión</h1>

      <form @submit.prevent="handleSignIn" class="form">
        <input id="email" v-model="email" type="email" placeholder="Correo institucional" required />
        <input id="password" v-model="password" type="password" placeholder="Contraseña" required />
        <button type="submit" class="btn-primary" :disabled="loading">
          <span v-if="loading">Ingresando...</span>
          <span v-else>Entrar</span>
        </button>
      </form>

      <div class="divider"><span>o</span></div>

      <button type="button" class="btn-google" @click="handleGoogleSignIn">
        <img src="https://www.svgrepo.com/show/355037/google.svg" alt="Google" />
        Iniciar con Google
      </button>

      <p v-if="error" class="error">{{ error }}</p>

      <p class="hint">
        ¿No tienes cuenta?
        <router-link to="/signup">Regístrate</router-link>
      </p>
    </div>
  </section>
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

// ✅ CORREGIDO: URL dinámica para Docker y desarrollo
const getBackendUrl = () => {
  if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
    return 'http://localhost:5002'  // Desarrollo local
  }
  return '/api/auth'  // Docker (proxy Nginx)
}

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
    const backendUrl = getBackendUrl()
    const res = await fetch(`${backendUrl}/api/auth/sync-user`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ tenant })
    })

    if (!res.ok) {
      const backendError = await res.text()
      console.warn('Sync-user falló:', backendError)
      // ✅ CORREGIDO: No bloqueamos el flujo si sync falla
    }
  } catch (e) {
    console.warn('No se pudo sincronizar con backend:', e)
    // ✅ CORREGIDO: Continuamos aunque falle sync
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
      options: { 
        redirectTo: `${window.location.origin}/auth/callback`
      }
    })
    if (err) throw err
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
.auth {
  min-height: calc(100vh - 140px);
  display: grid;
  place-items: center;
  padding: 2rem 1rem;
}
.card {
  width: 100%;
  max-width: 420px;
  background: #fff;
  border-radius: 16px;
  padding: 1.5rem;
  box-shadow: 0 10px 30px rgba(0, 0, 0, .06);
  animation: rise .4s ease;
}
.card h1 {
  margin-bottom: 1rem;
  text-align: center;
  color: #2a4dd0;
}
.form {
  display: flex;
  flex-direction: column;
  gap: .75rem;
}
input {
  border: 1px solid #e5e7eb;
  border-radius: 10px;
  padding: .8rem;
  font: inherit;
  transition: border-color .15s, box-shadow .2s;
}
input:focus {
  outline: none;
  border-color: #2a4dd0;
  box-shadow: 0 0 0 4px rgba(42, 77, 208, .12);
}
.btn-primary {
  background: #2a4dd0;
  color: #fff;
  border: none;
  padding: .7rem 1rem;
  border-radius: 10px;
  cursor: pointer;
  font-weight: 600;
  transition: transform .15s, box-shadow .2s;
}
.btn-primary:disabled {
  background-color: #a5b4fc;
  cursor: not-allowed;
}
.btn-primary:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 6px 20px rgba(42, 77, 208, .2);
}
.divider {
  display: flex;
  align-items: center;
  margin: 1.5rem 0;
  color: #6b7280;
  font-size: 0.9rem;
}
.divider::before,
.divider::after {
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
.error {
  margin-top: 1rem;
  color: #dc2626;
  font-weight: bold;
  font-size: 0.9rem;
}
.hint {
  margin-top: .75rem;
  text-align: center;
  color: #666;
}
@keyframes rise {
  from {
    opacity: 0;
    transform: translateY(8px);
  }
  to {
    opacity: 1;
    transform: none;
  }
}
</style>