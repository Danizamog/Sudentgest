import { createRouter, createWebHistory } from 'vue-router'
import SignIn from './views/SignIn.vue'
import Home from './views/Home.vue'

const routes = [
  { path: '/signin', component: SignIn },
  { path: '/', component: Home, meta: { requiresAuth: true } }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 🔒 middleware para proteger rutas
router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('token')

  if (to.meta.requiresAuth && !token) {
    next('/signin')
  } else if (to.path === '/signin' && token) {
    next('/')
  } else {
    next()
  }
})

export default router
