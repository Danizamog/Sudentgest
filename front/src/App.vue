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

onMounted(async () => {
  const { data: { session } } = await supabase.auth.getSession()

  if (session) {
    localStorage.setItem('token', session.access_token)
    localStorage.setItem('user_id', session.user.id)
  } else {
    localStorage.removeItem('token')
    localStorage.removeItem('user_id')
  }
})

// Mantener sincronización de sesión
supabase.auth.onAuthStateChange((event, session) => {
  if (session) {
    localStorage.setItem('token', session.access_token)
    localStorage.setItem('user_id', session.user.id)
  } else {
    localStorage.removeItem('token')
    localStorage.removeItem('user_id')
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