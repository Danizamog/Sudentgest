<template>
  <div class="forum-container">
    <!-- Header -->
    <header class="forum-header">
      <div class="header-content">
        <div class="logo">
          <div class="logo-icon">SG</div>
          <h1>StudentGest</h1>
        </div>
        <div class="user-info" @click="toggleUserMenu">
          <div class="avatar">MP</div>
          <span>María Pérez (Maestra)</span>
          <i class="fas fa-chevron-down"></i>
        </div>
      </div>
    </header>

    <!-- Main Content -->
    <main class="forum-main">
      <div class="forum-actions">
        <div class="forum-title">
          <h2>Foro de Padres y Maestros</h2>
          <p>Comparte opiniones, ideas y experiencias</p>
        </div>
        <button class="btn btn-primary pulse" @click="showNewThreadModal = true">
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
            <input 
              v-model="searchQuery" 
              type="text" 
              placeholder="Buscar en el foro..."
              @input="debounceSearch"
            >
          </div>
          
          <div class="sidebar-section">
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
          </div>

          <div class="sidebar-section">
            <h3>Estadísticas</h3>
            <div class="stats">
              <div class="stat-item">
                <span class="stat-number">{{ threads.length }}</span>
                <span class="stat-label">Temas</span>
              </div>
              <div class="stat-item">
                <span class="stat-number">{{ totalPosts }}</span>
                <span class="stat-label">Respuestas</span>
              </div>
              <div class="stat-item">
                <span class="stat-number">{{ onlineUsers }}</span>
                <span class="stat-label">En línea</span>
              </div>
            </div>
          </div>
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
                 :class="{ unread: thread.unread, pinned: thread.isPinned }"
                 @click="selectThread(thread)">
              <div class="thread-header">
                <div>
                  <h3 class="thread-title">
                    <span v-if="thread.isPinned" class="pinned-icon">
                      <i class="fas fa-thumbtack"></i>
                    </span>
                    {{ thread.title }}
                  </h3>
                  <div class="thread-meta">
                    <div class="thread-author">
                      <div class="author-avatar">{{ getInitials(thread.author) }}</div>
                      {{ thread.author }}
                    </div>
                    <span>en {{ thread.category }}</span>
                    <span>{{ formatDate(thread.date) }}</span>
                    <span v-if="thread.unread" class="tag new-tag">Nuevo</span>
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
              <h2 class="thread-view-title">
                <span v-if="selectedThread.isPinned" class="pinned-icon">
                  <i class="fas fa-thumbtack"></i>
                </span>
                {{ selectedThread.title }}
              </h2>
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
                  <button class="action-btn" @click="toggleLike(post)">
                    <i class="far fa-thumbs-up"></i> Me gusta ({{ post.likes }})
                  </button>
                  <button class="action-btn" @click="quotePost(post)">
                    <i class="far fa-comment"></i> Responder
                  </button>
                  <button class="action-btn" v-if="post.author === 'María Pérez'">
                    <i class="fas fa-edit"></i> Editar
                  </button>
                </div>
              </div>
            </div>

            <div class="reply-form">
              <div class="form-group">
                <label for="reply">Tu respuesta</label>
                <textarea id="reply" class="form-control" v-model="newReply" placeholder="Escribe tu respuesta aquí..."></textarea>
              </div>
              <div class="reply-actions">
                <button class="btn btn-outline" @click="selectedThread = null">
                  <i class="fas fa-arrow-left"></i> Volver al foro
                </button>
                <button class="btn btn-primary" @click="addReply" :disabled="!newReply.trim()">
                  <i class="fas fa-paper-plane"></i> Publicar respuesta
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </main>

    <!-- New Thread Modal -->
    <div v-if="showNewThreadModal" class="modal-overlay" @click.self="showNewThreadModal = false">
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
        <p>StudentGest - Sistema de Gestión Estudiantil &copy; 2024</p>
        <p>Foro de Padres y Maestros - Conectando la comunidad educativa</p>
      </div>
    </footer>
  </div>
</template>

<script>
import { ref, computed, onMounted } from 'vue'

