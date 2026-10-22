using Microsoft.Extensions.Logging;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Dominio.Notificaciones;

namespace SB.GestionSolicitudes.Infraestructura.Notificaciones.Canales;

/// <summary>
/// Simulated email channel. Replacing it with a real SMTP or queue publisher is a
/// matter of registering another <see cref="ICanalNotificacion"/>; no use case changes.
/// </summary>
public sealed class CanalCorreoSimuladoNotificacion : ICanalNotificacion
{
    private readonly ILogger<CanalCorreoSimuladoNotificacion> _registro;

    public CanalCorreoSimuladoNotificacion(ILogger<CanalCorreoSimuladoNotificacion> registro)
    {
        _registro = registro;
    }

    public CanalNotificacion Canal => CanalNotificacion.Correo;

    public Task<bool> EnviarAsync(
        Notificacion notificacion,
        DestinatarioNotificacion destinatario,
        CancellationToken cancellationToken = default)
    {
        _registro.LogInformation(
            "CORREO SIMULADO para {Correo} ({Nombre}) | {Asunto} | {Mensaje}",
            destinatario.Correo,
            destinatario.Nombre,
            notificacion.Asunto,
            notificacion.Mensaje);

        return Task.FromResult(true);
    }
}
