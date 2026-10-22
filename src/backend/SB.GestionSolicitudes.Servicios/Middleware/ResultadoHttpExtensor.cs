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

    public static IActionResult CrearRespuestaError(ErrorAplicacion error, ControllerBase controlador) =>
        new ObjectResult(new RespuestaError
        {
            Type = $"https://httpstatuses.io/{CodigoEstado(error.Tipo)}",
            Title = TituloEstado(error.Tipo),
            Status = CodigoEstado(error.Tipo),
            Detail = error.Mensaje,
            Instance = controlador.Request.Path,
            Codigo = error.Codigo,
            Trazabilidad = controlador.HttpContext.TraceIdentifier
        })
        {
            StatusCode = CodigoEstado(error.Tipo)
        };

    public static int CodigoEstado(TipoError tipo) => tipo switch
    {
        TipoError.Validacion => StatusCodes.Status400BadRequest,
        TipoError.NoEncontrado => StatusCodes.Status404NotFound,
        TipoError.Conflicto => StatusCodes.Status409Conflict,
        TipoError.NoAutorizado => StatusCodes.Status401Unauthorized,
        TipoError.Prohibido => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string TituloEstado(TipoError tipo) => tipo switch
    {
        TipoError.Validacion => "Solicitud inválida",
        TipoError.NoEncontrado => "Recurso no encontrado",
        TipoError.Conflicto => "Conflicto con el estado actual",
        TipoError.NoAutorizado => "No autenticado",
        TipoError.Prohibido => "Acceso denegado",
        _ => "Error interno"
    };
}