export default {
  name: 'Foro',
  setup() {
    // Variables CSS personalizadas
    const cssVars = {
      '--primary': '#10375C',
      '--secondary': '#EB8317',
      '--accent': '#F3C623',
      '--light': '#F4F6FF',
      '--dark': '#2D3748',
      '--success': '#38A169',
      '--warning': '#D69E2E',
      '--border': '#E2E8F0',
      '--shadow': 'rgba(16, 55, 92, 0.1)',
      '--gradient': 'linear-gradient(135deg, var(--primary) 0%, #0A2A4A 100%)'
    }
    
    // Aplicar variables CSS al montar el componente
    onMounted(() => {
      Object.keys(cssVars).forEach(key => {
        document.documentElement.style.setProperty(key, cssVars[key])
      })
    })

    // Datos de ejemplo actualizados
    const categories = ref([
      { id: 'all', name: 'Todos los temas', icon: 'fas fa-list', threadCount: 12 },
      { id: 'general', name: 'General', icon: 'fas fa-comments', threadCount: 5 },
      { id: 'homework', name: 'Tareas y deberes', icon: 'fas fa-book', threadCount: 3 },
      { id: 'events', name: 'Eventos escolares', icon: 'fas fa-calendar', threadCount: 2 },
      { id: 'academic', name: 'Rendimiento académico', icon: 'fas fa-chart-line', threadCount: 1 },
      { id: 'behavior', name: 'Comportamiento', icon: 'fas fa-users', threadCount: 1 }
    ])

    const threads = ref([
      {
        id: '1',
        title: 'Nuevo sistema de evaluación para el tercer trimestre',
        content: 'Quisiera compartir con ustedes el nuevo sistema de evaluación que implementaremos este trimestre. Hemos decidido incorporar más evaluaciones prácticas para medir mejor las competencias de los estudiantes.',
        excerpt: 'Quisiera compartir con ustedes el nuevo sistema de evaluación que implementaremos este trimestre...',
        category: 'Rendimiento académico',
        author: 'Ana Rodríguez',
        authorRole: 'Maestra de Matemáticas',
        date: '2024-09-15',
        lastActivity: '2024-09-16',
        tags: ['evaluación', 'trimestre', 'calificaciones'],
        replies: 8,
        views: 42,
        unread: true,
        isPinned: true,
        posts: [
          {
            id: 1,
            author: 'Ana Rodríguez',
            role: 'Maestra de Matemáticas',
            date: '2024-09-15',
            content: 'Quisiera compartir con ustedes el nuevo sistema de evaluación que implementaremos este trimestre. Hemos decidido incorporar más evaluaciones prácticas para medir mejor las competencias de los estudiantes.',
            likes: 5,
            isLiked: false
          },
          {
            id: 2,
            author: 'Carlos Méndez',
            role: 'Padre de familia',
            date: '2024-09-16',
            content: 'Me parece una excelente idea. ¿Podría explicar más sobre cómo serán estas evaluaciones prácticas?',
            likes: 2,
            isLiked: false
          },
          {
            id: 3,
            author: 'Laura González',
            role: 'Maestra de Ciencias',
            date: '2024-09-16',
            content: 'Comparto la inquietud de Carlos. En el área de ciencias estamos considerando algo similar, me gustaría conocer más detalles.',
            likes: 3,
            isLiked: false
          }
        ]
      },
      {
        id: '2',
        title: 'Preparativos para la feria de ciencias anual',
        content: 'Estamos organizando la feria de ciencias de este año y necesitamos la colaboración de todos. La fecha tentativa es el 15 de noviembre.',
        excerpt: 'Estamos organizando la feria de ciencias de este año y necesitamos la colaboración de todos...',
        category: 'Eventos escolares',
        author: 'Miguel Torres',
        authorRole: 'Coordinador de Ciencias',
        date: '2024-09-10',
        lastActivity: '2024-09-11',
        tags: ['feria', 'ciencias', 'evento'],
        replies: 5,
        views: 31,
        unread: false,
        isPinned: true,
        posts: [
          {
            id: 1,
            author: 'Miguel Torres',
            role: 'Coordinador de Ciencias',
            date: '2024-09-10',
            content: 'Estamos organizando la feria de ciencias de este año y necesitamos la colaboración de todos. La fecha tentativa es el 15 de noviembre.',
            likes: 3,
            isLiked: false
          },
          {
            id: 2,
            author: 'Elena Vargas',
            role: 'Madre de familia',
            date: '2024-09-11',
            content: 'Mi hija está muy emocionada. ¿En qué podemos ayudar los padres?',
            likes: 1,
            isLiked: false
          }
        ]
      },
      {
        id: '3',
        title: 'Dudas sobre las tareas de matemáticas',
        content: 'Mi hijo tiene dificultades con los problemas de matemáticas de la página 45. ¿Alguien podría explicar el método que están usando?',
        excerpt: 'Mi hijo tiene dificultades con los problemas de matemáticas de la página 45...',
        category: 'Tareas y deberes',
        author: 'Roberto Sánchez',
        authorRole: 'Padre de familia',
        date: '2024-09-05',
        lastActivity: '2024-09-06',
        tags: ['matemáticas', 'tareas', 'dudas'],
        replies: 12,
        views: 67,
        unread: true,
        isPinned: false,
        posts: [
          {
            id: 1,
            author: 'Roberto Sánchez',
            role: 'Padre de familia',
            date: '2024-09-05',
            content: 'Mi hijo tiene dificultades con los problemas de matemáticas de la página 45. ¿Alguien podría explicar el método que están usando?',
            likes: 1,
            isLiked: false
          },
          {
            id: 2,
            author: 'Ana Rodríguez',
            role: 'Maestra de Matemáticas',
            date: '2024-09-06',
            content: 'Claro que sí, Roberto. Estamos usando el método de resolución por pasos. Puedo enviarle una guía por correo electrónico.',
            likes: 4,
            isLiked: false
          }
        ]
      }
    ])

    // Estado de la aplicación
    const selectedCategory = ref('all')
    const selectedThread = ref(null)
    const showNewThreadModal = ref(false)
    const searchQuery = ref('')
    const newReply = ref('')
    
    // Toast notification
    const toast = ref({
      show: false,
      message: '',
      type: 'success'
    })

    const newThread = ref({
      title: '',
      category: '',
      content: '',
      tags: ''
    })

    // Computed properties
    const filteredThreads = computed(() => {
      let filtered = threads.value

      if (selectedCategory.value !== 'all') {
        const category = categories.value.find(c => c.id === selectedCategory.value)
        filtered = filtered.filter(thread => thread.category === category.name)
      }

      if (searchQuery.value) {
        const query = searchQuery.value.toLowerCase()
        filtered = filtered.filter(thread => 
          thread.title.toLowerCase().includes(query) ||
          thread.content.toLowerCase().includes(query) ||
          thread.tags.some(tag => tag.toLowerCase().includes(query))
        )
      }

      // Ordenar por fecha (más recientes primero)
      filtered.sort((a, b) => new Date(b.date) - new Date(a.date))

      return filtered
    })

    const totalPosts = computed(() => {
      return threads.value.reduce((total, thread) => total + thread.replies, 0)
    })

    const onlineUsers = computed(() => 12)

    // Métodos
    const selectCategory = (categoryId) => {
      selectedCategory.value = categoryId
      selectedThread.value = null
    }

    const selectThread = (thread) => {
      selectedThread.value = thread
      thread.views++
      thread.unread = false // Marcar como leído
    }

    const getInitials = (name) => {
      return name.split(' ').map(n => n[0]).join('').toUpperCase()
    }

    const formatDate = (dateString) => {
      const options = { year: 'numeric', month: 'long', day: 'numeric' }
      return new Date(dateString).toLocaleDateString('es-ES', options)
    }

    // Mostrar toast
    const showToast = (message, type = 'success') => {
      toast.value = { show: true, message, type }
      setTimeout(() => {
        toast.value.show = false
      }, 3000)
    }

    const createThread = () => {
      if (!newThread.value.title || !newThread.value.content) {
        showToast('Por favor, complete el título y el contenido del tema', 'error')
        return
      }
      
      if (!newThread.value.category) {
        showToast('Por favor, seleccione una categoría', 'error')
        return
      }

      const tagsArray = newThread.value.tags ? 
        newThread.value.tags.split(',').map(tag => tag.trim()) : []

      const newThreadObj = {
        id: Date.now().toString(),
        title: newThread.value.title,
        content: newThread.value.content,
        excerpt: newThread.value.content.substring(0, 100) + '...',
        category: newThread.value.category,
        author: 'María Pérez',
        authorRole: 'Maestra',
        date: new Date().toISOString().split('T')[0],
        lastActivity: new Date().toISOString().split('T')[0],
        tags: tagsArray,
        replies: 0,
        views: 0,
        unread: false,
        isPinned: false,
        posts: [
          {
            id: 1,
            author: 'María Pérez',
            role: 'Maestra',
            date: new Date().toISOString().split('T')[0],
            content: newThread.value.content,
            likes: 0,
            isLiked: false
          }
        ]
      }

      threads.value.unshift(newThreadObj)
      showNewThreadModal.value = false
      showToast('Tema creado exitosamente')
      
      // Reset form
      newThread.value = {
        title: '',
        category: '',
        content: '',
        tags: ''
      }
    }

    const addReply = () => {
      if (!newReply.value.trim()) {
        showToast('Por favor, escriba una respuesta', 'error')
        return
      }

      const newPost = {
        id: selectedThread.value.posts.length + 1,
        author: 'María Pérez',
        role: 'Maestra',
        date: new Date().toISOString().split('T')[0],
        content: newReply.value,
        likes: 0,
        isLiked: false
      }

      selectedThread.value.posts.push(newPost)
      selectedThread.value.replies++
      selectedThread.value.lastActivity = new Date().toISOString().split('T')[0]
      newReply.value = ''
      showToast('Respuesta publicada exitosamente')
    }

    const toggleLike = (post) => {
      post.isLiked = !post.isLiked
      post.likes += post.isLiked ? 1 : -1
      showToast(post.isLiked ? '¡Me gusta agregado!' : 'Me gusta eliminado')
    }

    const quotePost = (post) => {
      newReply.value = `@${post.author} dijo: "${post.content.substring(0, 100)}..."\n\n`
      showToast('Puedes citar esta respuesta en tu mensaje')
    }

    const toggleUserMenu = () => {
      showToast('Menú de usuario', 'success')
    }

    const debounceSearch = () => {
      // Implementación simple de debounce para búsqueda
      clearTimeout(window.searchTimeout)
      window.searchTimeout = setTimeout(() => {
        // La búsqueda se realiza automáticamente gracias a computed property
      }, 300)
    }

    onMounted(() => {
      console.log('✅ Foro cargado correctamente')
    })

    return {
      categories,
      threads,
      selectedCategory,
      selectedThread,
      showNewThreadModal,
      searchQuery,
      newReply,
      newThread,
      toast,
      filteredThreads,
      totalPosts,
      onlineUsers,
      selectCategory,
      selectThread,
      getInitials,
      formatDate,
      createThread,
      addReply,
      toggleLike,
      quotePost,
      toggleUserMenu,
      debounceSearch,
      showToast
    }
  }
}
</script>

