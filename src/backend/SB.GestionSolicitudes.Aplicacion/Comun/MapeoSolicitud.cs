using SB.GestionSolicitudes.Aplicacion.Dtos;
using SB.GestionSolicitudes.Dominio.Solicitudes;

namespace SB.GestionSolicitudes.Aplicacion.Comun;

/// <summary>
/// Explicit mapping between the domain aggregate and the API contracts.
/// Hand-written on purpose: it is cheaper to read than a mapping profile and it
/// fails at compile time when a contract changes.
/// </summary>
public static class MapeoSolicitud
{
    public static SolicitudResumenDto Resumen(Solicitud solicitud, DateTime fechaReferencia) =>
        new(
            solicitud.Id,
            solicitud.Codigo,
            solicitud.Titulo,
            solicitud.Estado,
            Descripciones.DeEstado(solicitud.Estado),
            solicitud.Prioridad,
            Descripciones.DePrioridad(solicitud.Prioridad),
            solicitud.Area?.Nombre ?? string.Empty,
            solicitud.TipoSolicitud?.Nombre ?? string.Empty,
            solicitud.Solicitante?.Nombre ?? string.Empty,
            solicitud.Responsable?.Nombre,
            solicitud.FechaCreacion,
            solicitud.FechaCompromiso,
            solicitud.EstaVencida(fechaReferencia));

    public static SolicitudDetalleDto Detalle(
        Solicitud solicitud,
        DateTime fechaReferencia,
        bool incluirComentariosInternos)
    {
        var comentarios = solicitud.Comentarios
            .Where(comentario => incluirComentariosInternos || comentario.Visibilidad == VisibilidadComentario.Publica)
            .OrderBy(comentario => comentario.Fecha)
            .Select(comentario => new ComentarioDto(
                comentario.Id,
                comentario.Texto,
                comentario.Visibilidad,
                comentario.Fecha,
                comentario.Usuario?.Nombre ?? string.Empty))
            .ToList();

        var historial = solicitud.Historial
            .OrderBy(registro => registro.Fecha)
            .Select(registro => new HistorialEstadoDto(
                registro.Id,
                registro.EstadoAnterior,
                registro.EstadoAnterior is null ? null : Descripciones.DeEstado(registro.EstadoAnterior.Value),
                registro.EstadoNuevo,
                Descripciones.DeEstado(registro.EstadoNuevo),
                registro.Fecha,
                registro.Usuario?.Nombre ?? string.Empty,
                registro.Comentario))
            .ToList();

        var estadosPermitidos = FlujoEstadoSolicitud
            .ObtenerDestinosPermitidos(solicitud.Estado)
            .Select(estado => new EstadoPermitidoDto(estado, Descripciones.DeEstado(estado)))
            .ToList();

        return new SolicitudDetalleDto(
            solicitud.Id,
            solicitud.Codigo,
            solicitud.Titulo,
            solicitud.Descripcion,
            solicitud.Estado,
            Descripciones.DeEstado(solicitud.Estado),
            solicitud.Prioridad,
            Descripciones.DePrioridad(solicitud.Prioridad),
            solicitud.AreaId,
            solicitud.Area?.Nombre ?? string.Empty,
            solicitud.TipoSolicitudId,
            solicitud.TipoSolicitud?.Nombre ?? string.Empty,
            solicitud.SolicitanteId,
            solicitud.Solicitante?.Nombre ?? string.Empty,
            solicitud.ResponsableId,
            solicitud.Responsable?.Nombre,
            solicitud.FechaCreacion,
            solicitud.FechaCompromiso,
            solicitud.FechaCierre,
            solicitud.UrlEvidencia,
            solicitud.EstaVencida(fechaReferencia),
            estadosPermitidos,
            historial,
            comentarios);
    }
}
