import { createRouter, createWebHistory } from 'vue-router'

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
  { path: '/', redirect: '/signin' },

  // Autenticación
  { path: '/signin', component: SignIn },
  { path: '/auth/callback', component: AuthCallback },

  // Rutas protegidas
  { path: '/features', component: Features,meta: { requiresAuth: true } },
  { path: '/pricing', component: Pricing,meta: { requiresAuth: true } },
  { path: '/info', component: Info,meta: { requiresAuth: true } },
  { path: '/contact', component: Contact,meta: { requiresAuth: true } },
  { path: '/nosotros', component: Nosotros,meta: { requiresAuth: true } },
  { path: '/base', component: Base,meta: { requiresAuth: true } },
  { path: '/home', component: Home, meta: { requiresAuth: true } },
  { path: '/foro', component: Foro, meta: { requiresAuth: true } },

  // Ruta para errores 404
  { path: '/:pathMatch(.*)*', redirect: '/signin' }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// Protección de rutas
router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('token')

  if (to.meta.requiresAuth && !token) {
    next('/signin')
  } else if (to.path === '/signin' && token) {
    next('/home')
  } else {
    next()
  }
})

export default router