# SB · Plataforma de Gestión de Solicitudes Internas y Entidades Gubernamentales

Solución integral Full Stack desarrollada para la **Superintendencia de Bancos de la República Dominicana (SB)** según las especificaciones técnicas requeridas.

La plataforma implementa un sistema empresarial para registrar, dar seguimiento, clasificar y auditar solicitudes internas de servicios tecnológicos, acompañado de un mantenimiento completo para el catálogo oficial de entidades gubernamentales de la República Dominicana, persistido en archivo de texto plano.

---

## 🏛️ Características Principales

- **Arquitectura Cebolla (Onion Architecture)**: Estricta separación por capas (`Dominio`, `Aplicación`, `Infraestructura`, `Servicios/API`, `Pruebas`).
- **Autenticación y Autorización (RBAC)**: Tokens JWT con roles mínimos definidos (`Administrador`, `Analista`, `Solicitante`).
- **Gestión Completa de Solicitudes**:
  - Códigos unívocos legibles autogenerados (formato `SOL-YYYY-XXXX`).
  - Ciclo de vida con estados: *Registrada*, *En análisis*, *En progreso*, *En espera del solicitante*, *Resuelta* y *Cerrada*.
  - Validación obligatoria de comentario de resolución para el cierre.
  - Reapertura de solicitudes cerradas restringida a *Administrador* y *Analista*.
  - Trazabilidad y auditoría completa de cambios de estado y comentarios internos/públicos.
- **Tablero de Indicadores (Dashboard)**: Métricas en tiempo real de solicitudes abiertas, cerradas, vencidas y por prioridad.
- **Mantenimiento de Entidades Gubernamentales**:
  - Cumplimiento de la norma: persistencia en **archivo de texto plano JSON** dentro del directorio del proyecto (`database/entidades-gubernamentales.json`), con semáforo de concurrencia asíncrono.
  - Precargado a partir del archivo oficial `ListaEntidadesGubernamentales.xlsx`.
- **Notificaciones Desacopladas**:
  - Arquitectura multi-canal lista para extender (`Base de Datos`, `Correo Simulado`, `Consola`, `Colas de Mensajería`).
- **Documentación Dual de API**:
  - Interfaz interactiva **Scalar** en `/docs/v1`.
  - Especificación **Swagger / OpenAPI** en `/swagger`.
- **Frontend Moderno y Fiel a la Identidad SB**:
  - Construido con **React 18 + TypeScript + Vite + Tailwind CSS**.
  - Cumplimiento de colores institucionales: Azul institucional `rgba(13, 48, 72, .9)`, Gris neutro `rgba(237, 240, 247)`, logotipo oficial SB e icono vectorial `home.svg`.

---

## 📋 Estructura del Repositorio

```text
├── database/
│   ├── entidades-gubernamentales.json       # Base de datos en archivo plano (RD)
│   ├── entidades-gubernamentales.seed.json  # Copia semilla del catálogo gubernamental
│   ├── schema.sql                           # Script DDL de creación de tablas e índices
│   └── seed.sql                             # Script DML con datos semilla reproducibles
├── docs/
│   ├── arquitectura.md                      # Descripción exhaustiva de arquitectura y patrones
│   ├── evidencias.md                        # Guía del flujo principal y evidencias de pruebas
├── src/
│   ├── backend/
│   │   ├── SB.GestionSolicitudes.sln        # Solución .NET 8
│   │   ├── SB.GestionSolicitudes.Dominio/   # Núcleo: Entidades, Enums, Reglas Invariantes
│   │   ├── SB.GestionSolicitudes.Aplicacion/# Casos de uso, DTOs, Contratos e Interfaces
│   │   ├── SB.GestionSolicitudes.Infraestructura/ # EF Core, Archivo plano, Seguridad, Canales
│   │   ├── SB.GestionSolicitudes.Servicios/ # ASP.NET Core Web API, Middlewares, Controllers
│   │   │   └── Dockerfile                   # Imagen multi-etapa del API (runtime, pruebas, desarrollo)
│   │   └── SB.GestionSolicitudes.Pruebas/   # Suite de 118 Pruebas Unitarias e Integración
│   └── frontend/                            # Aplicación React + TypeScript + Vite + Tailwind
│       ├── Dockerfile                       # Imagen multi-etapa (build, desarrollo, nginx)
│       └── nginx.conf                       # Servidor estático + proxy hacia el API
├── compose.yaml                             # Entorno tipo producción (API + Web)
├── compose.dev.yaml                         # Superposición de desarrollo con recarga en caliente
├── compose.test.yaml                        # Ejecutor de la suite de pruebas en contenedor
├── .env.example                             # Variables de Compose documentadas
└── README.md                                # Documentación de inicio rápido
```

