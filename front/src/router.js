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
import Courses from './views/Courses.vue'
import MyCourses from './views/MyCourses.vue'
import CourseDetail from './views/CourseDetail.vue'
import Attendance from './views/Attendance.vue'
import Excuses from './views/Excuses.vue'
import ExcusesManagement from './views/ExcusesManagement.vue'
import AttendanceHistory from './views/AttendanceHistory.vue'
import ExcusesHistory from './views/ExcusesHistory.vue'
import AssignmentsView from './views/AssignmentsView.vue'
import DirectorPanel from './views/DirectorPanel.vue'

const routes = [
  { path: '/', redirect: '/signin', meta: { hideNavbar: true } },
  { path: '/signin', component: SignIn, meta: { hideNavbar: true } },
  { path: '/auth/callback', component: AuthCallback, meta: { hideNavbar: true } },
  { path: '/home', component: Home, meta: { requiresAuth: true } },
  { path: '/foro', component: Foro, meta: { requiresAuth: true } },
  { path: '/features', component: Features, meta: { requiresAuth: true } },
  { path: '/pricing', component: Pricing, meta: { requiresAuth: true } },
  { path: '/info', component: Info, meta: { requiresAuth: true } },
  { path: '/contact', component: Contact, meta: { requiresAuth: true } },
  { path: '/nosotros', component: Nosotros, meta: { requiresAuth: true } },
  { path: '/base', component: Base, meta: { requiresAuth: true, requiresDirector: true } },
  { path: '/director', component: DirectorPanel, meta: { requiresAuth: true, requiresDirector: true } },
  { path: '/courses', component: Courses, meta: { requiresAuth: true } },
  { path: '/my-courses', component: MyCourses, meta: { requiresAuth: true } },
  { path: '/courses/:id', component: CourseDetail, meta: { requiresAuth: true } },
  { path: '/courses/:id/assignments', name: 'assignments', component: AssignmentsView, meta: { requiresAuth: true } },
  { path: '/attendance', component: Attendance, meta: { requiresAuth: true } },
  { path: '/excuses', component: Excuses, meta: { requiresAuth: true } },
  { path: '/excuses/manage', component: ExcusesManagement, meta: { requiresAuth: true, requiresDirector: true } },
  { path: '/attendance/history', component: AttendanceHistory, meta: { requiresAuth: true } },
  { path: '/excuses/history', component: ExcusesHistory, meta: { requiresAuth: true } },
  { path: '/:pathMatch(.*)*', redirect: '/signin' }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// Cache de rol
let userRoleCache = null

async function getUserRole() {
  if (userRoleCache) return userRoleCache

  try {
    // ✅ CAMBIO: Ruta relativa
    const response = await fetch('/auth/user-profile', {
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include'
    })

    if (response.ok) {
      const profile = await response.json()
      userRoleCache = profile.rol || 'Estudiante'
      return userRoleCache
    }

    throw new Error('Failed to fetch user profile')
  } catch (error) {
    userRoleCache = 'Estudiante'
    return userRoleCache
  }
}

async function checkAuthentication() {
  try {
    // ✅ CAMBIO: Ruta relativa
    const response = await fetch('/auth/check-cookie', {
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include'
    })

    if (response.ok) {
      const data = await response.json()
      return data.authenticated
    }
    return false
  } catch (error) {
    return false
  }
}

router.beforeEach(async (to, from, next) => {
  try {
    const isAuthenticated = await checkAuthentication()

    if (to.meta.requiresAuth) {
      if (!isAuthenticated) {
        // Limpiar localStorage por si acaso
        localStorage.removeItem('token')
        localStorage.removeItem('user_id')
        return next('/signin')
      }

      if (to.meta.requiresDirector) {
        const userRole = await getUserRole()
        if (userRole !== 'Director') return next('/home')
      }

      next()
    } else if ((to.path === '/signin' || to.path === '/') && isAuthenticated) {
      next('/home')
    } else {
      next()
    }
  } catch (error) {
    console.error('Error en router guard:', error)
    localStorage.removeItem('token')
    localStorage.removeItem('user_id')
    next('/signin')
  }
})

supabase.auth.onAuthStateChange(() => {
  userRoleCache = null
})

export default router