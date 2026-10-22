namespace SB.GestionSolicitudes.Infraestructura.Archivos;

/// <summary>
/// Resolves the plain-text database location for both development (folder in the
/// repository) and any other environment (folder next to the application).
/// Walking upwards keeps <c>dotnet run</c>, tests and published binaries working
/// with the same relative configuration value.
/// </summary>
public static class ResolutorRutas
{
    public const string NOMBRE_DIRECTORIO_DATOS = "database";
    private const int PROFUNDIDAD_MAXIMA_BUSQUEDA = 5;

    public static string ResolverRutaEscritura(string rutaRelativa, string directorioContenido)
    {
        if (Path.IsPathRooted(rutaRelativa))
        {
            return rutaRelativa;
        }

        var directorioDatos = BuscarDirectorioDatos(directorioContenido);

        return Path.GetFullPath(Path.Combine(
            directorioDatos ?? directorioContenido,
            Path.GetFileName(rutaRelativa)));
    }

    public static string ResolverRutaSemilla(string rutaRelativa, string directorioContenido)
    {
        if (Path.IsPathRooted(rutaRelativa))
        {
            return rutaRelativa;
        }

        var directorioDatos = BuscarDirectorioDatos(directorioContenido);

        return Path.GetFullPath(Path.Combine(
            directorioDatos ?? directorioContenido,
            Path.GetFileName(rutaRelativa)));
    }

    private static string? BuscarDirectorioDatos(string directorioInicial)
    {
        var actual = new DirectoryInfo(directorioInicial);

        for (var nivel = 0; nivel <= PROFUNDIDAD_MAXIMA_BUSQUEDA && actual is not null; nivel++)
        {
            var candidato = Path.Combine(actual.FullName, NOMBRE_DIRECTORIO_DATOS);

            if (Directory.Exists(candidato))
            {
                return candidato;
            }

            actual = actual.Parent;
        }

        return null;
    }
}
