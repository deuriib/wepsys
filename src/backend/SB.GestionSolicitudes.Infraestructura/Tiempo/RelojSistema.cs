using SB.GestionSolicitudes.Aplicacion.Contratos;

namespace SB.GestionSolicitudes.Infraestructura.Tiempo;

/// <summary>
/// System clock. Everything is stored and compared in UTC; the UI localizes.
/// </summary>
public sealed class RelojSistema : IReloj
{
    public DateTime Ahora => DateTime.UtcNow;
}
