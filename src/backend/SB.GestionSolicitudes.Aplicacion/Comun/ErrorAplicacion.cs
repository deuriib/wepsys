namespace SB.GestionSolicitudes.Aplicacion.Comun;

/// <summary>
/// Serializable description of a failure. <see cref="Codigo"/> is a stable
/// machine-readable value the frontend can branch on.
/// </summary>
public sealed record ErrorAplicacion(TipoError Tipo, string Codigo, string Mensaje)
{
    public static ErrorAplicacion Validacion(string mensaje, string codigo = "VALIDACION") =>
        new(TipoError.Validacion, codigo, mensaje);

    public static ErrorAplicacion NoEncontrado(string mensaje, string codigo = "NO_ENCONTRADO") =>
        new(TipoError.NoEncontrado, codigo, mensaje);

    public static ErrorAplicacion Conflicto(string mensaje, string codigo = "CONFLICTO") =>
        new(TipoError.Conflicto, codigo, mensaje);

    public static ErrorAplicacion NoAutorizado(string mensaje, string codigo = "NO_AUTORIZADO") =>
        new(TipoError.NoAutorizado, codigo, mensaje);

    public static ErrorAplicacion Prohibido(string mensaje, string codigo = "PROHIBIDO") =>
        new(TipoError.Prohibido, codigo, mensaje);
}
