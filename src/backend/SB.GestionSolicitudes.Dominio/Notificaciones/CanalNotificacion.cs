namespace SB.GestionSolicitudes.Dominio.Notificaciones;

/// <summary>
/// Delivery channel. Adding a channel is a database value, not a code change,
/// because the dispatcher resolves channels by contract.
/// </summary>
public enum CanalNotificacion
{
    BaseDatos = 1,
    Correo = 2,
    Consola = 3
}
