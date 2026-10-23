import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useSesion } from '../sesion/ContextoSesion';
import type {
  ActualizarArea,
  ActualizarEntidad,
  ActualizarOpcionCatalogo,
  ActualizarSolicitud,
  ActualizarTipoSolicitud,
  ActualizarUsuario,
  CrearArea,
  CrearEntidad,
  CrearSolicitud,
  CrearTipoSolicitud,
  CrearUsuario,
  EstadoSolicitud,
  FiltrosEntidad,
  FiltrosSolicitud,
  TipoCatalogoEntidad,
  VisibilidadComentario
} from './tipos';

/** Claves centralizadas de consulta para control y purga de caché */
export const claves = {
  tablero: ['tablero'] as const,
  solicitudes: (filtros: FiltrosSolicitud) => ['solicitudes', filtros] as const,
  solicitud: (id: number) => ['solicitud', id] as const,
  areas: ['catalogos', 'areas'] as const,
  tiposSolicitud: ['catalogos', 'tipos-solicitud'] as const,
  usuarios: ['usuarios'] as const,
  responsables: ['responsables'] as const,
  notificaciones: (soloNoLeidas: boolean) => ['notificaciones', { soloNoLeidas }] as const,
  entidades: (filtros: FiltrosEntidad) => ['entidades', filtros] as const,
  opcionesEntidades: ['entidades', 'opciones'] as const
};

// -----------------------------------------------------------------------------
// Queries (Consultas con TanStack Query)
// -----------------------------------------------------------------------------

export function useResumenTablero() {
  const { cliente, usuario } = useSesion();

  return useQuery({
    queryKey: claves.tablero,
    queryFn: () => cliente.obtenerResumen(),
    enabled: Boolean(usuario)
  });
}

export function useConsultarSolicitudes(filtros: FiltrosSolicitud) {
  const { cliente, usuario } = useSesion();

  return useQuery({
    queryKey: claves.solicitudes(filtros),
    queryFn: () => cliente.consultarSolicitudes(filtros),
    enabled: Boolean(usuario),
    placeholderData: (datosPrevios) => datosPrevios
  });
}

export function useSolicitud(id: number, habilitado: boolean = true) {
  const { cliente, usuario } = useSesion();

  return useQuery({
    queryKey: claves.solicitud(id),
    queryFn: () => cliente.obtenerSolicitud(id),
    enabled: Boolean(usuario && habilitado && Number.isFinite(id) && id > 0)
  });
}

export function useAreas() {
  const { cliente, usuario } = useSesion();

  return useQuery({
    queryKey: claves.areas,
    queryFn: () => cliente.obtenerAreas(),
    enabled: Boolean(usuario),
    staleTime: 1000 * 60 * 5 // Catálogo estático por 5 minutos
  });
}

export function useTiposSolicitud() {
  const { cliente, usuario } = useSesion();

  return useQuery({
    queryKey: claves.tiposSolicitud,
    queryFn: () => cliente.obtenerTiposSolicitud(),
    enabled: Boolean(usuario),
    staleTime: 1000 * 60 * 5
  });
}

export function useUsuarios() {
  const { cliente, usuario } = useSesion();

  return useQuery({
    queryKey: claves.usuarios,
    queryFn: () => cliente.obtenerUsuarios(),
    enabled: Boolean(usuario && usuario.rol === 'Administrador')
  });
}

export function useResponsables() {
  const { cliente, usuario } = useSesion();

  return useQuery({
    queryKey: claves.responsables,
    queryFn: () => cliente.obtenerResponsables(),
    enabled: Boolean(usuario && (usuario.rol === 'Administrador' || usuario.rol === 'Analista')),
    staleTime: 1000 * 60 * 2
  });
}

export function useNotificaciones(soloNoLeidas: boolean, refetchInterval?: number) {
  const { cliente, usuario } = useSesion();

  return useQuery({
    queryKey: claves.notificaciones(soloNoLeidas),
    queryFn: () => cliente.obtenerNotificaciones(soloNoLeidas),
    enabled: Boolean(usuario),
    refetchInterval: refetchInterval ?? false
  });
}

