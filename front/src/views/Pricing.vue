<template>
  <transition name="fade">
    <section class="wrap">
      <h1>Planes y Precios</h1>
      <p class="lead">Elige el plan que mejor se adapte a tu institución.</p>
      <div class="grid">
        <article class="plan">
          <h3>Básico</h3>
          <p class="price">US$ 49/mes</p>
          <ul>
            <li>Hasta 1.000 estudiantes</li>
            <li>Asistencias y calificaciones</li>
            <li>Soporte por email</li>
          </ul>
          <button>Empezar</button>
        </article>
        <article class="plan featured">
          <h3>Pro</h3>
          <p class="price">US$ 149/mes</p>
          <ul>
            <li>Hasta 10.000 estudiantes</li>
            <li>Foros y reportes avanzados</li>
            <li>Soporte prioritario</li>
          </ul>
          <button>Empezar</button>
        </article>
        <article class="plan">
          <h3>Enterprise</h3>
          <p class="price">Contactar</p>
          <ul>
            <li>+100.000 estudiantes</li>
            <li>Integraciones y SSO</li>
            <li>Soporte dedicado</li>
          </ul>
          <button>Contactar</button>
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
.grid { display: grid; gap: 1rem; grid-template-columns: repeat(3, 1fr); }
.plan { background:#fff; border-radius:12px; padding:1.5rem; box-shadow:0 2px 8px rgba(0,0,0,.06); text-align:center; transition: transform .18s ease, box-shadow .2s ease; }
.plan:hover { transform: translateY(-4px); box-shadow: 0 10px 24px rgba(0,0,0,.08); }
.plan h3{ margin-bottom:.5rem; }
.price{ font-weight:700; color:#2a4dd0; margin:.25rem 0 1rem; }
.plan button{ background:#2a4dd0; color:#fff; border:none; padding:.5rem 1rem; border-radius:8px; cursor:pointer; transition: transform .15s, box-shadow .2s; }
.plan button:hover{ transform: translateY(-1px); box-shadow: 0 6px 20px rgba(42,77,208,.2); }
.featured{ border:2px solid #2a4dd0; transform: scale(1.01); }
.fade-enter-active,.fade-leave-active{ transition: opacity .4s; }
.fade-enter-from,.fade-leave-to{ opacity:0; }
@keyframes slideIn { from{ transform: translateY(20px); opacity:0 } to{ transform: translateY(0); opacity:1 } }
@media (max-width: 900px){ .grid{ grid-template-columns: repeat(2,1fr) } }
@media (max-width: 600px){ .grid{ grid-template-columns: 1fr } }
</style>