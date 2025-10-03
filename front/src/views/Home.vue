<template>
  <div class="home-layout">
    <!-- Barra lateral -->
    <aside class="sidebar" :class="{ 'sidebar-collapsed': isSidebarCollapsed }">
      <div class="sidebar-header">
        <h3>StudentGest</h3>
        <p>Acceso rápido</p>
        <button class="toggle-sidebar" @click="toggleSidebar">
          {{ isSidebarCollapsed ? '→' : '←' }}
        </button>
      </div>
      <nav class="sidebar-nav">
        <router-link to="/features" class="nav-item">
          <div class="nav-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
              <path d="M8 0a8 8 0 1 1 0 16A8 8 0 0 1 8 0zM4.5 7.5a.5.5 0 0 0 0 1h5.793l-2.147 2.146a.5.5 0 0 0 .708.708l3-3a.5.5 0 0 0 0-.708l-3-3a.5.5 0 1 0-.708.708L10.293 7.5H4.5z"/>
            </svg>
          </div>
          <div class="nav-content" v-if="!isSidebarCollapsed">
            <strong>Features</strong>
            <span>Características principales</span>
          </div>
        </router-link>
        
        <router-link to="/info" class="nav-item">
          <div class="nav-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
              <path d="M8 16A8 8 0 1 0 8 0a8 8 0 0 0 0 16zm.93-9.412-1 4.705c-.07.34.029.533.304.533.194 0 .487-.07.686-.246l-.088.416c-.287.346-.92.598-1.465.598-.703 0-1.002-.422-.808-1.319l.738-3.468c.064-.293.006-.399-.287-.47l-.451-.081.082-.381 2.29-.287zM8 5.5a1 1 0 1 1 0-2 1 1 0 0 1 0 2z"/>
            </svg>
          </div>
          <div class="nav-content" v-if="!isSidebarCollapsed">
            <strong>Info</strong>
            <span>Información detallada</span>
          </div>
        </router-link>
        
        <router-link to="/contact" class="nav-item">
          <div class="nav-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
              <path d="M0 4a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V4zm2-1a1 1 0 0 0-1 1v.217l7 4.2 7-4.2V4a1 1 0 0 0-1-1H2zm13 2.383-4.708 2.825L15 11.105V5.383zm-.034 6.876-5.64-3.471L8 9.583l-1.326-.795-5.64 3.47A1 1 0 0 0 2 13h12a1 1 0 0 0 .966-.741zM1 11.105l4.708-2.897L1 5.383v5.722z"/>
            </svg>
          </div>
          <div class="nav-content" v-if="!isSidebarCollapsed">
            <strong>Contact</strong>
            <span>Contáctanos</span>
          </div>
        </router-link>

        <router-link to="/foro" class="nav-item">
          <div class="nav-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
              <path d="M14 1a1 1 0 0 1 1 1v8a1 1 0 0 1-1 1h-2.5a2 2 0 0 0-1.6.8L8 14.333 6.1 11.8a2 2 0 0 0-1.6-.8H2a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1h12zM2 0a2 2 0 0 0-2 2v8a2 2 0 0 0 2 2h2.5a1 1 0 0 1 .8.4l1.9 2.533a1 1 0 0 0 1.6 0l1.9-2.533a1 1 0 0 1 .8-.4H14a2 2 0 0 0 2-2V2a2 2 0 0 0-2-2H2z"/>
            </svg>
          </div>
          <div class="nav-content" v-if="!isSidebarCollapsed">
            <strong>Foro</strong>
            <span>Comunidad</span>
          </div>
        </router-link>

        <router-link to="/pricing" class="nav-item">
          <div class="nav-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
              <path d="M12.136.326A1.5 1.5 0 0 1 14 1.78V3h.5A1.5 1.5 0 0 1 16 4.5v9a1.5 1.5 0 0 1-1.5 1.5h-13A1.5 1.5 0 0 1 0 13.5v-9a1.5 1.5 0 0 1 1.432-1.499L12.136.326zM5.562 3H13V1.78a.5.5 0 0 0-.621-.484L5.562 3zM1.5 4a.5.5 0 0 0-.5.5v9a.5.5 0 0 0 .5.5h13a.5.5 0 0 0 .5-.5v-9a.5.5 0 0 0-.5-.5h-13z"/>
            </svg>
          </div>
          <div class="nav-content" v-if="!isSidebarCollapsed">
            <strong>Pricing</strong>
            <span>Planes y precios</span>
          </div>
        </router-link>
      </nav>
    </aside>

    <!-- Contenido principal -->
    <main class="main-content" :class="{ 'expanded': isSidebarCollapsed }">
      <!-- Hero de inicio -->
      <section class="home-hero">
        <div class="hero-content">
          <h1>Moderniza la gestión académica de tu institución</h1>
          <p>Asistencias, calificaciones, reportes y comunicación en una sola plataforma.</p>
          <div class="hero-actions">
            <router-link to="/features" class="btn-outline">Ver características</router-link>
          </div>
        </div>
      </section>

      <!-- Contadores estáticos -->
      <section class="stats">
        <div class="stat">
          <strong>250+</strong>
          <span>Instituciones</span>
        </div>
        <div class="stat">
          <strong>120k</strong>
          <span>Estudiantes</span>
        </div>
        <div class="stat">
          <strong>99.95%</strong>
          <span>Uptime</span>
        </div>
        <div class="stat">
          <strong>4.8/5</strong>
          <span>Satisfacción</span>
        </div>
      </section>

      <!-- Sobre SG -->
      <div class="info-negocio">
        <h2>Sobre StudentGest</h2>
        <p>
          StudentGest es una plataforma SaaS para la gestión académica, diseñada para instituciones educativas que buscan eficiencia, seguridad y comunicación efectiva entre padres, profesores y estudiantes.
        </p>
        <p>
          Nuestra plataforma ha sido desarrollada con las últimas tecnologías para garantizar un rendimiento óptimo y una experiencia de usuario excepcional. Con más de 5 años en el mercado, hemos perfeccionado cada aspecto de nuestro sistema para adaptarnos a las necesidades cambiantes del sector educativo.
        </p>
        <ul>
          <li>Gestión de asistencias y calificaciones</li>
          <li>Foros de comunicación y reportes detallados</li>
          <li>Acceso seguro desde cualquier dispositivo</li>
          <li>Escalable para miles de estudiantes</li>
          <li>Integración con sistemas existentes</li>
          <li>Soporte técnico 24/7</li>
        </ul>
      </div>

      <!-- Galería (imágenes) -->
      <section class="gallery">
        <div class="gallery-item">
          <img src="https://images.unsplash.com/photo-1523580846011-d3a5bc25702b?q=80&w=800&auto=format&fit=crop" alt="Aula y aprendizaje" />
          <div class="gallery-caption">Aulas modernas equipadas con tecnología</div>
        </div>
        <div class="gallery-item">
          <img src="https://images.unsplash.com/photo-1529101091764-c3526daf38fe?q=80&w=800&auto=format&fit=crop" alt="Trabajo en equipo" />
          <div class="gallery-caption">Colaboración entre estudiantes y profesores</div>
        </div>
        <div class="gallery-item">
          <img src="https://images.unsplash.com/photo-1509062522246-3755977927d7?q=80&w=800&auto=format&fit=crop" alt="Tecnología educativa" />
          <div class="gallery-caption">Tecnología al servicio de la educación</div>
        </div>
        <div class="gallery-item">
          <img src="https://images.unsplash.com/photo-1581094794329-c8112a89af12?q=80&w=800&auto=format&fit=crop" alt="Laboratorio" />
          <div class="gallery-caption">Laboratorios equipados para prácticas</div>
        </div>
        <div class="gallery-item">
          <img src="https://images.unsplash.com/photo-1588072432836-e10032774350?q=80&w=800&auto=format&fit=crop" alt="Biblioteca" />
          <div class="gallery-caption">Espacios de estudio y bibliotecas digitales</div>
        </div>
        <div class="gallery-item">
          <img src="https://images.unsplash.com/photo-1516321318423-f06f85e504b3?q=80&w=800&auto=format&fit=crop" alt="Deportes" />
          <div class="gallery-caption">Actividades extracurriculares y deportivas</div>
        </div>
      </section>

      <!-- Beneficios adicionales -->
      <section class="benefits">
        <h2>Beneficios de StudentGest</h2>
        <div class="benefits-grid">
          <div class="benefit-card">
            <h3>Eficiencia Operativa</h3>
            <p>Automatiza procesos administrativos repetitivos, reduciendo el tiempo dedicado a tareas manuales en un 70%.</p>
          </div>
          <div class="benefit-card">
            <h3>Comunicación Efectiva</h3>
            <p>Conecta a profesores, estudiantes y padres en tiempo real, facilitando el seguimiento académico continuo.</p>
          </div>
          <div class="benefit-card">
            <h3>Análisis de Datos</h3>
            <p>Obtén insights valiosos sobre el rendimiento estudiantil y toma decisiones basadas en datos.</p>
          </div>
          <div class="benefit-card">
            <h3>Seguridad Garantizada</h3>
            <p>Protege la información sensible con encriptación de grado empresarial y backups automáticos.</p>
          </div>
        </div>
      </section>

      <!-- Testimonios -->
      <section class="testimonials">
        <h2>Lo que dicen nuestros clientes</h2>
        <div class="testimonials-grid">
          <article class="tcard">
            <p>"StudentGest nos permitió optimizar procesos y mejorar la comunicación con los padres. La implementación fue sencilla y el equipo de soporte estuvo disponible en todo momento."</p>
            <span>- Directora, UCB</span>
          </article>
          <article class="tcard">
            <p>"Los reportes son claros y el control de asistencias es inmediato. Hemos reducido el tiempo de gestión administrativa en más de un 60%, permitiéndonos enfocarnos en lo que realmente importa: la educación."</p>
            <span>- Coordinador Académico, UPB</span>
          </article>
          <article class="tcard">
            <p>"Escalable y seguro, ideal para nuestra red de colegios. La plataforma se adaptó perfectamente a nuestras necesidades específicas y ha sido fundamental en nuestro proceso de transformación digital."</p>
            <span>- CTO, Red Educativa</span>
          </article>
        </div>
      </section>

      <!-- Aliados/Partners -->
      <section class="partners">
        <h2>Nuestros Aliados Tecnológicos</h2>
        <div class="partners-grid">
          <span class="badge">Google Workspace</span>
          <span class="badge">Microsoft 365</span>
          <span class="badge">Supabase</span>
          <span class="badge">Azure</span>
          <span class="badge">AWS</span>
          <span class="badge">Slack</span>
        </div>
      </section>

      <!-- Llamado a la acción -->
      <section class="cta">
        <h3>¿Listo para modernizar tu institución?</h3>
        <div class="cta-actions">
          <router-link to="/contact" class="btn-outline">Hablar con ventas</router-link>
        </div>
      </section>
    </main>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'

