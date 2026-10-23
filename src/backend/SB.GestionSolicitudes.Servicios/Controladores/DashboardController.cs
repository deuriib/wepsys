using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SB.GestionSolicitudes.Aplicacion.Dtos;
using SB.GestionSolicitudes.Aplicacion.Servicios;
using SB.GestionSolicitudes.Servicios.Errores;
using SB.GestionSolicitudes.Servicios.Middleware;

namespace SB.GestionSolicitudes.Servicios.Controladores;

/// <summary>
/// Indicadores de gestión del tablero.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly TableroServicio _tablero;

    public DashboardController(TableroServicio tablero)
    {
        _tablero = tablero;
    }

    /// <summary>
    /// Devuelve las métricas básicas: totales, abiertas, cerradas, vencidas,
    /// distribución por estado y prioridad, y las últimas solicitudes registradas.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpGet("resumen")]
    [ProducesResponseType(typeof(TableroResumenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerResumen(CancellationToken cancellationToken)
    {
        var resultado = await _tablero.ObtenerResumenAsync(cancellationToken);
        return resultado.ComoRespuestaHttp(this);
    }
}
