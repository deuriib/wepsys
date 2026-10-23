using Microsoft.EntityFrameworkCore;
using SB.GestionSolicitudes.Dominio.Catalogos;
using SB.GestionSolicitudes.Dominio.Notificaciones;
using SB.GestionSolicitudes.Dominio.Solicitudes;
using SB.GestionSolicitudes.Dominio.Usuarios;

namespace SB.GestionSolicitudes.Infraestructura.Persistencia;

/// <summary>
/// Entity Framework Core unit of work over the relational model. The relational
/// store is SQL Server; the government entities catalog lives outside of it, in
/// the plain-text file required by the specification.
/// </summary>
public sealed class ContextoBaseDatos : DbContext
{
    public ContextoBaseDatos(DbContextOptions<ContextoBaseDatos> opciones)
        : base(opciones)
    {
    }

    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();

    public DbSet<HistorialEstado> HistorialesEstado => Set<HistorialEstado>();

    public DbSet<Comentario> Comentarios => Set<Comentario>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Area> Areas => Set<Area>();

    public DbSet<TipoSolicitud> TiposSolicitud => Set<TipoSolicitud>();

    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContextoBaseDatos).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
