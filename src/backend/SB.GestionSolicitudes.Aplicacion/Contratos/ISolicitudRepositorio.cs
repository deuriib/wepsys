using SB.GestionSolicitudes.Aplicacion.Comun;
using SB.GestionSolicitudes.Aplicacion.Dtos;
using SB.GestionSolicitudes.Dominio.Solicitudes;

namespace SB.GestionSolicitudes.Aplicacion.Contratos;

/// <summary>
/// Persistence contract for the request aggregate. The application layer only
/// depends on this abstraction, which is what makes it testable without a
/// database and swappable from SQL Server to another engine.
/// </summary>
public interface ISolicitudRepositorio
{
    Task<Solicitud?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Solicitud?> ObtenerDetalleAsync(int id, CancellationToken cancellationToken = default);

    Task<ResultadoPaginado<Solicitud>> ConsultarAsync(
        FiltroSolicitudConsulta filtro,
        AlcanceSolicitud alcance,
        int usuarioId,
        ParametrosPaginacion paginacion,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Solicitud>> ObtenerRecientesAsync(
        AlcanceSolicitud alcance,
        int usuarioId,
        int cantidad,
        CancellationToken cancellationToken = default);

    Task<ResumenSolicitudes> ObtenerResumenAsync(
        AlcanceSolicitud alcance,
        int usuarioId,
        DateTime fechaReferencia,
        CancellationToken cancellationToken = default);

    Task<int> ObtenerSiguienteSecuenciaAsync(int anio, CancellationToken cancellationToken = default);

    Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken = default);

    Task AgregarAsync(Solicitud solicitud, CancellationToken cancellationToken = default);
}
