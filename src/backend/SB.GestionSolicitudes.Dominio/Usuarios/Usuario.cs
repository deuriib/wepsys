namespace SB.GestionSolicitudes.Dominio.Usuarios;

/// <summary>
/// Simplified user model (the exercise explicitly allows a simplified own model
/// instead of ASP.NET Identity). Password storage is delegated to an
/// infrastructure service through <c>IServicioHashContrasena</c>.
/// </summary>
public class Usuario
{
    public const int LONGITUD_MAXIMA_NOMBRE = 150;
    public const int LONGITUD_MAXIMA_CORREO = 200;

    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public string HashContrasena { get; set; } = string.Empty;

    public RolUsuario Rol { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; }

    public ICollection<Solicitudes.Solicitud> SolicitudesCreadas { get; set; } = new List<Solicitudes.Solicitud>();

    public ICollection<Solicitudes.Solicitud> SolicitudesAsignadas { get; set; } = new List<Solicitudes.Solicitud>();

    public bool TieneRolOperativo() =>
        Rol is RolUsuario.Administrador or RolUsuario.Analista;
}
