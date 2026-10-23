import type {
  ActualizarArea,
  ActualizarEntidad,
  ActualizarOpcionCatalogo,
  ActualizarSolicitud,
  ActualizarTipoSolicitud,
  ActualizarUsuario,
  Area,
  CrearArea,
  CrearEntidad,
  CrearSolicitud,
  CrearTipoSolicitud,
  CrearUsuario,
  EntidadGubernamental,
  EstadoSolicitud,
  FiltrosEntidad,
  FiltrosSolicitud,
  Notificacion,
  OpcionesFiltroEntidades,
  Pagina,
  ResumenTablero,
  RespuestaAutenticacion,
  SolicitudDetalle,
  SolicitudResumen,
  TipoCatalogoEntidad,
  TipoSolicitud,
  Usuario,
  VisibilidadComentario
} from './tipos';

const BASE = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '');

/** Error de negocio devuelto por la API (RFC 9457 Problem Details + codigo estable). */
export class ErrorApi extends Error {
  readonly estado: number;
  readonly codigo?: string;
  readonly errores?: Record<string, string[]>;
  readonly trazabilidad?: string;

  constructor(
    estado: number,
    mensaje: string,
    codigo?: string,
    errores?: Record<string, string[]>,
    trazabilidad?: string
  ) {
    super(mensaje);
    this.name = 'ErrorApi';
    this.estado = estado;
    this.codigo = codigo;
    this.errores = errores;
    this.trazabilidad = trazabilidad;
  }
}

let alExpirarSesion: (() => void) | null = null;

/** La sesión avisa al usuario cuando el token deja de ser válido. */
export function configurarExpiracionSesion(manejador: (() => void) | null): void {
  alExpirarSesion = manejador;
}

async function solicitar<T>(ruta: string, token: string | null, opciones: RequestInit = {}): Promise<T> {
  const cabeceras = new Headers(opciones.headers);

  if (opciones.body !== undefined) {
    cabeceras.set('Content-Type', 'application/json');
  }

  if (token) {
    cabeceras.set('Authorization', `Bearer ${token}`);
  }

  let respuesta: Response;

  try {
    respuesta = await fetch(`${BASE}${ruta}`, { ...opciones, headers: cabeceras });
  } catch {
    throw new ErrorApi(0, 'No fue posible comunicarse con el servicio. Verifique que la API esté en ejecución.');
  }

  if (respuesta.status === 401 && token) {
    alExpirarSesion?.();
  }

  if (respuesta.status === 204) {
    return undefined as T;
  }

  const texto = await respuesta.text();
  let cuerpo: unknown = null;

  if (texto) {
    try {
      cuerpo = JSON.parse(texto);
    } catch {
      cuerpo = null;
    }
  }

  if (!respuesta.ok) {
    const problema = (cuerpo ?? {}) as {
      detail?: string;
      title?: string;
      codigo?: string;
      errors?: Record<string, string[]>;
      trazabilidad?: string;
    };

    throw new ErrorApi(
      respuesta.status,
      problema.detail ?? problema.title ?? 'No fue posible completar la operación.',
      problema.codigo,
      problema.errors,
      problema.trazabilidad
    );
  }

  return cuerpo as T;
}

function cadenaConsultaLimpia(valores: Record<string, string | number | boolean | undefined | ''>): string {
  const parametros = new URLSearchParams();

  Object.entries(valores).forEach(([clave, valor]) => {
    if (valor !== undefined && valor !== '') {
      parametros.set(clave, String(valor));
    }
  });

  const cadena = parametros.toString();
  return cadena ? `?${cadena}` : '';
}

