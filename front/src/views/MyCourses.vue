<template>
  <transition name="fade">
    <section class="wrap">
      <h1>Mis Cursos</h1>
      <p class="subtitle">Cursos en los que estás inscrito</p>

      <div v-if="loading" class="loading">Cargando tus cursos...</div>
      <div v-else-if="error" class="error">{{ error }}</div>
      
      <div v-else-if="cursos.length === 0" class="empty">
        <p>No estás inscrito en ningún curso aún.</p>
        <router-link to="/courses" class="btn-primary">Ver cursos disponibles</router-link>
      </div>

      <div v-else class="grid">
        <article v-for="curso in cursos" :key="curso.id" class="card">
          <div class="card-header">
            <h3>{{ curso.nombre }}</h3>
            <span class="badge">{{ curso.codigo }}</span>
          </div>
          <p class="descripcion">{{ curso.descripcion || 'Sin descripción' }}</p>
          <div class="info">
            <span><strong>Créditos:</strong> {{ curso.creditos }}</span>
            <span v-if="curso.horario"><strong>Horario:</strong> {{ curso.horario }}</span>
          </div>
          <div class="actions">
            <button class="btn-outline">Ver detalles</button>
          </div>
        </article>
      </div>
    </section>
  </transition>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { supabase } from '../supabase'

const cursos = ref([])
const loading = ref(true)
const error = ref(null)
const userInfo = ref(null)

const COURSES_API = import.meta.env.VITE_COURSES_API

async function fetchMyCourses() {
  try {
    loading.value = true
    error.value = null
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) {
      error.value = 'No estás autenticado'
      return
    }

    const response = await fetch(`${COURSES_API}/api/courses/my-courses`, {
      headers: {
        'Authorization': `Bearer ${session.access_token}`
      }
    })

    if (response.ok) {
      const data = await response.json()
      cursos.value = data.cursos || []
      userInfo.value = { usuario: data.usuario, rol: data.rol }
    } else {
      error.value = 'Error al cargar tus cursos'
    }
  } catch (e) {
    console.error('Error fetching my courses:', e)
    error.value = 'Error de conexión'
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  fetchMyCourses()
})
</script>

<style scoped>
.wrap { max-width: 1100px; margin: 2rem auto; padding: 0 1rem; animation: slideIn .7s; }
.subtitle { color: #666; margin-bottom: 1.5rem; }
.loading, .error, .empty { text-align: center; padding: 2rem; }
.error { color: #ef4444; }
.empty p { margin-bottom: 1rem; color: #666; }
.grid { display: grid; gap: 1rem; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); }
.card { background: #fff; border-radius: 12px; padding: 1.25rem; box-shadow: 0 2px 8px rgba(0,0,0,.06); transition: transform .18s ease, box-shadow .2s ease; }
.card:hover { transform: translateY(-4px); box-shadow: 0 10px 24px rgba(0,0,0,.08); }
.card-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: .75rem; }
.card h3 { color: #2a4dd0; margin: 0; }
.badge { background: #eef2ff; color: #2a4dd0; padding: .25rem .5rem; border-radius: 6px; font-size: .85rem; font-weight: 600; }
.descripcion { color: #666; margin: .5rem 0; }
.info { display: flex; flex-direction: column; gap: .25rem; font-size: .9rem; color: #555; margin-bottom: .75rem; }
.actions { display: flex; gap: .5rem; }
.btn-primary { background: #2a4dd0; color: #fff; border: none; padding: .6rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; text-decoration: none; display: inline-block; }
.btn-outline { background: #fff; color: #2a4dd0; border: 2px solid #2a4dd0; padding: .5rem .75rem; border-radius: 8px; font-weight: 600; cursor: pointer; font-size: .9rem; }

.fade-enter-active, .fade-leave-active { transition: opacity .4s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
@keyframes slideIn { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
</style>
