using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SB.GestionSolicitudes.Dominio.Notificaciones;
using SB.GestionSolicitudes.Dominio.Solicitudes;
using SB.GestionSolicitudes.Dominio.Usuarios;

namespace SB.GestionSolicitudes.Infraestructura.Persistencia.Configuraciones;

public sealed class SolicitudConfiguracion : IEntityTypeConfiguration<Solicitud>
{
    public void Configure(EntityTypeBuilder<Solicitud> builder)
    {
        builder.ToTable("Solicitudes");
        builder.HasKey(solicitud => solicitud.Id);

        builder.Property(solicitud => solicitud.Codigo).HasMaxLength(30).IsRequired();
        builder.HasIndex(solicitud => solicitud.Codigo).IsUnique();
        builder.HasIndex(solicitud => solicitud.Estado);
        builder.HasIndex(solicitud => solicitud.FechaCompromiso);

        builder.Property(solicitud => solicitud.Titulo).HasMaxLength(Solicitud.LONGITUD_MAXIMA_TITULO).IsRequired();
        builder.Property(solicitud => solicitud.Descripcion).HasMaxLength(Solicitud.LONGITUD_MAXIMA_DESCRIPCION).IsRequired();
        builder.Property(solicitud => solicitud.UrlEvidencia).HasMaxLength(Solicitud.LONGITUD_MAXIMA_URL_EVIDENCIA);

        builder.Property(solicitud => solicitud.Prioridad).HasConversion<int>();
        builder.Property(solicitud => solicitud.Estado).HasConversion<int>();

        builder.HasOne(solicitud => solicitud.Area)
            .WithMany()
            .HasForeignKey(solicitud => solicitud.AreaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(solicitud => solicitud.TipoSolicitud)
            .WithMany()
            .HasForeignKey(solicitud => solicitud.TipoSolicitudId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(solicitud => solicitud.Solicitante)
            .WithMany(usuario => usuario.SolicitudesCreadas)
            .HasForeignKey(solicitud => solicitud.SolicitanteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(solicitud => solicitud.Responsable)
            .WithMany(usuario => usuario.SolicitudesAsignadas)
            .HasForeignKey(solicitud => solicitud.ResponsableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(solicitud => solicitud.Historial)
            .WithOne(historial => historial.Solicitud!)
            .HasForeignKey(historial => historial.SolicitudId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(solicitud => solicitud.Comentarios)
            .WithOne(comentario => comentario.Solicitud!)
            .HasForeignKey(comentario => comentario.SolicitudId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class HistorialEstadoConfiguracion : IEntityTypeConfiguration<HistorialEstado>
{
    public void Configure(EntityTypeBuilder<HistorialEstado> builder)
    {
        builder.ToTable("HistorialesEstado");
        builder.HasKey(historial => historial.Id);

        builder.Property(historial => historial.EstadoNuevo).HasConversion<int>();
        builder.Property(historial => historial.EstadoAnterior).HasConversion<int?>();
        builder.Property(historial => historial.Comentario).HasMaxLength(Solicitud.LONGITUD_MAXIMA_COMENTARIO);

        builder.HasOne(historial => historial.Usuario)
            .WithMany()
            .HasForeignKey(historial => historial.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(historial => new { historial.SolicitudId, historial.Fecha });
    }
}

public sealed class ComentarioConfiguracion : IEntityTypeConfiguration<Comentario>
{
    public void Configure(EntityTypeBuilder<Comentario> builder)
    {
        builder.ToTable("Comentarios");
        builder.HasKey(comentario => comentario.Id);

        builder.Property(comentario => comentario.Texto).HasMaxLength(Solicitud.LONGITUD_MAXIMA_COMENTARIO).IsRequired();
        builder.Property(comentario => comentario.Visibilidad).HasConversion<int>();

        builder.HasOne(comentario => comentario.Usuario)
            .WithMany()
            .HasForeignKey(comentario => comentario.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(comentario => new { comentario.SolicitudId, comentario.Fecha });
    }
}

public sealed class NotificacionConfiguracion : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> builder)
    {
        builder.ToTable("Notificaciones");
        builder.HasKey(notificacion => notificacion.Id);

        builder.Property(notificacion => notificacion.Canal).HasConversion<int>();
        builder.Property(notificacion => notificacion.Estado).HasConversion<int>();
        builder.Property(notificacion => notificacion.Asunto).HasMaxLength(Notificacion.LONGITUD_MAXIMA_ASUNTO).IsRequired();
        builder.Property(notificacion => notificacion.Mensaje).HasMaxLength(Notificacion.LONGITUD_MAXIMA_MENSAJE).IsRequired();

        builder.HasOne(notificacion => notificacion.Solicitud)
            .WithMany()
            .HasForeignKey(notificacion => notificacion.SolicitudId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(notificacion => notificacion.Destinatario)
            .WithMany()
            .HasForeignKey(notificacion => notificacion.DestinatarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(notificacion => new { notificacion.DestinatarioId, notificacion.Leida });
    }
}
