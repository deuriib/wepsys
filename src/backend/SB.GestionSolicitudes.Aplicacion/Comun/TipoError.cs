namespace SB.GestionSolicitudes.Aplicacion.Comun;

/// <summary>
/// Classifies a failure so the API layer can translate it into an HTTP status
/// without knowing which use case produced it.
/// </summary>
public enum TipoError
{
    Validacion = 1,
    NoEncontrado = 2,
    Conflicto = 3,
    NoAutorizado = 4,
    Prohibido = 5
}
