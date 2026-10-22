using Microsoft.EntityFrameworkCore;
using SB.GestionSolicitudes.Aplicacion.Comun;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Aplicacion.Dtos;
using SB.GestionSolicitudes.Dominio.Solicitudes;

namespace SB.GestionSolicitudes.Infraestructura.Persistencia.Repositorios;

/// <summary>
/// EF Core implementation of the request repository. Filtering, sorting and
/// paging are translated to SQL, so the database does the work instead of the
/// application.
/// </summary>
public sealed class SolicitudRepositorio : ISolicitudRepositorio
{
    private readonly ContextoBaseDatos _contexto;

    public SolicitudRepositorio(ContextoBaseDatos contexto)
    {
        _contexto = contexto;
    }

    public Task<Solicitud?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        _contexto.Solicitudes.FirstOrDefaultAsync(solicitud => solicitud.Id == id, cancellationToken);

    public Task<Solicitud?> ObtenerDetalleAsync(int id, CancellationToken cancellationToken = default) =>
        _contexto.Solicitudes
            .Include(solicitud => solicitud.Area)
            .Include(solicitud => solicitud.TipoSolicitud)
            .Include(solicitud => solicitud.Solicitante)
            .Include(solicitud => solicitud.Responsable)
            .Include(solicitud => solicitud.Historial)
                .ThenInclude(historial => historial.Usuario)
            .Include(solicitud => solicitud.Comentarios)
                .ThenInclude(comentario => comentario.Usuario)
            .AsSplitQuery()
            .FirstOrDefaultAsync(solicitud => solicitud.Id == id, cancellationToken);

    public async Task<ResultadoPaginado<Solicitud>> ConsultarAsync(
        FiltroSolicitudConsulta filtro,
        AlcanceSolicitud alcance,
        int usuarioId,
        ParametrosPaginacion paginacion,
        CancellationToken cancellationToken = default)
    {
        var fechaReferencia = DateTime.UtcNow;
        var consulta = AplicarFiltros(
            AplicarAlcance(_contexto.Solicitudes.AsNoTracking(), alcance, usuarioId),
            filtro,
            fechaReferencia);

        var totalRegistros = await consulta.CountAsync(cancellationToken);

        if (totalRegistros == 0)
        {
            return ResultadoPaginado<Solicitud>.Vacio(paginacion);
        }

        var elementos = await consulta
            .Include(solicitud => solicitud.Area)
            .Include(solicitud => solicitud.TipoSolicitud)
            .Include(solicitud => solicitud.Solicitante)
            .Include(solicitud => solicitud.Responsable)
            .OrderByDescending(solicitud => solicitud.FechaCreacion)
            .ThenByDescending(solicitud => solicitud.Id)
            .Skip(paginacion.Omitir)
            .Take(paginacion.TamanoNormalizado)
            .ToListAsync(cancellationToken);

        return new ResultadoPaginado<Solicitud>(
            elementos,
            totalRegistros,
            paginacion.PaginaNormalizada,
            paginacion.TamanoNormalizado);
    }

    public async Task<IReadOnlyList<Solicitud>> ObtenerRecientesAsync(
        AlcanceSolicitud alcance,
        int usuarioId,
        int cantidad,
        CancellationToken cancellationToken = default)
    {
        return await AplicarAlcance(_contexto.Solicitudes.AsNoTracking(), alcance, usuarioId)
            .Include(solicitud => solicitud.Area)
            .Include(solicitud => solicitud.TipoSolicitud)
            .Include(solicitud => solicitud.Solicitante)
            .Include(solicitud => solicitud.Responsable)
            .OrderByDescending(solicitud => solicitud.FechaCreacion)
            .ThenByDescending(solicitud => solicitud.Id)
            .Take(cantidad)
            .ToListAsync(cancellationToken);
    }

