using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Dominio.Notificaciones;
using SB.GestionSolicitudes.Dominio.Usuarios;
using SB.GestionSolicitudes.Infraestructura.Notificaciones;
using Xunit;

namespace SB.GestionSolicitudes.Pruebas.Unitarias;

public sealed class NotificadorSolicitudPruebas
{
    private const int DESTINATARIO_UNO = 10;
    private const int DESTINATARIO_DOS = 20;
    private const int SOLICITUD_ID = 100;
    private const string CODIGO_SOLICITUD = "SOL-2026-0011";

    private readonly IUsuarioRepositorio _usuarios = Substitute.For<IUsuarioRepositorio>();
    private readonly INotificacionRepositorio _notificaciones = Substitute.For<INotificacionRepositorio>();
    private readonly IUnidadDeTrabajo _unidadDeTrabajo = Substitute.For<IUnidadDeTrabajo>();
    private readonly IReloj _reloj = Substitute.For<IReloj>();

    private readonly ICanalNotificacion _canalBaseDatos = Substitute.For<ICanalNotificacion>();
    private readonly ICanalNotificacion _canalCorreo = Substitute.For<ICanalNotificacion>();
    private readonly ICanalNotificacion _canalConsola = Substitute.For<ICanalNotificacion>();

    public NotificadorSolicitudPruebas()
    {
        _canalBaseDatos.Canal.Returns(CanalNotificacion.BaseDatos);
        _canalBaseDatos.EnviarAsync(Arg.Any<Notificacion>(), Arg.Any<DestinatarioNotificacion>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _canalCorreo.Canal.Returns(CanalNotificacion.Correo);
        _canalCorreo.EnviarAsync(Arg.Any<Notificacion>(), Arg.Any<DestinatarioNotificacion>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _canalConsola.Canal.Returns(CanalNotificacion.Consola);
        _canalConsola.EnviarAsync(Arg.Any<Notificacion>(), Arg.Any<DestinatarioNotificacion>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _reloj.Ahora.Returns(new DateTime(2026, 10, 23, 13, 8, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task NotificarAsync_persiste_exactamente_una_notificacion_por_destinatario_independientemente_del_numero_de_canales()
    {
        var usuario = new Usuario { Id = DESTINATARIO_UNO, Nombre = "Luis Peña", Correo = "luis@sb.gob.do", Rol = RolUsuario.Analista, Activo = true };
        _usuarios.ObtenerPorIdAsync(DESTINATARIO_UNO, Arg.Any<CancellationToken>()).Returns(usuario);

        var canales = new[] { _canalBaseDatos, _canalCorreo, _canalConsola };
        var notificador = new NotificadorSolicitud(
            canales,
            _usuarios,
            _notificaciones,
            _unidadDeTrabajo,
            _reloj,
            NullLogger<NotificadorSolicitud>.Instance);

        var evento = new EventoNotificacionSolicitud(
            TipoEventoNotificacion.SolicitudAsignada,
            SOLICITUD_ID,
            CODIGO_SOLICITUD,
            "No puedo acceder a mi heart",
            "La solicitud fue asignada a Luis Peña.",
            new[] { DESTINATARIO_UNO });

        await notificador.NotificarAsync(evento);

        // Se persiste únicamente una sola notificación (bandeja en base de datos)
        await _notificaciones.Received(1).AgregarAsync(
            Arg.Is<Notificacion>(n =>
                n.SolicitudId == SOLICITUD_ID &&
                n.DestinatarioId == DESTINATARIO_UNO &&
                n.Canal == CanalNotificacion.BaseDatos &&
                !n.Leida &&
                n.Asunto == $"Solicitud asignada {CODIGO_SOLICITUD}"),
            Arg.Any<CancellationToken>());

        // Cada canal registrado fue notificado para su correspondiente entrega
        await _canalBaseDatos.Received(1).EnviarAsync(Arg.Any<Notificacion>(), Arg.Any<DestinatarioNotificacion>(), Arg.Any<CancellationToken>());
        await _canalCorreo.Received(1).EnviarAsync(Arg.Any<Notificacion>(), Arg.Any<DestinatarioNotificacion>(), Arg.Any<CancellationToken>());
        await _canalConsola.Received(1).EnviarAsync(Arg.Any<Notificacion>(), Arg.Any<DestinatarioNotificacion>(), Arg.Any<CancellationToken>());

        await _unidadDeTrabajo.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotificarAsync_crea_una_notificacion_para_cada_destinatario_distinto()
    {
        var usuarioUno = new Usuario { Id = DESTINATARIO_UNO, Nombre = "Luis Peña", Correo = "luis@sb.gob.do", Rol = RolUsuario.Analista, Activo = true };
        var usuarioDos = new Usuario { Id = DESTINATARIO_DOS, Nombre = "Juan Pérez", Correo = "juan@sb.gob.do", Rol = RolUsuario.Solicitante, Activo = true };
        _usuarios.ObtenerPorIdAsync(DESTINATARIO_UNO, Arg.Any<CancellationToken>()).Returns(usuarioUno);
        _usuarios.ObtenerPorIdAsync(DESTINATARIO_DOS, Arg.Any<CancellationToken>()).Returns(usuarioDos);

        var canales = new[] { _canalBaseDatos, _canalCorreo, _canalConsola };
        var notificador = new NotificadorSolicitud(
            canales,
            _usuarios,
            _notificaciones,
            _unidadDeTrabajo,
            _reloj,
            NullLogger<NotificadorSolicitud>.Instance);

        var evento = new EventoNotificacionSolicitud(
            TipoEventoNotificacion.SolicitudAsignada,
            SOLICITUD_ID,
            CODIGO_SOLICITUD,
            "No puedo acceder a mi heart",
            "La solicitud fue asignada a Luis Peña.",
            new[] { DESTINATARIO_UNO, DESTINATARIO_DOS, DESTINATARIO_UNO }); // Incluye duplicado intencional

        await notificador.NotificarAsync(evento);

        // Se agrega una notificación para cada destinatario único (total: 2)
        await _notificaciones.Received(2).AgregarAsync(Arg.Any<Notificacion>(), Arg.Any<CancellationToken>());
        await _unidadDeTrabajo.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }
}
