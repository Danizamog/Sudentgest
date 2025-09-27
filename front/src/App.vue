<template>
  <div id="app">
    <HelloWorld msg="Bienvenido al Sistema Académico" />
    <router-view />
  </div>
</template>

<script>
import HelloWorld from './components/HelloWorld.vue'

export default {
  name: 'App',
  components: { HelloWorld }
}
</script>
