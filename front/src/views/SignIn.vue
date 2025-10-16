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

      <button type="button" class="btn-google" @click="handleGoogleSignIn" :disabled="loading">
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

function getTenantFromEmail(email) {
  if (email.endsWith('@ucb.edu.bo')) return 'ucb.edu.bo'
  if (email.endsWith('@upb.edu.bo')) return 'upb.edu.bo'
  if (email.endsWith('@gmail.com')) return 'gmail.com'
  return null
}

// Login con email/password
async function handleSignIn() {
  loading.value = true
  error.value = null
  try {
    const { data, error: signError } = await supabase.auth.signInWithPassword({
      email: email.value,
      password: password.value
    })

    if (signError) throw signError
    const session = data.session
    if (!session) throw new Error('No session received')

    // ✅ Ruta relativa para establecer cookie
    const cookieResponse = await fetch('/auth/session-cookie', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${session.access_token}`,
        'Content-Type': 'application/json'
      },
      credentials: 'include'
    })

    if (!cookieResponse.ok) {
      throw new Error('Error estableciendo sesión')
    }

    // ✅ Ruta relativa para sincronizar usuario
    const userEmail = session.user?.email
    if (userEmail) {
      const tenant = getTenantFromEmail(userEmail)
      if (tenant) {
        try {
          await fetch('/auth/sync-user', {
            method: 'POST',
            headers: {
              'Authorization': `Bearer ${session.access_token}`,
              'Content-Type': 'application/json'
            },
            body: JSON.stringify({ tenant })
          })
        } catch (e) {
          console.warn('No se pudo sincronizar con backend:', e)
        }
      }
    }

    // Redirigir al home
    router.push('/home')
  } catch (e) {
    error.value = e.message || String(e)
  } finally {
    loading.value = false
  }
}

// Login con Google
async function handleGoogleSignIn() {
  try {
    error.value = null
    loading.value = true

    const { error: err } = await supabase.auth.signInWithOAuth({
      provider: 'google',
      options: { 
        redirectTo: `${window.location.origin}/auth/callback`
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
  // Verificar si ya está autenticado
  const { data: { session } } = await supabase.auth.getSession()
  if (session?.access_token) {
    router.push('/home')
  }
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
  font-size: 1rem;
}

.signin-form input:focus { 
  border-color: #4f46e5; 
  outline: none; 
  background: #fff; 
  box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.1);
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
  font-size: 1rem;
}

.btn-primary:disabled { 
  background-color: #a5b4fc; 
  cursor: not-allowed; 
  opacity: 0.7;
}

.btn-primary:hover:not(:disabled) { 
  background-color: #3730a3; 
  transform: translateY(-1px);
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
  background: white;
  padding: 0 10px;
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
  transition: all 0.2s ease; 
  font-size: 1rem;
}

.btn-google:hover { 
  background-color: #f9fafb; 
  border-color: #9ca3af; 
  transform: translateY(-1px);
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.btn-google:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
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
  padding: 0.75rem;
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 0.5rem;
}

/* Responsive */
@media (max-width: 480px) {
  .signin-container {
    margin: 2rem auto;
    padding: 2rem 1.5rem;
  }
  
  .title {
    font-size: 1.5rem;
  }
}
</style>