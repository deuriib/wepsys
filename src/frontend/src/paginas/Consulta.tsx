import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Alerta, Boton, Cargando, Entrada, Seleccion, Tarjeta, Vacio } from '../componentes/Diseno';
import { InsigniaEstado, InsigniaPrioridad, InsigniaVencida } from '../componentes/Insignias';
import { IconoBuscar, IconoFiltro } from '../componentes/Iconos';
import { Paginacion } from '../componentes/Paginacion';
import { ErrorApi } from '../lib/api';
import { formatearFecha } from '../lib/formato';
import type {
  Area,
  EstadoSolicitud,
  Pagina,
  PrioridadSolicitud,
  SolicitudResumen
} from '../lib/tipos';
import { useSesion } from '../sesion/ContextoSesion';

const ESTADOS: EstadoSolicitud[] = [
  'Registrada',
  'EnAnalisis',
  'EnProgreso',
  'EnEsperaDelSolicitante',
  'Resuelta',
  'Cerrada'
];

const PRIORIDADES: PrioridadSolicitud[] = ['Critica', 'Alta', 'Media', 'Baja'];

const ETIQUETAS_ESTADO: Record<EstadoSolicitud, string> = {
  Registrada: 'Registrada',
  EnAnalisis: 'En análisis',
  EnProgreso: 'En progreso',
  EnEsperaDelSolicitante: 'En espera del solicitante',
  Resuelta: 'Resuelta',
  Cerrada: 'Cerrada'
};

const ETIQUETAS_PRIORIDAD: Record<PrioridadSolicitud, string> = {
  Critica: 'Crítica',
  Alta: 'Alta',
  Media: 'Media',
  Baja: 'Baja'
};

