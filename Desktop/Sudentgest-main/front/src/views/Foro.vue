<template>
  <div id="app">

    <!-- Main Content -->
    <main>
      <div class="forum-header">
        <div class="forum-title">
          <h2>Foro de Padres y Maestros</h2>
          <p>Comparte opiniones, ideas y experiencias</p>
        </div>
        <button class="btn btn-primary" @click="showNewThreadModal = true">
          <i class="fas fa-plus"></i> Nuevo Tema
        </button>
      </div>

      <!-- Breadcrumb -->
      <div class="breadcrumb" v-if="selectedThread">
        <a href="#" @click.prevent="selectedThread = null">
          <i class="fas fa-home"></i> Foro
        </a>
        <i class="fas fa-chevron-right"></i>
        <span>{{ selectedThread.category }}</span>
        <i class="fas fa-chevron-right"></i>
        <span>{{ selectedThread.title }}</span>
      </div>

      <div class="forum-layout">
        <!-- Sidebar -->
        <aside class="sidebar">
          <div class="search-box">
            <i class="fas fa-search"></i>
            <input type="text" placeholder="Buscar en el foro..." v-model="searchQuery">
          </div>
          <h3>Categorías</h3>
          <ul class="categories">
            <li v-for="category in categories" :key="category.id">
              <a href="#" 
                 :class="{ active: selectedCategory === category.id }" 
                 @click.prevent="selectCategory(category.id)">
                <i :class="category.icon"></i>
                {{ category.name }}
                <span class="badge">{{ category.threadCount }}</span>
              </a>
            </li>
          </ul>
        </aside>

        <!-- Forum Content -->
        <div class="forum-content">
          <!-- Thread List -->
          <div v-if="!selectedThread">
            <div v-if="filteredThreads.length === 0" class="empty-state">
              <i class="fas fa-comments"></i>
              <h3>No se encontraron temas</h3>
              <p>Intenta cambiar los filtros de búsqueda o crear un nuevo tema.</p>
              <button class="btn btn-accent" @click="showNewThreadModal = true">
                <i class="fas fa-plus"></i> Crear primer tema
              </button>
            </div>
            
            <div class="thread-card" 
                 v-for="thread in filteredThreads" 
                 :key="thread.id" 
                 :class="{ unread: thread.unread }"
                 @click="selectThread(thread)">
              <div class="thread-header">
                <div>
                  <h3 class="thread-title">{{ thread.title }}</h3>
                  <div class="thread-meta">
                    <div class="thread-author">
                      <div class="author-avatar">{{ getInitials(thread.author) }}</div>
                      {{ thread.author }}
                    </div>
                    <span>en {{ thread.category }}</span>
                    <span>{{ formatDate(thread.date) }}</span>
                    <span v-if="thread.unread" class="tag" style="background-color: var(--accent); color: var(--primary);">Nuevo</span>
                  </div>
                </div>
                <div class="thread-stats">
                  <div class="stat">
                    <i class="far fa-comment"></i> {{ thread.replies }}
                  </div>
                  <div class="stat">
                    <i class="far fa-eye"></i> {{ thread.views }}
                  </div>
                </div>
              </div>
              <p class="thread-excerpt">{{ thread.excerpt }}</p>
              <div class="thread-tags">
                <span class="tag" v-for="tag in thread.tags" :key="tag">{{ tag }}</span>
              </div>
            </div>
          </div>

          <!-- Thread View -->
          <div class="thread-view" v-if="selectedThread">
            <div class="thread-view-header">
              <h2 class="thread-view-title">{{ selectedThread.title }}</h2>
              <div class="thread-view-meta">
                <span>Por {{ selectedThread.author }}</span>
                <span>en {{ selectedThread.category }}</span>
                <span>{{ formatDate(selectedThread.date) }}</span>
                <span>{{ selectedThread.replies }} respuestas</span>
                <span>{{ selectedThread.views }} vistas</span>
              </div>
            </div>

            <div class="posts">
              <div class="post" v-for="post in selectedThread.posts" :key="post.id">
                <div class="post-header">
                  <div class="post-author">
                    <div class="post-avatar">{{ getInitials(post.author) }}</div>
                    <div class="author-info">
                      <h4>{{ post.author }}</h4>
                      <span>{{ post.role }}</span>
                    </div>
                  </div>
                  <div class="post-date">{{ formatDate(post.date) }}</div>
                </div>
                <div class="post-content">
                  <p>{{ post.content }}</p>
                </div>
                <div class="post-actions">
                  <button class="action-btn">
                    <i class="far fa-thumbs-up"></i> Me gusta
                  </button>
                  <button class="action-btn">
                    <i class="far fa-comment"></i> Responder
                  </button>
                </div>
              </div>
            </div>

            <div class="reply-form">
              <div class="form-group">
                <label for="reply">Tu respuesta</label>
                <textarea id="reply" class="form-control" v-model="newReply" placeholder="Escribe tu respuesta aquí..."></textarea>
              </div>
              <div style="display: flex; gap: 10px; justify-content: flex-end;">
                <button class="btn btn-outline" @click="selectedThread = null">
                  <i class="fas fa-arrow-left"></i> Volver al foro
                </button>
                <button class="btn btn-primary" @click="addReply">
                  <i class="fas fa-paper-plane"></i> Publicar respuesta
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </main>

    <!-- New Thread Modal -->
    <div class="modal-overlay" v-if="showNewThreadModal">
      <div class="modal">
        <div class="modal-header">
          <h3>Nuevo Tema de Discusión</h3>
          <button class="close-btn" @click="showNewThreadModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label for="thread-title">Título</label>
            <input type="text" id="thread-title" class="form-control" v-model="newThread.title" placeholder="Escribe un título claro y descriptivo">
          </div>
          <div class="form-group">
            <label for="thread-category">Categoría</label>
            <select id="thread-category" class="form-control" v-model="newThread.category">
              <option value="">Selecciona una categoría</option>
              <option v-for="category in categories.filter(c => c.id !== 'all')" :key="category.id" :value="category.name">
                {{ category.name }}
              </option>
            </select>
          </div>
          <div class="form-group">
            <label for="thread-content">Contenido</label>
            <textarea id="thread-content" class="form-control" v-model="newThread.content" placeholder="Describe tu tema en detalle..."></textarea>
          </div>
          <div class="form-group">
            <label for="thread-tags">Etiquetas (separadas por comas)</label>
            <input type="text" id="thread-tags" class="form-control" v-model="newThread.tags" placeholder="ej: tareas, evaluación, primaria">
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-outline" @click="showNewThreadModal = false">Cancelar</button>
          <button class="btn btn-primary" @click="createThread">Crear Tema</button>
        </div>
      </div>
    </div>

    <!-- Toast Notification -->
    <div class="toast success" v-if="toast.show" :class="toast.type">
      <i class="fas fa-check-circle"></i>
      <span>{{ toast.message }}</span>
    </div>

    <!-- Footer -->
    <footer>
      <div class="footer-content">
        <p>StudentGest - Sistema de Gestión Estudiantil &copy; 2025</p>
        <p>Foro de Padres y Maestros - Conectando la comunidad educativa</p>
      </div>
    </footer>
  </div>
