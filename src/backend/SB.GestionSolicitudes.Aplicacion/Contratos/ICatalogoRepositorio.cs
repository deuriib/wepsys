using SB.GestionSolicitudes.Dominio.Catalogos;

namespace SB.GestionSolicitudes.Aplicacion.Contratos;

/// <summary>
/// Read/write contract for the administrative catalogs (areas and request types).
/// </summary>
public interface ICatalogoRepositorio
{
    Task<IReadOnlyList<Area>> ObtenerAreasAsync(bool soloActivas, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TipoSolicitud>> ObtenerTiposSolicitudAsync(bool soloActivos, CancellationToken cancellationToken = default);

    Task<bool> ExisteAreaAsync(int areaId, CancellationToken cancellationToken = default);

    Task<bool> ExisteTipoSolicitudAsync(int tipoSolicitudId, CancellationToken cancellationToken = default);

    Task AgregarAreaAsync(Area area, CancellationToken cancellationToken = default);

    Task AgregarTipoSolicitudAsync(TipoSolicitud tipoSolicitud, CancellationToken cancellationToken = default);

    Task<Area?> ObtenerAreaPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<TipoSolicitud?> ObtenerTipoSolicitudPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> ExisteNombreAreaAsync(string nombre, int? excluirId, CancellationToken cancellationToken = default);

    Task<bool> ExisteNombreTipoSolicitudAsync(string nombre, int? excluirId, CancellationToken cancellationToken = default);

    Task<bool> AreaTieneSolicitudesAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> TipoSolicitudTieneSolicitudesAsync(int id, CancellationToken cancellationToken = default);

    void EliminarArea(Area area);

    void EliminarTipoSolicitud(TipoSolicitud tipoSolicitud);
}
