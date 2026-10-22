using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using SB.GestionSolicitudes.Aplicacion.Comun;
using SB.GestionSolicitudes.Aplicacion.Dtos;
using SB.GestionSolicitudes.Dominio.EntidadesGubernamentales;
using SB.GestionSolicitudes.Infraestructura.Archivos;
using Xunit;

namespace SB.GestionSolicitudes.Pruebas.Unitarias;

/// <summary>
/// The government entities catalog must survive a process restart, so these tests
/// exercise the file store itself: seeding, atomic writes, filtering and paging.
/// </summary>
public sealed class RepositorioEntidadesArchivoPruebas : IDisposable
{
    private readonly string _directorio = Path.Combine(
        Path.GetTempPath(),
        "sb-repositorio-pruebas",
        Guid.NewGuid().ToString("N"));

    private readonly string _rutaArchivo;
    private readonly string _rutaSemilla;

    public RepositorioEntidadesArchivoPruebas()
    {
        Directory.CreateDirectory(_directorio);
        _rutaArchivo = Path.Combine(_directorio, "entidades-gubernamentales.json");
        _rutaSemilla = Path.Combine(_directorio, "entidades-gubernamentales.seed.json");

        File.WriteAllText(_rutaSemilla, JsonSerializer.Serialize(
            new DocumentoEntidadesGubernamentales
            {
                Entidades = new List<EntidadGubernamental>
                {
                    CrearEntidad("Acuario Nacional", "Organismo Descentralizado Funcionalmente", "Medio Ambiente"),
                    CrearEntidad("Banco Central de la República Dominicana", "Organismo Autónomo", "Finanzas"),
                    CrearEntidad("Archivo General de la Nación", "Organismo Descentralizado Funcionalmente", "Cultura")
                }
            },
            OpcionesAlmacenEntidades.OPCIONES_JSON));
    }

    [Fact]
    public async Task La_primera_lectura_crea_el_archivo_a_partir_de_la_semilla()
    {
        using var repositorio = CrearRepositorio();

        var total = await repositorio.ContarAsync();

        total.ShouldBe(3);
        File.Exists(_rutaArchivo).ShouldBeTrue();
    }

    [Fact]
    public async Task Sin_semilla_el_archivo_se_crea_vacio()
    {
        File.Delete(_rutaSemilla);
        using var repositorio = CrearRepositorio();

        (await repositorio.ContarAsync()).ShouldBe(0);
        File.Exists(_rutaArchivo).ShouldBeTrue();
    }

    [Fact]
    public async Task Agregar_persiste_y_una_nueva_instancia_lo_encuentra()
    {
        var entidad = CrearEntidad("Instituto Nacional de Estabilidad Financiera", "Organismo Autónomo", "Finanzas");

        using (var repositorio = CrearRepositorio())
        {
            await repositorio.AgregarAsync(entidad);
        }

        using var repositorioReiniciado = CrearRepositorio();
        var recuperada = await repositorioReiniciado.ObtenerPorIdAsync(entidad.Id);

        recuperada.ShouldNotBeNull();
        recuperada.Nombre.ShouldBe(entidad.Nombre);
        (await repositorioReiniciado.ContarAsync()).ShouldBe(4);
    }

    [Fact]
    public async Task Actualizar_reemplaza_el_registro_y_persiste()
    {
        using (var repositorio = CrearRepositorio())
        {
            var entidad = await repositorio.ObtenerPorIdAsync(
                (await repositorio.ConsultarAsync(new FiltroEntidadGubernamental(), new ParametrosPaginacion())).Elementos[0].Id);

            entidad!.Sector = "Sector Actualizado";
            entidad.Activa = false;
            await repositorio.ActualizarAsync(entidad);
        }

        using var repositorioReiniciado = CrearRepositorio();
        var entidades = await repositorioReiniciado.ConsultarAsync(new FiltroEntidadGubernamental(), new ParametrosPaginacion());

        entidades.Elementos.ShouldContain(entidad => entidad.Sector == "Sector Actualizado" && !entidad.Activa);
    }

    [Fact]
    public async Task Eliminar_devuelve_falso_cuando_el_id_no_existe()
    {
        using var repositorio = CrearRepositorio();

        (await repositorio.EliminarAsync(Guid.NewGuid())).ShouldBeFalse();
        (await repositorio.ContarAsync()).ShouldBe(3);
    }

