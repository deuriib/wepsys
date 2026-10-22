using NSubstitute;
using Shouldly;
using SB.GestionSolicitudes.Aplicacion.Comun;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Aplicacion.Dtos;
using SB.GestionSolicitudes.Aplicacion.Servicios;
using SB.GestionSolicitudes.Dominio.Solicitudes;
using SB.GestionSolicitudes.Dominio.Usuarios;
using Xunit;

namespace SB.GestionSolicitudes.Pruebas.Unitarias;

/// <summary>
/// Service-level tests with mocked contracts. They cover the decisions the HTTP
/// tests cannot isolate cheaply: code generation, authorization, error mapping and
/// the notification that must (or must not) be published.
/// </summary>
public sealed class SolicitudServicioPruebas
{
    private const int SOLICITANTE_ID = 10;
    private const int OTRO_USUARIO_ID = 11;
    private const int ANALISTA_ID = 20;
    private const int ADMINISTRADOR_ID = 30;
    private const int AREA_ID = 1;
    private const int TIPO_ID = 2;

    private static readonly DateTime AHORA = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

    private readonly ISolicitudRepositorio _solicitudes = Substitute.For<ISolicitudRepositorio>();
    private readonly ICatalogoRepositorio _catalogos = Substitute.For<ICatalogoRepositorio>();
    private readonly IUsuarioRepositorio _usuarios = Substitute.For<IUsuarioRepositorio>();
    private readonly IUnidadDeTrabajo _unidadDeTrabajo = Substitute.For<IUnidadDeTrabajo>();
    private readonly INotificadorSolicitud _notificador = Substitute.For<INotificadorSolicitud>();
    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();

    [Fact]
    public async Task Crear_genera_el_codigo_secuencial_y_notifica_al_solicitante()
    {
        ConfigurarUsuario(SOLICITANTE_ID, RolUsuario.Solicitante);
        _catalogos.ExisteAreaAsync(AREA_ID, Arg.Any<CancellationToken>()).Returns(true);
        _catalogos.ExisteTipoSolicitudAsync(TIPO_ID, Arg.Any<CancellationToken>()).Returns(true);
        _solicitudes.ObtenerSiguienteSecuenciaAsync(AHORA.Year, Arg.Any<CancellationToken>()).Returns(5);
        _solicitudes.ExisteCodigoAsync("SOL-2026-0005", Arg.Any<CancellationToken>()).Returns(false);

        var resultado = await CrearServicio().CrearAsync(PeticionCreacion());

        resultado.Exito.ShouldBeTrue();
        resultado.Valor!.Codigo.ShouldBe("SOL-2026-0005");
        resultado.Valor.Estado.ShouldBe(EstadoSolicitud.Registrada);

        await _solicitudes.Received(1).AgregarAsync(
            Arg.Is<Solicitud>(solicitud => solicitud.Codigo == "SOL-2026-0005"),
            Arg.Any<CancellationToken>());
        await _unidadDeTrabajo.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());

