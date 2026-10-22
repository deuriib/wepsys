-- =============================================================================
-- SUPERINTENDENCIA DE BANCOS DE LA REPÚBLICA DOMINICANA (SB)
-- Plataforma de Gestión de Solicitudes Internas y Notificaciones
-- Esquema de Base de Datos Relacional (DDL)
-- =============================================================================
-- Compatible con SQLite (ejecución por defecto) y convertible a SQL Server / Oracle.
-- Tablas normalizadas, restricciones de integridad referencial e índices de cobertura.
-- =============================================================================

PRAGMA foreign_keys = ON;

-- -----------------------------------------------------------------------------
-- Tabla: Areas
-- Catálogo de áreas organizacionales solicitantes y resolutoras
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Areas (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Nombre TEXT NOT NULL,
    Activa INTEGER NOT NULL DEFAULT 1
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_Areas_Nombre ON Areas (Nombre);

-- -----------------------------------------------------------------------------
-- Tabla: TiposSolicitud
-- Catálogo de clasificación de solicitudes (Incidente, Requerimiento, Acceso, etc.)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS TiposSolicitud (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Nombre TEXT NOT NULL,
    Descripcion TEXT NOT NULL,
    Activo INTEGER NOT NULL DEFAULT 1
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_TiposSolicitud_Nombre ON TiposSolicitud (Nombre);

-- -----------------------------------------------------------------------------
-- Tabla: Usuarios
-- Registro de usuarios del sistema con roles:
-- 1: Administrador, 2: Analista, 3: Solicitante
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Usuarios (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Nombre TEXT NOT NULL,
    Correo TEXT NOT NULL,
    HashContrasena TEXT NOT NULL,
    Rol INTEGER NOT NULL,
    Activo INTEGER NOT NULL DEFAULT 1,
    FechaCreacion TEXT NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_Usuarios_Correo ON Usuarios (Correo);

-- -----------------------------------------------------------------------------
-- Tabla: Solicitudes
-- Entidad principal de gestión de solicitudes internas
-- Prioridades: 1: Critica, 2: Alta, 3: Media, 4: Baja
-- Estados: 1: Registrada, 2: EnAnalisis, 3: EnProgreso, 4: EnEsperaDelSolicitante, 5: Resuelta, 6: Cerrada
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Solicitudes (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Codigo TEXT NOT NULL,
    Titulo TEXT NOT NULL,
    Descripcion TEXT NOT NULL,
    Prioridad INTEGER NOT NULL,
    Estado INTEGER NOT NULL,
    FechaCreacion TEXT NOT NULL,
    FechaCompromiso TEXT NOT NULL,
    FechaCierre TEXT NULL,
    AreaId INTEGER NOT NULL,
    TipoSolicitudId INTEGER NOT NULL,
    SolicitanteId INTEGER NOT NULL,
    ResponsableId INTEGER NULL,
    UrlEvidencia TEXT NULL,
    Secuencia INTEGER NOT NULL,
    CONSTRAINT FK_Solicitudes_Areas_AreaId FOREIGN KEY (AreaId) REFERENCES Areas (Id) ON DELETE RESTRICT,
    CONSTRAINT FK_Solicitudes_TiposSolicitud_TipoSolicitudId FOREIGN KEY (TipoSolicitudId) REFERENCES TiposSolicitud (Id) ON DELETE RESTRICT,
    CONSTRAINT FK_Solicitudes_Usuarios_SolicitanteId FOREIGN KEY (SolicitanteId) REFERENCES Usuarios (Id) ON DELETE RESTRICT,
    CONSTRAINT FK_Solicitudes_Usuarios_ResponsableId FOREIGN KEY (ResponsableId) REFERENCES Usuarios (Id) ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_Solicitudes_Codigo ON Solicitudes (Codigo);
CREATE INDEX IF NOT EXISTS IX_Solicitudes_AreaId ON Solicitudes (AreaId);
CREATE INDEX IF NOT EXISTS IX_Solicitudes_TipoSolicitudId ON Solicitudes (TipoSolicitudId);
CREATE INDEX IF NOT EXISTS IX_Solicitudes_SolicitanteId ON Solicitudes (SolicitanteId);
CREATE INDEX IF NOT EXISTS IX_Solicitudes_ResponsableId ON Solicitudes (ResponsableId);
CREATE INDEX IF NOT EXISTS IX_Solicitudes_Estado ON Solicitudes (Estado);
CREATE INDEX IF NOT EXISTS IX_Solicitudes_FechaCompromiso ON Solicitudes (FechaCompromiso);

-- -----------------------------------------------------------------------------
-- Tabla: Comentarios
-- Comentarios asociados a la solicitud con visibilidad:
-- 1: Publica (visible para solicitante y equipo), 2: Interna (solo analistas y admin)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Comentarios (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    SolicitudId INTEGER NOT NULL,
    Texto TEXT NOT NULL,
    Visibilidad INTEGER NOT NULL,
    Fecha TEXT NOT NULL,
    UsuarioId INTEGER NOT NULL,
    CONSTRAINT FK_Comentarios_Solicitudes_SolicitudId FOREIGN KEY (SolicitudId) REFERENCES Solicitudes (Id) ON DELETE CASCADE,
    CONSTRAINT FK_Comentarios_Usuarios_UsuarioId FOREIGN KEY (UsuarioId) REFERENCES Usuarios (Id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS IX_Comentarios_SolicitudId_Fecha ON Comentarios (SolicitudId, Fecha);
CREATE INDEX IF NOT EXISTS IX_Comentarios_UsuarioId ON Comentarios (UsuarioId);

-- -----------------------------------------------------------------------------
-- Tabla: HistorialesEstado
-- Pista de auditoría y trazabilidad de cada transición de ciclo de vida
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS HistorialesEstado (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    SolicitudId INTEGER NOT NULL,
    EstadoAnterior INTEGER NULL,
    EstadoNuevo INTEGER NOT NULL,
    Fecha TEXT NOT NULL,
    UsuarioId INTEGER NOT NULL,
    Comentario TEXT NULL,
    CONSTRAINT FK_HistorialesEstado_Solicitudes_SolicitudId FOREIGN KEY (SolicitudId) REFERENCES Solicitudes (Id) ON DELETE CASCADE,
    CONSTRAINT FK_HistorialesEstado_Usuarios_UsuarioId FOREIGN KEY (UsuarioId) REFERENCES Usuarios (Id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS IX_HistorialesEstado_SolicitudId_Fecha ON HistorialesEstado (SolicitudId, Fecha);
CREATE INDEX IF NOT EXISTS IX_HistorialesEstado_UsuarioId ON HistorialesEstado (UsuarioId);

-- -----------------------------------------------------------------------------
-- Tabla: Notificaciones
-- Registro de eventos y avisos generados por el ciclo de solicitudes
-- Canales: 1: BaseDeDatos, 2: CorreoSimulado, 3: Consola
-- Estados: 1: Pendiente, 2: Enviada, 3: Fallida
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Notificaciones (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    SolicitudId INTEGER NOT NULL,
    DestinatarioId INTEGER NOT NULL,
    Canal INTEGER NOT NULL,
    Asunto TEXT NOT NULL,
    Mensaje TEXT NOT NULL,
    Estado INTEGER NOT NULL,
    Fecha TEXT NOT NULL,
    Leida INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT FK_Notificaciones_Solicitudes_SolicitudId FOREIGN KEY (SolicitudId) REFERENCES Solicitudes (Id) ON DELETE CASCADE,
    CONSTRAINT FK_Notificaciones_Usuarios_DestinatarioId FOREIGN KEY (DestinatarioId) REFERENCES Usuarios (Id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS IX_Notificaciones_DestinatarioId_Leida ON Notificaciones (DestinatarioId, Leida);
CREATE INDEX IF NOT EXISTS IX_Notificaciones_SolicitudId ON Notificaciones (SolicitudId);
