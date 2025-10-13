<template>
  <footer class="footer">
    <div class="wrap">
      <div class="brand">SG StudentGest</div>
      <nav class="links">
        <router-link to="/features">Características</router-link>
        <router-link to="/pricing">Precios</router-link>
        <router-link to="/nosotros">Nosotros</router-link>
        <router-link to="/contact">Contacto</router-link>
      </nav>
      <div class="copy">© 2024 StudentGest - Todos los derechos reservados.</div>
    </div>
  </footer>
</template>

<script setup>
// vacío
</script>

<style scoped>
.footer{ background:#111827; color:#e5e7eb; margin-top:auto; }
.wrap{ max-width:1100px; margin:0 auto; padding:1rem; display:flex; flex-wrap:wrap; align-items:center; justify-content:space-between; gap:.75rem; }
.brand{ font-weight:700; color:#fff; }
.links{ display:flex; gap:1rem; }
.links a{ color:#e5e7eb; text-decoration:none; }
.links a.router-link-active{ color:#93c5fd; }
.copy{ font-size:.9rem; color:#9ca3af; }
@media (max-width: 600px){ .wrap{ flex-direction:column; align-items:flex-start; } }
</style>