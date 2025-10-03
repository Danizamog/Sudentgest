<template>
  <nav class="navbar">
    <router-link to="/home" class="nav-link">Inicio</router-link>
    <router-link to="/foro" class="nav-link">Foro</router-link>
    <router-link to="/features" class="nav-link">Características</router-link>
    <router-link to="/pricing" class="nav-link">Precios</router-link>
    <router-link to="/info" class="nav-link">Información</router-link>
    <router-link to="/contact" class="nav-link">Contacto</router-link>
    <router-link to="/nosotros" class="nav-link">Nosotros</router-link>
    <button @click="handleLogout" class="logout-btn">Cerrar sesión</button>
  </nav>
</template>

<script setup>
import { useRouter } from 'vue-router'
import { supabase } from '../supabase'
import { ref } from 'vue'

const router = useRouter()
const loading = ref(false)
const isAuthenticated = ref(true)
const userProfile = ref(null)
const mobileMenuOpen = ref(false)

async function handleLogout() {
  try {
    loading.value = true
    console.log('🔹 Navbar - Iniciando cierre de sesión...')

    const { error } = await supabase.auth.signOut()
    if (error) {
      console.error('Error en supabase signOut:', error)
      throw error
    }

    localStorage.removeItem('token')
    isAuthenticated.value = false
    userProfile.value = null
    mobileMenuOpen.value = false
    document.body.style.overflow = 'auto'

    console.log('🔹 Navbar - Sesión cerrada exitosamente')
    await router.push('/signin')
  } catch (error) {
    console.error('Error completo al cerrar sesión:', error)
    localStorage.removeItem('token')
    isAuthenticated.value = false
    userProfile.value = null
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.navbar {
  display: flex;
  gap: 12px;
  align-items: center;
  padding: 12px;
  background: #f5f5f5;
}
.nav-link {
  text-decoration: none;
  color: #333;
  font-weight: 500;
  padding: 6px 10px;
  border-radius: 4px;
  transition: background 0.2s;
}
.nav-link:hover {
  background: #e0e0e0;
}
.logout-btn {
  padding: 6px 14px;
  background: #e53935;
  color: #fff;
  border: none;
  border-radius: 4px;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.2s;
}
.logout-btn:hover {
  background: #b71c1c;
}
</style>