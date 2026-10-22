using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Dominio.Solicitudes;
using SB.GestionSolicitudes.Dominio.Usuarios;

namespace SB.GestionSolicitudes.Aplicacion.Comun;

/// <summary>
/// Centralizes the "who can see what" rules. Enforcing them here (and in the
/// query) means a client cannot bypass them by calling the API directly.
/// </summary>
public static class AutorizacionSolicitud
{
    public static AlcanceSolicitud ResolverAlcance(RolUsuario rol) => rol switch
    {
        RolUsuario.Administrador => AlcanceSolicitud.Todos,
        RolUsuario.Analista => AlcanceSolicitud.AsignadosODisponibles,
        _ => AlcanceSolicitud.Propios
    };

    public static bool PuedeVer(IUsuarioActual usuario, Solicitud solicitud) => usuario.Rol switch
    {
        RolUsuario.Administrador => true,
        RolUsuario.Analista => solicitud.ResponsableId is null || solicitud.ResponsableId == usuario.Id,
        RolUsuario.Solicitante => solicitud.SolicitanteId == usuario.Id,
        _ => false
    };

    public static bool PuedeGestionar(IUsuarioActual usuario, Solicitud solicitud) => usuario.Rol switch
    {
        RolUsuario.Administrador => true,
        RolUsuario.Analista => solicitud.ResponsableId is null || solicitud.ResponsableId == usuario.Id,
        _ => false
    };

    public static bool PuedeAsignar(IUsuarioActual usuario) =>
        usuario.Rol is RolUsuario.Administrador or RolUsuario.Analista;
}
