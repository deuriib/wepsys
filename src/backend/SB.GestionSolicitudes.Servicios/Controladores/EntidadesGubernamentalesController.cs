using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SB.GestionSolicitudes.Aplicacion.Comun;
using SB.GestionSolicitudes.Aplicacion.Dtos;
using SB.GestionSolicitudes.Aplicacion.Servicios;
using SB.GestionSolicitudes.Servicios.Errores;
using SB.GestionSolicitudes.Servicios.Middleware;

namespace SB.GestionSolicitudes.Servicios.Controladores;

/// <summary>
/// Mantenimiento del listado de entidades gubernamentales de la República
/// Dominicana. La información se persiste en un archivo de texto plano dentro del
/// proyecto, tal como exige la especificación técnica de la API.
/// </summary>
[ApiController]
[Route("api/entidades-gubernamentales")]
[Authorize]
[Produces("application/json")]
public sealed class EntidadesGubernamentalesController : ControllerBase
{
    private readonly EntidadGubernamentalServicio _entidades;

    public EntidadesGubernamentalesController(EntidadGubernamentalServicio entidades)
    {
        _entidades = entidades;
    }

    /// <summary>
    /// Consulta paginada del catálogo con filtros por texto, categoría, poder del
    /// estado, sector y estado activo.
    /// </summary>
    /// <param name="filtro">Filtros del catálogo.</param>
    /// <param name="paginacion">Número de página y tamaño de página (máximo 100).</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<EntidadGubernamentalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Consultar(
        [FromQuery] FiltroEntidadGubernamental filtro,
        [FromQuery] ParametrosPaginacion paginacion,
        CancellationToken cancellationToken)
    {
        var pagina = await _entidades.ConsultarAsync(filtro, paginacion, cancellationToken);
        return Ok(pagina);
    }

    /// <summary>
    /// Devuelve las opciones disponibles para los filtros (categorías, poderes del
    /// estado y sectores) sin cargar todo el catálogo.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpGet("opciones-filtro")]
    [ProducesResponseType(typeof(OpcionesFiltroEntidadesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerOpcionesFiltro(CancellationToken cancellationToken)
    {
        var opciones = await _entidades.ObtenerOpcionesFiltroAsync(cancellationToken);
        return Ok(opciones);
    }

    /// <summary>
    /// Obtiene una entidad gubernamental por su identificador.
    /// </summary>
    /// <param name="id">Identificador de la entidad.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EntidadGubernamentalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _entidades.ObtenerAsync(id, cancellationToken);
        return resultado.ComoRespuestaHttp(this);
    }

    /// <summary>
    /// Registra una nueva entidad gubernamental. Requiere rol Administrador.
    /// </summary>
    /// <param name="peticion">Datos de la entidad.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(EntidadGubernamentalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespuestaError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(
        [FromBody] CrearEntidadGubernamentalRequest peticion,
        CancellationToken cancellationToken)
    {
        var resultado = await _entidades.CrearAsync(peticion, cancellationToken);

        return resultado.Exito
            ? StatusCode(StatusCodes.Status201Created, resultado.Valor)
            : resultado.ComoRespuestaHttp(this);
    }

    /// <summary>
    /// Actualiza parcialmente una entidad gubernamental. Requiere rol Administrador.
    /// </summary>
    /// <param name="id">Identificador de la entidad.</param>
    /// <param name="peticion">Campos a modificar.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(EntidadGubernamentalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespuestaError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(
        Guid id,
        [FromBody] ActualizarEntidadGubernamentalRequest peticion,
        CancellationToken cancellationToken)
    {
        var resultado = await _entidades.ActualizarAsync(id, peticion, cancellationToken);
        return resultado.ComoRespuestaHttp(this);
    }

    /// <summary>
    /// Elimina una entidad gubernamental del catálogo. Requiere rol Administrador.
    /// </summary>
    /// <param name="id">Identificador de la entidad.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(RespuestaError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _entidades.EliminarAsync(id, cancellationToken);
        return resultado.ComoRespuestaHttp(this);
    }
}
