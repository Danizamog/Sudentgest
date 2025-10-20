<template>
  <div class="student-dashboard">
    <div class="welcome-card card">
      <h2>Bienvenido, {{ userName }} 👋</h2>
      <p>Aquí está un resumen de tu actividad académica</p>
    </div>

    <div class="dashboard-grid">
      <!-- My Courses Card -->
      <router-link to="/my-courses" class="dashboard-card card">
        <div class="card-icon">📚</div>
        <h3>Mis Cursos</h3>
        <p>Ver cursos inscritos</p>
        <div class="card-badge badge badge-primary">{{ coursesCount }} cursos</div>
      </router-link>

      <!-- Grades Card -->
      <router-link to="/student-grades" class="dashboard-card card">
        <div class="card-icon">📝</div>
        <h3>Mis Notas</h3>
        <p>Consultar calificaciones</p>
      </router-link>

      <!-- Attendance Card -->
      <router-link to="/attendance/history" class="dashboard-card card">
        <div class="card-icon">✅</div>
        <h3>Mi Asistencia</h3>
        <p>Historial de asistencias</p>
      </router-link>

      <!-- Excuses Card -->
      <router-link to="/excuses" class="dashboard-card card">
        <div class="card-icon">📄</div>
        <h3>Mis Excusas</h3>
        <p>Gestionar excusas</p>
      </router-link>

      <!-- Forum Card -->
      <router-link to="/foro" class="dashboard-card card">
        <div class="card-icon">💬</div>
        <h3>Foro</h3>
        <p>Participar en discusiones</p>
      </router-link>

      <!-- Assignments Card -->
      <div class="dashboard-card card">
        <div class="card-icon">📋</div>
        <h3>Tareas</h3>
        <p>Ver mis tareas</p>
        <div class="card-badge badge badge-warning">Próximamente</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'

const userName = ref('Estudiante')
const coursesCount = ref(0)

const getUserProfile = async () => {
  try {
    const response = await fetch('/auth/user-profile', {
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include'
    })

    if (response.ok) {
      const profile = await response.json()
      userName.value = profile.nombre_completo || profile.nombre || 'Estudiante'
    }
  } catch (error) {
    console.error('Error al obtener perfil:', error)
  }
}

const getCourses = async () => {
  try {
    const response = await fetch('/courses/my-courses', {
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include'
    })

    if (response.ok) {
      const data = await response.json()
      coursesCount.value = Array.isArray(data) ? data.length : 0
    }
  } catch (error) {
    console.error('Error al obtener cursos:', error)
  }
}

onMounted(() => {
  getUserProfile()
  getCourses()
})
</script>

<style scoped>
.student-dashboard {
  max-width: 1200px;
  margin: 0 auto;
}

.welcome-card {
  margin-bottom: var(--space-8);
  text-align: center;
  background: linear-gradient(135deg, var(--secondary) 0%, var(--accent) 100%);
  color: var(--white);
  padding: var(--space-8);
}

.welcome-card h2 {
  color: var(--white);
  font-size: var(--text-3xl);
  margin-bottom: var(--space-3);
}

.welcome-card p {
  font-size: var(--text-lg);
  color: rgba(255, 255, 255, 0.9);
  margin: 0;
}

.dashboard-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: var(--space-6);
}

.dashboard-card {
  text-decoration: none;
  color: inherit;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  padding: var(--space-8);
  transition: all var(--transition-base);
  cursor: pointer;
  position: relative;
}

.dashboard-card:hover {
  transform: translateY(-8px);
  box-shadow: var(--shadow-xl);
}

.card-icon {
  font-size: var(--text-6xl);
  margin-bottom: var(--space-4);
}

.dashboard-card h3 {
  font-size: var(--text-xl);
  font-weight: var(--font-semibold);
  color: var(--primary);
  margin-bottom: var(--space-2);
}

.dashboard-card p {
  font-size: var(--text-base);
  color: var(--gray-600);
  margin-bottom: var(--space-4);
}

.card-badge {
  margin-top: auto;
}

@media (max-width: 768px) {
  .dashboard-grid {
    grid-template-columns: 1fr;
  }
}
</style>
