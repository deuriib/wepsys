using SB.GestionSolicitudes.Dominio.Excepciones;
using SB.GestionSolicitudes.Dominio.Usuarios;

namespace SB.GestionSolicitudes.Dominio.Solicitudes;

/// <summary>
/// Aggregate root of the exercise. It owns every invariant related to the
/// lifecycle of a request, so the API and the persistence layer stay thin.
/// </summary>
public class Solicitud
{
    public const int LONGITUD_MAXIMA_TITULO = 200;
    public const int LONGITUD_MAXIMA_DESCRIPCION = 4000;
    public const int LONGITUD_MAXIMA_URL_EVIDENCIA = 500;
    public const int LONGITUD_MAXIMA_COMENTARIO = 2000;
    public const string PREFIJO_CODIGO = "SOL";
    public const int DIGITOS_SECUENCIA = 4;

    public int Id { get; private set; }

    public string Codigo { get; private set; } = string.Empty;

    public string Titulo { get; private set; } = string.Empty;

    public string Descripcion { get; private set; } = string.Empty;

    public PrioridadSolicitud Prioridad { get; private set; }

    public EstadoSolicitud Estado { get; private set; }

    public DateTime FechaCreacion { get; private set; }

    public DateTime FechaCompromiso { get; private set; }

    public DateTime? FechaCierre { get; private set; }

    public int AreaId { get; private set; }

    public Catalogos.Area? Area { get; private set; }

    public int TipoSolicitudId { get; private set; }

    public Catalogos.TipoSolicitud? TipoSolicitud { get; private set; }

    public int SolicitanteId { get; private set; }

    public Usuario? Solicitante { get; private set; }

    public int? ResponsableId { get; private set; }

    public Usuario? Responsable { get; private set; }

    public string? UrlEvidencia { get; private set; }

    public int Secuencia { get; private set; }

    public ICollection<HistorialEstado> Historial { get; private set; } = new List<HistorialEstado>();

    public ICollection<Comentario> Comentarios { get; private set; } = new List<Comentario>();

    /// <summary>
    /// Creates a request in its initial state. The readable code is assigned
    /// by the application service once the sequence for the year is known.
    /// </summary>
    public static Solicitud Registrar(
        string codigo,
        string titulo,
        string descripcion,
        PrioridadSolicitud prioridad,
        int areaId,
        int tipoSolicitudId,
        int solicitanteId,
        DateTime fechaCompromiso,
        DateTime fechaCreacion,
        string? urlEvidencia,
        int secuencia)
    {
        ValidarTextoRequerido(titulo, nameof(titulo), LONGITUD_MAXIMA_TITULO, "El título es obligatorio.");
        ValidarTextoRequerido(descripcion, nameof(descripcion), LONGITUD_MAXIMA_DESCRIPCION, "La descripción es obligatoria.");
        ValidarUrlEvidencia(urlEvidencia);

        if (!Enum.IsDefined(prioridad))
        {
            throw new ExcepcionReglaNegocio("La prioridad indicada no es válida.");
        }

        if (areaId <= 0)
        {
            throw new ExcepcionReglaNegocio("El área solicitante es obligatoria.");
        }

        if (tipoSolicitudId <= 0)
        {
            throw new ExcepcionReglaNegocio("El tipo de solicitud es obligatorio.");
        }

        if (solicitanteId <= 0)
        {
            throw new ExcepcionReglaNegocio("El usuario solicitante es obligatorio.");
        }

        if (fechaCompromiso < fechaCreacion)
        {
            throw new ExcepcionReglaNegocio("La fecha de compromiso no puede ser anterior a la fecha de creación.");
        }

        if (secuencia <= 0)
        {
            throw new ExcepcionReglaNegocio("La secuencia de la solicitud debe ser mayor que cero.");
        }

        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ExcepcionReglaNegocio("El código de la solicitud es obligatorio.");
        }

