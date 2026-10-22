using Microsoft.EntityFrameworkCore;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Dominio.Catalogos;

namespace SB.GestionSolicitudes.Infraestructura.Persistencia.Repositorios;

/// <summary>
/// Catalog repository. Lookups used by validation only hit indexed columns.
/// </summary>
public sealed class CatalogoRepositorio : ICatalogoRepositorio
{
    private readonly ContextoBaseDatos _contexto;

    public CatalogoRepositorio(ContextoBaseDatos contexto)
    {
        _contexto = contexto;
    }

    public async Task<IReadOnlyList<Area>> ObtenerAreasAsync(
        bool soloActivas,
        CancellationToken cancellationToken = default)
    {
        var consulta = _contexto.Areas.AsNoTracking();

        if (soloActivas)
        {
            consulta = consulta.Where(area => area.Activa);
        }

        return await consulta.OrderBy(area => area.Nombre).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TipoSolicitud>> ObtenerTiposSolicitudAsync(
        bool soloActivos,
        CancellationToken cancellationToken = default)
    {
        var consulta = _contexto.TiposSolicitud.AsNoTracking();

        if (soloActivos)
        {
            consulta = consulta.Where(tipo => tipo.Activo);
        }

        return await consulta.OrderBy(tipo => tipo.Nombre).ToListAsync(cancellationToken);
    }

    public Task<bool> ExisteAreaAsync(int areaId, CancellationToken cancellationToken = default) =>
        _contexto.Areas.AnyAsync(area => area.Id == areaId && area.Activa, cancellationToken);

    public Task<bool> ExisteTipoSolicitudAsync(int tipoSolicitudId, CancellationToken cancellationToken = default) =>
        _contexto.TiposSolicitud.AnyAsync(
            tipo => tipo.Id == tipoSolicitudId && tipo.Activo,
            cancellationToken);

    public async Task AgregarAreaAsync(Area area, CancellationToken cancellationToken = default)
    {
        await _contexto.Areas.AddAsync(area, cancellationToken);
    }

    public async Task AgregarTipoSolicitudAsync(
        TipoSolicitud tipoSolicitud,
        CancellationToken cancellationToken = default)
    {
        await _contexto.TiposSolicitud.AddAsync(tipoSolicitud, cancellationToken);
    }
}
