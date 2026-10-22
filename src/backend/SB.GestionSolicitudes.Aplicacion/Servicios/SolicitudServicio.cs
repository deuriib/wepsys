using SB.GestionSolicitudes.Aplicacion.Comun;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Aplicacion.Dtos;
using SB.GestionSolicitudes.Dominio.Excepciones;
using SB.GestionSolicitudes.Dominio.Solicitudes;
using SB.GestionSolicitudes.Dominio.Usuarios;

namespace SB.GestionSolicitudes.Aplicacion.Servicios;

/// <summary>
/// Use cases of the request workflow. It orchestrates domain rules, persistence
/// and notifications; it contains no HTTP and no SQL.
/// </summary>
public sealed class SolicitudServicio
{
    private const int MAXIMO_INTENTOS_CODIGO = 3;

    private readonly ISolicitudRepositorio _solicitudes;
    private readonly ICatalogoRepositorio _catalogos;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly INotificadorSolicitud _notificador;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IReloj _reloj;

    public SolicitudServicio(
        ISolicitudRepositorio solicitudes,
        ICatalogoRepositorio catalogos,
        IUsuarioRepositorio usuarios,
        IUnidadDeTrabajo unidadDeTrabajo,
        INotificadorSolicitud notificador,
        IUsuarioActual usuarioActual,
        IReloj reloj)
    {
        _solicitudes = solicitudes;
        _catalogos = catalogos;
        _usuarios = usuarios;
        _unidadDeTrabajo = unidadDeTrabajo;
        _notificador = notificador;
        _usuarioActual = usuarioActual;
        _reloj = reloj;
    }

    public async Task<Resultado<ResultadoPaginado<SolicitudResumenDto>>> ConsultarAsync(
        FiltroSolicitudConsulta filtro,
        ParametrosPaginacion paginacion,
        CancellationToken cancellationToken = default)
    {
        var alcance = AutorizacionSolicitud.ResolverAlcance(_usuarioActual.Rol);
        var pagina = await _solicitudes.ConsultarAsync(
            filtro,
            alcance,
            _usuarioActual.Id,
            paginacion,
            cancellationToken);

        var ahora = _reloj.Ahora;
        var elementos = pagina.Elementos
            .Select(solicitud => MapeoSolicitud.Resumen(solicitud, ahora))
            .ToList();

        return Resultado.Ok(new ResultadoPaginado<SolicitudResumenDto>(
            elementos,
            pagina.TotalRegistros,
            pagina.Pagina,
            pagina.TamanoPagina));
    }

    public async Task<Resultado<SolicitudDetalleDto>> ObtenerAsync(int id, CancellationToken cancellationToken = default)
    {
        var solicitud = await _solicitudes.ObtenerDetalleAsync(id, cancellationToken);

        if (solicitud is null)
        {
            return Resultado.Falla<SolicitudDetalleDto>(
                ErrorAplicacion.NoEncontrado($"No existe la solicitud {id}."));
        }

        if (!AutorizacionSolicitud.PuedeVer(_usuarioActual, solicitud))
        {
            return Resultado.Falla<SolicitudDetalleDto>(
                ErrorAplicacion.Prohibido("No tiene permisos para consultar esta solicitud."));
        }

        var incluirInternos = _usuarioActual.Rol != RolUsuario.Solicitante;
        return Resultado.Ok(MapeoSolicitud.Detalle(solicitud, _reloj.Ahora, incluirInternos));
    }

