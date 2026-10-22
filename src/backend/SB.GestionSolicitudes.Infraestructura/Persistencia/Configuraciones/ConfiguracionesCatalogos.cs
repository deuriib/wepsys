using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SB.GestionSolicitudes.Dominio.Catalogos;
using SB.GestionSolicitudes.Dominio.Usuarios;

namespace SB.GestionSolicitudes.Infraestructura.Persistencia.Configuraciones;

public sealed class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(usuario => usuario.Id);

        builder.Property(usuario => usuario.Nombre).HasMaxLength(Usuario.LONGITUD_MAXIMA_NOMBRE).IsRequired();
        builder.Property(usuario => usuario.Correo).HasMaxLength(Usuario.LONGITUD_MAXIMA_CORREO).IsRequired();
        builder.Property(usuario => usuario.HashContrasena).HasMaxLength(500).IsRequired();
        builder.Property(usuario => usuario.Rol).HasConversion<int>();

        builder.HasIndex(usuario => usuario.Correo).IsUnique();
    }
}

public sealed class AreaConfiguracion : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> builder)
    {
        builder.ToTable("Areas");
        builder.HasKey(area => area.Id);
        builder.Property(area => area.Nombre).HasMaxLength(Area.LONGITUD_MAXIMA_NOMBRE).IsRequired();
        builder.HasIndex(area => area.Nombre).IsUnique();
    }
}

public sealed class TipoSolicitudConfiguracion : IEntityTypeConfiguration<TipoSolicitud>
{
    public void Configure(EntityTypeBuilder<TipoSolicitud> builder)
    {
        builder.ToTable("TiposSolicitud");
        builder.HasKey(tipo => tipo.Id);
        builder.Property(tipo => tipo.Nombre).HasMaxLength(TipoSolicitud.LONGITUD_MAXIMA_NOMBRE).IsRequired();
        builder.Property(tipo => tipo.Descripcion).HasMaxLength(TipoSolicitud.LONGITUD_MAXIMA_DESCRIPCION);
        builder.HasIndex(tipo => tipo.Nombre).IsUnique();
    }
}
