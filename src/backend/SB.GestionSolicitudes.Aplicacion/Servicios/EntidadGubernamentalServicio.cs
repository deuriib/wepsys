using SB.GestionSolicitudes.Aplicacion.Comun;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Aplicacion.Dtos;
using SB.GestionSolicitudes.Dominio.EntidadesGubernamentales;
using SB.GestionSolicitudes.Dominio.Usuarios;

namespace SB.GestionSolicitudes.Aplicacion.Servicios;

/// <summary>
/// Maintenance of the government entities catalog. The persistence is a
/// plain-text file (see <see cref="IEntidadGubernamentalRepositorio"/>), which is
/// exactly what the first specification requires for this catalog.
/// </summary>
public sealed class EntidadGubernamentalServicio
{
    private readonly IEntidadGubernamentalRepositorio _repositorio;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IReloj _reloj;

    public EntidadGubernamentalServicio(
        IEntidadGubernamentalRepositorio repositorio,
        IUsuarioActual usuarioActual,
        IReloj reloj)
    {
        _repositorio = repositorio;
        _usuarioActual = usuarioActual;
        _reloj = reloj;
    }

    public async Task<ResultadoPaginado<EntidadGubernamentalDto>> ConsultarAsync(
        FiltroEntidadGubernamental filtro,
        ParametrosPaginacion paginacion,
        CancellationToken cancellationToken = default)
    {
        var pagina = await _repositorio.ConsultarAsync(filtro, paginacion, cancellationToken);

        return new ResultadoPaginado<EntidadGubernamentalDto>(
            pagina.Elementos.Select(Mapear).ToList(),
            pagina.TotalRegistros,
            pagina.Pagina,
            pagina.TamanoPagina);
    }

    public async Task<Resultado<EntidadGubernamentalDto>> ObtenerAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entidad = await _repositorio.ObtenerPorIdAsync(id, cancellationToken);

