namespace SB.GestionSolicitudes.Dominio.Solicitudes;

/// <summary>
/// Immutable audit record of every state transition (and assignment event).
/// Keeping it append-only is what makes the traceability requirement provable.
/// </summary>
public class HistorialEstado
{
    public int Id { get; set; }

    public int SolicitudId { get; set; }

    public Solicitud? Solicitud { get; set; }

    public EstadoSolicitud? EstadoAnterior { get; set; }

    public EstadoSolicitud EstadoNuevo { get; set; }

    public DateTime Fecha { get; set; }

    public int UsuarioId { get; set; }

    public Usuarios.Usuario? Usuario { get; set; }

    public string? Comentario { get; set; }
}
