<template>
  <div id="app">
    <!-- Barra superior -->
    <header class="navbar">
      <div class="logo">SG StudentGest</div>
      <nav>
        <router-link to="/home">Inicio</router-link>
        <router-link to="/features">Características</router-link>
        <router-link to="/pricing">Precios</router-link>
        <router-link to="/info">Información</router-link>
        <router-link to="/nosotros">Nosotros</router-link>
        <router-link to="/contact">Contacto</router-link>
      </nav>
      <div class="actions">
        <button class="btn-outline" @click="$router.push('/signin')">Iniciar Sesión</button>
        <button class="btn-primary" @click="$router.push('/signup')">Registrarse</button>
      </div>
    </header>

    <!-- Transición de rutas -->
    <transition name="route" mode="out-in">
      <router-view />
    </transition>

    <AppFooter />
  </div>
</template>

<script>
import AppFooter from './components/AppFooter.vue'
export default {
  name: 'App',
  components: { AppFooter },
  mounted() {
    // Si entras por '/', fuerza Home como landing sin tocar los routers aquí.
    if (this.$route?.path === '/') {
      this.$router.replace('/home')
    }
  }
}
</script>

<style scoped>
#app {
  font-family: 'Segoe UI', Arial, sans-serif;
  background: #f8f9fb;
  color: #222;
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}
.navbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: #fff;
  padding: 1rem 2rem;
  box-shadow: 0 2px 8px rgba(0,0,0,0.04);
  flex-wrap: wrap;
  position: sticky;
  top: 0;
  z-index: 10;
}
.logo {
  font-weight: bold;
  font-size: 1.2rem;
  color: #2a4dd0;
}
.navbar nav {
  display: flex;
  gap: 1.5rem;
}
.navbar a, .navbar .router-link-active {
  color: #222;
  text-decoration: none;
  font-weight: 600; /* ligero énfasis para navegación principal */
}
.navbar .router-link-active {
  color: #2a4dd0;
  border-bottom: 2px solid #2a4dd0;
}
.actions {
  display: flex;
  gap: 0.5rem;
}
.btn-primary {
  background: #2a4dd0;
  color: #fff;
  border: none;
  padding: 0.5rem 1.2rem;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
  transition: transform .15s ease, box-shadow .2s;
}
.btn-primary:hover {
  transform: translateY(-1px);
  box-shadow: 0 6px 20px rgba(42,77,208,.2);
}
.btn-outline {
  background: #fff;
  color: #2a4dd0;
  border: 2px solid #2a4dd0;
  padding: 0.5rem 1.2rem;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
  transition: background .2s, color .2s;
}
.btn-outline:hover {
  background: #2a4dd0;
  color: #fff;
}
.footer {
  background: #222;
  color: #fff;
  text-align: center;
  padding: 1rem 0;
  margin-top: auto;
  font-size: 0.95rem;
}

/* Animación de cambio de ruta */
.route-enter-active, .route-leave-active {
  transition: opacity .25s ease, transform .25s ease;
}
.route-enter-from, .route-leave-to {
  opacity: 0;
  transform: translateY(8px);
}

/* Responsive */
@media (max-width: 600px) {
  .navbar {
    flex-direction: column;
    align-items: flex-start;
    padding: 1rem;
    gap: .5rem;
  }
}
/* Additional styles */
.hero {
  padding: 3rem 1rem 2rem 1rem;
}
.hero h1 {
  font-size: 2rem;
  margin-bottom: 1rem;
}
.hero p {
  font-size: 1.1rem;
  margin-bottom: 2rem;
}
.hero-actions {
  display: flex;
  gap: 1rem;
  justify-content: center;
  flex-wrap: wrap;
}
.features {
  padding: 2rem 1rem;
  max-width: 1100px;
  margin: 0 auto;
}
.features h2 {
  text-align: center;
  font-size: 1.5rem;
  margin-bottom: 0.5rem;
}
.features p {
  text-align: center;
  margin-bottom: 2rem;
}
.features-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1.5rem;
}
.feature-card {
  background: #fff;
  border-radius: 10px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.04);
  padding: 1.5rem;
  text-align: center;
}
.feature-card h3 {
  color: #2a4dd0;
  margin-bottom: 0.5rem;
  font-size: 1.1rem;
}
.footer {
  background: #222;
  color: #fff;
  text-align: center;
  padding: 1rem 0;
  margin-top: auto;
  font-size: 0.95rem;
}

/* Responsive */
@media (max-width: 900px) {
  .features-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}
@media (max-width: 600px) {
  .navbar {
    flex-direction: column;
    align-items: flex-start;
    padding: 1rem;
  }
  .features-grid {
    grid-template-columns: 1fr;
  }
  .hero h1 {
    font-size: 1.3rem;
  }
}
</style>