export function useConsultarEntidades(filtros: FiltrosEntidad) {
  const { cliente, usuario } = useSesion();

  return useQuery({
    queryKey: claves.entidades(filtros),
    queryFn: () => cliente.consultarEntidades(filtros),
    enabled: Boolean(usuario),
    placeholderData: (datosPrevios) => datosPrevios
  });
}

export function useOpcionesEntidades() {
  const { cliente, usuario } = useSesion();

  return useQuery({
    queryKey: claves.opcionesEntidades,
    queryFn: () => cliente.obtenerOpcionesEntidades(),
    enabled: Boolean(usuario),
    staleTime: 1000 * 60 * 5
  });
}

// -----------------------------------------------------------------------------
// Mutations (Mutaciones con invalidación automática de caché)
// -----------------------------------------------------------------------------

export function useCrearSolicitudMutation() {
  const { cliente } = useSesion();
  const clienteQuery = useQueryClient();

  return useMutation({
    mutationFn: (peticion: CrearSolicitud) => cliente.crearSolicitud(peticion),
    onSuccess: () => {
      void clienteQuery.invalidateQueries({ queryKey: ['solicitudes'] });
      void clienteQuery.invalidateQueries({ queryKey: claves.tablero });
      void clienteQuery.invalidateQueries({ queryKey: ['notificaciones'] });
    }
  });
}

export function useActualizarSolicitudMutation() {
  const { cliente } = useSesion();
  const clienteQuery = useQueryClient();

  return useMutation({
    mutationFn: ({ id, peticion }: { id: number; peticion: ActualizarSolicitud }) =>
      cliente.actualizarSolicitud(id, peticion),
    onSuccess: (solicitudActualizada) => {
      clienteQuery.setQueryData(claves.solicitud(solicitudActualizada.id), solicitudActualizada);
      void clienteQuery.invalidateQueries({ queryKey: ['solicitudes'] });
      void clienteQuery.invalidateQueries({ queryKey: claves.tablero });
    }
  });
}

export function useCambiarEstadoMutation() {
  const { cliente } = useSesion();
  const clienteQuery = useQueryClient();

  return useMutation({
    mutationFn: ({
      id,
      estado,
      comentario
    }: {
      id: number;
      estado: EstadoSolicitud;
      comentario?: string;
    }) => cliente.cambiarEstado(id, estado, comentario),
    onSuccess: (solicitudActualizada) => {
      clienteQuery.setQueryData(claves.solicitud(solicitudActualizada.id), solicitudActualizada);
      void clienteQuery.invalidateQueries({ queryKey: ['solicitudes'] });
      void clienteQuery.invalidateQueries({ queryKey: claves.tablero });
      void clienteQuery.invalidateQueries({ queryKey: ['notificaciones'] });
    }
  });
}

export function useAsignarResponsableMutation() {
  const { cliente } = useSesion();
  const clienteQuery = useQueryClient();

  return useMutation({
    mutationFn: ({ id, responsableId }: { id: number; responsableId: number }) =>
      cliente.asignarResponsable(id, responsableId),
    onSuccess: (solicitudActualizada) => {
      clienteQuery.setQueryData(claves.solicitud(solicitudActualizada.id), solicitudActualizada);
      void clienteQuery.invalidateQueries({ queryKey: ['solicitudes'] });
      void clienteQuery.invalidateQueries({ queryKey: ['notificaciones'] });
    }
  });
}

export function useComentarMutation() {
  const { cliente } = useSesion();
  const clienteQuery = useQueryClient();

  return useMutation({
    mutationFn: ({
      id,
      texto,
      visibilidad
    }: {
      id: number;
      texto: string;
      visibilidad: VisibilidadComentario;
    }) => cliente.comentar(id, texto, visibilidad),
    onSuccess: (solicitudActualizada) => {
      clienteQuery.setQueryData(claves.solicitud(solicitudActualizada.id), solicitudActualizada);
      void clienteQuery.invalidateQueries({ queryKey: ['solicitudes'] });
    }
  });
}