export interface ClienteApi {
  iniciarSesion(correo: string, contrasena: string): Promise<RespuestaAutenticacion>;
  consultarSolicitudes(filtros: FiltrosSolicitud): Promise<Pagina<SolicitudResumen>>;
  obtenerSolicitud(id: number): Promise<SolicitudDetalle>;
  crearSolicitud(peticion: CrearSolicitud): Promise<SolicitudDetalle>;
  actualizarSolicitud(id: number, peticion: ActualizarSolicitud): Promise<SolicitudDetalle>;
  cambiarEstado(id: number, estado: EstadoSolicitud, comentario?: string): Promise<SolicitudDetalle>;
  asignarResponsable(id: number, responsableId: number): Promise<SolicitudDetalle>;
  comentar(id: number, texto: string, visibilidad: VisibilidadComentario): Promise<SolicitudDetalle>;
  obtenerResumen(): Promise<ResumenTablero>;
  consultarEntidades(filtros: FiltrosEntidad): Promise<Pagina<EntidadGubernamental>>;
  obtenerEntidad(id: string): Promise<EntidadGubernamental>;
  crearEntidad(peticion: CrearEntidad): Promise<EntidadGubernamental>;
  actualizarEntidad(id: string, peticion: ActualizarEntidad): Promise<EntidadGubernamental>;
  eliminarEntidad(id: string): Promise<void>;
  obtenerOpcionesEntidades(): Promise<OpcionesFiltroEntidades>;
  crearOpcionCatalogo(tipo: TipoCatalogoEntidad, valor: string): Promise<void>;
  actualizarOpcionCatalogo(tipo: TipoCatalogoEntidad, peticion: ActualizarOpcionCatalogo): Promise<void>;
  eliminarOpcionCatalogo(tipo: TipoCatalogoEntidad, valor: string): Promise<void>;
  obtenerAreas(soloActivas?: boolean): Promise<Area[]>;
  crearArea(peticion: CrearArea): Promise<Area>;
  actualizarArea(id: number, peticion: ActualizarArea): Promise<Area>;
  eliminarArea(id: number): Promise<void>;
  obtenerTiposSolicitud(soloActivos?: boolean): Promise<TipoSolicitud[]>;
  crearTipoSolicitud(peticion: CrearTipoSolicitud): Promise<TipoSolicitud>;
  actualizarTipoSolicitud(id: number, peticion: ActualizarTipoSolicitud): Promise<TipoSolicitud>;
  eliminarTipoSolicitud(id: number): Promise<void>;
  obtenerUsuarios(): Promise<Usuario[]>;
  obtenerResponsables(): Promise<Usuario[]>;
  crearUsuario(peticion: CrearUsuario): Promise<Usuario>;
  actualizarUsuario(id: number, peticion: ActualizarUsuario): Promise<Usuario>;
  obtenerNotificaciones(soloNoLeidas: boolean): Promise<Notificacion[]>;
  marcarNotificacionLeida(id: number): Promise<void>;
}

