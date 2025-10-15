<template>
  <div class="container">
    <h2>📘 Subir Notas</h2>

    <div class="form-section">
      <div class="input-group">
        <label>Institución:</label>
        <input v-model="institution" placeholder="Ej: UCB" />
      </div>
      <div class="input-group">
        <label>Materia:</label>
        <input v-model="subject" placeholder="Ej: Redes" />
      </div>
    </div>

    <h3>Estudiantes</h3>
    <table class="table">
      <thead>
        <tr><th>#</th><th>Nombre</th><th>Nota</th></tr>
      </thead>
      <tbody>
        <tr v-for="(student, index) in students" :key="index">
          <td>{{ index + 1 }}</td>
          <td>{{ student.student_name }}</td>
          <td>
            <input
              type="number"
              v-model.number="student.grade"
              min="0"
              max="100"
              placeholder="0-100"
            />
          </td>
        </tr>
      </tbody>
    </table>

    <div class="actions">
      <button @click="saveGrades" :disabled="loading">
        <span v-if="!loading">💾 Guardar Notas</span>
        <span v-else>Guardando...</span>
      </button>
    </div>

    <div v-if="message" :class="['message', messageType]">{{ message }}</div>
  </div>
</template>

<script>
export default {
  name: "UploadGrades",
  data() {
    return {
      institution: "",
      subject: "",
      students: [
        { student_name: "Ana Gutiérrez", grade: null },
        { student_name: "Luis Fernández", grade: null },
        { student_name: "María López", grade: null },
      ],
      loading: false,
      message: "",
      messageType: "",
    };
  },
  methods: {
    async saveGrades() {
      if (!this.institution || !this.subject) {
        this.message = "⚠️ Debes llenar la institución y la materia.";
        this.messageType = "danger";
        return;
      }

      // Validar notas
      for (const s of this.students) {
        if (s.grade != null && (s.grade < 0 || s.grade > 100)) {
          this.message = "❌ Las notas deben estar entre 0 y 100.";
          this.messageType = "danger";
          return;
        }
      }

      this.loading = true;
      this.message = "";

      try {
        for (const s of this.students) {
          const body = {
            institution: this.institution,
            subject: this.subject,
            student_name: s.student_name,
            grade: s.grade ?? 0,
          };

          await fetch("http://localhost:5012/grades", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body),
          });
        }

        this.message = "✅ Notas guardadas correctamente.";
        this.messageType = "success";
      } catch (error) {
        console.error(error);
        this.message = "❌ Error al guardar las notas.";
        this.messageType = "danger";
      } finally {
        this.loading = false;
      }
    },
  },
};
</script>

<style scoped>
.container {
  max-width: 800px;
  margin: 0 auto;
  padding: 16px;
  background: #f9fafb;
  border-radius: 10px;
}
.form-section {
  display: flex;
  gap: 20px;
  margin-bottom: 16px;
}
.input-group {
  display: flex;
  flex-direction: column;
  flex: 1;
}
.input-group label {
  font-weight: 600;
  margin-bottom: 4px;
}
.input-group input {
  padding: 8px;
  border-radius: 6px;
  border: 1px solid #ccc;
}
.table {
  width: 100%;
  border-collapse: collapse;
  margin-top: 10px;
  background: white;
}
.table th, .table td {
  border: 1px solid #ddd;
  padding: 8px;
  text-align: center;
}
.table th {
  background-color: #f3f4f6;
}
input[type="number"] {
  width: 80px;
  padding: 4px;
  border-radius: 6px;
  border: 1px solid #ccc;
  text-align: center;
}
.actions {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
button {
  padding: 10px 16px;
  background: #1e40af;
  color: white;
  border: none;
  border-radius: 8px;
  cursor: pointer;
}
button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
.message {
  margin-top: 16px;
  padding: 10px;
  border-radius: 6px;
  font-weight: 500;
}
.success {
  background: #e6ffed;
  color: #065f46;
  border: 1px solid #6ee7b7;
}
.danger {
  background: #ffecec;
  color: #7f1d1d;
  border: 1px solid #fca5a5;
}
</style>
