using Microsoft.AspNetCore.Mvc;

namespace SB.GestionSolicitudes.Servicios.Errores;

/// <summary>
/// MVC result that hands the problem document to the registered
/// <see cref="IProblemDetailsService"/> instead of the action's output formatters.
/// That is what guarantees the <c>application/problem+json</c> media type even when the
/// controller declares <c>[Produces("application/json")]</c> for its successful payloads.
/// </summary>
public sealed class ResultadoProblemDetails : IActionResult
{
    private readonly ProblemDetails _problema;

    public ResultadoProblemDetails(ProblemDetails problema)
    {
        _problema = problema;
    }

    public Task ExecuteResultAsync(ActionContext contexto) =>
        ProblemaApi.EscribirAsync(contexto.HttpContext, _problema);
}