    public async Task<Resultado<SolicitudDetalleDto>> CrearAsync(
        CrearSolicitudRequest peticion,
        CancellationToken cancellationToken = default)
    {
        var areaId = peticion.AreaId!.Value;
        var tipoSolicitudId = peticion.TipoSolicitudId!.Value;

        if (!await _catalogos.ExisteAreaAsync(areaId, cancellationToken))
        {
            return Resultado.Falla<SolicitudDetalleDto>(
                ErrorAplicacion.Validacion("El área indicada no existe o está inactiva.", "AREA_INVALIDA"));
        }

        if (!await _catalogos.ExisteTipoSolicitudAsync(tipoSolicitudId, cancellationToken))
        {
            return Resultado.Falla<SolicitudDetalleDto>(
                ErrorAplicacion.Validacion("El tipo de solicitud indicado no existe o está inactivo.", "TIPO_INVALIDO"));
        }

        var ahora = _reloj.Ahora;

        try
        {
            var solicitud = await ConstruirConCodigoUnicoAsync(peticion, areaId, tipoSolicitudId, ahora, cancellationToken);

            await _solicitudes.AgregarAsync(solicitud, cancellationToken);
            await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

            await NotificarAsync(
                new EventoNotificacionSolicitud(
                    TipoEventoNotificacion.SolicitudCreada,
                    solicitud.Id,
                    solicitud.Codigo,
                    solicitud.Titulo,
                    "Su solicitud fue registrada y está pendiente de análisis.",
                    new[] { solicitud.SolicitanteId }),
                cancellationToken);

            var creada = await _solicitudes.ObtenerDetalleAsync(solicitud.Id, cancellationToken);
            return Resultado.Ok(MapeoSolicitud.Detalle(creada ?? solicitud, _reloj.Ahora, true));
        }
        catch (ExcepcionReglaNegocio excepcion)
        {
            return Resultado.Falla<SolicitudDetalleDto>(ErrorAplicacion.Validacion(excepcion.Message));
        }
    }

    public async Task<Resultado<SolicitudDetalleDto>> ActualizarAsync(
        int id,
        ActualizarSolicitudRequest peticion,
        CancellationToken cancellationToken = default)
    {
        var (solicitud, error) = await ObtenerParaGestionAsync(id, cancellationToken);
        if (solicitud is null)
        {
            return Resultado.Falla<SolicitudDetalleDto>(error!);
        }

        if (peticion.TipoSolicitudId is not null &&
            !await _catalogos.ExisteTipoSolicitudAsync(peticion.TipoSolicitudId.Value, cancellationToken))
        {
            return Resultado.Falla<SolicitudDetalleDto>(
                ErrorAplicacion.Validacion("El tipo de solicitud indicado no existe o está inactivo.", "TIPO_INVALIDO"));
        }

        try
        {
            solicitud.ActualizarParcial(
                peticion.Titulo,
                peticion.Descripcion,
                peticion.Prioridad,
                peticion.FechaCompromiso,
                peticion.UrlEvidencia,
                peticion.TipoSolicitudId);

            await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
            var actualizada = await _solicitudes.ObtenerDetalleAsync(id, cancellationToken);

            return Resultado.Ok(MapeoSolicitud.Detalle(actualizada ?? solicitud, _reloj.Ahora, true));
        }
        catch (ExcepcionReglaNegocio excepcion)
        {
            // Editing a closed request is a state conflict, not malformed input.
            return Resultado.Falla<SolicitudDetalleDto>(ErrorAplicacion.Conflicto(excepcion.Message));
        }
    }

    public async Task<Resultado<SolicitudDetalleDto>> CambiarEstadoAsync(
        int id,
        CambiarEstadoRequest peticion,
        CancellationToken cancellationToken = default)
    {
        if (!AutorizacionSolicitud.PuedeAsignar(_usuarioActual))
        {
            return Resultado.Falla<SolicitudDetalleDto>(
                ErrorAplicacion.Prohibido("Solo un administrador o un analista puede cambiar el estado de una solicitud."));
        }

        var (solicitud, error) = await ObtenerParaGestionAsync(id, cancellationToken);
        if (solicitud is null)
        {
            return Resultado.Falla<SolicitudDetalleDto>(error!);
        }

        try
        {
            var estadoAnterior = solicitud.Estado;
            var nuevoEstado = peticion.Estado!.Value;

            solicitud.CambiarEstado(
                nuevoEstado,
                _usuarioActual.Id,
                _usuarioActual.Rol,
                peticion.Comentario,
                _reloj.Ahora);

            await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

            var tipoEvento = nuevoEstado == EstadoSolicitud.Cerrada
                ? TipoEventoNotificacion.SolicitudCerrada
                : TipoEventoNotificacion.SolicitudCambioEstado;

            await NotificarAsync(
                new EventoNotificacionSolicitud(
                    tipoEvento,
                    solicitud.Id,
                    solicitud.Codigo,
                    solicitud.Titulo,
                    $"La solicitud pasó de {Descripciones.DeEstado(estadoAnterior)} a {Descripciones.DeEstado(nuevoEstado)}.",
                    DestinatariosDe(solicitud)),
                cancellationToken);

            var actualizada = await _solicitudes.ObtenerDetalleAsync(id, cancellationToken);
            return Resultado.Ok(MapeoSolicitud.Detalle(actualizada ?? solicitud, _reloj.Ahora, true));
        }
        catch (ExcepcionReglaNegocio excepcion)
        {
            return Resultado.Falla<SolicitudDetalleDto>(ErrorAplicacion.Conflicto(excepcion.Message));
        }
    }

