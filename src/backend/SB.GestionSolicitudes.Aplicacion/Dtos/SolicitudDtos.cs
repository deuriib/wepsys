using System.ComponentModel.DataAnnotations;
using SB.GestionSolicitudes.Dominio.Solicitudes;

namespace SB.GestionSolicitudes.Aplicacion.Dtos;

/// <summary>
/// Row shape used by listings. It is intentionally flat: the list screen must
/// not pay the cost of loading comments and history.
/// </summary>
public sealed record SolicitudResumenDto(
    int Id,
    string Codigo,
    string Titulo,
    EstadoSolicitud Estado,
    string EstadoDescripcion,
    PrioridadSolicitud Prioridad,
    string PrioridadDescripcion,
    string Area,
    string TipoSolicitud,
    string Solicitante,
    string? Responsable,
    DateTime FechaCreacion,
    DateTime FechaCompromiso,
    bool Vencida);

/// <summary>
/// Full detail including the audit trail and the visible comments.
/// </summary>
public sealed record SolicitudDetalleDto(
    int Id,
    string Codigo,
    string Titulo,
    string Descripcion,
    EstadoSolicitud Estado,
    string EstadoDescripcion,
    PrioridadSolicitud Prioridad,
    string PrioridadDescripcion,
    int AreaId,
    string Area,
    int TipoSolicitudId,
    string TipoSolicitud,
    int SolicitanteId,
    string Solicitante,
    int? ResponsableId,
    string? Responsable,
    DateTime FechaCreacion,
    DateTime FechaCompromiso,
    DateTime? FechaCierre,
    string? UrlEvidencia,
    bool Vencida,
    IReadOnlyList<EstadoPermitidoDto> EstadosPermitidos,
    IReadOnlyList<HistorialEstadoDto> Historial,
    IReadOnlyList<ComentarioDto> Comentarios);

public sealed record EstadoPermitidoDto(EstadoSolicitud Estado, string Descripcion);

public sealed record HistorialEstadoDto(
    int Id,
    EstadoSolicitud? EstadoAnterior,
    string? EstadoAnteriorDescripcion,
    EstadoSolicitud EstadoNuevo,
    string EstadoNuevoDescripcion,
    DateTime Fecha,
    string Usuario,
    string? Comentario);

public sealed record ComentarioDto(
    int Id,
    string Texto,
    VisibilidadComentario Visibilidad,
    DateTime Fecha,
    string Usuario);

/// <summary>
/// Creation payload. Data annotations are the single source of truth for both
/// ASP.NET Core model validation and the generated OpenAPI schema.
/// </summary>
public sealed class CrearSolicitudRequest
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(Solicitud.LONGITUD_MAXIMA_TITULO, MinimumLength = 5, ErrorMessage = "El título debe tener entre 5 y 200 caracteres.")]
    public string Titulo { get; init; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(Solicitud.LONGITUD_MAXIMA_DESCRIPCION, MinimumLength = 10, ErrorMessage = "La descripción debe tener entre 10 y 4000 caracteres.")]
    public string Descripcion { get; init; } = string.Empty;

    [Required(ErrorMessage = "La prioridad es obligatoria.")]
    public PrioridadSolicitud? Prioridad { get; init; }

    [Required(ErrorMessage = "El área es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "El área es obligatoria.")]
    public int? AreaId { get; init; }

    [Required(ErrorMessage = "El tipo de solicitud es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El tipo de solicitud es obligatorio.")]
    public int? TipoSolicitudId { get; init; }

    [Required(ErrorMessage = "La fecha de compromiso es obligatoria.")]
    public DateTime? FechaCompromiso { get; init; }

    [StringLength(Solicitud.LONGITUD_MAXIMA_URL_EVIDENCIA, ErrorMessage = "La referencia de evidencia no puede superar 500 caracteres.")]
    public string? UrlEvidencia { get; init; }
}

/// <summary>
/// Partial update payload: every field is optional on purpose (PATCH semantics).
/// </summary>
public sealed class ActualizarSolicitudRequest
{
    [StringLength(Solicitud.LONGITUD_MAXIMA_TITULO, MinimumLength = 5, ErrorMessage = "El título debe tener entre 5 y 200 caracteres.")]
    public string? Titulo { get; init; }

    [StringLength(Solicitud.LONGITUD_MAXIMA_DESCRIPCION, MinimumLength = 10, ErrorMessage = "La descripción debe tener entre 10 y 4000 caracteres.")]
    public string? Descripcion { get; init; }

    public PrioridadSolicitud? Prioridad { get; init; }

    public DateTime? FechaCompromiso { get; init; }

    [StringLength(Solicitud.LONGITUD_MAXIMA_URL_EVIDENCIA, ErrorMessage = "La referencia de evidencia no puede superar 500 caracteres.")]
    public string? UrlEvidencia { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "El tipo de solicitud es obligatorio.")]
    public int? TipoSolicitudId { get; init; }
}

public sealed class CambiarEstadoRequest
{
    [Required(ErrorMessage = "El estado nuevo es obligatorio.")]
    public EstadoSolicitud? Estado { get; init; }

    [StringLength(Solicitud.LONGITUD_MAXIMA_COMENTARIO, ErrorMessage = "El comentario no puede superar 2000 caracteres.")]
    public string? Comentario { get; init; }
}

public sealed class AsignarResponsableRequest
{
    [Required(ErrorMessage = "El responsable es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El responsable es obligatorio.")]
    public int? ResponsableId { get; init; }
}

public sealed class CrearComentarioRequest
{
    [Required(ErrorMessage = "El comentario es obligatorio.")]
    [StringLength(Solicitud.LONGITUD_MAXIMA_COMENTARIO, MinimumLength = 1, ErrorMessage = "El comentario no puede superar 2000 caracteres.")]
    public string Texto { get; init; } = string.Empty;

    public VisibilidadComentario Visibilidad { get; init; } = VisibilidadComentario.Publica;
}

/// <summary>
/// Backend-side filters. They are applied in the query, so paging remains
/// correct regardless of how many rows the database holds.
/// </summary>
public sealed class FiltroSolicitudConsulta
{
    public EstadoSolicitud? Estado { get; init; }

    public PrioridadSolicitud? Prioridad { get; init; }

    public int? AreaId { get; init; }

    public int? SolicitanteId { get; init; }

    public int? ResponsableId { get; init; }

    public DateTime? FechaDesde { get; init; }

    public DateTime? FechaHasta { get; init; }

    public string? Texto { get; init; }

    public bool SoloVencidas { get; init; }
}
