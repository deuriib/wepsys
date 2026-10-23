using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using SB.GestionSolicitudes.Dominio.Excepciones;
using SB.GestionSolicitudes.Servicios.Errores;

namespace SB.GestionSolicitudes.Servicios.Middleware;

/// <summary>
/// Central error boundary implemented as an <see cref="IExceptionHandler"/>: every
/// unhandled exception is logged with a correlation id and returned as an RFC 9457
/// problem document, so clients never receive a stack trace and logs always have the
/// matching trace id.
/// </summary>
public sealed class ManejadorExcepciones : IExceptionHandler
{
    private readonly ILogger<ManejadorExcepciones> _registro;

    public ManejadorExcepciones(ILogger<ManejadorExcepciones> registro)
    {
        _registro = registro;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext contexto,
        Exception excepcion,
        CancellationToken cancellationToken)
    {
        var trazabilidad = contexto.TraceIdentifier;

        if (contexto.Response.HasStarted)
        {
            _registro.LogError(
                excepcion,
                "Excepción después de iniciar la respuesta. Trazabilidad {Trazabilidad}",
                trazabilidad);
            return false;
        }

        var (estado, codigo, titulo, detalle, registrarComoError) = Clasificar(excepcion);

        if (registrarComoError)
        {
            _registro.LogError(
                excepcion,
                "Error no controlado en {Metodo} {Ruta}. Trazabilidad {Trazabilidad}",
                contexto.Request.Method,
                contexto.Request.GetDisplayUrl(),
                trazabilidad);
        }
        else
        {
            _registro.LogWarning(
                "Regla de negocio rechazada en {Metodo} {Ruta}: {Mensaje}. Trazabilidad {Trazabilidad}",
                contexto.Request.Method,
                contexto.Request.GetDisplayUrl(),
                excepcion.Message,
                trazabilidad);
        }

        contexto.Response.Clear();

        await ProblemaApi.EscribirAsync(
            contexto,
            ProblemaApi.Crear(estado, codigo, titulo, detalle, contexto));

        return true;
    }

    private static (int Estado, string Codigo, string Titulo, string Detalle, bool RegistrarComoError) Clasificar(
        Exception excepcion) => excepcion switch
    {
        ExcepcionReglaNegocio regla =>
            (StatusCodes.Status409Conflict, "REGLA_NEGOCIO", "Conflicto con una regla de negocio", regla.Message, false),

        DbUpdateException =>
            (StatusCodes.Status409Conflict, "CONFLICTO_PERSISTENCIA", "Conflicto al guardar los datos",
                "La operación entra en conflicto con datos existentes. Verifique los valores únicos e intente nuevamente.", true),

        UnauthorizedAccessException =>
            (StatusCodes.Status401Unauthorized, "NO_AUTENTICADO", "No autenticado",
                "Se requiere un token válido para ejecutar esta operación.", false),

        KeyNotFoundException noEncontrado =>
            (StatusCodes.Status404NotFound, "NO_ENCONTRADO", "Recurso no encontrado", noEncontrado.Message, false),

        OperationCanceledException =>
            (StatusCodes.Status499ClientClosedRequest, "CANCELADO", "Operación cancelada",
                "La solicitud fue cancelada por el cliente.", false),

        _ => (StatusCodes.Status500InternalServerError, "ERROR_INTERNO", "Error interno del servidor",
            "Ocurrió un error inesperado. Comparta el identificador de trazabilidad con soporte.", true)
    };
}