export function useMarcarNotificacionLeidaMutation() {
  const { cliente } = useSesion();
  const clienteQuery = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => cliente.marcarNotificacionLeida(id),
    onSuccess: () => {
      void clienteQuery.invalidateQueries({ queryKey: ['notificaciones'] });
    }
  });
}

export function useCrearEntidadMutation() {
  const { cliente } = useSesion();
  const clienteQuery = useQueryClient();

  return useMutation({
    mutationFn: (peticion: CrearEntidad) => cliente.crearEntidad(peticion),
    onSuccess: () => {
      void clienteQuery.invalidateQueries({ queryKey: ['entidades'] });
      void clienteQuery.invalidateQueries({ queryKey: claves.opcionesEntidades });
    }
  });
}

export function useActualizarEntidadMutation() {
  const { cliente } = useSesion();
  const clienteQuery = useQueryClient();

  return useMutation({
    mutationFn: ({ id, peticion }: { id: string; peticion: ActualizarEntidad }) =>
      cliente.actualizarEntidad(id, peticion),
    onSuccess: () => {
      void clienteQuery.invalidateQueries({ queryKey: ['entidades'] });
      void clienteQuery.invalidateQueries({ queryKey: claves.opcionesEntidades });
    }
  });
}

export function useEliminarEntidadMutation() {
  const { cliente } = useSesion();
  const clienteQuery = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => cliente.eliminarEntidad(id),
    onSuccess: () => {
      void clienteQuery.invalidateQueries({ queryKey: ['entidades'] });
      void clienteQuery.invalidateQueries({ queryKey: claves.opcionesEntidades });
    }
  });
}

export function useCrearUsuarioMutation() {
  const { cliente } = useSesion();
  const clienteQuery = useQueryClient();

  return useMutation({
    mutationFn: (peticion: CrearUsuario) => cliente.crearUsuario(peticion),
    onSuccess: () => {
      void clienteQuery.invalidateQueries({ queryKey: claves.usuarios });
      void clienteQuery.invalidateQueries({ queryKey: claves.responsables });
    }
  });
}

export function useActualizarUsuarioMutation() {
  const { cliente } = useSesion();
  const clienteQuery = useQueryClient();

  return useMutation({
    mutationFn: ({ id, peticion }: { id: number; peticion: ActualizarUsuario }) =>
      cliente.actualizarUsuario(id, peticion),
    onSuccess: () => {
      void clienteQuery.invalidateQueries({ queryKey: claves.usuarios });
      void clienteQuery.invalidateQueries({ queryKey: claves.responsables });
    }
  });
}

// -----------------------------------------------------------------------------
// Catálogos — Áreas (admin: incluye inactivas)
// -----------------------------------------------------------------------------

export const CLAVE_AREAS_ADMIN = ['catalogos', 'areas', 'admin'] as const;
export const CLAVE_TIPOS_ADMIN = ['catalogos', 'tipos-solicitud', 'admin'] as const;

export function useAreasAdmin() {
  const { cliente, usuario } = useSesion();

  return useQuery({
    queryKey: CLAVE_AREAS_ADMIN,
    queryFn: () => cliente.obtenerAreas(false),
    enabled: Boolean(usuario && usuario.rol === 'Administrador')
  });
}

export function useTiposSolicitudAdmin() {
  const { cliente, usuario } = useSesion();

  return useQuery({
    queryKey: CLAVE_TIPOS_ADMIN,
    queryFn: () => cliente.obtenerTiposSolicitud(false),
    enabled: Boolean(usuario && usuario.rol === 'Administrador')
  });
}

export function useCrearAreaMutation() {
  const { cliente } = useSesion();
  const clienteQuery = useQueryClient();

  return useMutation({
    mutationFn: (peticion: CrearArea) => cliente.crearArea(peticion),
    onSuccess: () => {
      void clienteQuery.invalidateQueries({ queryKey: ['catalogos', 'areas'] });
    }
  });
}

