using SB.GestionSolicitudes.Dominio.Usuarios;

namespace SB.GestionSolicitudes.Aplicacion.Contratos;

/// <summary>
/// The authenticated caller, resolved from the JWT by the API layer. Keeping it
/// abstract lets the application layer run in tests without HTTP.
/// </summary>
public interface IUsuarioActual
{
    int Id { get; }

    string Nombre { get; }

    string Correo { get; }

    RolUsuario Rol { get; }

    bool EstaAutenticado { get; }
}
