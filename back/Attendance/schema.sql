

-- Tabla de asistencias
CREATE TABLE IF NOT EXISTS tenant_ucb_asistencias (
  id BIGSERIAL PRIMARY KEY,
  estudiante_id BIGINT REFERENCES tenant_ucb_usuarios(id) ON DELETE CASCADE,
  curso_id BIGINT REFERENCES tenant_ucb_cursos(id) ON DELETE CASCADE,
  fecha DATE NOT NULL,
  estado VARCHAR(20) CHECK (estado IN ('presente', 'ausente', 'tardanza')) NOT NULL,
  observaciones TEXT,
  registrado_por BIGINT REFERENCES tenant_ucb_usuarios(id),
  created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Tabla de excusas
CREATE TABLE IF NOT EXISTS tenant_ucb_excusas (
  id BIGSERIAL PRIMARY KEY,
  estudiante_id BIGINT REFERENCES tenant_ucb_usuarios(id) ON DELETE CASCADE,
  curso_id BIGINT REFERENCES tenant_ucb_cursos(id) ON DELETE CASCADE,
  fecha_inicio DATE NOT NULL,
  fecha_fin DATE NOT NULL,
  motivo TEXT NOT NULL,
  documento_url TEXT,
  creado_por BIGINT REFERENCES tenant_ucb_usuarios(id),
  estado VARCHAR(20) CHECK (estado IN ('pendiente', 'aprobada', 'rechazada')) DEFAULT 'pendiente',
  comentario_director TEXT,
  aprobado_por BIGINT REFERENCES tenant_ucb_usuarios(id),
  fecha_aprobacion TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_ucb_asistencias_estudiante ON tenant_ucb_asistencias(estudiante_id);
CREATE INDEX IF NOT EXISTS idx_ucb_asistencias_curso ON tenant_ucb_asistencias(curso_id);
CREATE INDEX IF NOT EXISTS idx_ucb_asistencias_fecha ON tenant_ucb_asistencias(fecha);
CREATE INDEX IF NOT EXISTS idx_ucb_excusas_estado ON tenant_ucb_excusas(estado);
CREATE INDEX IF NOT EXISTS idx_ucb_excusas_estudiante ON tenant_ucb_excusas(estudiante_id);


CREATE TABLE IF NOT EXISTS tenant_upb_asistencias (
  id BIGSERIAL PRIMARY KEY,
  estudiante_id BIGINT REFERENCES tenant_upb_usuarios(id) ON DELETE CASCADE,
  curso_id BIGINT REFERENCES tenant_upb_cursos(id) ON DELETE CASCADE,
  fecha DATE NOT NULL,
  estado VARCHAR(20) CHECK (estado IN ('presente', 'ausente', 'tardanza')) NOT NULL,
  observaciones TEXT,
  registrado_por BIGINT REFERENCES tenant_upb_usuarios(id),
  created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS tenant_upb_excusas (
  id BIGSERIAL PRIMARY KEY,
  estudiante_id BIGINT REFERENCES tenant_upb_usuarios(id) ON DELETE CASCADE,
  curso_id BIGINT REFERENCES tenant_upb_cursos(id) ON DELETE CASCADE,
  fecha_inicio DATE NOT NULL,
  fecha_fin DATE NOT NULL,
  motivo TEXT NOT NULL,
  documento_url TEXT,
  creado_por BIGINT REFERENCES tenant_upb_usuarios(id),
  estado VARCHAR(20) CHECK (estado IN ('pendiente', 'aprobada', 'rechazada')) DEFAULT 'pendiente',
  comentario_director TEXT,
  aprobado_por BIGINT REFERENCES tenant_upb_usuarios(id),
  fecha_aprobacion TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_upb_asistencias_estudiante ON tenant_upb_asistencias(estudiante_id);
CREATE INDEX IF NOT EXISTS idx_upb_asistencias_curso ON tenant_upb_asistencias(curso_id);
CREATE INDEX IF NOT EXISTS idx_upb_asistencias_fecha ON tenant_upb_asistencias(fecha);
CREATE INDEX IF NOT EXISTS idx_upb_excusas_estado ON tenant_upb_excusas(estado);
CREATE INDEX IF NOT EXISTS idx_upb_excusas_estudiante ON tenant_upb_excusas(estudiante_id);


CREATE TABLE IF NOT EXISTS tenant_gmail_asistencias (
  id BIGSERIAL PRIMARY KEY,
  estudiante_id BIGINT REFERENCES tenant_gmail_usuarios(id) ON DELETE CASCADE,
  curso_id BIGINT REFERENCES tenant_gmail_cursos(id) ON DELETE CASCADE,
  fecha DATE NOT NULL,
  estado VARCHAR(20) CHECK (estado IN ('presente', 'ausente', 'tardanza')) NOT NULL,
  observaciones TEXT,
  registrado_por BIGINT REFERENCES tenant_gmail_usuarios(id),
  created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS tenant_gmail_excusas (
  id BIGSERIAL PRIMARY KEY,
  estudiante_id BIGINT REFERENCES tenant_gmail_usuarios(id) ON DELETE CASCADE,
  curso_id BIGINT REFERENCES tenant_gmail_cursos(id) ON DELETE CASCADE,
  fecha_inicio DATE NOT NULL,
  fecha_fin DATE NOT NULL,
  motivo TEXT NOT NULL,
  documento_url TEXT,
  creado_por BIGINT REFERENCES tenant_gmail_usuarios(id),
  estado VARCHAR(20) CHECK (estado IN ('pendiente', 'aprobada', 'rechazada')) DEFAULT 'pendiente',
  comentario_director TEXT,
  aprobado_por BIGINT REFERENCES tenant_gmail_usuarios(id),
  fecha_aprobacion TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_gmail_asistencias_estudiante ON tenant_gmail_asistencias(estudiante_id);
CREATE INDEX IF NOT EXISTS idx_gmail_asistencias_curso ON tenant_gmail_asistencias(curso_id);
CREATE INDEX IF NOT EXISTS idx_gmail_asistencias_fecha ON tenant_gmail_asistencias(fecha);
CREATE INDEX IF NOT EXISTS idx_gmail_excusas_estado ON tenant_gmail_excusas(estado);
CREATE INDEX IF NOT EXISTS idx_gmail_excusas_estudiante ON tenant_gmail_excusas(estudiante_id);
