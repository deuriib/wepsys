namespace SB.GestionSolicitudes.Aplicacion.Contratos;

/// <summary>
/// Abstraction over time. Business rules receive the current instant from this
/// contract so tests can control expiry and overdue calculations.
/// </summary>
public interface IReloj
{
    DateTime Ahora { get; }
}
