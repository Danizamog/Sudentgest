<template>
  <transition name="fade">
    <section class="wrap">
      <header class="hero">
        <h1>Nosotros</h1>
        <p>Impulsamos la transformación digital en educación con herramientas simples, seguras y escalables.</p>
      </header>

      <section class="values">
        <article>
          <h3>Misión</h3>
          <p>Optimizar la gestión académica y la comunicación entre comunidad educativa.</p>
        </article>
        <article>
          <h3>Visión</h3>
          <p>Ser la plataforma líder de gestión educativa en LATAM.</p>
        </article>
        <article>
          <h3>Valores</h3>
          <p>Seguridad, Transparencia, Innovación y Empatía con el usuario.</p>
        </article>
      </section>

    </section>
  </transition>
</template>

<script setup>
// vacío por ahora
</script>

<style scoped>
.wrap{ max-width: 1100px; margin: 2rem auto; padding: 0 1rem; animation: slideIn .7s; }
.hero{ text-align:center; margin-bottom:1.5rem; }
.values{ display:grid; grid-template-columns: repeat(3,1fr); gap:1rem; margin-bottom:1.5rem; }
.values article{ background:#fff; padding:1rem; border-radius:12px; box-shadow:0 2px 8px rgba(0,0,0,.06); }
.values h3{ color:#2a4dd0; margin-bottom:.4rem; }

.team h2{ text-align:center; margin-bottom:1rem; }
.members{ display:grid; grid-template-columns: repeat(3,1fr); gap:1rem; }
.member{ background:#fff; border-radius:12px; padding:1rem; text-align:center; box-shadow:0 2px 8px rgba(0,0,0,.06); transition: transform .18s ease, box-shadow .2s ease; }
.member:hover{ transform: translateY(-4px); box-shadow: 0 10px 24px rgba(0,0,0,.08); }
.member img{ width:100%; height:220px; object-fit:cover; border-radius:10px; margin-bottom:.6rem; }

.fade-enter-active,.fade-leave-active{ transition: opacity .4s; }
.fade-enter-from,.fade-leave-to{ opacity:0; }
@keyframes slideIn { from{ transform: translateY(20px); opacity:0 } to{ transform: translateY(0); opacity:1 } }
@media (max-width: 900px){ .values, .members{ grid-template-columns: repeat(2,1fr); } }
@media (max-width: 600px){ .values, .members{ grid-template-columns: 1fr; } }
</style>