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
import Courses from './views/Courses.vue'
import MyCourses from './views/MyCourses.vue'
import CourseDetail from './views/CourseDetail.vue'
import Teachers from './views/Teachers.vue'
import Students from './views/Students.vue'
import UploadGrades from './views/UploadGrades.vue'

const routes = [
  { 
    path: '/', 
    redirect: '/signin'
  },
  { 
    path: '/signin', 
    component: SignIn
  },
  { 
    path: '/auth/callback', 
    component: AuthCallback
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
    path: '/courses', 
    component: Courses, 
    meta: { requiresAuth: true } 
  },
  { 
    path: '/my-courses', 
    component: MyCourses, 
    meta: { requiresAuth: true } 
  },
  { 
    path: '/courses/:id', 
    component: CourseDetail, 
    meta: { requiresAuth: true } 
  },
  { 
    path: '/teachers', 
    component: Teachers, 
    meta: { 
      requiresAuth: true,
      requiresDirector: true 
    } 
  },
  { 
    path: '/students', 
    component: Students, 
    meta: { 
      requiresAuth: true,
      requiresTeacherOrDirector: true 
    } 
  },
  { 
    path: '/upload-grades', 
    component: UploadGrades, 
    meta: { 
      requiresAuth: true,
      requiresTeacherOrDirector: true 
    } 
  },
  { 
    path: '/:pathMatch(.*)*', 
    redirect: '/signin'
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// Cache de rol de usuario
let userRoleCache = null

// Función para obtener rol del usuario
async function getUserRole() {
  if (userRoleCache) return userRoleCache
  
  try {
    const token = localStorage.getItem('token')
    if (!token) throw new Error('No token found')
    
    const backendUrl = window.location.hostname === 'localhost' 
      ? 'http://localhost:5002' 
      : '/api/auth'
    
    const response = await fetch(`${backendUrl}/api/auth/user-profile`, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    })
    
    if (response.ok) {
      const profile = await response.json()
      userRoleCache = profile.rol || 'estudiante'
      return userRoleCache
    }
    
    throw new Error('Failed to fetch user profile')
  } catch (error) {
    userRoleCache = 'estudiante'
    return userRoleCache
  }
}

// Guardia de navegación
router.beforeEach(async (to, from, next) => {
  try {
    const { data: { session } } = await supabase.auth.getSession()
    const token = localStorage.getItem('token')
    const isAuthenticated = !!(session && token)

    // Rutas que requieren autenticación
    if (to.meta.requiresAuth) {
      if (!isAuthenticated) {
        localStorage.removeItem('token')
        return next('/signin')
      }

      // Verificación de rol director
      if (to.meta.requiresDirector) {
        const userRole = await getUserRole()
        if (userRole !== 'Director') {
          return next('/home')
        }
      }

      // Verificación para profesores o directores
      if (to.meta.requiresTeacherOrDirector) {
        const userRole = await getUserRole()
        if (userRole !== 'director' && userRole !== 'profesor') {
          return next('/home')
        }
      }
      
      next()
    } 
    // Redirigir a home si ya está autenticado y trata de acceder a signin
    else if ((to.path === '/signin' || to.path === '/') && isAuthenticated) {
      next('/home')
    } 
    // Permitir acceso a rutas públicas
    else {
      next()
    }
    
  } catch (error) {
    localStorage.removeItem('token')
    next('/signin')
  }
})

// Limpiar cache cuando cambie la autenticación
supabase.auth.onAuthStateChange(() => {
  userRoleCache = null
})

export default router