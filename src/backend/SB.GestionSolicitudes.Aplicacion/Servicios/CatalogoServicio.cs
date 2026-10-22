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

    public async Task<IReadOnlyList<AreaDto>> ObtenerAreasAsync(CancellationToken cancellationToken = default)
    {
        var areas = await _catalogos.ObtenerAreasAsync(true, cancellationToken);
        return areas.Select(area => new AreaDto(area.Id, area.Nombre, area.Activa)).ToList();
    }

    public async Task<IReadOnlyList<TipoSolicitudDto>> ObtenerTiposSolicitudAsync(CancellationToken cancellationToken = default)
    {
        var tipos = await _catalogos.ObtenerTiposSolicitudAsync(true, cancellationToken);
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

        var area = new Area { Nombre = peticion.Nombre.Trim(), Activa = true };

        await _catalogos.AgregarAreaAsync(area, cancellationToken);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        return Resultado.Ok(new AreaDto(area.Id, area.Nombre, area.Activa));
    }

    public async Task<Resultado<TipoSolicitudDto>> CrearTipoSolicitudAsync(
        CrearTipoSolicitudRequest peticion,
        CancellationToken cancellationToken = default)
    {
        if (_usuarioActual.Rol != Dominio.Usuarios.RolUsuario.Administrador)
        {
            return Resultado.Falla<TipoSolicitudDto>(ErrorAplicacion.Prohibido("Solo un administrador puede crear tipos de solicitud."));
        }

        var tipo = new TipoSolicitud
        {
            Nombre = peticion.Nombre.Trim(),
            Descripcion = peticion.Descripcion.Trim(),
            Activo = true
        };

        await _catalogos.AgregarTipoSolicitudAsync(tipo, cancellationToken);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        return Resultado.Ok(new TipoSolicitudDto(tipo.Id, tipo.Nombre, tipo.Descripcion, tipo.Activo));
    }
}
