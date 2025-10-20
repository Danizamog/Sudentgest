<template>
  <div class="app-layout">
    <!-- Sidebar -->
    <aside class="app-sidebar" :class="{ 'collapsed': sidebarCollapsed }">
      <div class="sidebar-header">
        <div class="sidebar-logo">
          <span class="logo-icon">🎓</span>
          <transition name="fade">
            <span v-if="!sidebarCollapsed" class="logo-text">StudentGest</span>
          </transition>
        </div>
        <button class="sidebar-toggle" @click="toggleSidebar">
          <span>{{ sidebarCollapsed ? '→' : '←' }}</span>
        </button>
      </div>

      <nav class="sidebar-nav">
        <!-- Dashboard Section -->
        <div class="nav-section">
          <p v-if="!sidebarCollapsed" class="nav-section-title">Principal</p>
          <router-link 
            :to="dashboardRoute" 
            class="nav-item"
            :title="sidebarCollapsed ? 'Dashboard' : ''"
          >
            <span class="nav-icon">📊</span>
            <span v-if="!sidebarCollapsed" class="nav-text">Dashboard</span>
          </router-link>
        </div>

        <!-- Academic Section -->
        <div class="nav-section">
          <p v-if="!sidebarCollapsed" class="nav-section-title">Académico</p>
          <router-link 
            to="/courses" 
            class="nav-item"
            :title="sidebarCollapsed ? 'Cursos' : ''"
          >
            <span class="nav-icon">📚</span>
            <span v-if="!sidebarCollapsed" class="nav-text">Cursos</span>
          </router-link>
          <router-link 
            v-if="userRole === 'Estudiante' || userRole === 'Profesor'"
            to="/my-assignments" 
            class="nav-item"
            :title="sidebarCollapsed ? 'Tareas' : ''"
          >
            <span class="nav-icon">📋</span>
            <span v-if="!sidebarCollapsed" class="nav-text">{{ userRole === 'Profesor' ? 'Mis Tareas' : 'Tareas' }}</span>
          </router-link>
          <router-link 
            v-if="userRole === 'Estudiante'"
            to="/student-grades" 
            class="nav-item"
            :title="sidebarCollapsed ? 'Notas' : ''"
          >
            <span class="nav-icon">📝</span>
            <span v-if="!sidebarCollapsed" class="nav-text">Mis Notas</span>
          </router-link>
          <router-link 
            v-if="userRole === 'Profesor'"
            to="/teacher-grades" 
            class="nav-item"
            :title="sidebarCollapsed ? 'Calificar' : ''"
          >
            <span class="nav-icon">✏️</span>
            <span v-if="!sidebarCollapsed" class="nav-text">Calificar</span>
          </router-link>
        </div>

        <!-- Attendance Section -->
        <div class="nav-section">
          <p v-if="!sidebarCollapsed" class="nav-section-title">Asistencia</p>
          <router-link 
            v-if="userRole === 'Profesor' || userRole === 'Director'"
            to="/attendance" 
            class="nav-item"
            :title="sidebarCollapsed ? 'Asistencia' : ''"
          >
            <span class="nav-icon">✅</span>
            <span v-if="!sidebarCollapsed" class="nav-text">Asistencia</span>
          </router-link>
          <router-link 
            v-if="userRole === 'Estudiante'"
            to="/attendance/history" 
            class="nav-item"
            :title="sidebarCollapsed ? 'Mi Asistencia' : ''"
          >
            <span class="nav-icon">✅</span>
            <span v-if="!sidebarCollapsed" class="nav-text">Mi Asistencia</span>
          </router-link>
          <router-link 
            to="/excuses" 
            class="nav-item"
            :title="sidebarCollapsed ? 'Excusas' : ''"
          >
            <span class="nav-icon">📄</span>
            <span v-if="!sidebarCollapsed" class="nav-text">Excusas</span>
          </router-link>
          <router-link 
            v-if="userRole === 'Director'"
            to="/excuses/manage" 
            class="nav-item"
            :title="sidebarCollapsed ? 'Gestionar Excusas' : ''"
          >
            <span class="nav-icon">📋</span>
            <span v-if="!sidebarCollapsed" class="nav-text">Gestionar Excusas</span>
          </router-link>
        </div>

        <!-- Communication Section -->
        <div class="nav-section">
          <p v-if="!sidebarCollapsed" class="nav-section-title">Comunicación</p>
          <router-link 
            to="/foro" 
            class="nav-item"
            :title="sidebarCollapsed ? 'Foro' : ''"
          >
            <span class="nav-icon">💬</span>
            <span v-if="!sidebarCollapsed" class="nav-text">Foro</span>
          </router-link>
          <router-link 
            to="/support" 
            class="nav-item"
            :title="sidebarCollapsed ? 'Contacto' : ''"
          >
            <span class="nav-icon">📧</span>
            <span v-if="!sidebarCollapsed" class="nav-text">Contacto</span>
          </router-link>
        </div>

        <!-- Reports Section -->
        <div v-if="userRole === 'Profesor' || userRole === 'Director'" class="nav-section">
          <p v-if="!sidebarCollapsed" class="nav-section-title">Reportes</p>
          <router-link 
            v-if="userRole === 'Profesor'"
            to="/teacher-reports" 
            class="nav-item"
            :title="sidebarCollapsed ? 'Reportes' : ''"
          >
            <span class="nav-icon">📈</span>
            <span v-if="!sidebarCollapsed" class="nav-text">Reportes</span>
          </router-link>
          <router-link 
            v-if="userRole === 'Director'"
            to="/director-reports" 
            class="nav-item"
            :title="sidebarCollapsed ? 'Reportes' : ''"
          >
            <span class="nav-icon">📊</span>
            <span v-if="!sidebarCollapsed" class="nav-text">Reportes</span>
          </router-link>
        </div>

        <!-- Director Section -->
        <div v-if="userRole === 'Director'" class="nav-section">
          <p v-if="!sidebarCollapsed" class="nav-section-title">Administración</p>
          <router-link 
            to="/director" 
            class="nav-item"
            :title="sidebarCollapsed ? 'Panel Director' : ''"
          >
            <span class="nav-icon">⚙️</span>
            <span v-if="!sidebarCollapsed" class="nav-text">Panel Director</span>
          </router-link>
          <router-link 
            to="/base" 
            class="nav-item"
            :title="sidebarCollapsed ? 'Base de Datos' : ''"
          >
            <span class="nav-icon">💾</span>
            <span v-if="!sidebarCollapsed" class="nav-text">Base de Datos</span>
          </router-link>
        </div>
      </nav>

      <div class="sidebar-footer">
        <div class="user-info" :class="{ 'collapsed': sidebarCollapsed }">
          <div class="user-avatar">{{ userInitials }}</div>
          <div v-if="!sidebarCollapsed" class="user-details">
            <p class="user-name">{{ userName }}</p>
            <p class="user-role">{{ userRole }}</p>
          </div>
        </div>
        <button class="logout-btn" @click="handleLogout" :title="sidebarCollapsed ? 'Cerrar Sesión' : ''">
          <span class="nav-icon">🚪</span>
          <span v-if="!sidebarCollapsed" class="nav-text">Cerrar Sesión</span>
        </button>
      </div>
    </aside>

    <!-- Main Content Area -->
    <div class="app-main" :class="{ 'sidebar-collapsed': sidebarCollapsed }">
      <!-- Top Bar -->
      <header class="app-header">
        <div class="header-content">
          <h1 class="page-title">{{ pageTitle }}</h1>
          <div class="header-actions">
            <!-- Add notifications, user menu, etc. here -->
          </div>
        </div>
      </header>

      <!-- Content -->
      <main class="app-content">
        <router-view />
      </main>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { supabase } from '../supabase'

