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
│   ├── schema.sql                           # DDL T-SQL idempotente: tablas, restricciones e índices
│   └── seed.sql                             # Datos de referencia en T-SQL (SQL Server)
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
│   │   └── SB.GestionSolicitudes.Pruebas/   # Suite de 127 Pruebas Unitarias e Integración
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
- **SQL Server 2022**: basta el contenedor oficial `mcr.microsoft.com/mssql/server:2022-latest` que levanta Compose; también sirve cualquier instancia accesible por cadena de conexión.
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) con Compose v2.24+ *(para el SQL Server local, la ejecución containerizada y las pruebas de integración, que levantan su propio contenedor)*

---

## ⚙️ Instrucciones de Ejecución

### 1. Backend (.NET 8 Web API)

Desde la raíz del proyecto:

```bash
# 1. Levantar SQL Server 2022 en segundo plano (puerto 1433)
docker compose up -d sqlserver

# 2. Restaurar dependencias y compilar
dotnet build src/backend/SB.GestionSolicitudes.sln

# 3. Ejecutar el API Web
dotnet run --project src/backend/SB.GestionSolicitudes.Servicios
```

El backend se iniciará escuchando por defecto en:
- **API Base**: `http://localhost:5080`
- **Documentación Scalar**: `http://localhost:5080/docs/v1`
- **Swagger UI**: `http://localhost:5080/swagger`
- **Health Check**: `http://localhost:5080/salud`

> **Nota de inicialización:** en el primer arranque el backend aplica las migraciones de EF Core sobre SQL Server, crea el esquema si hace falta y carga los datos semilla (usuarios, áreas, tipos y solicitudes de demostración). El catálogo de entidades gubernamentales sigue en el archivo de texto plano `database/entidades-gubernamentales.json`, tal como exige la especificación técnica.
>
> La cadena base vive en `src/backend/SB.GestionSolicitudes.Servicios/appsettings.json` y apunta a la instancia local de SQL Server (`localhost,1433`, base `SBGestionSolicitudes`). Suministre `ConnectionStrings__BaseDatos` mediante una variable de entorno para proporcionar la credencial fuera del repositorio.

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

### 3. Todo el Stack con Docker (API + Web + SQL Server)

La ruta más rápida para levantar la plataforma completa sin instalar .NET ni Node. El API corre en la imagen `aspnet:8.0.32` con un usuario sin privilegios y la web se sirve con nginx, que además actúa como proxy inverso de `/api` y `/salud`.

```bash
# 1. Configure credenciales únicas para JWT y SQL Server en `.env`
cp .env.example .env

# 2. Construir y levantar los servicios
docker compose up --build -d
```

| Servicio | URL | Descripción |
| :--- | :--- | :--- |
| **Web** | `http://localhost:8080` | Aplicación React servida por nginx |
| **API** | `http://localhost:5080` | API REST sin intermediarios |
| **Base de datos** | `localhost:1433` | SQL Server 2022 (`mcr.microsoft.com/mssql/server:2022-latest`) |
| **Swagger** | `http://localhost:5080/swagger` | Documentación interactiva |
| **Scalar** | `http://localhost:5080/docs/v1` | Documentación alternativa |
| **Salud** | `http://localhost:5080/salud` | Sonda usada por los healthchecks |

Notas de operación:

- **Persistencia relacional:** el servicio `sqlserver` publica el puerto 1433 (`PUERTO_SQLSERVER`) y guarda los datos en el volumen `sb-gestion-solicitudes-sqlserver` (`/var/opt/mssql`). El API aplica ahí las migraciones y la semilla en el primer arranque. `docker compose down -v` borra también ese volumen: la base vuelve a su estado inicial.
- **Primer arranque:** mientras EF Core crea la base `SBGestionSolicitudes`, SQL Server registra uno o dos `Error: 18456, State: 38` (*Failed to open the explicitly specified database*) porque el chequeo de existencia abre la conexión con un catálogo que todavía no existe. Es esperado, se resuelve solo en segundos y no reaparece en arranques posteriores; el API queda `healthy` después de aplicar la migración.
- **Persistencia del catálogo:** el volumen `sb-gestion-solicitudes-datos` (montado en `/app/database`) conserva únicamente el catálogo de entidades en JSON; se inicializa desde `entidades-gubernamentales.seed.json` en el primer arranque. Los logs de Serilog viven en `/app/logs`, dentro del contenedor.
- **Credenciales:** `JWT_CLAVE_SECRETA` (al menos 32 caracteres) y `SQLSERVER_CLAVE_SA` deben definirse en `.env` antes de iniciar Compose. No se incluyen valores de acceso predeterminados en el repositorio.
- **Logs:** Serilog escribe en consola (visible con `docker compose logs -f api`) y en `/app/logs` dentro del contenedor.
- **Puertos:** se ajustan con `PUERTO_API`, `PUERTO_WEB` y `PUERTO_SQLSERVER` en el archivo `.env`.
- **Versiones reproducibles:** las imágenes fijan el mismo toolchain declarado en `mise.toml` (SDK `8.0.426` con runtime ASP.NET Core `8.0.32`, Node `24.22.1`), para que el contenedor y la máquina local compilen con versiones idénticas.

#### Entorno de desarrollo con recarga en caliente

Compone la misma definición base con la superposición de desarrollo: el API corre con `dotnet watch` y la web con el servidor de Vite.

