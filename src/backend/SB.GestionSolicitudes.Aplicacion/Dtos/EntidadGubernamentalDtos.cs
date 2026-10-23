using System.ComponentModel.DataAnnotations;
using SB.GestionSolicitudes.Dominio.EntidadesGubernamentales;

namespace SB.GestionSolicitudes.Aplicacion.Dtos;

public sealed record EntidadGubernamentalDto(
    Guid Id,
    string Nombre,
    string Categoria,
    string PoderDelEstado,
    string Sector,
    bool Activa,
    DateTime FechaCreacion,
    DateTime FechaActualizacion);

public sealed class CrearEntidadGubernamentalRequest
{
    [Required(ErrorMessage = "El nombre de la entidad es obligatorio.")]
    [StringLength(EntidadGubernamental.LONGITUD_MAXIMA_NOMBRE, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 200 caracteres.")]
    public string Nombre { get; init; } = string.Empty;

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    [StringLength(EntidadGubernamental.LONGITUD_MAXIMA_CATEGORIA, MinimumLength = 3, ErrorMessage = "La categoría debe tener entre 3 y 120 caracteres.")]
    public string Categoria { get; init; } = string.Empty;

    [Required(ErrorMessage = "El poder del estado es obligatorio.")]
    [StringLength(EntidadGubernamental.LONGITUD_MAXIMA_PODER, MinimumLength = 3, ErrorMessage = "El poder del estado debe tener entre 3 y 80 caracteres.")]
    public string PoderDelEstado { get; init; } = string.Empty;

    [Required(ErrorMessage = "El sector es obligatorio.")]
    [StringLength(EntidadGubernamental.LONGITUD_MAXIMA_SECTOR, MinimumLength = 2, ErrorMessage = "El sector debe tener entre 2 y 120 caracteres.")]
    public string Sector { get; init; } = string.Empty;

    public bool Activa { get; init; } = true;
}

public sealed class ActualizarEntidadGubernamentalRequest
{
    [StringLength(EntidadGubernamental.LONGITUD_MAXIMA_NOMBRE, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 200 caracteres.")]
    public string? Nombre { get; init; }

    [StringLength(EntidadGubernamental.LONGITUD_MAXIMA_CATEGORIA, MinimumLength = 3, ErrorMessage = "La categoría debe tener entre 3 y 120 caracteres.")]
    public string? Categoria { get; init; }

    [StringLength(EntidadGubernamental.LONGITUD_MAXIMA_PODER, MinimumLength = 3, ErrorMessage = "El poder del estado debe tener entre 3 y 80 caracteres.")]
    public string? PoderDelEstado { get; init; }

    [StringLength(EntidadGubernamental.LONGITUD_MAXIMA_SECTOR, MinimumLength = 2, ErrorMessage = "El sector debe tener entre 2 y 120 caracteres.")]
    public string? Sector { get; init; }

    public bool? Activa { get; init; }
}

public sealed class FiltroEntidadGubernamental
{
    public string? Texto { get; init; }

    public string? Categoria { get; init; }

    public string? PoderDelEstado { get; init; }

    public string? Sector { get; init; }

    public bool? Activa { get; init; }
}

/// <summary>
/// Filter options exposed to the UI so it can build dropdowns without loading
/// the whole catalog.
/// </summary>
public sealed record OpcionesFiltroEntidadesDto(
    IReadOnlyList<string> Categorias,
    IReadOnlyList<string> PoderesDelEstado,
    IReadOnlyList<string> Sectores);

public sealed class CrearOpcionCatalogoRequest
{
    [Required(ErrorMessage = "El valor es obligatorio.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "El valor debe tener entre 2 y 120 caracteres.")]
    public string Valor { get; init; } = string.Empty;
}

public sealed class ActualizarOpcionCatalogoRequest
{
    [Required(ErrorMessage = "El valor actual es obligatorio.")]
    public string ValorActual { get; init; } = string.Empty;

    [Required(ErrorMessage = "El nuevo valor es obligatorio.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "El nuevo valor debe tener entre 2 y 120 caracteres.")]
    public string NuevoValor { get; init; } = string.Empty;
}