// Estado reactivo para la barra lateral
const isSidebarCollapsed = ref(false)

// Función para alternar la barra lateral
const toggleSidebar = () => {
  isSidebarCollapsed.value = !isSidebarCollapsed.value
}

// Detectar cambios en el tamaño de la ventana
const handleResize = () => {
  if (window.innerWidth < 900) {
    isSidebarCollapsed.value = true
  } else {
    isSidebarCollapsed.value = false
  }
}

// Configurar listeners de eventos
onMounted(() => {
  window.addEventListener('resize', handleResize)
  handleResize() // Verificar el tamaño inicial
})

onUnmounted(() => {
  window.removeEventListener('resize', handleResize)
})
</script>

<style scoped>
.home-layout {
  display: grid;
  grid-template-columns: 280px 1fr;
  min-height: 100vh;
  gap: 0;
  transition: grid-template-columns 0.3s ease;
}

.home-layout.sidebar-collapsed {
  grid-template-columns: 70px 1fr;
}

/* Barra lateral */
.sidebar {
  background: linear-gradient(135deg, #2a4dd0 0%, #1e40af 100%);
  color: white;
  padding: 1.5rem 1rem;
  height: 100vh;
  position: sticky;
  top: 0;
  overflow-y: auto;
  transition: all 0.3s ease;
  box-shadow: 2px 0 10px rgba(0, 0, 0, 0.1);
}

.sidebar-collapsed {
  width: 70px;
  padding: 1.5rem 0.5rem;
}

.sidebar-header {
  text-align: center;
  margin-bottom: 2rem;
  padding-bottom: 1.5rem;
  border-bottom: 1px solid rgba(255,255,255,0.2);
  position: relative;
}

.sidebar-collapsed .sidebar-header {
  padding-bottom: 1rem;
}

.sidebar-header h3 {
  font-size: 1.3rem;
  font-weight: 700;
  margin-bottom: 0.5rem;
  transition: opacity 0.3s ease;
}

.sidebar-collapsed .sidebar-header h3 {
  font-size: 1rem;
}

.sidebar-header p {
  opacity: 0.8;
  font-size: 0.8rem;
  transition: opacity 0.3s ease;
}

.sidebar-collapsed .sidebar-header p {
  opacity: 0;
  height: 0;
  margin: 0;
}

.toggle-sidebar {
  position: absolute;
  top: 0;
  right: 0;
  background: rgba(255, 255, 255, 0.2);
  border: none;
  color: white;
  width: 24px;
  height: 24px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  font-size: 0.8rem;
  transition: background 0.3s ease;
}

.toggle-sidebar:hover {
  background: rgba(255, 255, 255, 0.3);
}

.sidebar-nav {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.8rem;
  border-radius: 10px;
  text-decoration: none;
  color: white;
  transition: all 0.3s ease;
  border: 1px solid transparent;
  position: relative;
}

.sidebar-collapsed .nav-item {
  padding: 0.8rem;
  justify-content: center;
}

.nav-item:hover {
  background: rgba(255,255,255,0.1);
  border-color: rgba(255,255,255,0.3);
  transform: translateX(3px);
}

.nav-item.router-link-active {
  background: rgba(255,255,255,0.15);
  border-color: rgba(255,255,255,0.4);
}

.nav-icon {
  width: 24px;
  height: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(255,255,255,0.1);
  border-radius: 8px;
  flex-shrink: 0;
}

.nav-icon svg {
  width: 16px;
  height: 16px;
}

.nav-content {
  flex: 1;
  transition: opacity 0.3s ease;
}

.sidebar-collapsed .nav-content {
  opacity: 0;
  width: 0;
  height: 0;
  overflow: hidden;
}

.nav-content strong {
  display: block;
  font-weight: 600;
  margin-bottom: 0.2rem;
  font-size: 0.9rem;
}

.nav-content span {
  font-size: 0.75rem;
  opacity: 0.8;
}

/* Contenido principal */
.main-content {
  padding: 2rem;
  background: #f8fafc;
  overflow-y: auto;
  transition: padding 0.3s ease;
}

.main-content.expanded {
  padding-left: 1rem;
}

/* Hero */
.home-hero {
  position: relative;
  border-radius: 16px;
  padding: 3rem 1rem;
  margin: 0 auto 2rem;
  max-width: 100%;
  overflow: hidden;
  background:
    linear-gradient(135deg, rgba(42,77,208,0.95), rgba(59,130,246,0.9)),
    url('https://images.unsplash.com/photo-1523580846011-d3a5bc25702b?q=80&w=1600&auto=format&fit=crop');
  background-size: cover;
  background-position: center;
  color: #fff;
  box-shadow: 0 12px 30px rgba(0,0,0,.12);
}
.hero-content { 
  max-width: 760px; 
  margin: 0 auto; 
  text-align: center; 
}
.hero-content h1 { 
  font-size: 2rem; 
  margin-bottom: .5rem; 
}
.hero-content p { 
  opacity: .95; 
  margin-bottom: 1rem;
}
.hero-actions { 
  margin-top: 1rem; 
  display:flex; 
  gap:.75rem; 
  justify-content:center; 
  flex-wrap: wrap; 
}
.hero-actions .btn-outline { 
  text-decoration:none; 
  padding:.6rem 1rem; 
  border-radius:8px; 
  font-weight:600; 
  background:transparent; 
  color:#fff; 
  border:2px solid #fff; 
  transition: all 0.3s ease;
}
.hero-actions .btn-outline:hover {
  background: rgba(255, 255, 255, 0.1);
}

/* Info */
.info-negocio {
  margin: 2rem auto;
  max-width: 100%;
  background: #fff;
  border-radius: 1rem;
  box-shadow: 0 2px 8px rgba(0,0,0,0.07);
  padding: 1.5rem;
  text-align: left;
}
.info-negocio h2 { 
  color: #2a4dd0; 
  margin-bottom: 0.5rem; 
}
.info-negocio p {
  margin-bottom: 1rem;
  line-height: 1.6;
}
.info-negocio ul { 
  margin-top: 1rem; 
  padding-left: 1.2rem; 
  columns: 2;
}
.info-negocio li { 
  margin-bottom: 0.5rem; 
}

/* Stats */
.stats {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 1rem;
  max-width: 100%;
  margin: 1.5rem auto 2rem;
}
.stat {
  background: #fff;
  border-radius: 14px;
  padding: 1rem;
  text-align:center;
  box-shadow: 0 2px 10px rgba(0,0,0,.06);
  transition: transform 0.3s ease;
}
.stat:hover {
  transform: translateY(-5px);
}
.stat strong { 
  display:block; 
  font-size: 1.8rem; 
  line-height:1; 
  color:#2a4dd0; 
}
.stat span { 
  color:#555; 
}

/* Gallery */
.gallery {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1rem;
  max-width: 100%;
  margin: 2rem auto;
}
.gallery-item {
  position: relative;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(0,0,0,.06);
}
.gallery img {
  width: 100%;
  height: 220px;
  object-fit: cover;
  transition: transform 0.3s ease;
}
.gallery img:hover {
  transform: scale(1.05);
}
.gallery-caption {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  background: rgba(0, 0, 0, 0.7);
  color: white;
  padding: 0.5rem;
  font-size: 0.8rem;
  text-align: center;
}

/* Benefits */
.benefits {
  margin: 3rem auto;
  max-width: 100%;
}
.benefits h2 {
  text-align: center;
  color: #2a4dd0;
  margin-bottom: 2rem;
}
.benefits-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1.5rem;
}
.benefit-card {
  background: #fff;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 2px 8px rgba(0,0,0,.06);
  transition: transform 0.3s ease;
}
.benefit-card:hover {
  transform: translateY(-5px);
}
.benefit-card h3 {
  color: #2a4dd0;
  margin-bottom: 0.5rem;
}
.benefit-card p {
  color: #555;
  line-height: 1.5;
}

