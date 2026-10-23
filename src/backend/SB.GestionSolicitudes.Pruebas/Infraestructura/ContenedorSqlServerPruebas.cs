using Testcontainers.MsSql;

namespace SB.GestionSolicitudes.Pruebas.Infraestructura;

/// <summary>
/// One SQL Server container per test run, shared by every integration test.
///
/// The engine is disposable: it is created on the first test that needs it and
/// disappears when the process ends, so nobody has to install or start SQL Server
/// to run the suite. Only Docker is required.
///
/// If <c>ConnectionStrings__BaseDatosPruebas</c> is defined, <see cref="BaseDatosPruebas"/>
/// uses that instance instead and this container is never created. That is how CI
/// reuses a service container without exposing the Docker socket to the runner.
/// </summary>
internal static class ContenedorSqlServerPruebas
{
    public const string IMAGEN = "mcr.microsoft.com/mssql/server:2022-latest";

    // Lazy keeps a single start even when xunit opens several test classes at once;
    // ExecutionAndPublication makes the other threads wait instead of racing.
    private static readonly Lazy<MsSqlContainer> Contenedor =
        new(Iniciar, LazyThreadSafetyMode.ExecutionAndPublication);

    public static string CadenaConexion => Contenedor.Value.GetConnectionString();

    private static MsSqlContainer Iniciar()
    {
        var contenedor = new MsSqlBuilder(IMAGEN)
            .Build();

        try
        {
            // The factory builds the host synchronously, so the first query waits here
            // while the engine becomes ready. Delivery of the image can take a minute.
            contenedor.StartAsync().GetAwaiter().GetResult();
        }
        catch (Exception excepcion)
        {
            throw new InvalidOperationException(
                $"No se pudo iniciar el contenedor de pruebas '{IMAGEN}'. " +
                "Verifique que Docker esté en ejecución, o defina " +
                $"'{BaseDatosPruebas.VARIABLE_CADENA_CONEXION}' para usar una instancia de SQL Server existente.",
                excepcion);
        }

        // Best effort only: the process is already shutting down, so a container that
        // Docker can no longer reach must not turn a green run into a failed one.
        // Ryuk is the real guarantee that nothing survives the process.
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                contenedor.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            catch (Exception excepcion)
            {
                Console.Error.WriteLine(
                    $"No se pudo detener el contenedor de pruebas al cerrar el proceso: {excepcion.Message}");
            }
        };

        return contenedor;
    }
}
