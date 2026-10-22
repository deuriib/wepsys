namespace SB.GestionSolicitudes.Infraestructura.Configuracion;

/// <summary>
/// JWT settings bound from configuration. The secret is never committed: the
/// repository only contains a clearly marked development placeholder that is
/// overridden by an environment variable in any real environment.
/// </summary>
public sealed class OpcionesJwt
{
    public const string SECCION = "Jwt";
    public const int LONGITUD_MINIMA_CLAVE = 32;

    public string Emisor { get; set; } = "SB.GestionSolicitudes";

    public string Audiencia { get; set; } = "SB.GestionSolicitudes.Cliente";

    public string ClaveSecreta { get; set; } = string.Empty;

    public int MinutosVigencia { get; set; } = 120;
}
