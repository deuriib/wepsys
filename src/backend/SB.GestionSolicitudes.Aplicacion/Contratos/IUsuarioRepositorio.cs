using SB.GestionSolicitudes.Dominio.Usuarios;

namespace SB.GestionSolicitudes.Aplicacion.Contratos;

/// <summary>
/// User persistence contract. Password hashing lives behind
/// <see cref="IServicioHashContrasena"/> so the storage never sees plain text.
/// </summary>
public interface IUsuarioRepositorio
{
    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Usuario>> ObtenerTodosAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Usuario>> ObtenerAnalistasActivosAsync(CancellationToken cancellationToken = default);

    Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken = default);

    Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default);
}
