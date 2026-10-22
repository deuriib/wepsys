using SB.GestionSolicitudes.Aplicacion.Servicios;

namespace SB.GestionSolicitudes.Servicios;

/// <summary>
/// Registers the use cases. Their lifetime is scoped because they depend on the
/// unit of work, which is per request.
/// </summary>
public static class InyeccionDependencias
{
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        servicios.AddScoped<AutenticacionServicio>();
        servicios.AddScoped<SolicitudServicio>();
        servicios.AddScoped<TableroServicio>();
        servicios.AddScoped<CatalogoServicio>();
        servicios.AddScoped<UsuarioServicio>();
        servicios.AddScoped<EntidadGubernamentalServicio>();
        servicios.AddScoped<NotificacionServicio>();

        return servicios;
    }
}
