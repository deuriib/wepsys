import { useDeferredValue, useState } from 'react';
import { Link } from 'react-router-dom';
import { Alerta, Boton, Cargando, Entrada, Seleccion, Tarjeta, Vacio } from '../componentes/Diseno';
import { InsigniaEstado, InsigniaPrioridad, InsigniaVencida } from '../componentes/Insignias';
import { IconoBuscar, IconoFiltro } from '../componentes/Iconos';
import { Paginacion } from '../componentes/Paginacion';
import { ErrorApi } from '../lib/api';
import { useAreas, useConsultarSolicitudes, useResponsables } from '../lib/consultas';
import { formatearFecha } from '../lib/formato';
import type {
  EstadoSolicitud,
  PrioridadSolicitud
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
  const { usuario } = useSesion();
  const [texto, setTexto] = useState('');
  const textoDiferido = useDeferredValue(texto);
  const [estado, setEstado] = useState<EstadoSolicitud | ''>('');
  const [prioridad, setPrioridad] = useState<PrioridadSolicitud | ''>('');
  const [areaId, setAreaId] = useState<number | ''>('');
  const [responsableId, setResponsableId] = useState<number | ''>('');
  const [fechaDesde, setFechaDesde] = useState('');
  const [fechaHasta, setFechaHasta] = useState('');
  const [soloVencidas, setSoloVencidas] = useState(false);
  const [pagina, setPagina] = useState(1);

  const { data: areas = [] } = useAreas();
  const { data: responsables = [] } = useResponsables();

  const filtros = {
    texto: textoDiferido,
    estado,
    prioridad,
    areaId,
    responsableId,
    fechaDesde: fechaDesde || undefined,
    fechaHasta: fechaHasta || undefined,
    soloVencidas,
    pagina,
    tamanoPagina: 10
  };

  const {
    data: datos,
    isLoading: cargandoInicial,
    isFetching: cargando,
    error: errorQuery
  } = useConsultarSolicitudes(filtros);

  const error = errorQuery
    ? errorQuery instanceof ErrorApi
      ? errorQuery.message
      : 'No fue posible consultar las solicitudes.'
    : null;

  const limpiar = () => {
    setTexto('');
    setEstado('');
    setPrioridad('');
    setAreaId('');
    setResponsableId('');
    setFechaDesde('');
    setFechaHasta('');
    setSoloVencidas(false);
    setPagina(1);
  };

  const hayFiltros = Boolean(
    texto || estado || prioridad || areaId || responsableId || fechaDesde || fechaHasta || soloVencidas
  );
  const solicitudes = datos?.elementos ?? [];
  const puedeVerResponsables = usuario?.rol === 'Administrador' || usuario?.rol === 'Analista';

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <p className="max-w-2xl text-sm text-slate-500">
          Filtre por estado, prioridad, área, responsable o rango de fechas. Los resultados respetan el alcance de su rol.
        </p>
        <Link to="/crear" className="shrink-0">
          <Boton>Nueva solicitud</Boton>
        </Link>
      </div>

      <Tarjeta>
        <div className="mb-4 flex items-center justify-between">
          <div className="flex items-center gap-2 text-sm font-semibold text-sb-900">
            <IconoFiltro className="h-4 w-4 text-acento" />
            Filtros avanzados de búsqueda
          </div>
          {cargando && !cargandoInicial && (
            <span className="text-xs text-slate-400 animate-pulse">Actualizando resultados…</span>
          )}
        </div>

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-12">
          {/* Búsqueda por texto */}
          <div className="sm:col-span-2 lg:col-span-4">
            <label htmlFor="filtro-texto" className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-slate-500">
              Buscar texto
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

          {/* Estado */}
          <div className="lg:col-span-3">
            <label htmlFor="filtro-estado" className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-slate-500">
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

          {/* Prioridad */}
          <div className="lg:col-span-2">
            <label htmlFor="filtro-prioridad" className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-slate-500">
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

          {/* Área */}
          <div className="lg:col-span-3">
            <label htmlFor="filtro-area" className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-slate-500">
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

          {/* Responsable (si aplica al rol) */}
          {puedeVerResponsables && (
            <div className="lg:col-span-4">
              <label htmlFor="filtro-responsable" className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-slate-500">
                Responsable asignado
              </label>
              <Seleccion
                id="filtro-responsable"
                value={responsableId}
                onChange={(evento) => {
                  setResponsableId(evento.target.value ? Number(evento.target.value) : '');
                  setPagina(1);
                }}
              >
                <option value="">Todos los técnicos</option>
                {responsables.map((resp) => (
                  <option key={resp.id} value={resp.id}>
                    {resp.nombre}
                  </option>
                ))}
              </Seleccion>
            </div>
          )}

          {/* Rango de fechas */}
          <div className={puedeVerResponsables ? 'lg:col-span-4' : 'lg:col-span-6'}>
            <label htmlFor="filtro-fecha-desde" className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-slate-500">
              Fecha creación desde
            </label>
            <Entrada
              id="filtro-fecha-desde"
              type="date"
              value={fechaDesde}
              onChange={(evento) => {
                setFechaDesde(evento.target.value);
                setPagina(1);
              }}
            />
          </div>

          <div className={puedeVerResponsables ? 'lg:col-span-4' : 'lg:col-span-6'}>
            <label htmlFor="filtro-fecha-hasta" className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-slate-500">
              Fecha creación hasta
            </label>
            <Entrada
              id="filtro-fecha-hasta"
              type="date"
              value={fechaHasta}
              onChange={(evento) => {
                setFechaHasta(evento.target.value);
                setPagina(1);
              }}
            />
          </div>

          {/* Vencidas y Limpiar */}
          <div className="flex items-center justify-between gap-3 pt-2 sm:col-span-2 lg:col-span-12 border-t border-slate-100">
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
              Mostrar solo solicitudes vencidas
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
        {cargandoInicial && !datos ? (
          <Cargando mensaje="Consultando solicitudes…" />
        ) : solicitudes.length === 0 ? (
          <Vacio
            titulo="No se encontraron solicitudes"
            descripcion={
              hayFiltros
                ? 'Ninguna solicitud coincide con los filtros aplicados. Intente modificarlos o limpiarlos.'
                : 'Aún no se han registrado solicitudes en el sistema.'
            }
            accion={
              hayFiltros ? (
                <Boton variante="secundario" onClick={limpiar}>
                  Limpiar filtros
                </Boton>
              ) : (
                <Link to="/crear">
                  <Boton>Crear una solicitud</Boton>
                </Link>
              )
            }
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-200/80 text-left text-sm">
              <thead className="bg-slate-50/60 text-xs font-semibold uppercase tracking-wider text-slate-500">
                <tr>
                  <th scope="col" className="px-4 py-3">Código</th>
                  <th scope="col" className="px-4 py-3">Título</th>
                  <th scope="col" className="px-4 py-3">Área / Tipo</th>
                  <th scope="col" className="px-4 py-3">Estado</th>
                  <th scope="col" className="px-4 py-3">Prioridad</th>
                  <th scope="col" className="px-4 py-3">Solicitante</th>
                  <th scope="col" className="px-4 py-3">Responsable</th>
                  <th scope="col" className="px-4 py-3 text-right">Fechas</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 bg-white">
                {solicitudes.map((item) => (
                  <tr key={item.id} className="transition hover:bg-slate-50/70">
                    <td className="whitespace-nowrap px-4 py-3.5 font-mono text-xs font-semibold text-sb-800">
                      <Link to={`/solicitudes/${item.id}`} className="hover:underline">
                        {item.codigo}
                      </Link>
                    </td>
                    <td className="px-4 py-3.5 font-medium text-slate-800">
                      <Link to={`/solicitudes/${item.id}`} className="hover:text-acento">
                        {item.titulo}
                      </Link>
                    </td>
                    <td className="whitespace-nowrap px-4 py-3.5 text-xs text-slate-500">
                      <div>{item.area}</div>
                      <div className="text-slate-400">{item.tipoSolicitud}</div>
                    </td>
                    <td className="whitespace-nowrap px-4 py-3.5">
                      <InsigniaEstado estado={item.estado} />
                    </td>
                    <td className="whitespace-nowrap px-4 py-3.5">
                      <div className="flex items-center gap-1.5">
                        <InsigniaPrioridad prioridad={item.prioridad} />
                        <InsigniaVencida vencida={item.vencida} />
                      </div>
                    </td>
                    <td className="whitespace-nowrap px-4 py-3.5 text-xs text-slate-600">
                      {item.solicitante}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3.5 text-xs text-slate-600">
                      {item.responsable ?? <span className="italic text-slate-400">Sin asignar</span>}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3.5 text-right text-xs text-slate-400">
                      <div>{formatearFecha(item.fechaCreacion)}</div>
                      <div className="text-[11px] text-slate-400">
                        Compromiso: {formatearFecha(item.fechaCompromiso)}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {datos && datos.totalPaginas > 1 && (
          <div className="border-t border-slate-100 p-4">
            <Paginacion
              pagina={datos.pagina}
              totalPaginas={datos.totalPaginas}
              totalRegistros={datos.totalRegistros}
              tamanoPagina={datos.tamanoPagina}
              onCambiar={setPagina}
            />
          </div>
        )}
      </Tarjeta>
    </div>
  );
}
