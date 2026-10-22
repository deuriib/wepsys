namespace SB.GestionSolicitudes.Dominio.Solicitudes;

/// <summary>
/// Visibility of a comment. Internal comments are visible to the technical team
/// only; public comments are also visible to the requester.
/// </summary>
public enum VisibilidadComentario
{
    Interna = 1,
    Publica = 2
}
