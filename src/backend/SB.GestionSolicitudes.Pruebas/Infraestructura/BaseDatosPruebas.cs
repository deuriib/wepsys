using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace SB.GestionSolicitudes.Pruebas.Infraestructura;

/// <summary>
/// Gives every integration test its own SQL Server database, so the suite can run
/// in parallel and never touches the developer's data.
///
/// The instance comes from <c>ConnectionStrings__BaseDatosPruebas</c> when that
/// variable is defined (CI service container, an existing server); otherwise a
/// disposable container is started by <see cref="ContenedorSqlServerPruebas"/>.
/// </summary>
internal static partial class BaseDatosPruebas
{
    public const string VARIABLE_CADENA_CONEXION = "ConnectionStrings__BaseDatosPruebas";

    private const string PREFIJO_NOMBRE = "SbPruebas_";
    private const int INTENTOS_CREACION = 3;
    private const int INTENTOS_DISPONIBILIDAD = 15;
    private const int MILISEGUNDOS_ENTRE_INTENTOS = 500;
    private const int MILISEGUNDOS_ENTRE_ESPERAS = 2000;
    private const int SEGUNDOS_TIEMPO_ESPERA_COMANDO = 60;

    /// <summary>
    /// Creates an empty database and returns its name plus the connection string that
    /// points to it.
    /// </summary>
    public static (string Nombre, string CadenaConexion) Crear()
    {
        EsperarServidorDisponible();

        var nombre = $"{PREFIJO_NOMBRE}{Guid.NewGuid():N}";

        // The name is generated here, never received from outside, but it is still
        // validated because it is interpolated into DDL where parameters are not allowed.
        if (!NombreValido().IsMatch(nombre))
        {
            throw new InvalidOperationException($"Nombre de base de datos de pruebas inválido: '{nombre}'.");
        }

        EjecutarConReintentos($"CREATE DATABASE [{nombre}]");

        return (nombre, CadenaConexion(nombre));
    }

    /// <summary>
    /// Drops the database created by <see cref="Crear"/>. Cleanup never fails a test:
    /// whatever cannot be dropped is left for the next run to replace.
    /// </summary>
    public static void Eliminar(string nombre)
    {
        if (!NombreValido().IsMatch(nombre))
        {
            return;
        }

        try
        {
            Ejecutar($"IF DB_ID('{nombre}') IS NOT NULL BEGIN ALTER DATABASE [{nombre}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{nombre}]; END");
        }
        catch (SqlException excepcion)
        {
            // A database held open by another test process is not a test failure, but
            // it must be visible: with a disposable container these disappear with it,
            // with an external instance they would pile up silently.
            Console.Error.WriteLine($"No se pudo eliminar la base de datos de pruebas '{nombre}': {excepcion.Message}");
        }
    }

    private static string CadenaConexion(string nombreBaseDatos)
    {
        var constructor = new SqlConnectionStringBuilder(CadenaMaestra)
        {
            InitialCatalog = nombreBaseDatos
        };

        return constructor.ConnectionString;
    }

    private static string CadenaMaestra =>
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(VARIABLE_CADENA_CONEXION))
            ? ContenedorSqlServerPruebas.CadenaConexion
            : Environment.GetEnvironmentVariable(VARIABLE_CADENA_CONEXION)!;

    /// <summary>
    /// Waits for the server to answer. `docker compose run` starts the SQL Server
    /// container and the test runner at the same time, so the first attempt usually
    /// arrives while the engine is still recovering.
    /// </summary>
    private static void EsperarServidorDisponible()
    {
        for (var intento = 1; ; intento++)
        {
            try
            {
                Ejecutar("SELECT 1");
                return;
            }
            catch (SqlException) when (intento < INTENTOS_DISPONIBILIDAD)
            {
                Thread.Sleep(MILISEGUNDOS_ENTRE_ESPERAS);
            }
        }
    }

    private static void EjecutarConReintentos(string sentencia)
    {
        for (var intento = 1; intento <= INTENTOS_CREACION; intento++)
        {
            try
            {
                Ejecutar(sentencia);
                return;
            }
            catch (SqlException) when (intento < INTENTOS_CREACION)
            {
                // Creating many databases at once makes SQL Server serialize access to
                // its model database; a short wait is enough to get through.
                Thread.Sleep(MILISEGUNDOS_ENTRE_INTENTOS);
            }
        }
    }

    private static void Ejecutar(string sentencia)
    {
        using var conexion = new SqlConnection(CadenaMaestra);
        conexion.Open();

        using var comando = new SqlCommand(sentencia, conexion)
        {
            CommandTimeout = SEGUNDOS_TIEMPO_ESPERA_COMANDO
        };

        comando.ExecuteNonQuery();
    }

    [GeneratedRegex("^[A-Za-z0-9_]{1,80}$", RegexOptions.CultureInvariant)]
    private static partial Regex NombreValido();
}
