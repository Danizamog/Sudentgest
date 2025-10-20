<template>
  <div id="app">
    <!-- Landing Layout for public pages -->
    <LandingLayout v-if="currentLayout === 'landing'">
      <router-view />
    </LandingLayout>

    <!-- App Layout for authenticated pages with sidebar -->
    <AppLayout v-else-if="currentLayout === 'app'">
      <router-view />
    </AppLayout>

    <!-- No layout (for signin, etc.) -->
    <router-view v-else />
  </div>
</template>

<script setup>
import { computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { supabase } from './supabase'
import LandingLayout from './layouts/LandingLayout.vue'
import AppLayout from './layouts/AppLayout.vue'

const route = useRoute()

// Determine which layout to use based on route meta
const currentLayout = computed(() => {
  return route.meta.layout || 'none'
})

// Función para establecer cookie de sesión cuando hay autenticación
const handleSession = async (session) => {
  if (session) {
    const token = session.access_token
    
    localStorage.setItem('token', token)
    localStorage.setItem('user_id', session.user.id)

    // Solicitar al backend que establezca la cookie HttpOnly
    try {
      await fetch('/auth/session-cookie', {
        method: 'POST',
        headers: { 
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        credentials: 'include'
      })
    } catch (e) {
      console.warn('No se pudo setear cookie HttpOnly:', e)
    }
  } else {
    localStorage.removeItem('token')
    localStorage.removeItem('user_id')
  }
}

onMounted(async () => {
  try {
    const { data: { session } } = await supabase.auth.getSession()
    // Only call handleSession if there's actually a session
    if (session) {
      await handleSession(session)
    }
  } catch (error) {
    console.error('Error al obtener sesión:', error)
  }
})

supabase.auth.onAuthStateChange(async (event, session) => {
  try {
    await handleSession(session)
  } catch (error) {
    console.error('Error en cambio de estado de autenticación:', error)
  }
})
</script>

<style>
#app {
  min-height: 100vh;
}
</style>