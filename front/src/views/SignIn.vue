<template>
  <div class="signin-container">
    <h2>Iniciar sesión</h2>
    <form @submit.prevent="handleSignIn" class="signin-form">
      <div class="input-group">
        <label for="email">Correo</label>
        <input id="email" v-model="email" type="email" placeholder="correo@ejemplo.com" required />
      </div>

      <div class="input-group">
        <label for="password">Contraseña</label>
        <input id="password" v-model="password" type="password" placeholder="••••••••" required />
      </div>

      <button type="submit" :disabled="loading">
        <span v-if="loading">Ingresando...</span>
        <span v-else>Entrar</span>
      </button>

      <p v-if="error" class="error">{{ error }}</p>
    </form>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { supabase } from '../supabase'

const email = ref('')
const password = ref('')
const error = ref(null)
const loading = ref(false)
const router = useRouter()

async function handleSignIn() {
  error.value = null
  loading.value = true

  try {
    const { data, error: err } = await supabase.auth.signInWithPassword({
      email: email.value,
      password: password.value
    })

    if (err) {
      error.value = err.message
    } else if (data.session?.access_token) {
      localStorage.setItem('token', data.session.access_token)
      router.push('/') // Redirige solo si login exitoso
    }
  } catch (e) {
    error.value = 'Error inesperado. Intenta nuevamente.'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.signin-container {
  max-width: 400px;
  margin: 5rem auto;
  padding: 2rem;
  border-radius: 1rem;
  box-shadow: 0 4px 12px rgba(0,0,0,0.1);
  background-color: #fff;
  font-family: Arial, sans-serif;
}

.signin-form .input-group {
  margin-bottom: 1rem;
}

.signin-form label {
  display: block;
  margin-bottom: 0.25rem;
  font-weight: bold;
}

.signin-form input {
  width: 100%;
  padding: 0.5rem;
  border-radius: 0.5rem;
  border: 1px solid #ccc;
}

button {
  width: 100%;
  padding: 0.75rem;
  border: none;
  border-radius: 0.5rem;
  background-color: #4f46e5;
  color: white;
  font-weight: bold;
  cursor: pointer;
  transition: background 0.3s ease;
}

button:disabled {
  background-color: #a5b4fc;
  cursor: not-allowed;
}

button:hover:not(:disabled) {
  background-color: #3730a3;
}

.error {
  margin-top: 1rem;
  color: #dc2626;
  font-weight: bold;
}
</style>
