using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SB.GestionSolicitudes.Pruebas.Infraestructura;

/// <summary>
/// Boots the real API with an isolated SQL Server database and an isolated plain-text
/// catalog, so integration tests can run in parallel and never touch the
/// developer's data. The engine is a Testcontainers instance unless
/// <c>ConnectionStrings__BaseDatosPruebas</c> points somewhere else.
/// </summary>
public sealed class FabricaAplicacionWeb : WebApplicationFactory<Program>
{
    public const string CLAVE_JWT_PRUEBAS = "clave-de-pruebas-integracion-0123456789abcdef";

    private readonly string _nombreBaseDatos;
    private readonly string _cadenaConexion;
    private readonly string _directorioTrabajo = Path.Combine(
        Path.GetTempPath(),
        "sb-gestion-solicitudes-pruebas",
        Guid.NewGuid().ToString("N"));

    private readonly string _rutaEntidades;
    private readonly string _rutaSemilla;

    public FabricaAplicacionWeb()
    {
        Directory.CreateDirectory(_directorioTrabajo);

        _rutaEntidades = Path.Combine(_directorioTrabajo, "entidades-gubernamentales.json");
        _rutaSemilla = Path.Combine(_directorioTrabajo, "entidades-gubernamentales.seed.json");

        File.WriteAllText(_rutaSemilla, ConstruirSemillaEntidades());

        // The database is created before the host starts, because the API applies the
        // migrations and the seed during startup.
        (_nombreBaseDatos, _cadenaConexion) = BaseDatosPruebas.Crear();
    }

    public string RutaArchivoEntidades => _rutaEntidades;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuracion) =>
        {
            configuracion.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BaseDatos"] = _cadenaConexion,
                ["AlmacenEntidadesGubernamentales:RutaArchivo"] = _rutaEntidades,
                ["AlmacenEntidadesGubernamentales:RutaSemilla"] = _rutaSemilla,
                ["Jwt:ClaveSecreta"] = CLAVE_JWT_PRUEBAS,
                ["Jwt:MinutosVigencia"] = "60",
                ["Documentacion:Habilitada"] = "false",
                ["Serilog:MinimumLevel:Default"] = "Warning",
                ["Serilog:MinimumLevel:Override:Microsoft"] = "Error"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        BaseDatosPruebas.Eliminar(_nombreBaseDatos);

        try
        {
            if (Directory.Exists(_directorioTrabajo))
            {
                Directory.Delete(_directorioTrabajo, recursive: true);
            }
        }
        catch (IOException)
        {
            // A locked file must not fail the test run; the OS temp folder is cleaned
            // by the CI runner anyway.
        }
    }

    private static string ConstruirSemillaEntidades()
    {
        var documento = new
        {
            version = 1,
            entidades = new object[]
            {
                new
                {
                    id = Guid.Parse("11111111-1111-4111-8111-111111111111"),
                    nombre = "Acuario Nacional",
                    categoria = "Organismo Descentralizado Funcionalmente",
                    poderDelEstado = "Poder Ejecutivo",
                    sector = "Medio Ambiente y Recursos Naturales",
                    activa = true,
                    fechaCreacion = "2026-01-01T00:00:00Z",
                    fechaActualizacion = "2026-01-01T00:00:00Z"
                },
                new
                {
                    id = Guid.Parse("22222222-2222-4222-8222-222222222222"),
                    nombre = "Archivo General de la Nación",
                    categoria = "Organismo Descentralizado Funcionalmente",
                    poderDelEstado = "Poder Ejecutivo",
                    sector = "Cultura",
                    activa = true,
                    fechaCreacion = "2026-01-01T00:00:00Z",
                    fechaActualizacion = "2026-01-01T00:00:00Z"
                },
                new
                {
                    id = Guid.Parse("33333333-3333-4333-8333-333333333333"),
                    nombre = "Banco Central de la República Dominicana",
                    categoria = "Organismo Autónomo",
                    poderDelEstado = "Poder Ejecutivo",
                    sector = "Finanzas",
                    activa = true,
                    fechaCreacion = "2026-01-01T00:00:00Z",
                    fechaActualizacion = "2026-01-01T00:00:00Z"
                }
            }
        };

        return JsonSerializer.Serialize(documento, new JsonSerializerOptions { WriteIndented = true });
    }
}
