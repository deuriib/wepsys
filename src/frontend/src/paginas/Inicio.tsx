import { useEffect, useState } from 'react';
import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { Alerta, Boton, Cargando, Tarjeta, Vacio } from '../componentes/Diseno';
import { InsigniaEstado, InsigniaPrioridad, InsigniaVencida } from '../componentes/Insignias';
import { IconoAlerta, IconoCheck, IconoDocumento, IconoReloj } from '../componentes/Iconos';
import { ErrorApi } from '../lib/api';
import { formatearFecha, tituloEstado, tituloPrioridad } from '../lib/formato';
import type { ConteoSolicitudes, ResumenTablero } from '../lib/tipos';
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
  const { usuario, cliente } = useSesion();
  const [resumen, setResumen] = useState<ResumenTablero | null>(null);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let vigente = true;
    setCargando(true);

    cliente
      .obtenerResumen()
      .then((datos) => {
        if (vigente) {
          setResumen(datos);
          setError(null);
        }
      })
      .catch((excepcion: unknown) => {
        if (vigente) {
          setError(excepcion instanceof ErrorApi ? excepcion.message : 'No fue posible cargar el tablero.');
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
  }, [cliente]);

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
              icono={<IconoAlerta className="h-5 w-5 text-red-600" />}
              color="bg-red-50"
            />
          </div>

          <div className="grid gap-6 xl:grid-cols-2">
            <Tarjeta titulo="Solicitudes por estado" descripcion="Distribución del flujo completo.">
              <Barras datos={estados} total={resumen.total} colores={COLORES_ESTADO} etiqueta={tituloEstado} />
            </Tarjeta>
            <Tarjeta titulo="Solicitudes por prioridad" descripcion="Nivel de urgencia declarado.">
              <Barras
                datos={prioridades}
                total={resumen.total}
                colores={COLORES_PRIORIDAD}
                etiqueta={tituloPrioridad}
              />
            </Tarjeta>
          </div>

          <Tarjeta
            titulo="Últimas solicitudes"
            descripcion="Los registros más recientes de su alcance."
            acciones={
              <Link to="/consulta" className="text-sm font-semibold text-acento hover:underline">
                Ver todas
              </Link>
            }
            className="overflow-hidden"
          >
            {resumen.ultimasSolicitudes.length === 0 ? (
              <Vacio
                titulo="Todavía no hay solicitudes"
                descripcion="Cree el primer registro para comenzar a dar seguimiento."
                accion={
                  <Link to="/crear">
                    <Boton>Crear registro</Boton>
                  </Link>
                }
              />
            ) : (
              <div className="-mx-6 -my-5 overflow-x-auto">
                <table className="w-full min-w-[760px] text-left text-sm">
                  <thead>
                    <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
                      <th className="px-6 py-3 font-semibold">Código</th>
                      <th className="px-6 py-3 font-semibold">Solicitud</th>
                      <th className="px-6 py-3 font-semibold">Estado</th>
                      <th className="px-6 py-3 font-semibold">Prioridad</th>
                      <th className="px-6 py-3 font-semibold">Compromiso</th>
                      <th className="px-6 py-3 font-semibold">Responsable</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {resumen.ultimasSolicitudes.map((solicitud) => (
                      <tr key={solicitud.id} className="transition hover:bg-slate-50/80">
                        <td className="whitespace-nowrap px-6 py-3.5">
                          <Link
                            to={`/solicitudes/${solicitud.id}`}
                            className="font-semibold text-sb-800 hover:text-acento"
                          >
                            {solicitud.codigo}
                          </Link>
                        </td>
                        <td className="max-w-[320px] px-6 py-3.5">
                          <Link to={`/solicitudes/${solicitud.id}`} className="block truncate text-sb-900 hover:text-acento">
                            {solicitud.titulo}
                          </Link>
                          <span className="text-xs text-slate-400">{solicitud.area}</span>
                        </td>
                        <td className="px-6 py-3.5">
                          <div className="flex items-center gap-2">
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
                        <td className="px-6 py-3.5 text-slate-600">{solicitud.responsable ?? 'Sin asignar'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </Tarjeta>
        </>
      ) : null}
    </div>
  );
}
