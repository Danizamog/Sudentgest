<template>
  <nav v-if="showNavbar" class="navbar">
    <div class="nav-container">
      <div class="nav-logo">
        <router-link to="/home">StudentGest</router-link>
      </div>
      <ul class="nav-links">
        <li><router-link to="/home">Inicio</router-link></li>
        <li><router-link to="/foro">Foro</router-link></li>
        <li><router-link to="/features">Características</router-link></li>
        <li><router-link to="/pricing">Precios</router-link></li>
        <li><router-link to="/info">Información</router-link></li>
        <li><router-link to="/contact">Contacto</router-link></li>
        <li><router-link to="/nosotros">Nosotros</router-link></li>
        <li><router-link to="/base">Base de Datos</router-link></li>
        <li><button @click="handleLogout" class="logout-btn">Cerrar sesión</button></li>
      </ul>
    </div>
  </nav>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { supabase } from '../supabase'

const router = useRouter()
const route = useRoute()
const loading = ref(false)
const isAuthenticated = ref(false)

// 🔹 COMPUTED: Mostrar navbar solo si está autenticado Y no está en signin
const showNavbar = computed(() => {
  const hideOnRoutes = ['/signin', '/', '/auth/callback']
  return isAuthenticated.value && !hideOnRoutes.includes(route.path)
})

// 🔹 FUNCIÓN: Verificar autenticación
function checkAuth() {
  const token = localStorage.getItem('token')
  isAuthenticated.value = !!token
  console.log('🔹 Navbar - Autenticado:', isAuthenticated.value, 'Ruta:', route.path)
}

// 🔹 ESCUCHAR CAMBIOS
function setupListeners() {
  // Escuchar cambios de autenticación de Supabase
  const { data: { subscription } } = supabase.auth.onAuthStateChange((event, session) => {
    console.log('🔹 Navbar - Auth state change:', event)
    checkAuth()
  })

  // Escuchar cambios de ruta
  const removeRouteListener = router.afterEach(() => {
    checkAuth()
  })

  return () => {
    subscription?.unsubscribe()
    removeRouteListener()
  }
}

async function handleLogout() {
  try {
    loading.value = true
    console.log('🔹 Navbar - Cerrando sesión...')
    await supabase.auth.signOut()
    localStorage.removeItem('token')
    isAuthenticated.value = false
    router.push('/signin')
  } catch (error) {
    console.error('Error al cerrar sesión:', error)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  console.log('🔹 Navbar - Montado')
  checkAuth()
  const cleanup = setupListeners()
  
  onUnmounted(() => {
    cleanup()
  })
})
</script>

<style scoped>
.navbar {
  background: linear-gradient(to right, #1e3c72, #2a5298);
  padding: 0.5rem 1rem;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  position: sticky;
  top: 0;
  z-index: 1000;
}

.nav-container {
  display: flex;
  justify-content: space-between;
  align-items: center;
  max-width: 1200px;
  margin: 0 auto;
}

.nav-logo a {
  color: white;
  font-size: 1.5rem;
  font-weight: bold;
  text-decoration: none;
}

.nav-links {
  list-style: none;
  display: flex;
  gap: 1rem;
  margin: 0;
  padding: 0;
  align-items: center;
}

.nav-links li {
  display: flex;
  align-items: center;
}

.router-link-active,
.router-link-exact-active {
  font-weight: bold;
  text-decoration: underline;
  background-color: rgba(255, 255, 255, 0.3) !important;
}

.nav-links a {
  color: white;
  background-color: transparent;
  border: none;
  padding: 0.5rem 1rem;
  font-size: 0.9rem;
  cursor: pointer;
  transition: background-color 0.3s ease;
  border-radius: 5px;
  text-decoration: none;
  display: block;
  white-space: nowrap;
}

.nav-links a:hover {
  background-color: rgba(255, 255, 255, 0.2);
}

.logout-btn {
  color: white;
  background-color: rgba(239, 68, 68, 0.8);
  border: none;
  padding: 0.5rem 1rem;
  font-size: 0.9rem;
  cursor: pointer;
  transition: background-color 0.3s ease;
  border-radius: 5px;
  white-space: nowrap;
}

.logout-btn:hover {
  background-color: rgba(220, 38, 38, 0.9);
}

/* Responsive */
@media (max-width: 768px) {
  .nav-container {
    flex-direction: column;
    gap: 1rem;
  }
  
  .nav-links {
    flex-wrap: wrap;
    justify-content: center;
  }
  
  .nav-links a,
  .logout-btn {
    font-size: 0.8rem;
    padding: 0.4rem 0.8rem;
  }
}
</style>