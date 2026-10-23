using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Shouldly;
using SB.GestionSolicitudes.Dominio.Solicitudes;
using SB.GestionSolicitudes.Pruebas.Infraestructura;
using static SB.GestionSolicitudes.Pruebas.Infraestructura.ClienteApiExtensor;
using Xunit;

namespace SB.GestionSolicitudes.Pruebas.Integracion;

/// <summary>
/// Integration coverage of the request lifecycle: creation, paging, filters,
/// role scoping, state machine, assignment and comments.
/// </summary>
public sealed class SolicitudesPruebas
{
    private const int AREA_TECNOLOGIA = 1;
    private const int TIPO_INCIDENTE = 1;

    [Fact]
    public async Task Crear_solicitud_devuelve_201_con_codigo_unico_y_estado_registrada()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteSolicitanteAsync();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/solicitudes",
            ConstruirPeticion(),
            OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
        respuesta.Headers.Location.ShouldNotBeNull();

        var creada = await respuesta.LeerComoAsync<DetallePrueba>();
        creada.ShouldNotBeNull();
        Regex.IsMatch(creada.Codigo, @"^SOL-\d{4}-\d{4}$").ShouldBeTrue($"Código inesperado: {creada.Codigo}");
        creada.Estado.ShouldBe(EstadoSolicitud.Registrada);
        creada.Historial.Count.ShouldBe(1);
        creada.Comentarios.ShouldBeEmpty();
    }

    [Fact]
    public async Task Crear_solicitud_con_titulo_demasiado_corto_devuelve_400_por_campo()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteSolicitanteAsync();

        var peticion = ConstruirPeticion();
        peticion["titulo"] = "abc";

        var respuesta = await cliente.PostAsJsonAsync("/api/solicitudes", peticion, OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var error = await respuesta.LeerComoAsync<ErrorPrueba>();
        error!.Codigo.ShouldBe("VALIDACION");
        error.Errors.ShouldNotBeNull();
        error.Errors.Keys.ShouldContain(clave => clave.Contains("Titulo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Crear_solicitud_con_area_inexistente_devuelve_400_de_negocio()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteSolicitanteAsync();

        var peticion = ConstruirPeticion();
        peticion["areaId"] = 9999;

        var respuesta = await cliente.PostAsJsonAsync("/api/solicitudes", peticion, OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await respuesta.LeerComoAsync<ErrorPrueba>())!.Codigo.ShouldBe("AREA_INVALIDA");
    }

    [Fact]
    public async Task Crear_solicitud_sin_autenticacion_devuelve_401()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/solicitudes", ConstruirPeticion(), OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task La_consulta_paginada_devuelve_totales_y_respeta_el_tamano_de_pagina()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();

        var pagina = await cliente.GetFromJsonAsync<PaginaPrueba>(
            "/api/solicitudes?pagina=2&tamanoPagina=4",
            OPCIONES_JSON);

        pagina.ShouldNotBeNull();
        pagina.TotalRegistros.ShouldBe(10);
        pagina.Pagina.ShouldBe(2);
        pagina.TamanoPagina.ShouldBe(4);
        pagina.TotalPaginas.ShouldBe(3);
        pagina.Elementos.Count.ShouldBe(4);
    }

    [Fact]
    public async Task La_consulta_supera_el_tamano_maximo_de_pagina_sin_fallar()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();

        var pagina = await cliente.GetFromJsonAsync<PaginaPrueba>(
            "/api/solicitudes?tamanoPagina=5000",
            OPCIONES_JSON);

        pagina!.TamanoPagina.ShouldBe(100);
        pagina.Elementos.Count.ShouldBe(10);
    }

    [Fact]
    public async Task El_filtro_por_estado_devuelve_solo_ese_estado()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();

        var pagina = await cliente.GetFromJsonAsync<PaginaPrueba>(
            "/api/solicitudes?estado=EnAnalisis",
            OPCIONES_JSON);

        pagina.ShouldNotBeNull();
        pagina.TotalRegistros.ShouldBeGreaterThan(0);
        pagina.Elementos.ShouldAllBe(fila => fila.Estado == EstadoSolicitud.EnAnalisis);
    }

    [Fact]
    public async Task El_filtro_por_texto_busca_en_codigo_y_titulo()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();

        var pagina = await cliente.GetFromJsonAsync<PaginaPrueba>(
            "/api/solicitudes?texto=reporte",
            OPCIONES_JSON);

        pagina.ShouldNotBeNull();
        pagina.TotalRegistros.ShouldBeGreaterThan(0);
        pagina.Elementos.ShouldAllBe(fila =>
            fila.Titulo.Contains("reporte", StringComparison.OrdinalIgnoreCase) ||
            fila.Codigo.Contains("reporte", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task El_solicitante_solo_ve_sus_propias_solicitudes()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var administrador = await fabrica.ClienteAdministradorAsync();

        var creadaPorAdministrador = await administrador.PostAsJsonAsync(
            "/api/solicitudes",
            ConstruirPeticion(),
            OPCIONES_JSON);
        creadaPorAdministrador.StatusCode.ShouldBe(HttpStatusCode.Created);

        var solicitante = await fabrica.ClienteSolicitanteAsync();
        var paginaSolicitante = await solicitante.GetFromJsonAsync<PaginaPrueba>(
            "/api/solicitudes?tamanoPagina=100",
            OPCIONES_JSON);

        paginaSolicitante!.TotalRegistros.ShouldBe(10);
        paginaSolicitante.Elementos.ShouldAllBe(fila => fila.Solicitante == "Pedro Ramírez");

        var paginaAdministrador = await administrador.GetFromJsonAsync<PaginaPrueba>(
            "/api/solicitudes?tamanoPagina=100",
            OPCIONES_JSON);

        paginaAdministrador!.TotalRegistros.ShouldBe(11);
    }

    [Fact]
    public async Task El_solicitante_no_puede_abrir_una_solicitud_ajena()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var administrador = await fabrica.ClienteAdministradorAsync();

        var creada = await administrador.PostAsJsonAsync("/api/solicitudes", ConstruirPeticion(), OPCIONES_JSON);
        var detalle = await creada.LeerComoAsync<DetallePrueba>();

        var solicitante = await fabrica.ClienteSolicitanteAsync();
        var respuesta = await solicitante.GetAsync($"/api/solicitudes/{detalle!.Id}");

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Consultar_una_solicitud_inexistente_devuelve_404()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();

        var respuesta = await cliente.GetAsync("/api/solicitudes/999999");

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await respuesta.LeerComoAsync<ErrorPrueba>())!.Codigo.ShouldBe("NO_ENCONTRADO");
    }

    [Fact]
    public async Task El_detalle_incluye_historial_comentarios_y_estados_permitidos()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();

        var pagina = await cliente.GetFromJsonAsync<PaginaPrueba>("/api/solicitudes?tamanoPagina=1", OPCIONES_JSON);
        var id = pagina!.Elementos[0].Id;

        var detalle = await cliente.GetFromJsonAsync<DetallePrueba>($"/api/solicitudes/{id}", OPCIONES_JSON);

        detalle.ShouldNotBeNull();
        detalle.Historial.ShouldNotBeEmpty();
        detalle.Comentarios.ShouldNotBeEmpty();
        detalle.EstadosPermitidos.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task El_administrador_cambia_el_estado_y_queda_registrado_en_el_historial()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();
        var id = await ObtenerIdEnEstadoAsync(cliente, EstadoSolicitud.Registrada);

        var detalleAntes = await cliente.GetFromJsonAsync<DetallePrueba>($"/api/solicitudes/{id}", OPCIONES_JSON);

        var respuesta = await cliente.PatchAsJsonAsync(
            $"/api/solicitudes/{id}/estado",
            new { estado = "EnAnalisis", comentario = "Tomada por el equipo de soporte." },
            OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var detalle = await respuesta.LeerComoAsync<DetallePrueba>();
        detalle!.Estado.ShouldBe(EstadoSolicitud.EnAnalisis);
        detalle.Historial.Count.ShouldBe(detalleAntes!.Historial.Count + 1);
        detalle.Historial.Last().EstadoNuevo.ShouldBe(EstadoSolicitud.EnAnalisis);
        detalle.Historial.Last().Comentario.ShouldBe("Tomada por el equipo de soporte.");
    }

    [Fact]
    public async Task Una_transicion_no_permitida_devuelve_409()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();
        var id = await ObtenerIdEnEstadoAsync(cliente, EstadoSolicitud.Registrada);

        var respuesta = await cliente.PatchAsJsonAsync(
            $"/api/solicitudes/{id}/estado",
            new { estado = "Cerrada", comentario = "Cierre directo." },
            OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await respuesta.LeerComoAsync<ErrorPrueba>())!.Codigo.ShouldBe("CONFLICTO");
    }

    [Fact]
    public async Task Cerrar_una_solicitud_resuelta_sin_comentario_devuelve_409()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();
        var id = await ObtenerIdEnEstadoAsync(cliente, EstadoSolicitud.Resuelta);

        var respuesta = await cliente.PatchAsJsonAsync(
            $"/api/solicitudes/{id}/estado",
            new { estado = "Cerrada" },
            OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cerrar_una_solicitud_resuelta_con_comentario_la_cierra()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();
        var id = await ObtenerIdEnEstadoAsync(cliente, EstadoSolicitud.Resuelta);

        var respuesta = await cliente.PatchAsJsonAsync(
            $"/api/solicitudes/{id}/estado",
            new { estado = "Cerrada", comentario = "Solución validada con el solicitante." },
            OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var detalle = await respuesta.LeerComoAsync<DetallePrueba>();
        detalle!.Estado.ShouldBe(EstadoSolicitud.Cerrada);
        detalle.FechaCierre.ShouldNotBeNull();
    }

    [Fact]
    public async Task El_solicitante_no_puede_cambiar_el_estado()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var solicitante = await fabrica.ClienteSolicitanteAsync();
        var id = await ObtenerIdEnEstadoAsync(solicitante, EstadoSolicitud.Registrada);

        var respuesta = await solicitante.PatchAsJsonAsync(
            $"/api/solicitudes/{id}/estado",
            new { estado = "EnAnalisis" },
            OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await respuesta.LeerComoAsync<ErrorPrueba>())!.Codigo.ShouldBe("PROHIBIDO");
    }

    [Fact]
    public async Task El_administrador_asigna_la_solicitud_a_un_analista()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();
        var id = await ObtenerIdEnEstadoAsync(cliente, EstadoSolicitud.Registrada);

        var usuarios = await cliente.GetFromJsonAsync<List<UsuarioPrueba>>("/api/usuarios", OPCIONES_JSON);
        var analista = usuarios!.Single(usuario => usuario.Correo == CORREO_ANALISTA);

        var respuesta = await cliente.PatchAsJsonAsync(
            $"/api/solicitudes/{id}/asignacion",
            new { responsableId = analista.Id },
            OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var detalle = await respuesta.LeerComoAsync<DetallePrueba>();
        detalle!.ResponsableId.ShouldBe(analista.Id);
        detalle.Historial.Last().Comentario.ShouldBe("Solicitud asignada.");
    }

    [Fact]
    public async Task Asignar_un_usuario_inexistente_devuelve_400()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();
        var id = await ObtenerIdEnEstadoAsync(cliente, EstadoSolicitud.Registrada);

        var respuesta = await cliente.PatchAsJsonAsync(
            $"/api/solicitudes/{id}/asignacion",
            new { responsableId = 999999 },
            OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await respuesta.LeerComoAsync<ErrorPrueba>())!.Codigo.ShouldBe("RESPONSABLE_INVALIDO");
    }

    [Fact]
    public async Task Un_comentario_del_solicitante_siempre_queda_publico()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var solicitante = await fabrica.ClienteSolicitanteAsync();
        var id = await ObtenerIdEnEstadoAsync(solicitante, EstadoSolicitud.Registrada);

        var respuesta = await solicitante.PostAsJsonAsync(
            $"/api/solicitudes/{id}/comentarios",
            new { texto = "Añado el identificador del equipo afectado.", visibilidad = "Interna" },
            OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        var detalle = await respuesta.LeerComoAsync<DetallePrueba>();
        var comentario = detalle!.Comentarios.Single(item => item.Texto.StartsWith("Añado", StringComparison.Ordinal));
        comentario.Visibilidad.ShouldBe(VisibilidadComentario.Publica);
    }

    [Fact]
    public async Task Un_comentario_interno_del_analista_no_lo_ve_el_solicitante()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var administrador = await fabrica.ClienteAdministradorAsync();
        var id = await ObtenerIdEnEstadoAsync(administrador, EstadoSolicitud.Registrada);

        var comentarioInterno = await administrador.PostAsJsonAsync(
            $"/api/solicitudes/{id}/comentarios",
            new { texto = "Nota interna: revisar la configuración del servidor.", visibilidad = "Interna" },
            OPCIONES_JSON);
        comentarioInterno.StatusCode.ShouldBe(HttpStatusCode.Created);

        var solicitante = await fabrica.ClienteSolicitanteAsync();
        var detalle = await solicitante.GetFromJsonAsync<DetallePrueba>($"/api/solicitudes/{id}", OPCIONES_JSON);

        detalle!.Comentarios.ShouldNotContain(item => item.Texto.StartsWith("Nota interna", StringComparison.Ordinal));
    }

    [Fact]
    public async Task El_administrador_edita_parcialmente_una_solicitud_abierta()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();
        var id = await ObtenerIdEnEstadoAsync(cliente, EstadoSolicitud.Registrada);

        var respuesta = await cliente.PatchAsJsonAsync(
            $"/api/solicitudes/{id}",
            new { prioridad = "Critica", urlEvidencia = "https://evidencias.sb.gob.do/caso-1" },
            OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var detalle = await respuesta.LeerComoAsync<DetallePrueba>();
        detalle!.Prioridad.ShouldBe(PrioridadSolicitud.Critica);
        detalle.UrlEvidencia.ShouldBe("https://evidencias.sb.gob.do/caso-1");
    }

    [Fact]
    public async Task Editar_una_solicitud_cerrada_devuelve_409()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var cliente = await fabrica.ClienteAdministradorAsync();
        var id = await ObtenerIdEnEstadoAsync(cliente, EstadoSolicitud.Cerrada);

        var respuesta = await cliente.PatchAsJsonAsync(
            $"/api/solicitudes/{id}",
            new { titulo = "Un título distinto para la solicitud" },
            OPCIONES_JSON);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Las_notificaciones_se_generan_para_el_solicitante_al_cambiar_el_estado()
    {
        using var fabrica = new FabricaAplicacionWeb();
        var administrador = await fabrica.ClienteAdministradorAsync();
        var id = await ObtenerIdEnEstadoAsync(administrador, EstadoSolicitud.Registrada);

        var cambio = await administrador.PatchAsJsonAsync(
            $"/api/solicitudes/{id}/estado",
            new { estado = "EnAnalisis", comentario = "En revisión." },
            OPCIONES_JSON);
        cambio.StatusCode.ShouldBe(HttpStatusCode.OK);

        var solicitante = await fabrica.ClienteSolicitanteAsync();
        var notificaciones = await solicitante.GetFromJsonAsync<List<NotificacionPrueba>>(
            "/api/notificaciones",
            OPCIONES_JSON);

        notificaciones.ShouldNotBeNull();
        notificaciones.ShouldContain(notificacion => notificacion.SolicitudId == id);
        notificaciones.Count(notificacion => notificacion.SolicitudId == id).ShouldBe(1);
    }

    private static Dictionary<string, object?> ConstruirPeticion() => new()
    {
        ["titulo"] = "No puedo generar el reporte regulatorio",
        ["descripcion"] = "El sistema devuelve un error al exportar el reporte mensual de cartera.",
        ["prioridad"] = "Alta",
        ["areaId"] = AREA_TECNOLOGIA,
        ["tipoSolicitudId"] = TIPO_INCIDENTE,
        ["fechaCompromiso"] = DateTime.UtcNow.AddDays(7),
        ["urlEvidencia"] = (string?)null
    };

    private static async Task<int> ObtenerIdEnEstadoAsync(HttpClient cliente, EstadoSolicitud estado)
    {
        var pagina = await cliente.GetFromJsonAsync<PaginaPrueba>(
            $"/api/solicitudes?estado={estado}&tamanoPagina=100",
            OPCIONES_JSON);

        pagina.ShouldNotBeNull();
        pagina.Elementos.ShouldNotBeEmpty($"La semilla debe contener solicitudes en estado {estado}.");
        return pagina.Elementos[0].Id;
    }

    private sealed record PaginaPrueba(
        IReadOnlyList<FilaPrueba> Elementos,
        int TotalRegistros,
        int Pagina,
        int TamanoPagina,
        int TotalPaginas);

    private sealed record FilaPrueba(
        int Id,
        string Codigo,
        string Titulo,
        EstadoSolicitud Estado,
        PrioridadSolicitud Prioridad,
        string Solicitante,
        string? Responsable,
        DateTime FechaCompromiso,
        bool Vencida);

    private sealed record DetallePrueba(
        int Id,
        string Codigo,
        string Titulo,
        EstadoSolicitud Estado,
        PrioridadSolicitud Prioridad,
        int? ResponsableId,
        DateTime? FechaCierre,
        string? UrlEvidencia,
        IReadOnlyList<EstadoPermitidoPrueba> EstadosPermitidos,
        IReadOnlyList<HistorialPrueba> Historial,
        IReadOnlyList<ComentarioPrueba> Comentarios);

    private sealed record EstadoPermitidoPrueba(EstadoSolicitud Estado, string Descripcion);

    private sealed record HistorialPrueba(
        int Id,
        EstadoSolicitud? EstadoAnterior,
        EstadoSolicitud EstadoNuevo,
        DateTime Fecha,
        string Usuario,
        string? Comentario);

    private sealed record ComentarioPrueba(int Id, string Texto, VisibilidadComentario Visibilidad, DateTime Fecha, string Usuario);

    private sealed record UsuarioPrueba(int Id, string Nombre, string Correo, string Rol, bool Activo);

    private sealed record NotificacionPrueba(int Id, int SolicitudId, string Asunto, string Mensaje, bool Leida);

    private sealed record ErrorPrueba(string? Codigo, string? Detalle, IDictionary<string, string[]>? Errors);
}
