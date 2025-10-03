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

  // 🔐 CUALQUIER otra ruta no definida también va a signin
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


router.beforeEach(async (to, from, next) => {
  console.log('🛡️ Router Guard - De:', from.path, 'A:', to.path)
  
  try {
    // Verificar autenticación para rutas protegidas
    if (to.meta.requiresAuth) {
      const { data: { session } } = await supabase.auth.getSession()
      const token = localStorage.getItem('token')
      
      console.log('🔐 Sesión:', !!session, 'Token:', !!token)
      
      // Si NO hay sesión activa, redirigir a signin
      if (!session || !token) {
        console.warn('🚫 Acceso denegado - No autenticado')
        await cleanupAuth()
        return next('/signin')
      }
      
      // Si requiere rol director, verificar
      if (to.meta.requiresDirector) {
        const userRole = await checkUserRole()
        console.log('👤 Rol del usuario:', userRole)
        
        if (userRole !== 'director') {
          console.warn('🚫 Acceso denegado - Se requiere rol director')
          return next('/home')
        }
      }
      
      console.log('✅ Acceso permitido')
      next()
    } 
    // Rutas públicas (signin, callback)
    else {
      // Si ya está autenticado y va a signin, redirigir a home
      if (to.path === '/signin') {
        const { data: { session } } = await supabase.auth.getSession()
        const token = localStorage.getItem('token')
        
        if (session && token) {
          console.log('🔐 Ya autenticado, redirigiendo a home')
          return next('/home')
        }
      }
      
      console.log('🌐 Ruta pública - Acceso permitido')
      next()
    }
    
  } catch (error) {
    console.error('💥 Error en guardia de ruta:', error)
    await cleanupAuth()
    next('/signin')
  }
})

// 🔧 FUNCIÓN: Limpiar autenticación
async function cleanupAuth() {
  try {
    localStorage.removeItem('token')
    await supabase.auth.signOut()
    console.log('🧹 Autenticación limpiada')
  } catch (error) {
    console.error('Error limpiando auth:', error)
  }
}

// 🔧 FUNCIÓN: Verificar rol del usuario
async function checkUserRole() {
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
      return profile.rol || 'estudiante'
    } else {
      throw new Error('Error del servidor')
    }
  } catch (error) {
    console.error('❌ Error verificando rol:', error)
    throw error
  }
}

// 🚨 Manejar errores globales del router
router.onError((error) => {
  console.error('🚨 Error de navegación:', error)
})

export default router