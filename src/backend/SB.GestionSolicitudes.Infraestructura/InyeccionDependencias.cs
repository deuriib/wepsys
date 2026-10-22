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
            opciones.UseSqlite(ResolverCadenaConexion(ObtenerCadenaConexion(configuracionActual), directorioContenido));
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
    /// missing, instead of letting SQLite create a file in the current directory.
    /// </summary>
    private static string ObtenerCadenaConexion(IConfiguration configuracion) =>
        configuracion.GetConnectionString(NOMBRE_CADENA_CONEXION)
        ?? throw new InvalidOperationException(
            $"Falta la cadena de conexión '{NOMBRE_CADENA_CONEXION}' en la configuración (appsettings o variables de entorno).");

    /// <summary>
    /// Makes a relative SQLite path absolute (and creates its folder), so the
    /// database always lands inside the project's <c>database/</c> directory no
    /// matter which working directory launched the process.
    /// </summary>
    private static string ResolverCadenaConexion(string cadenaConexion, string directorioContenido)
    {
        const string PREFIJO = "Data Source=";

        if (!cadenaConexion.StartsWith(PREFIJO, StringComparison.OrdinalIgnoreCase))
        {
            return cadenaConexion;
        }

        var ruta = cadenaConexion[PREFIJO.Length..].Trim();

        if (Path.IsPathRooted(ruta) || ruta.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return cadenaConexion;
        }

        var rutaAbsoluta = ResolutorRutas.ResolverRutaEscritura(ruta, directorioContenido);
        var directorio = Path.GetDirectoryName(rutaAbsoluta);

        if (!string.IsNullOrWhiteSpace(directorio))
        {
            Directory.CreateDirectory(directorio);
        }

        return $"{PREFIJO}{rutaAbsoluta}";
    }
}
