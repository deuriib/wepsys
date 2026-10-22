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
}
