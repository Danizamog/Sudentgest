<template>
  <div class="landing-layout">
    <!-- Navigation -->
    <nav class="landing-nav" :class="{ 'scrolled': isScrolled }">
      <div class="container">
        <div class="nav-content">
          <!-- Logo -->
          <router-link to="/" class="logo">
            <span class="logo-icon">🎓</span>
            <span class="logo-text">StudentGest</span>
          </router-link>

          <!-- Desktop Menu -->
          <div class="nav-links desktop-menu">
            <router-link to="/landing" class="nav-link">Inicio</router-link>
            <router-link to="/features" class="nav-link">Características</router-link>
            <router-link to="/pricing" class="nav-link">Precios</router-link>
            <router-link to="/nosotros" class="nav-link">Nosotros</router-link>
            <router-link to="/contact" class="nav-link">Contacto</router-link>
          </div>

          <!-- CTA Button -->
          <div class="nav-actions desktop-menu">
            <router-link to="/signin" class="btn btn-primary">
              Iniciar Sesión
            </router-link>
          </div>

          <!-- Mobile Menu Toggle -->
          <button class="mobile-menu-btn" @click="toggleMobileMenu" :class="{ 'active': mobileMenuOpen }">
            <span></span>
            <span></span>
            <span></span>
          </button>
        </div>
      </div>

      <!-- Mobile Menu -->
      <div class="mobile-menu" :class="{ 'open': mobileMenuOpen }">
        <div class="mobile-menu-content">
          <router-link to="/landing" class="mobile-nav-link" @click="closeMobileMenu">Inicio</router-link>
          <router-link to="/features" class="mobile-nav-link" @click="closeMobileMenu">Características</router-link>
          <router-link to="/pricing" class="mobile-nav-link" @click="closeMobileMenu">Precios</router-link>
          <router-link to="/nosotros" class="mobile-nav-link" @click="closeMobileMenu">Nosotros</router-link>
          <router-link to="/contact" class="mobile-nav-link" @click="closeMobileMenu">Contacto</router-link>
          <router-link to="/signin" class="btn btn-primary mobile-signin" @click="closeMobileMenu">
            Iniciar Sesión
          </router-link>
        </div>
      </div>
    </nav>

    <!-- Main Content -->
    <main class="landing-main">
      <router-view />
    </main>

    <!-- Footer -->
    <footer class="landing-footer">
      <div class="container">
        <div class="footer-content">
          <!-- Company Info -->
          <div class="footer-section">
            <div class="footer-logo">
              <span class="logo-icon">🎓</span>
              <span class="logo-text">StudentGest</span>
            </div>
            <p class="footer-description">
              Plataforma integral de gestión educativa para instituciones modernas.
            </p>
            <div class="social-links">
              <a href="#" class="social-link" aria-label="Facebook">📘</a>
              <a href="#" class="social-link" aria-label="Twitter">🐦</a>
              <a href="#" class="social-link" aria-label="LinkedIn">💼</a>
              <a href="#" class="social-link" aria-label="Instagram">📷</a>
            </div>
          </div>

          <!-- Quick Links -->
          <div class="footer-section">
            <h4 class="footer-title">Producto</h4>
            <ul class="footer-links">
              <li><router-link to="/features">Características</router-link></li>
              <li><router-link to="/pricing">Precios</router-link></li>
              <li><a href="#">Integraciones</a></li>
              <li><a href="#">API</a></li>
            </ul>
          </div>

          <!-- Company -->
          <div class="footer-section">
            <h4 class="footer-title">Empresa</h4>
            <ul class="footer-links">
              <li><router-link to="/nosotros">Nosotros</router-link></li>
              <li><router-link to="/contact">Contacto</router-link></li>
              <li><a href="#">Blog</a></li>
              <li><a href="#">Carreras</a></li>
            </ul>
          </div>

          <!-- Legal -->
          <div class="footer-section">
            <h4 class="footer-title">Legal</h4>
            <ul class="footer-links">
              <li><a href="#">Privacidad</a></li>
              <li><a href="#">Términos</a></li>
              <li><a href="#">Seguridad</a></li>
              <li><a href="#">Cookies</a></li>
            </ul>
          </div>
        </div>

        <!-- Copyright -->
        <div class="footer-bottom">
          <p>&copy; 2025 StudentGest. Todos los derechos reservados.</p>
        </div>
      </div>
    </footer>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'

const isScrolled = ref(false)
const mobileMenuOpen = ref(false)

const handleScroll = () => {
  isScrolled.value = window.scrollY > 20
}

const toggleMobileMenu = () => {
  mobileMenuOpen.value = !mobileMenuOpen.value
  if (mobileMenuOpen.value) {
    document.body.style.overflow = 'hidden'
  } else {
    document.body.style.overflow = ''
  }
}

const closeMobileMenu = () => {
  mobileMenuOpen.value = false
  document.body.style.overflow = ''
}

onMounted(() => {
  window.addEventListener('scroll', handleScroll)
})

onUnmounted(() => {
  window.removeEventListener('scroll', handleScroll)
  document.body.style.overflow = ''
})
</script>

