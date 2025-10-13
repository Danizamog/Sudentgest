<template>
  <transition name="fade">
    <section class="wrap">
      <h1>Información del Sistema</h1>
      <p class="lead">StudentGest optimiza la gestión académica y la comunicación institucional.</p>
      <div class="blocks">
        <article>
          <h3>Misión</h3>
          <p>Proveer herramientas simples y seguras para la gestión educativa.</p>
        </article>
        <article>
          <h3>Visión</h3>
          <p>Ser la plataforma líder en administración académica en LATAM.</p>
        </article>
        <article>
          <h3>Multi-tenant</h3>
          <p>Separación de datos por institución (tenant) para máxima seguridad.</p>
        </article>
      </div>
    </section>
  </transition>
</template>

<script setup>
// vacío por ahora
</script>

<style scoped>
.wrap{ max-width:900px; margin:2rem auto; padding:0 1rem; animation: slideIn .7s; }
.blocks{ display:grid; gap:1rem; grid-template-columns: repeat(3,1fr); }
article{ background:#fff; border-radius:12px; padding:1rem; box-shadow:0 2px 8px rgba(0,0,0,.06); transition: transform .18s ease, box-shadow .2s ease; }
article:hover{ transform: translateY(-4px); box-shadow: 0 10px 24px rgba(0,0,0,.08); }
h3{ color:#2a4dd0; margin-bottom:.5rem; }
.lead{ color:#555; margin:.5rem 0 1rem; }
.fade-enter-active,.fade-leave-active{ transition: opacity .4s; }
.fade-enter-from,.fade-leave-to{ opacity:0; }
@keyframes slideIn { from{ transform: translateY(20px); opacity:0 } to{ transform: translateY(0); opacity:1 } }
@media (max-width: 900px){ .blocks{ grid-template-columns: repeat(2,1fr) } }
@media (max-width: 600px){ .blocks{ grid-template-columns: 1fr } }
</style>