---

## 🚀 Requisitos Previos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) (recomendado v20 o v24)
- Git
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) con Compose v2.24+ *(solo para la ejecución containerizada)*

---

## ⚙️ Instrucciones de Ejecución

### 1. Backend (.NET 8 Web API)

Desde la raíz del proyecto:

```bash
# Restaurar dependencias y compilar
dotnet build src/backend/SB.GestionSolicitudes.sln

# Ejecutar el API Web
dotnet run --project src/backend/SB.GestionSolicitudes.Servicios
```

El backend se iniciará escuchando por defecto en:
- **API Base**: `http://localhost:5080`
- **Documentación Scalar**: `http://localhost:5080/docs/v1`
- **Swagger UI**: `http://localhost:5080/swagger`
- **Health Check**: `http://localhost:5080/salud`

> **Nota de inicialización:** Al arrancar por primera vez, el backend aplica automáticamente las migraciones de EF Core y carga los datos semilla tanto en la base de datos relacional (`database/sb-gestion-solicitudes.db`) como en el archivo plano de entidades (`database/entidades-gubernamentales.json`).

---

### 2. Frontend (React + TypeScript)

En una segunda terminal:

```bash
cd src/frontend

# Instalar dependencias (si no se han instalado)
npm install

# Iniciar servidor de desarrollo
npm run dev
```

La aplicación web estará disponible en:
- `http://localhost:5173`

Para generar la compilación de producción:
```bash
npm run build
```

---

### 3. Todo el Stack con Docker (API + Web)

La ruta más rápida para levantar la plataforma completa sin instalar .NET ni Node. El API corre en la imagen `aspnet:8.0.32` con un usuario sin privilegios y la web se sirve con nginx, que además actúa como proxy inverso de `/api` y `/salud`.

```bash
# 1. Variables opcionales (todas tienen valores por defecto)
cp .env.example .env

# 2. Construir y levantar ambos servicios
docker compose up --build -d
```

| Servicio | URL | Descripción |
| :--- | :--- | :--- |
| **Web** | `http://localhost:8080` | Aplicación React servida por nginx |
| **API** | `http://localhost:5080` | API REST sin intermediarios |
| **Swagger** | `http://localhost:5080/swagger` | Documentación interactiva |
| **Scalar** | `http://localhost:5080/docs/v1` | Documentación alternativa |
| **Salud** | `http://localhost:5080/salud` | Sonda usada por los healthchecks |

Notas de operación:

- **Persistencia:** el API guarda la base SQLite y el catálogo de entidades en el volumen `sb-gestion-solicitudes-datos` (montado en `/app/database`), inicializado desde `entidades-gubernamentales.seed.json` en el primer arranque. Para reiniciar desde cero: `docker compose down -v`.
- **Secreto JWT:** en `Production` el API exige `JWT_CLAVE_SECRETA` de 32 caracteres o más. El valor por defecto solo sirve para una demo local; cámbielo en su `.env` antes de publicar la imagen.
- **Logs:** Serilog escribe en consola (visible con `docker compose logs -f api`) y en `/app/logs` dentro del contenedor.
- **Puertos:** se ajustan con `PUERTO_API` y `PUERTO_WEB` en el archivo `.env`.
- **Versiones reproducibles:** las imágenes fijan el mismo toolchain declarado en `mise.toml` (SDK `8.0.426` con runtime ASP.NET Core `8.0.32`, Node `24.22.1`), para que el contenedor y la máquina local compilen con versiones idénticas.

