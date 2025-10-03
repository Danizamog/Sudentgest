<template>
  <div class="home-layout">
    <!-- Barra lateral -->
    <aside class="sidebar">
      <div class="sidebar-header">
        <h3>StudentGest</h3>
        <p>Acceso rápido</p>
      </div>
      <nav class="sidebar-nav">
        <router-link to="/features" class="nav-item">
          <div class="nav-icon">🚀</div>
          <div class="nav-content">
            <strong>Features</strong>
            <span>Características principales</span>
          </div>
        </router-link>
        
        <router-link to="/info" class="nav-item">
          <div class="nav-icon">ℹ️</div>
          <div class="nav-content">
            <strong>Info</strong>
            <span>Información detallada</span>
          </div>
        </router-link>
        
        <router-link to="/contact" class="nav-item">
          <div class="nav-icon">📞</div>
          <div class="nav-content">
            <strong>Contact</strong>
            <span>Contáctanos</span>
          </div>
        </router-link>

        <router-link to="/foro" class="nav-item">
          <div class="nav-icon">💬</div>
          <div class="nav-content">
            <strong>Foro</strong>
            <span>Comunidad</span>
          </div>
        </router-link>

        <router-link to="/pricing" class="nav-item">
          <div class="nav-icon">💰</div>
          <div class="nav-content">
            <strong>Pricing</strong>
            <span>Planes y precios</span>
          </div>
        </router-link>
      </nav>
    </aside>

    <!-- Contenido principal -->
    <main class="main-content">
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
          <p>"StudentGest nos permitió optimizar procesos y mejorar la comunicación con los padres."</p>
          <span>- Directora, UCB</span>
        </article>
        <article class="tcard">
          <p>"Los reportes son claros y el control de asistencias es inmediato."</p>
          <span>- Coordinador Académico, UPB</span>
        </article>
        <article class="tcard">
          <p>"Escalable y seguro, ideal para nuestra red de colegios."</p>
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
    </main>
  </div>
</template>

<script setup>
// Sin lógica - Solo contenido estático
</script>

<style scoped>
.home-layout {
  display: grid;
  grid-template-columns: 300px 1fr;
  min-height: 100vh;
  gap: 0;
}

/* Barra lateral */
.sidebar {
  background: linear-gradient(135deg, #2a4dd0 0%, #1e40af 100%);
  color: white;
  padding: 2rem 1.5rem;
  height: 100vh;
  position: sticky;
  top: 0;
  overflow-y: auto;
}

.sidebar-header {
  text-align: center;
  margin-bottom: 2rem;
  padding-bottom: 1.5rem;
  border-bottom: 1px solid rgba(255,255,255,0.2);
}

.sidebar-header h3 {
  font-size: 1.5rem;
  font-weight: 700;
  margin-bottom: 0.5rem;
}

.sidebar-header p {
  opacity: 0.8;
  font-size: 0.9rem;
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
  padding: 1rem;
  border-radius: 12px;
  text-decoration: none;
  color: white;
  transition: all 0.3s ease;
  border: 1px solid transparent;
}

.nav-item:hover {
  background: rgba(255,255,255,0.1);
  border-color: rgba(255,255,255,0.3);
  transform: translateX(5px);
}

.nav-item.router-link-active {
  background: rgba(255,255,255,0.15);
  border-color: rgba(255,255,255,0.4);
}

.nav-icon {
  font-size: 1.5rem;
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(255,255,255,0.1);
  border-radius: 10px;
}

.nav-content {
  flex: 1;
}

.nav-content strong {
  display: block;
  font-weight: 600;
  margin-bottom: 0.2rem;
}

.nav-content span {
  font-size: 0.8rem;
  opacity: 0.8;
}

/* Contenido principal */
.main-content {
  padding: 2rem;
  background: #f8fafc;
  overflow-y: auto;
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
  max-width: 100%;
  background: #fff;
  border-radius: 1rem;
  box-shadow: 0 2px 8px rgba(0,0,0,0.07);
  padding: 1.5rem;
  text-align: left;
}
.info-negocio h2 { color: #2a4dd0; margin-bottom: 0.5rem; }
.info-negocio ul { margin-top: 1rem; padding-left: 1.2rem; }
.info-negocio li { margin-bottom: 0.5rem; }

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
}
.stat strong { display:block; font-size: 1.8rem; line-height:1; color:#2a4dd0; }
.stat span { color:#555; }

/* Gallery */
.gallery {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1rem;
  max-width: 100%;
  margin: 2rem auto;
}
.gallery img {
  width: 100%;
  height: 220px;
  object-fit: cover;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0,0,0,.06);
}

/* Testimonials */
.testimonials {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1rem;
  max-width: 100%;
  margin: 2rem auto;
}
.tcard {
  background: #fff;
  border-radius: 12px;
  padding: 1rem;
  box-shadow: 0 2px 8px rgba(0,0,0,.06);
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
  max-width: 100%;
}
.cta h3 { margin-bottom: .75rem; }
.cta-actions { display: flex; gap: .75rem; justify-content: center; }
.cta .btn-primary, .cta .btn-outline { text-decoration: none; padding: .55rem 1rem; border-radius: 8px; font-weight: 600; }
.cta .btn-primary { background: #2a4dd0; color: #fff; }
.cta .btn-outline { background: #fff; color: #2a4dd0; border: 2px solid #2a4dd0; }

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
  
  .stats { grid-template-columns: repeat(2, 1fr); }
  .testimonials { grid-template-columns: repeat(2, 1fr); }
  .gallery { grid-template-columns: repeat(2, 1fr); }
}

@media (max-width: 600px) {
  .home-hero { padding: 2rem 1rem; }
  .hero-content h1 { font-size: 1.5rem; }
  .stats, .testimonials, .gallery { grid-template-columns: 1fr; }
  .main-content { padding: 1rem; }
}
</style>