/* Testimonials */
.testimonials {
  margin: 3rem auto;
  max-width: 100%;
}
.testimonials h2 {
  text-align: center;
  color: #2a4dd0;
  margin-bottom: 2rem;
}
.testimonials-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1rem;
}
.tcard {
  background: #fff;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 2px 8px rgba(0,0,0,.06);
  transition: transform 0.3s ease;
}
.tcard:hover {
  transform: translateY(-5px);
}
.tcard p { 
  margin: 0 0 .5rem; 
  font-style: italic;
}
.tcard span { 
  color: #666; 
  font-weight: 600;
}

/* Partners */
.partners {
  margin: 3rem auto;
  max-width: 100%;
  text-align: center;
}
.partners h2 {
  color: #2a4dd0;
  margin-bottom: 1.5rem;
}
.partners-grid {
  display: flex;
  flex-wrap: wrap;
  gap: .75rem;
  justify-content: center;
}
.badge {
  background: #eef2ff;
  color: #2a4dd0;
  border: 1px solid #c7d2fe;
  padding: .5rem 1rem;
  border-radius: 999px;
  font-weight: 600;
  transition: all 0.3s ease;
}
.badge:hover {
  background: #2a4dd0;
  color: white;
  transform: scale(1.05);
}

/* CTA */
.cta {
  text-align: center;
  margin: 2rem auto;
  padding: 1.5rem;
  background: #ffffff;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0,0,0,.06);
  max-width: 100%;
}
.cta h3 { 
  margin-bottom: .75rem; 
}
.cta-actions { 
  display: flex; 
  gap: .75rem; 
  justify-content: center; 
}
.cta .btn-outline { 
  text-decoration: none; 
  padding: .55rem 1rem; 
  border-radius: 8px; 
  font-weight: 600; 
  background: #fff; 
  color: #2a4dd0; 
  border: 2px solid #2a4dd0; 
  transition: all 0.3s ease;
}
.cta .btn-outline:hover {
  background: #2a4dd0;
  color: #fff;
}

/* Responsive */
@media (max-width: 900px) {
  .home-layout {
    grid-template-columns: 1fr;
  }
  
  .sidebar {
    height: auto;
    position: relative;
    padding: 1rem;
  }
  
  .sidebar-nav {
    flex-direction: row;
    overflow-x: auto;
    padding-bottom: 1rem;
  }
  
  .nav-item {
    min-width: 200px;
  }
  
  .stats { 
    grid-template-columns: repeat(2, 1fr); 
  }
  .testimonials-grid { 
    grid-template-columns: repeat(2, 1fr); 
  }
  .gallery { 
    grid-template-columns: repeat(2, 1fr); 
  }
  .benefits-grid {
    grid-template-columns: 1fr;
  }
  .info-negocio ul {
    columns: 1;
  }
}

@media (max-width: 600px) {
  .home-hero { 
    padding: 2rem 1rem; 
  }
  .hero-content h1 { 
    font-size: 1.5rem; 
  }
  .stats, 
  .testimonials-grid, 
  .gallery { 
    grid-template-columns: 1fr; 
  }
  .main-content { 
    padding: 1rem; 
  }
  .benefits-grid {
    grid-template-columns: 1fr;
  }
}
</style>