#### Entorno de desarrollo con recarga en caliente

Compone la misma definición base con la superposición de desarrollo: el API corre con `dotnet watch` y la web con el servidor de Vite.

```bash
docker compose -f compose.yaml -f compose.dev.yaml up --build
```

- Web: `http://localhost:5173`
- API: `http://localhost:5080`
- El directorio `database/` del repositorio se monta en el contenedor, por lo que se comparte el mismo archivo plano que en la ejecución local.
- Los artefactos de compilación (`bin/`, `obj/`) y las dependencias de Node viven en volúmenes propios del contenedor, de modo que el entorno de desarrollo no contamina ni sobrescribe lo compilado en Windows.
- Si cambian `package.json` o `package-lock.json`, reconstruya la imagen web: `docker compose -f compose.yaml -f compose.dev.yaml up --build`.

#### Pruebas automatizadas en contenedor

```bash
docker compose -f compose.test.yaml run --rm pruebas
```

La etapa `pruebas` del Dockerfile compila y ejecuta los 118 tests con el SDK oficial; el código de salida del contenedor es el de `dotnet test`.

---

### 4. Base de Datos y Migraciones

La base de datos relacional opera sobre SQLite por defecto (configurable a SQL Server / Oracle en `appsettings.json`).

- Para regenerar el esquema o aplicar migraciones manualmente:
  ```bash
  dotnet ef database update --project src/backend/SB.GestionSolicitudes.Infraestructura --startup-project src/backend/SB.GestionSolicitudes.Servicios
  ```
- Para inspeccionar el modelo SQL:
  - `database/schema.sql`: Contiene el DDL completo con tablas, restricciones e índices.
  - `database/seed.sql`: Contiene los scripts DML de inserción con los usuarios y solicitudes de prueba.

---

## 🔑 Credenciales de Prueba (Usuarios Semilla)

El sistema provee 3 cuentas de usuario preconfiguradas con diferentes roles para evaluar los permisos:

| Correo Electrónico | Contraseña | Rol | Alcance de Permisos |
| :--- | :--- | :--- | :--- |
| `admin@sb.gob.do` | `Admin123!` | **Administrador** | Acceso total: Dashboard, creación, consulta, cambio de estado, asignación, reabrir solicitudes, entidades gubernamentales y gestión de usuarios. |
| `analista@sb.gob.do` | `Analista123!` | **Analista** | Gestión operativa: Ver solicitudes asignadas/disponibles, cambiar estados, comentarios internos/públicos, reabrir solicitudes cerradas. |
| `solicitante@sb.gob.do` | `Solicitud123!` | **Solicitante** | Autoservicio: Registro de solicitudes nuevas, consulta de sus propias solicitudes y comentarios públicos. |

---

## 🧪 Ejecución de Pruebas Automatizadas

El proyecto incluye **118 pruebas automatizadas** que validan la lógica de negocio, reglas de transición de estados, seguridad de tokens JWT, persistencia concurrente en archivo plano y endpoints de integración HTTP con `WebApplicationFactory`.

Para ejecutar la suite completa de pruebas:

```bash
dotnet test src/backend/SB.GestionSolicitudes.sln
```

Resultado esperado:
```text
Passed!  - Failed: 0, Passed: 118, Skipped: 0, Total: 118
```

---

## 📚 Documentación Adicional

- [docs/arquitectura.md](file:///C:/Users/deuri/projects/wepsys/docs/arquitectura.md): Explicación detallada de la arquitectura cebolla, principios SOLID, patrones aplicados y decisiones técnicas.
- [docs/evidencias.md](file:///C:/Users/deuri/projects/wepsys/docs/evidencias.md): Guía funcional paso a paso, ejemplos de peticiones cURL y evidencias de verificación.
