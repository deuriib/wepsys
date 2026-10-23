# Evidencias de Ejecución y Validación del Sistema

Este documento certifica el cumplimiento funcional y técnico de la solución desarrollada para la **Superintendencia de Bancos de la República Dominicana (SB)**.

---

## 1. Validación de Pruebas Automatizadas

El proyecto cuenta con **127 pruebas automatizadas** entre pruebas unitarias (de dominio, servicios, seguridad y repositorio plano) y pruebas de integración sobre endpoints HTTP reales utilizando `WebApplicationFactory<Program>`.

Las pruebas de integración corren contra **SQL Server 2022** real: **Testcontainers** levanta un contenedor efímero por corrida (`mcr.microsoft.com/mssql/server:2022-latest`) y cada prueba crea su propia base desechable `SbPruebas_<guid>`, que elimina al terminar. El único requisito es tener Docker en ejecución.

### Resultado de Ejecución de Pruebas

Comando ejecutado (sin variables de entorno: Testcontainers provee el motor):

```bash
dotnet test src/backend/SB.GestionSolicitudes.sln
```

```text
Test run for C:\Users\deuri\projects\wepsys\src\backend\SB.GestionSolicitudes.Pruebas\bin\Debug\net8.0\SB.GestionSolicitudes.Pruebas.dll (.NETCoreApp,Version=v8.0)
VSTest version 17.11.1 (x64)

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:   127, Skipped:     0, Total:   127, Duration: 2 m 48 s - SB.GestionSolicitudes.Pruebas.dll (net8.0)
```

La duración incluye crear, migrar, sembrar y eliminar 53 bases de datos sobre el contenedor de SQL Server; al terminar, el contenedor desaparece (lo retira Ryuk) y no queda ninguna base `SbPruebas_*` en la instancia.

---

### Validación de los Scripts T-SQL de Referencia

`database/schema.sql` y `database/seed.sql` se aplicaron sobre una base desechable (`SbValidacionScripts`) dentro del contenedor SQL Server 2022 de la composición, usando `sqlcmd` 18 con `-b` (aborta ante cualquier error):

```text
==== schema.sql ====
(1 rows affected)
EXIT=0

==== seed.sql (1a vez) ====
EXIT1=0

==== seed.sql (2a vez: idempotencia) ====
EXIT2=0

Tabla|Filas
-----|-----
Areas|6
TiposSolicitud|5
Usuarios|4
Solicitudes|5
HistorialesEstado|9
Comentarios|3
Notificaciones|3
```

La segunda ejecución de `seed.sql` no duplica filas: cada tabla está protegida con `IF NOT EXISTS` y los `IDENTITY_INSERT` se activan y desactivan por tabla. Los hashes PBKDF2-SHA256 de las cuentas semilla se comprobaron con 120 000 iteraciones.

---

## 2. Flujo Principal del Sistema

### 2.1 Autenticación y Generación de Token JWT
- **Endpoint**: `POST /api/auth/login`
- **Credenciales semilla documentadas**:
  - Administrador: `admin@sb.gob.do` / `Admin123!`
  - Analista: `analista@sb.gob.do` / `Analista123!`
  - Analista 2: `analista2@sb.gob.do` / `Analista123!`
  - Solicitante: `solicitante@sb.gob.do` / `Solicitud123!`

```bash
curl -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"correo": "admin@sb.gob.do", "contrasena": "Admin123!"}'
```

**Respuesta HTTP 200 OK**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiraEn": "2026-10-22T23:20:00Z",
  "usuario": {
    "id": 1,
    "nombre": "Ana Martínez",
    "correo": "admin@sb.gob.do",
    "rol": "Administrador"
  }
}
```

---

### 2.2 Tablero de Indicadores y Métricas Operativas
- **Endpoint**: `GET /api/dashboard/resumen`
- **Métricas provistas**: Total de solicitudes, solicitudes abiertas, solicitudes cerradas, solicitudes vencidas (según fecha de compromiso), desglose por estado y prioridad, y lista de últimas solicitudes registradas.

---

### 2.3 Ciclo de Vida de Solicitud Interna

1. **Creación**: `POST /api/solicitudes`
   - Generación automática de código legible correlativo (ej. `SOL-2026-0001`).
   - Estado inicial: `Registrada`.
   - Se emite notificación de creación en BD, correo simulado y consola.
2. **Asignación a Técnico**: `PATCH /api/solicitudes/{id}/asignacion`
   - Asigna analista responsable y añade entrada de auditoría al historial.
3. **Transición de Estados**: `PATCH /api/solicitudes/{id}/estado`
   - Flujo soportado: `Registrada` → `En análisis` → `En progreso` → `En espera del solicitante` → `Resuelta` → `Cerrada`.
   - **Regla de negocio verificada**: No se permite cerrar una solicitud sin comentario de resolución obligatorio.
   - **Reapertura controlada**: Solo usuarios con rol `Administrador` o `Analista` pueden reabrir solicitudes cerradas.
4. **Comentarios Colaborativos**: `POST /api/solicitudes/{id}/comentarios`
   - Comentarios públicos: visibles para el solicitante y analistas.
   - Comentarios internos: restringidos a roles técnicos (`Analista`, `Administrador`).

---

### 2.4 Mantenimiento de Entidades Gubernamentales (Archivo Plano)
- **Persistencia**: Archivo de texto plano JSON (`database/entidades-gubernamentales.json`).
- **Endpoints**:
  - `GET /api/entidades-gubernamentales` (Paginado, búsqueda por texto, filtro por categoría, poder del estado, sector y estado activo/inactivo).
  - `POST /api/entidades-gubernamentales` (Creación con validación de nombre único y campos requeridos).
  - `GET /api/entidades-gubernamentales/{id}` (Consulta individual).
  - `PUT /api/entidades-gubernamentales/{id}` (Actualización completa).
  - `DELETE /api/entidades-gubernamentales/{id}` (Eliminación segura).

---

## 3. Conformidad Visual con Maqueta SB

La interfaz desarrollada en React y Tailwind CSS respeta estrictamente los lineamientos gráficos de la maqueta:
- **Color Azul Institucional**: `rgba(13, 48, 72, .9)` aplicado a la barra lateral de navegación.
- **Color Gris Neutro de Fondo**: `rgba(237, 240, 247)` en el contenedor principal de pantallas.
- **Identidad Visual**: Logotipo oficial de la Superintendencia de Bancos ubicado en el encabezado superior y en la barra lateral.
- **Iconografía**: Icono `home.svg` provisto en recursos para el enlace Inicio, complementado con iconografía SVG consistente.
