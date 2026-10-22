namespace SB.GestionSolicitudes.Aplicacion.Comun;

/// <summary>
/// Outcome of a use case. Returning a result instead of throwing for expected
/// failures keeps controllers branch-free and the flow explicit.
/// </summary>
public class Resultado
{
    protected Resultado(bool exito, ErrorAplicacion? error)
    {
        Exito = exito;
        Error = error;
    }

    public bool Exito { get; }

    public ErrorAplicacion? Error { get; }

    public bool Fallo => !Exito;

    public static Resultado Ok() => new(true, null);

    public static Resultado Falla(ErrorAplicacion error) => new(false, error);

    public static Resultado<T> Ok<T>(T valor) => Resultado<T>.Ok(valor);

    public static Resultado<T> Falla<T>(ErrorAplicacion error) => Resultado<T>.Falla(error);
}

/// <summary>
/// Result carrying a value on success.
/// </summary>
public sealed class Resultado<T> : Resultado
{
    private Resultado(bool exito, T? valor, ErrorAplicacion? error)
        : base(exito, error)
    {
        Valor = valor;
    }

    public T? Valor { get; }

    public static Resultado<T> Ok(T valor) => new(true, valor, null);

    public static new Resultado<T> Falla(ErrorAplicacion error) => new(false, default, error);

    /// <summary>
    /// Projects the value while keeping the failure untouched, which lets use
    /// cases compose without repetitive <c>if</c> statements.
    /// </summary>
    public Resultado<TSalida> Mapear<TSalida>(Func<T, TSalida> proyeccion) =>
        Exito && Valor is not null
            ? Resultado<TSalida>.Ok(proyeccion(Valor))
            : Resultado<TSalida>.Falla(Error ?? ErrorAplicacion.Conflicto("Operación sin resultado."));
}
