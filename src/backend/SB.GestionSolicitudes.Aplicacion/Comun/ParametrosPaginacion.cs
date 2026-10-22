namespace SB.GestionSolicitudes.Aplicacion.Comun;

/// <summary>
/// Pagination input shared by every listing endpoint. Bounds are constants to
/// avoid magic numbers and to keep the API predictable.
/// </summary>
public sealed class ParametrosPaginacion
{
    public const int PAGINA_PREDETERMINADA = 1;
    public const int TAMANO_PREDETERMINADO = 10;
    public const int TAMANO_MAXIMO = 100;

    public int Pagina { get; init; } = PAGINA_PREDETERMINADA;

    public int TamanoPagina { get; init; } = TAMANO_PREDETERMINADO;

    public int PaginaNormalizada => Pagina < PAGINA_PREDETERMINADA ? PAGINA_PREDETERMINADA : Pagina;

    public int TamanoNormalizado => TamanoPagina switch
    {
        < 1 => TAMANO_PREDETERMINADO,
        > TAMANO_MAXIMO => TAMANO_MAXIMO,
        _ => TamanoPagina
    };

    public int Omitir => (PaginaNormalizada - 1) * TamanoNormalizado;
}
