<template>
  <transition name="fade">
    <section class="wrap">
      <h1>Contacto</h1>
      <p class="lead">¿Necesitas soporte o una demo?</p>
      
      <!-- Mensaje de éxito -->
      <div v-if="showSuccess" class="success-message">
        <h3>¡Mensaje enviado exitosamente!</h3>
        <p>Gracias por contactarnos. Te responderemos pronto.</p>
      </div>
      
      <!-- Mensaje de error -->
      <div v-if="showError" class="error-message">
        <h3>Error al enviar mensaje</h3>
        <p>{{ errorMessage }}</p>
      </div>
      
      <!-- Formulario -->
      <form v-if="!showSuccess" class="form" @submit.prevent="sendMessage">
        <input 
          v-model="form.nombre" 
          type="text" 
          placeholder="Nombre" 
          required 
          :disabled="loading"
          :readonly="!!userProfile?.nombre"
          :class="{ 'readonly-field': !!userProfile?.nombre }"
        />
        <input 
          v-model="form.email" 
          type="email" 
          placeholder="Correo" 
          required 
          :disabled="loading"
          readonly
          class="readonly-field"
        />
        <input 
          v-model="form.asunto" 
          type="text" 
          placeholder="Asunto (opcional)" 
          :disabled="loading"
        />
        <input 
          v-model="form.telefono" 
          type="tel" 
          placeholder="Teléfono (opcional)" 
          :disabled="loading"
        />
        <textarea 
          v-model="form.mensaje" 
          rows="4" 
          placeholder="Mensaje" 
          required 
          :disabled="loading"
        ></textarea>
        <button type="submit" :disabled="loading || !form.email">
          {{ loading ? 'Enviando...' : 'Enviar' }}
        </button>
      </form>
      
      <!-- Información adicional -->
      <div class="info-section">
        <p class="hint">También puedes escribirnos a soporte@studentgest.com</p>
      </div>
      
      <!-- Botón para enviar otro mensaje -->
      <button v-if="showSuccess" @click="resetForm" class="reset-btn">
        Enviar otro mensaje
      </button>
    </section>
  </transition>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { supabase } from '../supabase'

// Estado reactivo
const loading = ref(false)
const showSuccess = ref(false)
const showError = ref(false)
const errorMessage = ref('')
const userProfile = ref(null)

// Formulario reactivo
const form = reactive({
  nombre: '',
  email: '',
  asunto: '',
  telefono: '',
  mensaje: ''
})

// URL del servicio de contacto
const CONTACT_API_URL = 'http://localhost:5013/api/contact'

// Obtener perfil del usuario
async function getUserProfile() {
  try {
    const response = await fetch('/auth/user-profile', {
      method: 'GET',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include'
    })

    if (response.ok) {
      const profile = await response.json()
      userProfile.value = profile
      
      // Auto-populate form with user data
      if (profile.email) {
        form.email = profile.email
      }
      if (profile.nombre) {
        form.nombre = profile.nombre + (profile.apellido ? ` ${profile.apellido}` : '')
      }
      
      return profile
    } else {
      // Fallback: try to get user from Supabase session
      const { data: { session } } = await supabase.auth.getSession()
      if (session?.user?.email) {
        form.email = session.user.email
        userProfile.value = { email: session.user.email }
      }
    }
  } catch (error) {
    console.error('Error obteniendo perfil:', error)
    // Try Supabase as fallback
    try {
      const { data: { session } } = await supabase.auth.getSession()
      if (session?.user?.email) {
        form.email = session.user.email
        userProfile.value = { email: session.user.email }
      }
    } catch (e) {
      console.error('Error obteniendo sesión de Supabase:', e)
    }
  }
}

// Función para enviar mensaje
const sendMessage = async () => {
  loading.value = true
  showError.value = false
  errorMessage.value = ''
  
  try {
    const payload = {
      nombre: form.nombre.trim(),
      email: form.email.trim(),
      mensaje: form.mensaje.trim(),
      asunto: form.asunto?.trim() || null,
      telefono: form.telefono?.trim() || null
    }
    
    const response = await fetch(`${CONTACT_API_URL}/send`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(payload)
    })
    
    let data
    try {
      data = await response.json()
    } catch (parseError) {
      throw new Error('Error de comunicación con el servidor')
    }
    
    if (response.ok && data.success) {
      showSuccess.value = true
    } else {
      // Simple error message extraction
      let errorMsg = 'Error desconocido'
      
      if (data && data.message) {
        errorMsg = data.message
      } else if (data && data.error) {
        errorMsg = data.error
      } else if (data && data.detail) {
        errorMsg = data.detail
      } else if (data && data.details) {
        if (Array.isArray(data.details)) {
          errorMsg = `Error de validación: ${data.details.map(d => d.msg || d).join(', ')}`
        } else {
          errorMsg = `Error de validación: ${data.details}`
        }
      }
      
      throw new Error(errorMsg)
    }
  } catch (error) {
    showError.value = true
    
    // Handle different types of errors
    if (typeof error.message === 'string') {
      errorMessage.value = error.message
    } else if (error.message && typeof error.message === 'object') {
      errorMessage.value = JSON.stringify(error.message)
    } else {
      errorMessage.value = 'Error de conexión con el servidor'
    }
  } finally {
    loading.value = false
  }
}

