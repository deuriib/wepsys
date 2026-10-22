import { useCallback, useEffect, useState } from 'react';
import type { FormEvent, ReactNode } from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import {
  Alerta,
  AreaTexto,
  Boton,
  Campo,
  Seleccion,
  Tarjeta,
  Vacio,
  Cargando
} from '../componentes/Diseno';
import { InsigniaEstado, InsigniaPrioridad, InsigniaVencida } from '../componentes/Insignias';
import { IconoEditar, IconoVolver } from '../componentes/Iconos';
import { ErrorApi } from '../lib/api';
import { formatearFecha, formatearFechaHora, tituloEstado } from '../lib/formato';
import type {
  Comentario,
  EstadoSolicitud,
  SolicitudDetalle,
  Usuario,
  VisibilidadComentario
} from '../lib/tipos';
import { useSesion } from '../sesion/ContextoSesion';
import { ModalEdicion } from './ModalEdicion';

const COLORES_PUNTO: Record<EstadoSolicitud, string> = {
  Registrada: 'bg-sky-500',
  EnAnalisis: 'bg-amber-500',
  EnProgreso: 'bg-indigo-500',
  EnEsperaDelSolicitante: 'bg-violet-500',
  Resuelta: 'bg-emerald-500',
  Cerrada: 'bg-slate-400'
};

interface Aviso {
  tono: 'exito' | 'error' | 'info';
  texto: string;
}

function Dato({ etiqueta, children }: { etiqueta: string; children: ReactNode }) {
  return (
    <div>
      <dt className="text-xs font-semibold uppercase tracking-wide text-slate-400">{etiqueta}</dt>
      <dd className="mt-1 text-sm text-sb-900">{children}</dd>
    </div>
  );
}