        var solicitud = new Solicitud
        {
            Codigo = codigo.Trim(),
            Titulo = titulo.Trim(),
            Descripcion = descripcion.Trim(),
            Prioridad = prioridad,
            Estado = EstadoSolicitud.Registrada,
            FechaCreacion = fechaCreacion,
            FechaCompromiso = fechaCompromiso,
            AreaId = areaId,
            TipoSolicitudId = tipoSolicitudId,
            SolicitanteId = solicitanteId,
            UrlEvidencia = string.IsNullOrWhiteSpace(urlEvidencia) ? null : urlEvidencia.Trim(),
            Secuencia = secuencia
        };

        solicitud.Historial.Add(new HistorialEstado
        {
            EstadoAnterior = null,
            EstadoNuevo = EstadoSolicitud.Registrada,
            Fecha = fechaCreacion,
            UsuarioId = solicitanteId,
            Comentario = "Solicitud registrada."
        });

        return solicitud;
    }

    public static string ConstruirCodigo(int anio, int secuencia) =>
        $"{PREFIJO_CODIGO}-{anio}-{secuencia.ToString($"D{DIGITOS_SECUENCIA}")}";

    /// <summary>
    /// Partial update. Only the values provided (non-null) are replaced, which
    /// keeps the endpoint idempotent for the fields the client does not own.
    /// </summary>
    public void ActualizarParcial(
        string? titulo,
        string? descripcion,
        PrioridadSolicitud? prioridad,
        DateTime? fechaCompromiso,
        string? urlEvidencia,
        int? tipoSolicitudId)
    {
        if (Estado == EstadoSolicitud.Cerrada)
        {
            throw new ExcepcionReglaNegocio("Una solicitud cerrada no se puede editar. Debe reabrirse primero.");
        }

        if (titulo is not null)
        {
            ValidarTextoRequerido(titulo, nameof(titulo), LONGITUD_MAXIMA_TITULO, "El título es obligatorio.");
            Titulo = titulo.Trim();
        }

        if (descripcion is not null)
        {
            ValidarTextoRequerido(descripcion, nameof(descripcion), LONGITUD_MAXIMA_DESCRIPCION, "La descripción es obligatoria.");
            Descripcion = descripcion.Trim();
        }

        if (prioridad is not null)
        {
            if (!Enum.IsDefined(prioridad.Value))
            {
                throw new ExcepcionReglaNegocio("La prioridad indicada no es válida.");
            }

            Prioridad = prioridad.Value;
        }

        if (fechaCompromiso is not null)
        {
            if (fechaCompromiso.Value < FechaCreacion)
            {
                throw new ExcepcionReglaNegocio("La fecha de compromiso no puede ser anterior a la fecha de creación.");
            }

            FechaCompromiso = fechaCompromiso.Value;
        }

        if (tipoSolicitudId is not null)
        {
            if (tipoSolicitudId.Value <= 0)
            {
                throw new ExcepcionReglaNegocio("El tipo de solicitud es obligatorio.");
            }

            TipoSolicitudId = tipoSolicitudId.Value;
        }

        if (urlEvidencia is not null)
        {
            ValidarUrlEvidencia(urlEvidencia);
            UrlEvidencia = string.IsNullOrWhiteSpace(urlEvidencia) ? null : urlEvidencia.Trim();
        }
    }

    /// <summary>
    /// Assigns (or reassigns) the technical owner. Reassignment is allowed until
    /// the request is closed, which preserves the audit trail.
    /// </summary>
    public HistorialEstado AsignarResponsable(int responsableId, int usuarioQueAsigna, DateTime fecha)
    {
        if (responsableId <= 0)
        {
            throw new ExcepcionReglaNegocio("El responsable técnico es obligatorio.");
        }

        if (Estado == EstadoSolicitud.Cerrada)
        {
            throw new ExcepcionReglaNegocio("Una solicitud cerrada no se puede reasignar.");
        }

        var esReasignacion = ResponsableId is not null && ResponsableId != responsableId;
        ResponsableId = responsableId;

        return RegistrarHistorico(
            Estado,
            usuarioQueAsigna,
            esReasignacion ? "Solicitud reasignada." : "Solicitud asignada.",
            fecha);
    }

    /// <summary>
    /// Moves the request through the workflow, enforcing the transition map and
    /// the closing rule (a closing comment is mandatory).
    /// </summary>
    public HistorialEstado CambiarEstado(
        EstadoSolicitud nuevoEstado,
        int usuarioId,
        RolUsuario rolUsuario,
        string? comentario,
        DateTime fecha)
    {
        if (!Enum.IsDefined(nuevoEstado))
        {
            throw new ExcepcionReglaNegocio("El estado indicado no es válido.");
        }

        if (nuevoEstado == Estado)
        {
            throw new ExcepcionReglaNegocio($"La solicitud ya se encuentra en estado {Estado}.");
        }

        if (!FlujoEstadoSolicitud.EsTransicionPermitida(Estado, nuevoEstado))
        {
            throw new ExcepcionReglaNegocio($"No se permite pasar de {Estado} a {nuevoEstado}.");
        }

        if (Estado == EstadoSolicitud.Cerrada && !UsuarioPuedeReabrir(rolUsuario))
        {
            throw new ExcepcionReglaNegocio("Solo un administrador o un analista puede reabrir una solicitud cerrada.");
        }

        if (nuevoEstado == EstadoSolicitud.Cerrada && string.IsNullOrWhiteSpace(comentario))
        {
            throw new ExcepcionReglaNegocio("No se puede cerrar una solicitud sin un comentario de resolución.");
        }

        if (comentario is not null && comentario.Length > LONGITUD_MAXIMA_COMENTARIO)
        {
            throw new ExcepcionReglaNegocio($"El comentario no puede superar {LONGITUD_MAXIMA_COMENTARIO} caracteres.");
        }

        var estadoAnterior = Estado;
        Estado = nuevoEstado;
        FechaCierre = nuevoEstado == EstadoSolicitud.Cerrada ? fecha : null;

        var historial = new HistorialEstado
        {
            EstadoAnterior = estadoAnterior,
            EstadoNuevo = nuevoEstado,
            Fecha = fecha,
            UsuarioId = usuarioId,
            Comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim()
        };

        Historial.Add(historial);
        return historial;
    }

    public Comentario AgregarComentario(
        string texto,
        VisibilidadComentario visibilidad,
        int usuarioId,
        DateTime fecha)
    {
        ValidarTextoRequerido(texto, nameof(texto), LONGITUD_MAXIMA_COMENTARIO, "El comentario no puede estar vacío.");

        var comentario = new Comentario
        {
            SolicitudId = Id,
            Texto = texto.Trim(),
            Visibilidad = visibilidad,
            UsuarioId = usuarioId,
            Fecha = fecha
        };

        Comentarios.Add(comentario);
        return comentario;
    }

    public bool EstaVencida(DateTime fechaReferencia) =>
        FechaCompromiso < fechaReferencia &&
        Estado is not (EstadoSolicitud.Resuelta or EstadoSolicitud.Cerrada);

    public bool EstaAbierta() =>
        Estado is not (EstadoSolicitud.Resuelta or EstadoSolicitud.Cerrada);

    public static bool UsuarioPuedeReabrir(RolUsuario rolUsuario) =>
        rolUsuario is RolUsuario.Administrador or RolUsuario.Analista;

    private HistorialEstado RegistrarHistorico(
        EstadoSolicitud estado,
        int usuarioId,
        string comentario,
        DateTime fecha)
    {
        var historial = new HistorialEstado
        {
            EstadoAnterior = estado,
            EstadoNuevo = estado,
            Fecha = fecha,
            UsuarioId = usuarioId,
            Comentario = comentario
        };

        Historial.Add(historial);
        return historial;
    }

    private static void ValidarTextoRequerido(string valor, string nombre, int longitudMaxima, string mensaje)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ExcepcionReglaNegocio(mensaje);
        }

        if (valor.Trim().Length > longitudMaxima)
        {
            throw new ExcepcionReglaNegocio($"El campo {nombre} no puede superar {longitudMaxima} caracteres.");
        }
    }

    private static void ValidarUrlEvidencia(string? urlEvidencia)
    {
        if (string.IsNullOrWhiteSpace(urlEvidencia))
        {
            return;
        }

        if (urlEvidencia.Trim().Length > LONGITUD_MAXIMA_URL_EVIDENCIA)
        {
            throw new ExcepcionReglaNegocio($"La referencia de evidencia no puede superar {LONGITUD_MAXIMA_URL_EVIDENCIA} caracteres.");
        }
    }
}
