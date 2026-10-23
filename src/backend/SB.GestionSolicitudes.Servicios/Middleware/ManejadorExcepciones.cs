using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Data.SqlClient;
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
    // SqlClient closes the connection when it reports severity 20 or higher
    // (connection refused, reset, unreachable), and uses number -2 for a timeout.
    private const int SEVERIDAD_CONEXION_PERDIDA = 20;
    private const int NUMERO_TIEMPO_ESPERA = -2;

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

        // The relational store is now remote, so a transport failure must not be
        // reported as a data conflict. SqlClient reports a lost or refused connection
        // with severity 20 and above, and a command timeout with number -2; a unique
        // index or foreign key violation arrives with severity 14. Both entry points
        // are covered: SaveChanges wraps the failure in DbUpdateException, while a
        // failed read surfaces the SqlException on its own.
        DbUpdateException { InnerException: SqlException baseDatos } when EsFalloDeConexion(baseDatos) =>
            (StatusCodes.Status503ServiceUnavailable, "BASE_DATOS_NO_DISPONIBLE", "Servicio no disponible",
                "La base de datos no está disponible en este momento. Intente nuevamente en unos segundos.", true),

        SqlException baseDatos when EsFalloDeConexion(baseDatos) =>
            (StatusCodes.Status503ServiceUnavailable, "BASE_DATOS_NO_DISPONIBLE", "Servicio no disponible",
                "La base de datos no está disponible en este momento. Intente nuevamente en unos segundos.", true),

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

    /// <summary>
    /// Tells a transport failure apart from a data conflict. Severity 20 and above is
    /// the engine closing the connection, so the caller may retry; a constraint
    /// violation (severity 14) keeps the 409 contract.
    /// </summary>
    private static bool EsFalloDeConexion(SqlException excepcion) =>
        excepcion.Class >= SEVERIDAD_CONEXION_PERDIDA || excepcion.Number == NUMERO_TIEMPO_ESPERA;
}
