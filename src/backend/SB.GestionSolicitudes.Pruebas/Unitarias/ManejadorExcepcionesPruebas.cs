using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using SB.GestionSolicitudes.Servicios.Middleware;
using Xunit;

namespace SB.GestionSolicitudes.Pruebas.Unitarias;

/// <summary>
/// The error boundary must tell a database outage apart from a data conflict. Both
/// failures reach the API as Entity Framework exceptions, and reporting an outage as
/// "your data conflicts" (409) tells the caller to fix something that is not their
/// fault and blocks the retry that would succeed.
///
/// The <see cref="SqlException"/> instances are real: they come from trying to reach a
/// port where nothing is listening, so the test asserts the numbers the driver
/// actually reports instead of a hand-built exception.
/// </summary>
public sealed class ManejadorExcepcionesPruebas
{
    private const string CADENA_PUERTO_CERRADO =
        "Server=127.0.0.1,1;Database=master;Integrated Security=True;Encrypt=False;Connect Timeout=1;ConnectRetryCount=0";

    private const string CODIGO_BASE_DATOS_NO_DISPONIBLE = "BASE_DATOS_NO_DISPONIBLE";
    private const string CODIGO_CONFLICTO_PERSISTENCIA = "CONFLICTO_PERSISTENCIA";

    [Fact]
    public async Task Guardar_con_la_conexion_perdida_devuelve_503()
    {
        var (contexto, cuerpo) = await EjecutarAsync(new DbUpdateException("fallo", CapturarErrorDeConexion()));

        contexto.Response.StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        cuerpo[Codigo]!.GetValue<string>().ShouldBe(CODIGO_BASE_DATOS_NO_DISPONIBLE);
    }

    [Fact]
    public async Task Leer_con_la_conexion_perdida_devuelve_503()
    {
        var (contexto, cuerpo) = await EjecutarAsync(CapturarErrorDeConexion());

        contexto.Response.StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        cuerpo[Codigo]!.GetValue<string>().ShouldBe(CODIGO_BASE_DATOS_NO_DISPONIBLE);
    }

    [Fact]
    public async Task Guardar_con_un_fallo_que_no_es_de_conexion_devuelve_409()
    {
        var (contexto, cuerpo) = await EjecutarAsync(
            new DbUpdateException("conflicto", new InvalidOperationException("índice único")));

        contexto.Response.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        cuerpo[Codigo]!.GetValue<string>().ShouldBe(CODIGO_CONFLICTO_PERSISTENCIA);
    }

    [Fact]
    public async Task Falla_inesperada_devuelve_500_sin_exponer_detalles()
    {
        var (contexto, cuerpo) = await EjecutarAsync(new InvalidOperationException("detalle interno"));

        contexto.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        cuerpo["detail"]!.GetValue<string>().ShouldNotContain("detalle interno");
    }

    private static string Codigo => "codigo";

    private static SqlException CapturarErrorDeConexion()
    {
        using var conexion = new SqlConnection(CADENA_PUERTO_CERRADO);

        return Should.Throw<SqlException>(() => conexion.Open());
    }

    private static async Task<(DefaultHttpContext Contexto, JsonObject Cuerpo)> EjecutarAsync(Exception excepcion)
    {
        var servicios = new ServiceCollection();
        servicios.AddOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>();
        using var proveedor = servicios.BuildServiceProvider();

        var contexto = new DefaultHttpContext
        {
            RequestServices = proveedor,
            TraceIdentifier = "trazabilidad-de-prueba"
        };
        contexto.Request.Path = "/api/solicitudes";
        contexto.Response.Body = new MemoryStream();

        var manejador = new ManejadorExcepciones(NullLogger<ManejadorExcepciones>.Instance);
        var manejado = await manejador.TryHandleAsync(contexto, excepcion, CancellationToken.None);

        manejado.ShouldBeTrue();

        contexto.Response.Body.Position = 0;
        var texto = await new StreamReader(contexto.Response.Body).ReadToEndAsync();
        var cuerpo = JsonNode.Parse(texto)!.AsObject();

        return (contexto, cuerpo);
    }
}