```bash
docker compose -f compose.yaml -f compose.dev.yaml up --build
```

- Web: `http://localhost:5173`
- API: `http://localhost:5080`
- El directorio `database/` del repositorio se monta en el contenedor, por lo que se comparte el mismo archivo plano que en la ejecución local.
- El API toma la instancia de SQL Server del servicio `sqlserver` de la misma composición, así que no hace falta una instancia externa.
- Los artefactos de compilación (`bin/`, `obj/`) y las dependencias de Node viven en volúmenes propios del contenedor, de modo que el entorno de desarrollo no contamina ni sobrescribe lo compilado en Windows.
- Si cambian `package.json` o `package-lock.json`, reconstruya la imagen web: `docker compose -f compose.yaml -f compose.dev.yaml up --build`.

#### Pruebas automatizadas en contenedor

```bash
docker compose -f compose.test.yaml run --rm pruebas
```

La etapa `pruebas` del Dockerfile compila y ejecuta los 127 tests con el SDK oficial; el código de salida del contenedor es el de `dotnet test`. La composición levanta además su propia instancia de SQL Server y le pasa al runner la cadena de conexión en `ConnectionStrings__BaseDatosPruebas`, de modo que cada prueba de integración crea su base desechable y la borra al terminar. El contenedor de la base queda arriba para la siguiente corrida; se retira con `docker compose -f compose.test.yaml down`.

---

### 4. Base de Datos y Migraciones

La base de datos relacional opera sobre **SQL Server 2022** mediante **Entity Framework Core 8** (proveedor `Microsoft.EntityFrameworkCore.SqlServer 8.0.10`). La cadena de conexión se define en `src/backend/SB.GestionSolicitudes.Servicios/appsettings.json` y se sobrescribe con la variable de entorno `ConnectionStrings__BaseDatos`.

- Para levantar la instancia local a la que apunta esa cadena de conexión:
  ```bash
  docker compose up -d sqlserver
  ```
  El contenedor toma la clave `SA` de `SQLSERVER_CLAVE_SA` en el `.env`. Para ejecutar el API directamente con `dotnet run`, configure `ConnectionStrings__BaseDatos` como variable de entorno usando esa credencial.
- Las migraciones versionadas viven en `src/backend/SB.GestionSolicitudes.Infraestructura/Persistencia/Migraciones`; el API las aplica y carga la semilla de demostración al arrancar (`SemillaDatos`).
- Para aplicar migraciones manualmente:
  ```bash
  dotnet ef database update --project src/backend/SB.GestionSolicitudes.Infraestructura --startup-project src/backend/SB.GestionSolicitudes.Servicios
  ```
- Scripts de referencia en T-SQL:
  - `database/schema.sql`: DDL idempotente con tablas, restricciones e índices.
  - `database/seed.sql`: datos de referencia reproducibles (usuarios, catálogos y solicitudes de ejemplo).
- El catálogo de entidades gubernamentales **no** usa SQL Server: permanece en `database/entidades-gubernamentales.json` por requisito de la especificación.

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

El proyecto incluye **127 pruebas automatizadas** que validan la lógica de negocio, reglas de transición de estados, seguridad de tokens JWT, persistencia concurrente en archivo plano, el contrato RFC 9457 de errores y endpoints de integración HTTP con `WebApplicationFactory`.

Para ejecutar la suite completa de pruebas:

```bash
dotnet test src/backend/SB.GestionSolicitudes.sln
```

Resultado esperado:
```text
Passed!  - Failed: 0, Passed: 127, Skipped: 0, Total: 127
```

### Pruebas de integración contra SQL Server

Cada prueba de integración levanta el API con su propia base de datos desechable (`SbPruebas_<guid>`) y la elimina al terminar. La instancia de SQL Server la provee **Testcontainers**: el primer test arranca un contenedor `mcr.microsoft.com/mssql/server:2022-latest`, lo comparte con el resto de la corrida y lo retira al final. No hay que instalar ni configurar SQL Server; solo tener Docker en ejecución.

```bash
# Requisito: Docker en ejecución. Nada más.
dotnet test src/backend/SB.GestionSolicitudes.sln
```

Si prefiere una instancia existente (CI con contenedor de servicio o un servidor propio), defina `SQLSERVER_CLAVE_SA` y `ConnectionStrings__BaseDatosPruebas` apuntando a `master`; en ese caso Testcontainers no se usa:

```powershell
$env:ConnectionStrings__BaseDatosPruebas = "Server=localhost,1433;Database=master;User Id=sa;Password=$env:SQLSERVER_CLAVE_SA;Encrypt=True;TrustServerCertificate=True"
dotnet test src/backend/SB.GestionSolicitudes.sln
```

El ejecutor en contenedor usa esa misma vía con la instancia de `compose.test.yaml`:

```bash
docker compose -f compose.test.yaml run --rm pruebas
```

---

## 📚 Documentación Adicional

- [docs/arquitectura.md](file:///C:/Users/deuri/projects/wepsys/docs/arquitectura.md): Explicación detallada de la arquitectura cebolla, principios SOLID, patrones aplicados y decisiones técnicas.
- [docs/evidencias.md](file:///C:/Users/deuri/projects/wepsys/docs/evidencias.md): Guía funcional paso a paso, ejemplos de peticiones cURL y evidencias de verificación.
