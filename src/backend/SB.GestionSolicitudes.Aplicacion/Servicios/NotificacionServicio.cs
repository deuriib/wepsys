using SB.GestionSolicitudes.Aplicacion.Comun;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Aplicacion.Dtos;

namespace SB.GestionSolicitudes.Aplicacion.Servicios;

/// <summary>
/// In-app notification inbox. It reads what the notification dispatcher already
/// persisted, so the UI has a traceable history of every event.
/// </summary>
public sealed class NotificacionServicio
{
    private const int LIMITE_PREDETERMINADO = 50;

    private readonly INotificacionRepositorio _notificaciones;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IUsuarioActual _usuarioActual;

    public NotificacionServicio(
        INotificacionRepositorio notificaciones,
        IUnidadDeTrabajo unidadDeTrabajo,
        IUsuarioActual usuarioActual)
    {
        _notificaciones = notificaciones;
        _unidadDeTrabajo = unidadDeTrabajo;
        _usuarioActual = usuarioActual;
    }

    public async Task<IReadOnlyList<NotificacionDto>> ObtenerMisNotificacionesAsync(
        bool soloNoLeidas,
        CancellationToken cancellationToken = default)
    {
        var notificaciones = await _notificaciones.ObtenerPorUsuarioAsync(
            _usuarioActual.Id,
            soloNoLeidas,
            LIMITE_PREDETERMINADO,
            cancellationToken);

        return notificaciones
            .Select(notificacion => new NotificacionDto(
                notificacion.Id,
                notificacion.SolicitudId,
                notificacion.Solicitud?.Codigo ?? string.Empty,
                notificacion.Asunto,
                notificacion.Mensaje,
                notificacion.Canal.ToString(),
                notificacion.Estado.ToString(),
                notificacion.Fecha,
                notificacion.Leida))
            .ToList();
    }

    public async Task<Resultado> MarcarComoLeidaAsync(int id, CancellationToken cancellationToken = default)
    {
        var actualizada = await _notificaciones.MarcarComoLeidaAsync(id, _usuarioActual.Id, cancellationToken);

        if (!actualizada)
        {
            return Resultado.Falla(
                ErrorAplicacion.NoEncontrado($"No existe la notificación {id} para el usuario actual."));
        }

        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        return Resultado.Ok();
    }
}
