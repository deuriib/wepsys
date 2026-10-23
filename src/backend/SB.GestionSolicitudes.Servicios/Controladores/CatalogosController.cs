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
    /// Consulta las áreas activas.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpGet("areas")]
    [ProducesResponseType(typeof(IReadOnlyList<AreaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerAreas(CancellationToken cancellationToken)
    {
        var areas = await _catalogos.ObtenerAreasAsync(cancellationToken);
        return Ok(areas);
    }

    /// <summary>
    /// Consulta los tipos de solicitud activos.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpGet("tipos-solicitud")]
    [ProducesResponseType(typeof(IReadOnlyList<TipoSolicitudDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerTiposSolicitud(CancellationToken cancellationToken)
    {
        var tipos = await _catalogos.ObtenerTiposSolicitudAsync(cancellationToken);
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
    /// Crea un tipo de solicitud. Requiere rol Administrador.
    /// </summary>
    /// <param name="peticion">Nombre y descripción del tipo.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpPost("tipos-solicitud")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(TipoSolicitudDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CrearTipoSolicitud(
        [FromBody] CrearTipoSolicitudRequest peticion,
        CancellationToken cancellationToken)
    {
        var resultado = await _catalogos.CrearTipoSolicitudAsync(peticion, cancellationToken);

        return resultado.Exito
            ? StatusCode(StatusCodes.Status201Created, resultado.Valor)
            : resultado.ComoRespuestaHttp(this);
    }
}
