using SB.GestionSolicitudes.Aplicacion.Comun;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Aplicacion.Dtos;
using SB.GestionSolicitudes.Dominio.Usuarios;

namespace SB.GestionSolicitudes.Aplicacion.Servicios;

/// <summary>
/// User administration. Every mutation is restricted to administrators and the
/// rule is enforced in the application layer, not only by the route policy.
/// </summary>
public sealed class UsuarioServicio
{
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IServicioHashContrasena _hash;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IReloj _reloj;

    public UsuarioServicio(
        IUsuarioRepositorio usuarios,
        IServicioHashContrasena hash,
        IUnidadDeTrabajo unidadDeTrabajo,
        IUsuarioActual usuarioActual,
        IReloj reloj)
    {
        _usuarios = usuarios;
        _hash = hash;
        _unidadDeTrabajo = unidadDeTrabajo;
        _usuarioActual = usuarioActual;
        _reloj = reloj;
    }

    public async Task<Resultado<IReadOnlyList<UsuarioDto>>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
    {
        if (_usuarioActual.Rol != RolUsuario.Administrador)
        {
            return Resultado.Falla<IReadOnlyList<UsuarioDto>>(
                ErrorAplicacion.Prohibido("Solo un administrador puede consultar el listado de usuarios."));
        }

        var usuarios = await _usuarios.ObtenerTodosAsync(cancellationToken);
        return Resultado.Ok<IReadOnlyList<UsuarioDto>>(usuarios.Select(Mapear).ToList());
    }

    /// <summary>
    /// Active technical users, used to populate the "responsable" selector. It is
    /// available to analysts as well because they need it to self-assign.
    /// </summary>
    public async Task<Resultado<IReadOnlyList<UsuarioDto>>> ObtenerResponsablesDisponiblesAsync(
        CancellationToken cancellationToken = default)
    {
        if (_usuarioActual.Rol == RolUsuario.Solicitante)
        {
            return Resultado.Falla<IReadOnlyList<UsuarioDto>>(
                ErrorAplicacion.Prohibido("No tiene permisos para consultar responsables técnicos."));
        }

        var analistas = await _usuarios.ObtenerAnalistasActivosAsync(cancellationToken);
        return Resultado.Ok<IReadOnlyList<UsuarioDto>>(analistas.Select(Mapear).ToList());
    }

    public async Task<Resultado<UsuarioDto>> CrearAsync(
        CrearUsuarioRequest peticion,
        CancellationToken cancellationToken = default)
    {
        if (_usuarioActual.Rol != RolUsuario.Administrador)
        {
            return Resultado.Falla<UsuarioDto>(ErrorAplicacion.Prohibido("Solo un administrador puede crear usuarios."));
        }

        var correo = peticion.Correo.Trim().ToLowerInvariant();

        if (await _usuarios.ExisteCorreoAsync(correo, cancellationToken))
        {
            return Resultado.Falla<UsuarioDto>(
                ErrorAplicacion.Conflicto("Ya existe un usuario registrado con ese correo.", "CORREO_DUPLICADO"));
        }

        var usuario = new Usuario
        {
            Nombre = peticion.Nombre.Trim(),
            Correo = correo,
            HashContrasena = _hash.Hashear(peticion.Contrasena),
            Rol = peticion.Rol!.Value,
            Activo = true,
            FechaCreacion = _reloj.Ahora
        };

        await _usuarios.AgregarAsync(usuario, cancellationToken);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        return Resultado.Ok(Mapear(usuario));
    }

    public async Task<Resultado<UsuarioDto>> ActualizarAsync(
        int id,
        ActualizarUsuarioRequest peticion,
        CancellationToken cancellationToken = default)
    {
        if (_usuarioActual.Rol != RolUsuario.Administrador)
        {
            return Resultado.Falla<UsuarioDto>(ErrorAplicacion.Prohibido("Solo un administrador puede modificar usuarios."));
        }

        var usuario = await _usuarios.ObtenerPorIdAsync(id, cancellationToken);
        if (usuario is null)
        {
            return Resultado.Falla<UsuarioDto>(ErrorAplicacion.NoEncontrado($"No existe el usuario {id}."));
        }

        if (peticion.Nombre is not null)
        {
            usuario.Nombre = peticion.Nombre.Trim();
        }

        if (peticion.Rol is not null)
        {
            usuario.Rol = peticion.Rol.Value;
        }

        if (peticion.Activo is not null)
        {
            usuario.Activo = peticion.Activo.Value;
        }

        if (!string.IsNullOrWhiteSpace(peticion.Contrasena))
        {
            usuario.HashContrasena = _hash.Hashear(peticion.Contrasena);
        }

        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        return Resultado.Ok(Mapear(usuario));
    }

    private static UsuarioDto Mapear(Usuario usuario) =>
        new(
            usuario.Id,
            usuario.Nombre,
            usuario.Correo,
            usuario.Rol,
            Descripciones.DeRol(usuario.Rol),
            usuario.Activo,
            usuario.FechaCreacion);
}
