namespace SB.GestionSolicitudes.Dominio.Catalogos;

/// <summary>
/// Classifies a request (incident, hardware, access, development...).
/// New types can be added through the catalog without touching business logic.
/// </summary>
public class TipoSolicitud
{
    public const int LONGITUD_MAXIMA_NOMBRE = 120;
    public const int LONGITUD_MAXIMA_DESCRIPCION = 300;

    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;
}
