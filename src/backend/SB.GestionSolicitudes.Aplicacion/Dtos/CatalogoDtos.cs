using System.ComponentModel.DataAnnotations;
using SB.GestionSolicitudes.Dominio.Catalogos;

namespace SB.GestionSolicitudes.Aplicacion.Dtos;

public sealed record AreaDto(int Id, string Nombre, bool Activa);

public sealed record TipoSolicitudDto(int Id, string Nombre, string Descripcion, bool Activo);

public sealed class CrearAreaRequest
{
    [Required(ErrorMessage = "El nombre del área es obligatorio.")]
    [StringLength(Area.LONGITUD_MAXIMA_NOMBRE, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 120 caracteres.")]
    public string Nombre { get; init; } = string.Empty;
}

public sealed class ActualizarAreaRequest
{
    [Required(ErrorMessage = "El nombre del área es obligatorio.")]
    [StringLength(Area.LONGITUD_MAXIMA_NOMBRE, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 120 caracteres.")]
    public string Nombre { get; init; } = string.Empty;

    public bool? Activa { get; init; }
}

public sealed class CrearTipoSolicitudRequest
{
    [Required(ErrorMessage = "El nombre del tipo es obligatorio.")]
    [StringLength(TipoSolicitud.LONGITUD_MAXIMA_NOMBRE, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 120 caracteres.")]
    public string Nombre { get; init; } = string.Empty;

    [StringLength(TipoSolicitud.LONGITUD_MAXIMA_DESCRIPCION, ErrorMessage = "La descripción no puede superar 300 caracteres.")]
    public string Descripcion { get; init; } = string.Empty;
}

public sealed class ActualizarTipoSolicitudRequest
{
    [Required(ErrorMessage = "El nombre del tipo es obligatorio.")]
    [StringLength(TipoSolicitud.LONGITUD_MAXIMA_NOMBRE, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 120 caracteres.")]
    public string Nombre { get; init; } = string.Empty;

    [StringLength(TipoSolicitud.LONGITUD_MAXIMA_DESCRIPCION, ErrorMessage = "La descripción no puede superar 300 caracteres.")]
    public string? Descripcion { get; init; }

    public bool? Activo { get; init; }
}