        await _notificador.Received(1).NotificarAsync(
            Arg.Is<EventoNotificacionSolicitud>(evento =>
                evento.Tipo == TipoEventoNotificacion.SolicitudCreada &&
                evento.Codigo == "SOL-2026-0005" &&
                evento.Destinatarios.Contains(SOLICITANTE_ID)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Crear_rechaza_un_area_inexistente_sin_escribir_nada()
    {
        ConfigurarUsuario(SOLICITANTE_ID, RolUsuario.Solicitante);
        _catalogos.ExisteAreaAsync(AREA_ID, Arg.Any<CancellationToken>()).Returns(false);

        var resultado = await CrearServicio().CrearAsync(PeticionCreacion());

        resultado.Exito.ShouldBeFalse();
        resultado.Error!.Codigo.ShouldBe("AREA_INVALIDA");
        resultado.Error.Tipo.ShouldBe(TipoError.Validacion);

        await _solicitudes.DidNotReceiveWithAnyArgs().AgregarAsync(default!, default);
        await _notificador.DidNotReceiveWithAnyArgs().NotificarAsync(default!, default);
    }

    [Fact]
    public async Task Un_solicitante_no_puede_cambiar_el_estado()
    {
        ConfigurarUsuario(SOLICITANTE_ID, RolUsuario.Solicitante);
        _solicitudes.ObtenerDetalleAsync(1, Arg.Any<CancellationToken>()).Returns(CrearSolicitud());

        var resultado = await CrearServicio().CambiarEstadoAsync(1, new CambiarEstadoRequest
        {
            Estado = EstadoSolicitud.EnAnalisis
        });

        resultado.Exito.ShouldBeFalse();
        resultado.Error!.Codigo.ShouldBe("PROHIBIDO");
        resultado.Error.Tipo.ShouldBe(TipoError.Prohibido);
        await _unidadDeTrabajo.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Cerrar_una_solicitud_resuelta_sin_comentario_devuelve_conflicto()
    {
        var solicitud = CrearSolicitud();
        solicitud.CambiarEstado(EstadoSolicitud.EnAnalisis, ANALISTA_ID, RolUsuario.Analista, null, AHORA.AddHours(1));
        solicitud.CambiarEstado(EstadoSolicitud.Resuelta, ANALISTA_ID, RolUsuario.Analista, "Atendido.", AHORA.AddHours(2));

        ConfigurarUsuario(ADMINISTRADOR_ID, RolUsuario.Administrador);
        _solicitudes.ObtenerDetalleAsync(1, Arg.Any<CancellationToken>()).Returns(solicitud);

        var resultado = await CrearServicio().CambiarEstadoAsync(1, new CambiarEstadoRequest
        {
            Estado = EstadoSolicitud.Cerrada
        });

        resultado.Exito.ShouldBeFalse();
        resultado.Error!.Codigo.ShouldBe("CONFLICTO");
        resultado.Error.Tipo.ShouldBe(TipoError.Conflicto);
        solicitud.Estado.ShouldBe(EstadoSolicitud.Resuelta);
    }

    [Fact]
    public async Task Un_solicitante_no_puede_ver_una_solicitud_ajena()
    {
        ConfigurarUsuario(OTRO_USUARIO_ID, RolUsuario.Solicitante);
        _solicitudes.ObtenerDetalleAsync(1, Arg.Any<CancellationToken>()).Returns(CrearSolicitud());

        var resultado = await CrearServicio().ObtenerAsync(1);

        resultado.Exito.ShouldBeFalse();
        resultado.Error!.Codigo.ShouldBe("PROHIBIDO");
    }

    [Fact]
    public async Task El_comentario_de_un_solicitante_siempre_se_guarda_como_publico()
    {
        var solicitud = CrearSolicitud();
        ConfigurarUsuario(SOLICITANTE_ID, RolUsuario.Solicitante);
        _solicitudes.ObtenerDetalleAsync(1, Arg.Any<CancellationToken>()).Returns(solicitud);

        var resultado = await CrearServicio().AgregarComentarioAsync(1, new CrearComentarioRequest
        {
            Texto = "Adjunto la captura del error.",
            Visibilidad = VisibilidadComentario.Interna
        });

        resultado.Exito.ShouldBeTrue();
        resultado.Valor!.Comentarios.ShouldContain(comentario =>
            comentario.Texto == "Adjunto la captura del error." &&
            comentario.Visibilidad == VisibilidadComentario.Publica);
    }

    [Fact]
    public async Task Consultar_traduce_el_rol_a_un_alcance_de_datos()
    {
        ConfigurarUsuario(ANALISTA_ID, RolUsuario.Analista);
        _solicitudes.ConsultarAsync(
                Arg.Any<FiltroSolicitudConsulta>(),
                Arg.Any<AlcanceSolicitud>(),
                Arg.Any<int>(),
                Arg.Any<ParametrosPaginacion>(),
                Arg.Any<CancellationToken>())
            .Returns(ResultadoPaginado<Solicitud>.Vacio(new ParametrosPaginacion()));

        await CrearServicio().ConsultarAsync(new FiltroSolicitudConsulta(), new ParametrosPaginacion());

        await _solicitudes.Received(1).ConsultarAsync(
            Arg.Any<FiltroSolicitudConsulta>(),
            AlcanceSolicitud.AsignadosODisponibles,
            ANALISTA_ID,
            Arg.Any<ParametrosPaginacion>(),
            Arg.Any<CancellationToken>());
    }

    private SolicitudServicio CrearServicio() =>
        new(_solicitudes, _catalogos, _usuarios, _unidadDeTrabajo, _notificador, _usuarioActual, new RelojFijo(AHORA));

    private void ConfigurarUsuario(int id, RolUsuario rol)
    {
        _usuarioActual.Id.Returns(id);
        _usuarioActual.Rol.Returns(rol);
        _usuarioActual.Nombre.Returns("Usuario de prueba");
        _usuarioActual.Correo.Returns("usuario@sb.gob.do");
        _usuarioActual.EstaAutenticado.Returns(true);
    }

    private static CrearSolicitudRequest PeticionCreacion() => new()
    {
        Titulo = "No puedo generar el reporte regulatorio",
        Descripcion = "El sistema devuelve un error al exportar el reporte mensual de cartera.",
        Prioridad = PrioridadSolicitud.Alta,
        AreaId = AREA_ID,
        TipoSolicitudId = TIPO_ID,
        FechaCompromiso = AHORA.AddDays(7)
    };

    private static Solicitud CrearSolicitud() =>
        Solicitud.Registrar(
            "SOL-2026-0001",
            "No puedo generar el reporte regulatorio",
            "El sistema devuelve un error al exportar el reporte mensual de cartera.",
            PrioridadSolicitud.Alta,
            AREA_ID,
            TIPO_ID,
            SOLICITANTE_ID,
            AHORA.AddDays(7),
            AHORA,
            null,
            1);

    private sealed class RelojFijo : IReloj
    {
        public RelojFijo(DateTime ahora) => Ahora = ahora;

        public DateTime Ahora { get; }
    }
}