<style scoped>
/* ===== Landing Layout ===== */
.landing-layout {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

/* ===== Navigation ===== */
.landing-nav {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: var(--z-sticky);
  background-color: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(10px);
  transition: all var(--transition-base);
  border-bottom: 1px solid transparent;
}

.landing-nav.scrolled {
  box-shadow: var(--shadow-md);
  border-bottom-color: var(--gray-200);
}

.nav-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--space-4) 0;
}

/* Logo */
.logo {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  font-size: var(--text-xl);
  font-weight: var(--font-bold);
  color: var(--primary);
  text-decoration: none;
  transition: opacity var(--transition-fast);
}

.logo:hover {
  opacity: 0.8;
}

.logo-icon {
  font-size: var(--text-3xl);
}

.logo-text {
  background: linear-gradient(135deg, var(--secondary) 0%, var(--accent) 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

/* Navigation Links */
.nav-links {
  display: flex;
  align-items: center;
  gap: var(--space-6);
}

.nav-link {
  font-size: var(--text-base);
  font-weight: var(--font-medium);
  color: var(--gray-700);
  text-decoration: none;
  transition: color var(--transition-fast);
  position: relative;
}

.nav-link:hover {
  color: var(--secondary);
}

.nav-link::after {
  content: '';
  position: absolute;
  bottom: -4px;
  left: 0;
  width: 0;
  height: 2px;
  background: linear-gradient(90deg, var(--secondary), var(--accent));
  transition: width var(--transition-base);
}

.nav-link:hover::after,
.nav-link.router-link-active::after {
  width: 100%;
}

/* Mobile Menu Button */
.mobile-menu-btn {
  display: none;
  flex-direction: column;
  gap: 4px;
  background: none;
  border: none;
  cursor: pointer;
  padding: var(--space-2);
}

.mobile-menu-btn span {
  display: block;
  width: 24px;
  height: 2px;
  background-color: var(--gray-700);
  transition: all var(--transition-base);
}

.mobile-menu-btn.active span:nth-child(1) {
  transform: rotate(45deg) translate(5px, 5px);
}

.mobile-menu-btn.active span:nth-child(2) {
  opacity: 0;
}

.mobile-menu-btn.active span:nth-child(3) {
  transform: rotate(-45deg) translate(7px, -7px);
}

/* Mobile Menu */
.mobile-menu {
  position: fixed;
  top: 73px;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: var(--white);
  transform: translateX(-100%);
  transition: transform var(--transition-base);
  overflow-y: auto;
  z-index: var(--z-fixed);
}

.mobile-menu.open {
  transform: translateX(0);
}

.mobile-menu-content {
  display: flex;
  flex-direction: column;
  padding: var(--space-6);
  gap: var(--space-4);
}

.mobile-nav-link {
  font-size: var(--text-lg);
  font-weight: var(--font-medium);
  color: var(--gray-700);
  text-decoration: none;
  padding: var(--space-4);
  border-radius: var(--radius-lg);
  transition: all var(--transition-fast);
}

.mobile-nav-link:hover,
.mobile-nav-link.router-link-active {
  background-color: var(--gray-100);
  color: var(--secondary);
}

.mobile-signin {
  margin-top: var(--space-4);
  width: 100%;
  justify-content: center;
}

/* ===== Main Content ===== */
.landing-main {
  flex: 1;
  margin-top: 73px; /* Height of navbar */
}

/* ===== Footer ===== */
.landing-footer {
  background-color: var(--gray-900);
  color: var(--gray-300);
  padding: var(--space-16) 0 var(--space-8);
}

.footer-content {
  display: grid;
  grid-template-columns: 2fr 1fr 1fr 1fr;
  gap: var(--space-12);
  margin-bottom: var(--space-12);
}

.footer-section h4 {
  color: var(--white);
  font-size: var(--text-lg);
  margin-bottom: var(--space-4);
}

.footer-logo {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  font-size: var(--text-xl);
  font-weight: var(--font-bold);
  color: var(--white);
  margin-bottom: var(--space-4);
}

.footer-description {
  color: var(--gray-400);
  line-height: var(--leading-relaxed);
  margin-bottom: var(--space-6);
}

.social-links {
  display: flex;
  gap: var(--space-3);
}

.social-link {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  background-color: var(--gray-800);
  border-radius: var(--radius-lg);
  font-size: var(--text-lg);
  transition: all var(--transition-base);
}

.social-link:hover {
  background-color: var(--secondary);
  transform: translateY(-2px);
}

.footer-links {
  list-style: none;
}

.footer-links li {
  margin-bottom: var(--space-3);
}

.footer-links a {
  color: var(--gray-400);
  transition: color var(--transition-fast);
}

.footer-links a:hover {
  color: var(--white);
}

.footer-title {
  color: var(--white);
  font-size: var(--text-base);
  font-weight: var(--font-semibold);
  margin-bottom: var(--space-4);
}

.footer-bottom {
  padding-top: var(--space-8);
  border-top: 1px solid var(--gray-800);
  text-align: center;
  color: var(--gray-500);
}

/* ===== Responsive ===== */
@media (max-width: 1024px) {
  .footer-content {
    grid-template-columns: 1fr 1fr;
  }
}

@media (max-width: 768px) {
  .desktop-menu {
    display: none;
  }

  .mobile-menu-btn {
    display: flex;
  }

  .footer-content {
    grid-template-columns: 1fr;
    gap: var(--space-8);
  }
}
</style>
