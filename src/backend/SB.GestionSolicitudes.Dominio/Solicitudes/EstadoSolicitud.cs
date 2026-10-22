namespace SB.GestionSolicitudes.Dominio.Solicitudes;

/// <summary>
/// Lifecycle states of a request. Values are explicit to keep the persisted
/// representation stable across refactors.
/// </summary>
public enum EstadoSolicitud
{
    Registrada = 1,
    EnAnalisis = 2,
    EnProgreso = 3,
    EnEsperaDelSolicitante = 4,
    Resuelta = 5,
    Cerrada = 6
}
