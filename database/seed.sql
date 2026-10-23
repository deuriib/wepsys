-- =============================================================================
-- SUPERINTENDENCIA DE BANCOS DE LA REPUBLICA DOMINICANA (SB)
-- Plataforma de Gestion de Solicitudes Internas y Notificaciones
-- Datos de Referencia para SQL Server (DML)
-- =============================================================================
-- El API siembra estos mismos datos al arrancar sobre una base vacia
-- (SemillaDatos: 4 usuarios, 6 areas, 5 tipos y 10 solicitudes). Este archivo es
-- el equivalente manual y reproducible para revisar el modelo sin levantar el API.
--
-- Requisitos: aplicar primero `database/schema.sql`.
--   sqlcmd -S localhost,1433 -U sa -P "<clave>" -C -i database/schema.sql
--   sqlcmd -S localhost,1433 -U sa -P "<clave>" -C -i database/seed.sql
--
-- Los scripts son idempotentes por tabla: si la tabla ya tiene filas, se omite.
-- =============================================================================
SET NOCOUNT ON;
GO

-- -----------------------------------------------------------------------------
-- Areas
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Areas)
BEGIN
    SET IDENTITY_INSERT dbo.Areas ON;

    INSERT INTO dbo.Areas (Id, Nombre, Activa) VALUES
    (1, N'Tecnología de la Información', 1),
    (2, N'Canales de Atención', 1),
    (3, N'Recursos Humanos', 1),
    (4, N'Operaciones', 1),
    (5, N'Riesgos', 1),
    (6, N'Finanzas', 1);

    SET IDENTITY_INSERT dbo.Areas OFF;
END;
GO

-- -----------------------------------------------------------------------------
-- TiposSolicitud
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.TiposSolicitud)
BEGIN
    SET IDENTITY_INSERT dbo.TiposSolicitud ON;

    INSERT INTO dbo.TiposSolicitud (Id, Nombre, Descripcion, Activo) VALUES
    (1, N'Incidente', N'Falla que afecta la operación.', 1),
    (2, N'Requerimiento', N'Solicitud de un servicio o dato.', 1),
    (3, N'Acceso', N'Alta, baja o cambio de permisos.', 1),
    (4, N'Hardware', N'Equipos, periféricos y consumibles.', 1),
    (5, N'Mejora', N'Automatización o desarrollo nuevo.', 1);

    SET IDENTITY_INSERT dbo.TiposSolicitud OFF;
END;
GO

-- -----------------------------------------------------------------------------
-- Usuarios
-- Rol: 1 = Administrador, 2 = Analista, 3 = Solicitante
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios)
BEGIN
    SET IDENTITY_INSERT dbo.Usuarios ON;

    INSERT INTO dbo.Usuarios (Id, Nombre, Correo, HashContrasena, Rol, Activo, FechaCreacion) VALUES
    (1, N'Ana Martínez', N'admin@sb.gob.do', N'120000.wKnHDjaQZ5ALWsXeLqDAug==.+wgKYTgkRtlDYCqLHLlMHpXn8rNEfmRfATrGwGssVqw=', 1, 1, '2026-02-01T08:00:00'),
    (2, N'Luis Peña', N'analista@sb.gob.do', N'120000.E8DQVfR+3iDFJiTxuxUdWA==.NwBygD7KhCN/ww7K2A7Fj/zEswSJ6gRzbgDtIlZsklU=', 2, 1, '2026-03-01T08:00:00'),
    (3, N'Carla Jiménez', N'analista2@sb.gob.do', N'120000.E8DQVfR+3iDFJiTxuxUdWA==.NwBygD7KhCN/ww7K2A7Fj/zEswSJ6gRzbgDtIlZsklU=', 2, 1, '2026-05-01T08:00:00'),
    (4, N'Pedro Ramírez', N'solicitante@sb.gob.do', N'120000.TVyREf2K7y68qbgakNd8kw==.1EGIONGlfJGmx/xmUe7NJ48mybCarwT7d7Jj8mXZ/x0=', 3, 1, '2026-04-01T08:00:00');

    SET IDENTITY_INSERT dbo.Usuarios OFF;
END;
GO

