using System.Security.Claims;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Dominio.Usuarios;

namespace SB.GestionSolicitudes.Servicios.Autenticacion;

/// <summary>
/// Resolves the caller from the validated JWT. Reading the claims here (and only
/// here) keeps the application layer free of ASP.NET Core types.
/// </summary>
public sealed class UsuarioActual : IUsuarioActual
{
    private readonly IHttpContextAccessor _accesoHttp;

    public UsuarioActual(IHttpContextAccessor accesoHttp)
    {
        _accesoHttp = accesoHttp;
    }

    public int Id
    {
        get
        {
            var valor = ObtenerClaim(ClaimTypes.NameIdentifier);

            return int.TryParse(valor, out var id) ? id : 0;
        }
    }

    public string Nombre => ObtenerClaim(ClaimTypes.Name) ?? string.Empty;

    public string Correo => ObtenerClaim(ClaimTypes.Email) ?? string.Empty;

    public RolUsuario Rol =>
        Enum.TryParse<RolUsuario>(ObtenerClaim(ClaimTypes.Role), ignoreCase: true, out var rol)
            ? rol
            : RolUsuario.Solicitante;

    public bool EstaAutenticado => _accesoHttp.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    private string? ObtenerClaim(string tipo) =>
        _accesoHttp.HttpContext?.User.FindFirst(tipo)?.Value;
}
