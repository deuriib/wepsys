using Microsoft.EntityFrameworkCore;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Dominio.Notificaciones;

namespace SB.GestionSolicitudes.Infraestructura.Persistencia.Repositorios;

public sealed class NotificacionRepositorio : INotificacionRepositorio
{
    private readonly ContextoBaseDatos _contexto;

    public NotificacionRepositorio(ContextoBaseDatos contexto)
    {
        _contexto = contexto;
    }

    public async Task AgregarAsync(Notificacion notificacion, CancellationToken cancellationToken = default)
    {
        await _contexto.Notificaciones.AddAsync(notificacion, cancellationToken);
    }

    public async Task<IReadOnlyList<Notificacion>> ObtenerPorUsuarioAsync(
        int usuarioId,
        bool soloNoLeidas,
        int limite,
        CancellationToken cancellationToken = default)
    {
        var consulta = _contexto.Notificaciones
            .AsNoTracking()
            .Include(notificacion => notificacion.Solicitud)
            .Where(notificacion => notificacion.DestinatarioId == usuarioId);

        if (soloNoLeidas)
        {
            consulta = consulta.Where(notificacion => !notificacion.Leida);
        }

        return await consulta
            .OrderByDescending(notificacion => notificacion.Fecha)
            .Take(limite)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> MarcarComoLeidaAsync(
        int notificacionId,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var notificacion = await _contexto.Notificaciones.FirstOrDefaultAsync(
            item => item.Id == notificacionId && item.DestinatarioId == usuarioId,
            cancellationToken);

        if (notificacion is null)
        {
            return false;
        }

        notificacion.Leida = true;
        return true;
    }
}
