export type RolUsuario = 'Administrador' | 'Analista' | 'Solicitante';

export type EstadoSolicitud =
  | 'Registrada'
  | 'EnAnalisis'
  | 'EnProgreso'
  | 'EnEsperaDelSolicitante'
  | 'Resuelta'
  | 'Cerrada';

export type PrioridadSolicitud = 'Baja' | 'Media' | 'Alta' | 'Critica';

export type VisibilidadComentario = 'Publica' | 'Interna';

export interface Pagina<T> {
  elementos: T[];
  totalRegistros: number;
  pagina: number;
  tamanoPagina: number;
  totalPaginas: number;
}

export interface UsuarioSesion {
  id: number;
  nombre: string;
  correo: string;
  rol: RolUsuario;
  rolDescripcion: string;
}

export interface RespuestaAutenticacion {
  token: string;
  expiraEn: string;
  usuario: UsuarioSesion;
}

export interface SolicitudResumen {
  id: number;
  codigo: string;
  titulo: string;
  estado: EstadoSolicitud;
  estadoDescripcion: string;
  prioridad: PrioridadSolicitud;
  prioridadDescripcion: string;
  area: string;
  tipoSolicitud: string;
  solicitante: string;
  responsable: string | null;
  fechaCreacion: string;
  fechaCompromiso: string;
  vencida: boolean;
}

export interface EstadoPermitido {
  estado: EstadoSolicitud;
  descripcion: string;
}

export interface HistorialEstado {
  id: number;
  estadoAnterior: EstadoSolicitud | null;
  estadoAnteriorDescripcion: string | null;
  estadoNuevo: EstadoSolicitud;
  estadoNuevoDescripcion: string;
  fecha: string;
  usuario: string;
  comentario: string | null;
}

export interface Comentario {
  id: number;
  texto: string;
  visibilidad: VisibilidadComentario;
  fecha: string;
  usuario: string;
}

export interface SolicitudDetalle {
  id: number;
  codigo: string;
  titulo: string;
  descripcion: string;
  estado: EstadoSolicitud;
  estadoDescripcion: string;
  prioridad: PrioridadSolicitud;
  prioridadDescripcion: string;
  areaId: number;
  area: string;
  tipoSolicitudId: number;
  tipoSolicitud: string;
  solicitanteId: number;
  solicitante: string;
  responsableId: number | null;
  responsable: string | null;
  fechaCreacion: string;
  fechaCompromiso: string;
  fechaCierre: string | null;
  urlEvidencia: string | null;
  vencida: boolean;
  estadosPermitidos: EstadoPermitido[];
  historial: HistorialEstado[];
  comentarios: Comentario[];
}

export interface CrearSolicitud {
  titulo: string;
  descripcion: string;
  prioridad: PrioridadSolicitud;
  areaId: number;
  tipoSolicitudId: number;
  fechaCompromiso: string;
  urlEvidencia?: string | null;
}

export interface ActualizarSolicitud {
  titulo?: string;
  descripcion?: string;
  prioridad?: PrioridadSolicitud;
  fechaCompromiso?: string;
  urlEvidencia?: string | null;
  tipoSolicitudId?: number;
}

export interface FiltrosSolicitud {
  texto?: string;
  estado?: EstadoSolicitud | '';
  prioridad?: PrioridadSolicitud | '';
  areaId?: number | '';
  solicitanteId?: number | '';
  responsableId?: number | '';
  fechaDesde?: string;
  fechaHasta?: string;
  soloVencidas?: boolean;
  pagina?: number;
  tamanoPagina?: number;
}

export interface ConteoSolicitudes {
  clave: string;
  total: number;
}

export interface ResumenTablero {
  total: number;
  abiertas: number;
  cerradas: number;
  vencidas: number;
  porEstado: ConteoSolicitudes[];
  porPrioridad: ConteoSolicitudes[];
  ultimasSolicitudes: SolicitudResumen[];
}

export interface EntidadGubernamental {
  id: string;
  nombre: string;
  categoria: string;
  poderDelEstado: string;
  sector: string;
  activa: boolean;
  fechaCreacion: string;
  fechaActualizacion: string;
}

export interface CrearEntidad {
  nombre: string;
  categoria: string;
  poderDelEstado: string;
  sector: string;
  activa: boolean;
}

export type ActualizarEntidad = Partial<CrearEntidad>;

export interface FiltrosEntidad {
  texto?: string;
  categoria?: string;
  poderDelEstado?: string;
  sector?: string;
  activa?: boolean | '';
  pagina?: number;
  tamanoPagina?: number;
}

export interface OpcionesFiltroEntidades {
  categorias: string[];
  poderesDelEstado: string[];
  sectores: string[];
}

export type TipoCatalogoEntidad = 'categorias' | 'sectores' | 'poderes-del-estado';

export interface ActualizarOpcionCatalogo {
  valorActual: string;
  nuevoValor: string;
}

export interface Area {
  id: number;
  nombre: string;
  activa: boolean;
}

export interface CrearArea {
  nombre: string;
}

export interface ActualizarArea {
  nombre: string;
  activa: boolean;
}

export interface TipoSolicitud {
  id: number;
  nombre: string;
  descripcion: string;
  activo: boolean;
}

export interface CrearTipoSolicitud {
  nombre: string;
  descripcion: string;
}

export interface ActualizarTipoSolicitud {
  nombre: string;
  descripcion: string;
  activo: boolean;
}

export interface Usuario {
  id: number;
  nombre: string;
  correo: string;
  rol: RolUsuario;
  rolDescripcion: string;
  activo: boolean;
  fechaCreacion: string;
}

export interface CrearUsuario {
  nombre: string;
  correo: string;
  contrasena: string;
  rol: RolUsuario;
}

export interface ActualizarUsuario {
  nombre?: string;
  rol?: RolUsuario;
  activo?: boolean;
  contrasena?: string;
}

export interface Notificacion {
  id: number;
  solicitudId: number;
  solicitudCodigo: string;
  asunto: string;
  mensaje: string;
  canal: string;
  estado: string;
  fecha: string;
  leida: boolean;
}
