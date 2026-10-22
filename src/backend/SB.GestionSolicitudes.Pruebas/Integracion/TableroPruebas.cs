using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SB.GestionSolicitudes.Dominio.Solicitudes;
using SB.GestionSolicitudes.Pruebas.Infraestructura;
using static SB.GestionSolicitudes.Pruebas.Infraestructura.ClienteApiExtensor;
using Xunit;

namespace SB.GestionSolicitudes.Pruebas.Integracion;

/// <summary>
/// The dashboard is a single aggregate call; these tests protect the numbers the
/// UI shows against silent drift.
/// </summary>
public sealed class TableroPruebas
{
    [Fact]
    public async Task El_resumen_es_coherente_con_las_solicitudes_sembradas()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();

        var resumen = await cliente.GetFromJsonAsync<ResumenPrueba>("/api/dashboard/resumen", OPCIONES_JSON);

        resumen.ShouldNotBeNull();
        resumen.Total.ShouldBe(10);
        resumen.Abiertas.ShouldBe(8);
        resumen.Cerradas.ShouldBe(2);
        resumen.Abiertas.ShouldBe(resumen.Total - resumen.Cerradas);
        resumen.PorEstado.Sum(conteo => conteo.Total).ShouldBe(resumen.Total);
        resumen.PorPrioridad.Sum(conteo => conteo.Total).ShouldBe(resumen.Total);
        resumen.UltimasSolicitudes.Count.ShouldBeLessThanOrEqualTo(8);
    }

    [Fact]
    public async Task Las_ultimas_solicitudes_llegan_ordenadas_de_la_mas_reciente_a_la_mas_antigua()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();

        var resumen = await cliente.GetFromJsonAsync<ResumenPrueba>("/api/dashboard/resumen", OPCIONES_JSON);

        resumen!.UltimasSolicitudes.ShouldNotBeEmpty();
        resumen.UltimasSolicitudes
            .Select(solicitud => solicitud.FechaCreacion)
            .ShouldBe(resumen.UltimasSolicitudes
                .Select(solicitud => solicitud.FechaCreacion)
                .OrderByDescending(fecha => fecha)
                .ToList());
    }

    [Fact]
    public async Task El_conteo_de_vencidas_coincide_con_el_filtro_de_la_consulta()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();

        var resumen = await cliente.GetFromJsonAsync<ResumenPrueba>("/api/dashboard/resumen", OPCIONES_JSON);
        var vencidas = await cliente.GetFromJsonAsync<PaginaPrueba>(
            "/api/solicitudes?soloVencidas=true&tamanoPagina=100",
            OPCIONES_JSON);

        vencidas!.TotalRegistros.ShouldBe(resumen!.Vencidas);
    }

    [Fact]
    public async Task El_resumen_exige_autenticacion()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/api/dashboard/resumen");

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task El_tablero_del_analista_cuenta_solo_su_alcance()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var analista = await fabrica.ClienteAnalistaAsync();

        var resumen = await analista.GetFromJsonAsync<ResumenPrueba>("/api/dashboard/resumen", OPCIONES_JSON);
        var lista = await analista.GetFromJsonAsync<PaginaPrueba>("/api/solicitudes?tamanoPagina=100", OPCIONES_JSON);

        resumen.ShouldNotBeNull();
        resumen.Total.ShouldBe(lista!.TotalRegistros);
        resumen.Total.ShouldBeLessThan(10);
    }

    private sealed record ResumenPrueba(
        int Total,
        int Abiertas,
        int Cerradas,
        int Vencidas,
        IReadOnlyList<ConteoPrueba> PorEstado,
        IReadOnlyList<ConteoPrueba> PorPrioridad,
        IReadOnlyList<FilaPrueba> UltimasSolicitudes);

    private sealed record ConteoPrueba(string Clave, int Total);

    private sealed record FilaPrueba(int Id, string Codigo, string Titulo, EstadoSolicitud Estado, DateTime FechaCreacion);

    private sealed record PaginaPrueba(IReadOnlyList<FilaPrueba> Elementos, int TotalRegistros);
}
