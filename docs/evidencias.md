# Evidencias de Ejecución y Validación del Sistema

Este documento certifica el cumplimiento funcional y técnico de la solución desarrollada para la **Superintendencia de Bancos de la República Dominicana (SB)**.

---

## 1. Validación de Pruebas Automatizadas

El proyecto cuenta con **118 pruebas automatizadas** entre pruebas unitarias (de dominio, servicios, seguridad y repositorio plano) y pruebas de integración sobre endpoints HTTP reales utilizando `WebApplicationFactory<Program>`.

### Resultado de Ejecución de Pruebas
```text
Test run for C:\Users\deuri\projects\wepsys\src\backend\SB.GestionSolicitudes.Pruebas\bin\Debug\net8.0\SB.GestionSolicitudes.Pruebas.dll (.NETCoreApp,Version=v8.0)
VSTest version 17.11.1 (x64)

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:   118, Skipped:     0, Total:   118, Duration: 12 s - SB.GestionSolicitudes.Pruebas.dll (net8.0)
```

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