    [Fact]
    public async Task Eliminar_quita_el_registro_del_archivo()
    {
        using var repositorio = CrearRepositorio();
        var todas = await repositorio.ConsultarAsync(new FiltroEntidadGubernamental(), new ParametrosPaginacion());

        (await repositorio.EliminarAsync(todas.Elementos[0].Id)).ShouldBeTrue();

        (await repositorio.ContarAsync()).ShouldBe(2);
        (await repositorio.ObtenerPorIdAsync(todas.Elementos[0].Id)).ShouldBeNull();
    }

    [Fact]
    public async Task ExisteNombre_ignora_mayusculas_y_respeta_la_exclusion()
    {
        using var repositorio = CrearRepositorio();
        var existente = (await repositorio.ObtenerPorIdAsync(
            (await repositorio.ConsultarAsync(new FiltroEntidadGubernamental(), new ParametrosPaginacion())).Elementos[0].Id))!;

        (await repositorio.ExisteNombreAsync("  acuario nacional  ", null)).ShouldBeTrue();
        (await repositorio.ExisteNombreAsync(existente.Nombre, existente.Id)).ShouldBeFalse();
        (await repositorio.ExisteNombreAsync("Entidad Inexistente", null)).ShouldBeFalse();
    }

    [Fact]
    public async Task La_consulta_filtra_por_texto_y_ordena_alfabeticamente()
    {
        using var repositorio = CrearRepositorio();

        var pagina = await repositorio.ConsultarAsync(
            new FiltroEntidadGubernamental { Texto = "organismo autónomo" },
            new ParametrosPaginacion());

        pagina.TotalRegistros.ShouldBe(1);
        pagina.Elementos.Single().Nombre.ShouldBe("Banco Central de la República Dominicana");

        var todas = await repositorio.ConsultarAsync(new FiltroEntidadGubernamental(), new ParametrosPaginacion());
        todas.Elementos.Select(entidad => entidad.Nombre).ShouldBe(
            new[] { "Acuario Nacional", "Archivo General de la Nación", "Banco Central de la República Dominicana" });
    }

    [Fact]
    public async Task La_consulta_pagina_sobre_el_total_filtrado()
    {
        using var repositorio = CrearRepositorio();

        var segundaPagina = await repositorio.ConsultarAsync(
            new FiltroEntidadGubernamental(),
            new ParametrosPaginacion { Pagina = 2, TamanoPagina = 2 });

        segundaPagina.TotalRegistros.ShouldBe(3);
        segundaPagina.TotalPaginas.ShouldBe(2);
        segundaPagina.Elementos.Count.ShouldBe(1);
        segundaPagina.Elementos.Single().Nombre.ShouldBe("Banco Central de la República Dominicana");
    }

    [Fact]
    public async Task Las_opciones_de_filtro_no_se_repiten()
    {
        using var repositorio = CrearRepositorio();

        var opciones = await repositorio.ObtenerOpcionesFiltroAsync();

        opciones.Categorias.Count.ShouldBe(2);
        opciones.Sectores.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Ninguna_escritura_deja_archivos_temporales()
    {
        using var repositorio = CrearRepositorio();

        await repositorio.AgregarAsync(CrearEntidad("Tesorería de la Seguridad Social", "Organismo Autónomo", "Seguridad Social"));

        Directory.GetFiles(_directorio, "*.tmp").ShouldBeEmpty();
    }

    [Fact]
    public async Task Un_archivo_corrupto_falla_con_un_mensaje_explicito()
    {
        await File.WriteAllTextAsync(_rutaArchivo, "{ esto no es json ");
        using var repositorio = CrearRepositorio();

        var excepcion = await Should.ThrowAsync<InvalidOperationException>(() => repositorio.ContarAsync());
        excepcion.Message.ShouldContain("JSON válido");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directorio))
        {
            Directory.Delete(_directorio, recursive: true);
        }
    }

    private RepositorioEntidadesGubernamentalesArchivo CrearRepositorio() =>
        new(
            Options.Create(new OpcionesAlmacenEntidades
            {
                RutaArchivo = _rutaArchivo,
                RutaSemilla = _rutaSemilla
            }),
            NullLogger<RepositorioEntidadesGubernamentalesArchivo>.Instance);

    private static EntidadGubernamental CrearEntidad(string nombre, string categoria, string sector) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = nombre,
        Categoria = categoria,
        PoderDelEstado = "Poder Ejecutivo",
        Sector = sector,
        Activa = true,
        FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        FechaActualizacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };
}
