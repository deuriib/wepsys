import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { Alerta, Boton, Cargando, Tarjeta, Vacio } from '../componentes/Diseno';
import { InsigniaEstado, InsigniaPrioridad, InsigniaVencida } from '../componentes/Insignias';
import { IconoAlerta, IconoCheck, IconoDocumento, IconoReloj } from '../componentes/Iconos';
import { ErrorApi } from '../lib/api';
import { useResumenTablero } from '../lib/consultas';
import { formatearFecha, tituloEstado, tituloPrioridad } from '../lib/formato';
import type { ConteoSolicitudes } from '../lib/tipos';
import { useSesion } from '../sesion/ContextoSesion';

const COLORES_ESTADO: Record<string, string> = {
  Registrada: 'bg-sky-500',
  EnAnalisis: 'bg-amber-500',
  EnProgreso: 'bg-indigo-500',
  EnEsperaDelSolicitante: 'bg-violet-500',
  Resuelta: 'bg-emerald-500',
  Cerrada: 'bg-slate-400'
};

const COLORES_PRIORIDAD: Record<string, string> = {
  Baja: 'bg-slate-400',
  Media: 'bg-sky-500',
  Alta: 'bg-amber-500',
  Critica: 'bg-red-500'
};

const ORDEN_ESTADOS = ['Registrada', 'EnAnalisis', 'EnProgreso', 'EnEsperaDelSolicitante', 'Resuelta', 'Cerrada'];
const ORDEN_PRIORIDADES = ['Critica', 'Alta', 'Media', 'Baja'];

function saludo(): string {
  const hora = new Date().getHours();
  if (hora < 12) return 'Buenos días';
  if (hora < 19) return 'Buenas tardes';
  return 'Buenas noches';
}

function Indicador({
  etiqueta,
  valor,
  detalle,
  icono,
  color
}: {
  etiqueta: string;
  valor: number;
  detalle: string;
  icono: ReactNode;
  color: string;
}) {
  return (
    <div className="rounded-panel border border-slate-200/80 bg-white p-5 shadow-panel">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-sm font-medium text-slate-500">{etiqueta}</p>
          <p className="mt-2 text-3xl font-bold tracking-tight text-sb-900">{valor}</p>
        </div>
        <span className={`flex h-11 w-11 items-center justify-center rounded-xl ${color}`}>{icono}</span>
      </div>
      <p className="mt-3 text-xs text-slate-500">{detalle}</p>
    </div>
  );
}

function Barras({
  datos,
  total,
  colores,
  etiqueta
}: {
  datos: ConteoSolicitudes[];
  total: number;
  colores: Record<string, string>;
  etiqueta: (clave: string) => string;
}) {
  return (
    <ul className="space-y-3.5">
      {datos.map((item) => {
        const porcentaje = total > 0 ? Math.round((item.total / total) * 100) : 0;

        return (
          <li key={item.clave}>
            <div className="mb-1.5 flex items-center justify-between gap-3 text-sm">
              <span className="text-slate-600">{etiqueta(item.clave)}</span>
              <span className="font-semibold text-sb-900">
                {item.total}
                <span className="ml-1.5 text-xs font-normal text-slate-400">{porcentaje}%</span>
              </span>
            </div>
            <div className="h-2 overflow-hidden rounded-full bg-slate-100">
              <div
                className={`h-full rounded-full ${colores[item.clave] ?? 'bg-slate-400'}`}
                style={{ width: `${porcentaje}%` }}
              />
            </div>
          </li>
        );
      })}
    </ul>
  );
}

