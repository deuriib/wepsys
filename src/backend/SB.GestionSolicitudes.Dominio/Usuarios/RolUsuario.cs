namespace SB.GestionSolicitudes.Dominio.Usuarios;

/// <summary>
/// Minimum roles required by the exercise. The API authorizes endpoints with
/// policies built on top of these values, never on the client's claims alone.
/// </summary>
public enum RolUsuario
{
    Administrador = 1,
    Analista = 2,
    Solicitante = 3
}
