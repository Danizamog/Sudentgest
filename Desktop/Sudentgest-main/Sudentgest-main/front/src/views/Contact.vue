<template>
  <transition name="fade">
    <section class="wrap">
      <h1>Contacto</h1>
      <p class="lead">¿Necesitas soporte o una demo?</p>
      <form class="form">
        <input type="text" placeholder="Nombre" />
        <input type="email" placeholder="Correo" />
        <textarea rows="4" placeholder="Mensaje"></textarea>
        <button type="button">Enviar</button>
      </form>
      <p class="hint">También puedes escribirnos a soporte@studentgest.com</p>
    </section>
  </transition>
</template>

<script setup>
// vacío por ahora
</script>

<style scoped>
.wrap {
  max-width: 700px;
  margin: 2rem auto;
  padding: 0 1rem;
  animation: slideIn 0.7s;
}
.form {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  background: #fff;
  padding: 1rem;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
}
input,
textarea {
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 0.75rem;
  font: inherit;
  transition:
    border-color 0.15s,
    box-shadow 0.2s;
}
input:focus,
textarea:focus {
  outline: none;
  border-color: #2a4dd0;
  box-shadow: 0 0 0 4px rgba(42, 77, 208, 0.12);
}
button {
  align-self: flex-start;
  background: #2a4dd0;
  color: #fff;
  border: none;
  padding: 0.6rem 1rem;
  border-radius: 8px;
  cursor: pointer;
  transition:
    transform 0.15s,
    box-shadow 0.2s;
}
button:hover {
  transform: translateY(-1px);
  box-shadow: 0 6px 20px rgba(42, 77, 208, 0.2);
}
.lead {
  color: #555;
  margin: 0.5rem 0 1rem;
}
.hint {
  margin-top: 0.75rem;
  color: #666;
}
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.4s;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
@keyframes slideIn {
  from {
    transform: translateY(20px);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}
</style>