<template>
  <div class="subir-notas">
    <h1>Subir Notas</h1>

    <!-- Selección de materia -->
    <div class="form-section">
      <label>Materia:</label>
      <select v-model="materiaSeleccionada" @change="cargarAlumnos">
        <option disabled value="">-- Selecciona una materia --</option>
        <option v-for="materia in materias" :key="materia.id" :value="materia.id">
          {{ materia.nombre }}
        </option>
      </select>
    </div>

    <!-- Selección de alumno -->
    <div v-if="materiaSeleccionada" class="form-section">
      <label>Alumno:</label>
      <select v-model="alumnoSeleccionado">
        <option disabled value="">-- Selecciona un alumno --</option>
        <option v-for="alumno in alumnos" :key="alumno.id" :value="alumno.id">
          {{ alumno.nombre }}
        </option>
      </select>
    </div>

    <!-- Campo para nota -->
    <div v-if="alumnoSeleccionado" class="form-section">
      <label>Nota:</label>
      <input type="number" v-model="nota" min="0" max="100" />
      <button @click="guardarNota">Guardar Nota</button>
    </div>

    <!-- Listado de notas -->
    <div v-if="notas.length > 0" class="notas-lista">
      <h2>Notas registradas</h2>
      <table>
        <thead>
          <tr>
            <th>Alumno</th>
            <th>Materia</th>
            <th>Nota</th>
            <th>Acciones</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="n in notas" :key="n.id">
            <td>{{ n.alumno }}</td>
            <td>{{ n.materia }}</td>
            <td>{{ n.valor }}</td>
            <td>
              <button @click="editarNota(n)">Editar</button>
              <button @click="eliminarNota(n.id)">Eliminar</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Mensajes -->
    <p v-if="mensaje" class="mensaje">{{ mensaje }}</p>
  </div>
</template>

<script>
import axios from "axios";

export default {
  name: "SubirNotas",
  data() {
    return {
      materias: [],
      alumnos: [],
      notas: [],
      materiaSeleccionada: "",
      alumnoSeleccionado: "",
      nota: "",
      profesorId: 1, // Esto luego lo tomas del login real
      mensaje: ""
    };
  },
  created() {
    this.cargarMaterias();
    this.cargarNotas();
  },
  methods: {
    async cargarMaterias() {
      try {
        const res = await axios.get(`http://localhost:5000/api/materias/${this.profesorId}`);
        this.materias = res.data;
      } catch (e) {
        this.mensaje = "Error al cargar materias";
      }
    },
    async cargarAlumnos() {
      try {
        const res = await axios.get(`http://localhost:5000/api/alumnos/${this.materiaSeleccionada}`);
        this.alumnos = res.data;
      } catch (e) {
        this.mensaje = "Error al cargar alumnos";
      }
    },
    async cargarNotas() {
      try {
        const res = await axios.get("http://localhost:5000/api/notas");
        this.notas = res.data;
      } catch (e) {
        this.mensaje = "Error al cargar notas";
      }
    },
    async guardarNota() {
      if (!this.nota) {
        this.mensaje = "Debes ingresar una nota";
        return;
      }
      try {
        await axios.post("http://localhost:5000/api/notas", {
          alumnoId: this.alumnoSeleccionado,
          materiaId: this.materiaSeleccionada,
          valor: this.nota
        });
        this.mensaje = "Nota guardada con éxito";
        this.cargarNotas();
        this.nota = "";
        this.alumnoSeleccionado = "";
      } catch (e) {
        this.mensaje = "Error al guardar la nota";
      }
    },
    async editarNota(n) {
      const nuevoValor = prompt("Nueva nota:", n.valor);
      if (!nuevoValor) return;
      try {
        await axios.put(`http://localhost:5000/api/notas/${n.id}`, { valor: nuevoValor });
        this.mensaje = "Nota actualizada";
        this.cargarNotas();
      } catch (e) {
        this.mensaje = "Error al editar la nota";
      }
    },
    async eliminarNota(id) {
      try {
        await axios.delete(`http://localhost:5000/api/notas/${id}`);
        this.mensaje = "Nota eliminada";
        this.cargarNotas();
      } catch (e) {
        this.mensaje = "Error al eliminar la nota";
      }
    }
  }
};
</script>

<style scoped>
.subir-notas {
  max-width: 800px;
  margin: auto;
  padding: 20px;
}

.form-section {
  margin: 15px 0;
}

table {
  width: 100%;
  border-collapse: collapse;
  margin-top: 15px;
}

th, td {
  border: 1px solid #ddd;
  padding: 10px;
}

button {
  margin: 0 5px;
}

.mensaje {
  margin-top: 15px;
  font-weight: bold;
}
</style>