    public async Task<Resultado<SolicitudDetalleDto>> AsignarAsync(
        int id,
        AsignarResponsableRequest peticion,
        CancellationToken cancellationToken = default)
    {
        if (!AutorizacionSolicitud.PuedeAsignar(_usuarioActual))
        {
            return Resultado.Falla<SolicitudDetalleDto>(
                ErrorAplicacion.Prohibido("Solo un administrador o un analista puede asignar responsables."));
        }

        var responsableId = peticion.ResponsableId!.Value;

        if (_usuarioActual.Rol == RolUsuario.Analista && responsableId != _usuarioActual.Id)
        {
            return Resultado.Falla<SolicitudDetalleDto>(
                ErrorAplicacion.Prohibido("Un analista solo puede asignarse solicitudes a sí mismo."));
        }

        var responsable = await _usuarios.ObtenerPorIdAsync(responsableId, cancellationToken);
        if (responsable is null || !responsable.Activo || !responsable.TieneRolOperativo())
        {
            return Resultado.Falla<SolicitudDetalleDto>(
                ErrorAplicacion.Validacion("El responsable indicado no existe, está inactivo o no tiene perfil técnico.", "RESPONSABLE_INVALIDO"));
        }

        var (solicitud, error) = await ObtenerParaGestionAsync(id, cancellationToken);
        if (solicitud is null)
        {
            return Resultado.Falla<SolicitudDetalleDto>(error!);
        }

        try
        {
            solicitud.AsignarResponsable(responsableId, _usuarioActual.Id, _reloj.Ahora);
            await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

            await NotificarAsync(
                new EventoNotificacionSolicitud(
                    TipoEventoNotificacion.SolicitudAsignada,
                    solicitud.Id,
                    solicitud.Codigo,
                    solicitud.Titulo,
                    $"La solicitud fue asignada a {responsable.Nombre}.",
                    DestinatariosDe(solicitud)),
                cancellationToken);

            var actualizada = await _solicitudes.ObtenerDetalleAsync(id, cancellationToken);
            return Resultado.Ok(MapeoSolicitud.Detalle(actualizada ?? solicitud, _reloj.Ahora, true));
        }
        catch (ExcepcionReglaNegocio excepcion)
        {
            return Resultado.Falla<SolicitudDetalleDto>(ErrorAplicacion.Conflicto(excepcion.Message));
        }
    }

