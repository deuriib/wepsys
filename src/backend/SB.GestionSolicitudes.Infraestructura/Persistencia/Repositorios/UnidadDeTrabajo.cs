using SB.GestionSolicitudes.Aplicacion.Contratos;

namespace SB.GestionSolicitudes.Infraestructura.Persistencia.Repositorios;

/// <summary>
/// Entity Framework Core unit of work: one <c>SaveChanges</c> commits the whole
/// aggregate graph (request + history + comments + notifications) atomically.
/// </summary>
public sealed class UnidadDeTrabajo : IUnidadDeTrabajo
{
    private readonly ContextoBaseDatos _contexto;

    public UnidadDeTrabajo(ContextoBaseDatos contexto)
    {
        _contexto = contexto;
    }

    public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
        _contexto.SaveChangesAsync(cancellationToken);
}
