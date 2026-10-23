using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace SB.GestionSolicitudes.Servicios.Errores;

/// <summary>
/// Builds and writes the error responses of the API with the framework
/// <see cref="ProblemDetails"/> contract (RFC 9457, successor of RFC 7807), so every
/// failure carries the standard members plus the two extension members this API
/// guarantees: <c>codigo</c> (stable, machine-readable business code) and
/// <c>trazabilidad</c> (correlation id that matches the logs and the <c>X-Trace-Id</c>
/// header).
/// </summary>
public static class ProblemaApi
{
    /// <summary>Media type defined by RFC 9457 for problem responses.</summary>
    public const string TIPO_CONTENIDO = "application/problem+json";

    /// <summary>Extension member with the stable business code.</summary>
    public const string CLAVE_CODIGO = "codigo";

    /// <summary>Extension member with the correlation id.</summary>
    public const string CLAVE_TRAZABILIDAD = "trazabilidad";

    /// <summary>Response header that repeats the correlation id outside the body.</summary>
    public const string CABECERA_TRAZABILIDAD = "X-Trace-Id";

    /// <summary>
    /// Creates a problem document for a known failure. The <paramref name="titulo"/>
    /// summarizes the status and the <paramref name="detalle"/> explains it to the
    /// caller without exposing internals.
    /// </summary>
    public static ProblemDetails Crear(
        int estado,
        string codigo,
        string titulo,
        string detalle,
        HttpContext contexto)
    {
        var problema = new ProblemDetails
        {
            Type = UriTipo(estado),
            Title = titulo,
            Status = estado,
            Detail = detalle,
            Instance = contexto.Request.Path
        };

        problema.Extensions[CLAVE_CODIGO] = codigo;

        return problema;
    }

    /// <summary>
    /// Creates the <see cref="ValidationProblemDetails"/> used when one or more input
    /// fields fail validation. The per-field messages travel in the standard
    /// <c>errors</c> member.
    /// </summary>
    public static ValidationProblemDetails CrearValidacion(
        IDictionary<string, string[]> errores,
        HttpContext contexto)
    {
        var problema = new ValidationProblemDetails(errores)
        {
            Type = UriTipo(StatusCodes.Status400BadRequest),
            Title = "Solicitud inválida",
            Status = StatusCodes.Status400BadRequest,
            Detail = "Uno o más campos no cumplen las validaciones.",
            Instance = contexto.Request.Path
        };

        problema.Extensions[CLAVE_CODIGO] = "VALIDACION";

        return problema;
    }

    /// <summary>
    /// Wraps a problem document in an MVC result that writes it with the
    /// <c>application/problem+json</c> media type.
    /// </summary>
    public static IActionResult ComoResultado(ProblemDetails problema) => new ResultadoProblemDetails(problema);

    /// <summary>
    /// Single writer for every problem response. It serializes the runtime type, so a
    /// <see cref="ValidationProblemDetails"/> keeps its <c>errors</c> member, and it sets
    /// the media type directly instead of leaving it to content negotiation.
    /// </summary>
    public static async Task EscribirAsync(HttpContext contexto, ProblemDetails problema)
    {
        Completar(problema, contexto);

        contexto.Response.StatusCode = problema.Status ?? StatusCodes.Status500InternalServerError;

        // Serializing as `object` is what preserves the derived members of the problem
        // type; the registered `ProblemDetails` writer would drop `errors`.
        await contexto.Response.WriteAsJsonAsync<object>(
            problema,
            OpcionesJson(contexto),
            TIPO_CONTENIDO);
    }

    /// <summary>
    /// Default business code, Spanish title and generic explanation for a status the API
    /// did not raise from a use case (challenge, unknown route, unsupported media type,
    /// and so on).
    /// </summary>
    public static (string Codigo, string Titulo, string Detalle) DescribirEstado(int estado) => estado switch
    {
        StatusCodes.Status400BadRequest =>
            ("VALIDACION", "Solicitud inválida", "La solicitud no cumple el formato esperado."),
        StatusCodes.Status401Unauthorized =>
            ("NO_AUTENTICADO", "No autenticado", "Se requiere un token válido para ejecutar esta operación."),
        StatusCodes.Status403Forbidden =>
            ("PROHIBIDO", "Acceso denegado", "El usuario autenticado no tiene permisos para esta operación."),
        StatusCodes.Status404NotFound =>
            ("NO_ENCONTRADO", "Recurso no encontrado", "El recurso solicitado no existe o no está disponible."),
        StatusCodes.Status405MethodNotAllowed =>
            ("METODO_NO_PERMITIDO", "Método no permitido", "El método HTTP no está permitido para esta ruta."),
        StatusCodes.Status409Conflict =>
            ("CONFLICTO", "Conflicto con el estado actual", "La operación entra en conflicto con el estado actual del recurso."),
        StatusCodes.Status415UnsupportedMediaType =>
            ("FORMATO_NO_SOPORTADO", "Formato de contenido no soportado", "El Content-Type de la petición no está soportado."),
        StatusCodes.Status429TooManyRequests =>
            ("DEMASIADAS_SOLICITUDES", "Demasiadas solicitudes", "Se excedió el límite de solicitudes permitido."),
        >= StatusCodes.Status500InternalServerError =>
            ("ERROR_INTERNO", "Error interno del servidor",
                "Ocurrió un error inesperado. Comparta el identificador de trazabilidad con soporte."),
        _ => ("ERROR_HTTP", "No fue posible procesar la solicitud", "La solicitud no pudo completarse.")
    };

    /// <summary>URI that identifies the problem type. It is derived from the status.</summary>
    public static string UriTipo(int estado) => $"https://httpstatuses.io/{estado}";

    /// <summary>
    /// Completes a problem document written by the framework with the correlation id
    /// and, when the origin did not provide one, the default business code.
    /// </summary>
    public static void CompletarConTrazabilidad(ProblemDetailsContext contexto) =>
        Completar(contexto.ProblemDetails, contexto.HttpContext);

    private static void Completar(ProblemDetails problema, HttpContext contexto)
    {
        var trazabilidad = contexto.TraceIdentifier;

        problema.Extensions.TryAdd(CLAVE_TRAZABILIDAD, trazabilidad);
        problema.Extensions.TryAdd(
            CLAVE_CODIGO,
            DescribirEstado(problema.Status ?? StatusCodes.Status500InternalServerError).Codigo);

        contexto.Response.Headers[CABECERA_TRAZABILIDAD] = trazabilidad;
    }

    private static JsonSerializerOptions OpcionesJson(HttpContext contexto) =>
        contexto.RequestServices
            .GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()
            .Value
            .SerializerOptions;
}