</template>

<script>
const API = import.meta.env.VITE_API_URL || 'http://localhost:5006';
const PLACEHOLDER_USER_ID = import.meta.env.VITE_PLACEHOLDER_USER_ID || '00000000-0000-0000-0000-000000000000';

export default {
  name: 'ForumApp',
  data() {
    return {
      showNewThreadModal: false,
      selectedCategory: 'all',
      selectedThread: null,
      newReply: '',
      searchQuery: '',
      toast: {
        show: false,
        message: '',
        type: 'success'
      },
      newThread: {
        title: '',
        category: '',
        content: '',
        tags: ''
      },
      // keep initial categories for UI while loading; loadCategories will replace them
      categories: [
        { id: 'all', name: 'Todos los temas', icon: 'fas fa-list', threadCount: 0 }
      ],
      // threads will be loaded from backend (initial demo items removed to avoid duplication)
      threads: []
    };
  },
  computed: {
    filteredThreads() {
      let filtered = this.threads;
      
      // Filtrar por categoría
      if (this.selectedCategory !== 'all') {
        const category = this.categories.find(c => c.id === this.selectedCategory);
        if (category) filtered = filtered.filter(thread => thread.category === category.name);
      }
      
      // Filtrar por búsqueda
      if (this.searchQuery) {
        const query = this.searchQuery.toLowerCase();
        filtered = filtered.filter(thread => 
          (thread.title || '').toLowerCase().includes(query) || 
          (thread.excerpt || '').toLowerCase().includes(query) ||
          (thread.tags || []).some(tag => (tag || '').toLowerCase().includes(query))
        );
      }
      
      return filtered;
    }
  },
  mounted() {
    this.loadCategories();
    this.loadThreads();
  },
  methods: {
    async loadCategories() {
      try {
        const res = await fetch(`${API}/categories`);
        if (!res.ok) throw new Error('no categories');
        const data = await res.json();
        const mapped = data.map(c => ({
          id: c.id,
          name: c.name,
          icon: c.icon || 'fas fa-comments',
          threadCount: 0
        }));
        this.categories = [{ id: 'all', name: 'Todos los temas', icon: 'fas fa-list', threadCount: 0 }, ...mapped];
        this.updateCategoryCounts();
      } catch (err) {
        // leave existing categories if fetch fails
        this.showToast('No se pudieron cargar las categorías', 'error');
      }
    },

    async loadThreads() {
      try {
        let url = `${API}/threads`;
        if (this.selectedCategory && this.selectedCategory !== 'all') {
          url += `?categoryId=${this.selectedCategory}`;
        }
        const res = await fetch(url);
        if (!res.ok) throw new Error('no threads');
        const data = await res.json();
        this.threads = data.map(t => {
          const categoryObj = this.categories.find(c => c.id === (t.category_id || t.categoryId)) || {};
          return {
            id: t.id,
            title: t.title,
            author: t.user_id || t.userId || 'Anónimo',
            category: categoryObj.name || (t.category_name || 'Sin categoría'),
            date: t.last_activity || t.created_at || new Date().toISOString(),
            excerpt: t.excerpt || (t.content ? (t.content.substring(0, 120) + '...') : ''),
            content: t.content || '',
            tags: t.tags || [],
            replies: t.reply_count || t.replies || 0,
            views: t.views || 0,
            unread: false,
            posts: []
          };
        });
        this.updateCategoryCounts();
      } catch (err) {
        this.showToast('No se pudieron cargar los temas', 'error');
      }
    },

    updateCategoryCounts() {
      const counts = {};
      (this.threads || []).forEach(t => {
        const cat = t.category || 'Sin categoría';
        counts[cat] = (counts[cat] || 0) + 1;
      });
      this.categories = this.categories.map(c => ({
        ...c,
        threadCount: counts[c.name] || 0
      }));
    },

    selectCategory(categoryId) {
      this.selectedCategory = categoryId;
      this.selectedThread = null;
      // If category is backend id, reload threads filtered by category id
      this.loadThreads();
    },

    async selectThread(thread) {
      this.selectedThread = thread;
      thread.unread = false;
      // increment local views to reflect immediate feedback
      thread.views = (thread.views || 0) + 1;

      try {
        const res = await fetch(`${API}/threads/${thread.id}/replies`);
        if (!res.ok) throw new Error('no replies');
        const replies = await res.json();
        const posts = [
          {
            id: `${thread.id}-op`,
            author: thread.author || 'Anónimo',
            role: '',
            date: thread.date,
            content: thread.content || thread.excerpt || ''
          },
          ...replies.map(r => ({
            id: r.id,
            author: r.user_id || r.userId || 'Anónimo',
            role: '',
            date: r.created_at,
            content: r.content
          }))
        ];
        this.$set(this.selectedThread, 'posts', posts);
        this.selectedThread.replies = replies.length;
      } catch (err) {
        // Keep at least OP post
        if (!this.selectedThread.posts || this.selectedThread.posts.length === 0) {
          this.$set(this.selectedThread, 'posts', [{
            id: `${thread.id}-op`,
            author: thread.author || 'Anónimo',
            role: '',
            date: thread.date,
            content: thread.content || thread.excerpt || ''
          }]);
        }
        this.showToast('No se pudieron cargar las respuestas', 'error');
      }
    },

    getInitials(name) {
      if (!name) return 'U';
      return (name || 'U').split(' ').map(n => n[0]).join('').toUpperCase();
    },

    formatDate(dateString) {
      if (!dateString) return '';
      const options = { year: 'numeric', month: 'long', day: 'numeric' };
      return new Date(dateString).toLocaleDateString('es-ES', options);
    },

    showToast(message, type = 'success') {
      this.toast = { show: true, message, type };
      setTimeout(() => {
        this.toast.show = false;
      }, 3000);
    },

    async createThread() {
      if (!this.newThread.title || !this.newThread.content) {
        this.showToast('Por favor, complete el título y el contenido del tema', 'error');
        return;
      }
      if (!this.newThread.category) {
        this.showToast('Por favor, seleccione una categoría', 'error');
        return;
      }

      const cat = this.categories.find(c => c.name === this.newThread.category);
      const payload = {
        title: this.newThread.title,
        content: this.newThread.content,
        categoryId: cat ? cat.id : null,
        userId: PLACEHOLDER_USER_ID,
        tags: this.newThread.tags ? this.newThread.tags.split(',').map(t => t.trim()) : [],
        visibility: 'PUBLIC',
        tenantTypeId: null
      };

      try {
        const res = await fetch(`${API}/threads`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload)
        });
        if (!res.ok) {
          const text = await res.text();
          this.showToast('Error al crear tema: ' + text, 'error');
          return;
        }
        const created = await res.json();
        this.showNewThreadModal = false;
        this.showToast('Tema creado exitosamente');
        this.newThread = { title: '', category: '', content: '', tags: '' };
        await this.loadThreads();
      } catch (err) {
        this.showToast('Error al crear tema', 'error');
      }
    },

    async addReply() {
      if (!this.newReply.trim()) {
        this.showToast('Por favor, escriba una respuesta', 'error');
        return;
      }
      if (!this.selectedThread) return;

      const payload = {
        content: this.newReply,
        userId: PLACEHOLDER_USER_ID,
        tenantTypeId: null
      };

      try {
        const res = await fetch(`${API}/threads/${this.selectedThread.id}/replies`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload)
        });
        if (!res.ok) {
          const text = await res.text();
          this.showToast('Error al crear respuesta: ' + text, 'error');
          return;
        }
        const created = await res.json();
        const post = {
          id: (created && created.id) || Date.now(),
          author: 'Anónimo',
          role: '',
          date: new Date().toISOString(),
          content: this.newReply
        };
        this.selectedThread.posts.push(post);
        this.selectedThread.replies = (this.selectedThread.replies || 0) + 1;
        this.newReply = '';
        this.showToast('Respuesta publicada exitosamente');
        await this.loadThreads();
      } catch (err) {
        this.showToast('Error al crear respuesta', 'error');
      }
    }
  }
};
</script>

