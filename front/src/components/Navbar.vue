<template>
  <div v-if="showNavbar">
    <!-- HEADER (full width) -->
    <header class="site-header">
      <div class="container">
        <h1 class="site-title">StudentGest</h1>
      </div>
    </header>

    <!-- NAV (full width) -->
    <nav class="site-nav">
      <div class="container nav-flex">
        <div class="nav-left">
          <!-- espacio para logo o link futuro -->
        </div>

        <div class="nav-center">
          <router-link to="/home" class="nav-link">Inicio</router-link>
          <router-link to="/foro" class="nav-link">Foro</router-link>
          <router-link to="/features" class="nav-link">Características</router-link>
          <router-link to="/pricing" class="nav-link">Precios</router-link>
          <router-link to="/info" class="nav-link">Información</router-link>
          <router-link to="/contact" class="nav-link">Contacto</router-link>
          <router-link to="/nosotros" class="nav-link">Nosotros</router-link>
          <router-link to="/base" class="nav-link">Base de Datos</router-link>
        </div>

        <div class="nav-right">
          <button @click="handleLogout" class="logout-btn">Cerrar sesión</button>
        </div>
      </div>
    </nav>
  </div>
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
/* contenedor central que mantiene el contenido alineado y con max-width */
.container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 20px;
}

/* HEADER */
.site-header {
  width: 100%;
  background: #111318; /* gris muy oscuro */
  color: #ffffff;
  padding: 18px 0;
  box-shadow: 0 1px 0 rgba(0,0,0,0.15);
}
.site-title {
  margin: 0;
  font-size: 22px;
  font-weight: 700;
  letter-spacing: 0.3px;
  color: #ffffff;
  text-align: left;
}

/* NAV */
.site-nav {
  width: 100%;
  background: #f5f6f8; /* gris claro para navbar */
  border-bottom: 1px solid #e6e7ea;
}
.nav-flex {
  display: flex;
  align-items: center;
  gap: 16px;
  height: 64px;
}

/* Tres columnas equilibradas: left-center-right */
.nav-left, .nav-center, .nav-right {
  flex: 1;
}
.nav-left { text-align: left; }
.nav-center { text-align: center; }
.nav-right { text-align: right; }

/* enlaces centrales */
.nav-link {
  margin: 0 12px;
  color: #374151; /* gris sobrio */
  text-decoration: none;
  font-weight: 600;
  font-size: 15px;
  padding: 8px 10px;
  display: inline-block;
  cursor: pointer;
  transition: all 0.2s ease;
}
.nav-link:hover {
  color: #0f172a;
  text-decoration: underline;
}

/* 🔹 ESTILOS PARA RUTA ACTIVA */
.router-link-active,
.router-link-exact-active {
  color: #1e40af !important;
  font-weight: 700;
  background-color: rgba(30, 64, 175, 0.1);
  border-radius: 6px;
}

/* botón de logout */
.logout-btn {
  background: #dc2626; /* rojo para logout */
  color: #ffffff;
  padding: 8px 14px;
  border-radius: 8px;
  text-decoration: none;
  font-weight: 700;
  display: inline-block;
  cursor: pointer;
  border: none;
  box-shadow: 0 2px 6px rgba(220, 38, 38, 0.2);
  transition: background-color 0.2s ease;
}
.logout-btn:hover {
  background: #b91c1c;
}

/* Responsive */
@media (max-width: 768px) {
  .nav-flex {
    flex-direction: column;
    height: auto;
    padding: 10px 0;
    gap: 8px;
  }
  .site-title {
    text-align: center;
    font-size: 20px;
  }
  .nav-left { order: 1; width: 100%; text-align: center; }
  .nav-center { 
    order: 2; 
    width: 100%; 
    text-align: center; 
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
    gap: 8px;
  }
  .nav-right { order: 3; width: 100%; text-align: center; }
  .nav-link { 
    margin: 4px 6px; 
    font-size: 14px;
    padding: 6px 8px;
  }
  .logout-btn {
    font-size: 14px;
    padding: 6px 12px;
  }
}
</style>