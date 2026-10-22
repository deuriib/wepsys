using Microsoft.Extensions.Logging;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Dominio.Notificaciones;

namespace SB.GestionSolicitudes.Infraestructura.Notificaciones.Canales;

/// <summary>
/// Console channel, useful for local demos: it shows the event in the terminal
/// while keeping the same contract as any other channel.
/// </summary>
public sealed class CanalConsolaNotificacion : ICanalNotificacion
{
    private readonly ILogger<CanalConsolaNotificacion> _registro;

    public CanalConsolaNotificacion(ILogger<CanalConsolaNotificacion> registro)
    {
        _registro = registro;
    }

    public CanalNotificacion Canal => CanalNotificacion.Consola;

    public Task<bool> EnviarAsync(
        Notificacion notificacion,
        DestinatarioNotificacion destinatario,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[NOTIFICACIÓN] {notificacion.Asunto} -> {destinatario.Nombre}: {notificacion.Mensaje}");
        _registro.LogDebug("Notificación enviada al canal consola para {DestinatarioId}.", destinatario.Id);
        return Task.FromResult(true);
    }
}
