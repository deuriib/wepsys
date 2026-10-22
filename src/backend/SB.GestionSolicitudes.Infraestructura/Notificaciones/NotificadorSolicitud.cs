using Microsoft.Extensions.Logging;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Dominio.Notificaciones;

namespace SB.GestionSolicitudes.Infraestructura.Notificaciones;

/// <summary>
/// Dispatches a business event to every registered channel and persists the
/// outcome. Failures are recorded as <see cref="EstadoNotificacion.Fallida"/> and
/// never bubble up, so a broken email provider cannot break the workflow.
/// </summary>
public sealed class NotificadorSolicitud : INotificadorSolicitud
{
    private readonly IEnumerable<ICanalNotificacion> _canales;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly INotificacionRepositorio _notificaciones;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;
    private readonly ILogger<NotificadorSolicitud> _registro;

    public NotificadorSolicitud(
        IEnumerable<ICanalNotificacion> canales,
        IUsuarioRepositorio usuarios,
        INotificacionRepositorio notificaciones,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj,
        ILogger<NotificadorSolicitud> registro)
    {
        _canales = canales;
        _usuarios = usuarios;
        _notificaciones = notificaciones;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
        _registro = registro;
    }

    public async Task NotificarAsync(
        EventoNotificacionSolicitud evento,
        CancellationToken cancellationToken = default)
    {
        var destinatarios = evento.Destinatarios.Distinct().ToList();

        if (destinatarios.Count == 0)
        {
            return;
        }

        var asunto = ConstruirAsunto(evento);
        var mensaje = $"[{evento.Codigo}] {evento.Titulo}. {evento.Detalle}";
        var canales = _canales.ToList();

        try
        {
            foreach (var destinatarioId in destinatarios)
            {
                var usuario = await _usuarios.ObtenerPorIdAsync(destinatarioId, cancellationToken);

                if (usuario is null)
                {
                    _registro.LogWarning(
                        "Se omitió la notificación del evento {Evento}: el destinatario {DestinatarioId} no existe.",
                        evento.Tipo,
                        destinatarioId);
                    continue;
                }

                var destinatario = new DestinatarioNotificacion(usuario.Id, usuario.Nombre, usuario.Correo);

                foreach (var canal in canales)
                {
                    var notificacion = new Notificacion
                    {
                        SolicitudId = evento.SolicitudId,
                        DestinatarioId = usuario.Id,
                        Canal = canal.Canal,
                        Asunto = Truncar(asunto, Notificacion.LONGITUD_MAXIMA_ASUNTO),
                        Mensaje = Truncar(mensaje, Notificacion.LONGITUD_MAXIMA_MENSAJE),
                        Estado = EstadoNotificacion.Pendiente,
                        Fecha = _reloj.Ahora,
                        Leida = false
                    };

                    var enviada = await IntentarEnviarAsync(canal, notificacion, destinatario, cancellationToken);
                    notificacion.Estado = enviada ? EstadoNotificacion.Enviada : EstadoNotificacion.Fallida;

                    await _notificaciones.AgregarAsync(notificacion, cancellationToken);
                }
            }

            await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        }
        catch (Exception excepcion)
        {
            // Notifications are a side effect: they must not break the workflow.
            _registro.LogError(
                excepcion,
                "No fue posible registrar las notificaciones del evento {Evento} para la solicitud {SolicitudId}.",
                evento.Tipo,
                evento.SolicitudId);
        }
    }

    private async Task<bool> IntentarEnviarAsync(
        ICanalNotificacion canal,
        Notificacion notificacion,
        DestinatarioNotificacion destinatario,
        CancellationToken cancellationToken)
    {
        try
        {
            return await canal.EnviarAsync(notificacion, destinatario, cancellationToken);
        }
        catch (Exception excepcion)
        {
            _registro.LogError(
                excepcion,
                "El canal {Canal} falló al notificar la solicitud {SolicitudId}.",
                canal.Canal,
                notificacion.SolicitudId);

            return false;
        }
    }

    private static string ConstruirAsunto(EventoNotificacionSolicitud evento) => evento.Tipo switch
    {
        TipoEventoNotificacion.SolicitudCreada => $"Solicitud registrada {evento.Codigo}",
        TipoEventoNotificacion.SolicitudAsignada => $"Solicitud asignada {evento.Codigo}",
        TipoEventoNotificacion.SolicitudCambioEstado => $"Cambio de estado {evento.Codigo}",
        TipoEventoNotificacion.SolicitudCerrada => $"Solicitud cerrada {evento.Codigo}",
        TipoEventoNotificacion.ComentarioAgregado => $"Nuevo comentario en {evento.Codigo}",
        _ => $"Actualización de la solicitud {evento.Codigo}"
    };

    private static string Truncar(string valor, int longitudMaxima) =>
        valor.Length <= longitudMaxima ? valor : valor[..longitudMaxima];
}
