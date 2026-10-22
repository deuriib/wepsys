namespace SB.GestionSolicitudes.Aplicacion.Contratos;

using SB.GestionSolicitudes.Dominio.Notificaciones;

/// <summary>
/// Business events that must produce a notification. Business code only names
/// the event; it never knows which channel will deliver it.
/// </summary>
public enum TipoEventoNotificacion
{
    SolicitudCreada = 1,
    SolicitudAsignada = 2,
    SolicitudCambioEstado = 3,
    SolicitudCerrada = 4,
    ComentarioAgregado = 5
}

/// <summary>
/// Payload of a notification event. Destinations are user ids, not emails, so
/// changing the delivery channel never changes the business call.
/// </summary>
public sealed record EventoNotificacionSolicitud(
    TipoEventoNotificacion Tipo,
    int SolicitudId,
    string Codigo,
    string Titulo,
    string Detalle,
    IReadOnlyList<int> Destinatarios);

/// <summary>
/// Recipient resolved by the dispatcher, so a channel can address the message
/// (e.g. send a simulated email) without querying the user store.
/// </summary>
public sealed record DestinatarioNotificacion(int Id, string Nombre, string Correo);

/// <summary>
/// Extension point: implement this interface and register it in the container to
/// add a delivery channel (queue, SMS, push) without editing any use case.
/// </summary>
public interface ICanalNotificacion
{
    CanalNotificacion Canal { get; }

    Task<bool> EnviarAsync(
        Notificacion notificacion,
        DestinatarioNotificacion destinatario,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Entry point used by the use cases to publish a business event. Implementations
/// must never throw: a delivery failure cannot roll back a business transaction.
/// </summary>
public interface INotificadorSolicitud
{
    Task NotificarAsync(EventoNotificacionSolicitud evento, CancellationToken cancellationToken = default);
}