        return entidad is null
            ? Resultado.Falla<EntidadGubernamentalDto>(
                ErrorAplicacion.NoEncontrado($"No existe la entidad gubernamental {id}."))
            : Resultado.Ok(Mapear(entidad));
    }

    public async Task<Resultado<EntidadGubernamentalDto>> CrearAsync(
        CrearEntidadGubernamentalRequest peticion,
        CancellationToken cancellationToken = default)
    {
        if (!PuedeAdministrar())
        {
            return Resultado.Falla<EntidadGubernamentalDto>(
                ErrorAplicacion.Prohibido("Solo un administrador puede crear entidades gubernamentales."));
        }

        var nombre = peticion.Nombre.Trim();

        if (await _repositorio.ExisteNombreAsync(nombre, null, cancellationToken))
        {
            return Resultado.Falla<EntidadGubernamentalDto>(
                ErrorAplicacion.Conflicto("Ya existe una entidad gubernamental con ese nombre.", "ENTIDAD_DUPLICADA"));
        }

        var ahora = _reloj.Ahora;
        var entidad = new EntidadGubernamental
        {
            Id = Guid.NewGuid(),
            Nombre = nombre,
            Categoria = peticion.Categoria.Trim(),
            PoderDelEstado = peticion.PoderDelEstado.Trim(),
            Sector = peticion.Sector.Trim(),
            Activa = peticion.Activa,
            FechaCreacion = ahora,
            FechaActualizacion = ahora
        };

        await _repositorio.AgregarAsync(entidad, cancellationToken);
        return Resultado.Ok(Mapear(entidad));
    }

    public async Task<Resultado<EntidadGubernamentalDto>> ActualizarAsync(
        Guid id,
        ActualizarEntidadGubernamentalRequest peticion,
        CancellationToken cancellationToken = default)
    {
        if (!PuedeAdministrar())
        {
            return Resultado.Falla<EntidadGubernamentalDto>(
                ErrorAplicacion.Prohibido("Solo un administrador puede modificar entidades gubernamentales."));
        }

        var entidad = await _repositorio.ObtenerPorIdAsync(id, cancellationToken);
        if (entidad is null)
        {
            return Resultado.Falla<EntidadGubernamentalDto>(
                ErrorAplicacion.NoEncontrado($"No existe la entidad gubernamental {id}."));
        }

        if (peticion.Nombre is not null)
        {
            var nombre = peticion.Nombre.Trim();
            if (await _repositorio.ExisteNombreAsync(nombre, id, cancellationToken))
            {
                return Resultado.Falla<EntidadGubernamentalDto>(
                    ErrorAplicacion.Conflicto("Ya existe una entidad gubernamental con ese nombre.", "ENTIDAD_DUPLICADA"));
            }

            entidad.Nombre = nombre;
        }

        if (peticion.Categoria is not null)
        {
            entidad.Categoria = peticion.Categoria.Trim();
        }

        if (peticion.PoderDelEstado is not null)
        {
            entidad.PoderDelEstado = peticion.PoderDelEstado.Trim();
        }

        if (peticion.Sector is not null)
        {
            entidad.Sector = peticion.Sector.Trim();
        }

        if (peticion.Activa is not null)
        {
            entidad.Activa = peticion.Activa.Value;
        }

        entidad.FechaActualizacion = _reloj.Ahora;

        await _repositorio.ActualizarAsync(entidad, cancellationToken);
        return Resultado.Ok(Mapear(entidad));
    }

    public async Task<Resultado> EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!PuedeAdministrar())
        {
            return Resultado.Falla(
                ErrorAplicacion.Prohibido("Solo un administrador puede eliminar entidades gubernamentales."));
        }

        var eliminada = await _repositorio.EliminarAsync(id, cancellationToken);

        return eliminada
            ? Resultado.Ok()
            : Resultado.Falla(ErrorAplicacion.NoEncontrado($"No existe la entidad gubernamental {id}."));
    }

    public Task<OpcionesFiltroEntidadesDto> ObtenerOpcionesFiltroAsync(CancellationToken cancellationToken = default) =>
        _repositorio.ObtenerOpcionesFiltroAsync(cancellationToken);

    public async Task<Resultado> AgregarOpcionCatalogoAsync(
        string tipoCatalogo,
        string valor,
        CancellationToken cancellationToken = default)
    {
        if (!PuedeAdministrar())
        {
            return Resultado.Falla(ErrorAplicacion.Prohibido("Solo un administrador puede modificar los catálogos."));
        }

        var agregada = await _repositorio.AgregarOpcionCatalogoAsync(tipoCatalogo, valor, cancellationToken);
        if (!agregada)
        {
            return Resultado.Falla(ErrorAplicacion.Conflicto($"La opción '{valor}' ya existe en el catálogo.", "OPCION_DUPLICADA"));
        }

        return Resultado.Ok();
    }

    public async Task<Resultado<int>> ActualizarOpcionCatalogoAsync(
        string tipoCatalogo,
        string valorActual,
        string nuevoValor,
        CancellationToken cancellationToken = default)
    {
        if (!PuedeAdministrar())
        {
            return Resultado.Falla<int>(ErrorAplicacion.Prohibido("Solo un administrador puede modificar los catálogos."));
        }

        var entidadesActualizadas = await _repositorio.ActualizarOpcionCatalogoAsync(tipoCatalogo, valorActual, nuevoValor, cancellationToken);
        return Resultado.Ok(entidadesActualizadas);
    }

    public async Task<Resultado> EliminarOpcionCatalogoAsync(
        string tipoCatalogo,
        string valor,
        CancellationToken cancellationToken = default)
    {
        if (!PuedeAdministrar())
        {
            return Resultado.Falla(ErrorAplicacion.Prohibido("Solo un administrador puede modificar los catálogos."));
        }

        var eliminada = await _repositorio.EliminarOpcionCatalogoAsync(tipoCatalogo, valor, cancellationToken);
        if (!eliminada)
        {
            return Resultado.Falla(ErrorAplicacion.Conflicto(
                $"No se puede eliminar la opción '{valor}' porque está asociada a una o más entidades gubernamentales.",
                "OPCION_EN_USO"));
        }

        return Resultado.Ok();
    }

    private bool PuedeAdministrar() =>
        _usuarioActual.Rol is RolUsuario.Administrador or RolUsuario.Analista;

    private static EntidadGubernamentalDto Mapear(EntidadGubernamental entidad) =>
        new(
            entidad.Id,
            entidad.Nombre,
            entidad.Categoria,
            entidad.PoderDelEstado,
            entidad.Sector,
            entidad.Activa,
            entidad.FechaCreacion,
            entidad.FechaActualizacion);
}
