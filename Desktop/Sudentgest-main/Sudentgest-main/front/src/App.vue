<template>
  <div id="app">
    <Navbar />
    <router-view />
  </div>
</template>

<script setup>
import Navbar from './components/Navbar.vue'
import { onMounted } from 'vue'
import { supabase } from './supabase'

// 🔹 VERIFICAR SESIÓN AL INICIAR LA APP
onMounted(async () => {
  console.log('🔹 App - Verificando sesión inicial...')
  const { data: { session } } = await supabase.auth.getSession()
  
  if (session) {
    console.log('🔹 App - Sesión encontrada:', session.user.email)
    localStorage.setItem('token', session.access_token)
  } else {
    console.log('🔹 App - No hay sesión activa')
    localStorage.removeItem('token')
  }
})
</script>