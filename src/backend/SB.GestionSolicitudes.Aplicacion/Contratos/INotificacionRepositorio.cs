using SB.GestionSolicitudes.Dominio.Notificaciones;

namespace SB.GestionSolicitudes.Aplicacion.Contratos;

/// <summary>
/// Read model for the in-app notification inbox.
/// </summary>
public interface INotificacionRepositorio
{
    Task AgregarAsync(Notificacion notificacion, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notificacion>> ObtenerPorUsuarioAsync(
        int usuarioId,
        bool soloNoLeidas,
        int limite,
        CancellationToken cancellationToken = default);

    Task<bool> MarcarComoLeidaAsync(int notificacionId, int usuarioId, CancellationToken cancellationToken = default);
}
