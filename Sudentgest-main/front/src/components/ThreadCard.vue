<template>
  <div class="thread-card" @click="$emit('click', thread)">
    <div class="card-header">
      <div class="thread-title">
        <h4>{{ thread.title }}</h4>
        <div class="thread-meta">
          <span class="author">{{ thread.author }}</span>
          <span class="category">{{ thread.category }}</span>
          <span class="date">{{ formatDate(thread.date) }}</span>
        </div>
      </div>
      <div class="thread-stats">
        <div class="stat">
          <i class="fas fa-comment"></i>
          <span>{{ thread.replies }}</span>
        </div>
        <div class="stat">
          <i class="fas fa-eye"></i>
          <span>{{ thread.views }}</span>
        </div>
      </div>
    </div>
    
    <p class="thread-excerpt">{{ thread.excerpt }}</p>
    
    <div class="thread-tags">
      <span 
        v-for="tag in thread.tags" 
        :key="tag" 
        class="tag"
        @click.stop="$emit('tag-click', tag)"
      >
        {{ tag }}
      </span>
    </div>
  </div>
</template>

<script>
export default {
  name: 'ThreadCard',
  props: {
    thread: {
      type: Object,
      required: true
    }
  },
  emits: ['click', 'tag-click'],
  methods: {
    formatDate(dateString) {
      return new Date(dateString).toLocaleDateString('es-ES')
    }
  }
}
</script>