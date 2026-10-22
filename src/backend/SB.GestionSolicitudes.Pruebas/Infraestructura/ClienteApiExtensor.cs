using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SB.GestionSolicitudes.Pruebas.Infraestructura;

/// <summary>
/// Helpers shared by the integration tests: authentication and JSON handling with
/// the same options the API uses (enums as strings).
/// </summary>
public static class ClienteApiExtensor
{
    public static readonly JsonSerializerOptions OPCIONES_JSON = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public const string CORREO_ADMINISTRADOR = "admin@sb.gob.do";
    public const string CONTRASENA_ADMINISTRADOR = "Admin123!";
    public const string CORREO_ANALISTA = "analista@sb.gob.do";
    public const string CONTRASENA_ANALISTA = "Analista123!";
    public const string CORREO_SOLICITANTE = "solicitante@sb.gob.do";
    public const string CONTRASENA_SOLICITANTE = "Solicitud123!";

    public static async Task<string> IniciarSesionAsync(
        this HttpClient cliente,
        string correo,
        string contrasena)
    {
        var respuesta = await cliente.PostAsJsonAsync(
            "/api/auth/login",
            new { correo, contrasena },
            OPCIONES_JSON);

        respuesta.EnsureSuccessStatusCode();

        var contenido = await respuesta.Content.ReadFromJsonAsync<RespuestaLogin>(OPCIONES_JSON);
        return contenido!.Token;
    }

    public static async Task<HttpClient> ClienteAutenticadoAsync(
        this FabricaAplicacionWeb fabrica,
        string correo,
        string contrasena)
    {
        var cliente = fabrica.CreateClient();
        var token = await cliente.IniciarSesionAsync(correo, contrasena);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    public static async Task<HttpClient> ClienteAdministradorAsync(this FabricaAplicacionWeb fabrica) =>
        await fabrica.ClienteAutenticadoAsync(CORREO_ADMINISTRADOR, CONTRASENA_ADMINISTRADOR);

    public static async Task<HttpClient> ClienteAnalistaAsync(this FabricaAplicacionWeb fabrica) =>
        await fabrica.ClienteAutenticadoAsync(CORREO_ANALISTA, CONTRASENA_ANALISTA);

    public static async Task<HttpClient> ClienteSolicitanteAsync(this FabricaAplicacionWeb fabrica) =>
        await fabrica.ClienteAutenticadoAsync(CORREO_SOLICITANTE, CONTRASENA_SOLICITANTE);

    public static Task<T?> LeerComoAsync<T>(this HttpResponseMessage respuesta) =>
        respuesta.Content.ReadFromJsonAsync<T>(OPCIONES_JSON);

    private sealed record RespuestaLogin(string Token);
}