const router = useRouter()
const route = useRoute()

const sidebarCollapsed = ref(false)
const userRole = ref('Estudiante')
const userName = ref('Usuario')
const userEmail = ref('')

// Computed properties
const userInitials = computed(() => {
  return userName.value
    .split(' ')
    .map(n => n[0])
    .join('')
    .toUpperCase()
    .substring(0, 2)
})

const dashboardRoute = computed(() => {
  if (userRole.value === 'Director') return '/director-dashboard'
  if (userRole.value === 'Profesor') return '/teacher-dashboard'
  return '/home'
})

const pageTitle = computed(() => {
  const titles = {
    '/home': 'Inicio',
    '/courses': 'Cursos',
    '/my-courses': 'Mis Cursos',
    '/foro': 'Foro',
    '/attendance': 'Asistencia',
    '/excuses': 'Excusas',
    '/excuses/manage': 'Gestionar Excusas',
    '/student-grades': 'Mis Notas',
    '/teacher-grades': 'Calificar Estudiantes',
    '/teacher-reports': 'Reportes Académicos',
    '/director-reports': 'Reportes Institucionales',
    '/teacher-dashboard': 'Dashboard Profesor',
    '/director-dashboard': 'Dashboard Director',
    '/director': 'Panel de Director',
    '/base': 'Base de Datos'
  }
  return titles[route.path] || 'StudentGest'
})

// Methods
const toggleSidebar = () => {
  sidebarCollapsed.value = !sidebarCollapsed.value
  localStorage.setItem('sidebarCollapsed', sidebarCollapsed.value)
}

const getUserProfile = async () => {
  try {
    const response = await fetch('/auth/user-profile', {
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include'
    })

    if (response.ok) {
      const profile = await response.json()
      userName.value = profile.nombre_completo || profile.nombre || 'Usuario'
      userEmail.value = profile.email || ''
      userRole.value = profile.rol || 'Estudiante'
    }
  } catch (error) {
    console.error('Error al obtener perfil:', error)
  }
}

const handleLogout = async () => {
  try {
    await supabase.auth.signOut()
    localStorage.removeItem('token')
    localStorage.removeItem('user_id')
    router.push('/signin')
  } catch (error) {
    console.error('Error al cerrar sesión:', error)
  }
}

