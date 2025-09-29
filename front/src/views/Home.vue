<template>
  <transition name="fade">
    <div class="home-container">
      <!-- Hero de inicio -->
      <section class="home-hero">
        <div class="hero-content">
          <h1>Moderniza la gestión académica de tu institución</h1>
          <p>Asistencias, calificaciones, reportes y comunicación en una sola plataforma.</p>
          <div class="hero-actions">
            <router-link to="/signup" class="btn-primary">Comenzar</router-link>
            <router-link to="/features" class="btn-outline">Ver características</router-link>
          </div>
        </div>
      </section>

      <!-- Contadores animados -->
      <section ref="statsSection" class="stats">
        <div class="stat" v-for="s in stats" :key="s.key">
          <strong>{{ formatStat(s) }}</strong>
          <span>{{ s.label }}</span>
        </div>
      </section>

      <!-- Sobre SG -->
      <div class="info-negocio">
        <h2>Sobre StudentGest</h2>
        <p>
          StudentGest es una plataforma SaaS para la gestión académica, diseñada para instituciones educativas que buscan eficiencia, seguridad y comunicación efectiva entre padres, profesores y estudiantes.
        </p>
        <ul>
          <li>Gestión de asistencias y calificaciones</li>
          <li>Foros de comunicación y reportes detallados</li>
          <li>Acceso seguro desde cualquier dispositivo</li>
          <li>Escalable para miles de estudiantes</li>
        </ul>
      </div>

      <!-- Galería (imágenes) -->
      <section class="gallery">
        <img src="https://images.unsplash.com/photo-1523580846011-d3a5bc25702b?q=80&w=800&auto=format&fit=crop" alt="Aula y aprendizaje" />
        <img src="https://images.unsplash.com/photo-1529101091764-c3526daf38fe?q=80&w=800&auto=format&fit=crop" alt="Trabajo en equipo" />
        <img src="https://images.unsplash.com/photo-1509062522246-3755977927d7?q=80&w=800&auto=format&fit=crop" alt="Tecnología educativa" />
      </section>

      <!-- Testimonios -->
      <section class="testimonials">
        <article class="tcard">
          <p>“StudentGest nos permitió optimizar procesos y mejorar la comunicación con los padres.”</p>
          <span>- Directora, UCB</span>
        </article>
        <article class="tcard">
          <p>“Los reportes son claros y el control de asistencias es inmediato.”</p>
          <span>- Coordinador Académico, UPB</span>
        </article>
        <article class="tcard">
          <p>“Escalable y seguro, ideal para nuestra red de colegios.”</p>
          <span>- CTO, Red Educativa</span>
        </article>
      </section>

      <!-- Aliados/Partners -->
      <section class="partners">
        <span class="badge">Google Workspace</span>
        <span class="badge">Microsoft 365</span>
        <span class="badge">Supabase</span>
        <span class="badge">Azure</span>
      </section>

      <!-- Llamado a la acción -->
      <section class="cta">
        <h3>¿Listo para modernizar tu institución?</h3>
        <div class="cta-actions">
          <router-link to="/signup" class="btn-primary">Crear cuenta</router-link>
          <router-link to="/contact" class="btn-outline">Hablar con ventas</router-link>
        </div>
      </section>

      <!-- Botón de cierre de sesión (Logout) -->
      <button v-if="username" @click="handleLogout" class="btn-logout">
        Cerrar sesión
      </button>
    </div>
  </transition>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import { supabase } from '../supabase'

const router = useRouter()
const username = ref('')
const tenant = ref('')
const usuariosUcb = ref([])
const usuariosUpb = ref([])

function getTenantFromEmail(email) {
  if (email.endsWith('@ucb.edu.bo')) return 'tenant_ucb'
  if (email.endsWith('@upb.edu.bo')) return 'tenant_upb'
  if (email.endsWith('@gmail.com')) return 'tenant_gmail'
  return null
}

// Contadores
const statsSection = ref(null)
const stats = ref([
  { key: 'institutions', label: 'Instituciones', target: 250, value: 0, suffix: '+' },
  { key: 'students',     label: 'Estudiantes',   target: 120000, value: 0 },
  { key: 'uptime',       label: 'Uptime',        target: 99.95, value: 0, decimals: 2, suffix: '%' },
  { key: 'rating',       label: 'Satisfacción',  target: 4.8, value: 0, decimals: 1, suffix: '/5' },
])

