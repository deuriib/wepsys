namespace SB.GestionSolicitudes.Servicios.Errores;

/// <summary>
/// Error contract of the API. It follows RFC 7807 (Problem Details) and adds a
/// stable <see cref="Codigo"/> plus the <see cref="Trazabilidad"/> id so a client
/// can report a failure without exposing internal details.
/// </summary>
public sealed class RespuestaError
{
    /// <summary>URI que identifica el tipo de problema.</summary>
    public string? Type { get; init; }

    /// <summary>Título corto y legible del error.</summary>
    public string? Title { get; init; }

    /// <summary>Código HTTP de la respuesta.</summary>
    public int Status { get; init; }

    /// <summary>Explicación del error para el consumidor de la API.</summary>
    public string? Detail { get; init; }

    /// <summary>Ruta que originó el error.</summary>
    public string? Instance { get; init; }

    /// <summary>Código estable de negocio (por ejemplo CREDENCIALES_INVALIDAS).</summary>
    public string? Codigo { get; init; }

    /// <summary>Identificador de correlación para localizar el evento en los logs.</summary>
    public string? Trazabilidad { get; init; }

    /// <summary>Errores de validación por campo, cuando aplica.</summary>
    public IDictionary<string, string[]>? Errores { get; init; }
}
