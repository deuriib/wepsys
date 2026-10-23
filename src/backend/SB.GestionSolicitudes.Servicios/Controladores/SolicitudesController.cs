using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SB.GestionSolicitudes.Aplicacion.Comun;
using SB.GestionSolicitudes.Aplicacion.Dtos;
using SB.GestionSolicitudes.Aplicacion.Servicios;
using SB.GestionSolicitudes.Servicios.Errores;
using SB.GestionSolicitudes.Servicios.Middleware;

namespace SB.GestionSolicitudes.Servicios.Controladores;

/// <summary>
/// Ciclo de vida de las solicitudes internas: consulta, registro, edición,
/// cambios de estado, asignación y comentarios.
/// </summary>
[ApiController]
[Route("api/solicitudes")]
[Produces("application/json")]
[Authorize]
public sealed class SolicitudesController : ControllerBase
{
    private readonly SolicitudServicio _solicitudes;

    public SolicitudesController(SolicitudServicio solicitudes)
    {
        _solicitudes = solicitudes;
    }

    /// <summary>
    /// Consulta paginada de solicitudes con filtros aplicados en el backend.
    /// </summary>
    /// <remarks>
    /// El resultado se limita automáticamente según el rol: el administrador ve
    /// todo, el analista ve las asignadas o disponibles y el solicitante solo las
    /// propias.
    /// </remarks>
    /// <param name="filtro">Filtros por estado, prioridad, área, solicitante, responsable y fechas.</param>
    /// <param name="paginacion">Número de página y tamaño de página (máximo 100).</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<SolicitudResumenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Consultar(
        [FromQuery] FiltroSolicitudConsulta filtro,
        [FromQuery] ParametrosPaginacion paginacion,
        CancellationToken cancellationToken)
    {
        var resultado = await _solicitudes.ConsultarAsync(filtro, paginacion, cancellationToken);
        return resultado.ComoRespuestaHttp(this);
    }

    /// <summary>
    /// Obtiene el detalle de una solicitud con su historial de estados y comentarios.
    /// </summary>
    /// <param name="id">Identificador de la solicitud.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SolicitudDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id, CancellationToken cancellationToken)
    {
        var resultado = await _solicitudes.ObtenerAsync(id, cancellationToken);
        return resultado.ComoRespuestaHttp(this);
    }

    /// <summary>
    /// Registra una nueva solicitud y genera su código único (SOL-AAAA-0000).
    /// </summary>
    /// <param name="peticion">Datos de la solicitud.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpPost]
    [ProducesResponseType(typeof(SolicitudDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Crear(
        [FromBody] CrearSolicitudRequest peticion,
        CancellationToken cancellationToken)
    {
        var resultado = await _solicitudes.CrearAsync(peticion, cancellationToken);

        if (!resultado.Exito)
        {
            return resultado.ComoRespuestaHttp(this);
        }

        return CreatedAtAction(nameof(Obtener), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    /// <summary>
    /// Actualiza parcialmente una solicitud (solo los campos enviados).
    /// </summary>
    /// <param name="id">Identificador de la solicitud.</param>
    /// <param name="peticion">Campos a modificar.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(SolicitudDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(
        int id,
        [FromBody] ActualizarSolicitudRequest peticion,
        CancellationToken cancellationToken)
    {
        var resultado = await _solicitudes.ActualizarAsync(id, peticion, cancellationToken);
        return resultado.ComoRespuestaHttp(this);
    }

    /// <summary>
    /// Cambia el estado de la solicitud respetando el flujo definido y registra
    /// la transición en el historial.
    /// </summary>
    /// <remarks>
    /// Para cerrar una solicitud el comentario de resolución es obligatorio. Solo
    /// un administrador o un analista puede reabrir una solicitud cerrada.
    /// </remarks>
    /// <param name="id">Identificador de la solicitud.</param>
    /// <param name="peticion">Estado nuevo y comentario de la transición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(typeof(SolicitudDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CambiarEstado(
        int id,
        [FromBody] CambiarEstadoRequest peticion,
        CancellationToken cancellationToken)
    {
        var resultado = await _solicitudes.CambiarEstadoAsync(id, peticion, cancellationToken);
        return resultado.ComoRespuestaHttp(this);
    }

    /// <summary>
    /// Asigna o reasigna la solicitud a un responsable técnico.
    /// </summary>
    /// <param name="id">Identificador de la solicitud.</param>
    /// <param name="peticion">Usuario responsable.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpPatch("{id:int}/asignacion")]
    [ProducesResponseType(typeof(SolicitudDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Asignar(
        int id,
        [FromBody] AsignarResponsableRequest peticion,
        CancellationToken cancellationToken)
    {
        var resultado = await _solicitudes.AsignarAsync(id, peticion, cancellationToken);
        return resultado.ComoRespuestaHttp(this);
    }

    /// <summary>
    /// Registra un comentario interno o público en la solicitud.
    /// </summary>
    /// <remarks>
    /// Un solicitante solo puede crear comentarios públicos; el backend fuerza esa
    /// visibilidad aunque el cliente envíe otro valor.
    /// </remarks>
    /// <param name="id">Identificador de la solicitud.</param>
    /// <param name="peticion">Texto y visibilidad del comentario.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    [HttpPost("{id:int}/comentarios")]
    [ProducesResponseType(typeof(SolicitudDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AgregarComentario(
        int id,
        [FromBody] CrearComentarioRequest peticion,
        CancellationToken cancellationToken)
    {
        var resultado = await _solicitudes.AgregarComentarioAsync(id, peticion, cancellationToken);

        return resultado.Exito
            ? StatusCode(StatusCodes.Status201Created, resultado.Valor)
            : resultado.ComoRespuestaHttp(this);
    }
}