export function DetalleSolicitud() {
  const { id } = useParams<{ id: string }>();
  const solicitudId = Number(id);
  const { cliente, usuario } = useSesion();
  const ubicacion = useLocation();

  const [solicitud, setSolicitud] = useState<SolicitudDetalle | null>(null);
  const [responsables, setResponsables] = useState<Usuario[]>([]);
  const [cargando, setCargando] = useState(true);
  const [errorCarga, setErrorCarga] = useState<string | null>(null);
  const [aviso, setAviso] = useState<Aviso | null>(() =>
    (ubicacion.state as { creada?: boolean } | null)?.creada
      ? { tono: 'exito', texto: 'La solicitud se registró correctamente.' }
      : null
  );

  const [nuevoEstado, setNuevoEstado] = useState<EstadoSolicitud | ''>('');
  const [comentarioEstado, setComentarioEstado] = useState('');
  const [responsableId, setResponsableId] = useState('');
  const [textoComentario, setTextoComentario] = useState('');
  const [visibilidad, setVisibilidad] = useState<VisibilidadComentario>('Publica');
  const [ocupado, setOcupado] = useState<'estado' | 'asignacion' | 'comentario' | null>(null);
  const [modalEdicion, setModalEdicion] = useState(false);

  const esAdministrador = usuario?.rol === 'Administrador';
  const puedeGestionar = esAdministrador || usuario?.rol === 'Analista';

  const cargar = useCallback(async () => {
    if (!Number.isFinite(solicitudId)) {
      setErrorCarga('La solicitud indicada no es válida.');
      setCargando(false);
      return;
    }

    try {
      const detalle = await cliente.obtenerSolicitud(solicitudId);
      setSolicitud(detalle);
      setNuevoEstado((actual) => actual || (detalle.estadosPermitidos[0]?.estado ?? ''));
      setResponsableId((actual) => actual || (detalle.responsableId ? String(detalle.responsableId) : ''));
      setErrorCarga(null);
    } catch (excepcion) {
      setErrorCarga(
        excepcion instanceof ErrorApi
          ? excepcion.estado === 404
            ? 'La solicitud no existe o fue eliminada.'
            : excepcion.message
          : 'No fue posible cargar la solicitud.'
      );
    } finally {
      setCargando(false);
    }
  }, [cliente, solicitudId]);

  useEffect(() => {
    void cargar();
  }, [cargar]);

  useEffect(() => {
    if (!puedeGestionar) {
      return;
    }

    cliente
      .obtenerResponsables()
      .then(setResponsables)
      .catch(() => setResponsables([]));
  }, [cliente, puedeGestionar]);

  const cambiarEstado = async (evento: FormEvent<HTMLFormElement>) => {
    evento.preventDefault();

    if (!solicitud || !nuevoEstado) {
      return;
    }

    if (nuevoEstado === 'Cerrada' && !comentarioEstado.trim()) {
      setAviso({ tono: 'error', texto: 'Para cerrar una solicitud debe registrar un comentario de confirmación.' });
      return;
    }

    setOcupado('estado');
    setAviso(null);

    try {
      const actualizada = await cliente.cambiarEstado(solicitud.id, nuevoEstado, comentarioEstado);
      setSolicitud(actualizada);
      setComentarioEstado('');
      setNuevoEstado(actualizada.estadosPermitidos[0]?.estado ?? '');
      setAviso({ tono: 'exito', texto: `Estado actualizado a ${tituloEstado(actualizada.estado)}.` });
    } catch (excepcion) {
      setAviso({
        tono: 'error',
        texto: excepcion instanceof ErrorApi ? excepcion.message : 'No fue posible cambiar el estado.'
      });
    } finally {
      setOcupado(null);
    }
  };

  const asignar = async (evento: FormEvent<HTMLFormElement>) => {
    evento.preventDefault();

    if (!solicitud || !responsableId) {
      setAviso({ tono: 'error', texto: 'Seleccione un responsable.' });
      return;
    }

    setOcupado('asignacion');
    setAviso(null);

    try {
      const actualizada = await cliente.asignarResponsable(solicitud.id, Number(responsableId));
      setSolicitud(actualizada);
      setAviso({ tono: 'exito', texto: `Solicitud asignada a ${actualizada.responsable ?? 'el responsable'}.` });
    } catch (excepcion) {
      setAviso({
        tono: 'error',
        texto: excepcion instanceof ErrorApi ? excepcion.message : 'No fue posible asignar el responsable.'
      });
    } finally {
      setOcupado(null);
    }
  };

  const comentar = async (evento: FormEvent<HTMLFormElement>) => {
    evento.preventDefault();

    if (!solicitud || !textoComentario.trim()) {
      setAviso({ tono: 'error', texto: 'Escriba un comentario antes de enviarlo.' });
      return;
    }

    setOcupado('comentario');
    setAviso(null);

    try {
      const actualizada = await cliente.comentar(
        solicitud.id,
        textoComentario.trim(),
        puedeGestionar ? visibilidad : 'Publica'
      );
      setSolicitud(actualizada);
      setTextoComentario('');
      setAviso({ tono: 'exito', texto: 'Comentario agregado.' });
    } catch (excepcion) {
      setAviso({
        tono: 'error',
        texto: excepcion instanceof ErrorApi ? excepcion.message : 'No fue posible agregar el comentario.'
      });
    } finally {
      setOcupado(null);
    }
  };

  if (cargando) {
    return (
      <Tarjeta>
        <Cargando mensaje="Cargando la solicitud…" />
      </Tarjeta>
    );
  }

  if (errorCarga || !solicitud) {
    return (
      <div className="space-y-4">
        <Alerta tono="error">{errorCarga ?? 'No fue posible cargar la solicitud.'}</Alerta>
        <Link to="/consulta">
          <Boton variante="secundario" icono={<IconoVolver className="h-4 w-4" />}>
            Volver a la consulta
          </Boton>
        </Link>
      </div>
    );
  }

  const comentarios = [...solicitud.comentarios].sort(
    (a, b) => new Date(a.fecha).getTime() - new Date(b.fecha).getTime()
  );
  const historial = [...solicitud.historial].sort(
    (a, b) => new Date(b.fecha).getTime() - new Date(a.fecha).getTime()
  );
  const cerrada = solicitud.estado === 'Cerrada';
  const estadosPosibles = solicitud.estadosPermitidos;

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="min-w-0">
          <Link
            to="/consulta"
            className="mb-3 inline-flex items-center gap-1.5 text-sm font-medium text-slate-500 hover:text-sb-900"
          >
            <IconoVolver className="h-4 w-4" />
            Volver a la consulta
          </Link>
          <div className="flex flex-wrap items-center gap-2">
            <span className="rounded-md bg-sb-900 px-2 py-1 text-xs font-bold tracking-wide text-white">
              {solicitud.codigo}
            </span>
            <InsigniaEstado estado={solicitud.estado} />
            <InsigniaPrioridad prioridad={solicitud.prioridad} />
            <InsigniaVencida vencida={solicitud.vencida} />
          </div>
          <h2 className="mt-3 text-2xl font-bold leading-tight tracking-tight text-sb-900">
            {solicitud.titulo}
          </h2>
        </div>

        {puedeGestionar && !cerrada && (
          <Boton
            variante="secundario"
            icono={<IconoEditar />}
            onClick={() => setModalEdicion(true)}
            className="shrink-0"
          >
            Editar solicitud
          </Boton>
        )}
      </div>

      {aviso && <Alerta tono={aviso.tono}>{aviso.texto}</Alerta>}

      {solicitud.vencida && !cerrada && (
        <Alerta tono="aviso">
          El compromiso venció el {formatearFecha(solicitud.fechaCompromiso)} y la solicitud sigue abierta.
        </Alerta>
      )}

      <Tarjeta>
        <dl className="grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
          <Dato etiqueta="Área">{solicitud.area}</Dato>
          <Dato etiqueta="Tipo">{solicitud.tipoSolicitud}</Dato>
          <Dato etiqueta="Solicitante">{solicitud.solicitante}</Dato>
          <Dato etiqueta="Responsable">{solicitud.responsable ?? 'Sin asignar'}</Dato>
          <Dato etiqueta="Fecha de creación">{formatearFechaHora(solicitud.fechaCreacion)}</Dato>
          <Dato etiqueta="Fecha de compromiso">
            <span className={solicitud.vencida && !cerrada ? 'font-semibold text-red-600' : ''}>
              {formatearFecha(solicitud.fechaCompromiso)}
            </span>
          </Dato>
          <Dato etiqueta="Fecha de cierre">{formatearFechaHora(solicitud.fechaCierre)}</Dato>
          <Dato etiqueta="Evidencia">
            {solicitud.urlEvidencia ? (
              <a
                href={solicitud.urlEvidencia}
                target="_blank"
                rel="noreferrer"
                className="break-all font-medium text-acento hover:underline"
              >
                {solicitud.urlEvidencia}
              </a>
            ) : (
              <span className="text-slate-400">No adjunta</span>
            )}
          </Dato>
        </dl>
      </Tarjeta>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_380px]">
        <div className="space-y-6">
          <Tarjeta titulo="Descripción" descripcion="Detalle reportado por el solicitante.">
            <p className="whitespace-pre-line text-sm leading-relaxed text-slate-700">{solicitud.descripcion}</p>
          </Tarjeta>

          <Tarjeta
            titulo="Comentarios"
            descripcion="Conversación entre el solicitante y el equipo técnico."
            acciones={
              <span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-semibold text-slate-600">
                {comentarios.length}
              </span>
            }
          >
            {comentarios.length === 0 ? (
              <Vacio titulo="Sin comentarios" descripcion="Todavía no hay intercambios en esta solicitud." />
            ) : (
              <ul className="space-y-4">
                {comentarios.map((comentario: Comentario) => {
                  const interna = comentario.visibilidad === 'Interna';

                  return (
                    <li
                      key={comentario.id}
                      className={`rounded-xl border p-4 ${
                        interna ? 'border-amber-200 bg-amber-50/60' : 'border-slate-200 bg-slate-50/60'
                      }`}
                    >
                      <div className="flex flex-wrap items-center justify-between gap-2">
                        <p className="text-sm font-semibold text-sb-900">{comentario.usuario}</p>
                        <p className="text-xs text-slate-400">{formatearFechaHora(comentario.fecha)}</p>
                      </div>
                      {interna && (
                        <span className="mt-1 inline-block rounded bg-amber-100 px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-wide text-amber-800">
                          Nota interna
                        </span>
                      )}
                      <p className="mt-2 whitespace-pre-line text-sm leading-relaxed text-slate-700">
                        {comentario.texto}
                      </p>
                    </li>
                  );
                })}
              </ul>
            )}

            {!cerrada && (
              <form className="mt-6 space-y-3 border-t border-slate-100 pt-5" onSubmit={comentar}>
                <label htmlFor="comentario" className="block text-sm font-medium text-sb-900">
                  Agregar comentario
                </label>
                <AreaTexto
                  id="comentario"
                  value={textoComentario}
                  onChange={(evento) => setTextoComentario(evento.target.value)}
                  placeholder="Escriba una actualización o solicitada información adicional…"
                  maxLength={2000}
                  className="min-h-[90px]"
                />

                <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                  {puedeGestionar ? (
                    <label className="flex items-center gap-2 text-sm text-slate-600">
                      Visibilidad
                      <Seleccion
                        value={visibilidad}
                        onChange={(evento) => setVisibilidad(evento.target.value as VisibilidadComentario)}
                        className="w-auto py-1.5 text-xs"
                        aria-label="Visibilidad del comentario"
                      >
                        <option value="Publica">Pública (visible al solicitante)</option>
                        <option value="Interna">Interna (solo equipo técnico)</option>
                      </Seleccion>
                    </label>
                  ) : (
                    <p className="text-xs text-slate-500">Su comentario será visible para el equipo técnico.</p>
                  )}

                  <Boton type="submit" cargando={ocupado === 'comentario'} disabled={!textoComentario.trim()}>
                    Publicar comentario
                  </Boton>
                </div>
              </form>
            )}
          </Tarjeta>
        </div>

        <div className="space-y-6">
          {puedeGestionar && (
            <Tarjeta titulo="Gestión" descripcion="Cambie el estado o asigne un responsable.">
              {estadosPosibles.length > 0 ? (
                <form className="space-y-4" onSubmit={cambiarEstado}>
                  <Campo etiqueta="Nuevo estado" para="nuevoEstado" requerido>
                    <Seleccion
                      id="nuevoEstado"
                      value={nuevoEstado}
                      onChange={(evento) => setNuevoEstado(evento.target.value as EstadoSolicitud)}
                    >
                      {estadosPosibles.map((opcion) => (
                        <option key={opcion.estado} value={opcion.estado}>
                          {tituloEstado(opcion.estado)}
                        </option>
                      ))}
                    </Seleccion>
                  </Campo>

                  <Campo
                    etiqueta="Comentario del cambio"
                    para="comentarioEstado"
                    ayuda={nuevoEstado === 'Cerrada' ? 'Obligatorio para cerrar la solicitud.' : 'Opcional, queda en el historial.'}
                  >
                    <AreaTexto
                      id="comentarioEstado"
                      value={comentarioEstado}
                      onChange={(evento) => setComentarioEstado(evento.target.value)}
                      placeholder="Explique brevemente el motivo del cambio…"
                      maxLength={2000}
                      className="min-h-[80px]"
                    />
                  </Campo>

                  <Boton type="submit" cargando={ocupado === 'estado'} className="w-full">
                    Actualizar estado
                  </Boton>
                </form>
              ) : (
                <p className="rounded-lg bg-slate-50 px-3 py-3 text-sm text-slate-500">
                  No hay transiciones disponibles para su rol en el estado actual.
                </p>
              )}

              <form className="mt-5 space-y-3 border-t border-slate-100 pt-5" onSubmit={asignar}>
                <Campo etiqueta="Responsable" para="responsable">
                  <Seleccion
                    id="responsable"
                    value={responsableId}
                    onChange={(evento) => setResponsableId(evento.target.value)}
                  >
                    <option value="">Sin asignar</option>
                    {responsables.map((responsable) => (
                      <option key={responsable.id} value={responsable.id}>
                        {responsable.nombre} · {responsable.rolDescripcion}
                      </option>
                    ))}
                  </Seleccion>
                </Campo>

                <Boton
                  type="submit"
                  variante="secundario"
                  cargando={ocupado === 'asignacion'}
                  disabled={!responsableId}
                  className="w-full"
                >
                  Guardar asignación
                </Boton>
              </form>
            </Tarjeta>
          )}

          <Tarjeta titulo="Historial" descripcion="Cada cambio de estado queda registrado.">
            {historial.length === 0 ? (
              <Vacio titulo="Sin movimientos" />
            ) : (
              <ol className="relative space-y-5 border-l border-slate-200 pl-5">
                {historial.map((movimiento) => (
                  <li key={movimiento.id} className="relative">
                    <span
                      className={`absolute -left-[26px] top-1 h-3 w-3 rounded-full ring-4 ring-white ${
                        COLORES_PUNTO[movimiento.estadoNuevo]
                      }`}
                    />
                    <p className="text-sm font-semibold text-sb-900">
                      {movimiento.estadoAnteriorDescripcion
                        ? `${movimiento.estadoAnteriorDescripcion} → ${movimiento.estadoNuevoDescripcion}`
                        : movimiento.estadoNuevoDescripcion}
                    </p>
                    <p className="mt-0.5 text-xs text-slate-400">
                      {formatearFechaHora(movimiento.fecha)} · {movimiento.usuario}
                    </p>
                    {movimiento.comentario && (
                      <p className="mt-2 rounded-lg bg-slate-50 px-3 py-2 text-xs leading-relaxed text-slate-600">
                        {movimiento.comentario}
                      </p>
                    )}
                  </li>
                ))}
              </ol>
            )}
          </Tarjeta>
        </div>
      </div>

      {puedeGestionar && (
        <ModalEdicion
          abierta={modalEdicion}
          solicitud={solicitud}
          onCerrar={() => setModalEdicion(false)}
          onGuardada={(actualizada) => {
            setSolicitud(actualizada);
            setModalEdicion(false);
            setAviso({ tono: 'exito', texto: 'La solicitud se actualizó correctamente.' });
          }}
          onError={(mensaje) => setAviso({ tono: 'error', texto: mensaje })}
          cliente={cliente}
        />
      )}
    </div>
  );
}
