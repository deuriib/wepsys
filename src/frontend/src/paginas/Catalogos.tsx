import { useEffect, useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alerta,
  Boton,
  Campo,
  Cargando,
  Entrada,
  Modal,
  Tarjeta,
  Vacio
} from '../componentes/Diseno';
import { InsigniaActiva } from '../componentes/Insignias';
import { IconoBasura, IconoBuscar, IconoEditar } from '../componentes/Iconos';
import { Paginacion } from '../componentes/Paginacion';
import { ErrorApi } from '../lib/api';
import {
  useAreasAdmin,
  useCrearAreaMutation,
  useActualizarAreaMutation,
  useEliminarAreaMutation,
  useTiposSolicitudAdmin,
  useCrearTipoSolicitudMutation,
  useActualizarTipoSolicitudMutation,
  useEliminarTipoSolicitudMutation
} from '../lib/consultas';
import type { Area, TipoSolicitud } from '../lib/tipos';

// ---------------------------------------------------------------------------
// Area CRUD
// ---------------------------------------------------------------------------

interface FormularioArea {
  nombre: string;
  activa: boolean;
}

interface PropiedadesModalArea {
  abierta: boolean;
  area: Area | null;
  onCerrar: () => void;
  onGuardada: (msg: string) => void;
  onError: (msg: string) => void;
}

