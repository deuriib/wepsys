using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SB.GestionSolicitudes.Infraestructura.Persistencia;

/// <summary>
/// Design-time factory used by <c>dotnet ef migrations</c>. It avoids booting the
/// whole API (and its configuration) just to scaffold a migration.
/// </summary>
public sealed class FabricaContextoDiseno : IDesignTimeDbContextFactory<ContextoBaseDatos>
{
    private const string CADENA_DISENO = "Data Source=diseno-migraciones.db";

    public ContextoBaseDatos CreateDbContext(string[] args)
    {
        var opciones = new DbContextOptionsBuilder<ContextoBaseDatos>()
            .UseSqlite(CADENA_DISENO)
            .Options;

        return new ContextoBaseDatos(opciones);
    }
}