// Función para resetear el formulario
const resetForm = () => {
  // Keep email and name from user profile
  const savedEmail = userProfile.value?.email || ''
  const savedName = userProfile.value ? 
    (userProfile.value.nombre + (userProfile.value.apellido ? ` ${userProfile.value.apellido}` : '')) : ''
  
  form.nombre = savedName
  form.email = savedEmail
  form.asunto = ''
  form.telefono = ''
  form.mensaje = ''
  showSuccess.value = false
  showError.value = false
  errorMessage.value = ''
}

// Initialize when component mounts
onMounted(() => {
  getUserProfile()
})
</script>

<style scoped>
.wrap {
  max-width: 700px;
  margin: 2rem auto;
  padding: 0 1rem;
  animation: slideIn 0.7s;
}

.form {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  background: #fff;
  padding: 1rem;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
  margin-bottom: 1rem;
}

input,
textarea {
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 0.75rem;
  font: inherit;
  transition:
    border-color 0.15s,
    box-shadow 0.2s;
}

input:focus,
textarea:focus {
  outline: none;
  border-color: #2a4dd0;
  box-shadow: 0 0 0 4px rgba(42, 77, 208, 0.12);
}

input:disabled,
textarea:disabled {
  background-color: #f5f5f5;
  cursor: not-allowed;
  opacity: 0.7;
}

.readonly-field {
  background-color: #f8fafc !important;
  border-color: #e2e8f0 !important;
  color: #64748b !important;
  cursor: default !important;
}

.readonly-field:focus {
  border-color: #e2e8f0 !important;
  box-shadow: none !important;
}

.field-info {
  font-size: 0.75rem;
  color: #059669;
  margin-top: -0.5rem;
  margin-bottom: 0.25rem;
  font-style: italic;
  display: block;
}

button {
  align-self: flex-start;
  background: #2a4dd0;
  color: #fff;
  border: none;
  padding: 0.6rem 1rem;
  border-radius: 8px;
  cursor: pointer;
  transition:
    transform 0.15s,
    box-shadow 0.2s,
    opacity 0.2s;
}

button:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 6px 20px rgba(42, 77, 208, 0.2);
}

button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}

.reset-btn {
  background: #10b981;
  margin-top: 1rem;
}

.reset-btn:hover {
  box-shadow: 0 6px 20px rgba(16, 185, 129, 0.2);
}

.success-message {
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
  border-radius: 12px;
  padding: 1.5rem;
  margin-bottom: 1rem;
  text-align: center;
}

.success-message h3 {
  color: #166534;
  margin: 0 0 0.5rem 0;
  font-size: 1.25rem;
}

.success-message p {
  color: #15803d;
  margin: 0.25rem 0;
}

.error-message {
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 12px;
  padding: 1.5rem;
  margin-bottom: 1rem;
  text-align: center;
}

.error-message h3 {
  color: #dc2626;
  margin: 0 0 0.5rem 0;
  font-size: 1.25rem;
}

.error-message p {
  color: #ef4444;
  margin: 0.25rem 0;
}

.info-section {
  margin-top: 1.5rem;
}

.hint {
  color: #666;
  text-align: center;
  margin-bottom: 1rem;
}

.tenant-info-box {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  padding: 1rem;
}

.tenant-info-box h4 {
  color: #334155;
  margin: 0 0 0.5rem 0;
  font-size: 1rem;
}

.tenant-info-box ul {
  list-style: none;
  padding: 0;
  margin: 0;
}

.tenant-info-box li {
  color: #64748b;
  padding: 0.25rem 0;
  font-size: 0.875rem;
}

.tenant-info-box strong {
  color: #2a4dd0;
  font-family: monospace;
}

.lead {
  color: #555;
  margin: 0.5rem 0 1rem;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.4s;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

@keyframes slideIn {
  from {
    transform: translateY(20px);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

/* Responsive design */
@media (max-width: 768px) {
  .wrap {
    margin: 1rem auto;
    padding: 0 0.5rem;
  }
  
  .form,
  .success-message,
  .error-message,
  .tenant-info-box {
    padding: 0.75rem;
  }
}
</style>