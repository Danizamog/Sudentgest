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
            <router-link to="/home" class="nav-link">
              <span class="nav-icon">🏠</span>
              Inicio
            </router-link>
            <router-link to="/foro" class="nav-link">
              <span class="nav-icon">💬</span>
              Foro
            </router-link>
            <router-link to="/pricing" class="nav-link">
              <span class="nav-icon">💰</span>
              Precios
            </router-link>
            <router-link to="/nosotros" class="nav-link">
              <span class="nav-icon">👥</span>
              Nosotros
            </router-link>
            <router-link 
              v-if="userProfile?.rol === 'director'" 
              to="/base" 
              class="nav-link admin-link"
            >
              <span class="nav-icon">🗃️</span>
              Base de Datos
            </router-link>
          </div>

          <div class="nav-actions">
            <button @click="handleLogout" class="logout-btn" :disabled="loading">
              <span class="btn-icon">{{ loading ? '⏳' : '🚪' }}</span>
              <span class="btn-text">{{ loading ? 'Cerrando...' : 'Cerrar sesión' }}</span>
            </button>
          </div>
        </div>

        <!-- Mobile Navigation -->
        <div class="nav-mobile">
          <button @click="toggleMobileMenu" class="mobile-menu-btn">
            <span class="menu-icon">☰</span>
          </button>
          
          <div v-if="mobileMenuOpen" class="mobile-menu-overlay" @click="toggleMobileMenu"></div>
          
          <div :class="['mobile-menu', { 'mobile-menu-open': mobileMenuOpen }]">
            <div class="mobile-menu-header">
              <h3>Menú</h3>
              <button @click="toggleMobileMenu" class="close-menu-btn">
                <span class="close-icon">×</span>
              </button>
            </div>
            
            <div class="mobile-nav-links">
              <router-link to="/home" class="nav-link" @click="toggleMobileMenu">
                <span class="nav-icon">🏠</span>
                Inicio
              </router-link>
              <router-link to="/foro" class="nav-link" @click="toggleMobileMenu">
                <span class="nav-icon">💬</span>
                Foro
              </router-link>
              <router-link to="/features" class="nav-link" @click="toggleMobileMenu">
                <span class="nav-icon">⭐</span>
                Características
              </router-link>
              <router-link to="/pricing" class="nav-link" @click="toggleMobileMenu">
                <span class="nav-icon">💰</span>
                Precios
              </router-link>
              <router-link to="/info" class="nav-link" @click="toggleMobileMenu">
                <span class="nav-icon">ℹ️</span>
                Información
              </router-link>
              <router-link to="/contact" class="nav-link" @click="toggleMobileMenu">
                <span class="nav-icon">📞</span>
                Contacto
              </router-link>
              <router-link to="/nosotros" class="nav-link" @click="toggleMobileMenu">
                <span class="nav-icon">👥</span>
                Nosotros
              </router-link>
              <router-link 
                v-if="userProfile?.rol === 'director'" 
                to="/base" 
                class="nav-link admin-link"
                @click="toggleMobileMenu"
              >
                <span class="nav-icon">🗃️</span>
                Base de Datos
              </router-link>
              
              <button @click="handleLogout" class="logout-btn mobile-logout" :disabled="loading">
                <span class="btn-icon">{{ loading ? '⏳' : '🚪' }}</span>
                <span class="btn-text">{{ loading ? 'Cerrando...' : 'Cerrar sesión' }}</span>
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

// 🔹 COMPUTED: Mostrar navbar solo si está autenticado Y no está en signin
const showNavbar = computed(() => {
  const hideOnRoutes = ['/signin', '/', '/auth/callback']
  return isAuthenticated.value && !hideOnRoutes.includes(route.path)
})

// 🔹 FUNCIÓN: Obtener perfil del usuario desde el backend
async function getUserProfile() {
  try {
    const { data: { user } } = await supabase.auth.getUser()
    if (!user) return null

    const token = localStorage.getItem('token')
    if (!token) return null

    // Obtener tenant del email
    const tenant = getTenantFromEmail(user.email)
    if (!tenant) return null

    // Llamar al backend para obtener el perfil real
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
      console.log('🔹 Perfil de usuario obtenido:', profileData)
    } else {
      console.warn('No se pudo obtener el perfil del usuario, usando datos básicos')
      // Fallback a datos básicos
      userProfile.value = {
        nombre: user.user_metadata?.full_name || user.user_metadata?.name || 'Usuario',
        email: user.email,
        rol: 'estudiante'
      }
    }
    
  } catch (error) {
    console.error('Error obteniendo perfil:', error)
    // Fallback en caso de error
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

// 🔹 FUNCIÓN: Verificar autenticación
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
    
    console.log('🔹 Navbar - Autenticado:', isAuthenticated.value, 'Ruta:', route.path)
  } catch (error) {
    console.error('Error verificando autenticación:', error)
    isAuthenticated.value = false
    userProfile.value = null
  }
}

// 🔹 FUNCIÓN: Toggle menú móvil
function toggleMobileMenu() {
  mobileMenuOpen.value = !mobileMenuOpen.value
  if (mobileMenuOpen.value) {
    document.body.style.overflow = 'hidden'
  } else {
    document.body.style.overflow = 'auto'
  }
}

