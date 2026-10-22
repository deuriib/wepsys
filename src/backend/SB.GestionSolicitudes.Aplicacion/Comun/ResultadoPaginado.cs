namespace SB.GestionSolicitudes.Aplicacion.Comun;

/// <summary>
/// Envelope for paginated responses. Returning the totals lets the UI render
/// real pagination instead of guessing from the page size.
/// </summary>
public sealed class ResultadoPaginado<T>
{
    public ResultadoPaginado(IReadOnlyList<T> elementos, int totalRegistros, int pagina, int tamanoPagina)
    {
        Elementos = elementos;
        TotalRegistros = totalRegistros;
        Pagina = pagina;
        TamanoPagina = tamanoPagina;
        TotalPaginas = tamanoPagina <= 0
            ? 0
            : (int)Math.Ceiling(totalRegistros / (double)tamanoPagina);
    }

    public IReadOnlyList<T> Elementos { get; }

    public int TotalRegistros { get; }

    public int Pagina { get; }

    public int TamanoPagina { get; }

    public int TotalPaginas { get; }

    public static ResultadoPaginado<T> Vacio(ParametrosPaginacion paginacion) =>
        new(Array.Empty<T>(), 0, paginacion.PaginaNormalizada, paginacion.TamanoNormalizado);
}