-- -----------------------------------------------------------------------------
-- Solicitudes
-- Prioridad: 1 = Critica, 2 = Alta, 3 = Media, 4 = Baja
-- Estado: 1 = Registrada, 2 = EnAnalisis, 3 = EnProgreso,
--         4 = EnEsperaDelSolicitante, 5 = Resuelta, 6 = Cerrada
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Solicitudes)
BEGIN
    SET IDENTITY_INSERT dbo.Solicitudes ON;

    INSERT INTO dbo.Solicitudes (Id, Codigo, Titulo, Descripcion, Prioridad, Estado, FechaCreacion, FechaCompromiso, FechaCierre, AreaId, TipoSolicitudId, SolicitanteId, ResponsableId, UrlEvidencia, Secuencia) VALUES
    (1, N'SOL-2026-0001', N'No puedo acceder al sistema de reportes', N'Al iniciar sesión el sistema devuelve un error 500 y no permite continuar.', 2, 2, '2026-10-21T09:00:00', '2026-10-25T18:00:00', NULL, 1, 1, 4, 2, N'https://evidencia.sb.gob.do/error-500.png', 1),
    (2, N'SOL-2026-0002', N'Solicitud de acceso a la base de datos de clientes', N'Se requiere acceso de solo lectura para el análisis mensual de cartera.', 3, 3, '2026-10-20T10:30:00', '2026-10-27T18:00:00', NULL, 5, 3, 4, 3, NULL, 2),
    (3, N'SOL-2026-0003', N'Reemplazo de equipo de escritorio', N'El equipo asignado presenta fallas de encendido recurrentes y ya cumplió su vida útil.', 4, 1, '2026-10-19T14:15:00', '2026-10-21T18:00:00', NULL, 3, 4, 4, NULL, NULL, 3),
    (4, N'SOL-2026-0004', N'Error al generar el reporte regulatorio mensual', N'El reporte se genera incompleto: faltan las últimas dos columnas de conciliación.', 1, 4, '2026-10-16T11:00:00', '2026-10-25T18:00:00', NULL, 6, 1, 4, 2, N'https://evidencia.sb.gob.do/reporte-fallido.pdf', 4),
    (5, N'SOL-2026-0005', N'Cambio de contraseña del usuario de servicio', N'La contraseña expiró y bloquea la integración con el proveedor externo.', 2, 6, '2026-10-07T08:00:00', '2026-10-10T18:00:00', '2026-10-08T16:00:00', 1, 3, 4, 2, NULL, 5);

    SET IDENTITY_INSERT dbo.Solicitudes OFF;
END;
GO

-- -----------------------------------------------------------------------------
-- HistorialesEstado
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.HistorialesEstado)
BEGIN
    SET IDENTITY_INSERT dbo.HistorialesEstado ON;

    INSERT INTO dbo.HistorialesEstado (Id, SolicitudId, EstadoAnterior, EstadoNuevo, Fecha, UsuarioId, Comentario) VALUES
    (1, 1, NULL, 1, '2026-10-21T09:00:00', 4, N'Creación de la solicitud inicial.'),
    (2, 1, 1, 2, '2026-10-21T13:00:00', 2, N'Pasa a análisis técnico del equipo de canales.'),
    (3, 2, NULL, 1, '2026-10-20T10:30:00', 4, N'Registro de solicitud de permisos.'),
    (4, 2, 1, 2, '2026-10-20T14:00:00', 3, N'Evaluando matriz de accesos y aprobaciones.'),
    (5, 2, 2, 3, '2026-10-21T09:00:00', 3, N'Configuración de permisos en ambiente de base de datos.'),
    (6, 5, NULL, 1, '2026-10-07T08:00:00', 4, N'Registro de incidente de contraseña.'),
    (7, 5, 1, 2, '2026-10-07T10:00:00', 2, N'Revisión con el oficial de seguridad.'),
    (8, 5, 2, 5, '2026-10-08T14:00:00', 2, N'Se restablecieron credenciales y se validó enlace.'),
    (9, 5, 5, 6, '2026-10-08T16:00:00', 2, N'Se validó la solución con el solicitante y el proveedor.');

    SET IDENTITY_INSERT dbo.HistorialesEstado OFF;
END;
GO

-- -----------------------------------------------------------------------------
-- Comentarios
-- Visibilidad: 1 = Interna, 2 = Publica
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Comentarios)
BEGIN
    SET IDENTITY_INSERT dbo.Comentarios ON;

    INSERT INTO dbo.Comentarios (Id, SolicitudId, Texto, Visibilidad, Fecha, UsuarioId) VALUES
    (1, 1, N'Se solicitó volcado de traza al equipo de infraestructura.', 1, '2026-10-21T14:00:00', 2),
    (2, 1, N'Estamos revisando el incidente con alta prioridad.', 2, '2026-10-21T15:00:00', 2),
    (3, 5, N'Credencial de integración regenerada exitosamente.', 2, '2026-10-08T15:30:00', 2);

    SET IDENTITY_INSERT dbo.Comentarios OFF;
END;
GO

-- -----------------------------------------------------------------------------
-- Notificaciones
-- Canal: 1 = BaseDatos, 2 = Correo, 3 = Consola
-- Estado: 1 = Pendiente, 2 = Enviada, 3 = Fallida
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Notificaciones)
BEGIN
    SET IDENTITY_INSERT dbo.Notificaciones ON;

    INSERT INTO dbo.Notificaciones (Id, SolicitudId, DestinatarioId, Canal, Asunto, Mensaje, Estado, Fecha, Leida) VALUES
    (1, 1, 4, 1, N'Solicitud SOL-2026-0001 registrada', N'Su solicitud ha sido registrada satisfactoriamente.', 2, '2026-10-21T09:00:00', 1),
    (2, 1, 2, 1, N'Nueva solicitud asignada: SOL-2026-0001', N'Se le ha asignado la solicitud No puedo acceder al sistema de reportes.', 2, '2026-10-21T13:00:00', 0),
    (3, 5, 4, 1, N'Solicitud SOL-2026-0005 cerrada', N'Su solicitud ha sido cerrada con resolución satisfactoria.', 2, '2026-10-08T16:00:00', 1);

    SET IDENTITY_INSERT dbo.Notificaciones OFF;
END;
GO
