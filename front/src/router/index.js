import { createRouter, createWebHistory } from 'vue-router'
import SignIn from '../views/SignIn.vue'
import SignUp from '../views/SignUp.vue'
import Home from '../views/Home.vue'
import Features from '../views/Features.vue'
import Pricing from '../views/Pricing.vue'
import Info from '../views/Info.vue'
import Contact from '../views/Contact.vue'
import Nosotros from '../views/Nosotros.vue'

const routes = [
  { path: '/', redirect: '/features' },
  { path: '/signin', component: SignIn },
  { path: '/signup', component: SignUp },
  { path: '/features', component: Features },
  { path: '/pricing', component: Pricing },
  { path: '/info', component: Info },
  { path: '/nosotros', component: Nosotros },
  { path: '/contact', component: Contact },
  { path: '/home', component: Home },
  { path: '/:pathMatch(.*)*', redirect: '/features' }
]

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior() {
    return { top: 0, behavior: 'smooth' }
  }
})

export default router