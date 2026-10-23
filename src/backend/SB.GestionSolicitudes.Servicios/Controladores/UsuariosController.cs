using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SB.GestionSolicitudes.Aplicacion.Dtos;
using SB.GestionSolicitudes.Aplicacion.Servicios;
using SB.GestionSolicitudes.Servicios.Errores;
using SB.GestionSolicitudes.Servicios.Middleware;

namespace SB.GestionSolicitudes.Servicios.Controladores;

/// <summary>
/// Administración de usuarios y consulta de responsables técnicos.
/// </summary>
[ApiController]
[Route("api/usuarios")]
[Produces("application/json")]
[Authorize]
public sealed class UsuariosController : ControllerBase
{
    private readonly UsuarioServicio _usuarios;

    public UsuariosController(UsuarioServicio usuarios)
    {
        _usuarios = usuarios;
    }

    /// <summary>
    /// Lista todos los usuarios. Requiere rol Administrador.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpGet]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(IReadOnlyList<UsuarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObtenerTodos(CancellationToken cancellationToken)
    {
        var resultado = await _usuarios.ObtenerTodosAsync(cancellationToken);
        return resultado.ComoRespuestaHttp(this);
    }

    /// <summary>
    /// Lista los usuarios técnicos activos disponibles para asignación.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpGet("responsables")]
    [Authorize(Roles = "Administrador,Analista")]
    [ProducesResponseType(typeof(IReadOnlyList<UsuarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObtenerResponsables(CancellationToken cancellationToken)
    {
        var resultado = await _usuarios.ObtenerResponsablesDisponiblesAsync(cancellationToken);
        return resultado.ComoRespuestaHttp(this);
    }

    /// <summary>
    /// Crea un usuario con rol Administrador, Analista o Solicitante.
    /// </summary>
    /// <param name="peticion">Datos del usuario y contraseña inicial.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(
        [FromBody] CrearUsuarioRequest peticion,
        CancellationToken cancellationToken)
    {
        var resultado = await _usuarios.CrearAsync(peticion, cancellationToken);

        return resultado.Exito
            ? StatusCode(StatusCodes.Status201Created, resultado.Valor)
            : resultado.ComoRespuestaHttp(this);
    }

    /// <summary>
    /// Actualiza parcialmente un usuario (nombre, rol, estado o contraseña).
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="peticion">Campos a modificar.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(
        int id,
        [FromBody] ActualizarUsuarioRequest peticion,
        CancellationToken cancellationToken)
    {
        var resultado = await _usuarios.ActualizarAsync(id, peticion, cancellationToken);
        return resultado.ComoRespuestaHttp(this);
    }
}