// Load user profile on mount
getUserProfile()

// Load sidebar state from localStorage
const savedState = localStorage.getItem('sidebarCollapsed')
if (savedState !== null) {
  sidebarCollapsed.value = savedState === 'true'
}
</script>

<style scoped>
/* ===== App Layout ===== */
.app-layout {
  display: flex;
  min-height: 100vh;
  background-color: var(--gray-50);
}

/* ===== Sidebar ===== */
.app-sidebar {
  width: 280px;
  background-color: var(--white);
  border-right: 1px solid var(--gray-200);
  display: flex;
  flex-direction: column;
  transition: width var(--transition-base);
  position: relative;
  z-index: var(--z-sticky);
}

.app-sidebar.collapsed {
  width: 80px;
}

.sidebar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--space-6) var(--space-4);
  border-bottom: 1px solid var(--gray-200);
}

.sidebar-logo {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  font-weight: var(--font-bold);
  color: var(--primary);
}

.logo-icon {
  font-size: var(--text-2xl);
}

.logo-text {
  font-size: var(--text-lg);
  background: linear-gradient(135deg, var(--secondary) 0%, var(--accent) 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.sidebar-toggle {
  background: none;
  border: none;
  cursor: pointer;
  padding: var(--space-2);
  color: var(--gray-600);
  font-size: var(--text-lg);
  transition: color var(--transition-fast);
}

.sidebar-toggle:hover {
  color: var(--secondary);
}

/* Sidebar Navigation */
.sidebar-nav {
  flex: 1;
  overflow-y: auto;
  padding: var(--space-4);
}

.nav-section {
  margin-bottom: var(--space-6);
}

.nav-section-title {
  font-size: var(--text-xs);
  font-weight: var(--font-semibold);
  color: var(--gray-500);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: var(--space-3);
  padding: 0 var(--space-3);
}

.nav-item {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-3);
  border-radius: var(--radius-lg);
  color: var(--gray-700);
  text-decoration: none;
  transition: all var(--transition-fast);
  margin-bottom: var(--space-2);
}

.nav-item:hover {
  background-color: var(--gray-100);
  color: var(--secondary);
}

.nav-item.router-link-active {
  background: linear-gradient(135deg, var(--secondary) 0%, var(--secondary-dark) 100%);
  color: var(--white);
}

.nav-icon {
  font-size: var(--text-xl);
  flex-shrink: 0;
}

.nav-text {
  font-size: var(--text-sm);
  font-weight: var(--font-medium);
  white-space: nowrap;
}

/* Sidebar Footer */
.sidebar-footer {
  padding: var(--space-4);
  border-top: 1px solid var(--gray-200);
}

.user-info {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-3);
  margin-bottom: var(--space-2);
}

.user-info.collapsed {
  justify-content: center;
}

.user-avatar {
  width: 40px;
  height: 40px;
  border-radius: var(--radius-full);
  background: linear-gradient(135deg, var(--secondary) 0%, var(--accent) 100%);
  color: var(--white);
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: var(--font-bold);
  font-size: var(--text-sm);
  flex-shrink: 0;
}

.user-details {
  overflow: hidden;
}

.user-name {
  font-size: var(--text-sm);
  font-weight: var(--font-semibold);
  color: var(--primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  margin-bottom: var(--space-1);
}

.user-role {
  font-size: var(--text-xs);
  color: var(--gray-600);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.logout-btn {
  width: 100%;
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-3);
  border: none;
  border-radius: var(--radius-lg);
  background-color: transparent;
  color: var(--error);
  font-size: var(--text-sm);
  font-weight: var(--font-medium);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.logout-btn:hover {
  background-color: var(--error);
  color: var(--white);
}

/* ===== Main Content ===== */
.app-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  margin-left: 0;
  transition: margin-left var(--transition-base);
}

/* App Header */
.app-header {
  background-color: var(--white);
  border-bottom: 1px solid var(--gray-200);
  padding: var(--space-6) var(--space-8);
}

.header-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.page-title {
  font-size: var(--text-3xl);
  font-weight: var(--font-bold);
  color: var(--primary);
  margin: 0;
}

/* App Content */
.app-content {
  flex: 1;
  overflow-y: auto;
  padding: var(--space-8);
}

/* Transitions */
.fade-enter-active,
.fade-leave-active {
  transition: opacity var(--transition-base);
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

/* ===== Responsive ===== */
@media (max-width: 768px) {
  .app-sidebar {
    position: fixed;
    left: 0;
    top: 0;
    bottom: 0;
    transform: translateX(-100%);
    z-index: var(--z-modal);
  }

  .app-sidebar:not(.collapsed) {
    transform: translateX(0);
  }

  .app-main {
    margin-left: 0 !important;
  }

  .app-content {
    padding: var(--space-4);
  }

  .page-title {
    font-size: var(--text-2xl);
  }
}
</style>
