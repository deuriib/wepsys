# syntax=docker/dockerfile:1.7

# Build context: repository root (the API needs ../../../database).
# Versions are pinned to the toolchain declared in mise.toml:
#   dotnet 8.0.426  ->  ASP.NET Core runtime 8.0.32
#   node   24.22.1  ->  see src/frontend/Dockerfile
# Update both files whenever mise.toml moves.
ARG VERSION_DOTNET_SDK=8.0.426
ARG VERSION_DOTNET_ASPNET=8.0.32

# ------------------------------------------------------------------------------
# Runtime base: only what the published API needs to run.
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:${VERSION_DOTNET_ASPNET} AS base
WORKDIR /app
# curl exists solely to probe /salud from the container health check.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

# ------------------------------------------------------------------------------
# Restore layer: project files only, so the NuGet cache survives source edits.
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:${VERSION_DOTNET_SDK} AS dependencias
WORKDIR /origen
COPY src/backend/SB.GestionSolicitudes.Dominio/SB.GestionSolicitudes.Dominio.csproj src/backend/SB.GestionSolicitudes.Dominio/
COPY src/backend/SB.GestionSolicitudes.Aplicacion/SB.GestionSolicitudes.Aplicacion.csproj src/backend/SB.GestionSolicitudes.Aplicacion/
COPY src/backend/SB.GestionSolicitudes.Infraestructura/SB.GestionSolicitudes.Infraestructura.csproj src/backend/SB.GestionSolicitudes.Infraestructura/
COPY src/backend/SB.GestionSolicitudes.Servicios/SB.GestionSolicitudes.Servicios.csproj src/backend/SB.GestionSolicitudes.Servicios/
COPY src/backend/SB.GestionSolicitudes.Pruebas/SB.GestionSolicitudes.Pruebas.csproj src/backend/SB.GestionSolicitudes.Pruebas/
RUN dotnet restore src/backend/SB.GestionSolicitudes.Servicios/SB.GestionSolicitudes.Servicios.csproj \
    && dotnet restore src/backend/SB.GestionSolicitudes.Pruebas/SB.GestionSolicitudes.Pruebas.csproj

# ------------------------------------------------------------------------------
# Source layer shared by the publish, test and development stages.
# ------------------------------------------------------------------------------
FROM dependencias AS origen
COPY src/backend src/backend
# The catalog seed is linked by the Servicios project and must sit at
# /origen/database for the ../../../database reference to resolve.
COPY database/entidades-gubernamentales.seed.json database/entidades-gubernamentales.seed.json

# ------------------------------------------------------------------------------
# Release artifact consumed by the final stage.
# ------------------------------------------------------------------------------
FROM origen AS publicacion
ARG CONFIGURACION=Release
WORKDIR /origen/src/backend/SB.GestionSolicitudes.Servicios
RUN dotnet publish SB.GestionSolicitudes.Servicios.csproj \
    --configuration ${CONFIGURACION} \
    --no-restore \
    --output /app/publicacion

# ------------------------------------------------------------------------------
# Test runner: `docker compose -f compose.test.yaml run --rm pruebas`.
# ------------------------------------------------------------------------------
FROM origen AS pruebas
WORKDIR /origen/src/backend
CMD ["dotnet", "test", "SB.GestionSolicitudes.Pruebas/SB.GestionSolicitudes.Pruebas.csproj", "--configuration", "Release", "--no-restore", "--logger", "console;verbosity=normal"]

# ------------------------------------------------------------------------------
# Hot-reload development environment (source is bind-mounted by compose.dev.yaml).
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:${VERSION_DOTNET_SDK} AS desarrollo
# Accepted so the shared compose build args are not reported as unused; the
# development stage always compiles in Debug through `dotnet watch`.
ARG CONFIGURACION=Debug
WORKDIR /app
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY src/backend src/backend
COPY database/entidades-gubernamentales.seed.json database/entidades-gubernamentales.seed.json
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Development \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=1
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=5s --start-period=120s --retries=10 \
    CMD curl --fail --silent http://127.0.0.1:8080/salud || exit 1
# ASPNETCORE_URLS above keeps the container on 8080; --no-launch-profile stops
# launchSettings.json from rebinding the app to localhost:5294.
CMD ["dotnet", "watch", "run", "--project", "src/backend/SB.GestionSolicitudes.Servicios", "--no-launch-profile"]

# ------------------------------------------------------------------------------
# Final image: non-root, writable folders for the plain-text catalog and the logs.
# ------------------------------------------------------------------------------
FROM base AS final
WORKDIR /app
COPY --from=publicacion --chown=app:app /app/publicacion .
# `/app/database` holds the government-entities catalog (seeded from the published
# `entidades-gubernamentales.seed.json`); `/app/logs` is where Serilog writes.
# Relational data is not on disk here: it lives in the SQL Server instance the
# connection string points to.
RUN mkdir -p /app/database /app/logs \
    && chown -R app:app /app/database /app/logs
USER app
HEALTHCHECK --interval=15s --timeout=5s --start-period=45s --retries=5 \
    CMD curl --fail --silent http://127.0.0.1:8080/salud || exit 1
ENTRYPOINT ["dotnet", "SB.GestionSolicitudes.Servicios.dll"]
