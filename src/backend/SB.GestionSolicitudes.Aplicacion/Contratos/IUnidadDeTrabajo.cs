namespace SB.GestionSolicitudes.Aplicacion.Contratos;

/// <summary>
/// Transaction boundary. The application layer decides <em>when</em> to commit;
/// Entity Framework Core decides <em>how</em>.
/// </summary>
public interface IUnidadDeTrabajo
{
    Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