let observer
const rafIds = new Map()

function animateCount(stat, duration = 1200) {
  const start = performance.now()
  const from = 0
  const to = stat.target
  const decimals = stat.decimals ?? 0
  const easeOutCubic = (x) => 1 - Math.pow(1 - x, 3)
  const tick = (now) => {
    const progress = Math.min((now - start) / duration, 1)
    const eased = easeOutCubic(progress)
    const current = from + (to - from) * eased
    stat.value = Number(current.toFixed(decimals))
    if (progress < 1) {
      const id = requestAnimationFrame(tick)
      rafIds.set(stat.key, id)
    }
  }
  const id = requestAnimationFrame(tick)
  rafIds.set(stat.key, id)
}

function startStats() {
  stats.value.forEach((s) => animateCount(s))
}

function formatStat(stat) {
  const v = stat.value ?? 0
  if (stat.key === 'students') return v >= 1000 ? `${Math.round(v / 1000)}k` : `${Math.round(v)}`
  if (stat.decimals != null) return `${v.toFixed(stat.decimals)}${stat.suffix ?? ''}`
  return `${Math.round(v)}${stat.suffix ?? ''}`
}

// Sesión (modo invitado si no hay usuario)
onMounted(async () => {
  try {
    const { data: { session } } = await supabase.auth.getSession()
    if (session?.user) {
      username.value = session.user.email
      tenant.value = getTenantFromEmail(username.value)

      // 🔹 Obtener usuarios de tenant_ucb
      const { data: ucb, error: errUcb } = await supabase.rpc('get_usuarios_ucb')
      if (errUcb) console.error('Error consultando tenant_ucb:', errUcb)
      else usuariosUcb.value = ucb

      // 🔹 Obtener usuarios de tenant_upb
      const { data: upb, error: errUpb } = await supabase.rpc('get_usuarios_upb')
      if (errUpb) console.error('Error consultando tenant_upb:', errUpb)
      else usuariosUpb.value = upb

      const { data: gmail, error: errGmail } = await supabase.rpc('get_usuarios_gmail')
      if (errGmail) console.error('Error consultando tenant_gmail:', errGmail)
      else console.log('Usuarios en tenant_gmail:', gmail)
    }
  } catch (e) {
    console.error('Error al obtener sesión:', e)
  }

  await nextTick()
  if ('IntersectionObserver' in window && statsSection.value) {
    let triggered = false
    observer = new IntersectionObserver((entries) => {
      if (!triggered && entries.some(e => e.isIntersecting)) {
        triggered = true
        startStats()
        observer.disconnect()
      }
    }, { threshold: 0.3 })
    observer.observe(statsSection.value)
  } else {
    startStats()
  }
})

onBeforeUnmount(() => {
  if (observer) observer.disconnect()
  rafIds.forEach((id) => cancelAnimationFrame(id))
})

async function handleLogout() {
  try {
    await supabase.auth.signOut()
    username.value = ''
    router.push('/signin')
  } catch (e) {
    console.error('Error al cerrar sesión:', e)
  }
}
</script>

<style scoped>
.home-container {
  text-align: center;
  margin-top: 0.5rem;
}

