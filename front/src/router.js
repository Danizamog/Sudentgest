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
import MyExcuses from './views/MyExcuses.vue'

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
  { path: '/courses', component: Courses, meta: { requiresAuth: true } },
  { path: '/my-courses', component: MyCourses, meta: { requiresAuth: true } },
  { path: '/courses/:id', component: CourseDetail, meta: { requiresAuth: true } },
  { 
    path: '/assignments', 
    name: 'assignments',
    component: AssignmentsView, 
    meta: { requiresAuth: true } 
  },
  { 
    path: '/courses/:id/assignments', 
    name: 'course-assignments', 
    component: AssignmentsView, 
    meta: { requiresAuth: true } 
  },
  { path: '/attendance', component: Attendance, meta: { requiresAuth: true } },
  { path: '/excuses', component: Excuses, meta: { requiresAuth: true } },
  { path: '/excuses/manage', component: ExcusesManagement, meta: { requiresAuth: true, requiresDirector: true } },
  { path: '/attendance/history', component: AttendanceHistory, meta: { requiresAuth: true } },
  { path: '/excuses/history', component: ExcusesHistory, meta: { requiresAuth: true } },
  {
    path: '/excuses/my',
    component: MyExcuses,
    meta: { requiresAuth: true }
  },
  { path: '/:pathMatch(.*)*', redirect: '/signin' }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// Cache de rol
let userRoleCache = null

async function getUserRole() {
  console.log('🔄 Obteniendo rol de usuario...');
  if (userRoleCache) {
    console.log('✅ Rol encontrado en cache:', userRoleCache);
    return userRoleCache;
  }

  try {
    console.log('📡 Haciendo fetch a /auth/user-profile...');
    const response = await fetch('/auth/user-profile', {
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include'
    })

    if (response.ok) {
      const profile = await response.json();
      userRoleCache = profile.rol || 'Estudiante';
      console.log('✅ Rol obtenido del servidor:', userRoleCache);
      return userRoleCache;
    }

    console.log('❌ Error en respuesta de user-profile:', response.status);
    throw new Error('Failed to fetch user profile');
  } catch (error) {
    console.error('❌ Error obteniendo rol:', error);
    userRoleCache = 'Estudiante';
    return userRoleCache;
  }
}

async function checkAuthentication() {
  console.log('🔐 Verificando autenticación...');
  try {
    const response = await fetch('/auth/check-cookie', {
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include'
    })

    if (response.ok) {
      const data = await response.json();
      console.log('✅ Usuario autenticado:', data.authenticated);
      return data.authenticated;
    }
    console.log('❌ Usuario no autenticado');
    return false;
  } catch (error) {
    console.error('❌ Error verificando autenticación:', error);
    return false;
  }
}

router.beforeEach(async (to, from, next) => {
  console.log('🚦 Navegando a:', to.path);
  console.log('📋 Meta:', to.meta);
  
  try {
    const isAuthenticated = await checkAuthentication();

    if (to.meta.requiresAuth) {
      if (!isAuthenticated) {
        console.log('❌ No autenticado, redirigiendo a signin');
        localStorage.removeItem('token');
        localStorage.removeItem('user_id');
        return next('/signin');
      }

      if (to.meta.requiresDirector) {
        console.log('🔍 Verificando rol de director...');
        const userRole = await getUserRole();
        if (userRole !== 'Director') {
          console.log('❌ No es director, redirigiendo a home');
          return next('/home');
        }
      }

      console.log('✅ Navegación permitida');
      next();
    } else if ((to.path === '/signin' || to.path === '/') && isAuthenticated) {
      console.log('✅ Usuario autenticado, redirigiendo a home');
      next('/home');
    } else {
      console.log('✅ Navegación libre');
      next();
    }
  } catch (error) {
    console.error('💥 Error en router guard:', error);
    localStorage.removeItem('token');
    localStorage.removeItem('user_id');
    next('/signin');
  }
})

supabase.auth.onAuthStateChange(() => {
  console.log('🔄 Estado de autenticación cambiado, limpiando cache de rol');
  userRoleCache = null;
})

export default router