<template>
  <div id="app">
    <Navbar />
    <router-view />
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import { supabase } from './supabase'
import Navbar from './components/Navbar.vue'

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
    await handleSession(session)
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
  font-family: Avenir, Helvetica, Arial, sans-serif;
  text-align: center;
  color: #2c3e50;
}
</style>