/* Hero */
.home-hero {
  position: relative;
  border-radius: 16px;
  padding: 3rem 1rem;
  margin: 1rem auto 0;
  max-width: 1100px;
  overflow: hidden;
  background:
    linear-gradient(135deg, rgba(42,77,208,0.95), rgba(59,130,246,0.9)),
    url('https://images.unsplash.com/photo-1523580846011-d3a5bc25702b?q=80&w=1600&auto=format&fit=crop');
  background-size: cover;
  background-position: center;
  color: #fff;
  box-shadow: 0 12px 30px rgba(0,0,0,.12);
}
.hero-content { max-width: 760px; margin: 0 auto; text-align: center; }
.hero-content h1 { font-size: 2rem; margin-bottom: .5rem; }
.hero-content p { opacity: .95; }
.hero-actions { margin-top: 1rem; display:flex; gap:.75rem; justify-content:center; flex-wrap: wrap; }
.hero-actions .btn-primary, .hero-actions .btn-outline { text-decoration:none; padding:.6rem 1rem; border-radius:8px; font-weight:600; }
.hero-actions .btn-primary { background:#fff; color:#1f3bb3; }
.hero-actions .btn-outline { background:transparent; color:#fff; border:2px solid #fff; }

/* Info */
.info-negocio {
  margin: 2rem auto;
  max-width: 900px;
  background: #fff;
  border-radius: 1rem;
  box-shadow: 0 2px 8px rgba(0,0,0,0.07);
  padding: 1.5rem;
  text-align: left;
  animation: slideIn 1s;
}
.info-negocio h2 { color: #2a4dd0; margin-bottom: 0.5rem; }
.info-negocio ul { margin-top: 1rem; padding-left: 1.2rem; }
.info-negocio li { margin-bottom: 0.5rem; }

/* Logout */
.btn-logout {
  margin-top: 2rem;
  padding: 0.75rem 1.5rem;
  background-color: #ef4444;
  color: white;
  border: none;
  border-radius: 0.375rem;
  cursor: pointer;
}

/* Stats */
.stats {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 1rem;
  max-width: 1100px;
  margin: 1.5rem auto 0;
}
.stat {
  background: #fff;
  border-radius: 14px;
  padding: 1rem;
  text-align:center;
  box-shadow: 0 2px 10px rgba(0,0,0,.06);
  animation: fadeUp .6s ease both;
}
.stat strong { display:block; font-size: 1.8rem; line-height:1; color:#2a4dd0; }
.stat span { color:#555; }

/* Gallery */
.gallery {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1rem;
  max-width: 1100px;
  margin: 2rem auto;
}
.gallery img {
  width: 100%;
  height: 220px;
  object-fit: cover;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0,0,0,.06);
  animation: zoomIn .5s ease both;
}

/* Testimonials */
.testimonials {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1rem;
  max-width: 1100px;
  margin: 2rem auto;
}
.tcard {
  background: #fff;
  border-radius: 12px;
  padding: 1rem;
  box-shadow: 0 2px 8px rgba(0,0,0,.06);
  animation: fadeUp .6s ease both;
}
.tcard p { margin: 0 0 .5rem; }
.tcard span { color: #666; }

/* Partners */
.partners {
  display: flex;
  flex-wrap: wrap;
  gap: .5rem;
  justify-content: center;
  margin: 2rem auto;
}
.badge {
  background: #eef2ff;
  color: #2a4dd0;
  border: 1px solid #c7d2fe;
  padding: .4rem .7rem;
  border-radius: 999px;
  font-weight: 600;
}

/* CTA */
.cta {
  text-align: center;
  margin: 2rem auto;
  padding: 1.5rem;
  background: #ffffff;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0,0,0,.06);
  max-width: 900px;
}
.cta h3 { margin-bottom: .75rem; }
.cta-actions { display: flex; gap: .75rem; justify-content: center; }
.cta .btn-primary, .cta .btn-outline { text-decoration: none; padding: .55rem 1rem; border-radius: 8px; font-weight: 600; }
.cta .btn-primary { background: #2a4dd0; color: #fff; }
.cta .btn-outline { background: #fff; color: #2a4dd0; border: 2px solid #2a4dd0; }

/* Animations */
.fade-enter-active, .fade-leave-active { transition: opacity 0.7s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
@keyframes slideIn { from { transform: translateY(40px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
@keyframes fadeUp { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: none; } }
@keyframes zoomIn { from { opacity: 0; transform: scale(.98); } to { opacity: 1; transform: scale(1); } }

/* Responsive */
@media (max-width: 900px) {
  .stats { grid-template-columns: repeat(2, 1fr); }
  .testimonials { grid-template-columns: repeat(2, 1fr); }
  .gallery { grid-template-columns: repeat(2, 1fr); }
}
@media (max-width: 600px) {
  .home-hero { padding: 2rem 1rem; }
  .hero-content h1 { font-size: 1.5rem; }
  .stats, .testimonials, .gallery { grid-template-columns: 1fr; }
}
</style>