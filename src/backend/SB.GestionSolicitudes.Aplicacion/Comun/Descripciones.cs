using SB.GestionSolicitudes.Dominio.Solicitudes;
using SB.GestionSolicitudes.Dominio.Usuarios;

namespace SB.GestionSolicitudes.Aplicacion.Comun;

/// <summary>
/// Human-readable labels for the enum values. Serving them from the API keeps a
/// single source of truth instead of duplicating translations in the frontend.
/// </summary>
public static class Descripciones
{
    public static string DeEstado(EstadoSolicitud estado) => estado switch
    {
        EstadoSolicitud.Registrada => "Registrada",
        EstadoSolicitud.EnAnalisis => "En análisis",
        EstadoSolicitud.EnProgreso => "En progreso",
        EstadoSolicitud.EnEsperaDelSolicitante => "En espera del solicitante",
        EstadoSolicitud.Resuelta => "Resuelta",
        EstadoSolicitud.Cerrada => "Cerrada",
        _ => estado.ToString()
    };

    public static string DePrioridad(PrioridadSolicitud prioridad) => prioridad switch
    {
        PrioridadSolicitud.Baja => "Baja",
        PrioridadSolicitud.Media => "Media",
        PrioridadSolicitud.Alta => "Alta",
        PrioridadSolicitud.Critica => "Crítica",
        _ => prioridad.ToString()
    };

    public static string DeRol(RolUsuario rol) => rol switch
    {
        RolUsuario.Administrador => "Administrador",
        RolUsuario.Analista => "Analista",
        RolUsuario.Solicitante => "Solicitante",
        _ => rol.ToString()
    };
}