<style scoped>
:root {
  --primary: #134686;    /* Azul principal */
  --secondary: #ED3F27;  /* Rojo/naranja para acentos */
  --accent: #FEB21A;     /* Amarillo para destacar */
  --light: #FDF4E3;      /* Fondo claro crema */
  --dark: #333333;       /* Texto oscuro */
  --success: #38a169;    /* Verde para confirmaciones */
  --warning: #d69e2e;    /* Amarillo para advertencias */
  --border: #e2e8f0;     /* Color de bordes */
}

* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
}

body {
  background-color: var(--light);
  color: var(--dark);
  line-height: 1.6;
}

#app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

/* Header */
header {
  background: linear-gradient(135deg, var(--primary) 0%, #0d3568 100%);
  color: white;
  padding: 1rem 0;
  box-shadow: 0 4px 12px rgba(19, 70, 134, 0.3);
  position: sticky;
  top: 0;
  z-index: 100;
}

.header-content {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 1.5rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.logo {
  display: flex;
  align-items: center;
  gap: 10px;
}

.logo h1 {
  font-size: 1.5rem;
  font-weight: 600;
}

.logo-icon {
  background-color: var(--accent);
  color: var(--primary);
  width: 40px;
  height: 40px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.user-info {
  display: flex;
  align-items: center;
  gap: 10px;
  background-color: rgba(255, 255, 255, 0.15);
  padding: 0.5rem 1rem;
  border-radius: 30px;
}

.avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background-color: var(--accent);
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  color: var(--primary);
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

/* Main Content */
main {
  flex: 1;
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem 1.5rem;
  width: 100%;
}

.forum-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  flex-wrap: wrap;
  gap: 1rem;
}

.forum-title h2 {
  color: var(--primary);
  font-size: 1.8rem;
  font-weight: 600;
}

.forum-title p {
  color: var(--dark);
  opacity: 0.8;
}

.btn {
  padding: 0.7rem 1.5rem;
  border: none;
  border-radius: 6px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.3s;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-size: 0.9rem;
}

.btn-primary {
  background-color: var(--secondary);
  color: white;
  box-shadow: 0 2px 8px rgba(237, 63, 39, 0.3);
}

.btn-primary:hover {
  background-color: #d6341f;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(237, 63, 39, 0.4);
}

.btn-outline {
  background-color: transparent;
  border: 1px solid var(--border);
  color: var(--dark);
}

.btn-outline:hover {
  background-color: rgba(253, 244, 227, 0.5);
}

.btn-accent {
  background-color: var(--accent);
  color: var(--primary);
  font-weight: 600;
}

.btn-accent:hover {
  background-color: #f0a500;
  transform: translateY(-2px);
}

/* Breadcrumb */
.breadcrumb {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 1.5rem;
  color: var(--dark);
  font-size: 0.9rem;
}

.breadcrumb a {
  color: var(--primary);
  text-decoration: none;
  font-weight: 500;
}

.breadcrumb a:hover {
  text-decoration: underline;
}

/* Forum Layout */
.forum-layout {
  display: grid;
  grid-template-columns: 250px 1fr;
  gap: 2rem;
}

/* Sidebar */
.sidebar {
  background-color: white;
  border-radius: 10px;
  padding: 1.5rem;
  box-shadow: 0 4px 12px rgba(0,0,0,0.08);
  height: fit-content;
  position: sticky;
  top: 100px;
  border: 1px solid rgba(253, 244, 227, 0.5);
}

.sidebar h3 {
  margin-bottom: 1rem;
  color: var(--primary);
  padding-bottom: 0.5rem;
  border-bottom: 2px solid var(--accent);
  font-weight: 600;
}

.categories {
  list-style: none;
}

.categories li {
  margin-bottom: 0.8rem;
}

.categories a {
  text-decoration: none;
  color: var(--dark);
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 0.5rem;
  border-radius: 5px;
  transition: all 0.2s;
  font-size: 0.9rem;
}

.categories a:hover, .categories a.active {
  background-color: rgba(254, 178, 26, 0.15);
  color: var(--primary);
}

.categories .badge {
  margin-left: auto;
  background-color: var(--secondary);
  color: white;
  padding: 0.2rem 0.5rem;
  border-radius: 20px;
  font-size: 0.7rem;
  min-width: 24px;
  text-align: center;
  font-weight: 600;
}

/* Search Box */
.search-box {
  margin-bottom: 1.5rem;
  position: relative;
}

.search-box input {
  width: 100%;
  padding: 0.7rem 1rem 0.7rem 2.5rem;
  border: 1px solid var(--border);
  border-radius: 6px;
  font-size: 0.9rem;
  background-color: #f9f9f9;
}

.search-box i {
  position: absolute;
  left: 0.8rem;
  top: 50%;
  transform: translateY(-50%);
  color: var(--secondary);
}

/* Forum Content */
.forum-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* Empty State */
.empty-state {
  text-align: center;
  padding: 3rem;
  background-color: white;
  border-radius: 10px;
  box-shadow: 0 4px 12px rgba(0,0,0,0.08);
  border: 1px solid rgba(253, 244, 227, 0.5);
}

.empty-state i {
  font-size: 3rem;
  color: var(--accent);
  margin-bottom: 1rem;
}

.empty-state h3 {
  color: var(--primary);
  margin-bottom: 0.5rem;
}

.empty-state p {
  color: var(--dark);
  margin-bottom: 1.5rem;
  opacity: 0.8;
}

/* Thread Card */
.thread-card {
  background-color: white;
  border-radius: 10px;
  padding: 1.5rem;
  box-shadow: 0 4px 12px rgba(0,0,0,0.08);
  transition: all 0.3s;
  cursor: pointer;
  border-left: 4px solid transparent;
  border: 1px solid rgba(253, 244, 227, 0.5);
}

.thread-card:hover {
  transform: translateY(-3px);
  box-shadow: 0 6px 16px rgba(0,0,0,0.12);
  border-left-color: var(--secondary);
}

.thread-card.unread {
  border-left-color: var(--accent);
  background-color: rgba(254, 178, 26, 0.05);
}

.thread-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1rem;
}

.thread-title {
  color: var(--primary);
  font-size: 1.2rem;
  margin-bottom: 0.5rem;
  font-weight: 600;
}

.thread-meta {
  display: flex;
  gap: 1rem;
  color: var(--dark);
  font-size: 0.9rem;
  flex-wrap: wrap;
  opacity: 0.8;
}

.thread-author {
  display: flex;
  align-items: center;
  gap: 8px;
}

.author-avatar {
  width: 30px;
  height: 30px;
  border-radius: 50%;
  background-color: var(--accent);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.8rem;
  font-weight: bold;
  color: var(--primary);
}

.thread-stats {
  display: flex;
  gap: 1rem;
}

.stat {
  display: flex;
  align-items: center;
  gap: 5px;
  color: var(--dark);
  font-size: 0.9rem;
  opacity: 0.8;
}

.thread-excerpt {
  color: var(--dark);
  margin-bottom: 1rem;
  line-height: 1.5;
  opacity: 0.9;
}

.thread-tags {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.tag {
  background-color: rgba(19, 70, 134, 0.1);
  color: var(--primary);
  padding: 0.3rem 0.7rem;
  border-radius: 20px;
  font-size: 0.8rem;
  font-weight: 500;
}

/* New Thread Modal */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0,0,0,0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.modal {
  background-color: white;
  border-radius: 10px;
  width: 100%;
  max-width: 600px;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 10px 25px rgba(0,0,0,0.2);
  border: 1px solid rgba(253, 244, 227, 0.5);
}

.modal-header {
  padding: 1.5rem;
  border-bottom: 1px solid var(--border);
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: linear-gradient(135deg, var(--primary) 0%, #0d3568 100%);
  color: white;
  border-radius: 10px 10px 0 0;
}

.modal-header h3 {
  font-weight: 600;
}

.close-btn {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: white;
  transition: color 0.2s;
}

.close-btn:hover {
  color: var(--accent);
}

.modal-body {
  padding: 1.5rem;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  color: var(--primary);
  font-weight: 500;
}

.form-control {
  width: 100%;
  padding: 0.8rem;
  border: 1px solid var(--border);
  border-radius: 6px;
  font-size: 1rem;
  transition: border-color 0.2s;
  background-color: #f9f9f9;
}

.form-control:focus {
  outline: none;
  border-color: var(--accent);
  box-shadow: 0 0 0 3px rgba(254, 178, 26, 0.1);
}

textarea.form-control {
  min-height: 150px;
  resize: vertical;
  line-height: 1.5;
}

.modal-footer {
  padding: 1.5rem;
  border-top: 1px solid var(--border);
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
}

/* Thread View */
.thread-view {
  background-color: white;
  border-radius: 10px;
  overflow: hidden;
  box-shadow: 0 4px 12px rgba(0,0,0,0.08);
  border: 1px solid rgba(253, 244, 227, 0.5);
}

.thread-view-header {
  padding: 1.5rem;
  background: linear-gradient(135deg, var(--primary) 0%, #0d3568 100%);
  color: white;
  border-bottom: 1px solid var(--border);
}

.thread-view-title {
  font-size: 1.5rem;
  margin-bottom: 0.5rem;
  font-weight: 600;
}

.thread-view-meta {
  display: flex;
  gap: 1rem;
  font-size: 0.9rem;
  flex-wrap: wrap;
  opacity: 0.9;
}

.posts {
  padding: 0;
}

.post {
  padding: 1.5rem;
  border-bottom: 1px solid var(--border);
  transition: background-color 0.2s;
}

.post:hover {
  background-color: rgba(253, 244, 227, 0.3);
}

.post:last-child {
  border-bottom: none;
}

.post-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.post-author {
  display: flex;
  align-items: center;
  gap: 10px;
}

.post-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background-color: var(--accent);
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  color: var(--primary);
}

.author-info h4 {
  color: var(--primary);
  font-weight: 600;
}

.author-info span {
  color: var(--dark);
  font-size: 0.9rem;
  opacity: 0.8;
}

.post-date {
  color: var(--dark);
  font-size: 0.9rem;
  opacity: 0.8;
}

.post-content {
  color: var(--dark);
  line-height: 1.6;
}

.post-actions {
  display: flex;
  gap: 1rem;
  margin-top: 1rem;
}

.action-btn {
  background: none;
  border: none;
  color: var(--dark);
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 0.9rem;
  transition: color 0.2s;
  opacity: 0.8;
}

.action-btn:hover {
  color: var(--secondary);
  opacity: 1;
}

.reply-form {
  padding: 1.5rem;
  border-top: 1px solid var(--border);
  background-color: rgba(253, 244, 227, 0.3);
}

/* Toast Notifications */
.toast {
  position: fixed;
  bottom: 20px;
  right: 20px;
  padding: 1rem 1.5rem;
  border-radius: 6px;
  color: white;
  box-shadow: 0 4px 12px rgba(0,0,0,0.15);
  z-index: 1100;
  display: flex;
  align-items: center;
  gap: 10px;
  max-width: 350px;
  animation: slideIn 0.3s ease-out;
}

.toast.success {
  background-color: var(--success);
}

.toast.error {
  background-color: var(--secondary);
}

@keyframes slideIn {
  from {
    transform: translateX(100%);
    opacity: 0;
  }
  to {
    transform: translateX(0);
    opacity: 1;
  }
}

/* Footer */
footer {
  background: linear-gradient(135deg, var(--primary) 0%, #0d3568 100%);
  color: white;
  padding: 2rem 0;
  margin-top: auto;
}

.footer-content {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 1.5rem;
  text-align: center;
}

/* Responsive */
@media (max-width: 768px) {
  .forum-layout {
    grid-template-columns: 1fr;
  }
  
  .sidebar {
    order: -1;
    position: static;
  }
  
  .thread-header {
    flex-direction: column;
    gap: 1rem;
  }
  
  .thread-stats {
    align-self: flex-start;
  }
  
  .forum-header {
    flex-direction: column;
    align-items: flex-start;
  }
  
  .user-info span {
    display: none;
  }
}
</style>