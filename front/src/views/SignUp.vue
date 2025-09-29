<template>
  <transition name="fade">
    <section class="wrap">
      <h1>Crear cuenta</h1>
      <form class="form" @submit.prevent>
        <input type="email" placeholder="Correo institucional" required />
        <input type="password" placeholder="Contraseña" required />
        <button type="submit">Registrarse</button>
      </form>
      <p class="hint">¿Ya tienes cuenta? <router-link to="/signin">Inicia sesión</router-link></p>
    </section>
  </transition>
</template>

<script setup>
// Integra tu lógica de registro (Supabase/Auth) aquí si corresponde.
</script>

<style scoped>
.wrap{ max-width:420px; margin:3rem auto; padding:0 1rem; animation: slideIn .7s; }
.form{ display:flex; flex-direction:column; gap:.75rem; background:#fff; padding:1rem; border-radius:12px; box-shadow:0 2px 8px rgba(0,0,0,.06); }
input{ border:1px solid #e5e7eb; border-radius:8px; padding:.75rem; font:inherit; }
button{ background:#2a4dd0; color:#fff; border:none; padding:.6rem 1rem; border-radius:8px; cursor:pointer; }
.hint{ margin-top:.75rem; color:#666; }
.fade-enter-active,.fade-leave-active{ transition: opacity .4s; }
.fade-enter-from,.fade-leave-to{ opacity:0; }
@keyframes slideIn { from{ transform: translateY(20px); opacity:0 } to{ transform: translateY(0); opacity:1 } }
</style>
