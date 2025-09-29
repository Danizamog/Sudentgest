<template>
  <div id="app">
    <Navbar v-if="isLoggedIn" />
    <SignIn v-else />

    <main class="content-area" v-if="isLoggedIn">
      <div class="content-wrapper">
        <router-view />
      </div>
    </main>

    <Footer v-if="isLoggedIn" />
  </div>
</template>

<script>
import Navbar from "./components/Navbar.vue";
import Footer from "./components/Footer.vue";
import SignIn from "./views/SignIn.vue";

export default {
  name: "App",
  components: { Navbar, Footer, SignIn },
  data() {
    return {
      isLoggedIn: !!localStorage.getItem('rol')
    }
  },
  created() {
    window.addEventListener('storage', () => {
      this.isLoggedIn = !!localStorage.getItem('rol');
    });
  }
}
</script>
