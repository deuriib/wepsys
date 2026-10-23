# syntax=docker/dockerfile:1.7

# Versions are pinned to the toolchain declared in mise.toml (node 24.22.1).
# Keep this in sync with the .NET image versions in the backend Dockerfile.
ARG VERSION_NODE=24.22.1-alpine

# ------------------------------------------------------------------------------
# Dependencies are installed once and reused by the build and dev stages.
# ------------------------------------------------------------------------------
FROM node:${VERSION_NODE} AS dependencias
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

# ------------------------------------------------------------------------------
# Static bundle. VITE_API_URL stays empty by default: the UI calls `/api` on its
# own origin and nginx forwards it to the API container.
# ------------------------------------------------------------------------------
FROM dependencias AS compilacion
ARG VITE_API_URL=""
ENV VITE_API_URL=${VITE_API_URL}
COPY . .
RUN npm run build

# ------------------------------------------------------------------------------
# Hot-reload development server (source is bind-mounted by compose.dev.yaml).
# ------------------------------------------------------------------------------
FROM dependencias AS desarrollo
WORKDIR /app
COPY . .
ENV VITE_PROXY_TARGET=http://api:8080
EXPOSE 5173
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0", "--port", "5173"]

# ------------------------------------------------------------------------------
# Final image: nginx serves the bundle and reverse-proxies the API.
# ------------------------------------------------------------------------------
FROM nginx:1.27-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=compilacion /app/dist /usr/share/nginx/html
EXPOSE 80
HEALTHCHECK --interval=15s --timeout=5s --start-period=10s --retries=5 \
    CMD wget --quiet --spider http://127.0.0.1/ || exit 1
