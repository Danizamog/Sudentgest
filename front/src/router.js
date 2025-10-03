import { createRouter, createWebHistory } from 'vue-router'
import { supabase } from './supabase'

// Vistas
import SignIn from './views/SignIn.vue'
import Home from './views/Home.vue'
import AuthCallback from './views/AuthCallback.vue'
import Foro from './views/Foro.vue'
import Features from './views/Features.vue'
import Pricing from './views/Pricing.vue'
import Info from './views/info.vue'
import Contact from './views/Contact.vue'
import Nosotros from './views/Nosotros.vue'
import Base from './views/BD.vue'

const routes = [
  { 
    path: '/', 
    redirect: '/signin',
    meta: { requiresAuth: false } 
  },

  // Autenticación
  { 
    path: '/signin', 
    component: SignIn, 
    meta: { requiresAuth: false } 
  },
  { 
    path: '/auth/callback', 
    component: AuthCallback, 
    meta: { requiresAuth: false } 
  },

  // Rutas protegidas
  { 
    path: '/home', 
    component: Home, 
    meta: { requiresAuth: true } 
  },
  { 
    path: '/foro', 
    component: Foro, 
    meta: { requiresAuth: true } 
  },
  { 
    path: '/features', 
    component: Features, 
    meta: { requiresAuth: true } 
  },
  { 
    path: '/pricing', 
    component: Pricing, 
    meta: { requiresAuth: true } 
  },
  { 
    path: '/info', 
    component: Info, 
    meta: { requiresAuth: true } 
  },
  { 
    path: '/contact', 
    component: Contact, 
    meta: { requiresAuth: true } 
  },
  { 
    path: '/nosotros', 
    component: Nosotros, 
    meta: { requiresAuth: true } 
  },
  { 
    path: '/base', 
    component: Base, 
    meta: { 
      requiresAuth: true,
      requiresDirector: true 
    } 
  },

  // Catch all - Redirige a signin
  { 
    path: '/:pathMatch(.*)*', 
    redirect: '/signin',
    meta: { requiresAuth: false } 
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 🔐 GUARDIA DE RUTAS CORREGIDA
router.beforeEach(async (to, from, next) => {
  console.log('🛡️ Router Guard - De:', from.path, 'A:', to.path)
  
  try {
    // Obtener sesión actual - FORZAR ACTUALIZACIÓN
    const { data: { session }, error } = await supabase.auth.getSession()
    const token = localStorage.getItem('token')
    
    // Verificar si hay sesión válida
    const isAuthenticated = !!(session && token && !error)
    
    console.log('🔐 Estado autenticación:', { 
      hasSession: !!session, 
      hasToken: !!token, 
      isAuthenticated 
    })

    // RUTAS PROTEGIDAS
    if (to.meta.requiresAuth) {
      if (!isAuthenticated) {
        console.warn('🚫 Acceso denegado - No autenticado, redirigiendo a signin')
        // Limpiar cache y redirigir
        userRoleCache = null
        localStorage.removeItem('token')
        return next('/signin')
      }

      // VERIFICACIÓN DE ROL DIRECTOR
      if (to.meta.requiresDirector) {
        const userRole = await getUserRole()
        console.log('👤 Rol del usuario:', userRole)
        
        if (userRole !== 'director') {
          console.warn('🚫 Acceso denegado - Se requiere rol director')
          return next('/home')
        }
      }
      
      console.log('✅ Acceso permitido a ruta protegida')
      next()
    } 
    // RUTAS PÚBLICAS (como /signin)
    else {
      // Si ya está autenticado y va a signin, redirigir a home
      if ((to.path === '/signin' || to.path === '/') && isAuthenticated) {
        console.log('🔐 Ya autenticado, redirigiendo a home desde:', to.path)
        return next('/home')
      }
      
      console.log('🌐 Ruta pública - Acceso permitido')
      next()
    }
    
  } catch (error) {
    console.error('💥 Error en guardia de ruta:', error)
    // En caso de error, limpiar y redirigir a signin
    userRoleCache = null
    localStorage.removeItem('token')
    next('/signin')
  }
})

// 🔧 FUNCIÓN: Obtener rol del usuario (cacheado)
let userRoleCache = null
async function getUserRole() {
  if (userRoleCache) return userRoleCache
  
  try {
    const token = localStorage.getItem('token')
    if (!token) throw new Error('No token')
    
    const backendUrl = window.location.hostname === 'localhost' 
      ? 'http://localhost:5002' 
      : '/api/auth'
    
    const response = await fetch(`${backendUrl}/api/auth/user-profile`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    })
    
    if (response.ok) {
      const profile = await response.json()
      userRoleCache = profile.rol || 'estudiante'
      return userRoleCache
    } else {
      throw new Error('Error del servidor')
    }
  } catch (error) {
    console.error('❌ Error verificando rol:', error)
    userRoleCache = 'estudiante' // Fallback seguro
    return userRoleCache
  }
}

// 🔄 Limpiar cache cuando cambie la autenticación
supabase.auth.onAuthStateChange(() => {
  userRoleCache = null
})

export default router