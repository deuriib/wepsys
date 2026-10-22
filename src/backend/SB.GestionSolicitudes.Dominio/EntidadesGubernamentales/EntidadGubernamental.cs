namespace SB.GestionSolicitudes.Dominio.EntidadesGubernamentales;

/// <summary>
/// Government entity of the Dominican Republic. This is the catalog required by
/// the first specification, persisted in a plain-text file inside the project
/// (see <c>database/entidades-gubernamentales.json</c>) instead of the
/// relational store used by the request workflow.
/// </summary>
public class EntidadGubernamental
{
    public const int LONGITUD_MAXIMA_NOMBRE = 200;
    public const int LONGITUD_MAXIMA_CATEGORIA = 120;
    public const int LONGITUD_MAXIMA_PODER = 80;
    public const int LONGITUD_MAXIMA_SECTOR = 120;

    public Guid Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public string PoderDelEstado { get; set; } = string.Empty;

    public string Sector { get; set; } = string.Empty;

    public bool Activa { get; set; } = true;

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaActualizacion { get; set; }
}
