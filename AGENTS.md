# AGENTS.md — SB · Gestión de Solicitudes

> Audience: AI agents (Antigravity, Codex, Claude, Gemini, Copilot, etc.) and human contributors.
> Read this file in full before touching any code. Every section is load-bearing.

---

## 1. Project Overview

**SB · Gestión de Solicitudes** is a full-stack enterprise platform built for the
**Superintendencia de Bancos de la República Dominicana (SB)**.

It manages the complete lifecycle of internal technology service requests, and includes a
maintenance module for the official catalogue of Dominican governmental entities persisted
in a flat-file JSON store (as required by the technical specification).

| Property          | Value                                                      |
| :---------------- | :--------------------------------------------------------- |
| **Owner**         | Deuri Vasquez — deuriib@gmail.com                          |
| **Architecture**  | Onion Architecture (Domain → Application → Infrastructure → Services) |
| **Backend**       | C# · .NET 8 · ASP.NET Core Web API                        |
| **Frontend**      | React 18 · TypeScript · Vite · Tailwind CSS                |
| **Auth**          | JWT Bearer (RBAC — roles: `Administrador`, `Analista`, `Solicitante`) |
| **Primary DB**    | SQL Server 2022 via EF Core 8                              |
| **Flat-file DB**  | `database/entidades-gubernamentales.json` (async semaphore-guarded) |
| **Logging**       | Serilog (console + rolling file)                           |
| **API Docs**      | Scalar (`/docs/v1`) + Swagger UI (`/swagger`)              |
| **Tests**         | 144 automated tests (unit + integration via Testcontainers) |
| **CI**            | GitHub Actions — `.github/workflows/ci.yml`                |
| **Task runner**   | [mise](https://mise.jdx.dev/) — `mise.toml`               |

---

## 2. Repository Layout

```text
├── database/
│   ├── entidades-gubernamentales.json       # Flat-file store (must NOT migrate to SQL)
│   ├── entidades-gubernamentales.seed.json  # Seed copy of the governmental catalogue
│   ├── schema.sql                           # Idempotent T-SQL DDL (reference only)
│   └── seed.sql                             # Reference seed data in T-SQL
├── docs/
│   ├── arquitectura.md                      # Architecture decisions and patterns
│   ├── evidencias.md                        # Functional walkthrough and test evidence
│   └── instrucciones.txt                    # Original technical specification (source of truth)
├── src/
│   ├── backend/                             # .NET 8 solution
│   │   ├── SB.GestionSolicitudes.sln
│   │   ├── SB.GestionSolicitudes.Dominio/         # Layer 1 — Entities, Enums, Business Rules
│   │   ├── SB.GestionSolicitudes.Aplicacion/      # Layer 2 — Use cases, DTOs, Interfaces
│   │   ├── SB.GestionSolicitudes.Infraestructura/ # Layer 3 — EF Core, Flat-file, JWT, Notifications
│   │   ├── SB.GestionSolicitudes.Servicios/       # Layer 4 — ASP.NET Core API, Controllers, Middleware
│   │   └── SB.GestionSolicitudes.Pruebas/         # Test suite (144 tests)
│   └── frontend/                            # React + TypeScript SPA
│       ├── src/
│       ├── Dockerfile
│       └── nginx.conf                       # Static server + API reverse proxy
├── .github/
│   ├── workflows/ci.yml                     # CI pipeline
│   └── agents/                              # GitHub Copilot agent definitions
├── .agents/skills/                          # Antigravity skill definitions
├── .opencode/agents/                        # OpenCode agent definitions
├── compose.yaml                             # Production-like Docker stack
├── compose.dev.yaml                         # Hot-reload development overlay
├── compose.test.yaml                        # Containerized test runner
├── mise.toml                                # Toolchain versions + task definitions
├── .env.example                             # Environment variables template (copy → .env)
└── README.md                                # Quick-start guide
```

---

## 3. Toolchain & Versions

Versions are pinned in [`mise.toml`](mise.toml). Always match them.

| Tool       | Version    |
| :--------- | :--------- |
| .NET SDK   | `8.0.426`  |
| ASP.NET Core Runtime | `8.0.32` |
| Node.js    | `24.22.1`  |

Install everything in one command:

```bash
mise install
mise run setup   # dotnet restore + npm ci
```

---

## 4. Quick Start

### Local (no Docker)

```bash
# 1. Start SQL Server
docker compose up -d sqlserver

# 2. Backend
dotnet run --project src/backend/SB.GestionSolicitudes.Servicios
# → API: http://localhost:5080
# → Scalar docs: http://localhost:5080/docs/v1
# → Swagger: http://localhost:5080/swagger
# → Health: http://localhost:5080/salud

# 3. Frontend (second terminal)
cd src/frontend && npm run dev
# → http://localhost:5173
```

### Full Docker Stack

```bash
cp .env.example .env   # set JWT_CLAVE_SECRETA and SQLSERVER_CLAVE_SA
docker compose up --build -d
# → Web: http://localhost:8080
# → API: http://localhost:5080
```

### mise Tasks

| Task                     | What it does                                                   |
| :----------------------- | :------------------------------------------------------------- |
| `mise run backend:build` | Restore + compile the .NET solution                            |
| `mise run backend:test`  | Run the full test suite (Docker required for integration tests) |
| `mise run backend:run`   | Start the API (requires a valid SQL Server connection string)  |
| `mise run frontend:dev`  | Start the Vite dev server                                      |
| `mise run frontend:build`| TypeScript type-check + production build                       |
| `mise run docker:arriba` | Build and start the production Docker stack                    |
| `mise run docker:desarrollo` | Hot-reload stack (`dotnet watch` + Vite)                  |
| `mise run docker:pruebas`| Run tests inside a container                                   |
| `mise run docker:abajo`  | Stop containers (data volumes preserved)                       |
| `mise run docker:limpiar`| **Destructive:** stop containers and delete data volumes       |

---

## 5. Architecture — Onion Layers

Dependency flow is strictly **inward only**: outer layers depend on inner layers, never the reverse.

```
┌─────────────────────────────────────────────┐
│  Servicios (ASP.NET Core Web API)            │  ← HTTP, Controllers, Middleware, DI
│  ┌───────────────────────────────────────┐   │
│  │  Infraestructura                      │   │  ← EF Core, File store, JWT impl, Notifications
│  │  ┌─────────────────────────────────┐  │   │
│  │  │  Aplicacion                     │  │   │  ← Use cases, DTOs, Interfaces, Services
│  │  │  ┌───────────────────────────┐  │  │   │
│  │  │  │  Dominio                  │  │  │   │  ← Entities, Enums, Business rules, Exceptions
│  │  │  │  (zero external deps)     │  │  │   │
│  │  │  └───────────────────────────┘  │  │   │
│  │  └─────────────────────────────────┘  │   │
│  └───────────────────────────────────────┘   │
└─────────────────────────────────────────────┘
```

**Absolute rule:** `Dominio` has zero NuGet dependencies. `Aplicacion` depends only on `Dominio`.
Any violation of the dependency direction is a blocking defect.

### Project ↔ Layer Mapping

| Project                              | Layer          | Responsibility                                  |
| :----------------------------------- | :------------- | :---------------------------------------------- |
| `SB.GestionSolicitudes.Dominio`      | Domain         | Entities, Enums, `ExcepcionReglaNegocio`        |
| `SB.GestionSolicitudes.Aplicacion`   | Application    | Use-case services, DTOs, contracts (interfaces) |
| `SB.GestionSolicitudes.Infraestructura` | Infrastructure | EF Core context, flat-file repo, JWT, notifications |
| `SB.GestionSolicitudes.Servicios`    | Services/API   | Controllers, Middleware, DI wiring, `Program.cs` |
| `SB.GestionSolicitudes.Pruebas`      | Tests          | Unit + integration tests                        |

---

## 6. Naming Conventions

These are non-negotiable. CI will reject PRs that violate them.

| Construct         | Convention      | Example                          |
| :---------------- | :-------------- | :------------------------------- |
| Classes           | `PascalCase`    | `SolicitudServicio`              |
| Methods           | `PascalCase`    | `ObtenerPorId`                   |
| Enums & values    | `PascalCase`    | `EstadoSolicitud.EnAnalisis`     |
| Properties        | `PascalCase`    | `FechaCreacion`                  |
| Local variables   | `camelCase`     | `solicitudActual`                |
| Parameters        | `camelCase`     | `solicitudId`                    |
| Constants         | `UPPER_SNAKE`   | `CODIGO_PREFIJO`                 |
| Interfaces        | `I` + PascalCase | `ISolicitudRepositorio`          |
| No abbreviations  | Write it out    | `Repositorio` not `Repo`         |
| No magic numbers  | Named constant  | `const int LIMITE_PAGINA = 50;`  |

**Connection strings** must live in `appsettings.json` (or environment variable overrides).
Never hardcode credentials in source code (DENY-01).

---

## 7. Coding Standards

### C# / Backend

- **Result pattern**: use `Resultado<T>` from `SB.GestionSolicitudes.Aplicacion.Comun` — never throw from application or infrastructure layers for expected failure paths.
- **Business rule violations**: throw `ExcepcionReglaNegocio` from the domain, nowhere else.
- **Flat-file concurrency**: the JSON file repository uses an `async SemaphoreSlim` — any refactor must preserve this guard. Do not access the file directly from application or domain code.
- **EF Core**: queries must be async (`ToListAsync`, `FirstOrDefaultAsync`). No synchronous DB calls.
- **Pagination**: use `ParametrosPaginacion` + `ResultadoPaginado<T>` for every list endpoint.
- **Serilog**: inject `ILogger<T>` — never use the static `Log.` API in application/domain code.
- **SQL**: parameterized queries only. String-built SQL is forbidden (DENY-03).

### TypeScript / Frontend

- Strict TypeScript (`"strict": true` in `tsconfig.json`). No `any` without an explicit comment explaining why.
- Data fetching via **TanStack Query** — no raw `fetch` calls outside of query/mutation functions.
- Routing via **React Router v6** — nested routes with `<Outlet>`.
- Styling via **Tailwind CSS** with institutional palette:
  - Blue: `rgba(13, 48, 72, .9)` → `[rgba(13,48,72,0.9)]`
  - Gray: `rgba(237, 240, 247)` → `[rgba(237,240,247)]`
- Components in `src/componentes/`, pages in `src/paginas/`, session/auth state in `src/sesion/`.

---

## 8. Authentication & Authorization

- **Mechanism**: JWT Bearer with HMAC-SHA256 signature.
- **Secret**: delivered via `JWT_CLAVE_SECRETA` environment variable — minimum 32 characters. Never committed to the repository (DENY-01).
- **Token lifetime**: `Jwt.MinutosVigencia` in `appsettings.json` (default 120 min).
- **Roles** (RBAC):

  | Role             | Permissions summary                                                |
  | :--------------- | :----------------------------------------------------------------- |
  | `Administrador`  | Full access: all endpoints, user management, reopen closed tickets |
  | `Analista`       | Operational: assigned tickets, state transitions, comments, reopen |
  | `Solicitante`    | Self-service: create and view own tickets, public comments         |

- **Seed accounts** (development only — change before any production deploy):

  | Email                        | Password       | Role            |
  | :--------------------------- | :------------- | :-------------- |
  | `admin@sb.gob.do`            | `Admin123!`    | Administrador   |
  | `analista@sb.gob.do`         | `Analista123!` | Analista        |
  | `solicitante@sb.gob.do`      | `Solicitud123!`| Solicitante     |

---

## 9. Environment Variables

All configuration is driven by the `.env` file (copy from `.env.example`).
The following variables are **required** before running any stack:

| Variable                  | Description                                          |
| :------------------------ | :--------------------------------------------------- |
| `JWT_CLAVE_SECRETA`       | JWT signing secret — min 32 chars. Never commit.     |
| `SQLSERVER_CLAVE_SA`      | SQL Server SA password. Never commit.                |
| `PUERTO_API`              | Host port for the API (default `5080`)               |
| `PUERTO_WEB`              | Host port for the web app (default `8080`)           |
| `PUERTO_SQLSERVER`        | Host port for SQL Server (default `1433`)            |

Override `appsettings.json` values at runtime via environment variables using the pattern
`ConnectionStrings__BaseDatos`, `Jwt__ClaveSecreta`, etc.

---

## 10. Flat-file Store — Governmental Entities

> This is a hard requirement from the technical specification. Do not migrate to SQL.

- **File location**: `database/entidades-gubernamentales.json`
- **Seed file**: `database/entidades-gubernamentales.seed.json` (read-only reference)
- **Concurrency**: guarded by `SemaphoreSlim(1, 1)` in `SB.GestionSolicitudes.Infraestructura`
- **Configuration**: paths are declared in `appsettings.json` under `AlmacenEntidadesGubernamentales`
- **Docker**: mounted as the volume `sb-gestion-solicitudes-datos` at `/app/database`

Any agent touching this module must preserve async concurrency semantics.

---

## 11. Database & Migrations

- **ORM**: Entity Framework Core 8 (`Microsoft.EntityFrameworkCore.SqlServer 8.0.10`)
- **Connection string key**: `ConnectionStrings:BaseDatos`
- **Migrations location**: `src/backend/SB.GestionSolicitudes.Infraestructura/Persistencia/Migraciones`
- **Auto-applied on startup**: yes — the API applies pending migrations and runs `SemillaDatos` on first boot.

To apply manually:

```bash
dotnet ef database update \
  --project src/backend/SB.GestionSolicitudes.Infraestructura \
  --startup-project src/backend/SB.GestionSolicitudes.Servicios
```

To add a new migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/backend/SB.GestionSolicitudes.Infraestructura \
  --startup-project src/backend/SB.GestionSolicitudes.Servicios
```

> **DENY-11 applies here.** Schema changes that drop columns or tables require a verified rollback
> plan before running in any shared environment.

---

## 12. Testing

- **Suite**: 144 tests — unit and integration.
- **Integration tests**: spin up a disposable SQL Server instance via **Testcontainers**. Requires Docker to be running.
- **Run everything**:

  ```bash
  dotnet test src/backend/SB.GestionSolicitudes.sln
  ```

- **Expected result**: `Failed: 0, Passed: 144, Skipped: 0, Total: 144`
- **In a container**:

  ```bash
  docker compose -f compose.test.yaml run --rm pruebas
  ```

- **CI**: tests run on every PR to `main` and every push to `main`. A red suite is a **hard block** — do not bypass (DENY-04).

---

## 13. Continuous Integration

Pipeline: [`.github/workflows/ci.yml`](.github/workflows/ci.yml)

Triggers: `push` to `main`, `pull_request` targeting `main`, manual dispatch.

| Job                  | What it validates                                                |
| :------------------- | :--------------------------------------------------------------- |
| `backend-tests`      | `dotnet test` — all 144 tests; integration tests use a SQL Server service container in CI (no Docker socket required on the runner) |
| `frontend-build`     | `tsc --noEmit && vite build` — type-safe production build       |
| `container-build`    | Multi-stage Docker builds for API and web (runs after both pass)|
| `dependency-review`  | Fails on high/critical severity dependency changes (PRs only)   |

---

## 14. Git Workflow

- **Branch strategy**: `main` is the production branch. Feature work goes on named branches (`feature/`, `fix/`, `chore/`).
- **No direct push to `main`** (DENY-12). Every change goes through a PR with at minimum CI passing.
- **No self-merge** (DENY-12).
- **Commit style**: Conventional Commits — `feat:`, `fix:`, `chore:`, `docs:`, `test:`, `refactor:`.
- **Commit scope**: one logical change per commit.

---

## 15. Documentation Rules (DOC-01 → DOC-06)

| Rule   | Requirement                                                                    |
| :----- | :----------------------------------------------------------------------------- |
| DOC-01 | Any behavior, convention, or structural change ships with its nearest README/AGENTS.md update **in the same commit**. No "docs later." |
| DOC-02 | Update the closest doc to the change first; touch root AGENTS.md only for cross-cutting changes. |
| DOC-03 | Docs in English. Preserve existing structure. Code examples must be verified.  |
| DOC-04 | Always update the CLI table or behavior section in the owning README.          |
| DOC-05 | No drive-by rewrites of untouched docs. No bundling unrelated polish into a behavior PR. |
| DOC-06 | Trivial-only edits need no code change; verify with `git status` showing both code + docs when a behavior trigger applies. |

---

## 16. Hard Gates (Deny Rules)

No exceptions. No silent passes.

| Rule    | What is forbidden                                                                |
| :------ | :------------------------------------------------------------------------------- |
| DENY-01 | Emitting secrets or PII anywhere — code, config, logs, commits, prompts, examples. |
| DENY-03 | String-built SQL, `eval`, shell injection, path traversal, unsafe deserialization. Use parameterized queries only. |
| DENY-04 | Disabling, skipping, or weakening a test gate, scan, or CI check to make a build pass. |
| DENY-05 | Manual production changes outside the pipeline. Pipeline is the only path to production. |
| DENY-11 | Destructive schema migration or data deletion without backup + rehearsed rollback + check. Halt and escalate. |
| DENY-12 | Self-merge, direct push to `main`, unreviewed pipeline changes.                  |

---

## 17. Agent Onboarding Checklist

Before making any code change:

- [ ] Read this file in full.
- [ ] Read `docs/arquitectura.md` to understand pattern decisions.
- [ ] Run `mise install && mise run setup` to verify the toolchain.
- [ ] Confirm `dotnet test` passes (144/144) with Docker running (Testcontainers) or `ConnectionStrings__BaseDatosPruebas` set.
- [ ] Check `.env.example` to understand required variables.
- [ ] Identify the correct Onion layer for your change — do not cross layer boundaries.
- [ ] Verify your change does not introduce a dependency violation (inner → outer).
- [ ] Ensure no credentials or PII are written anywhere (DENY-01).

---

## 18. Glossary

| Term | Meaning                                               |
| :--- | :---------------------------------------------------- |
| SB   | Superintendencia de Bancos de la República Dominicana |
| JWT  | JSON Web Token                                        |
| RBAC | Role-Based Access Control                             |
| EF Core | Entity Framework Core                              |
| Onion | Onion Architecture (layered, inward-only dependencies) |
| SOL  | Prefix for auto-generated request codes (`SOL-YYYY-XXXX`) |
