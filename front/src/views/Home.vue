<template>
  <div class="home-container">
    <h1>Bienvenido a StudentGest</h1>
    <p>Tu plataforma para la gestión académica eficiente.</p>
    <router-link to="/sign-in" class="btn btn-primary">Iniciar Sesión</router-link>
    <router-link to="/sign-up" class="btn btn-secondary">Registrarse</router-link>
  </div>
</template>
<script setup>

</script>
<style scoped>  
btn {
  margin: 10px;
  padding: 10px 20px;
  font-size: 16px;
  text-decoration: none;
  border-radius: 5px;
}
.btn-primary {
  background-color: #4f46e5;
  color: white;
}
.btn-secondary {
  background-color: #e5e7eb;
  color: #374151;
}
.home-container {
  text-align: center;
  margin-top: 100px;
  font-family: 'Inter', sans-serif;
}
h1 {
  font-size: 2.5rem;
  margin-bottom: 20px;
  color: #1f2937;
}
p {
  font-size: 1.2rem;
  margin-bottom: 40px;
  color: #4b5563;
}

</style>