-- =============================================================================
-- SUPERINTENDENCIA DE BANCOS DE LA REPÚBLICA DOMINICANA (SB)
-- Plataforma de Gestión de Solicitudes Internas y Notificaciones
-- Datos Semilla de Ejemplo (DML)
-- =============================================================================
-- Contraseñas hasheadas con BCrypt:
-- admin@sb.gob.do      => Admin123!
-- analista@sb.gob.do   => Analista123!
-- analista2@sb.gob.do  => Analista123!
-- solicitante@sb.gob.do => Solicitud123!
-- =============================================================================

-- Áreas
INSERT INTO Areas (Id, Nombre, Activa) VALUES
(1, 'Tecnología de la Información', 1),
(2, 'Canales de Atención', 1),
(3, 'Recursos Humanos', 1),
(4, 'Operaciones', 1),
(5, 'Riesgos', 1),
(6, 'Finanzas', 1);

-- Tipos de Solicitud
INSERT INTO TiposSolicitud (Id, Nombre, Descripcion, Activo) VALUES
(1, 'Incidente', 'Falla que afecta la operación.', 1),
(2, 'Requerimiento', 'Solicitud de un servicio o dato.', 1),
(3, 'Acceso', 'Alta, baja o cambio de permisos.', 1),
(4, 'Hardware', 'Equipos, periféricos y consumibles.', 1),
(5, 'Mejora', 'Automatización o desarrollo nuevo.', 1);

-- Usuarios
-- BCrypt cost 11 hash matching BCrypt.Net-Next
INSERT INTO Usuarios (Id, Nombre, Correo, HashContrasena, Rol, Activo, FechaCreacion) VALUES
(1, 'Ana Martínez', 'admin@sb.gob.do', '$2a$11$9eZc2LwM7y1Hn1WzHkWxI.sSgJ7Z9k9oP9/EwNlB0oKzV2R3mKjCW', 1, 1, '2026-02-01T08:00:00Z'),
(2, 'Luis Peña', 'analista@sb.gob.do', '$2a$11$9eZc2LwM7y1Hn1WzHkWxI.sSgJ7Z9k9oP9/EwNlB0oKzV2R3mKjCW', 2, 1, '2026-03-01T08:00:00Z'),
(3, 'Carla Jiménez', 'analista2@sb.gob.do', '$2a$11$9eZc2LwM7y1Hn1WzHkWxI.sSgJ7Z9k9oP9/EwNlB0oKzV2R3mKjCW', 2, 1, '2026-05-01T08:00:00Z'),
(4, 'Pedro Ramírez', 'solicitante@sb.gob.do', '$2a$11$9eZc2LwM7y1Hn1WzHkWxI.sSgJ7Z9k9oP9/EwNlB0oKzV2R3mKjCW', 3, 1, '2026-04-01T08:00:00Z');

-- Solicitudes de Ejemplo
INSERT INTO Solicitudes (Id, Codigo, Titulo, Descripcion, Prioridad, Estado, FechaCreacion, FechaCompromiso, FechaCierre, AreaId, TipoSolicitudId, SolicitanteId, ResponsableId, UrlEvidencia, Secuencia) VALUES
(1, 'SOL-2026-0001', 'No puedo acceder al sistema de reportes', 'Al iniciar sesión el sistema devuelve un error 500 y no permite continuar.', 2, 2, '2026-10-21T09:00:00Z', '2026-10-25T18:00:00Z', NULL, 1, 1, 4, 2, 'https://evidencia.sb.gob.do/error-500.png', 1),
(2, 'SOL-2026-0002', 'Solicitud de acceso a la base de datos de clientes', 'Se requiere acceso de solo lectura para el análisis mensual de cartera.', 3, 3, '2026-10-20T10:30:00Z', '2026-10-27T18:00:00Z', NULL, 5, 3, 4, 3, NULL, 2),
(3, 'SOL-2026-0003', 'Reemplazo de equipo de escritorio', 'El equipo asignado presenta fallas de encendido recurrentes y ya cumplió su vida útil.', 4, 1, '2026-10-19T14:15:00Z', '2026-10-21T18:00:00Z', NULL, 3, 4, 4, NULL, NULL, 3),
(4, 'SOL-2026-0004', 'Error al generar el reporte regulatorio mensual', 'El reporte se genera incompleto: faltan las últimas dos columnas de conciliación.', 1, 4, '2026-10-16T11:00:00Z', '2026-10-25T18:00:00Z', NULL, 6, 1, 4, 2, 'https://evidencia.sb.gob.do/reporte-fallido.pdf', 4),
(5, 'SOL-2026-0005', 'Cambio de contraseña del usuario de servicio', 'La contraseña expiró y bloquea la integración con el proveedor externo.', 2, 6, '2026-10-07T08:00:00Z', '2026-10-10T18:00:00Z', '2026-10-08T16:00:00Z', 1, 3, 4, 2, NULL, 5);

-- Historiales de Estado
INSERT INTO HistorialesEstado (Id, SolicitudId, EstadoAnterior, EstadoNuevo, Fecha, UsuarioId, Comentario) VALUES
(1, 1, NULL, 1, '2026-10-21T09:00:00Z', 4, 'Creación de la solicitud inicial.'),
(2, 1, 1, 2, '2026-10-21T13:00:00Z', 2, 'Pasa a análisis técnico del equipo de canales.'),
(3, 2, NULL, 1, '2026-10-20T10:30:00Z', 4, 'Registro de solicitud de permisos.'),
(4, 2, 1, 2, '2026-10-20T14:00:00Z', 3, 'Evaluando matriz de accesos y aprobaciones.'),
(5, 2, 2, 3, '2026-10-21T09:00:00Z', 3, 'Configuración de permisos en ambiente de base de datos.'),
(6, 5, NULL, 1, '2026-10-07T08:00:00Z', 4, 'Registro de incidente de contraseña.'),
(7, 5, 1, 2, '2026-10-07T10:00:00Z', 2, 'Revisión con el oficial de seguridad.'),
(8, 5, 2, 5, '2026-10-08T14:00:00Z', 2, 'Se restablecieron credenciales y se validó enlace.'),
(9, 5, 5, 6, '2026-10-08T16:00:00Z', 2, 'Se validó la solución con el solicitante y el proveedor.');

-- Comentarios
INSERT INTO Comentarios (Id, SolicitudId, Texto, Visibilidad, Fecha, UsuarioId) VALUES
(1, 1, 'Se solicitó volcado de traza al equipo de infraestructura.', 2, '2026-10-21T14:00:00Z', 2),
(2, 1, 'Estamos revisando el incidente con alta prioridad.', 1, '2026-10-21T15:00:00Z', 2),
(3, 5, 'Credencial de integración regenerada exitosamente.', 1, '2026-10-08T15:30:00Z', 2);

-- Notificaciones
INSERT INTO Notificaciones (Id, SolicitudId, DestinatarioId, Canal, Asunto, Mensaje, Estado, Fecha, Leida) VALUES
(1, 1, 4, 1, 'Solicitud SOL-2026-0001 registrada', 'Su solicitud ha sido registrada satisfactoriamente.', 2, '2026-10-21T09:00:00Z', 1),
(2, 1, 2, 1, 'Nueva solicitud asignada: SOL-2026-0001', 'Se le ha asignado la solicitud No puedo acceder al sistema de reportes.', 2, '2026-10-21T13:00:00Z', 0),
(3, 5, 4, 1, 'Solicitud SOL-2026-0005 cerrada', 'Su solicitud ha sido cerrada con resolución satisfactoria.', 2, '2026-10-08T16:00:00Z', 1);
