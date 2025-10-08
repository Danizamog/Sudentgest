-- ═══════════════════════════════════════════════════════════════
-- 🔹 TABLAS PARA TENANT: tenant_ucb (UCB)
-- ═══════════════════════════════════════════════════════════════

-- Tabla de cursos
CREATE TABLE IF NOT EXISTS tenant_ucb_cursos (
  id BIGSERIAL PRIMARY KEY,
  nombre VARCHAR(255) NOT NULL,
  codigo VARCHAR(50) UNIQUE NOT NULL,
  descripcion TEXT,
  creditos INTEGER DEFAULT 3,
  horario VARCHAR(255),
  profesor_id BIGINT REFERENCES tenant_ucb_usuarios(id) ON DELETE SET NULL,
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Índice para búsquedas por profesor
CREATE INDEX IF NOT EXISTS idx_ucb_cursos_profesor ON tenant_ucb_cursos(profesor_id);

-- Tabla de inscripciones (relación usuario-curso)
CREATE TABLE IF NOT EXISTS tenant_ucb_inscripciones (
  id BIGSERIAL PRIMARY KEY,
  curso_id BIGINT REFERENCES tenant_ucb_cursos(id) ON DELETE CASCADE,
  usuario_id BIGINT REFERENCES tenant_ucb_usuarios(id) ON DELETE CASCADE,
  created_at TIMESTAMPTZ DEFAULT NOW(),
  UNIQUE(curso_id, usuario_id)
);

-- Índices para optimizar consultas
CREATE INDEX IF NOT EXISTS idx_ucb_inscripciones_usuario ON tenant_ucb_inscripciones(usuario_id);
CREATE INDEX IF NOT EXISTS idx_ucb_inscripciones_curso ON tenant_ucb_inscripciones(curso_id);

-- ═══════════════════════════════════════════════════════════════
-- 🔹 TABLAS PARA TENANT: tenant_upb (UPB)
-- ═══════════════════════════════════════════════════════════════

CREATE TABLE IF NOT EXISTS tenant_upb_cursos (
  id BIGSERIAL PRIMARY KEY,
  nombre VARCHAR(255) NOT NULL,
  codigo VARCHAR(50) UNIQUE NOT NULL,
  descripcion TEXT,
  creditos INTEGER DEFAULT 3,
  horario VARCHAR(255),
  profesor_id BIGINT REFERENCES tenant_upb_usuarios(id) ON DELETE SET NULL,
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Índice para búsquedas por profesor
CREATE INDEX IF NOT EXISTS idx_upb_cursos_profesor ON tenant_upb_cursos(profesor_id);

CREATE TABLE IF NOT EXISTS tenant_upb_inscripciones (
  id BIGSERIAL PRIMARY KEY,
  curso_id BIGINT REFERENCES tenant_upb_cursos(id) ON DELETE CASCADE,
  usuario_id BIGINT REFERENCES tenant_upb_usuarios(id) ON DELETE CASCADE,
  created_at TIMESTAMPTZ DEFAULT NOW(),
  UNIQUE(curso_id, usuario_id)
);

CREATE INDEX IF NOT EXISTS idx_upb_inscripciones_usuario ON tenant_upb_inscripciones(usuario_id);
CREATE INDEX IF NOT EXISTS idx_upb_inscripciones_curso ON tenant_upb_inscripciones(curso_id);

-- ═══════════════════════════════════════════════════════════════
-- 🔹 TABLAS PARA TENANT: tenant_gmail (Gmail)
-- ═══════════════════════════════════════════════════════════════

CREATE TABLE IF NOT EXISTS tenant_gmail_cursos (
  id BIGSERIAL PRIMARY KEY,
  nombre VARCHAR(255) NOT NULL,
  codigo VARCHAR(50) UNIQUE NOT NULL,
  descripcion TEXT,
  creditos INTEGER DEFAULT 3,
  horario VARCHAR(255),
  profesor_id BIGINT REFERENCES tenant_gmail_usuarios(id) ON DELETE SET NULL,
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Índice para búsquedas por profesor
CREATE INDEX IF NOT EXISTS idx_gmail_cursos_profesor ON tenant_gmail_cursos(profesor_id);

CREATE TABLE IF NOT EXISTS tenant_gmail_inscripciones (
  id BIGSERIAL PRIMARY KEY,
  curso_id BIGINT REFERENCES tenant_gmail_cursos(id) ON DELETE CASCADE,
  usuario_id BIGINT REFERENCES tenant_gmail_usuarios(id) ON DELETE CASCADE,
  created_at TIMESTAMPTZ DEFAULT NOW(),
  UNIQUE(curso_id, usuario_id)
);

CREATE INDEX IF NOT EXISTS idx_gmail_inscripciones_usuario ON tenant_gmail_inscripciones(usuario_id);
CREATE INDEX IF NOT EXISTS idx_gmail_inscripciones_curso ON tenant_gmail_inscripciones(curso_id);

-- ═══════════════════════════════════════════════════════════════
-- 🔹 DATOS DE PRUEBA (OPCIONAL)
-- ═══════════════════════════════════════════════════════════════

-- Cursos de ejemplo para UCB
INSERT INTO tenant_ucb_cursos (nombre, codigo, descripcion, creditos, horario) VALUES
  ('Arquitectura de Software', 'ARQ-101', 'Patrones de diseño y arquitectura', 4, 'Lun-Mie 10:00-12:00'),
  ('Base de Datos Avanzadas', 'BD-201', 'PostgreSQL, NoSQL, indexación', 4, 'Mar-Jue 14:00-16:00'),
  ('Programación Web', 'WEB-101', 'HTML, CSS, JavaScript, Vue', 3, 'Vie 08:00-11:00')
ON CONFLICT (codigo) DO NOTHING;

-- Cursos de ejemplo para UPB
INSERT INTO tenant_upb_cursos (nombre, codigo, descripcion, creditos, horario) VALUES
  ('Inteligencia Artificial', 'IA-301', 'Machine Learning, Deep Learning', 4, 'Lun-Mie 16:00-18:00'),
  ('Redes de Computadoras', 'RED-201', 'TCP/IP, routing, seguridad', 3, 'Mar-Jue 10:00-12:00')
ON CONFLICT (codigo) DO NOTHING;
