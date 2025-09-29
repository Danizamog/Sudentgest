<template>
  <div class="callback-container">
    <div class="loading-spinner"></div>
    <p>Procesando autenticación...</p>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { supabase } from '../supabase'

const router = useRouter()

// 🔹 FUNCIÓN: Obtener tenant del email
function getTenantFromEmail(email) {
  if (email.endsWith('@ucb.edu.bo')) return 'ucb.edu.bo'
  if (email.endsWith('@upb.edu.bo')) return 'upb.edu.bo'
  if (email.endsWith('@gmail.com')) return 'gmail.com'
  return null
}

// 🔹 FUNCIÓN: Hacer sync del usuario
async function syncUser(session) {
  try {
    const userEmail = session.user?.email
    if (!userEmail) {
      console.error('❌ No se pudo obtener email del usuario')
      return false
    }

    const tenant = getTenantFromEmail(userEmail)
    if (!tenant) {
      console.error('❌ Dominio de email no permitido:', userEmail)
      return false
    }

    const backendUrl = window.location.hostname === 'localhost' ? 'http://localhost:5002' : '/api/auth'
    
    console.log('🔹 Llamando a sync-user...', { email: userEmail, tenant })
    
    const response = await fetch(`${backendUrl}/api/auth/sync-user`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${session.access_token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ tenant })
    })

    if (response.ok) {
      const result = await response.json()
      console.log('✅ Sync-user exitoso:', result)
      return true
    } else {
      const errorText = await response.text()
      console.error('❌ Error en sync-user:', errorText)
      return false
    }
  } catch (error) {
    console.error('❌ Error en sync:', error)
    return false
  }
}

onMounted(async () => {
  try {
    console.log('🔄 Procesando callback de OAuth...')
    
    // Obtener la sesión después del redirect de OAuth
    const { data: { session }, error } = await supabase.auth.getSession()
    
    if (error) {
      console.error('❌ Error en callback:', error)
      router.push('/signin?error=auth_failed')
      return
    }
    
    if (session) {
      console.log('✅ Sesión obtenida correctamente en callback')
      
      // Guardar el token
      localStorage.setItem('token', session.access_token)
      
      // 🔹 HACER SYNC DEL USUARIO ANTES DE REDIRIGIR
      console.log('🔄 Sincronizando usuario...')
      await syncUser(session)
      
      // Redirigir al home
      router.push('/home')
    } else {
      console.warn('⚠️ No se encontró sesión en callback')
      router.push('/signin?error=no_session')
    }
  } catch (error) {
    console.error('❌ Error inesperado en callback:', error)
    router.push('/signin?error=callback_failed')
  }
})
</script>

<style scoped>
.callback-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  font-family: 'Inter', sans-serif;
}

.loading-spinner {
  width: 50px;
  height: 50px;
  border: 4px solid rgba(255, 255, 255, 0.3);
  border-top: 4px solid white;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin-bottom: 1.5rem;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

p {
  font-size: 1.1rem;
  font-weight: 500;
}
</style>