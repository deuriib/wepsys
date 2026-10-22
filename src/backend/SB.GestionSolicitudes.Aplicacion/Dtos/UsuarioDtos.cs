using System.ComponentModel.DataAnnotations;
using SB.GestionSolicitudes.Dominio.Usuarios;

namespace SB.GestionSolicitudes.Aplicacion.Dtos;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(Usuario.LONGITUD_MAXIMA_CORREO, ErrorMessage = "El correo no puede superar 200 caracteres.")]
    public string Correo { get; init; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres.")]
    public string Contrasena { get; init; } = string.Empty;
}

public sealed record UsuarioSesionDto(int Id, string Nombre, string Correo, RolUsuario Rol, string RolDescripcion);

public sealed record RespuestaAutenticacionDto(
    string Token,
    DateTime ExpiraEn,
    UsuarioSesionDto Usuario);

public sealed record UsuarioDto(
    int Id,
    string Nombre,
    string Correo,
    RolUsuario Rol,
    string RolDescripcion,
    bool Activo,
    DateTime FechaCreacion);

public sealed class CrearUsuarioRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(Usuario.LONGITUD_MAXIMA_NOMBRE, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
    public string Nombre { get; init; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(Usuario.LONGITUD_MAXIMA_CORREO, ErrorMessage = "El correo no puede superar 200 caracteres.")]
    public string Correo { get; init; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres.")]
    public string Contrasena { get; init; } = string.Empty;

    [Required(ErrorMessage = "El rol es obligatorio.")]
    public RolUsuario? Rol { get; init; }
}

public sealed class ActualizarUsuarioRequest
{
    [StringLength(Usuario.LONGITUD_MAXIMA_NOMBRE, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
    public string? Nombre { get; init; }

    public RolUsuario? Rol { get; init; }

    public bool? Activo { get; init; }

    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres.")]
    public string? Contrasena { get; init; }
}

public sealed record NotificacionDto(
    int Id,
    int SolicitudId,
    string SolicitudCodigo,
    string Asunto,
    string Mensaje,
    string Canal,
    string Estado,
    DateTime Fecha,
    bool Leida);
