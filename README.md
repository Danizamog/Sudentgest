# StudentGest - Sistema de Gestión Universitaria

## Arquitectura de Microservicios

StudentGest es una aplicación web para la gestión de instituciones educativas que utiliza una arquitectura de microservicios con multi-tenancy para diferenciar entre diferentes instituciones.

### Microservicios Disponibles

1. **Auth Service** (Puerto 5002) - Autenticación y autorización
2. **Courses Service** (Puerto 5008) - Gestión de cursos
3. **Teachers Service** (Puerto 5009) - Gestión de profesores
4. **Students Service** (Puerto 5010) - Gestión de estudiantes  
5. **Directors Service** (Porto 5011) - Gestión de directores y administración institucional
6. **Frontend** (Puerto 5173) - Interfaz de usuario Vue.js

### Características Principales

- **Multi-tenancy**: Cada institución tiene su propia base de datos separada
- **Autenticación por dominio**: Los usuarios se autentican con sus emails institucionales
- **Control de acceso basado en roles**: 
  - **Director**: Acceso completo a gestión de profesores, estudiantes y cursos
  - **Profesor**: Puede ver estudiantes y gestionar sus cursos
  - **Estudiante**: Acceso a cursos y funcionalidades básicas

### Instituciones Soportadas

- Universidad Católica Boliviana (UCB) - `@ucb.edu.bo`
- Universidad Privada Boliviana (UPB) - `@upb.edu.bo`
- Institución Demo - `@gmail.com`

## Instalación y Ejecución

### Prerequisitos

- Docker y Docker Compose
- Node.js 16+ (para desarrollo local)
- Python 3.11+ (para desarrollo local)

### Usando Docker (Recomendado)

1. Clona el repositorio:
```bash
git clone <repository-url>
cd Sudentgest
```

2. Ejecuta todos los servicios con Docker Compose:
```bash
docker-compose up --build
```

3. Los servicios estarán disponibles en:
- Frontend: http://localhost:5173
- Auth API: http://localhost:5002
- Courses API: http://localhost:5008
- Teachers API: http://localhost:5009
- Students API: http://localhost:5010
- Directors API: http://localhost:5011

### Desarrollo Local

#### Backend (Python FastAPI)

Para cada microservicio (Teachers, Students, Directors, Courses):

```bash
cd back/[ServiceName]
pip install -r requirements.txt
uvicorn main:app --reload --port [PORT]
```

#### Frontend (Vue.js)

```bash
cd front
npm install
npm run dev
```

## Estructura de la Base de Datos

La aplicación utiliza Supabase como base de datos con esquemas separados por tenant:

### Tablas por Tenant

Cada institución tiene sus propias tablas:
- `tenant_[domain]_usuarios` - Usuarios (estudiantes, profesores, directores)
- `tenant_[domain]_cursos` - Cursos de la institución
- `tenant_[domain]_inscripciones` - Inscripciones de estudiantes en cursos

### Tablas Globales

- `tenants` - Información de las instituciones
- `profiles` - Perfiles de usuario de Supabase Auth
- `categories`, `threads`, `replies`, `likes` - Sistema de foros

## API Endpoints

### Teachers Service (Puerto 5009)

- `GET /api/teachers/` - Obtener todos los profesores (solo directores)
- `POST /api/teachers/` - Crear nuevo profesor (solo directores)
- `GET /api/teachers/{id}` - Obtener profesor por ID (solo directores)
- `PUT /api/teachers/{id}` - Actualizar profesor (solo directores)
- `DELETE /api/teachers/{id}` - Eliminar profesor (solo directores)

### Students Service (Puerto 5010)

- `GET /api/students/` - Obtener todos los estudiantes (directores y profesores)
- `POST /api/students/` - Crear nuevo estudiante (solo directores)
- `GET /api/students/{id}` - Obtener estudiante por ID (directores y profesores)
- `PUT /api/students/{id}` - Actualizar estudiante (solo directores)
- `DELETE /api/students/{id}` - Eliminar estudiante (solo directores)
- `GET /api/students/course/{curso_id}` - Obtener estudiantes por curso (directores y profesores)

### Directors Service (Puerto 5011)

- `GET /api/directors/overview` - Resumen institucional (solo directores)
- `GET /api/directors/all-users` - Todos los usuarios por rol (solo directores)
- `GET /api/directors/` - Obtener todos los directores (solo directores)
- `POST /api/directors/` - Crear nuevo director (solo directores)
- `PATCH /api/directors/users/{user_id}/role` - Actualizar rol de usuario (solo directores)

## Roles y Permisos

### Director
- Ver y gestionar profesores
- Ver y gestionar estudiantes
- Ver y gestionar cursos
- Ver resumen institucional
- Cambiar roles de usuarios
- Acceso a base de datos

### Profesor
- Ver estudiantes
- Ver cursos
- Gestionar inscripciones en sus cursos

### Estudiante
- Ver cursos disponibles
- Inscribirse en cursos
- Ver sus cursos
- Participar en foros

## Navegación por Roles

La barra de navegación muestra diferentes opciones según el rol del usuario:

- **Todos los usuarios**: Inicio, Foro, Precios, Nosotros, Cursos, Mis Cursos
- **Profesores**: + Estudiantes
- **Directores**: + Profesores, Estudiantes, Base de Datos

## Variables de Entorno

Cada microservicio necesita las siguientes variables de entorno en su archivo `.env`:

```env
SUPABASE_URL=https://your-project.supabase.co
SUPABASE_ANON_KEY=your-anon-key
SUPABASE_JWT_SECRET=your-jwt-secret
SUPABASE_SERVICE_ROLE_KEY=your-service-role-key
```

## Desarrollo y Contribución

### Estructura de Archivos por Microservicio

```
back/[ServiceName]/
├── main.py              # Aplicación FastAPI principal
├── requirements.txt     # Dependencias Python
├── Dockerfile          # Configuración Docker
├── .env                # Variables de entorno
├── models/             # Modelos Pydantic
├── controllers/        # Lógica de negocio
├── routes/            # Rutas API
└── utils/             # Utilidades (Supabase, etc.)
```

### Agregar Nuevas Funcionalidades

1. Crea los modelos en `models/`
2. Implementa la lógica en `controllers/`
3. Define las rutas en `routes/`
4. Registra las rutas en `main.py`
5. Actualiza el frontend si es necesario

## Solución de Problemas

### Error de Autenticación
- Verifica que las variables de entorno de Supabase estén correctamente configuradas
- Asegúrate de que el token JWT esté presente en localStorage

### Error de Permisos
- Verifica que el usuario tenga el rol correcto en la base de datos
- Revisa que el email del usuario coincida con el dominio institucional

### Error de Conexión a Microservicios
- Verifica que todos los servicios estén ejecutándose en los puertos correctos
- Revisa los logs de Docker Compose para errores específicos

## Licencia

[Especificar licencia del proyecto]