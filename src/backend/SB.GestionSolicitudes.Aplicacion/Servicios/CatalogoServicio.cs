using SB.GestionSolicitudes.Aplicacion.Comun;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Aplicacion.Dtos;
using SB.GestionSolicitudes.Dominio.Catalogos;

namespace SB.GestionSolicitudes.Aplicacion.Servicios;

/// <summary>
/// Administrative catalogs. Read endpoints are available to every authenticated
/// user because the forms need them; writes are restricted to administrators by
/// the authorization policy at the edge and re-checked here.
/// </summary>
public sealed class CatalogoServicio
{
    private readonly ICatalogoRepositorio _catalogos;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IUsuarioActual _usuarioActual;

    public CatalogoServicio(
        ICatalogoRepositorio catalogos,
        IUnidadDeTrabajo unidadDeTrabajo,
        IUsuarioActual usuarioActual)
    {
        _catalogos = catalogos;
        _unidadDeTrabajo = unidadDeTrabajo;
        _usuarioActual = usuarioActual;
    }

    public async Task<IReadOnlyList<AreaDto>> ObtenerAreasAsync(
        bool soloActivas = true,
        CancellationToken cancellationToken = default)
    {
        var areas = await _catalogos.ObtenerAreasAsync(soloActivas, cancellationToken);
        return areas.Select(area => new AreaDto(area.Id, area.Nombre, area.Activa)).ToList();
    }

    public async Task<IReadOnlyList<TipoSolicitudDto>> ObtenerTiposSolicitudAsync(
        bool soloActivos = true,
        CancellationToken cancellationToken = default)
    {
        var tipos = await _catalogos.ObtenerTiposSolicitudAsync(soloActivos, cancellationToken);
        return tipos
            .Select(tipo => new TipoSolicitudDto(tipo.Id, tipo.Nombre, tipo.Descripcion, tipo.Activo))
            .ToList();
    }

    public async Task<Resultado<AreaDto>> CrearAreaAsync(
        CrearAreaRequest peticion,
        CancellationToken cancellationToken = default)
    {
        if (_usuarioActual.Rol != Dominio.Usuarios.RolUsuario.Administrador)
        {
            return Resultado.Falla<AreaDto>(ErrorAplicacion.Prohibido("Solo un administrador puede crear áreas."));
        }

        var nombre = peticion.Nombre.Trim();
        if (await _catalogos.ExisteNombreAreaAsync(nombre, null, cancellationToken))
        {
            return Resultado.Falla<AreaDto>(ErrorAplicacion.Conflicto("Ya existe un área con ese nombre.", "AREA_DUPLICADA"));
        }

        var area = new Area { Nombre = nombre, Activa = true };

        await _catalogos.AgregarAreaAsync(area, cancellationToken);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        return Resultado.Ok(new AreaDto(area.Id, area.Nombre, area.Activa));
    }

    public async Task<Resultado<AreaDto>> ActualizarAreaAsync(
        int id,
        ActualizarAreaRequest peticion,
        CancellationToken cancellationToken = default)
    {
        if (_usuarioActual.Rol != Dominio.Usuarios.RolUsuario.Administrador)
        {
            return Resultado.Falla<AreaDto>(ErrorAplicacion.Prohibido("Solo un administrador puede modificar áreas."));
        }

        var area = await _catalogos.ObtenerAreaPorIdAsync(id, cancellationToken);
        if (area is null)
        {
            return Resultado.Falla<AreaDto>(ErrorAplicacion.NoEncontrado($"No existe el área {id}."));
        }

        var nombre = peticion.Nombre.Trim();
        if (await _catalogos.ExisteNombreAreaAsync(nombre, id, cancellationToken))
        {
            return Resultado.Falla<AreaDto>(ErrorAplicacion.Conflicto("Ya existe un área con ese nombre.", "AREA_DUPLICADA"));
        }

        area.Nombre = nombre;
        if (peticion.Activa.HasValue)
        {
            area.Activa = peticion.Activa.Value;
        }

        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        return Resultado.Ok(new AreaDto(area.Id, area.Nombre, area.Activa));
    }

    public async Task<Resultado> EliminarAreaAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (_usuarioActual.Rol != Dominio.Usuarios.RolUsuario.Administrador)
        {
            return Resultado.Falla(ErrorAplicacion.Prohibido("Solo un administrador puede eliminar áreas."));
        }

