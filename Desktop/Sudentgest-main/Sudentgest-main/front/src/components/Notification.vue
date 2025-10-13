<template>
  <div v-if="show" :class="['notification', type]" @click="hide">
    {{ message }}
  </div>
</template>

<script>
import { ref } from 'vue'

export default {
  name: 'Notification',
  setup() {
    const show = ref(false)
    const message = ref('')
    const type = ref('success') // success, error, warning, info

    const showNotification = (msg, notificationType = 'success') => {
      message.value = msg
      type.value = notificationType
      show.value = true
      setTimeout(() => {
        show.value = false
      }, 3000)
    }

    const hide = () => {
      show.value = false
    }

    return {
      show,
      message,
      type,
      showNotification,
      hide
    }
  }
}
</script>

<style scoped>
.notification {
  position: fixed;
  top: 20px;
  right: 20px;
  padding: 15px 20px;
  border-radius: 8px;
  color: white;
  font-weight: 500;
  z-index: 1000;
  cursor: pointer;
  box-shadow: 0 4px 12px rgba(0,0,0,0.15);
  animation: slideIn 0.3s ease;
}

.notification.success {
  background-color: #28a745;
}

.notification.error {
  background-color: #dc3545;
}

.notification.warning {
  background-color: #ffc107;
  color: #212529;
}

.notification.info {
  background-color: #17a2b8;
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
</style>