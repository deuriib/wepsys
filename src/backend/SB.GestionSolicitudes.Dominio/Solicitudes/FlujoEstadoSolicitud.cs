namespace SB.GestionSolicitudes.Dominio.Solicitudes;

/// <summary>
/// Single source of truth for the workflow. Declaring the map once keeps the
/// UI, the API and the tests aligned instead of scattering conditionals.
/// </summary>
public static class FlujoEstadoSolicitud
{
    private static readonly IReadOnlyDictionary<EstadoSolicitud, EstadoSolicitud[]> TRANSICIONES_PERMITIDAS =
        new Dictionary<EstadoSolicitud, EstadoSolicitud[]>
        {
            [EstadoSolicitud.Registrada] = new[]
            {
                EstadoSolicitud.EnAnalisis,
                EstadoSolicitud.EnProgreso,
                EstadoSolicitud.EnEsperaDelSolicitante
            },
            [EstadoSolicitud.EnAnalisis] = new[]
            {
                EstadoSolicitud.EnProgreso,
                EstadoSolicitud.EnEsperaDelSolicitante,
                EstadoSolicitud.Resuelta
            },
            [EstadoSolicitud.EnProgreso] = new[]
            {
                EstadoSolicitud.EnEsperaDelSolicitante,
                EstadoSolicitud.Resuelta
            },
            [EstadoSolicitud.EnEsperaDelSolicitante] = new[]
            {
                EstadoSolicitud.EnAnalisis,
                EstadoSolicitud.EnProgreso,
                EstadoSolicitud.Resuelta
            },
            [EstadoSolicitud.Resuelta] = new[]
            {
                EstadoSolicitud.Cerrada,
                EstadoSolicitud.EnProgreso
            },
            [EstadoSolicitud.Cerrada] = new[]
            {
                EstadoSolicitud.EnAnalisis,
                EstadoSolicitud.EnProgreso
            }
        };

    public static bool EsTransicionPermitida(EstadoSolicitud estadoActual, EstadoSolicitud estadoNuevo) =>
        TRANSICIONES_PERMITIDAS.TryGetValue(estadoActual, out var destinos) &&
        destinos.Contains(estadoNuevo);

    public static IReadOnlyCollection<EstadoSolicitud> ObtenerDestinosPermitidos(EstadoSolicitud estadoActual) =>
        TRANSICIONES_PERMITIDAS.TryGetValue(estadoActual, out var destinos)
            ? destinos
            : Array.Empty<EstadoSolicitud>();
}