<style scoped>
/* Variables CSS */
:root {
  --primary: #10375C;
  --secondary: #EB8317;
  --accent: #F3C623;
  --light: #F4F6FF;
  --dark: #2D3748;
  --success: #38A169;
  --warning: #D69E2E;
  --border: #E2E8F0;
  --shadow: rgba(16, 55, 92, 0.1);
  --gradient: linear-gradient(135deg, var(--primary) 0%, #0A2A4A 100%);
}

.forum-container {
  min-height: 100vh;
  background-color: var(--light);
  color: var(--dark);
  display: flex;
  flex-direction: column;
  font-family: 'Inter', 'Segoe UI', system-ui, sans-serif;
}

/* Header */
.forum-header {
  background: var(--gradient);
  color: white;
  padding: 1rem 0;
  box-shadow: 0 4px 20px var(--shadow);
  position: sticky;
  top: 0;
  z-index: 100;
  border-bottom: 4px solid var(--accent);
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
  gap: 12px;
  transition: transform 0.3s ease;
}

.logo:hover {
  transform: translateY(-2px);
}

.logo h1 {
  font-size: 1.6rem;
  font-weight: 700;
  background: linear-gradient(135deg, var(--accent) 0%, var(--secondary) 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.logo-icon {
  background: var(--accent);
  color: var(--primary);
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  font-size: 1.2rem;
  box-shadow: 0 4px 12px rgba(243, 198, 35, 0.3);
}

.user-info {
  display: flex;
  align-items: center;
  gap: 12px;
  background-color: rgba(255, 255, 255, 0.15);
  padding: 0.6rem 1.2rem;
  border-radius: 50px;
  transition: all 0.3s ease;
  cursor: pointer;
  backdrop-filter: blur(10px);
}

.user-info:hover {
  background-color: rgba(255, 255, 255, 0.25);
  transform: translateY(-2px);
}

.avatar {
  width: 44px;
  height: 44px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--accent) 0%, var(--secondary) 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  color: var(--primary);
  box-shadow: 0 4px 8px rgba(0,0,0,0.2);
  font-size: 1.1rem;
}

/* Main Content */
.forum-main {
  flex: 1;
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem 1.5rem;
  width: 100%;
}

.forum-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  flex-wrap: wrap;
  gap: 1rem;
}

.forum-title h2 {
  color: var(--primary);
  font-size: 2rem;
  font-weight: 700;
  margin-bottom: 0.5rem;
  background: linear-gradient(135deg, var(--primary) 0%, var(--secondary) 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.forum-title p {
  color: var(--dark);
  opacity: 0.8;
  font-size: 1.1rem;
  font-weight: 500;
}

/* Breadcrumb */
.breadcrumb {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 1.5rem;
  color: var(--dark);
  font-size: 0.9rem;
  padding: 1rem 1.2rem;
  background-color: white;
  border-radius: 12px;
  box-shadow: 0 2px 8px var(--shadow);
  border-left: 4px solid var(--accent);
}

.breadcrumb a {
  color: var(--primary);
  text-decoration: none;
  font-weight: 600;
  transition: color 0.2s;
}

.breadcrumb a:hover {
  color: var(--secondary);
}

/* Buttons */
.btn {
  padding: 0.8rem 1.8rem;
  border: none;
  border-radius: 10px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-size: 0.95rem;
  position: relative;
  overflow: hidden;
}

.btn::before {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(255,255,255,0.2), transparent);
  transition: left 0.5s;
}

.btn:hover::before {
  left: 100%;
}

.btn-primary {
  background: linear-gradient(135deg, var(--secondary) 0%, #D9730D 100%);
  color: white;
  box-shadow: 0 4px 15px rgba(235, 131, 23, 0.3);
}

.btn-primary:hover {
  transform: translateY(-3px);
  box-shadow: 0 6px 20px rgba(235, 131, 23, 0.4);
}

.btn-outline {
  background-color: transparent;
  border: 2px solid var(--border);
  color: var(--dark);
}

.btn-outline:hover {
  background-color: rgba(244, 246, 255, 0.5);
  transform: translateY(-2px);
  border-color: var(--secondary);
}

.btn-accent {
  background: linear-gradient(135deg, var(--accent) 0%, #E5B61A 100%);
  color: var(--primary);
  font-weight: 700;
}

.btn-accent:hover {
  transform: translateY(-3px);
  box-shadow: 0 6px 20px rgba(243, 198, 35, 0.3);
}

/* Forum Layout */
.forum-layout {
  display: grid;
  grid-template-columns: 300px 1fr;
  gap: 2rem;
}

/* Sidebar */
.sidebar {
  background-color: white;
  border-radius: 16px;
  padding: 1.8rem;
  box-shadow: 0 8px 25px var(--shadow);
  height: fit-content;
  position: sticky;
  top: 120px;
  border: 1px solid rgba(244, 246, 255, 0.8);
}

.sidebar-section {
  margin-bottom: 2rem;
}

.sidebar-section h3 {
  margin-bottom: 1.2rem;
  color: var(--primary);
  padding-bottom: 0.8rem;
  border-bottom: 3px solid var(--accent);
  font-weight: 700;
  font-size: 1.2rem;
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
  gap: 10px;
  padding: 0.8rem 1rem;
  border-radius: 10px;
  transition: all 0.3s;
  font-size: 0.95rem;
  font-weight: 500;
}

.categories a:hover, .categories a.active {
  background: linear-gradient(135deg, rgba(243, 198, 35, 0.15) 0%, rgba(235, 131, 23, 0.1) 100%);
  color: var(--primary);
  transform: translateX(8px);
  box-shadow: 0 4px 12px rgba(16, 55, 92, 0.1);
}

.categories .badge {
  margin-left: auto;
  background: var(--secondary);
  color: white;
  padding: 0.3rem 0.7rem;
  border-radius: 20px;
  font-size: 0.8rem;
  min-width: 28px;
  text-align: center;
  font-weight: 700;
  box-shadow: 0 2px 4px rgba(235, 131, 23, 0.3);
}

/* Search Box */
.search-box {
  margin-bottom: 1.8rem;
  position: relative;
}

.search-box input {
  width: 100%;
  padding: 0.9rem 1rem 0.9rem 3rem;
  border: 2px solid var(--border);
  border-radius: 12px;
  font-size: 0.95rem;
  background-color: var(--light);
  transition: all 0.3s;
  font-weight: 500;
}

.search-box input:focus {
  outline: none;
  border-color: var(--accent);
  box-shadow: 0 0 0 4px rgba(243, 198, 35, 0.15);
  background-color: white;
}

.search-box i {
  position: absolute;
  left: 1rem;
  top: 50%;
  transform: translateY(-50%);
  color: var(--secondary);
  font-size: 1.1rem;
}

/* Stats */
.stats {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1rem;
}

.stat-item {
  text-align: center;
  padding: 1.2rem 0.8rem;
  background: linear-gradient(135deg, var(--light) 0%, #E8ECFF 100%);
  border-radius: 12px;
  transition: transform 0.3s;
}

.stat-item:hover {
  transform: translateY(-3px);
}

.stat-number {
  display: block;
  font-size: 1.8rem;
  font-weight: 800;
  color: var(--primary);
  margin-bottom: 0.3rem;
}

.stat-label {
  font-size: 0.8rem;
  color: var(--dark);
  opacity: 0.8;
  font-weight: 600;
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
  padding: 3rem 2rem;
  background-color: white;
  border-radius: 16px;
  box-shadow: 0 8px 25px var(--shadow);
  border: 2px dashed var(--border);
}

.empty-state i {
  font-size: 4rem;
  background: linear-gradient(135deg, var(--accent) 0%, var(--secondary) 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  margin-bottom: 1.5rem;
}

.empty-state h3 {
  color: var(--primary);
  margin-bottom: 0.8rem;
  font-size: 1.5rem;
}

.empty-state p {
  color: var(--dark);
  margin-bottom: 2rem;
  opacity: 0.8;
  font-size: 1.1rem;
}

/* Thread Card */
.thread-card {
  background-color: white;
  border-radius: 16px;
  padding: 1.8rem;
  box-shadow: 0 4px 15px var(--shadow);
  transition: all 0.4s;
  cursor: pointer;
  border-left: 5px solid transparent;
  position: relative;
  overflow: hidden;
}

.thread-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 4px;
  background: linear-gradient(90deg, var(--primary), var(--accent), var(--secondary));
  transform: scaleX(0);
  transition: transform 0.4s;
}

.thread-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 12px 30px var(--shadow);
}

.thread-card:hover::before {
  transform: scaleX(1);
}

.thread-card.unread {
  border-left-color: var(--accent);
  background: linear-gradient(135deg, white 0%, rgba(243, 198, 35, 0.05) 100%);
}

.thread-card.pinned {
  border-left-color: var(--secondary);
  background: linear-gradient(135deg, white 0%, rgba(235, 131, 23, 0.05) 100%);
}

.thread-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1.2rem;
}

.thread-title {
  color: var(--primary);
  font-size: 1.3rem;
  margin-bottom: 0.8rem;
  font-weight: 700;
  line-height: 1.4;
}

.pinned-icon {
  color: var(--secondary);
  margin-right: 8px;
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
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--accent) 0%, var(--secondary) 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.9rem;
  font-weight: bold;
  color: var(--primary);
  box-shadow: 0 2px 6px rgba(0,0,0,0.1);
}

.thread-stats {
  display: flex;
  gap: 1.2rem;
}

.stat {
  display: flex;
  align-items: center;
  gap: 6px;
  color: var(--dark);
  font-size: 0.9rem;
  opacity: 0.8;
  font-weight: 500;
}

.thread-excerpt {
  color: var(--dark);
  margin-bottom: 1.2rem;
  line-height: 1.6;
  opacity: 0.9;
  font-size: 1rem;
}

.thread-tags {
  display: flex;
  gap: 0.6rem;
  flex-wrap: wrap;
}

.tag {
  background: linear-gradient(135deg, rgba(16, 55, 92, 0.1) 0%, rgba(16, 55, 92, 0.05) 100%);
  color: var(--primary);
  padding: 0.4rem 0.9rem;
  border-radius: 20px;
  font-size: 0.85rem;
  font-weight: 600;
  transition: all 0.2s;
  border: 1px solid rgba(16, 55, 92, 0.1);
}

.tag:hover {
  background: linear-gradient(135deg, rgba(235, 131, 23, 0.15) 0%, rgba(235, 131, 23, 0.1) 100%);
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(235, 131, 23, 0.2);
}

.new-tag {
  background: var(--accent);
  color: var(--primary);
  font-weight: 700;
}

/* Thread View */
.thread-view {
  background-color: white;
  border-radius: 20px;
  overflow: hidden;
  box-shadow: 0 8px 25px var(--shadow);
  border: 1px solid rgba(244, 246, 255, 0.8);
  animation: fadeIn 0.4s ease-out;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.thread-view-header {
  padding: 2rem;
  background: var(--gradient);
  color: white;
  border-bottom: 1px solid var(--border);
}

.thread-view-title {
  font-size: 1.8rem;
  margin-bottom: 1rem;
  font-weight: 700;
  line-height: 1.3;
}

.thread-view-meta {
  display: flex;
  gap: 1.5rem;
  font-size: 0.95rem;
  flex-wrap: wrap;
  opacity: 0.9;
}

/* Posts */
.posts {
  padding: 0;
}

.post {
  padding: 2rem;
  border-bottom: 1px solid var(--border);
  transition: background-color 0.3s;
  animation: slideIn 0.4s ease-out;
}

@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateX(-10px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

.post:hover {
  background-color: rgba(244, 246, 255, 0.5);
}

.post:last-child {
  border-bottom: none;
}

.post-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.2rem;
}

.post-author {
  display: flex;
  align-items: center;
  gap: 12px;
}

.post-avatar {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--accent) 0%, var(--secondary) 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  color: var(--primary);
  box-shadow: 0 4px 12px rgba(0,0,0,0.15);
  font-size: 1.1rem;
}

.author-info h4 {
  color: var(--primary);
  font-weight: 700;
  font-size: 1.1rem;
}

.author-info span {
  color: var(--dark);
  font-size: 0.9rem;
  opacity: 0.8;
  font-weight: 500;
}

.post-date {
  color: var(--dark);
  font-size: 0.9rem;
  opacity: 0.8;
  font-weight: 500;
}

.post-content {
  color: var(--dark);
  line-height: 1.7;
  font-size: 1.05rem;
}

.post-actions {
  display: flex;
  gap: 1.2rem;
  margin-top: 1.5rem;
}

.action-btn {
  background: none;
  border: none;
  color: var(--dark);
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 0.9rem;
  transition: all 0.2s;
  opacity: 0.8;
  padding: 0.6rem 1rem;
  border-radius: 8px;
  font-weight: 500;
}

.action-btn:hover {
  color: var(--secondary);
  opacity: 1;
  background-color: rgba(244, 246, 255, 0.8);
  transform: translateY(-2px);
}

/* Reply Form */
.reply-form {
  padding: 2rem;
  border-top: 1px solid var(--border);
  background: linear-gradient(135deg, rgba(244, 246, 255, 0.5) 0%, rgba(244, 246, 255, 0.8) 100%);
}

.reply-actions {
  display: flex;
  gap: 12px;
  justify-content: flex-end;
}

.form-group {
  margin-bottom: 1.8rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.8rem;
  color: var(--primary);
  font-weight: 600;
  font-size: 1rem;
}

.form-control {
  width: 100%;
  padding: 1rem;
  border: 2px solid var(--border);
  border-radius: 12px;
  font-size: 1rem;
  transition: all 0.3s;
  background-color: var(--light);
  font-weight: 500;
}

.form-control:focus {
  outline: none;
  border-color: var(--accent);
  box-shadow: 0 0 0 4px rgba(243, 198, 35, 0.15);
  background-color: white;
}

textarea.form-control {
  min-height: 160px;
  resize: vertical;
  line-height: 1.6;
}

/* Modal */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(16, 55, 92, 0.8);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
  backdrop-filter: blur(8px);
}

.modal {
  background-color: white;
  border-radius: 20px;
  width: 100%;
  max-width: 650px;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 20px 50px rgba(16, 55, 92, 0.3);
  border: 1px solid rgba(244, 246, 255, 0.8);
  animation: modalAppear 0.4s ease-out;
}

@keyframes modalAppear {
  from {
    opacity: 0;
    transform: scale(0.9) translateY(-20px);
  }
  to {
    opacity: 1;
    transform: scale(1) translateY(0);
  }
}

.modal-header {
  padding: 1.8rem;
  border-bottom: 1px solid var(--border);
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: var(--gradient);
  color: white;
  border-radius: 20px 20px 0 0;
}

.modal-header h3 {
  font-weight: 700;
  font-size: 1.4rem;
}

.close-btn {
  background: rgba(255, 255, 255, 0.2);
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: white;
  transition: all 0.3s;
  padding: 0.5rem;
  border-radius: 50%;
  width: 44px;
  height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
  backdrop-filter: blur(10px);
}

.close-btn:hover {
  color: var(--accent);
  background: rgba(255, 255, 255, 0.3);
  transform: rotate(90deg);
}

.modal-body {
  padding: 2rem;
}

.modal-footer {
  padding: 1.8rem;
  border-top: 1px solid var(--border);
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
}

/* Toast Notification */
.toast {
  position: fixed;
  bottom: 25px;
  right: 25px;
  padding: 1.2rem 1.8rem;
  border-radius: 12px;
  color: white;
  box-shadow: 0 8px 25px rgba(0,0,0,0.15);
  z-index: 1100;
  display: flex;
  align-items: center;
  gap: 12px;
  max-width: 380px;
  animation: slideInRight 0.4s ease-out;
  backdrop-filter: blur(10px);
}

@keyframes slideInRight {
  from {
    transform: translateX(100%);
    opacity: 0;
  }
  to {
    transform: translateX(0);
    opacity: 1;
  }
}

.toast.success {
  background: linear-gradient(135deg, var(--success) 0%, #2F855A 100%);
}

.toast.error {
  background: linear-gradient(135deg, var(--secondary) 0%, #D9730D 100%);
}

/* Footer */
footer {
  background: var(--gradient);
  color: white;
  padding: 2.5rem 0;
  margin-top: auto;
  border-top: 4px solid var(--accent);
}

.footer-content {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 1.5rem;
  text-align: center;
}

.footer-content p {
  margin-bottom: 0.5rem;
  opacity: 0.9;
}

/* Animations */
.pulse {
  animation: pulse 2s infinite;
}

@keyframes pulse {
  0% {
    transform: scale(0.95);
    box-shadow: 0 0 0 0 rgba(243, 198, 35, 0.7);
  }
  
  70% {
    transform: scale(1);
    box-shadow: 0 0 0 12px rgba(243, 198, 35, 0);
  }
  
  100% {
    transform: scale(0.95);
    box-shadow: 0 0 0 0 rgba(243, 198, 35, 0);
  }
}

/* Responsive */
@media (max-width: 968px) {
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
  
  .forum-actions {
    flex-direction: column;
    align-items: flex-start;
  }
  
  .user-info span {
    display: none;
  }
  
  .modal-footer {
    flex-direction: column;
  }
  
  .thread-view-meta {
    flex-direction: column;
    gap: 0.5rem;
  }
  
  .post-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 1rem;
  }
  
  .reply-actions {
    flex-direction: column;
  }
}

@media (max-width: 480px) {
  .forum-main {
    padding: 1.5rem 1rem;
  }
  
  .header-content {
    padding: 0 1rem;
  }
  
  .thread-card, .post {
    padding: 1.5rem;
  }
  
  .sidebar {
    padding: 1.5rem;
  }
  
  .stats {
    grid-template-columns: 1fr;
  }
}

/* Scroll personalizado */
::-webkit-scrollbar {
  width: 10px;
}

::-webkit-scrollbar-track {
  background: var(--light);
  border-radius: 10px;
}

::-webkit-scrollbar-thumb {
  background: linear-gradient(135deg, var(--primary) 0%, var(--secondary) 100%);
  border-radius: 10px;
}

::-webkit-scrollbar-thumb:hover {
  background: linear-gradient(135deg, var(--secondary) 0%, var(--accent) 100%);
}
</style>