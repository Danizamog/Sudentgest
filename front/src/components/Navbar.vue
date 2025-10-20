<template>
  <div v-if="showNavbar" class="navbar-wrapper">
    <!-- HEADER -->
    <header class="site-header">
      <div class="container">
        <div class="header-content">
          <h1 class="site-title">StudentGest</h1>
          <div class="user-info" v-if="userProfile">
            <span class="user-name">Hola, {{ userProfile.nombre }}</span>
            <span class="user-role" :class="userProfile.rol">{{ userProfile.rol }}</span>
          </div>
        </div>
      </div>
    </header>

    <!-- NAVIGATION -->
    <nav class="site-nav">
      <div class="container">
        <!-- Desktop Navigation -->
        <div class="nav-desktop">
          <div class="nav-main">
            <router-link to="/home" class="nav-link">Inicio</router-link>
            <router-link to="/foro" class="nav-link">Foro</router-link>
            <router-link to="/pricing" class="nav-link">Precios</router-link>
            <router-link to="/nosotros" class="nav-link">Nosotros</router-link>
            <router-link v-if="userProfile?.rol === 'Director'" to="/courses" class="nav-link">Cursos</router-link>
            <router-link v-if="['Estudiante'].includes(userProfile?.rol)" to="/my-courses" class="nav-link">Mis Cursos</router-link>
            <router-link 
              v-if="userProfile?.rol === 'Director'" 
              to="/base" 
              class="nav-link admin-link"
            >Roles</router-link>
            <router-link 
              v-if="userProfile?.rol === 'Director'" 
              to="/director" 
              class="nav-link admin-link"
            >Gestión Usuarios</router-link>
            <router-link 
              v-if="['Profesor'].includes(userProfile?.rol)" 
              to="/attendance" 
              class="nav-link"
            >
              Registrar Asistencia
            </router-link>
            
            <router-link 
              v-if="userProfile?.rol === 'Director'" 
              to="/excuses/manage" 
              class="nav-link admin-link"
            >
              Gestionar Excusas
            </router-link>
            <router-link 
              v-if="['Profesor', 'Padre', 'Admin'].includes(userProfile?.rol)" 
              to="/excuses" 
              class="nav-link"
            >
              Excusas
            </router-link>
            <router-link 
              v-if="['Profesor'].includes(userProfile?.rol)" 
              to="/attendance/history" 
              class="nav-link"
            >
              Historial Asist.
            </router-link>
            <router-link v-if="['Profesor', 'Director'].includes(userProfile?.rol)" to="/excuses/history" class="nav-link">
              {{ userProfile?.rol === 'Director' ? 'Historial Excusas' : 'Mis Excusas' }}
            </router-link>
            <router-link 
              v-if="userProfile?.rol === 'Profesor'" 
              to="/teacher-grades" 
              class="nav-link"
            >
              Calificaciones
            </router-link>
            <router-link 
              v-if="userProfile?.rol === 'Estudiante'" 
              to="/student-grades" 
              class="nav-link"
            >
              Mis Notas
            </router-link>
            <router-link 
              v-if="userProfile?.rol === 'Profesor'" 
              to="/teacher-reports" 
              class="nav-link"
            >
              Reportes
            </router-link>
            <router-link 
              v-if="userProfile?.rol === 'Director'" 
              to="/director-reports" 
              class="nav-link admin-link"
            >
              Reportes
            </router-link>
          </div>

          <div class="nav-actions">
            <button @click="handleLogout" class="logout-btn" :disabled="loading">
              {{ loading ? 'Cerrando...' : 'Cerrar sesión' }}
            </button>
          </div>
        </div>

        <!-- Mobile Navigation -->
        <div class="nav-mobile">
          <button @click="toggleMobileMenu" class="mobile-menu-btn">
            ☰
          </button>
          
          <div v-if="mobileMenuOpen" class="mobile-menu-overlay" @click="toggleMobileMenu"></div>
          
          <div :class="['mobile-menu', { 'mobile-menu-open': mobileMenuOpen }]">
            <div class="mobile-menu-header">
              <h3>Menú</h3>
              <button @click="toggleMobileMenu" class="close-menu-btn">
                ×
              </button>
            </div>
            
            <div class="mobile-nav-links">
              <router-link to="/home" class="nav-link" @click="toggleMobileMenu">Inicio</router-link>
              <router-link to="/foro" class="nav-link" @click="toggleMobileMenu">Foro</router-link>
              <router-link to="/pricing" class="nav-link" @click="toggleMobileMenu">Precios</router-link>
              <router-link to="/nosotros" class="nav-link" @click="toggleMobileMenu">Nosotros</router-link>
              <router-link to="/courses" class="nav-link" @click="toggleMobileMenu">Cursos</router-link>
              <router-link to="/my-courses" class="nav-link" @click="toggleMobileMenu">Mis Cursos</router-link>

              <router-link 
                v-if="userProfile?.rol === 'Director'" 
                to="/base" 
                class="nav-link admin-link"
                @click="toggleMobileMenu"
              >
                Roles
              </router-link>
              
              <router-link 
                v-if="userProfile?.rol === 'Director'" 
                to="/director" 
                class="nav-link admin-link"
                @click="toggleMobileMenu"
              >
                Gestión Usuarios
              </router-link>
              
              <router-link 
                v-if="userProfile?.rol === 'Profesor'" 
                to="/teacher-grades" 
                class="nav-link"
                @click="toggleMobileMenu"
              >
                Calificaciones
              </router-link>
              
              <router-link 
                v-if="userProfile?.rol === 'Estudiante'" 
                to="/student-grades" 
                class="nav-link"
                @click="toggleMobileMenu"
              >
                Mis Notas
              </router-link>
              
              <router-link 
                v-if="userProfile?.rol === 'Profesor'" 
                to="/teacher-reports" 
                class="nav-link"
                @click="toggleMobileMenu"
              >
                Reportes
              </router-link>
              
              <router-link 
                v-if="userProfile?.rol === 'Director'" 
                to="/director-reports" 
                class="nav-link admin-link"
                @click="toggleMobileMenu"
              >
                Reportes
              </router-link>
              
              <button @click="handleLogout" class="logout-btn mobile-logout" :disabled="loading">
                {{ loading ? 'Cerrando...' : 'Cerrar sesión' }}
              </button>
            </div>
          </div>
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
const userProfile = ref(null)
const mobileMenuOpen = ref(false)

