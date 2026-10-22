import type { EstadoSolicitud, PrioridadSolicitud, RolUsuario } from '../lib/tipos';
import { tituloEstado, tituloPrioridad, tituloRol } from '../lib/formato';

const ESTILOS_ESTADO: Record<EstadoSolicitud, string> = {
  Registrada: 'border-sky-200 bg-sky-50 text-sky-700',
  EnAnalisis: 'border-amber-200 bg-amber-50 text-amber-800',
  EnProgreso: 'border-indigo-200 bg-indigo-50 text-indigo-700',
  EnEsperaDelSolicitante: 'border-violet-200 bg-violet-50 text-violet-700',
  Resuelta: 'border-emerald-200 bg-emerald-50 text-emerald-700',
  Cerrada: 'border-slate-300 bg-slate-100 text-slate-600'
};

const ESTILOS_PRIORIDAD: Record<PrioridadSolicitud, string> = {
  Baja: 'border-slate-200 bg-slate-50 text-slate-600',
  Media: 'border-sky-200 bg-sky-50 text-sky-700',
  Alta: 'border-amber-200 bg-amber-50 text-amber-800',
  Critica: 'border-red-200 bg-red-50 text-red-700'
};

const ESTILOS_ROL: Record<RolUsuario, string> = {
  Administrador: 'border-sb-700/20 bg-sb-900/5 text-sb-800',
  Analista: 'border-sky-200 bg-sky-50 text-sky-700',
  Solicitante: 'border-slate-200 bg-slate-50 text-slate-600'
};

const BASE = 'inline-flex items-center gap-1.5 whitespace-nowrap rounded-full border px-2.5 py-1 text-xs font-semibold';

export function InsigniaEstado({ estado }: { estado: EstadoSolicitud }) {
  return <span className={`${BASE} ${ESTILOS_ESTADO[estado]}`}>{tituloEstado(estado)}</span>;
}

export function InsigniaPrioridad({ prioridad }: { prioridad: PrioridadSolicitud }) {
  return (
    <span className={`${BASE} ${ESTILOS_PRIORIDAD[prioridad]}`}>
      {prioridad === 'Critica' && <span aria-hidden="true">●</span>}
      {tituloPrioridad(prioridad)}
    </span>
  );
}

export function InsigniaRol({ rol }: { rol: RolUsuario }) {
  return <span className={`${BASE} ${ESTILOS_ROL[rol]}`}>{tituloRol(rol)}</span>;
}

export function InsigniaActiva({ activa }: { activa: boolean }) {
  return (
    <span
      className={`${BASE} ${
        activa ? 'border-emerald-200 bg-emerald-50 text-emerald-700' : 'border-slate-200 bg-slate-50 text-slate-500'
      }`}
    >
      {activa ? 'Activa' : 'Inactiva'}
    </span>
  );
}

export function InsigniaVencida({ vencida }: { vencida: boolean }) {
  if (!vencida) {
    return null;
  }

  return <span className={`${BASE} border-red-200 bg-red-50 text-red-700`}>Vencida</span>;
}
