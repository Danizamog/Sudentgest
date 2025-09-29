<template>
  <div>
    <!-- HEADER (full width) -->
    <header class="site-header">
      <div class="container">
        <h1 class="site-title">StudentGest</h1>
      </div>
    </header>

    <!-- NAV (full width) -->
    <nav class="site-nav" v-if="rol !== null">
      <div class="container nav-flex">
        <div class="nav-left">
          <!-- espacio para logo o link futuro -->
        </div>

        <div class="nav-center">
          <router-link to="/home" class="nav-link">Inicio</router-link>
          <router-link to="/help" class="nav-link">Ayuda</router-link>

          <!-- Solo para profesores -->
          <router-link v-if="rol === 'profesor'" to="/administrar" class="nav-link">Administrar</router-link>

          <!-- Solo para otros usuarios -->
          <router-link v-if="rol && rol !== 'profesor'" to="/ver-materias" class="nav-link">Ver materias</router-link>
        </div>

        <div class="nav-right">
          <router-link v-if="rol === 'invitado'" to="/signIn" class="login-btn">Iniciar sesión</router-link>
          <span v-else @click="logout" class="login-btn" style="cursor:pointer;">Cerrar sesión</span>
        </div>
      </div>
    </nav>
  </div>
</template>

<script>
import { supabase } from '../supabase'

export default {
  name: "Navbar",
  data() {
    return {
      rol: null // null significa que aún no cargó el rol
    };
  },
  async created() {
    const token = localStorage.getItem('token')
    if (!token) {
      this.rol = 'invitado'
      return
    }

    try {
      // Obtener usuario logueado desde Supabase
      const { data: { user }, error: userError } = await supabase.auth.getUser()
      if (userError || !user) {
        this.rol = 'invitado'
        return
      }

      // Obtener rol desde la tabla 'profiles' (ajusta si tu tabla tiene otro nombre)
      const { data: profile, error: profileError } = await supabase
        .from('profiles')
        .select('rol')
        .eq('id', user.id)
        .single()

      if (profileError || !profile) {
        this.rol = 'invitado'
      } else {
        this.rol = profile.rol
      }

    } catch (err) {
      console.error('Error al obtener rol', err)
      this.rol = 'invitado'
    }
  },
  methods: {
    logout() {
      localStorage.removeItem('token')
      this.rol = 'invitado'
      this.$router.push('/signIn')
    }
  }
};
</script>

<style scoped>
/* tu CSS existente */
.container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 20px;
}
.site-header {
  width: 100%;
  background: #111318;
  color: #ffffff;
  padding: 18px 0;
  box-shadow: 0 1px 0 rgba(0,0,0,0.15);
}
.site-title {
  margin: 0;
  font-size: 22px;
  font-weight: 700;
  letter-spacing: 0.3px;
  color: #ffffff;
  text-align: left;
}
.site-nav {
  width: 100%;
  background: #f5f6f8;
  border-bottom: 1px solid #e6e7ea;
}
.nav-flex {
  display: flex;
  align-items: center;
  gap: 16px;
  height: 64px;
}
.nav-left, .nav-center, .nav-right {
  flex: 1;
}
.nav-left { text-align: left; }
.nav-center { text-align: center; }
.nav-right { text-align: right; }
.nav-link {
  margin: 0 12px;
  color: #374151;
  text-decoration: none;
  font-weight: 600;
  font-size: 15px;
  padding: 8px 10px;
  display: inline-block;
  cursor: pointer;
}
.nav-link:hover {
  color: #0f172a;
  text-decoration: underline;
}
.login-btn {
  background: #374151;
  color: #ffffff;
  padding: 8px 14px;
  border-radius: 8px;
  text-decoration: none;
  font-weight: 700;
  display: inline-block;
  cursor: pointer;
  box-shadow: 0 2px 6px rgba(55,65,81,0.08);
}
.login-btn:hover {
  background: #2f3740;
}
@media (max-width: 768px) {
  .nav-flex {
    flex-direction: column;
    height: auto;
    padding: 10px 0;
    gap: 8px;
  }
  .site-title { text-align: center; font-size: 20px; }
  .nav-left { order: 1; width: 100%; text-align: center; }
  .nav-center { order: 2; width: 100%; text-align: center; }
  .nav-right { order: 3; width: 100%; text-align: center; }
  .nav-link { margin: 6px 10px; }
}
</style>
