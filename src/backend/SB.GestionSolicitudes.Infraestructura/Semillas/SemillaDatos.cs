using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Dominio.Catalogos;
using SB.GestionSolicitudes.Dominio.Solicitudes;
using SB.GestionSolicitudes.Dominio.Usuarios;
using SB.GestionSolicitudes.Infraestructura.Persistencia;

namespace SB.GestionSolicitudes.Infraestructura.Semillas;

/// <summary>
/// Deterministic demo data so a reviewer can explore the application (and the
/// dashboard) without creating everything by hand. It runs only when the
/// database is empty, so it never overwrites real work.
/// </summary>
public static class SemillaDatos
{
    // Documented demo credentials. They are not secrets: the exercise explicitly
    // asks for reproducible test users.
    public const string CORREO_ADMINISTRADOR = "admin@sb.gob.do";
    public const string CONTRASENA_ADMINISTRADOR = "Admin123!";
    public const string CORREO_ANALISTA = "analista@sb.gob.do";
    public const string CONTRASENA_ANALISTA = "Analista123!";
    public const string CORREO_ANALISTA_DOS = "analista2@sb.gob.do";
    public const string CORREO_SOLICITANTE = "solicitante@sb.gob.do";
    public const string CONTRASENA_SOLICITANTE = "Solicitud123!";

    public static async Task InicializarAsync(IServiceProvider proveedorServicios, CancellationToken cancellationToken = default)
    {
        using var alcance = proveedorServicios.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ContextoBaseDatos>();

        // The schema is versioned with EF Core migrations, so every environment
        // (local, tests, staging) advances the database the same way.
        await contexto.Database.MigrateAsync(cancellationToken);

        // The seed spans several tables; a transaction keeps it all-or-nothing so a
        // failure cannot leave a half-populated database behind. SQL Server retries
        // transient faults, and a retry cannot re-run a user-initiated transaction
        // unless it is wrapped by the configured execution strategy.
        var estrategia = contexto.Database.CreateExecutionStrategy();

        await estrategia.ExecuteAsync(async () =>
        {
            // A failed attempt leaves the added entities tracked, so a retry would
            // insert them twice. Clearing and re-checking makes the seed idempotent
            // from the point of view of the strategy.
            contexto.ChangeTracker.Clear();

            if (await contexto.Usuarios.AnyAsync(cancellationToken))
            {
                return;
            }

            await using var transaccion = await contexto.Database.BeginTransactionAsync(cancellationToken);

            var hash = alcance.ServiceProvider.GetRequiredService<IServicioHashContrasena>();
            var reloj = alcance.ServiceProvider.GetRequiredService<IReloj>();
            var ahora = reloj.Ahora;

            var administrador = CrearUsuario("Ana Martínez", CORREO_ADMINISTRADOR, CONTRASENA_ADMINISTRADOR, RolUsuario.Administrador, hash, ahora.AddMonths(-8));
            var analista = CrearUsuario("Luis Peña", CORREO_ANALISTA, CONTRASENA_ANALISTA, RolUsuario.Analista, hash, ahora.AddMonths(-7));
            var analistaDos = CrearUsuario("Carla Jiménez", CORREO_ANALISTA_DOS, CONTRASENA_ANALISTA, RolUsuario.Analista, hash, ahora.AddMonths(-5));
            var solicitante = CrearUsuario("Pedro Ramírez", CORREO_SOLICITANTE, CONTRASENA_SOLICITANTE, RolUsuario.Solicitante, hash, ahora.AddMonths(-6));

            contexto.Usuarios.AddRange(administrador, analista, analistaDos, solicitante);

            var areas = new[]
            {
                new Area { Nombre = "Tecnología de la Información", Activa = true },
                new Area { Nombre = "Canales de Atención", Activa = true },
                new Area { Nombre = "Recursos Humanos", Activa = true },
                new Area { Nombre = "Operaciones", Activa = true },
                new Area { Nombre = "Riesgos", Activa = true },
                new Area { Nombre = "Finanzas", Activa = true }
            };

            var tipos = new[]
            {
                new TipoSolicitud { Nombre = "Incidente", Descripcion = "Falla que afecta la operación.", Activo = true },
                new TipoSolicitud { Nombre = "Requerimiento", Descripcion = "Solicitud de un servicio o dato.", Activo = true },
                new TipoSolicitud { Nombre = "Acceso", Descripcion = "Alta, baja o cambio de permisos.", Activo = true },
                new TipoSolicitud { Nombre = "Hardware", Descripcion = "Equipos, periféricos y consumibles.", Activo = true },
                new TipoSolicitud { Nombre = "Mejora", Descripcion = "Automatización o desarrollo nuevo.", Activo = true }
            };

            contexto.Areas.AddRange(areas);
            contexto.TiposSolicitud.AddRange(tipos);
            await contexto.SaveChangesAsync(cancellationToken);

            var solicitudes = ConstruirSolicitudes(areas, tipos, solicitante, analista, analistaDos, ahora);
            contexto.Solicitudes.AddRange(solicitudes);
            await contexto.SaveChangesAsync(cancellationToken);

            await transaccion.CommitAsync(cancellationToken);
        });
    }

