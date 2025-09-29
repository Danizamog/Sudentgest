<template>
  <transition name="fade">
    <section class="wrap">
      <div class="hero">
        <h1>Características</h1>
        <p class="lead">Gestión académica integral, escalable y segura.</p>
        <div class="cta">
          <router-link to="/signup" class="btn-primary">Probar ahora</router-link>
          <router-link to="/pricing" class="btn-outline">Ver precios</router-link>
        </div>
      </div>

      <div class="grid">
        <article class="card">
          <h3>Asistencias</h3>
          <p>Control de ausencias, tardanzas y excusas por rol.</p>
        </article>
        <article class="card">
          <h3>Calificaciones</h3>
          <p>Seguimiento de tareas, notas y estado (pendiente/completado).</p>
        </article>
        <article class="card">
          <h3>Foros</h3>
          <p>Comunicación efectiva entre padres, docentes y administración.</p>
        </article>
        <article class="card">
          <h3>Reportes</h3>
          <p>Informes y analíticas para toma de decisiones.</p>
        </article>
        <article class="card">
          <h3>Seguridad</h3>
          <p>Protección de datos y control de acceso por tenant.</p>
        </article>
        <article class="card">
          <h3>Escalabilidad</h3>
          <p>De 1.000 a 1.000.000 de estudiantes sin perder rendimiento.</p>
        </article>
      </div>
    </section>
  </transition>
</template>

<script setup>
// vacío por ahora
</script>

<style scoped>
.wrap { max-width: 1100px; margin: 2rem auto; padding: 0 1rem; animation: slideIn .7s; }
.hero { text-align: center; margin-bottom: 1.25rem; }
.cta { display: flex; gap: .75rem; justify-content: center; margin-top: 1rem; }
.btn-primary { background: #2a4dd0; color: #fff; border: none; padding: .55rem 1rem; border-radius: 8px; font-weight: 600; text-decoration: none; }
.btn-outline { background: #fff; color: #2a4dd0; border: 2px solid #2a4dd0; padding: .45rem 1rem; border-radius: 8px; font-weight: 600; text-decoration: none; }
.lead { margin: .5rem 0 1.5rem; color: #555; }
.grid { display: grid; gap: 1rem; grid-template-columns: repeat(3, 1fr); }
.card { background: #fff; border-radius: 12px; padding: 1.25rem; box-shadow: 0 2px 8px rgba(0,0,0,.06); transition: transform .18s ease, box-shadow .2s ease; }
.card:hover { transform: translateY(-4px); box-shadow: 0 10px 24px rgba(0,0,0,.08); }
.card h3 { color: #2a4dd0; margin-bottom: .5rem; }
.fade-enter-active,.fade-leave-active{ transition: opacity .4s; }
.fade-enter-from,.fade-leave-to{ opacity: 0; }
@keyframes slideIn { from{ transform: translateY(20px); opacity:0 } to{ transform: translateY(0); opacity:1 } }
@media (max-width: 900px){ .grid{ grid-template-columns: repeat(2,1fr) } }
@media (max-width: 600px){ .grid{ grid-template-columns: 1fr } }
</style>