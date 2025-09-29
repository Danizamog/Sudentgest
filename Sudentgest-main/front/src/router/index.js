import { createRouter, createWebHistory } from 'vue-router'
import Foro from '../views/Foro.vue'

const routes = [
  {
    path: '/',
    name: 'Home',
    redirect: '/foro'  // Redirige directamente al foro
  },
  {
    path: '/foro',
    name: 'Foro', 
    component: Foro
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

export default router