-- =============================================================================
-- SUPERINTENDENCIA DE BANCOS DE LA REPUBLICA DOMINICANA (SB)
-- Plataforma de Gestion de Solicitudes Internas y Notificaciones
-- Esquema de Base de Datos Relacional para SQL Server (DDL)
-- =============================================================================
-- GENERADO: `dotnet ef migrations script --idempotent` a partir de las migraciones
-- de SB.GestionSolicitudes.Infraestructura. No editar a mano: todo cambio de modelo
-- entra por una migracion de EF Core y este archivo se regenera con:
--   dotnet ef migrations script --idempotent --project src/backend/SB.GestionSolicitudes.Infraestructura --startup-project src/backend/SB.GestionSolicitudes.Servicios --output database/schema.sql
-- El API aplica las migraciones al arrancar (SemillaDatos), por lo que este script
-- es el equivalente manual para una base vacia.
-- =============================================================================

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE TABLE [Areas] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(120) NOT NULL,
        [Activa] bit NOT NULL,
        CONSTRAINT [PK_Areas] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE TABLE [TiposSolicitud] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(120) NOT NULL,
        [Descripcion] nvarchar(300) NOT NULL,
        [Activo] bit NOT NULL,
        CONSTRAINT [PK_TiposSolicitud] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE TABLE [Usuarios] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(150) NOT NULL,
        [Correo] nvarchar(200) NOT NULL,
        [HashContrasena] nvarchar(500) NOT NULL,
        [Rol] int NOT NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE TABLE [Solicitudes] (
        [Id] int NOT NULL IDENTITY,
        [Codigo] nvarchar(30) NOT NULL,
        [Titulo] nvarchar(200) NOT NULL,
        [Descripcion] nvarchar(4000) NOT NULL,
        [Prioridad] int NOT NULL,
        [Estado] int NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        [FechaCompromiso] datetime2 NOT NULL,
        [FechaCierre] datetime2 NULL,
        [AreaId] int NOT NULL,
        [TipoSolicitudId] int NOT NULL,
        [SolicitanteId] int NOT NULL,
        [ResponsableId] int NULL,
        [UrlEvidencia] nvarchar(500) NULL,
        [Secuencia] int NOT NULL,
        CONSTRAINT [PK_Solicitudes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Solicitudes_Areas_AreaId] FOREIGN KEY ([AreaId]) REFERENCES [Areas] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Solicitudes_TiposSolicitud_TipoSolicitudId] FOREIGN KEY ([TipoSolicitudId]) REFERENCES [TiposSolicitud] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Solicitudes_Usuarios_ResponsableId] FOREIGN KEY ([ResponsableId]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Solicitudes_Usuarios_SolicitanteId] FOREIGN KEY ([SolicitanteId]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE TABLE [Comentarios] (
        [Id] int NOT NULL IDENTITY,
        [SolicitudId] int NOT NULL,
        [Texto] nvarchar(2000) NOT NULL,
        [Visibilidad] int NOT NULL,
        [Fecha] datetime2 NOT NULL,
        [UsuarioId] int NOT NULL,
        CONSTRAINT [PK_Comentarios] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Comentarios_Solicitudes_SolicitudId] FOREIGN KEY ([SolicitudId]) REFERENCES [Solicitudes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Comentarios_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE TABLE [HistorialesEstado] (
        [Id] int NOT NULL IDENTITY,
        [SolicitudId] int NOT NULL,
        [EstadoAnterior] int NULL,
        [EstadoNuevo] int NOT NULL,
        [Fecha] datetime2 NOT NULL,
        [UsuarioId] int NOT NULL,
        [Comentario] nvarchar(2000) NULL,
        CONSTRAINT [PK_HistorialesEstado] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HistorialesEstado_Solicitudes_SolicitudId] FOREIGN KEY ([SolicitudId]) REFERENCES [Solicitudes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_HistorialesEstado_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE TABLE [Notificaciones] (
        [Id] int NOT NULL IDENTITY,
        [SolicitudId] int NOT NULL,
        [DestinatarioId] int NOT NULL,
        [Canal] int NOT NULL,
        [Asunto] nvarchar(200) NOT NULL,
        [Mensaje] nvarchar(1000) NOT NULL,
        [Estado] int NOT NULL,
        [Fecha] datetime2 NOT NULL,
        [Leida] bit NOT NULL,
        CONSTRAINT [PK_Notificaciones] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notificaciones_Solicitudes_SolicitudId] FOREIGN KEY ([SolicitudId]) REFERENCES [Solicitudes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Notificaciones_Usuarios_DestinatarioId] FOREIGN KEY ([DestinatarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Areas_Nombre] ON [Areas] ([Nombre]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE INDEX [IX_Comentarios_SolicitudId_Fecha] ON [Comentarios] ([SolicitudId], [Fecha]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE INDEX [IX_Comentarios_UsuarioId] ON [Comentarios] ([UsuarioId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE INDEX [IX_HistorialesEstado_SolicitudId_Fecha] ON [HistorialesEstado] ([SolicitudId], [Fecha]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE INDEX [IX_HistorialesEstado_UsuarioId] ON [HistorialesEstado] ([UsuarioId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE INDEX [IX_Notificaciones_DestinatarioId_Leida] ON [Notificaciones] ([DestinatarioId], [Leida]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE INDEX [IX_Notificaciones_SolicitudId] ON [Notificaciones] ([SolicitudId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE INDEX [IX_Solicitudes_AreaId] ON [Solicitudes] ([AreaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Solicitudes_Codigo] ON [Solicitudes] ([Codigo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE INDEX [IX_Solicitudes_Estado] ON [Solicitudes] ([Estado]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE INDEX [IX_Solicitudes_FechaCompromiso] ON [Solicitudes] ([FechaCompromiso]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE INDEX [IX_Solicitudes_ResponsableId] ON [Solicitudes] ([ResponsableId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE INDEX [IX_Solicitudes_SolicitanteId] ON [Solicitudes] ([SolicitanteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE INDEX [IX_Solicitudes_TipoSolicitudId] ON [Solicitudes] ([TipoSolicitudId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TiposSolicitud_Nombre] ON [TiposSolicitud] ([Nombre]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Usuarios_Correo] ON [Usuarios] ([Correo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023020513_Inicial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261023020513_Inicial', N'8.0.10');
END;
GO

COMMIT;
GO
