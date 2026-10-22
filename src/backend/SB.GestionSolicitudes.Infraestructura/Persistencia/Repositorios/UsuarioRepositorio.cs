using Microsoft.EntityFrameworkCore;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Dominio.Usuarios;

namespace SB.GestionSolicitudes.Infraestructura.Persistencia.Repositorios;

public sealed class UsuarioRepositorio : IUsuarioRepositorio
{
    private readonly ContextoBaseDatos _contexto;

    public UsuarioRepositorio(ContextoBaseDatos contexto)
    {
        _contexto = contexto;
    }

    public Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        _contexto.Usuarios.FirstOrDefaultAsync(usuario => usuario.Id == id, cancellationToken);

    public Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancellationToken = default) =>
        _contexto.Usuarios.FirstOrDefaultAsync(
            usuario => usuario.Correo == correo,
            cancellationToken);

    public async Task<IReadOnlyList<Usuario>> ObtenerTodosAsync(CancellationToken cancellationToken = default) =>
        await _contexto.Usuarios
            .AsNoTracking()
            .OrderBy(usuario => usuario.Nombre)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Usuario>> ObtenerAnalistasActivosAsync(
        CancellationToken cancellationToken = default) =>
        await _contexto.Usuarios
            .AsNoTracking()
            .Where(usuario => usuario.Activo &&
                              (usuario.Rol == RolUsuario.Analista || usuario.Rol == RolUsuario.Administrador))
            .OrderBy(usuario => usuario.Nombre)
            .ToListAsync(cancellationToken);

    public Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken = default) =>
        _contexto.Usuarios.AnyAsync(usuario => usuario.Correo == correo, cancellationToken);

    public async Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        await _contexto.Usuarios.AddAsync(usuario, cancellationToken);
    }
}
