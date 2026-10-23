using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SB.GestionSolicitudes.Dominio.Usuarios;
using SB.GestionSolicitudes.Pruebas.Infraestructura;
using static SB.GestionSolicitudes.Pruebas.Infraestructura.ClienteApiExtensor;
using Xunit;

namespace SB.GestionSolicitudes.Pruebas.Integracion;

/// <summary>
/// End-to-end coverage of the authentication boundary: who gets a token, who does
/// not, and what an anonymous caller can reach.
/// </summary>
public sealed class AutenticacionPruebas
{
    [Fact]
    public async Task Login_con_credenciales_validas_devuelve_token_y_perfil()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/auth/login",
            new { correo = CORREO_ADMINISTRADOR, contrasena = CONTRASENA_ADMINISTRADOR },
            OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var contenido = await respuesta.LeerComoAsync<RespuestaAutenticacionPrueba>();
        contenido.ShouldNotBeNull();
        contenido.Token.Split('.').Length.ShouldBe(3);
        contenido.ExpiraEn.ShouldBeGreaterThan(DateTime.UtcNow);
        contenido.Usuario.Correo.ShouldBe(CORREO_ADMINISTRADOR);
        contenido.Usuario.Rol.ShouldBe(RolUsuario.Administrador);
    }

    [Fact]
    public async Task Login_con_contrasena_incorrecta_devuelve_401_sin_revelar_cual_campo_fallo()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/auth/login",
            new { correo = CORREO_ADMINISTRADOR, contrasena = "ContrasenaEquivocada123!" },
            OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var error = await respuesta.LeerComoAsync<ErrorPrueba>();
        error!.Codigo.ShouldBe("CREDENCIALES_INVALIDAS");
        error.Trazabilidad.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_con_correo_inexistente_devuelve_la_misma_respuesta_que_una_contrasena_incorrecta()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/auth/login",
            new { correo = "nadie@sb.gob.do", contrasena = CONTRASENA_ADMINISTRADOR },
            OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await respuesta.LeerComoAsync<ErrorPrueba>())!.Codigo.ShouldBe("CREDENCIALES_INVALIDAS");
    }

    [Fact]
    public async Task Login_con_correo_invalido_devuelve_400_de_validacion()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/auth/login",
            new { correo = "no-es-un-correo", contrasena = CONTRASENA_ADMINISTRADOR },
            OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var error = await respuesta.LeerComoAsync<ErrorPrueba>();
        error!.Codigo.ShouldBe("VALIDACION");
        error.Errors.ShouldNotBeNull();
        error.Errors.Keys.ShouldContain(clave => clave.Contains("Correo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Un_endpoint_protegido_sin_token_devuelve_401()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/api/solicitudes");

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Un_token_manipulado_devuelve_401()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();
        var tokenValido = cliente.DefaultRequestHeaders.Authorization!.Parameter!;

        var clienteManipulado = fabrica.CreateClient();
        clienteManipulado.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenValido[..^4] + "AAAA");

        var respuesta = await clienteManipulado.GetAsync("/api/solicitudes");

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task El_endpoint_de_salud_responde_sin_autenticacion()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/salud");

        respuesta.IsSuccessStatusCode.ShouldBeTrue();
    }

    private sealed record RespuestaAutenticacionPrueba(
        string Token,
        DateTime ExpiraEn,
        UsuarioSesionPrueba Usuario);

    private sealed record UsuarioSesionPrueba(int Id, string Nombre, string Correo, RolUsuario Rol, string RolDescripcion);

    private sealed record ErrorPrueba(string? Codigo, string? Trazabilidad, IDictionary<string, string[]>? Errors);
}
