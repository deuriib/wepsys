namespace SB.GestionSolicitudes.Aplicacion.Dtos;

/// <summary>
/// One bucket of a dashboard metric. <see cref="Clave"/> holds the enum name so
/// the UI can map it to a label and a color deterministically.
/// </summary>
public sealed record ConteoSolicitudes(string Clave, int Total);

/// <summary>
/// Raw aggregate produced by the repository (one round trip instead of six).
/// </summary>
public sealed record ResumenSolicitudes(
    int Total,
    int Abiertas,
    int Cerradas,
    int Vencidas,
    IReadOnlyList<ConteoSolicitudes> PorEstado,
    IReadOnlyList<ConteoSolicitudes> PorPrioridad);

/// <summary>
/// Payload of <c>GET /api/dashboard/resumen</c>.
/// </summary>
public sealed record TableroResumenDto(
    int Total,
    int Abiertas,
    int Cerradas,
    int Vencidas,
    IReadOnlyList<ConteoSolicitudes> PorEstado,
    IReadOnlyList<ConteoSolicitudes> PorPrioridad,
    IReadOnlyList<SolicitudResumenDto> UltimasSolicitudes);
