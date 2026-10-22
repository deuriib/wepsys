using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SB.GestionSolicitudes.Aplicacion.Dtos;
using SB.GestionSolicitudes.Aplicacion.Servicios;
using SB.GestionSolicitudes.Servicios.Errores;
using SB.GestionSolicitudes.Servicios.Middleware;

namespace SB.GestionSolicitudes.Servicios.Controladores;

/// <summary>
/// Bandeja de notificaciones del usuario autenticado.
/// </summary>
[ApiController]
[Route("api/notificaciones")]
[Authorize]
[Produces("application/json")]
public sealed class NotificacionesController : ControllerBase
{
    private readonly NotificacionServicio _notificaciones;

    public NotificacionesController(NotificacionServicio notificaciones)
    {
        _notificaciones = notificaciones;
    }

    /// <summary>
    /// Lista las notificaciones del usuario autenticado.
    /// </summary>
    /// <param name="soloNoLeidas">Cuando es verdadero devuelve únicamente las no leídas.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificacionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerMias(
        [FromQuery] bool soloNoLeidas,
        CancellationToken cancellationToken)
    {
        var notificaciones = await _notificaciones.ObtenerMisNotificacionesAsync(soloNoLeidas, cancellationToken);
        return Ok(notificaciones);
    }

    /// <summary>
    /// Marca una notificación como leída.
    /// </summary>
    /// <param name="id">Identificador de la notificación.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpPatch("{id:int}/leida")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(RespuestaError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarcarComoLeida(int id, CancellationToken cancellationToken)
    {
        var resultado = await _notificaciones.MarcarComoLeidaAsync(id, cancellationToken);
        return resultado.ComoRespuestaHttp(this);
    }
}