const showNavbar = computed(() => {
  const hideOnRoutes = ['/signin', '/', '/auth/callback']
  return isAuthenticated.value && !hideOnRoutes.includes(route.path)
})

// 🔹 ELIMINAR getBackendUrl() y usar siempre rutas relativas

// Obtener perfil del usuario usando solo cookies
async function getUserProfile() {
  try {
    // ✅ CAMBIO: Ruta relativa en lugar de localhost:5002
    const response = await fetch('/auth/user-profile', {
      method: 'GET',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include'
    })

    if (response.ok) {
      userProfile.value = await response.json()
      isAuthenticated.value = true
      return userProfile.value
    } else {
      isAuthenticated.value = false
      userProfile.value = null
      return null
    }
  } catch (error) {
    console.error('Error obteniendo perfil:', error)
    isAuthenticated.value = false
    userProfile.value = null
    return null
  }
}

// Verificar autenticación
async function checkAuth() {
  try {
    // ✅ CAMBIO: Ruta relativa
    const checkResponse = await fetch('/auth/check-cookie', {
      method: 'GET',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include'
    })

    if (checkResponse.ok) {
      const data = await checkResponse.json()
      if (data.authenticated) {
        isAuthenticated.value = true
        await getUserProfile()
        return
      }
    }

    // Si no hay cookie válida, verificar con Supabase
    const { data: { session } } = await supabase.auth.getSession()
    if (session) {
      // Si hay sesión en Supabase pero no cookie, establecer cookie
      const token = session.access_token
      
      // ✅ CAMBIO: Ruta relativa
      const cookieResponse = await fetch('/auth/session-cookie', {
        method: 'POST',
        headers: { 
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        credentials: 'include'
      })

      if (cookieResponse.ok) {
        isAuthenticated.value = true
        await getUserProfile()
        return
      }
    }

    // Si no hay autenticación
    isAuthenticated.value = false
    userProfile.value = null

  } catch (error) {
    console.error('Error en checkAuth:', error)
    isAuthenticated.value = false
    userProfile.value = null
  }
}

function toggleMobileMenu() {
  mobileMenuOpen.value = !mobileMenuOpen.value
  if (mobileMenuOpen.value) {
    document.body.style.overflow = 'hidden'
  } else {
    document.body.style.overflow = 'auto'
  }
}

function setupListeners() {
  const { data: { subscription } } = supabase.auth.onAuthStateChange(async () => {
    await checkAuth()
  })

  const removeRouteListener = router.afterEach(() => {
    checkAuth()
    mobileMenuOpen.value = false
    document.body.style.overflow = 'auto'
  })

  return () => {
    try { subscription?.unsubscribe() } catch {}
    try { removeRouteListener() } catch {}
  }
}

async function handleLogout() {
  try {
    loading.value = true

    // Limpiar Supabase
    try { await supabase.auth.signOut() } catch {}

    // Limpiar cookie en el backend
    try {
      // ✅ CAMBIO: Ruta relativa
      await fetch('/auth/clear-cookie', {
        method: 'POST',
        credentials: 'include'
      })
    } catch (e) {
      console.warn('clear-cookie failed:', e)
    }

    // Limpiar localStorage (por si acaso)
    localStorage.removeItem('token')
    localStorage.removeItem('user_id')

    // Resetear estado
    isAuthenticated.value = false
    userProfile.value = null
    mobileMenuOpen.value = false
    document.body.style.overflow = 'auto'

    router.push('/signin')
  } catch (error) {
    console.error('Error durante logout:', error)
    router.push('/signin')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  checkAuth()
  const cleanup = setupListeners()
  
  onUnmounted(() => {
    cleanup()
    document.body.style.overflow = 'auto'
  })
})
</script>

<style scoped>
/* Tus estilos CSS existentes se mantienen igual */
.navbar-wrapper {
  position: sticky;
  top: 0;
}

.container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 20px;
}

