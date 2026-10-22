using SB.GestionSolicitudes.Aplicacion.Comun;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Aplicacion.Dtos;

namespace SB.GestionSolicitudes.Aplicacion.Servicios;

/// <summary>
/// Authentication use case. It deliberately returns the same error for an
/// unknown user and a wrong password to avoid account enumeration.
/// </summary>
public sealed class AutenticacionServicio
{
    private const string MENSAJE_CREDENCIALES_INVALIDAS = "Correo o contraseña incorrectos.";

    private readonly IUsuarioRepositorio _usuarios;
    private readonly IServicioHashContrasena _hash;
    private readonly IGeneradorTokenJwt _generadorToken;

    public AutenticacionServicio(
        IUsuarioRepositorio usuarios,
        IServicioHashContrasena hash,
        IGeneradorTokenJwt generadorToken)
    {
        _usuarios = usuarios;
        _hash = hash;
        _generadorToken = generadorToken;
    }

    public async Task<Resultado<RespuestaAutenticacionDto>> IniciarSesionAsync(
        LoginRequest peticion,
        CancellationToken cancellationToken = default)
    {
        var correo = peticion.Correo.Trim().ToLowerInvariant();
        var usuario = await _usuarios.ObtenerPorCorreoAsync(correo, cancellationToken);

        if (usuario is null || !usuario.Activo || !_hash.Verificar(peticion.Contrasena, usuario.HashContrasena))
        {
            return Resultado.Falla<RespuestaAutenticacionDto>(
                ErrorAplicacion.NoAutorizado(MENSAJE_CREDENCIALES_INVALIDAS, "CREDENCIALES_INVALIDAS"));
        }

        var token = _generadorToken.Generar(usuario);

        return Resultado.Ok(new RespuestaAutenticacionDto(
            token.Token,
            token.ExpiraEn,
            new UsuarioSesionDto(
                usuario.Id,
                usuario.Nombre,
                usuario.Correo,
                usuario.Rol,
                Descripciones.DeRol(usuario.Rol))));
    }
}