        var area = await _catalogos.ObtenerAreaPorIdAsync(id, cancellationToken);
        if (area is null)
        {
            return Resultado.Falla(ErrorAplicacion.NoEncontrado($"No existe el área {id}."));
        }

        if (await _catalogos.AreaTieneSolicitudesAsync(id, cancellationToken))
        {
            return Resultado.Falla(ErrorAplicacion.Conflicto("No se puede eliminar el área porque está asociada a una o más solicitudes.", "AREA_EN_USO"));
        }

        _catalogos.EliminarArea(area);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        return Resultado.Ok();
    }

    public async Task<Resultado<TipoSolicitudDto>> CrearTipoSolicitudAsync(
        CrearTipoSolicitudRequest peticion,
        CancellationToken cancellationToken = default)
    {
        if (_usuarioActual.Rol != Dominio.Usuarios.RolUsuario.Administrador)
        {
            return Resultado.Falla<TipoSolicitudDto>(ErrorAplicacion.Prohibido("Solo un administrador puede crear tipos de solicitud."));
        }

        var nombre = peticion.Nombre.Trim();
        if (await _catalogos.ExisteNombreTipoSolicitudAsync(nombre, null, cancellationToken))
        {
            return Resultado.Falla<TipoSolicitudDto>(ErrorAplicacion.Conflicto("Ya existe un tipo de solicitud con ese nombre.", "TIPO_SOLICITUD_DUPLICADO"));
        }

        var tipo = new TipoSolicitud
        {
            Nombre = nombre,
            Descripcion = peticion.Descripcion.Trim(),
            Activo = true
        };

        await _catalogos.AgregarTipoSolicitudAsync(tipo, cancellationToken);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        return Resultado.Ok(new TipoSolicitudDto(tipo.Id, tipo.Nombre, tipo.Descripcion, tipo.Activo));
    }

    public async Task<Resultado<TipoSolicitudDto>> ActualizarTipoSolicitudAsync(
        int id,
        ActualizarTipoSolicitudRequest peticion,
        CancellationToken cancellationToken = default)
    {
        if (_usuarioActual.Rol != Dominio.Usuarios.RolUsuario.Administrador)
        {
            return Resultado.Falla<TipoSolicitudDto>(ErrorAplicacion.Prohibido("Solo un administrador puede modificar tipos de solicitud."));
        }

        var tipo = await _catalogos.ObtenerTipoSolicitudPorIdAsync(id, cancellationToken);
        if (tipo is null)
        {
            return Resultado.Falla<TipoSolicitudDto>(ErrorAplicacion.NoEncontrado($"No existe el tipo de solicitud {id}."));
        }

        var nombre = peticion.Nombre.Trim();
        if (await _catalogos.ExisteNombreTipoSolicitudAsync(nombre, id, cancellationToken))
        {
            return Resultado.Falla<TipoSolicitudDto>(ErrorAplicacion.Conflicto("Ya existe un tipo de solicitud con ese nombre.", "TIPO_SOLICITUD_DUPLICADO"));
        }

        tipo.Nombre = nombre;
        if (peticion.Descripcion is not null)
        {
            tipo.Descripcion = peticion.Descripcion.Trim();
        }

        if (peticion.Activo.HasValue)
        {
            tipo.Activo = peticion.Activo.Value;
        }

        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        return Resultado.Ok(new TipoSolicitudDto(tipo.Id, tipo.Nombre, tipo.Descripcion, tipo.Activo));
    }

    public async Task<Resultado> EliminarTipoSolicitudAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (_usuarioActual.Rol != Dominio.Usuarios.RolUsuario.Administrador)
        {
            return Resultado.Falla(ErrorAplicacion.Prohibido("Solo un administrador puede eliminar tipos de solicitud."));
        }

        var tipo = await _catalogos.ObtenerTipoSolicitudPorIdAsync(id, cancellationToken);
        if (tipo is null)
        {
            return Resultado.Falla(ErrorAplicacion.NoEncontrado($"No existe el tipo de solicitud {id}."));
        }

        if (await _catalogos.TipoSolicitudTieneSolicitudesAsync(id, cancellationToken))
        {
            return Resultado.Falla(ErrorAplicacion.Conflicto("No se puede eliminar el tipo de solicitud porque está asociado a una o más solicitudes.", "TIPO_SOLICITUD_EN_USO"));
        }

        _catalogos.EliminarTipoSolicitud(tipo);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        return Resultado.Ok();
    }
}