export function useActualizarAreaMutation() {
  const { cliente } = useSesion();
  const clienteQuery = useQueryClient();

  return useMutation({
    mutationFn: ({ id, peticion }: { id: number; peticion: ActualizarArea }) =>
      cliente.actualizarArea(id, peticion),
    onSuccess: () => {
      void clienteQuery.invalidateQueries({ queryKey: ['catalogos', 'areas'] });
    }
  });
}

export function useEliminarAreaMutation() {
  const { cliente } = useSesion();
  const clienteQuery = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => cliente.eliminarArea(id),
    onSuccess: () => {
      void clienteQuery.invalidateQueries({ queryKey: ['catalogos', 'areas'] });
    }
  });
}

export function useCrearTipoSolicitudMutation() {
  const { cliente } = useSesion();
  const clienteQuery = useQueryClient();

  return useMutation({
    mutationFn: (peticion: CrearTipoSolicitud) => cliente.crearTipoSolicitud(peticion),
    onSuccess: () => {
      void clienteQuery.invalidateQueries({ queryKey: ['catalogos', 'tipos-solicitud'] });
    }
  });
}

export function useActualizarTipoSolicitudMutation() {
  const { cliente } = useSesion();
  const clienteQuery = useQueryClient();

  return useMutation({
    mutationFn: ({ id, peticion }: { id: number; peticion: ActualizarTipoSolicitud }) =>
      cliente.actualizarTipoSolicitud(id, peticion),
    onSuccess: () => {
      void clienteQuery.invalidateQueries({ queryKey: ['catalogos', 'tipos-solicitud'] });
    }
  });
}

export function useEliminarTipoSolicitudMutation() {
  const { cliente } = useSesion();
  const clienteQuery = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => cliente.eliminarTipoSolicitud(id),
    onSuccess: () => {
      void clienteQuery.invalidateQueries({ queryKey: ['catalogos', 'tipos-solicitud'] });
    }
  });
}

// -----------------------------------------------------------------------------
// Opciones de Catálogo de Entidades (Categorías, Sectores, Poderes del Estado)
// -----------------------------------------------------------------------------

export function useCrearOpcionCatalogoMutation() {
  const { cliente } = useSesion();
  const clienteQuery = useQueryClient();

  return useMutation({
    mutationFn: ({ tipo, valor }: { tipo: TipoCatalogoEntidad; valor: string }) =>
      cliente.crearOpcionCatalogo(tipo, valor),
    onSuccess: () => {
      void clienteQuery.invalidateQueries({ queryKey: claves.opcionesEntidades });
      void clienteQuery.invalidateQueries({ queryKey: ['entidades'] });
    }
  });
}

export function useActualizarOpcionCatalogoMutation() {
  const { cliente } = useSesion();
  const clienteQuery = useQueryClient();

  return useMutation({
    mutationFn: ({ tipo, peticion }: { tipo: TipoCatalogoEntidad; peticion: ActualizarOpcionCatalogo }) =>
      cliente.actualizarOpcionCatalogo(tipo, peticion),
    onSuccess: () => {
      void clienteQuery.invalidateQueries({ queryKey: claves.opcionesEntidades });
      void clienteQuery.invalidateQueries({ queryKey: ['entidades'] });
    }
  });
}

export function useEliminarOpcionCatalogoMutation() {
  const { cliente } = useSesion();
  const clienteQuery = useQueryClient();

  return useMutation({
    mutationFn: ({ tipo, valor }: { tipo: TipoCatalogoEntidad; valor: string }) =>
      cliente.eliminarOpcionCatalogo(tipo, valor),
    onSuccess: () => {
      void clienteQuery.invalidateQueries({ queryKey: claves.opcionesEntidades });
      void clienteQuery.invalidateQueries({ queryKey: ['entidades'] });
    }
  });
}
