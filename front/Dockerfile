# ----------------------------------------------------------------------
# Stage 1: Build - Crear los archivos estáticos de producción con Node
# ----------------------------------------------------------------------
FROM node:20-alpine AS build
WORKDIR /app

# 1. Copia solo los archivos de configuración de dependencias (mejor para el cache de Docker)
COPY package*.json ./

# 2. Instala las dependencias
RUN npm ci --loglevel=error

# 3. Copia el resto del código fuente de la aplicación
COPY . .

# 4. Construye la aplicación. El resultado estático queda en /app/dist
# Los archivos .env (VITE_*) se incrustan en este paso.
RUN npm run build

# ----------------------------------------------------------------------
# Stage 2: Serve - Servir los archivos estáticos con Nginx
# ----------------------------------------------------------------------
# Usamos la imagen base más ligera (Alpine) para Nginx
FROM nginx:alpine

# 1. Copia la configuración personalizada de Nginx
# Esta configuración debe incluir la regla de proxy para /api/auth/
COPY nginx.conf /etc/nginx/conf.d/default.conf

# 2. Copia los archivos estáticos de la etapa 'build' al directorio de servicio de Nginx
COPY --from=build /app/dist /usr/share/nginx/html

# 3. Exponer el puerto interno del contenedor
EXPOSE 80

# 4. Comando de inicio (Nginx en modo no-daemon para que el contenedor se mantenga activo)
CMD ["nginx", "-g", "daemon off;"]
