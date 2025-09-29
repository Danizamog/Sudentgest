<template>
  <div class="thread-list">
    <div class="list-header">
      <h3>{{ title }}</h3>
      <div class="header-actions">
        <slot name="header-actions"></slot>
      </div>
    </div>
    
    <div class="threads-grid">
      <ThreadCard
        v-for="thread in threads"
        :key="thread.id"
        :thread="thread"
        @click="$emit('thread-select', thread)"
      />
    </div>
    
    <div v-if="threads.length === 0" class="empty-state">
      <slot name="empty-state">
        <i class="fas fa-comments"></i>
        <p>No hay temas disponibles</p>
      </slot>
    </div>
  </div>
</template>

<script>
import ThreadCard from './ThreadCard.vue'

export default {
  name: 'ThreadList',
  components: {
    ThreadCard
  },
  props: {
    threads: {
      type: Array,
      required: true
    },
    title: {
      type: String,
      default: 'Temas del Foro'
    }
  },
  emits: ['thread-select']
}
</script>