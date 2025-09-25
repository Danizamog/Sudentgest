<template>
  <div class="home">
    <h1>Welcome to Your Vue.js App</h1>
    <HelloWorld msg="Welcome to Your Vue.js App"/>
  </div>
</template>

<script>
import HelloWorld from '../components/HelloWorld.vue';

export default {
  name: 'Home',
  components: {
    HelloWorld
  }
}
</script>

<style scoped>
.home {
  text-align: center;
  padding: 20px;
}
</style>