.site-header {
  width: 100%;
  background: #111318;
  color: #ffffff;
  padding: 12px 0;
  border-bottom: 1px solid #2d3748;
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.site-title {
  margin: 0;
  font-size: 24px;
  font-weight: 700;
  color: #ffffff;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 14px;
}

.user-name {
  color: #e5e7eb;
  font-weight: 500;
}

.user-role {
  padding: 6px 12px;
  border-radius: 20px;
  font-size: 12px;
  font-weight: 600;
  text-transform: capitalize;
  border: 1px solid;
}

.user-role.Director {
  background: #10b981;
  color: white;
  border-color: #10b981;
}

.user-role.Estudiante {
  background: #3b82f6;
  color: white;
  border-color: #3b82f6;
}

.user-role.Profesor {
  background: #f68f3b;
  color: white;
  border-color: #f68f3b;
}

.site-nav {
  width: 100%;
  background: #f8fafc;
  border-bottom: 1px solid #e2e8f0;
}

.nav-desktop {
  display: flex;
  justify-content: space-between;
  align-items: center;
  height: 64px;
}

@media (max-width: 768px) {
  .nav-desktop {
    display: none;
  }
}

.nav-main {
  display: flex;
  align-items: center;
  gap: 2px;
}

.nav-link {
  display: flex;
  align-items: center;
  padding: 10px 16px;
  color: #475569;
  text-decoration: none;
  font-weight: 500;
  font-size: 14px;
  border-radius: 8px;
  transition: all 0.3s ease;
  white-space: nowrap;
}

.nav-link:hover {
  background: #ffffff;
  color: #1e293b;
}

.nav-link.router-link-active,
.nav-link.router-link-exact-active {
  background: #e0e7ff;
  color: #3730a3;
  font-weight: 600;
}

.nav-link.admin-link {
  background: #fef3c7;
  color: #92400e;
  font-weight: 600;
  border: 1px solid #f59e0b;
}

.nav-link.admin-link.router-link-active {
  background: #f59e0b;
  color: white;
}

.nav-actions {
  display: flex;
  align-items: center;
}

.logout-btn {
  display: flex;
  align-items: center;
  background: #dc2626;
  color: #ffffff;
  padding: 10px 20px;
  border-radius: 8px;
  border: none;
  font-weight: 600;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.3s ease;
}

.logout-btn:hover:not(:disabled) {
  background: #b91c1c;
}

.logout-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.nav-mobile {
  display: none;
}

@media (max-width: 768px) {
  .nav-mobile {
    display: block;
    height: 64px;
    display: flex;
    align-items: center;
    justify-content: space-between;
  }
}

.mobile-menu-btn {
  background: none;
  border: none;
  padding: 12px;
  cursor: pointer;
  color: #475569;
  border-radius: 8px;
  transition: all 0.3s ease;
  background: #ffffff;
}

.mobile-menu-btn:hover {
  background: #f1f5f9;
}

.mobile-menu-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0,0,0,0.5);
  z-index: 999;
}

.mobile-menu {
  position: fixed;
  top: 0;
  right: -100%;
  width: 300px;
  height: 100vh;
  background: white;
  transition: right 0.3s ease;
  z-index: 1000;
  display: flex;
  flex-direction: column;
}

.mobile-menu-open {
  right: 0;
}

.mobile-menu-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
  border-bottom: 1px solid #e2e8f0;
  background: #f8fafc;
}

.mobile-menu-header h3 {
  margin: 0;
  color: #1e293b;
  font-size: 20px;
  font-weight: 700;
}

.close-menu-btn {
  background: none;
  border: none;
  padding: 8px;
  cursor: pointer;
  color: #64748b;
  border-radius: 6px;
  transition: all 0.3s ease;
}

.close-menu-btn:hover {
  background: #f1f5f9;
}

.mobile-nav-links {
  flex: 1;
  padding: 20px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  overflow-y: auto;
}

.mobile-nav-links .nav-link {
  justify-content: flex-start;
  padding: 14px 16px;
  border-radius: 8px;
  margin-bottom: 4px;
}

.mobile-logout {
  margin-top: 20px;
  justify-content: center;
}

@media (max-width: 1024px) {
  .nav-main {
    gap: 1px;
  }
  
  .nav-link {
    padding: 8px 12px;
    font-size: 13px;
  }
}

@media (max-width: 880px) {
  .nav-desktop .nav-main {
    gap: 0;
  }
  
  .nav-link {
    padding: 8px 10px;
    font-size: 12px;
  }
  
  .logout-btn {
    padding: 10px;
  }
}

.mobile-nav-links::-webkit-scrollbar {
  width: 6px;
}

.mobile-nav-links::-webkit-scrollbar-track {
  background: #f1f5f9;
}

.mobile-nav-links::-webkit-scrollbar-thumb {
  background: #cbd5e1;
  border-radius: 3px;
}

.mobile-nav-links::-webkit-scrollbar-thumb:hover {
  background: #94a3b8;
}
</style>