using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SB.GestionSolicitudes.Aplicacion.Dtos;
using SB.GestionSolicitudes.Aplicacion.Servicios;
using SB.GestionSolicitudes.Servicios.Errores;
using SB.GestionSolicitudes.Servicios.Middleware;

namespace SB.GestionSolicitudes.Servicios.Controladores;

/// <summary>
/// Autenticación mediante JWT.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AutenticacionController : ControllerBase
{
    private readonly AutenticacionServicio _autenticacion;

    public AutenticacionController(AutenticacionServicio autenticacion)
    {
        _autenticacion = autenticacion;
    }

    /// <summary>
    /// Valida las credenciales y emite un token JWT con el rol del usuario.
    /// </summary>
    /// <param name="peticion">Correo y contraseña del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RespuestaAutenticacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> IniciarSesion(
        [FromBody] LoginRequest peticion,
        CancellationToken cancellationToken)
    {
        var resultado = await _autenticacion.IniciarSesionAsync(peticion, cancellationToken);
        return resultado.ComoRespuestaHttp(this);
    }
}
