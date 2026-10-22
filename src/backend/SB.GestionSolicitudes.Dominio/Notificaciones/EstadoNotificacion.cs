namespace SB.GestionSolicitudes.Dominio.Notificaciones;

/// <summary>
/// Outcome of a notification attempt. Failures are recorded instead of thrown so
/// a broken delivery channel never rolls back a business transaction.
/// </summary>
public enum EstadoNotificacion
{
    Pendiente = 1,
    Enviada = 2,
    Fallida = 3
}
