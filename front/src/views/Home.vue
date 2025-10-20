<template>
  <div class="home-page">
    <!-- Hero Section -->
    <section class="hero-section">
      <div class="container">
        <div class="hero-content">
          <div class="hero-text">
            <span class="hero-badge">🎓 Plataforma Educativa</span>
            <h1 class="hero-title">
              Gestión educativa <br/>
              <span class="gradient-text">moderna y eficiente</span>
            </h1>
            <p class="hero-description">
              StudentGest es la plataforma integral que conecta estudiantes, profesores y directores
              en un solo lugar. Gestiona cursos, asistencias, calificaciones y comunicación de manera simple.
            </p>
            <div class="hero-actions">
              <router-link to="/signin" class="btn btn-primary btn-lg">
                Comenzar Ahora
              </router-link>
              <router-link to="/features" class="btn btn-outline btn-lg">
                Ver Características
              </router-link>
            </div>
            <div class="hero-stats">
              <div class="stat-item">
                <span class="stat-number">500+</span>
                <span class="stat-label">Estudiantes</span>
              </div>
              <div class="stat-item">
                <span class="stat-number">50+</span>
                <span class="stat-label">Profesores</span>
              </div>
              <div class="stat-item">
                <span class="stat-number">3</span>
                <span class="stat-label">Instituciones</span>
              </div>
            </div>
          </div>
          <div class="hero-image">
            <div class="hero-card card-1">
              <div class="card-icon">📚</div>
              <h4>Cursos</h4>
              <p>Gestiona tus cursos fácilmente</p>
            </div>
            <div class="hero-card card-2">
              <div class="card-icon">✅</div>
              <h4>Asistencia</h4>
              <p>Control automático</p>
            </div>
            <div class="hero-card card-3">
              <div class="card-icon">📊</div>
              <h4>Reportes</h4>
              <p>Análisis en tiempo real</p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- Features Section -->
    <section class="features-section section">
      <div class="container">
        <div class="section-header text-center">
          <span class="section-badge">Características</span>
          <h2 class="section-title">Todo lo que necesitas en un solo lugar</h2>
          <p class="section-description">
            Herramientas poderosas diseñadas para facilitar la gestión educativa
          </p>
        </div>
        <div class="features-grid">
          <div class="feature-card card">
            <div class="feature-icon">📚</div>
            <h3>Gestión de Cursos</h3>
            <p>Crea, organiza y administra cursos con facilidad. Asigna materiales, tareas y mantén todo organizado.</p>
          </div>
          <div class="feature-card card">
            <div class="feature-icon">✅</div>
            <h3>Control de Asistencia</h3>
            <p>Registra asistencias rápidamente y gestiona excusas de manera eficiente.</p>
          </div>
          <div class="feature-card card">
            <div class="feature-icon">📝</div>
            <h3>Calificaciones</h3>
            <p>Sistema completo de notas con cálculo automático de promedios y visualización clara.</p>
          </div>
          <div class="feature-card card">
            <div class="feature-icon">💬</div>
            <h3>Foro de Discusión</h3>
            <p>Espacio de comunicación para estudiantes y profesores con hilos organizados por categorías.</p>
          </div>
          <div class="feature-card card">
            <div class="feature-icon">📊</div>
            <h3>Reportes y Analytics</h3>
            <p>Dashboards interactivos con métricas importantes y tendencias en tiempo real.</p>
          </div>
          <div class="feature-card card">
            <div class="feature-icon">🔒</div>
            <h3>Multi-tenant Seguro</h3>
            <p>Datos completamente aislados por institución con autenticación robusta.</p>
          </div>
        </div>
      </div>
    </section>

    <!-- CTA Section -->
    <section class="cta-section section">
      <div class="container">
        <div class="cta-content">
          <h2>¿Listo para transformar tu gestión educativa?</h2>
          <p>Únete a las instituciones que ya confían en StudentGest</p>
          <router-link to="/signin" class="btn btn-secondary btn-lg">
            Comenzar Gratis
          </router-link>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup>
// No complex logic needed for landing page
</script>

<style scoped>
/* ===== Hero Section ===== */
.hero-section {
  min-height: calc(100vh - 73px);
  display: flex;
  align-items: center;
  background: linear-gradient(135deg, #f6f8fc 0%, #ffffff 100%);
  position: relative;
  overflow: hidden;
}

.hero-section::before {
  content: '';
  position: absolute;
  top: 0;
  right: 0;
  width: 50%;
  height: 100%;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.05) 0%, rgba(235, 131, 23, 0.05) 100%);
  border-radius: 0 0 0 50%;
}

.hero-content {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-12);
  align-items: center;
  padding: var(--space-16) 0;
  position: relative;
  z-index: 1;
}

.hero-text {
  max-width: 600px;
}

