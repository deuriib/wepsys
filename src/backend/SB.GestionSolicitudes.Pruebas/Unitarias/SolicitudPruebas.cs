using Shouldly;
using SB.GestionSolicitudes.Dominio.Excepciones;
using SB.GestionSolicitudes.Dominio.Solicitudes;
using SB.GestionSolicitudes.Dominio.Usuarios;
using Xunit;

namespace SB.GestionSolicitudes.Pruebas.Unitarias;

/// <summary>
/// Domain invariants of the request aggregate. These tests need no database, no
/// HTTP and no clock, which is the point of keeping the rules in the domain.
/// </summary>
public sealed class SolicitudPruebas
{
    private static readonly DateTime AHORA = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);
    private const int AREA_ID = 1;
    private const int TIPO_ID = 2;
    private const int SOLICITANTE_ID = 3;
    private const int ANALISTA_ID = 4;

    [Fact]
    public void Registrar_crea_la_solicitud_en_estado_registrada_con_historial_inicial()
    {
        var solicitud = Crear();

        solicitud.Estado.ShouldBe(EstadoSolicitud.Registrada);
        solicitud.Codigo.ShouldBe("SOL-2026-0001");
        solicitud.Historial.Count.ShouldBe(1);
        solicitud.Historial.Single().EstadoAnterior.ShouldBeNull();
        solicitud.Historial.Single().EstadoNuevo.ShouldBe(EstadoSolicitud.Registrada);
        solicitud.FechaCierre.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Registrar_rechaza_titulo_vacio(string titulo)
    {
        Should.Throw<ExcepcionReglaNegocio>(() => Crear(titulo: titulo))
            .Message.ShouldContain("título");
    }

    [Fact]
    public void Registrar_rechaza_titulo_demasiado_largo()
    {
        var titulo = new string('a', Solicitud.LONGITUD_MAXIMA_TITULO + 1);

        Should.Throw<ExcepcionReglaNegocio>(() => Crear(titulo: titulo));
    }

    [Fact]
    public void Registrar_rechaza_fecha_de_compromiso_anterior_a_la_creacion()
    {
        Should.Throw<ExcepcionReglaNegocio>(() =>
                Crear(fechaCompromiso: AHORA.AddDays(-1)))
            .Message.ShouldContain("fecha de compromiso");
    }

    [Fact]
    public void Registrar_rechaza_area_invalida()
    {
        Should.Throw<ExcepcionReglaNegocio>(() => Crear(areaId: 0));
    }

    [Fact]
    public void ConstruirCodigo_usa_el_formato_legible_requerido()
    {
        Solicitud.ConstruirCodigo(2026, 7).ShouldBe("SOL-2026-0007");
        Solicitud.ConstruirCodigo(2026, 1234).ShouldBe("SOL-2026-1234");
    }

    [Fact]
    public void CambiarEstado_registra_la_transicion_con_usuario_y_fecha()
    {
        var solicitud = Crear();

        solicitud.CambiarEstado(EstadoSolicitud.EnAnalisis, ANALISTA_ID, RolUsuario.Analista, "Iniciando análisis.", AHORA.AddHours(1));

        solicitud.Estado.ShouldBe(EstadoSolicitud.EnAnalisis);
        solicitud.Historial.Count.ShouldBe(2);
        var ultima = solicitud.Historial.Last();
        ultima.EstadoAnterior.ShouldBe(EstadoSolicitud.Registrada);
        ultima.EstadoNuevo.ShouldBe(EstadoSolicitud.EnAnalisis);
        ultima.UsuarioId.ShouldBe(ANALISTA_ID);
        ultima.Comentario.ShouldBe("Iniciando análisis.");
        ultima.Fecha.ShouldBe(AHORA.AddHours(1));
    }

    [Fact]
    public void CambiarEstado_rechaza_transiciones_no_permitidas()
    {
        var solicitud = Crear();

        Should.Throw<ExcepcionReglaNegocio>(() =>
            solicitud.CambiarEstado(EstadoSolicitud.Cerrada, ANALISTA_ID, RolUsuario.Administrador, "Cierre.", AHORA));
    }

    [Fact]
    public void CambiarEstado_rechaza_el_mismo_estado()
    {
        var solicitud = Crear();

        Should.Throw<ExcepcionReglaNegocio>(() =>
            solicitud.CambiarEstado(EstadoSolicitud.Registrada, ANALISTA_ID, RolUsuario.Analista, null, AHORA));
    }

    [Fact]
    public void No_se_puede_cerrar_sin_comentario_de_resolucion()
    {
        var solicitud = Resuelta();

        var excepcion = Should.Throw<ExcepcionReglaNegocio>(() =>
            solicitud.CambiarEstado(EstadoSolicitud.Cerrada, ANALISTA_ID, RolUsuario.Analista, "  ", AHORA.AddDays(2)));

        excepcion.Message.ShouldContain("comentario de resolución");
        solicitud.Estado.ShouldBe(EstadoSolicitud.Resuelta);
    }

    [Fact]
    public void Cerrar_con_comentario_establece_la_fecha_de_cierre()
    {
        var solicitud = Resuelta();
        var fechaCierre = AHORA.AddDays(2);

        solicitud.CambiarEstado(EstadoSolicitud.Cerrada, ANALISTA_ID, RolUsuario.Analista, "Resuelto y validado.", fechaCierre);

        solicitud.Estado.ShouldBe(EstadoSolicitud.Cerrada);
        solicitud.FechaCierre.ShouldBe(fechaCierre);
    }

    [Theory]
    [InlineData(RolUsuario.Solicitante, false)]
    [InlineData(RolUsuario.Analista, true)]
    [InlineData(RolUsuario.Administrador, true)]
    public void Solo_administrador_y_analista_pueden_reabrir(RolUsuario rol, bool permitido)
    {
        var solicitud = Cerrada();

        if (permitido)
        {
            solicitud.CambiarEstado(EstadoSolicitud.EnProgreso, ANALISTA_ID, rol, "Reabierta.", AHORA.AddDays(3));
            solicitud.Estado.ShouldBe(EstadoSolicitud.EnProgreso);
        }
        else
        {
            Should.Throw<ExcepcionReglaNegocio>(() =>
                solicitud.CambiarEstado(EstadoSolicitud.EnProgreso, SOLICITANTE_ID, rol, "Reabierta.", AHORA.AddDays(3)));
        }
    }

    [Fact]
    public void Una_solicitud_abierta_con_compromiso_vencido_esta_vencida()
    {
        var solicitud = Crear(fechaCompromiso: AHORA.AddDays(3));

        solicitud.EstaVencida(AHORA.AddDays(1)).ShouldBeFalse();
        solicitud.EstaVencida(AHORA.AddDays(4)).ShouldBeTrue();
    }

    [Fact]
    public void Una_solicitud_cerrada_no_se_considera_vencida()
    {
        var solicitud = Cerrada();

        solicitud.EstaVencida(AHORA.AddYears(1)).ShouldBeFalse();
        solicitud.EstaAbierta().ShouldBeFalse();
    }

    [Fact]
    public void Una_solicitud_cerrada_no_se_puede_editar()
    {
        var solicitud = Cerrada();

        Should.Throw<ExcepcionReglaNegocio>(() =>
            solicitud.ActualizarParcial("Nuevo título", null, null, null, null, null));
    }

    [Fact]
    public void La_actualizacion_parcial_solo_cambia_los_campos_enviados()
    {
        var solicitud = Crear();
        var tituloOriginal = solicitud.Titulo;

        solicitud.ActualizarParcial(null, null, PrioridadSolicitud.Critica, null, "https://evidencia.local/1", null);

        solicitud.Titulo.ShouldBe(tituloOriginal);
        solicitud.Prioridad.ShouldBe(PrioridadSolicitud.Critica);
        solicitud.UrlEvidencia.ShouldBe("https://evidencia.local/1");
    }

    [Fact]
    public void La_actualizacion_parcial_rechaza_un_compromiso_anterior_a_la_creacion()
    {
        var solicitud = Crear();

        Should.Throw<ExcepcionReglaNegocio>(() =>
            solicitud.ActualizarParcial(null, null, null, AHORA.AddDays(-5), null, null));
    }

    [Fact]
    public void Asignar_responsable_deja_rastro_en_el_historial()
    {
        var solicitud = Crear();

        solicitud.AsignarResponsable(ANALISTA_ID, ANALISTA_ID, AHORA.AddHours(1));

        solicitud.ResponsableId.ShouldBe(ANALISTA_ID);
        solicitud.Historial.Count.ShouldBe(2);
        solicitud.Historial.Last().Comentario.ShouldBe("Solicitud asignada.");
    }

    [Fact]
    public void Reasignar_responsable_queda_registrado_como_reasignacion()
    {
        var solicitud = Crear();
        solicitud.AsignarResponsable(ANALISTA_ID, ANALISTA_ID, AHORA.AddHours(1));

        solicitud.AsignarResponsable(99, ANALISTA_ID, AHORA.AddHours(2));

        solicitud.ResponsableId.ShouldBe(99);
        solicitud.Historial.Last().Comentario.ShouldBe("Solicitud reasignada.");
    }

    [Fact]
    public void Agregar_comentario_valida_la_longitud()
    {
        var solicitud = Crear();
        var textoLargo = new string('x', Solicitud.LONGITUD_MAXIMA_COMENTARIO + 1);

        Should.Throw<ExcepcionReglaNegocio>(() =>
            solicitud.AgregarComentario(textoLargo, VisibilidadComentario.Publica, SOLICITANTE_ID, AHORA));
    }

    [Fact]
    public void Agregar_comentario_publico_lo_agrega_a_la_coleccion()
    {
        var solicitud = Crear();

        var comentario = solicitud.AgregarComentario("Información adicional.", VisibilidadComentario.Publica, SOLICITANTE_ID, AHORA);

        solicitud.Comentarios.ShouldContain(comentario);
        comentario.Visibilidad.ShouldBe(VisibilidadComentario.Publica);
        comentario.UsuarioId.ShouldBe(SOLICITANTE_ID);
    }

    private static Solicitud Crear(
        string titulo = "No puedo acceder al sistema de reportes",
        string descripcion = "Al iniciar sesión el sistema devuelve un error 500 y bloquea la operación.",
        PrioridadSolicitud prioridad = PrioridadSolicitud.Alta,
        int areaId = AREA_ID,
        DateTime? fechaCompromiso = null) =>
        Solicitud.Registrar(
            "SOL-2026-0001",
            titulo,
            descripcion,
            prioridad,
            areaId,
            TIPO_ID,
            SOLICITANTE_ID,
            fechaCompromiso ?? AHORA.AddDays(5),
            AHORA,
            null,
            1);

    private static Solicitud Resuelta()
    {
        var solicitud = Crear();
        solicitud.CambiarEstado(EstadoSolicitud.EnAnalisis, ANALISTA_ID, RolUsuario.Analista, null, AHORA.AddHours(1));
        solicitud.CambiarEstado(EstadoSolicitud.Resuelta, ANALISTA_ID, RolUsuario.Analista, "Atendido.", AHORA.AddDays(1));
        return solicitud;
    }

    private static Solicitud Cerrada()
    {
        var solicitud = Resuelta();
        solicitud.CambiarEstado(EstadoSolicitud.Cerrada, ANALISTA_ID, RolUsuario.Analista, "Resuelto y validado.", AHORA.AddDays(2));
        return solicitud;
    }
}
