using Microsoft.AspNetCore.Mvc;
using SB.GestionSolicitudes.Aplicacion.Comun;
using SB.GestionSolicitudes.Servicios.Errores;

namespace SB.GestionSolicitudes.Servicios.Middleware;

/// <summary>
/// Translates the application result into an HTTP response. Keeping this mapping
/// in one place is what allows the use cases to ignore HTTP entirely.
/// </summary>
public static class ResultadoHttpExtensor
{
    public static IActionResult ComoRespuestaHttp<T>(this Resultado<T> resultado, ControllerBase controlador)
    {
        if (resultado.Exito)
        {
            return new OkObjectResult(resultado.Valor);
        }

        return CrearRespuestaError(resultado.Error!, controlador);
    }

    public static IActionResult ComoRespuestaHttp(this Resultado resultado, ControllerBase controlador)
    {
        if (resultado.Exito)
        {
            return new NoContentResult();
        }

        return CrearRespuestaError(resultado.Error!, controlador);
    }

    /// <summary>
    /// A rejected use case becomes an RFC 9457 problem document: the status comes from
    /// <see cref="TipoError"/>, the title from the status and the stable business code
    /// from the error itself.
    /// </summary>
    public static IActionResult CrearRespuestaError(ErrorAplicacion error, ControllerBase controlador)
    {
        var estado = CodigoEstado(error.Tipo);
        var titulo = ProblemaApi.DescribirEstado(estado).Titulo;

        return ProblemaApi.ComoResultado(
            ProblemaApi.Crear(estado, error.Codigo, titulo, error.Mensaje, controlador.HttpContext));
    }

    public static int CodigoEstado(TipoError tipo) => tipo switch
    {
        TipoError.Validacion => StatusCodes.Status400BadRequest,
        TipoError.NoEncontrado => StatusCodes.Status404NotFound,
        TipoError.Conflicto => StatusCodes.Status409Conflict,
        TipoError.NoAutorizado => StatusCodes.Status401Unauthorized,
        TipoError.Prohibido => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };
}