.hero-badge {
  display: inline-block;
  padding: var(--space-2) var(--space-4);
  background-color: rgba(102, 126, 234, 0.1);
  color: var(--secondary);
  border-radius: var(--radius-full);
  font-size: var(--text-sm);
  font-weight: var(--font-semibold);
  margin-bottom: var(--space-6);
}

.hero-title {
  font-size: var(--text-6xl);
  font-weight: var(--font-extrabold);
  line-height: 1.1;
  color: var(--primary);
  margin-bottom: var(--space-6);
}

.gradient-text {
  background: linear-gradient(135deg, var(--secondary) 0%, var(--accent) 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.hero-description {
  font-size: var(--text-xl);
  color: var(--gray-600);
  line-height: var(--leading-relaxed);
  margin-bottom: var(--space-8);
}

.hero-actions {
  display: flex;
  gap: var(--space-4);
  margin-bottom: var(--space-12);
}

.hero-stats {
  display: flex;
  gap: var(--space-8);
  padding-top: var(--space-6);
  border-top: 1px solid var(--gray-200);
}

.stat-item {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.stat-number {
  font-size: var(--text-3xl);
  font-weight: var(--font-bold);
  color: var(--secondary);
}

.stat-label {
  font-size: var(--text-sm);
  color: var(--gray-600);
}

/* Hero Image - Floating Cards */
.hero-image {
  position: relative;
  height: 500px;
}

.hero-card {
  position: absolute;
  background: white;
  padding: var(--space-6);
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-xl);
  animation: float 3s ease-in-out infinite;
}

.hero-card h4 {
  font-size: var(--text-lg);
  font-weight: var(--font-semibold);
  color: var(--primary);
  margin-bottom: var(--space-2);
}

.hero-card p {
  font-size: var(--text-sm);
  color: var(--gray-600);
  margin: 0;
}

.card-icon {
  font-size: var(--text-4xl);
  margin-bottom: var(--space-3);
}

.card-1 {
  top: 50px;
  left: 50px;
  width: 220px;
  animation-delay: 0s;
}

.card-2 {
  top: 200px;
  right: 100px;
  width: 200px;
  animation-delay: 1s;
}

.card-3 {
  bottom: 80px;
  left: 100px;
  width: 210px;
  animation-delay: 0.5s;
}

@keyframes float {
  0%, 100% {
    transform: translateY(0px);
  }
  50% {
    transform: translateY(-20px);
  }
}

/* ===== Features Section ===== */
.features-section {
  background-color: var(--white);
}

.section-header {
  max-width: 700px;
  margin: 0 auto var(--space-12);
}

.section-badge {
  display: inline-block;
  padding: var(--space-2) var(--space-4);
  background-color: rgba(235, 131, 23, 0.1);
  color: var(--accent);
  border-radius: var(--radius-full);
  font-size: var(--text-sm);
  font-weight: var(--font-semibold);
  margin-bottom: var(--space-4);
}

.section-title {
  font-size: var(--text-4xl);
  font-weight: var(--font-bold);
  color: var(--primary);
  margin-bottom: var(--space-4);
}

.section-description {
  font-size: var(--text-lg);
  color: var(--gray-600);
}

.features-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--space-8);
}

.feature-card {
  text-align: center;
  transition: transform var(--transition-base);
}

.feature-card:hover {
  transform: translateY(-8px);
}

.feature-icon {
  font-size: var(--text-5xl);
  margin-bottom: var(--space-4);
}

.feature-card h3 {
  font-size: var(--text-xl);
  font-weight: var(--font-semibold);
  color: var(--primary);
  margin-bottom: var(--space-3);
}

.feature-card p {
  font-size: var(--text-base);
  color: var(--gray-600);
  line-height: var(--leading-relaxed);
  margin: 0;
}

/* ===== CTA Section ===== */
.cta-section {
  background: linear-gradient(135deg, var(--secondary) 0%, var(--accent) 100%);
  color: var(--white);
}

.cta-content {
  text-align: center;
  max-width: 700px;
  margin: 0 auto;
}

.cta-content h2 {
  font-size: var(--text-4xl);
  font-weight: var(--font-bold);
  color: var(--white);
  margin-bottom: var(--space-4);
}

.cta-content p {
  font-size: var(--text-xl);
  color: rgba(255, 255, 255, 0.9);
  margin-bottom: var(--space-8);
}

/* ===== Responsive ===== */
@media (max-width: 1024px) {
  .features-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 768px) {
  .hero-content {
    grid-template-columns: 1fr;
  }

  .hero-image {
    display: none;
  }

  .hero-title {
    font-size: var(--text-4xl);
  }

  .hero-actions {
    flex-direction: column;
  }

  .hero-actions .btn {
    width: 100%;
  }

  .features-grid {
    grid-template-columns: 1fr;
  }

  .cta-content h2 {
    font-size: var(--text-3xl);
  }
}
</style>
