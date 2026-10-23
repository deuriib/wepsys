using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Infraestructura.Archivos;
using SB.GestionSolicitudes.Infraestructura.Configuracion;
using SB.GestionSolicitudes.Infraestructura.Notificaciones;
using SB.GestionSolicitudes.Infraestructura.Notificaciones.Canales;
using SB.GestionSolicitudes.Infraestructura.Persistencia;
using SB.GestionSolicitudes.Infraestructura.Persistencia.Repositorios;
using SB.GestionSolicitudes.Infraestructura.Seguridad;
using SB.GestionSolicitudes.Infraestructura.Tiempo;

namespace SB.GestionSolicitudes.Infraestructura;

/// <summary>
/// Composition root of the infrastructure layer. Everything the application
/// layer needs is registered here behind its contract.
/// </summary>
public static class InyeccionDependencias
{
    private const string NOMBRE_CADENA_CONEXION = "BaseDatos";

    // SQL Server may still be starting its recovery when the API boots (compose
    // health checks are not always available, for example in Kubernetes). These
    // values make the first query wait instead of crashing the process.
    private const int INTENTOS_REINTENTO = 6;
    private const int SEGUNDOS_ESPERA_MAXIMA_REINTENTO = 10;

    public static IServiceCollection AgregarInfraestructura(
        this IServiceCollection servicios,
        IConfiguration configuracion,
        string directorioContenido)
    {
        // Configuration is resolved when the provider is built, not when this method
        // runs. That keeps the door open for integration tests and hosting
        // environments to inject their own values through the host configuration.
        servicios.AddDbContext<ContextoBaseDatos>((proveedor, opciones) =>
        {
            var configuracionActual = proveedor.GetRequiredService<IConfiguration>();
            opciones.UseSqlServer(
                ObtenerCadenaConexion(configuracionActual),
                sql => sql.EnableRetryOnFailure(
                    INTENTOS_REINTENTO,
                    TimeSpan.FromSeconds(SEGUNDOS_ESPERA_MAXIMA_REINTENTO),
                    null));
        });

        servicios.AddOptions<OpcionesAlmacenEntidades>()
            .Configure<IConfiguration>((opciones, configuracionActual) =>
            {
                var seccion = configuracionActual.GetSection(OpcionesAlmacenEntidades.SECCION);
                opciones.RutaArchivo = ResolutorRutas.ResolverRutaEscritura(
                    seccion[nameof(OpcionesAlmacenEntidades.RutaArchivo)] ?? "entidades-gubernamentales.json",
                    directorioContenido);
                opciones.RutaSemilla = ResolutorRutas.ResolverRutaSemilla(
                    seccion[nameof(OpcionesAlmacenEntidades.RutaSemilla)] ?? "entidades-gubernamentales.seed.json",
                    directorioContenido);
            });

        servicios.Configure<OpcionesJwt>(configuracion.GetSection(OpcionesJwt.SECCION));

        servicios.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
        servicios.AddScoped<ISolicitudRepositorio, SolicitudRepositorio>();
        servicios.AddScoped<ICatalogoRepositorio, CatalogoRepositorio>();
        servicios.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        servicios.AddScoped<INotificacionRepositorio, NotificacionRepositorio>();

        servicios.AddSingleton<IReloj, RelojSistema>();
        servicios.AddSingleton<IServicioHashContrasena, ServicioHashContrasena>();
        servicios.AddScoped<IGeneradorTokenJwt, GeneradorTokenJwt>();

        servicios.AddSingleton<IEntidadGubernamentalRepositorio, RepositorioEntidadesGubernamentalesArchivo>();

        servicios.AddScoped<ICanalNotificacion, CanalBaseDatosNotificacion>();
        servicios.AddScoped<ICanalNotificacion, CanalCorreoSimuladoNotificacion>();
        servicios.AddScoped<ICanalNotificacion, CanalConsolaNotificacion>();
        servicios.AddScoped<INotificadorSolicitud, NotificadorSolicitud>();

        return servicios;
    }

    /// <summary>
    /// Reads the connection string and fails with an actionable message when it is
    /// missing, instead of letting the SQL client fall back to a local instance.
    /// </summary>
    private static string ObtenerCadenaConexion(IConfiguration configuracion) =>
        configuracion.GetConnectionString(NOMBRE_CADENA_CONEXION)
        ?? throw new InvalidOperationException(
            $"Falta la cadena de conexión '{NOMBRE_CADENA_CONEXION}' en la configuración (appsettings o variables de entorno).");
}