export function Consulta() {
  const { cliente } = useSesion();
  const [texto, setTexto] = useState('');
  const [estado, setEstado] = useState<EstadoSolicitud | ''>('');
  const [prioridad, setPrioridad] = useState<PrioridadSolicitud | ''>('');
  const [areaId, setAreaId] = useState<number | ''>('');
  const [soloVencidas, setSoloVencidas] = useState(false);
  const [pagina, setPagina] = useState(1);
  const [areas, setAreas] = useState<Area[]>([]);
  const [datos, setDatos] = useState<Pagina<SolicitudResumen> | null>(null);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    cliente
      .obtenerAreas()
      .then(setAreas)
      .catch(() => setAreas([]));
  }, [cliente]);

  useEffect(() => {
    let vigente = true;
    setCargando(true);

    const temporizador = window.setTimeout(() => {
      cliente
        .consultarSolicitudes({
          texto,
          estado,
          prioridad,
          areaId,
          soloVencidas,
          pagina,
          tamanoPagina: 10
        })
        .then((resultado) => {
          if (vigente) {
            setDatos(resultado);
            setError(null);
          }
        })
        .catch((excepcion: unknown) => {
          if (vigente) {
            setError(excepcion instanceof ErrorApi ? excepcion.message : 'No fue posible consultar las solicitudes.');
          }
        })
        .finally(() => {
          if (vigente) {
            setCargando(false);
          }
        });
    }, 250);

    return () => {
      vigente = false;
      window.clearTimeout(temporizador);
    };
  }, [cliente, texto, estado, prioridad, areaId, soloVencidas, pagina]);

  const limpiar = () => {
    setTexto('');
    setEstado('');
    setPrioridad('');
    setAreaId('');
    setSoloVencidas(false);
    setPagina(1);
  };

  const hayFiltros = Boolean(texto || estado || prioridad || areaId || soloVencidas);
  const solicitudes = datos?.elementos ?? [];

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <p className="max-w-2xl text-sm text-slate-500">
          Filtre por estado, prioridad, área o texto. Los resultados respetan el alcance de su rol.
        </p>
        <Link to="/crear" className="shrink-0">
          <Boton>Nueva solicitud</Boton>
        </Link>
      </div>

      <Tarjeta>
        <div className="mb-4 flex items-center gap-2 text-sm font-semibold text-sb-900">
          <IconoFiltro className="h-4 w-4 text-acento" />
          Filtros
        </div>

        <div className="grid gap-4 lg:grid-cols-12">
          <div className="lg:col-span-4">
            <label htmlFor="filtro-texto" className="mb-1.5 block text-sm font-medium text-sb-900">
              Buscar
            </label>
            <div className="relative">
              <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400">
                <IconoBuscar className="h-4 w-4" />
              </span>
              <Entrada
                id="filtro-texto"
                className="pl-9"
                placeholder="Código, título o descripción…"
                value={texto}
                onChange={(evento) => {
                  setTexto(evento.target.value);
                  setPagina(1);
                }}
              />
            </div>
          </div>

          <div className="lg:col-span-3">
            <label htmlFor="filtro-estado" className="mb-1.5 block text-sm font-medium text-sb-900">
              Estado
            </label>
            <Seleccion
              id="filtro-estado"
              value={estado}
              onChange={(evento) => {
                setEstado(evento.target.value as EstadoSolicitud | '');
                setPagina(1);
              }}
            >
              <option value="">Todos los estados</option>
              {ESTADOS.map((valor) => (
                <option key={valor} value={valor}>
                  {ETIQUETAS_ESTADO[valor]}
                </option>
              ))}
            </Seleccion>
          </div>

          <div className="lg:col-span-2">
            <label htmlFor="filtro-prioridad" className="mb-1.5 block text-sm font-medium text-sb-900">
              Prioridad
            </label>
            <Seleccion
              id="filtro-prioridad"
              value={prioridad}
              onChange={(evento) => {
                setPrioridad(evento.target.value as PrioridadSolicitud | '');
                setPagina(1);
              }}
            >
              <option value="">Todas</option>
              {PRIORIDADES.map((valor) => (
                <option key={valor} value={valor}>
                  {ETIQUETAS_PRIORIDAD[valor]}
                </option>
              ))}
            </Seleccion>
          </div>

          <div className="lg:col-span-3">
            <label htmlFor="filtro-area" className="mb-1.5 block text-sm font-medium text-sb-900">
              Área
            </label>
            <Seleccion
              id="filtro-area"
              value={areaId}
              onChange={(evento) => {
                setAreaId(evento.target.value ? Number(evento.target.value) : '');
                setPagina(1);
              }}
            >
              <option value="">Todas las áreas</option>
              {areas.map((area) => (
                <option key={area.id} value={area.id}>
                  {area.nombre}
                </option>
              ))}
            </Seleccion>
          </div>

          <div className="flex items-end justify-between gap-3 lg:col-span-12">
            <label className="inline-flex cursor-pointer items-center gap-2 text-sm text-slate-600">
              <input
                type="checkbox"
                className="h-4 w-4 rounded border-slate-300 text-acento focus:ring-acento/40"
                checked={soloVencidas}
                onChange={(evento) => {
                  setSoloVencidas(evento.target.checked);
                  setPagina(1);
                }}
              />
              Mostrar solo vencidas
            </label>

            {hayFiltros && (
              <Boton variante="fantasma" tamano="sm" onClick={limpiar}>
                Limpiar filtros
              </Boton>
            )}
          </div>
        </div>
      </Tarjeta>

      {error && <Alerta tono="error">{error}</Alerta>}

      <Tarjeta
        titulo={datos ? `${datos.totalRegistros} solicitud${datos.totalRegistros === 1 ? '' : 'es'}` : 'Solicitudes'}
        descripcion={hayFiltros ? 'Resultados de los filtros aplicados.' : 'Todas las solicitudes de su alcance.'}
        className="overflow-hidden"
      >
        {cargando && !datos ? (
          <Cargando mensaje="Consultando solicitudes…" />
        ) : solicitudes.length === 0 ? (
          <Vacio
            titulo="Sin resultados"
            descripcion={
              hayFiltros
                ? 'Ninguna solicitud coincide con los filtros seleccionados.'
                : 'Aún no hay solicitudes registradas.'
            }
            accion={
              hayFiltros ? (
                <Boton variante="secundario" onClick={limpiar}>
                  Limpiar filtros
                </Boton>
              ) : (
                <Link to="/crear">
                  <Boton>Crear registro</Boton>
                </Link>
              )
            }
          />
        ) : (
          <>
            <div className="-mx-6 -mt-5 overflow-x-auto">
              <table className="w-full min-w-[820px] table-fixed text-left text-sm">
                <thead>
                  <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
                    <th className="w-[118px] px-6 py-3 font-semibold">Código</th>
                    <th className="px-6 py-3 font-semibold">Solicitud</th>
                    <th className="w-[168px] px-6 py-3 font-semibold">Estado</th>
                    <th className="w-[110px] px-6 py-3 font-semibold">Prioridad</th>
                    <th className="w-[120px] px-6 py-3 font-semibold">Compromiso</th>
                    <th className="hidden w-[150px] px-6 py-3 font-semibold 2xl:table-cell">Responsable</th>
                    <th className="w-[72px] px-3 py-3 text-right font-semibold">
                      <span className="sr-only">Acciones</span>
                    </th>
                  </tr>
                </thead>
                <tbody className={`divide-y divide-slate-100 ${cargando ? 'opacity-60' : ''}`}>
                  {solicitudes.map((solicitud) => (
                    <tr key={solicitud.id} className="transition hover:bg-slate-50/80">
                      <td className="whitespace-nowrap px-6 py-3.5">
                        <Link
                          to={`/solicitudes/${solicitud.id}`}
                          className="font-semibold text-sb-800 hover:text-acento"
                        >
                          {solicitud.codigo}
                        </Link>
                      </td>
                      <td className="px-6 py-3.5">
                        <Link
                          to={`/solicitudes/${solicitud.id}`}
                          className="block truncate font-medium text-sb-900 hover:text-acento"
                          title={solicitud.titulo}
                        >
                          {solicitud.titulo}
                        </Link>
                        <span className="block truncate text-xs text-slate-400" title={`${solicitud.area} · ${solicitud.tipoSolicitud}`}>
                          {solicitud.area} · {solicitud.tipoSolicitud}
                        </span>
                      </td>
                      <td className="px-6 py-3.5">
                        <div className="flex flex-wrap items-center gap-2">
                          <InsigniaEstado estado={solicitud.estado} />
                          <InsigniaVencida vencida={solicitud.vencida} />
                        </div>
                      </td>
                      <td className="px-6 py-3.5">
                        <InsigniaPrioridad prioridad={solicitud.prioridad} />
                      </td>
                      <td className="whitespace-nowrap px-6 py-3.5 text-slate-600">
                        {formatearFecha(solicitud.fechaCompromiso)}
                      </td>
                      <td className="hidden truncate px-6 py-3.5 text-slate-600 2xl:table-cell">
                        {solicitud.responsable ?? 'Sin asignar'}
                      </td>
                      <td className="whitespace-nowrap px-3 py-3.5 text-right">
                        <Link
                          to={`/solicitudes/${solicitud.id}`}
                          className="inline-flex items-center rounded-lg px-2 py-1 text-sm font-semibold text-acento transition hover:bg-acento-suave"
                          aria-label={`Ver detalle de ${solicitud.codigo}`}
                          title="Ver detalle"
                        >
                          Ver
                        </Link>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {datos && (
              <div className="-mx-6 -mb-5">
                <Paginacion
                  pagina={datos.pagina}
                  totalPaginas={datos.totalPaginas}
                  totalRegistros={datos.totalRegistros}
                  tamanoPagina={datos.tamanoPagina}
                  onCambiar={setPagina}
                />
              </div>
            )}
          </>
        )}
      </Tarjeta>
    </div>
  );
}