// 🔹 ESCUCHAR CAMBIOS
function setupListeners() {
  const { data: { subscription } } = supabase.auth.onAuthStateChange(async (event, session) => {
    console.log('🔹 Navbar - Auth state change:', event)
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

// 🔹 FUNCIÓN CORREGIDA: Cerrar sesión sin redirección automática
async function handleLogout() {
  try {
    loading.value = true
    console.log('🔹 Navbar - Iniciando cierre de sesión...')
    
    // 1. Cerrar sesión en Supabase
    const { error } = await supabase.auth.signOut()
    if (error) {
      console.error('Error en supabase signOut:', error)
      throw error
    }
    
    // 2. Limpiar localStorage
    localStorage.removeItem('token')
    
    // 3. Resetear estado local
    isAuthenticated.value = false
    userProfile.value = null
    mobileMenuOpen.value = false
    document.body.style.overflow = 'auto'
    
    console.log('🔹 Navbar - Sesión cerrada exitosamente')
    
    // 4. NO redirigir automáticamente - el router guard se encargará
    // El usuario permanecerá en la página actual pero sin navbar
    // y el router guard lo redirigirá si intenta acceder a rutas protegidas
    
  } catch (error) {
    console.error('Error completo al cerrar sesión:', error)
    
    // Forzar limpieza incluso si hay error
    localStorage.removeItem('token')
    isAuthenticated.value = false
    userProfile.value = null
    
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

/* HEADER STYLES */
.site-header {
  width: 100%;
  background: linear-gradient(135deg, #111318 0%, #1a1d24 100%);
  color: #ffffff;
  padding: 12px 0;
  box-shadow: 0 2px 10px rgba(0,0,0,0.1);
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
  letter-spacing: 0.5px;
  color: #ffffff;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
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
  transition: all 0.3s ease;
}

.user-role.director {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  color: white;
  border-color: #10b981;
  box-shadow: 0 2px 8px rgba(16, 185, 129, 0.3);
}

.user-role.estudiante {
  background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%);
  color: white;
  border-color: #3b82f6;
  box-shadow: 0 2px 8px rgba(59, 130, 246, 0.3);
}

/* NAVIGATION STYLES */
.site-nav {
  width: 100%;
  background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
  border-bottom: 1px solid #e2e8f0;
  box-shadow: 0 1px 3px rgba(0,0,0,0.1);
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
  gap: 8px;
  padding: 10px 16px;
  color: #475569;
  text-decoration: none;
  font-weight: 500;
  font-size: 14px;
  border-radius: 8px;
  transition: all 0.3s ease;
  white-space: nowrap;
  position: relative;
  overflow: hidden;
}

.nav-link::before {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(59, 130, 246, 0.1), transparent);
  transition: left 0.5s ease;
}

.nav-link:hover::before {
  left: 100%;
}

.nav-link:hover {
  background: #ffffff;
  color: #1e293b;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0,0,0,0.1);
}

.nav-link.router-link-active,
.nav-link.router-link-exact-active {
  background: linear-gradient(135deg, #e0e7ff 0%, #c7d2fe 100%);
  color: #3730a3;
  font-weight: 600;
  box-shadow: 0 2px 8px rgba(55, 48, 163, 0.2);
}

.nav-link.admin-link {
  background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%);
  color: #92400e;
  font-weight: 600;
  border: 1px solid #f59e0b;
}

.nav-link.admin-link.router-link-active {
  background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
  color: white;
  box-shadow: 0 2px 8px rgba(245, 158, 11, 0.3);
}

.nav-icon {
  font-size: 16px;
  transition: transform 0.3s ease;
}

.nav-link:hover .nav-icon {
  transform: scale(1.1);
}

.nav-actions {
  display: flex;
  align-items: center;
}

/* LOGOUT BUTTON STYLES */
.logout-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  background: linear-gradient(135deg, #dc2626 0%, #b91c1c 100%);
  color: #ffffff;
  padding: 10px 20px;
  border-radius: 8px;
  border: none;
  font-weight: 600;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.3s ease;
  position: relative;
  overflow: hidden;
}

.logout-btn::before {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(255,255,255,0.2), transparent);
  transition: left 0.5s ease;
}

.logout-btn:hover::before {
  left: 100%;
}

.logout-btn:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 6px 20px rgba(220, 38, 38, 0.4);
}

.logout-btn:active {
  transform: translateY(0);
}

.logout-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}

.btn-icon {
  font-size: 16px;
  transition: transform 0.3s ease;
}

.logout-btn:hover .btn-icon {
  transform: scale(1.1);
}

/* MOBILE STYLES */
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
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.mobile-menu-btn:hover {
  background: #f1f5f9;
  transform: translateY(-2px);
}

.menu-icon {
  font-size: 20px;
  font-weight: bold;
}

.mobile-menu-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0,0,0,0.5);
  z-index: 999;
  animation: fadeIn 0.3s ease;
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

.mobile-menu {
  position: fixed;
  top: 0;
  right: -100%;
  width: 300px;
  height: 100vh;
  background: white;
  box-shadow: -4px 0 20px rgba(0,0,0,0.15);
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
  background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
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
  transform: scale(1.1);
}

.close-icon {
  font-size: 24px;
  font-weight: bold;
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

/* RESPONSIVE ADJUSTMENTS */
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
  
  .logout-btn .btn-text {
    display: none;
  }
  
  .logout-btn {
    padding: 10px;
  }
}

/* SCROLLBAR STYLING */
.mobile-nav-links::-webkit-scrollbar {
  width: 6px;
}

.mobile-nav-links::-webkit-scrollbar-track {
  background: #f1f5f9;
  border-radius: 3px;
}

.mobile-nav-links::-webkit-scrollbar-thumb {
  background: #cbd5e1;
  border-radius: 3px;
}

.mobile-nav-links::-webkit-scrollbar-thumb:hover {
  background: #94a3b8;
}
</style>