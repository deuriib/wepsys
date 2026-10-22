using SB.GestionSolicitudes.Aplicacion.Comun;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Aplicacion.Dtos;

namespace SB.GestionSolicitudes.Aplicacion.Servicios;

/// <summary>
/// Builds the dashboard payload. Aggregates are computed by the repository in a
/// single query and the recent list is scoped by the caller's role.
/// </summary>
public sealed class TableroServicio
{
    private const int CANTIDAD_ULTIMAS_SOLICITUDES = 8;

    private readonly ISolicitudRepositorio _solicitudes;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IReloj _reloj;

    public TableroServicio(
        ISolicitudRepositorio solicitudes,
        IUsuarioActual usuarioActual,
        IReloj reloj)
    {
        _solicitudes = solicitudes;
        _usuarioActual = usuarioActual;
        _reloj = reloj;
    }

    public async Task<Resultado<TableroResumenDto>> ObtenerResumenAsync(CancellationToken cancellationToken = default)
    {
        var alcance = AutorizacionSolicitud.ResolverAlcance(_usuarioActual.Rol);
        var ahora = _reloj.Ahora;

        var resumen = await _solicitudes.ObtenerResumenAsync(alcance, _usuarioActual.Id, ahora, cancellationToken);
        var recientes = await _solicitudes.ObtenerRecientesAsync(
            alcance,
            _usuarioActual.Id,
            CANTIDAD_ULTIMAS_SOLICITUDES,
            cancellationToken);

        return Resultado.Ok(new TableroResumenDto(
            resumen.Total,
            resumen.Abiertas,
            resumen.Cerradas,
            resumen.Vencidas,
            resumen.PorEstado,
            resumen.PorPrioridad,
            recientes.Select(solicitud => MapeoSolicitud.Resumen(solicitud, ahora)).ToList()));
    }
}
