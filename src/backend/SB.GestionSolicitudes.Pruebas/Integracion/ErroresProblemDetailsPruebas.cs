using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SB.GestionSolicitudes.Pruebas.Infraestructura;
using Xunit;

namespace SB.GestionSolicitudes.Pruebas.Integracion;

/// <summary>
/// Locks the error contract: every failure leaves the API as an RFC 9457 problem
/// document, with the standard members, the stable business code and the correlation
/// id both in the body and in the response header.
/// </summary>
public sealed class ErroresProblemDetailsPruebas
{
    private const string TIPO_CONTENIDO = "application/problem+json";
    private const string CABECERA_TRAZABILIDAD = "X-Trace-Id";

    [Fact]
    public async Task Una_validacion_fallida_devuelve_problem_details_con_errores_por_campo()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/auth/login",
            new { correo = "no-es-un-correo", contrasena = "ClaveValida123!" },
            ClienteApiExtensor.OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        respuesta.Content.Headers.ContentType!.MediaType.ShouldBe(TIPO_CONTENIDO);

        var problema = await respuesta.LeerComoAsync<ProblemaPrueba>();
        problema.ShouldNotBeNull();
        problema.Type.ShouldBe("https://httpstatuses.io/400");
        problema.Title.ShouldBe("Solicitud inválida");
        problema.Status.ShouldBe(400);
        problema.Instance.ShouldBe("/api/auth/login");
        problema.Codigo.ShouldBe("VALIDACION");
        problema.Trazabilidad.ShouldNotBeNullOrWhiteSpace();
        problema.Errors.ShouldNotBeNull();
        problema.Errors.Keys.ShouldContain(clave => clave.Contains("Correo", StringComparison.OrdinalIgnoreCase));

        respuesta.Headers.GetValues(CABECERA_TRAZABILIDAD).Single().ShouldBe(problema.Trazabilidad);
    }

    [Fact]
    public async Task Una_regla_de_negocio_rechazada_devuelve_problem_details_con_su_codigo()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();

        var respuesta = await cliente.GetAsync($"/api/solicitudes/{int.MaxValue}");

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        respuesta.Content.Headers.ContentType!.MediaType.ShouldBe(TIPO_CONTENIDO);

        var problema = await respuesta.LeerComoAsync<ProblemaPrueba>();
        problema!.Codigo.ShouldBe("NO_ENCONTRADO");
        problema.Status.ShouldBe(404);
        problema.Trazabilidad.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Una_ruta_inexistente_devuelve_problem_details_en_lugar_de_un_cuerpo_vacio()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/api/ruta-que-no-existe");

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        respuesta.Content.Headers.ContentType!.MediaType.ShouldBe(TIPO_CONTENIDO);

        var problema = await respuesta.LeerComoAsync<ProblemaPrueba>();
        problema!.Codigo.ShouldBe("NO_ENCONTRADO");
        problema.Instance.ShouldBe("/api/ruta-que-no-existe");
        problema.Trazabilidad.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Una_peticion_sin_token_devuelve_problem_details_con_codigo_de_autenticacion()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/api/solicitudes");

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        respuesta.Content.Headers.ContentType!.MediaType.ShouldBe(TIPO_CONTENIDO);

        var problema = await respuesta.LeerComoAsync<ProblemaPrueba>();
        problema!.Codigo.ShouldBe("NO_AUTENTICADO");
        problema.Title.ShouldBe("No autenticado");
    }

    [Fact]
    public async Task Un_metodo_no_permitido_devuelve_problem_details()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PatchAsync("/api/auth/login", content: null);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        respuesta.Content.Headers.ContentType!.MediaType.ShouldBe(TIPO_CONTENIDO);

        (await respuesta.LeerComoAsync<ProblemaPrueba>())!.Codigo.ShouldBe("METODO_NO_PERMITIDO");
    }

    private sealed record ProblemaPrueba(
        string? Type,
        string? Title,
        int? Status,
        string? Detail,
        string? Instance,
        string? Codigo,
        string? Trazabilidad,
        IDictionary<string, string[]>? Errors);
}
