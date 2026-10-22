using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using SB.GestionSolicitudes.Pruebas.Infraestructura;
using static SB.GestionSolicitudes.Pruebas.Infraestructura.ClienteApiExtensor;
using Xunit;

namespace SB.GestionSolicitudes.Pruebas.Integracion;

/// <summary>
/// Covers the maintenance of the government entities catalog, including the fact
/// that it lives in a plain-text file inside the project.
/// </summary>
public sealed class EntidadesGubernamentalesPruebas
{
    [Fact]
    public async Task El_listado_devuelve_las_entidades_sembradas_con_sus_totales()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();

        var pagina = await cliente.GetFromJsonAsync<PaginaEntidadesPrueba>(
            "/api/entidades-gubernamentales?tamanoPagina=10",
            OPCIONES_JSON);

        pagina.ShouldNotBeNull();
        pagina.TotalRegistros.ShouldBe(3);
        pagina.Elementos.Count.ShouldBe(3);
        pagina.Elementos.ShouldContain(entidad => entidad.Nombre == "Banco Central de la República Dominicana");
    }

    [Fact]
    public async Task El_filtro_por_texto_ignora_mayusculas()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();

        var pagina = await cliente.GetFromJsonAsync<PaginaEntidadesPrueba>(
            "/api/entidades-gubernamentales?texto=archivo",
            OPCIONES_JSON);

        pagina!.TotalRegistros.ShouldBe(1);
        pagina.Elementos.Single().Nombre.ShouldBe("Archivo General de la Nación");
    }

    [Fact]
    public async Task El_filtro_por_categoria_devuelve_solo_esa_categoria()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();

        var pagina = await cliente.GetFromJsonAsync<PaginaEntidadesPrueba>(
            "/api/entidades-gubernamentales?categoria=Organismo%20Aut%C3%B3nomo",
            OPCIONES_JSON);

        pagina!.TotalRegistros.ShouldBe(1);
        pagina.Elementos.ShouldAllBe(entidad => entidad.Categoria == "Organismo Autónomo");
    }

    [Fact]
    public async Task Las_opciones_de_filtro_se_derivan_del_catalogo()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();

        var opciones = await cliente.GetFromJsonAsync<OpcionesPrueba>(
            "/api/entidades-gubernamentales/opciones-filtro",
            OPCIONES_JSON);

        opciones.ShouldNotBeNull();
        opciones.Categorias.ShouldContain("Organismo Autónomo");
        opciones.PoderesDelEstado.ShouldContain("Poder Ejecutivo");
        opciones.Sectores.ShouldContain("Finanzas");
    }

    [Fact]
    public async Task El_administrador_crea_una_entidad_y_se_persiste_en_el_archivo_de_texto()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/entidades-gubernamentales",
            new
            {
                nombre = "Instituto Nacional de Estabilidad Financiera",
                categoria = "Organismo Descentralizado Funcionalmente",
                poderDelEstado = "Poder Ejecutivo",
                sector = "Finanzas",
                activa = true
            },
            OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        var creada = await respuesta.LeerComoAsync<EntidadPrueba>();
        creada.ShouldNotBeNull();
        creada.Id.ShouldNotBe(Guid.Empty);

        var obtenida = await cliente.GetFromJsonAsync<EntidadPrueba>(
            $"/api/entidades-gubernamentales/{creada.Id}",
            OPCIONES_JSON);

        obtenida!.Nombre.ShouldBe(creada.Nombre);

        // The specification requires a plain-text store, so the file must exist and
        // contain the new record after the request completes.
        File.Exists(fabrica.RutaArchivoEntidades).ShouldBeTrue();

        using var documento = JsonDocument.Parse(await File.ReadAllTextAsync(fabrica.RutaArchivoEntidades));
        var nombres = documento.RootElement.GetProperty("entidades")
            .EnumerateArray()
            .Select(entidad => entidad.GetProperty("nombre").GetString())
            .ToList();

        nombres.ShouldContain(creada.Nombre);
        nombres.Count.ShouldBe(4);
    }

    [Fact]
    public async Task Crear_una_entidad_con_nombre_duplicado_devuelve_409()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/entidades-gubernamentales",
            new
            {
                nombre = "Acuario Nacional",
                categoria = "Organismo Descentralizado Funcionalmente",
                poderDelEstado = "Poder Ejecutivo",
                sector = "Medio Ambiente y Recursos Naturales",
                activa = true
            },
            OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await respuesta.LeerComoAsync<ErrorPrueba>())!.Codigo.ShouldBe("ENTIDAD_DUPLICADA");
    }

    [Fact]
    public async Task Crear_una_entidad_sin_nombre_devuelve_400_de_validacion()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/entidades-gubernamentales",
            new
            {
                nombre = "AB",
                categoria = "Organismo Autónomo",
                poderDelEstado = "Poder Ejecutivo",
                sector = "Finanzas"
            },
            OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await respuesta.LeerComoAsync<ErrorPrueba>())!.Codigo.ShouldBe("VALIDACION");
    }

    [Fact]
    public async Task Un_solicitante_no_puede_crear_entidades()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteSolicitanteAsync();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/entidades-gubernamentales",
            new
            {
                nombre = "Entidad No Autorizada",
                categoria = "Organismo Autónomo",
                poderDelEstado = "Poder Ejecutivo",
                sector = "Finanzas"
            },
            OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Un_analista_no_puede_eliminar_entidades()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var analista = await fabrica.ClienteAnalistaAsync();

        var respuesta = await analista.DeleteAsync("/api/entidades-gubernamentales/11111111-1111-4111-8111-111111111111");

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task El_administrador_actualiza_parcialmente_y_el_cambio_persiste()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();
        var id = Guid.Parse("11111111-1111-4111-8111-111111111111");

        var respuesta = await cliente.PatchAsJsonAsync(
            $"/api/entidades-gubernamentales/{id}",
            new { sector = "Ambiente y Recursos Naturales", activa = false },
            OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var actualizada = await respuesta.LeerComoAsync<EntidadPrueba>();
        actualizada!.Sector.ShouldBe("Ambiente y Recursos Naturales");
        actualizada.Activa.ShouldBeFalse();
        actualizada.FechaActualizacion.ShouldBeGreaterThanOrEqualTo(actualizada.FechaCreacion);
    }

    [Fact]
    public async Task El_administrador_elimina_una_entidad_y_deja_de_existir()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();
        var id = Guid.Parse("22222222-2222-4222-8222-222222222222");

        var eliminacion = await cliente.DeleteAsync($"/api/entidades-gubernamentales/{id}");
        eliminacion.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var consulta = await cliente.GetAsync($"/api/entidades-gubernamentales/{id}");
        consulta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Consultar_una_entidad_inexistente_devuelve_404()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();

        var respuesta = await cliente.GetAsync($"/api/entidades-gubernamentales/{Guid.NewGuid()}");

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task El_catalogo_real_contiene_las_181_entidades_entregadas()
    {
        // Guards the delivered data file itself, not the isolated test seed.
        var ruta = LocalizarArchivoSemilla();
        ruta.ShouldNotBeNull("No se encontró database/entidades-gubernamentales.seed.json.");

        using var documento = JsonDocument.Parse(await File.ReadAllTextAsync(ruta!));
        var entidades = documento.RootElement.GetProperty("entidades").EnumerateArray().ToList();

        entidades.Count.ShouldBe(181);
        entidades.Select(entidad => entidad.GetProperty("id").GetGuid()).Distinct().Count().ShouldBe(181);
        entidades.ShouldAllBe(entidad =>
            !string.IsNullOrWhiteSpace(entidad.GetProperty("nombre").GetString()) &&
            !string.IsNullOrWhiteSpace(entidad.GetProperty("categoria").GetString()));
    }

    private static string? LocalizarArchivoSemilla()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (directorio is not null)
        {
            var candidato = Path.Combine(directorio.FullName, "database", "entidades-gubernamentales.seed.json");

            if (File.Exists(candidato))
            {
                return candidato;
            }

            directorio = directorio.Parent;
        }

        return null;
    }

    private sealed record PaginaEntidadesPrueba(
        IReadOnlyList<EntidadPrueba> Elementos,
        int TotalRegistros,
        int Pagina,
        int TamanoPagina,
        int TotalPaginas);

    private sealed record EntidadPrueba(
        Guid Id,
        string Nombre,
        string Categoria,
        string PoderDelEstado,
        string Sector,
        bool Activa,
        DateTime FechaCreacion,
        DateTime FechaActualizacion);

    private sealed record OpcionesPrueba(
        IReadOnlyList<string> Categorias,
        IReadOnlyList<string> PoderesDelEstado,
        IReadOnlyList<string> Sectores);

    private sealed record ErrorPrueba(string? Codigo, string? Detalle, IDictionary<string, string[]>? Errores);
}