export function Inicio() {
  const { usuario } = useSesion();
  const { data: resumen, isLoading: cargando, error: errorQuery } = useResumenTablero();

  const error = errorQuery
    ? errorQuery instanceof ErrorApi
      ? errorQuery.message
      : 'No fue posible cargar el tablero.'
    : null;

  const estados = ORDEN_ESTADOS.map(
    (clave) => resumen?.porEstado.find((item) => item.clave === clave) ?? { clave, total: 0 }
  );
  const prioridades = ORDEN_PRIORIDADES.map(
    (clave) => resumen?.porPrioridad.find((item) => item.clave === clave) ?? { clave, total: 0 }
  );
  const primerNombre = usuario?.nombre.split(' ')[0] ?? '';

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-sm font-medium text-acento">{saludo()}, {primerNombre}</p>
          <h2 className="mt-1 text-2xl font-bold tracking-tight text-sb-900">
            Este es el estado de las solicitudes
          </h2>
          <p className="mt-1 text-sm text-slate-500">
            Resumen actualizado al {formatearFecha(new Date().toISOString())}.
          </p>
        </div>
        <div className="flex gap-2">
          <Link to="/consulta">
            <Boton variante="secundario">Ver consulta</Boton>
          </Link>
          <Link to="/crear">
            <Boton>Nueva solicitud</Boton>
          </Link>
        </div>
      </div>

      {error && <Alerta tono="error">{error}</Alerta>}

      {cargando && !resumen ? (
        <Tarjeta>
          <Cargando mensaje="Calculando indicadores…" />
        </Tarjeta>
      ) : resumen ? (
        <>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <Indicador
              etiqueta="Total de solicitudes"
              valor={resumen.total}
              detalle="Registros visibles según su rol"
              icono={<IconoDocumento className="h-5 w-5 text-sb-700" />}
              color="bg-sb-900/5"
            />
            <Indicador
              etiqueta="Abiertas"
              valor={resumen.abiertas}
              detalle="Pendientes de cierre"
              icono={<IconoReloj className="h-5 w-5 text-sky-600" />}
              color="bg-sky-50"
            />
            <Indicador
              etiqueta="Cerradas"
              valor={resumen.cerradas}
              detalle="Concluidas y confirmadas"
              icono={<IconoCheck className="h-5 w-5 text-emerald-600" />}
              color="bg-emerald-50"
            />
            <Indicador
              etiqueta="Vencidas"
              valor={resumen.vencidas}
              detalle="Compromiso superado sin cierre"
              icono={<IconoAlerta className="h-5 w-5 text-amber-600" />}
              color="bg-amber-50"
            />
          </div>

          <div className="grid gap-6 lg:grid-cols-2">
            <Tarjeta titulo="Distribución por estado" descripcion="Progreso actual de las solicitudes">
              <Barras
                datos={estados}
                total={resumen.total}
                colores={COLORES_ESTADO}
                etiqueta={(clave) => tituloEstado(clave as any)}
              />
            </Tarjeta>

            <Tarjeta titulo="Distribución por prioridad" descripcion="Clasificación según urgencia e impacto">
              <Barras
                datos={prioridades}
                total={resumen.total}
                colores={COLORES_PRIORIDAD}
                etiqueta={(clave) => tituloPrioridad(clave as any)}
              />
            </Tarjeta>
          </div>

          <Tarjeta
            titulo="Últimas solicitudes registradas"
            descripcion="Acceso rápido a las solicitudes más recientes"
            acciones={
              <Link to="/consulta" className="text-xs font-semibold text-sb-800 hover:underline">
                Ver todas ({resumen.total})
              </Link>
            }
          >
            {resumen.ultimasSolicitudes.length === 0 ? (
              <Vacio
                titulo="No hay solicitudes"
                descripcion="Aún no se han registrado solicitudes en el sistema."
                accion={
                  <Link to="/crear">
                    <Boton>Registrar la primera solicitud</Boton>
                  </Link>
                }
              />
            ) : (
              <div className="divide-y divide-slate-100">
                {resumen.ultimasSolicitudes.map((item) => (
                  <Link
                    key={item.id}
                    to={`/solicitudes/${item.id}`}
                    className="flex flex-col gap-3 py-3.5 transition first:pt-0 last:pb-0 hover:bg-slate-50/70 sm:flex-row sm:items-center sm:justify-between"
                  >
                    <div className="min-w-0">
                      <div className="flex flex-wrap items-center gap-2">
                        <span className="font-mono text-xs font-semibold text-sb-800">{item.codigo}</span>
                        <InsigniaEstado estado={item.estado} />
                        <InsigniaPrioridad prioridad={item.prioridad} />
                        <InsigniaVencida vencida={item.vencida} />
                      </div>
                      <p className="mt-1.5 truncate text-sm font-medium text-slate-800">{item.titulo}</p>
                      <p className="mt-0.5 text-xs text-slate-400">
                        {item.area} · Solicitado por <span className="text-slate-600">{item.solicitante}</span>
                        {item.responsable && (
                          <>
                            {' '}· Responsable: <span className="text-slate-600">{item.responsable}</span>
                          </>
                        )}
                      </p>
                    </div>
                    <div className="flex items-center justify-between text-xs text-slate-400 sm:flex-col sm:items-end">
                      <span>{formatearFecha(item.fechaCreacion)}</span>
                      <span className="text-[11px] text-slate-400">
                        Compromiso: {formatearFecha(item.fechaCompromiso)}
                      </span>
                    </div>
                  </Link>
                ))}
              </div>
            )}
          </Tarjeta>
        </>
      ) : null}
    </div>
  );
}
