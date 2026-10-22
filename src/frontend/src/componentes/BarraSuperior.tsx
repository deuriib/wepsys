import { useEffect, useRef, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useSesion } from '../sesion/ContextoSesion';
import type { Notificacion } from '../lib/tipos';
import { formatearFechaHora, iniciales, tituloRol } from '../lib/formato';
import { IconoCampana, IconoChevron, IconoMenu } from './Iconos';
import { useMarcarNotificacionLeidaMutation, useNotificaciones } from '../lib/consultas';

const TITULOS: Array<[RegExp, string]> = [
  [/^\/$/, 'Inicio'],
  [/^\/consulta/, 'Consulta de solicitudes'],
  [/^\/crear/, 'Crear registro'],
  [/^\/solicitudes\/\d+/, 'Detalle de la solicitud'],
  [/^\/entidades/, 'Entidades gubernamentales'],
  [/^\/usuarios/, 'Usuarios y roles'],
  [/^\/notificaciones/, 'Notificaciones']
];

function tituloDeRuta(ruta: string): string {
  return TITULOS.find(([patron]) => patron.test(ruta))?.[1] ?? 'Gestión de solicitudes';
}

export function BarraSuperior({ onAbrirMenu }: { onAbrirMenu: () => void }) {
  const { usuario } = useSesion();
  const ubicacion = useLocation();
  const navegar = useNavigate();
  const [abierto, setAbierto] = useState(false);
  const contenedor = useRef<HTMLDivElement | null>(null);

  const { data: notificaciones = [] } = useNotificaciones(true, 60_000);
  const mutacionMarcar = useMarcarNotificacionLeidaMutation();

  useEffect(() => {
    setAbierto(false);
  }, [ubicacion.pathname]);

  useEffect(() => {
    if (!abierto) {
      return;
    }

    const manejarClic = (evento: MouseEvent) => {
      if (contenedor.current && !contenedor.current.contains(evento.target as Node)) {
        setAbierto(false);
      }
    };

    document.addEventListener('mousedown', manejarClic);
    return () => document.removeEventListener('mousedown', manejarClic);
  }, [abierto]);

  const abrirNotificacion = async (notificacion: Notificacion) => {
    setAbierto(false);

    try {
      if (!notificacion.leida) {
        await mutacionMarcar.mutateAsync(notificacion.id);
      }
    } catch {
      // Si falla el marcado, igual navegamos: la acción principal es abrir el caso.
    }

    navegar(`/solicitudes/${notificacion.solicitudId}`);
  };

  if (!usuario) {
    return null;
  }

  return (
    <header className="flex h-[88px] items-center justify-between gap-4 px-5 lg:px-10">
      <div className="flex min-w-0 items-center gap-3">
        <button
          type="button"
          onClick={onAbrirMenu}
          className="rounded-lg p-2 text-white transition hover:bg-white/10 lg:hidden"
          aria-label="Abrir menú"
        >
          <IconoMenu />
        </button>
        <div className="min-w-0">
          <h1 className="truncate text-xl font-bold tracking-tight text-white lg:text-2xl">
            {tituloDeRuta(ubicacion.pathname)}
          </h1>
          <p className="hidden text-xs text-slate-300 sm:block">
            Superintendencia de Bancos · República Dominicana
          </p>
        </div>
      </div>

      <div className="flex items-center gap-2 sm:gap-4">
        <div className="relative" ref={contenedor}>
          <button
            type="button"
            onClick={() => setAbierto((valor) => !valor)}
            className="relative rounded-full border border-white/15 bg-white/10 p-2.5 text-white shadow-sm transition hover:border-white/30 hover:bg-white/15"
            aria-label={`Notificaciones${notificaciones.length > 0 ? `: ${notificaciones.length} sin leer` : ''}`}
            aria-expanded={abierto}
          >
            <IconoCampana />
            {notificaciones.length > 0 && (
              <span className="absolute -right-1 -top-1 flex h-5 min-w-5 items-center justify-center rounded-full bg-acento px-1 text-[11px] font-bold text-white">
                {notificaciones.length > 9 ? '9+' : notificaciones.length}
              </span>
            )}
          </button>

          {abierto && (
            <div className="absolute right-0 z-40 mt-2 w-[min(22rem,calc(100vw-2.5rem))] animate-aparecer overflow-hidden rounded-xl border border-slate-200 bg-white shadow-flotante">
              <div className="flex items-center justify-between border-b border-slate-100 px-4 py-3">
                <p className="text-sm font-semibold text-sb-900">Notificaciones sin leer</p>
                <button
                  type="button"
                  className="text-xs font-semibold text-acento hover:underline"
                  onClick={() => {
                    setAbierto(false);
                    navegar('/notificaciones');
                  }}
                >
                  Ver todas
                </button>
              </div>

              {notificaciones.length === 0 ? (
                <p className="px-4 py-8 text-center text-sm text-slate-500">No tiene notificaciones pendientes.</p>
              ) : (
                <ul className="max-h-80 divide-y divide-slate-100 overflow-y-auto">
                  {notificaciones.slice(0, 6).map((notificacion) => (
                    <li key={notificacion.id}>
                      <button
                        type="button"
                        onClick={() => void abrirNotificacion(notificacion)}
                        className="block w-full px-4 py-3 text-left transition hover:bg-slate-50"
                      >
                        <span className="flex items-center justify-between gap-2">
                          <span className="truncate text-xs font-semibold text-acento">
                            {notificacion.solicitudCodigo}
                          </span>
                          <span className="shrink-0 text-[11px] text-slate-400">
                            {formatearFechaHora(notificacion.fecha)}
                          </span>
                        </span>
                        <span className="mt-1 block text-sm font-medium text-sb-900">{notificacion.asunto}</span>
                        <span className="mt-0.5 block line-clamp-2 text-xs text-slate-500">{notificacion.mensaje}</span>
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          )}
        </div>

        <div className="hidden h-9 w-px bg-white/15 sm:block" />

        <div className="flex items-center gap-2.5">
          <span className="flex h-10 w-10 items-center justify-center rounded-full bg-acento text-sm font-bold text-white">
            {iniciales(usuario.nombre)}
          </span>
          <div className="hidden leading-tight sm:block">
            <p className="max-w-[180px] truncate text-sm font-semibold text-white">{usuario.nombre}</p>
            <p className="flex items-center gap-1 text-xs text-slate-300">
              {tituloRol(usuario.rol)}
              <IconoChevron className="h-3 w-3" />
            </p>
          </div>
        </div>
      </div>
    </header>
  );
}