    public async Task<ResumenSolicitudes> ObtenerResumenAsync(
        AlcanceSolicitud alcance,
        int usuarioId,
        DateTime fechaReferencia,
        CancellationToken cancellationToken = default)
    {
        var consulta = AplicarAlcance(_contexto.Solicitudes.AsNoTracking(), alcance, usuarioId);

        var porEstado = await consulta
            .GroupBy(solicitud => solicitud.Estado)
            .Select(grupo => new { Estado = grupo.Key, Total = grupo.Count() })
            .ToListAsync(cancellationToken);

        var porPrioridad = await consulta
            .GroupBy(solicitud => solicitud.Prioridad)
            .Select(grupo => new { Prioridad = grupo.Key, Total = grupo.Count() })
            .ToListAsync(cancellationToken);

        var vencidas = await consulta.CountAsync(
            solicitud => solicitud.FechaCompromiso < fechaReferencia &&
                         solicitud.Estado != EstadoSolicitud.Resuelta &&
                         solicitud.Estado != EstadoSolicitud.Cerrada,
            cancellationToken);

        var conteoEstados = Enum.GetValues<EstadoSolicitud>()
            .Select(estado => new ConteoSolicitudes(
                estado.ToString(),
                porEstado.FirstOrDefault(item => item.Estado == estado)?.Total ?? 0))
            .ToList();

        var conteoPrioridades = Enum.GetValues<PrioridadSolicitud>()
            .Select(prioridad => new ConteoSolicitudes(
                prioridad.ToString(),
                porPrioridad.FirstOrDefault(item => item.Prioridad == prioridad)?.Total ?? 0))
            .ToList();

        var total = porEstado.Sum(item => item.Total);

        // "Cerrada" is the only terminal state: a request marked "Resuelta" is still
        // open until the requester confirms it. Keeping these two buckets summing to
        // the total avoids a dashboard that does not add up.
        var cerradas = porEstado.FirstOrDefault(item => item.Estado == EstadoSolicitud.Cerrada)?.Total ?? 0;

        return new ResumenSolicitudes(
            total,
            total - cerradas,
            cerradas,
            vencidas,
            conteoEstados,
            conteoPrioridades);
    }

    public async Task<int> ObtenerSiguienteSecuenciaAsync(int anio, CancellationToken cancellationToken = default)
    {
        var maximo = await _contexto.Solicitudes
            .Where(solicitud => solicitud.FechaCreacion.Year == anio)
            .Select(solicitud => (int?)solicitud.Secuencia)
            .MaxAsync(cancellationToken);

        return (maximo ?? 0) + 1;
    }

    public Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken = default) =>
        _contexto.Solicitudes.AnyAsync(solicitud => solicitud.Codigo == codigo, cancellationToken);

    public async Task AgregarAsync(Solicitud solicitud, CancellationToken cancellationToken = default)
    {
        await _contexto.Solicitudes.AddAsync(solicitud, cancellationToken);
    }

    private static IQueryable<Solicitud> AplicarAlcance(
        IQueryable<Solicitud> consulta,
        AlcanceSolicitud alcance,
        int usuarioId) => alcance switch
        {
            AlcanceSolicitud.Todos => consulta,
            AlcanceSolicitud.Propios => consulta.Where(solicitud => solicitud.SolicitanteId == usuarioId),
            AlcanceSolicitud.AsignadosODisponibles => consulta.Where(
                solicitud => solicitud.ResponsableId == null || solicitud.ResponsableId == usuarioId),
            _ => consulta.Where(_ => false)
        };

    private static IQueryable<Solicitud> AplicarFiltros(
        IQueryable<Solicitud> consulta,
        FiltroSolicitudConsulta filtro,
        DateTime fechaReferencia)
    {
        if (filtro.Estado is not null)
        {
            consulta = consulta.Where(solicitud => solicitud.Estado == filtro.Estado.Value);
        }

        if (filtro.Prioridad is not null)
        {
            consulta = consulta.Where(solicitud => solicitud.Prioridad == filtro.Prioridad.Value);
        }

        if (filtro.AreaId is not null)
        {
            consulta = consulta.Where(solicitud => solicitud.AreaId == filtro.AreaId.Value);
        }

        if (filtro.SolicitanteId is not null)
        {
            consulta = consulta.Where(solicitud => solicitud.SolicitanteId == filtro.SolicitanteId.Value);
        }

        if (filtro.ResponsableId is not null)
        {
            consulta = consulta.Where(solicitud => solicitud.ResponsableId == filtro.ResponsableId.Value);
        }

        if (filtro.FechaDesde is not null)
        {
            consulta = consulta.Where(solicitud => solicitud.FechaCreacion >= filtro.FechaDesde.Value);
        }

        if (filtro.FechaHasta is not null)
        {
            consulta = consulta.Where(solicitud => solicitud.FechaCreacion <= filtro.FechaHasta.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var patron = $"%{filtro.Texto.Trim()}%";
            consulta = consulta.Where(solicitud =>
                EF.Functions.Like(solicitud.Codigo, patron) ||
                EF.Functions.Like(solicitud.Titulo, patron) ||
                EF.Functions.Like(solicitud.Descripcion, patron));
        }

        if (filtro.SoloVencidas)
        {
            consulta = consulta.Where(solicitud =>
                solicitud.FechaCompromiso < fechaReferencia &&
                solicitud.Estado != EstadoSolicitud.Resuelta &&
                solicitud.Estado != EstadoSolicitud.Cerrada);
        }

        return consulta;
    }
}
