namespace SB.GestionSolicitudes.Dominio.Solicitudes;

/// <summary>
/// Comment attached to a request. Visibility decides whether the requester can
/// see it, which is enforced in the query layer, not only in the UI.
/// </summary>
public class Comentario
{
    public int Id { get; set; }

    public int SolicitudId { get; set; }

    public Solicitud? Solicitud { get; set; }

    public string Texto { get; set; } = string.Empty;

    public VisibilidadComentario Visibilidad { get; set; }

    public DateTime Fecha { get; set; }

    public int UsuarioId { get; set; }

    public Usuarios.Usuario? Usuario { get; set; }
}
