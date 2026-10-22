using Microsoft.Extensions.Logging;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Dominio.Notificaciones;

namespace SB.GestionSolicitudes.Infraestructura.Notificaciones.Canales;

/// <summary>
/// In-app channel. The notification record itself is the delivery, so this
/// channel only confirms it; the inbox endpoint reads what the dispatcher stored.
/// </summary>
public sealed class CanalBaseDatosNotificacion : ICanalNotificacion
{
    private readonly ILogger<CanalBaseDatosNotificacion> _registro;

    public CanalBaseDatosNotificacion(ILogger<CanalBaseDatosNotificacion> registro)
    {
        _registro = registro;
    }

    public CanalNotificacion Canal => CanalNotificacion.BaseDatos;

    public Task<bool> EnviarAsync(
        Notificacion notificacion,
        DestinatarioNotificacion destinatario,
        CancellationToken cancellationToken = default)
    {
        _registro.LogInformation(
            "Notificación en bandeja para {Destinatario}: {Asunto}",
            destinatario.Correo,
            notificacion.Asunto);

        return Task.FromResult(true);
    }
}
