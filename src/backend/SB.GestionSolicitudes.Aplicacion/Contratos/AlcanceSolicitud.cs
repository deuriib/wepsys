namespace SB.GestionSolicitudes.Aplicacion.Contratos;

/// <summary>
/// Data visibility window derived from the caller's role. It is computed in the
/// application layer and enforced in the query, never in the client.
/// </summary>
public enum AlcanceSolicitud
{
    Todos = 1,
    Propios = 2,
    AsignadosODisponibles = 3
}
