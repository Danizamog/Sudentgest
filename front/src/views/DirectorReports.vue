<template>
  <div class="director-reports">
    <div class="container">
      <div class="page-header">
        <h1>📊 Reportes del Sistema</h1>
        <p class="subtitle">Vista completa del rendimiento académico</p>
      </div>

      <div class="report-card">
        <h2>Reporte General del Sistema</h2>
        <p>Genera un reporte completo con estadísticas de todos los cursos, profesores y estudiantes.</p>
        
        <div class="form-group">
          <label>Formato:</label>
          <div class="format-options">
            <label class="radio-option">
              <input type="radio" v-model="format" value="pdf" />
              <span>📄 PDF</span>
            </label>
            <label class="radio-option">
              <input type="radio" v-model="format" value="excel" />
              <span>📊 Excel</span>
            </label>
          </div>
        </div>

        <button @click="generateSystemReport" :disabled="generating" class="btn btn-primary">
          {{ generating ? 'Generando...' : 'Generar Reporte del Sistema' }}
        </button>
      </div>

      <div v-if="errorMessage" class="error-message">
        {{ errorMessage }}
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'

const REPORTS_API = import.meta.env.VITE_REPORTS_API || 'http://localhost:5014'

const format = ref('pdf')
const generating = ref(false)
const errorMessage = ref('')

async function getUserEmail() {
  try {
    const response = await fetch('/auth/user-profile', { credentials: 'include' })
    if (response.ok) {
      const profile = await response.json()
      return profile.email
    }
  } catch (err) {
    console.error('Error getting user email:', err)
  }
  return null
}

async function generateSystemReport() {
  generating.value = true
  errorMessage.value = ''
  
  try {
    const email = await getUserEmail()
    if (!email) throw new Error('No se pudo obtener el email del usuario')

    const response = await fetch(`${REPORTS_API}/api/reports/system-overview`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-User-Email': email
      },
      body: JSON.stringify({ format: format.value })
    })

    if (!response.ok) {
      const error = await response.json()
      throw new Error(error.detail || 'Error al generar reporte')
    }

    const blob = await response.blob()
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    const ext = format.value === 'pdf' ? 'pdf' : 'xlsx'
    a.download = `sistema_resumen.${ext}`
    document.body.appendChild(a)
    a.click()
    window.URL.revokeObjectURL(url)
    document.body.removeChild(a)
  } catch (err) {
    console.error('Error generating report:', err)
    errorMessage.value = err.message
  } finally {
    generating.value = false
  }
}
</script>

<style scoped>
.director-reports {
  min-height: calc(100vh - 120px);
  padding: 2rem 0;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.container {
  max-width: 800px;
  margin: 0 auto;
  padding: 0 1rem;
}

.page-header {
  text-align: center;
  margin-bottom: 2rem;
  color: white;
}

.page-header h1 {
  font-size: 2.5rem;
  margin-bottom: 0.5rem;
}

.subtitle {
  font-size: 1.1rem;
  opacity: 0.9;
}

.report-card {
  background: white;
  border-radius: 12px;
  padding: 2rem;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.report-card h2 {
  margin-top: 0;
  color: #333;
}

.report-card p {
  color: #666;
  margin-bottom: 1.5rem;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 600;
  color: #555;
}

.format-options {
  display: flex;
  gap: 1rem;
}

.radio-option {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1.5rem;
  border: 2px solid #ddd;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.3s;
}

.radio-option:hover {
  border-color: #667eea;
  background: #f8f9ff;
}

.radio-option input[type="radio"] {
  width: 18px;
  height: 18px;
}

.btn {
  padding: 0.75rem 2rem;
  border: none;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
}

.btn-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.btn-primary:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 6px 20px rgba(102, 126, 234, 0.4);
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.error-message {
  margin-top: 1.5rem;
  padding: 1rem;
  background: #ffebee;
  border-left: 4px solid #f44336;
  border-radius: 4px;
  color: #c62828;
}
</style>
