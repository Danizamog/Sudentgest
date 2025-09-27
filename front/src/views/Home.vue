<template>
  <div>
    <h1>Dashboard</h1>
    <button @click="logout">Cerrar sesión</button>

    <div v-if="loading">Cargando estudiantes...</div>
    <div v-if="error" class="error">{{ error }}</div>
    <div v-if="message" class="info">{{ message }}</div>

    <ul v-if="students.length">
      <li v-for="s in students" :key="s.id">
        {{ s.first_name }} {{ s.last_name }} - {{ s.email }}
        <span v-if="s.subject">({{ s.subject }}: {{ s.grade }})</span>
      </li>
    </ul>
    <p v-else-if="!loading && !error">No hay estudiantes disponibles.</p>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'

const students = ref([])
const loading = ref(false)
const error = ref(null)
const message = ref(null)
const router = useRouter()

onMounted(async () => {
  loading.value = true
  error.value = null
  message.value = null

  const token = localStorage.getItem('token')
  if (!token) {
    error.value = 'No se encontró token. Inicia sesión.'
    router.push('/signin')
    return
  }

  try {
    const res = await fetch('http://localhost:5003/users', {
      headers: { Authorization: `Bearer ${token}` }
    })

    if (!res.ok) {
      const data = await res.json().catch(() => null)
      throw new Error(data?.error || 'Error al cargar estudiantes')
    }

    const data = await res.json()

    // Diferenciar entre mensaje y lista
    if (Array.isArray(data)) {
      students.value = data
      if (!data.length) message.value = "No hay estudiantes en este tenant."
    } else if (data.message) {
      message.value = data.message
    } else if (data.error) {
      error.value = data.error
    }
  } catch (e) {
    error.value = e.message
  } finally {
    loading.value = false
  }
})

function logout() {
  localStorage.removeItem('token')
  router.push('/signin')
}
</script>

<style scoped>
.error { color: #dc2626; font-weight: bold; margin-top: 1rem; }
.info { color: #2563eb; font-weight: bold; margin-top: 1rem; }
ul { margin-top: 1rem; padding-left: 0; list-style: none; }
li { padding: 0.5rem 0; border-bottom: 1px solid #ddd; }
button { margin-bottom: 1rem; padding: 0.5rem 1rem; background-color: #4f46e5; color: white; border: none; border-radius: 0.5rem; cursor: pointer; }
button:hover { background-color: #3730a3; }
</style>
