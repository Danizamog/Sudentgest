-- Esquema de base de datos para el servicio de contacto
-- Se debe crear una tabla por cada tenant siguiendo el patrón de nomenclatura

-- Tabla para tenant UCB
CREATE TABLE IF NOT EXISTS tenant_ucb_contact_messages (
    id SERIAL PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL,
    mensaje TEXT NOT NULL,
    asunto VARCHAR(200),
    telefono VARCHAR(20),
    tenant VARCHAR(50) NOT NULL DEFAULT 'ucb.edu.bo',
    status VARCHAR(20) NOT NULL DEFAULT 'pendiente',
    user_id INTEGER REFERENCES tenant_ucb_usuarios(id),
    user_agent TEXT,
    ip_address VARCHAR(45),
    respuesta TEXT,
    atendido_por VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Tabla para tenant UPB
CREATE TABLE IF NOT EXISTS tenant_upb_contact_messages (
    id SERIAL PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL,
    mensaje TEXT NOT NULL,
    asunto VARCHAR(200),
    telefono VARCHAR(20),
    tenant VARCHAR(50) NOT NULL DEFAULT 'upb.edu.bo',
    status VARCHAR(20) NOT NULL DEFAULT 'pendiente',
    user_id INTEGER REFERENCES tenant_upb_usuarios(id),
    user_agent TEXT,
    ip_address VARCHAR(45),
    respuesta TEXT,
    atendido_por VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Tabla para tenant Gmail
CREATE TABLE IF NOT EXISTS tenant_gmail_contact_messages (
    id SERIAL PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL,
    mensaje TEXT NOT NULL,
    asunto VARCHAR(200),
    telefono VARCHAR(20),
    tenant VARCHAR(50) NOT NULL DEFAULT 'gmail.com',
    status VARCHAR(20) NOT NULL DEFAULT 'pendiente',
    user_id INTEGER REFERENCES tenant_gmail_usuarios(id),
    user_agent TEXT,
    ip_address VARCHAR(45),
    respuesta TEXT,
    atendido_por VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Índices para mejorar rendimiento
CREATE INDEX IF NOT EXISTS idx_ucb_contact_email ON tenant_ucb_contact_messages(email);
CREATE INDEX IF NOT EXISTS idx_ucb_contact_status ON tenant_ucb_contact_messages(status);
CREATE INDEX IF NOT EXISTS idx_ucb_contact_created ON tenant_ucb_contact_messages(created_at);

CREATE INDEX IF NOT EXISTS idx_upb_contact_email ON tenant_upb_contact_messages(email);
CREATE INDEX IF NOT EXISTS idx_upb_contact_status ON tenant_upb_contact_messages(status);
CREATE INDEX IF NOT EXISTS idx_upb_contact_created ON tenant_upb_contact_messages(created_at);

CREATE INDEX IF NOT EXISTS idx_gmail_contact_email ON tenant_gmail_contact_messages(email);
CREATE INDEX IF NOT EXISTS idx_gmail_contact_status ON tenant_gmail_contact_messages(status);
CREATE INDEX IF NOT EXISTS idx_gmail_contact_created ON tenant_gmail_contact_messages(created_at);

-- Función para actualizar updated_at automáticamente
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Triggers para actualizar updated_at
CREATE TRIGGER update_ucb_contact_updated_at BEFORE UPDATE ON tenant_ucb_contact_messages 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_upb_contact_updated_at BEFORE UPDATE ON tenant_upb_contact_messages 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_gmail_contact_updated_at BEFORE UPDATE ON tenant_gmail_contact_messages 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();