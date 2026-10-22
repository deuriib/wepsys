namespace SB.GestionSolicitudes.Dominio.Solicitudes;

/// <summary>
/// Business priority of a request. Used by the dashboard and by operational
/// ordering; the numeric values allow a stable sort in SQL.
/// </summary>
public enum PrioridadSolicitud
{
    Baja = 1,
    Media = 2,
    Alta = 3,
    Critica = 4
}