function ModalArea({ abierta, area, onCerrar, onGuardada, onError }: PropiedadesModalArea) {
  const mutacionCrear = useCrearAreaMutation();
  const mutacionActualizar = useActualizarAreaMutation();

  const [formulario, setFormulario] = useState<FormularioArea>(() =>
    area ? { nombre: area.nombre, activa: area.activa } : { nombre: '', activa: true }
  );
  const [errores, setErrores] = useState<Record<string, string>>({});

  const actualizar = <C extends keyof FormularioArea>(campo: C, valor: FormularioArea[C]) => {
    setFormulario((actual) => ({ ...actual, [campo]: valor }));
    setErrores((actuales) => {
      if (!actuales[campo]) return actuales;
      const copia = { ...actuales };
      delete copia[campo];
      return copia;
    });
  };

  const enviar = async (evento: FormEvent<HTMLFormElement>) => {
    evento.preventDefault();
    const nuevos: Record<string, string> = {};

    if (formulario.nombre.trim().length < 3) {
      nuevos.nombre = 'El nombre debe tener al menos 3 caracteres.';
    }

    setErrores(nuevos);
    if (Object.keys(nuevos).length > 0) return;

    try {
      if (area) {
        await mutacionActualizar.mutateAsync({
          id: area.id,
          peticion: { nombre: formulario.nombre.trim(), activa: formulario.activa }
        });
        onGuardada('Área actualizada correctamente.');
      } else {
        await mutacionCrear.mutateAsync({ nombre: formulario.nombre.trim() });
        onGuardada('Área registrada correctamente.');
      }
    } catch (excepcion) {
      onError(excepcion instanceof ErrorApi ? excepcion.message : 'No fue posible guardar el área.');
    }
  };

  const guardando = mutacionCrear.isPending || mutacionActualizar.isPending;

  return (
    <Modal
      abierto={abierta}
      titulo={area ? 'Editar área' : 'Nueva área'}
      descripcion="Las áreas se usan para clasificar las solicitudes de servicio."
      onCerrar={onCerrar}
      pie={
        <>
          <Boton variante="secundario" type="button" onClick={onCerrar}>
            Cancelar
          </Boton>
          <Boton type="submit" form="formulario-area" cargando={guardando}>
            {area ? 'Guardar cambios' : 'Registrar área'}
          </Boton>
        </>
      }
    >
      <form id="formulario-area" className="space-y-4" onSubmit={enviar} noValidate>
        <Campo etiqueta="Nombre" para="area-nombre" requerido error={errores.nombre}>
          <Entrada
            id="area-nombre"
            value={formulario.nombre}
            onChange={(evento) => actualizar('nombre', evento.target.value)}
            maxLength={120}
            placeholder="Ej. Tecnología de la Información"
          />
        </Campo>

        {area && (
          <Campo etiqueta="Estado" para="area-activa">
            <label className="flex h-[42px] items-center gap-2 rounded-lg border border-slate-300 bg-white px-3 text-sm text-slate-700">
              <input
                id="area-activa"
                type="checkbox"
                className="h-4 w-4 rounded border-slate-300 text-acento focus:ring-acento/40"
                checked={formulario.activa}
                onChange={(evento) => actualizar('activa', evento.target.checked)}
              />
              Área activa
            </label>
          </Campo>
        )}
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// TipoSolicitud CRUD
// ---------------------------------------------------------------------------

interface FormularioTipo {
  nombre: string;
  descripcion: string;
  activo: boolean;
}

interface PropiedadesModalTipo {
  abierta: boolean;
  tipo: TipoSolicitud | null;
  onCerrar: () => void;
  onGuardada: (msg: string) => void;
  onError: (msg: string) => void;
}

function ModalTipoSolicitud({
  abierta,
  tipo,
  onCerrar,
  onGuardada,
  onError
}: PropiedadesModalTipo) {
  const mutacionCrear = useCrearTipoSolicitudMutation();
  const mutacionActualizar = useActualizarTipoSolicitudMutation();

  const [formulario, setFormulario] = useState<FormularioTipo>(() =>
    tipo
      ? { nombre: tipo.nombre, descripcion: tipo.descripcion, activo: tipo.activo }
      : { nombre: '', descripcion: '', activo: true }
  );
  const [errores, setErrores] = useState<Record<string, string>>({});

  const actualizar = <C extends keyof FormularioTipo>(campo: C, valor: FormularioTipo[C]) => {
    setFormulario((actual) => ({ ...actual, [campo]: valor }));
    setErrores((actuales) => {
      if (!actuales[campo]) return actuales;
      const copia = { ...actuales };
      delete copia[campo];
      return copia;
    });
  };

  const enviar = async (evento: FormEvent<HTMLFormElement>) => {
    evento.preventDefault();
    const nuevos: Record<string, string> = {};

    if (formulario.nombre.trim().length < 3) {
      nuevos.nombre = 'El nombre debe tener al menos 3 caracteres.';
    }

    if (formulario.descripcion.trim().length > 300) {
      nuevos.descripcion = 'La descripción no puede superar 300 caracteres.';
    }

    setErrores(nuevos);
    if (Object.keys(nuevos).length > 0) return;

    try {
      if (tipo) {
        await mutacionActualizar.mutateAsync({
          id: tipo.id,
          peticion: {
            nombre: formulario.nombre.trim(),
            descripcion: formulario.descripcion.trim(),
            activo: formulario.activo
          }
        });
        onGuardada('Tipo de solicitud actualizado correctamente.');
      } else {
        await mutacionCrear.mutateAsync({
          nombre: formulario.nombre.trim(),
          descripcion: formulario.descripcion.trim()
        });
        onGuardada('Tipo de solicitud registrado correctamente.');
      }
    } catch (excepcion) {
      onError(
        excepcion instanceof ErrorApi
          ? excepcion.message
          : 'No fue posible guardar el tipo de solicitud.'
      );
    }
  };

  const guardando = mutacionCrear.isPending || mutacionActualizar.isPending;

  return (
    <Modal
      abierto={abierta}
      titulo={tipo ? 'Editar tipo de solicitud' : 'Nuevo tipo de solicitud'}
      descripcion="Los tipos clasifican las solicitudes dentro de un área."
      onCerrar={onCerrar}
      pie={
        <>
          <Boton variante="secundario" type="button" onClick={onCerrar}>
            Cancelar
          </Boton>
          <Boton type="submit" form="formulario-tipo" cargando={guardando}>
            {tipo ? 'Guardar cambios' : 'Registrar tipo'}
          </Boton>
        </>
      }
    >
      <form id="formulario-tipo" className="space-y-4" onSubmit={enviar} noValidate>
        <Campo etiqueta="Nombre" para="tipo-nombre" requerido error={errores.nombre}>
          <Entrada
            id="tipo-nombre"
            value={formulario.nombre}
            onChange={(evento) => actualizar('nombre', evento.target.value)}
            maxLength={120}
            placeholder="Ej. Solicitud de acceso a sistemas"
          />
        </Campo>

        <Campo etiqueta="Descripción" para="tipo-descripcion" error={errores.descripcion}>
          <Entrada
            id="tipo-descripcion"
            value={formulario.descripcion}
            onChange={(evento) => actualizar('descripcion', evento.target.value)}
            maxLength={300}
            placeholder="Descripción breve del tipo (opcional)"
          />
        </Campo>

        {tipo && (
          <Campo etiqueta="Estado" para="tipo-activo">
            <label className="flex h-[42px] items-center gap-2 rounded-lg border border-slate-300 bg-white px-3 text-sm text-slate-700">
              <input
                id="tipo-activo"
                type="checkbox"
                className="h-4 w-4 rounded border-slate-300 text-acento focus:ring-acento/40"
                checked={formulario.activo}
                onChange={(evento) => actualizar('activo', evento.target.checked)}
              />
              Tipo activo
            </label>
          </Campo>
        )}
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// Tabla reutilizable con acciones CRUD
// ---------------------------------------------------------------------------

interface ItemCatalogo {
  id: number;
  nombre: string;
  descripcion?: string;
  activo: boolean;
}

interface PropiedadesTablaItemCatalogo {
  items: ItemCatalogo[];
  etiquetaActivo: string;
  onEditar: (id: number) => void;
  onEliminar: (id: number, nombre: string) => void;
  cargando?: boolean;
}

function TablaItemsCatalogo({
  items,
  etiquetaActivo,
  onEditar,
  onEliminar,
  cargando = false
}: PropiedadesTablaItemCatalogo) {
  return (
    <div className="-mx-6 -my-5 overflow-x-auto">
      <table className="w-full text-left text-sm">
        <thead>
          <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
            <th className="px-6 py-3 font-semibold">Nombre</th>
            <th className="w-[100px] px-4 py-3 font-semibold">{etiquetaActivo}</th>
            <th className="w-[80px] px-2 py-3 text-right font-semibold">
              <span className="sr-only">Acciones</span>
            </th>
          </tr>
        </thead>
        <tbody className={`divide-y divide-slate-100 ${cargando ? 'opacity-60' : ''}`}>
          {items.map((item) => (
            <tr key={item.id} className="transition hover:bg-slate-50/80">
              <td className="px-6 py-3.5 font-medium text-sb-900">
                <div>{item.nombre}</div>
                {item.descripcion && (
                  <div className="mt-0.5 line-clamp-1 text-xs font-normal text-slate-500">
                    {item.descripcion}
                  </div>
                )}
              </td>
              <td className="px-4 py-3.5">
                <InsigniaActiva activa={item.activo} />
              </td>
              <td className="whitespace-nowrap px-2 py-3.5 text-right">
                <div className="inline-flex items-center gap-0.5">
                  <button
                    type="button"
                    onClick={() => onEditar(item.id)}
                    className="rounded-lg p-1.5 text-slate-500 transition hover:bg-sb-900/5 hover:text-sb-900"
                    aria-label={`Editar ${item.nombre}`}
                    title="Editar"
                  >
                    <IconoEditar className="h-4 w-4" />
                  </button>
                  <button
                    type="button"
                    onClick={() => onEliminar(item.id, item.nombre)}
                    className="rounded-lg p-1.5 text-slate-500 transition hover:bg-red-50 hover:text-red-600"
                    aria-label={`Eliminar ${item.nombre}`}
                    title="Eliminar"
                  >
                    <IconoBasura className="h-4 w-4" />
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Página principal de Catálogos
// ---------------------------------------------------------------------------

const ELEMENTOS_POR_PAGINA = 8;

export function Catalogos() {
  // --- Áreas ---
  const {
    data: areas = [],
    isLoading: cargandoAreas,
    isFetching: actualizandoAreas
  } = useAreasAdmin();
  const mutacionEliminarArea = useEliminarAreaMutation();

  const [modalArea, setModalArea] = useState<{ area: Area | null } | null>(null);
  const [eliminarArea, setEliminarArea] = useState<{ id: number; nombre: string } | null>(null);
  const [paginaAreas, setPaginaAreas] = useState(1);
  const [filtroAreas, setFiltroAreas] = useState('');

  const areasFiltradas = useMemo(() => {
    if (!filtroAreas.trim()) return areas;
    const q = filtroAreas.toLowerCase();
    return areas.filter((a) => a.nombre.toLowerCase().includes(q));
  }, [areas, filtroAreas]);

  const totalPaginasAreas = Math.ceil(areasFiltradas.length / ELEMENTOS_POR_PAGINA) || 1;

  useEffect(() => {
    if (paginaAreas > totalPaginasAreas) {
      setPaginaAreas(totalPaginasAreas);
    }
  }, [totalPaginasAreas, paginaAreas]);

  const areasPaginadas = useMemo(() => {
    const inicio = (paginaAreas - 1) * ELEMENTOS_POR_PAGINA;
    return areasFiltradas.slice(inicio, inicio + ELEMENTOS_POR_PAGINA);
  }, [areasFiltradas, paginaAreas]);

  // --- Tipos de solicitud ---
  const {
    data: tipos = [],
    isLoading: cargandoTipos,
    isFetching: actualizandoTipos
  } = useTiposSolicitudAdmin();
  const mutacionEliminarTipo = useEliminarTipoSolicitudMutation();

  const [modalTipo, setModalTipo] = useState<{ tipo: TipoSolicitud | null } | null>(null);
  const [eliminarTipo, setEliminarTipo] = useState<{ id: number; nombre: string } | null>(null);
  const [paginaTipos, setPaginaTipos] = useState(1);
  const [filtroTipos, setFiltroTipos] = useState('');

  const tiposFiltrados = useMemo(() => {
    if (!filtroTipos.trim()) return tipos;
    const q = filtroTipos.toLowerCase();
    return tipos.filter(
      (t) =>
        t.nombre.toLowerCase().includes(q) ||
        (t.descripcion && t.descripcion.toLowerCase().includes(q))
    );
  }, [tipos, filtroTipos]);

  const totalPaginasTipos = Math.ceil(tiposFiltrados.length / ELEMENTOS_POR_PAGINA) || 1;

  useEffect(() => {
    if (paginaTipos > totalPaginasTipos) {
      setPaginaTipos(totalPaginasTipos);
    }
  }, [totalPaginasTipos, paginaTipos]);

  const tiposPaginados = useMemo(() => {
    const inicio = (paginaTipos - 1) * ELEMENTOS_POR_PAGINA;
    return tiposFiltrados.slice(inicio, inicio + ELEMENTOS_POR_PAGINA);
  }, [tiposFiltrados, paginaTipos]);

  // --- Notificaciones globales ---
  const [aviso, setAviso] = useState<string | null>(null);

  const confirmarEliminarArea = async () => {
    if (!eliminarArea) return;
    try {
      await mutacionEliminarArea.mutateAsync(eliminarArea.id);
      setEliminarArea(null);
      setAviso('Área eliminada correctamente.');
    } catch {
      setEliminarArea(null);
    }
  };

  const confirmarEliminarTipo = async () => {
    if (!eliminarTipo) return;
    try {
      await mutacionEliminarTipo.mutateAsync(eliminarTipo.id);
      setEliminarTipo(null);
      setAviso('Tipo de solicitud eliminado correctamente.');
    } catch {
      setEliminarTipo(null);
    }
  };

  return (
    <div className="space-y-8">
      {aviso && <Alerta tono="exito">{aviso}</Alerta>}

      {/* ------------------------------------------------------------------ */}
      {/* Áreas                                                                */}
      {/* ------------------------------------------------------------------ */}
      <Tarjeta
        titulo="Áreas"
        descripcion="Unidades organizativas que emiten solicitudes de servicio."
        acciones={
          <Boton tamano="sm" onClick={() => setModalArea({ area: null })}>
            Nueva área
          </Boton>
        }
      >
        {cargandoAreas ? (
          <Cargando mensaje="Consultando áreas…" />
        ) : areas.length === 0 ? (
          <Vacio
            titulo="Sin áreas"
            descripcion="No hay áreas registradas aún."
            accion={
              <Boton variante="secundario" onClick={() => setModalArea({ area: null })}>
                Crear primera área
              </Boton>
            }
          />
        ) : (
          <div className="space-y-4">
            <div className="relative max-w-xs">
              <IconoBuscar className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input
                type="text"
                value={filtroAreas}
                onChange={(e) => {
                  setFiltroAreas(e.target.value);
                  setPaginaAreas(1);
                }}
                placeholder="Filtrar áreas…"
                className="w-full rounded-lg border border-slate-200 bg-slate-50/50 py-1.5 pl-9 pr-3 text-xs text-tinta placeholder:text-slate-400 focus:border-sb-500 focus:bg-white focus:outline-none focus:ring-2 focus:ring-sb-500/20"
              />
            </div>

            {areasFiltradas.length === 0 ? (
              <p className="py-6 text-center text-sm text-slate-500">
                No se encontraron áreas que coincidan con «{filtroAreas}».
              </p>
            ) : (
              <>
                <TablaItemsCatalogo
                  items={areasPaginadas.map((a) => ({ id: a.id, nombre: a.nombre, activo: a.activa }))}
                  etiquetaActivo="Estado"
                  cargando={actualizandoAreas && !cargandoAreas}
                  onEditar={(id) => {
                    const area = areas.find((a) => a.id === id) ?? null;
                    setModalArea({ area });
                  }}
                  onEliminar={(id, nombre) => setEliminarArea({ id, nombre })}
                />
                <div className="-mx-6 -mb-5">
                  <Paginacion
                    pagina={paginaAreas}
                    totalPaginas={totalPaginasAreas}
                    totalRegistros={areasFiltradas.length}
                    tamanoPagina={ELEMENTOS_POR_PAGINA}
                    onCambiar={setPaginaAreas}
                  />
                </div>
              </>
            )}
          </div>
        )}
      </Tarjeta>

      {/* ------------------------------------------------------------------ */}
      {/* Tipos de solicitud                                                   */}
      {/* ------------------------------------------------------------------ */}
      <Tarjeta
        titulo="Tipos de solicitud"
        descripcion="Clasificaciones de las solicitudes dentro de cada área."
        acciones={
          <Boton tamano="sm" onClick={() => setModalTipo({ tipo: null })}>
            Nuevo tipo
          </Boton>
        }
      >
        {cargandoTipos ? (
          <Cargando mensaje="Consultando tipos de solicitud…" />
        ) : tipos.length === 0 ? (
          <Vacio
            titulo="Sin tipos de solicitud"
            descripcion="No hay tipos de solicitud registrados aún."
            accion={
              <Boton variante="secundario" onClick={() => setModalTipo({ tipo: null })}>
                Crear primer tipo
              </Boton>
            }
          />
        ) : (
          <div className="space-y-4">
            <div className="relative max-w-xs">
              <IconoBuscar className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input
                type="text"
                value={filtroTipos}
                onChange={(e) => {
                  setFiltroTipos(e.target.value);
                  setPaginaTipos(1);
                }}
                placeholder="Filtrar tipos de solicitud…"
                className="w-full rounded-lg border border-slate-200 bg-slate-50/50 py-1.5 pl-9 pr-3 text-xs text-tinta placeholder:text-slate-400 focus:border-sb-500 focus:bg-white focus:outline-none focus:ring-2 focus:ring-sb-500/20"
              />
            </div>

            {tiposFiltrados.length === 0 ? (
              <p className="py-6 text-center text-sm text-slate-500">
                No se encontraron tipos que coincidan con «{filtroTipos}».
              </p>
            ) : (
              <>
                <TablaItemsCatalogo
                  items={tiposPaginados.map((t) => ({
                    id: t.id,
                    nombre: t.nombre,
                    descripcion: t.descripcion,
                    activo: t.activo
                  }))}
                  etiquetaActivo="Estado"
                  cargando={actualizandoTipos && !cargandoTipos}
                  onEditar={(id) => {
                    const tipo = tipos.find((t) => t.id === id) ?? null;
                    setModalTipo({ tipo });
                  }}
                  onEliminar={(id, nombre) => setEliminarTipo({ id, nombre })}
                />
                <div className="-mx-6 -mb-5">
                  <Paginacion
                    pagina={paginaTipos}
                    totalPaginas={totalPaginasTipos}
                    totalRegistros={tiposFiltrados.length}
                    tamanoPagina={ELEMENTOS_POR_PAGINA}
                    onCambiar={setPaginaTipos}
                  />
                </div>
              </>
            )}
          </div>
        )}
      </Tarjeta>

      {/* ------------------------------------------------------------------ */}
      {/* Modals — Área                                                        */}
      {/* ------------------------------------------------------------------ */}
      {modalArea && (
        <ModalArea
          abierta={modalArea !== null}
          area={modalArea.area}
          onCerrar={() => setModalArea(null)}
          onGuardada={(msg) => {
            setModalArea(null);
            setAviso(msg);
          }}
          onError={(msg) => setAviso(msg)}
        />
      )}

      <Modal
        abierto={eliminarArea !== null}
        titulo="Eliminar área"
        descripcion="Esta acción no se puede deshacer."
        onCerrar={() => setEliminarArea(null)}
        pie={
          <>
            <Boton variante="secundario" type="button" onClick={() => setEliminarArea(null)}>
              Cancelar
            </Boton>
            <Boton
              variante="peligro"
              type="button"
              cargando={mutacionEliminarArea.isPending}
              onClick={() => void confirmarEliminarArea()}
            >
              Eliminar definitivamente
            </Boton>
          </>
        }
      >
        <p className="text-sm text-slate-600">
          Se eliminará el área <strong className="text-sb-900">{eliminarArea?.nombre}</strong>.
        </p>
      </Modal>

      {/* ------------------------------------------------------------------ */}
      {/* Modals — Tipo de solicitud                                           */}
      {/* ------------------------------------------------------------------ */}
      {modalTipo && (
        <ModalTipoSolicitud
          abierta={modalTipo !== null}
          tipo={modalTipo.tipo}
          onCerrar={() => setModalTipo(null)}
          onGuardada={(msg) => {
            setModalTipo(null);
            setAviso(msg);
          }}
          onError={(msg) => setAviso(msg)}
        />
      )}

      <Modal
        abierto={eliminarTipo !== null}
        titulo="Eliminar tipo de solicitud"
        descripcion="Esta acción no se puede deshacer."
        onCerrar={() => setEliminarTipo(null)}
        pie={
          <>
            <Boton variante="secundario" type="button" onClick={() => setEliminarTipo(null)}>
              Cancelar
            </Boton>
            <Boton
              variante="peligro"
              type="button"
              cargando={mutacionEliminarTipo.isPending}
              onClick={() => void confirmarEliminarTipo()}
            >
              Eliminar definitivamente
            </Boton>
          </>
        }
      >
        <p className="text-sm text-slate-600">
          Se eliminará el tipo{' '}
          <strong className="text-sb-900">{eliminarTipo?.nombre}</strong>.
        </p>
      </Modal>
    </div>
  );
}