    private static Usuario CrearUsuario(
        string nombre,
        string correo,
        string contrasena,
        RolUsuario rol,
        IServicioHashContrasena hash,
        DateTime fechaCreacion) =>
        new()
        {
            Nombre = nombre,
            Correo = correo,
            HashContrasena = hash.Hashear(contrasena),
            Rol = rol,
            Activo = true,
            FechaCreacion = fechaCreacion
        };

    private static List<Solicitud> ConstruirSolicitudes(
        Area[] areas,
        TipoSolicitud[] tipos,
        Usuario solicitante,
        Usuario analista,
        Usuario analistaDos,
        DateTime ahora)
    {
        var solicitudes = new List<Solicitud>();

        var definiciones = new[]
        {
            (Titulo: "No puedo acceder al sistema de reportes", Descripcion: "Al iniciar sesión el sistema devuelve un error 500 y no permite continuar.", Prioridad: PrioridadSolicitud.Alta, Area: 0, Tipo: 0, DiasCreada: 1, DiasCompromiso: 4, Estados: new[] { EstadoSolicitud.EnAnalisis }, Responsable: analista.Id),
            (Titulo: "Solicitud de acceso a la base de datos de clientes", Descripcion: "Se requiere acceso de solo lectura para el análisis mensual de cartera.", Prioridad: PrioridadSolicitud.Media, Area: 4, Tipo: 2, DiasCreada: 2, DiasCompromiso: 7, Estados: new[] { EstadoSolicitud.EnAnalisis, EstadoSolicitud.EnProgreso }, Responsable: analistaDos.Id),
            (Titulo: "Reemplazo de equipo de escritorio", Descripcion: "El equipo asignado presenta fallas de encendido recurrentes y ya cumplió su vida útil.", Prioridad: PrioridadSolicitud.Baja, Area: 2, Tipo: 3, DiasCreada: 3, DiasCompromiso: 2, Estados: Array.Empty<EstadoSolicitud>(), Responsable: 0),
            (Titulo: "Error al generar el reporte regulatorio mensual", Descripcion: "El reporte se genera incompleto: faltan las últimas dos columnas de conciliación.", Prioridad: PrioridadSolicitud.Critica, Area: 5, Tipo: 0, DiasCreada: 6, DiasCompromiso: 9, Estados: new[] { EstadoSolicitud.EnAnalisis, EstadoSolicitud.EnProgreso, EstadoSolicitud.EnEsperaDelSolicitante }, Responsable: analista.Id),
            (Titulo: "Automatizar la conciliación diaria de canales", Descripcion: "Se requiere un proceso automático que consolide los archivos de los canales cada mañana.", Prioridad: PrioridadSolicitud.Alta, Area: 1, Tipo: 4, DiasCreada: 12, DiasCompromiso: 30, Estados: new[] { EstadoSolicitud.EnAnalisis, EstadoSolicitud.EnProgreso, EstadoSolicitud.Resuelta }, Responsable: analistaDos.Id),
            (Titulo: "Cambio de contraseña del usuario de servicio", Descripcion: "La contraseña expiró y bloquea la integración con el proveedor externo.", Prioridad: PrioridadSolicitud.Alta, Area: 0, Tipo: 2, DiasCreada: 15, DiasCompromiso: 18, Estados: new[] { EstadoSolicitud.EnAnalisis, EstadoSolicitud.Resuelta, EstadoSolicitud.Cerrada }, Responsable: analista.Id),
            (Titulo: "Alta de usuario para nueva analista de riesgos", Descripcion: "Se incorpora una analista al equipo y requiere los accesos estándar del área.", Prioridad: PrioridadSolicitud.Media, Area: 4, Tipo: 2, DiasCreada: 18, DiasCompromiso: 22, Estados: new[] { EstadoSolicitud.EnAnalisis, EstadoSolicitud.Resuelta, EstadoSolicitud.Cerrada }, Responsable: analistaDos.Id),
            (Titulo: "Lentitud en el portal de consultas internas", Descripcion: "Las consultas tardan más de treinta segundos desde la última actualización.", Prioridad: PrioridadSolicitud.Critica, Area: 1, Tipo: 0, DiasCreada: 20, DiasCompromiso: 18, Estados: new[] { EstadoSolicitud.EnAnalisis }, Responsable: analista.Id),
            (Titulo: "Solicitud de monitor adicional", Descripcion: "El puesto de trabajo requiere un segundo monitor para revisión de documentos.", Prioridad: PrioridadSolicitud.Baja, Area: 3, Tipo: 3, DiasCreada: 25, DiasCompromiso: 40, Estados: Array.Empty<EstadoSolicitud>(), Responsable: 0),
            (Titulo: "Ajuste en el flujo de aprobación de gastos", Descripcion: "El flujo actual exige una aprobación que ya no aplica según la nueva política.", Prioridad: PrioridadSolicitud.Media, Area: 5, Tipo: 4, DiasCreada: 30, DiasCompromiso: 45, Estados: new[] { EstadoSolicitud.EnAnalisis, EstadoSolicitud.EnProgreso, EstadoSolicitud.Resuelta }, Responsable: analistaDos.Id)
        };

        var secuencia = 1;

        foreach (var definicion in definiciones)
        {
            var fechaCreacion = ahora.AddDays(-definicion.DiasCreada);
            var fechaCompromiso = ahora.AddDays(definicion.DiasCompromiso - definicion.DiasCreada);
            var codigo = Solicitud.ConstruirCodigo(fechaCreacion.Year, secuencia);

            var solicitud = Solicitud.Registrar(
                codigo,
                definicion.Titulo,
                definicion.Descripcion,
                definicion.Prioridad,
                areas[definicion.Area].Id,
                tipos[definicion.Tipo].Id,
                solicitante.Id,
                fechaCompromiso,
                fechaCreacion,
                null,
                secuencia);

            var fechaTransicion = fechaCreacion.AddHours(4);

            if (definicion.Responsable > 0)
            {
                solicitud.AsignarResponsable(definicion.Responsable, analista.Id, fechaTransicion);
            }

            foreach (var estado in definicion.Estados)
            {
                fechaTransicion = fechaTransicion.AddHours(6);
                solicitud.CambiarEstado(
                    estado,
                    definicion.Responsable > 0 ? definicion.Responsable : analista.Id,
                    RolUsuario.Analista,
                    estado == EstadoSolicitud.Cerrada ? "Se validó la solución con el solicitante." : null,
                    fechaTransicion);
            }

            solicitud.AgregarComentario(
                "Se recibió la solicitud y se está revisando el detalle reportado.",
                VisibilidadComentario.Publica,
                definicion.Responsable > 0 ? definicion.Responsable : analista.Id,
                fechaTransicion.AddHours(1));

            solicitudes.Add(solicitud);
            secuencia++;
        }

        return solicitudes;
    }
}
