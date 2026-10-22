namespace SB.GestionSolicitudes.Dominio.Catalogos;

/// <summary>
/// Organizational area that raises a request. Administrative catalog with an
/// initial seed so the application is useful on first run.
/// </summary>
public class Area
{
    public const int LONGITUD_MAXIMA_NOMBRE = 120;

    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public bool Activa { get; set; } = true;
}
