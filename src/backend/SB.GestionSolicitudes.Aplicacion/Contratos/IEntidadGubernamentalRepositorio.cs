using SB.GestionSolicitudes.Aplicacion.Comun;
using SB.GestionSolicitudes.Aplicacion.Dtos;
using SB.GestionSolicitudes.Dominio.EntidadesGubernamentales;

namespace SB.GestionSolicitudes.Aplicacion.Contratos;

/// <summary>
/// Contract for the plain-text persistence of the government entities catalog.
/// The implementation writes a JSON document inside the repository, as required
/// by the first specification, while the API still exposes a regular CRUD.
/// </summary>
public interface IEntidadGubernamentalRepositorio
{
    Task<ResultadoPaginado<EntidadGubernamental>> ConsultarAsync(
        FiltroEntidadGubernamental filtro,
        ParametrosPaginacion paginacion,
        CancellationToken cancellationToken = default);

    Task<EntidadGubernamental?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId, CancellationToken cancellationToken = default);

    Task AgregarAsync(EntidadGubernamental entidad, CancellationToken cancellationToken = default);

    Task ActualizarAsync(EntidadGubernamental entidad, CancellationToken cancellationToken = default);

    Task<bool> EliminarAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> ContarAsync(CancellationToken cancellationToken = default);

    Task AgregarRangoAsync(IEnumerable<EntidadGubernamental> entidades, CancellationToken cancellationToken = default);

    Task<OpcionesFiltroEntidadesDto> ObtenerOpcionesFiltroAsync(CancellationToken cancellationToken = default);
}
