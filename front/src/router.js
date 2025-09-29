import { createRouter, createWebHistory } from 'vue-router'
import SignIn from './views/SignIn.vue'
import Home from './views/Home.vue'

const routes = [
  // Redirigir siempre a /signin por defecto
  { path: '/', redirect: '/signin' },
  { path: '/signin', component: SignIn },
  { path: '/home', component: Home, meta: { requiresAuth: true } },

  // Manejo de rutas no encontradas
  { path: '/:pathMatch(.*)*', redirect: '/signin' }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

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
