import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Alerta, Boton, Cargando, Tarjeta, Vacio } from '../componentes/Diseno';
import { IconoCampana, IconoCheck } from '../componentes/Iconos';
import { ErrorApi } from '../lib/api';
import { formatearFechaHora } from '../lib/formato';
import type { Notificacion } from '../lib/tipos';
import { useSesion } from '../sesion/ContextoSesion';

export function Notificaciones() {
  const { cliente } = useSesion();
  const [notificaciones, setNotificaciones] = useState<Notificacion[]>([]);
  const [soloNoLeidas, setSoloNoLeidas] = useState(false);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [marcando, setMarcando] = useState<number | null>(null);

  useEffect(() => {
    let vigente = true;
    setCargando(true);

    cliente
      .obtenerNotificaciones(soloNoLeidas)
      .then((datos) => {
        if (vigente) {
          setNotificaciones(datos);
          setError(null);
        }
      })
      .catch((excepcion: unknown) => {
        if (vigente) {
          setError(
            excepcion instanceof ErrorApi
              ? excepcion.message
              : 'No fue posible consultar las notificaciones.'
          );
        }
      })
      .finally(() => {
        if (vigente) {
          setCargando(false);
        }
      });

    return () => {
      vigente = false;
    };
  }, [cliente, soloNoLeidas]);

  const marcarLeida = async (notificacion: Notificacion) => {
    if (notificacion.leida) {
      return;
    }

    setMarcando(notificacion.id);

    try {
      await cliente.marcarNotificacionLeida(notificacion.id);
      setNotificaciones((actuales) =>
        soloNoLeidas
          ? actuales.filter((item) => item.id !== notificacion.id)
          : actuales.map((item) => (item.id === notificacion.id ? { ...item, leida: true } : item))
      );
    } catch (excepcion) {
      setError(
        excepcion instanceof ErrorApi ? excepcion.message : 'No fue posible marcar la notificación.'
      );
    } finally {
      setMarcando(null);
    }
  };

  const pendientes = notificaciones.filter((notificacion) => !notificacion.leida).length;

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <p className="max-w-2xl text-sm text-slate-500">
          Avisos generados por los cambios de estado y las asignaciones de sus solicitudes.
        </p>

        <div className="inline-flex shrink-0 rounded-lg border border-slate-200 bg-white p-1 shadow-sm">
          <button
            type="button"
            onClick={() => setSoloNoLeidas(false)}
            className={`rounded-md px-3 py-1.5 text-sm font-semibold transition ${
              soloNoLeidas ? 'text-slate-500 hover:text-sb-900' : 'bg-sb-900 text-white'
            }`}
          >
            Todas
          </button>
          <button
            type="button"
            onClick={() => setSoloNoLeidas(true)}
            className={`rounded-md px-3 py-1.5 text-sm font-semibold transition ${
              soloNoLeidas ? 'bg-sb-900 text-white' : 'text-slate-500 hover:text-sb-900'
            }`}
          >
            Sin leer{pendientes > 0 && !soloNoLeidas ? ` (${pendientes})` : ''}
          </button>
        </div>
      </div>

      {error && <Alerta tono="error">{error}</Alerta>}

      <Tarjeta
        titulo={soloNoLeidas ? 'Notificaciones sin leer' : 'Historial de notificaciones'}
        descripcion={`${notificaciones.length} aviso${notificaciones.length === 1 ? '' : 's'} en esta vista.`}
      >
        {cargando && notificaciones.length === 0 ? (
          <Cargando mensaje="Consultando notificaciones…" />
        ) : notificaciones.length === 0 ? (
          <Vacio
            titulo={soloNoLeidas ? 'No tiene notificaciones pendientes' : 'Sin notificaciones'}
            descripcion={
              soloNoLeidas
                ? 'Todos los avisos han sido revisados.'
                : 'Los avisos aparecerán cuando sus solicitudes cambien de estado.'
            }
            accion={
              soloNoLeidas ? (
                <Boton variante="secundario" onClick={() => setSoloNoLeidas(false)}>
                  Ver todas
                </Boton>
              ) : (
                <Link to="/consulta">
                  <Boton variante="secundario">Ir a la consulta</Boton>
                </Link>
              )
            }
          />
        ) : (
          <ul className={`divide-y divide-slate-100 ${cargando ? 'opacity-60' : ''}`}>
            {notificaciones.map((notificacion) => (
              <li
                key={notificacion.id}
                className={`flex flex-col gap-3 py-4 sm:flex-row sm:items-start sm:justify-between ${
                  notificacion.leida ? '' : 'bg-acento-suave/40'
                }`}
              >
                <div className="flex min-w-0 items-start gap-3">
                  <span
                    className={`mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-full ${
                      notificacion.leida ? 'bg-slate-100 text-slate-400' : 'bg-acento/10 text-acento'
                    }`}
                  >
                    <IconoCampana className="h-4 w-4" />
                  </span>
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                      <Link
                        to={`/solicitudes/${notificacion.solicitudId}`}
                        className="text-xs font-bold uppercase tracking-wide text-acento hover:underline"
                      >
                        {notificacion.solicitudCodigo}
                      </Link>
                      {!notificacion.leida && (
                        <span className="rounded-full bg-acento px-1.5 py-0.5 text-[10px] font-bold uppercase text-white">
                          Nueva
                        </span>
                      )}
                      <span className="rounded bg-slate-100 px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-slate-500">
                        {notificacion.canal}
                      </span>
                    </div>
                    <p className="mt-1 text-sm font-semibold text-sb-900">{notificacion.asunto}</p>
                    <p className="mt-0.5 text-sm leading-relaxed text-slate-600">
                      {notificacion.mensaje}
                    </p>
                    <p className="mt-1.5 text-xs text-slate-400">
                      {formatearFechaHora(notificacion.fecha)}
                    </p>
                  </div>
                </div>

                <div className="flex shrink-0 items-center gap-2 pl-12 sm:pl-0">
                  <Link to={`/solicitudes/${notificacion.solicitudId}`}>
                    <Boton variante="secundario" tamano="sm">
                      Ver solicitud
                    </Boton>
                  </Link>
                  {!notificacion.leida && (
                    <Boton
                      variante="fantasma"
                      tamano="sm"
                      cargando={marcando === notificacion.id}
                      onClick={() => void marcarLeida(notificacion)}
                      icono={<IconoCheck className="h-4 w-4" />}
                    >
                      Marcar leída
                    </Boton>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </Tarjeta>
    </div>
  );
}
