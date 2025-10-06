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
            <router-link to="/courses" class="nav-link" >Cursos</router-link>
            <router-link to="/my-courses" class="nav-link">Mis Cursos</router-link>
            
            <!-- Role-based navigation -->
            <router-link 
              v-if="userProfile?.rol === 'director'" 
              to="/teachers" 
              class="nav-link"
            >
              Profesores
            </router-link>
            
            <router-link 
              v-if="userProfile?.rol === 'director' || userProfile?.rol === 'profesor'" 
              to="/students" 
              class="nav-link"
            >
              Estudiantes
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
              
              <!-- Role-based navigation -->
              <router-link 
                v-if="userProfile?.rol === 'director'" 
                to="/teachers" 
                class="nav-link"
                @click="toggleMobileMenu"
              >
                Profesores
              </router-link>
              
              <router-link 
                v-if="userProfile?.rol === 'director' || userProfile?.rol === 'profesor'" 
                to="/students" 
                class="nav-link"
                @click="toggleMobileMenu"
              >
                Estudiantes
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

async function getUserProfile() {
  try {
    const { data: { user } } = await supabase.auth.getUser()
    if (!user) return null

    const token = localStorage.getItem('token')
    if (!token) return null

    const tenant = getTenantFromEmail(user.email)
    if (!tenant) return null

    const backendUrl = window.location.hostname === 'localhost' ? 'http://localhost:5002' : '/api/auth'
    
    const response = await fetch(`${backendUrl}/api/auth/user-profile`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    })

    if (response.ok) {
      const profileData = await response.json()
      userProfile.value = profileData
    } else {
      userProfile.value = {
        nombre: user.user_metadata?.full_name || user.user_metadata?.name || 'Usuario',
        email: user.email,
        rol: 'estudiante'
      }
    }
    
  } catch (error) {
    const { data: { user } } = await supabase.auth.getUser()
    if (user) {
      userProfile.value = {
        nombre: user.user_metadata?.full_name || user.user_metadata?.name || 'Usuario',
        email: user.email,
        rol: 'estudiante'
      }
    }
  }
}

function getTenantFromEmail(email) {
  if (email.endsWith('@ucb.edu.bo')) return 'ucb.edu.bo'
  if (email.endsWith('@upb.edu.bo')) return 'upb.edu.bo'
  if (email.endsWith('@gmail.com')) return 'gmail.com'
  return null
}

async function checkAuth() {
  try {
    const { data: { session } } = await supabase.auth.getSession()
    const token = localStorage.getItem('token')
    
    isAuthenticated.value = !!(session && token)
    
    if (isAuthenticated.value) {
      await getUserProfile()
    } else {
      userProfile.value = null
    }
  } catch (error) {
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
    subscription?.unsubscribe()
    removeRouteListener()
  }
}

async function handleLogout() {
  try {
    loading.value = true
    
    const { error } = await supabase.auth.signOut()
    if (error) throw error
    
    localStorage.removeItem('token')
    isAuthenticated.value = false
    userProfile.value = null
    mobileMenuOpen.value = false
    document.body.style.overflow = 'auto'
    
    router.push('/signin')

  } catch (error) {
    localStorage.removeItem('token')
    isAuthenticated.value = false
    userProfile.value = null
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
.navbar-wrapper {
  position: sticky;
  top: 0;
  z-index: 1000;
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

.user-role.director {
  background: #10b981;
  color: white;
  border-color: #10b981;
}

.user-role.estudiante {
  background: #3b82f6;
  color: white;
  border-color: #3b82f6;
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