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

onMounted(async () => {
  const { data: { session } } = await supabase.auth.getSession()
  
  if (session) {
    localStorage.setItem('token', session.access_token)
  } else {
    localStorage.removeItem('token')
  }
})
</script>
<style scoped>
#app {
  font-family: Avenir, Helvetica, Arial, sans-serif;
  margin:0;
}
</style>