    public async Task<Resultado<SolicitudDetalleDto>> AgregarComentarioAsync(
        int id,
        CrearComentarioRequest peticion,
        CancellationToken cancellationToken = default)
    {
        var solicitud = await _solicitudes.ObtenerDetalleAsync(id, cancellationToken);

        if (solicitud is null)
        {
            return Resultado.Falla<SolicitudDetalleDto>(
                ErrorAplicacion.NoEncontrado($"No existe la solicitud {id}."));
        }

        if (!AutorizacionSolicitud.PuedeVer(_usuarioActual, solicitud))
        {
            return Resultado.Falla<SolicitudDetalleDto>(
                ErrorAplicacion.Prohibido("No tiene permisos para comentar esta solicitud."));
        }

        // A requester can never write an internal note, even if the client sends one.
        var visibilidad = _usuarioActual.Rol == RolUsuario.Solicitante
            ? VisibilidadComentario.Publica
            : peticion.Visibilidad;

        try
        {
            solicitud.AgregarComentario(peticion.Texto, visibilidad, _usuarioActual.Id, _reloj.Ahora);
            await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

            var destinatarios = new List<int>();
            if (solicitud.SolicitanteId != _usuarioActual.Id)
            {
                destinatarios.Add(solicitud.SolicitanteId);
            }

            if (solicitud.ResponsableId is not null && solicitud.ResponsableId != _usuarioActual.Id)
            {
                destinatarios.Add(solicitud.ResponsableId.Value);
            }

            if (destinatarios.Count > 0)
            {
                await NotificarAsync(
                    new EventoNotificacionSolicitud(
                        TipoEventoNotificacion.ComentarioAgregado,
                        solicitud.Id,
                        solicitud.Codigo,
                        solicitud.Titulo,
                        "Se agregó un nuevo comentario a la solicitud.",
                        destinatarios),
                    cancellationToken);
            }

            var actualizada = await _solicitudes.ObtenerDetalleAsync(id, cancellationToken);
            var incluirInternos = _usuarioActual.Rol != RolUsuario.Solicitante;
            return Resultado.Ok(MapeoSolicitud.Detalle(actualizada ?? solicitud, _reloj.Ahora, incluirInternos));
        }
        catch (ExcepcionReglaNegocio excepcion)
        {
            return Resultado.Falla<SolicitudDetalleDto>(ErrorAplicacion.Validacion(excepcion.Message));
        }
    }

    private async Task<Solicitud> ConstruirConCodigoUnicoAsync(
        CrearSolicitudRequest peticion,
        int areaId,
        int tipoSolicitudId,
        DateTime ahora,
        CancellationToken cancellationToken)
    {
        var secuenciaBase = await _solicitudes.ObtenerSiguienteSecuenciaAsync(ahora.Year, cancellationToken);

        for (var intento = 0; intento < MAXIMO_INTENTOS_CODIGO; intento++)
        {
            var secuencia = secuenciaBase + intento;
            var codigo = Solicitud.ConstruirCodigo(ahora.Year, secuencia);

            if (await _solicitudes.ExisteCodigoAsync(codigo, cancellationToken))
            {
                continue;
            }

            return Solicitud.Registrar(
                codigo,
                peticion.Titulo,
                peticion.Descripcion,
                peticion.Prioridad!.Value,
                areaId,
                tipoSolicitudId,
                _usuarioActual.Id,
                peticion.FechaCompromiso!.Value,
                ahora,
                peticion.UrlEvidencia,
                secuencia);
        }

        throw new ExcepcionReglaNegocio("No fue posible generar un código único para la solicitud. Intente nuevamente.");
    }

    private async Task<(Solicitud? Solicitud, ErrorAplicacion? Error)> ObtenerParaGestionAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var solicitud = await _solicitudes.ObtenerDetalleAsync(id, cancellationToken);

        if (solicitud is null)
        {
            return (null, ErrorAplicacion.NoEncontrado($"No existe la solicitud {id}."));
        }

        if (!AutorizacionSolicitud.PuedeGestionar(_usuarioActual, solicitud))
        {
            return (null, ErrorAplicacion.Prohibido("No tiene permisos para gestionar esta solicitud."));
        }

        return (solicitud, null);
    }

    private static IReadOnlyList<int> DestinatariosDe(Solicitud solicitud)
    {
        var destinatarios = new List<int> { solicitud.SolicitanteId };

        if (solicitud.ResponsableId is not null && solicitud.ResponsableId != solicitud.SolicitanteId)
        {
            destinatarios.Add(solicitud.ResponsableId.Value);
        }

        return destinatarios;
    }

    private Task NotificarAsync(EventoNotificacionSolicitud evento, CancellationToken cancellationToken) =>
        _notificador.NotificarAsync(evento, cancellationToken);
}
