using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SB.GestionSolicitudes.Aplicacion.Dtos;
using SB.GestionSolicitudes.Aplicacion.Servicios;
using SB.GestionSolicitudes.Servicios.Errores;
using SB.GestionSolicitudes.Servicios.Middleware;

namespace SB.GestionSolicitudes.Servicios.Controladores;

/// <summary>
/// Catálogos administrables: áreas y tipos de solicitud.
/// </summary>
[ApiController]
[Route("api/catalogos")]
[Produces("application/json")]
[Authorize]
public sealed class CatalogosController : ControllerBase
{
    private readonly CatalogoServicio _catalogos;

    public CatalogosController(CatalogoServicio catalogos)
    {
        _catalogos = catalogos;
    }

    /// <summary>
    /// Consulta las áreas (activas por defecto, o todas si se especifica soloActivas=false).
    /// </summary>
    /// <param name="soloActivas">Indica si solo se devuelven las áreas activas.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpGet("areas")]
    [ProducesResponseType(typeof(IReadOnlyList<AreaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerAreas(
        [FromQuery] bool soloActivas = true,
        CancellationToken cancellationToken = default)
    {
        var areas = await _catalogos.ObtenerAreasAsync(soloActivas, cancellationToken);
        return Ok(areas);
    }

    /// <summary>
    /// Consulta los tipos de solicitud (activos por defecto, o todos si se especifica soloActivos=false).
    /// </summary>
    /// <param name="soloActivos">Indica si solo se devuelven los tipos de solicitud activos.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpGet("tipos-solicitud")]
    [ProducesResponseType(typeof(IReadOnlyList<TipoSolicitudDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerTiposSolicitud(
        [FromQuery] bool soloActivos = true,
        CancellationToken cancellationToken = default)
    {
        var tipos = await _catalogos.ObtenerTiposSolicitudAsync(soloActivos, cancellationToken);
        return Ok(tipos);
    }

    /// <summary>
    /// Crea un área. Requiere rol Administrador.
    /// </summary>
    /// <param name="peticion">Nombre del área.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpPost("areas")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(AreaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CrearArea(
        [FromBody] CrearAreaRequest peticion,
        CancellationToken cancellationToken)
    {
        var resultado = await _catalogos.CrearAreaAsync(peticion, cancellationToken);

        return resultado.Exito
            ? StatusCode(StatusCodes.Status201Created, resultado.Valor)
            : resultado.ComoRespuestaHttp(this);
    }

    /// <summary>
    /// Actualiza un área existente. Requiere rol Administrador.
    /// </summary>
    /// <param name="id">Identificador del área.</param>
    /// <param name="peticion">Datos a actualizar.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpPut("areas/{id:int}")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(AreaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ActualizarArea(
        int id,
        [FromBody] ActualizarAreaRequest peticion,
        CancellationToken cancellationToken)
    {
        var resultado = await _catalogos.ActualizarAreaAsync(id, peticion, cancellationToken);
        return resultado.ComoRespuestaHttp(this);
    }

    /// <summary>
    /// Elimina un área si no tiene solicitudes asociadas. Requiere rol Administrador.
    /// </summary>
    /// <param name="id">Identificador del área.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpDelete("areas/{id:int}")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EliminarArea(
        int id,
        CancellationToken cancellationToken)
    {
        var resultado = await _catalogos.EliminarAreaAsync(id, cancellationToken);
        return resultado.ComoRespuestaHttp(this);
    }

    /// <summary>
    /// Crea un tipo de solicitud. Requiere rol Administrador.
    /// </summary>
    /// <param name="peticion">Nombre y descripción del tipo.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpPost("tipos-solicitud")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(TipoSolicitudDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CrearTipoSolicitud(
        [FromBody] CrearTipoSolicitudRequest peticion,
        CancellationToken cancellationToken)
    {
        var resultado = await _catalogos.CrearTipoSolicitudAsync(peticion, cancellationToken);

        return resultado.Exito
            ? StatusCode(StatusCodes.Status201Created, resultado.Valor)
            : resultado.ComoRespuestaHttp(this);
    }

    /// <summary>
    /// Actualiza un tipo de solicitud existente. Requiere rol Administrador.
    /// </summary>
    /// <param name="id">Identificador del tipo de solicitud.</param>
    /// <param name="peticion">Datos a actualizar.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpPut("tipos-solicitud/{id:int}")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(TipoSolicitudDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ActualizarTipoSolicitud(
        int id,
        [FromBody] ActualizarTipoSolicitudRequest peticion,
        CancellationToken cancellationToken)
    {
        var resultado = await _catalogos.ActualizarTipoSolicitudAsync(id, peticion, cancellationToken);
        return resultado.ComoRespuestaHttp(this);
    }

    /// <summary>
    /// Elimina un tipo de solicitud si no tiene solicitudes asociadas. Requiere rol Administrador.
    /// </summary>
    /// <param name="id">Identificador del tipo de solicitud.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpDelete("tipos-solicitud/{id:int}")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EliminarTipoSolicitud(
        int id,
        CancellationToken cancellationToken)
    {
        var resultado = await _catalogos.EliminarTipoSolicitudAsync(id, cancellationToken);
        return resultado.ComoRespuestaHttp(this);
    }
}