/** Cliente atado al token de la sesión actual. */
export function crearCliente(token: string | null): ClienteApi {
  const pedir = <T>(ruta: string, opciones?: RequestInit) => solicitar<T>(ruta, token, opciones);

  return {
    iniciarSesion: (correo, contrasena) =>
      solicitar<RespuestaAutenticacion>('/api/auth/login', null, {
        method: 'POST',
        body: JSON.stringify({ correo, contrasena })
      }),

    consultarSolicitudes: (filtros) =>
      pedir<Pagina<SolicitudResumen>>(
        `/api/solicitudes${cadenaConsultaLimpia({
          texto: filtros.texto?.trim(),
          estado: filtros.estado,
          prioridad: filtros.prioridad,
          areaId: filtros.areaId,
          solicitanteId: filtros.solicitanteId,
          responsableId: filtros.responsableId,
          fechaDesde: filtros.fechaDesde,
          fechaHasta: filtros.fechaHasta,
          soloVencidas: filtros.soloVencidas,
          pagina: filtros.pagina,
          tamanoPagina: filtros.tamanoPagina
        })}`
      ),

    obtenerSolicitud: (id) => pedir<SolicitudDetalle>(`/api/solicitudes/${id}`),

    crearSolicitud: (peticion) =>
      pedir<SolicitudDetalle>('/api/solicitudes', {
        method: 'POST',
        body: JSON.stringify(peticion)
      }),

    actualizarSolicitud: (id, peticion) =>
      pedir<SolicitudDetalle>(`/api/solicitudes/${id}`, {
        method: 'PATCH',
        body: JSON.stringify(peticion)
      }),

    cambiarEstado: (id, estado, comentario) =>
      pedir<SolicitudDetalle>(`/api/solicitudes/${id}/estado`, {
        method: 'PATCH',
        body: JSON.stringify({ estado, comentario: comentario?.trim() ? comentario.trim() : null })
      }),

    asignarResponsable: (id, responsableId) =>
      pedir<SolicitudDetalle>(`/api/solicitudes/${id}/asignacion`, {
        method: 'PATCH',
        body: JSON.stringify({ responsableId })
      }),

    comentar: (id, texto, visibilidad) =>
      pedir<SolicitudDetalle>(`/api/solicitudes/${id}/comentarios`, {
        method: 'POST',
        body: JSON.stringify({ texto, visibilidad })
      }),

    obtenerResumen: () => pedir<ResumenTablero>('/api/dashboard/resumen'),

    consultarEntidades: (filtros) =>
      pedir<Pagina<EntidadGubernamental>>(
        `/api/entidades-gubernamentales${cadenaConsultaLimpia({
          texto: filtros.texto?.trim(),
          categoria: filtros.categoria,
          poderDelEstado: filtros.poderDelEstado,
          sector: filtros.sector,
          activa: filtros.activa,
          pagina: filtros.pagina,
          tamanoPagina: filtros.tamanoPagina
        })}`
      ),

    obtenerEntidad: (id) => pedir<EntidadGubernamental>(`/api/entidades-gubernamentales/${id}`),

    crearEntidad: (peticion) =>
      pedir<EntidadGubernamental>('/api/entidades-gubernamentales', {
        method: 'POST',
        body: JSON.stringify(peticion)
      }),

    actualizarEntidad: (id, peticion) =>
      pedir<EntidadGubernamental>(`/api/entidades-gubernamentales/${id}`, {
        method: 'PATCH',
        body: JSON.stringify(peticion)
      }),

    eliminarEntidad: (id) =>
      pedir<void>(`/api/entidades-gubernamentales/${id}`, { method: 'DELETE' }),

    obtenerOpcionesEntidades: () =>
      pedir<OpcionesFiltroEntidades>('/api/entidades-gubernamentales/opciones-filtro'),

    crearOpcionCatalogo: (tipo, valor) =>
      pedir<void>(`/api/entidades-gubernamentales/catalogos/${tipo}`, {
        method: 'POST',
        body: JSON.stringify({ valor })
      }),

    actualizarOpcionCatalogo: (tipo, peticion) =>
      pedir<void>(`/api/entidades-gubernamentales/catalogos/${tipo}`, {
        method: 'PUT',
        body: JSON.stringify(peticion)
      }),

    eliminarOpcionCatalogo: (tipo, valor) =>
      pedir<void>(`/api/entidades-gubernamentales/catalogos/${tipo}?valor=${encodeURIComponent(valor)}`, {
        method: 'DELETE'
      }),

    obtenerAreas: (soloActivas = true) =>
      pedir<Area[]>(`/api/catalogos/areas?soloActivas=${soloActivas}`),

    crearArea: (peticion) =>
      pedir<Area>('/api/catalogos/areas', {
        method: 'POST',
        body: JSON.stringify(peticion)
      }),

    actualizarArea: (id, peticion) =>
      pedir<Area>(`/api/catalogos/areas/${id}`, {
        method: 'PUT',
        body: JSON.stringify(peticion)
      }),

    eliminarArea: (id) =>
      pedir<void>(`/api/catalogos/areas/${id}`, { method: 'DELETE' }),

    obtenerTiposSolicitud: (soloActivos = true) =>
      pedir<TipoSolicitud[]>(`/api/catalogos/tipos-solicitud?soloActivos=${soloActivos}`),

    crearTipoSolicitud: (peticion) =>
      pedir<TipoSolicitud>('/api/catalogos/tipos-solicitud', {
        method: 'POST',
        body: JSON.stringify(peticion)
      }),

    actualizarTipoSolicitud: (id, peticion) =>
      pedir<TipoSolicitud>(`/api/catalogos/tipos-solicitud/${id}`, {
        method: 'PUT',
        body: JSON.stringify(peticion)
      }),

    eliminarTipoSolicitud: (id) =>
      pedir<void>(`/api/catalogos/tipos-solicitud/${id}`, { method: 'DELETE' }),

    obtenerUsuarios: () => pedir<Usuario[]>('/api/usuarios'),

    obtenerResponsables: () => pedir<Usuario[]>('/api/usuarios/responsables'),

    crearUsuario: (peticion) =>
      pedir<Usuario>('/api/usuarios', {
        method: 'POST',
        body: JSON.stringify(peticion)
      }),

    actualizarUsuario: (id, peticion) =>
      pedir<Usuario>(`/api/usuarios/${id}`, {
        method: 'PATCH',
        body: JSON.stringify(peticion)
      }),

    obtenerNotificaciones: (soloNoLeidas) =>
      pedir<Notificacion[]>(`/api/notificaciones?soloNoLeidas=${soloNoLeidas}`),

    marcarNotificacionLeida: (id) =>
      pedir<void>(`/api/notificaciones/${id}/leida`, { method: 'PATCH' })
  };
}
