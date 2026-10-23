import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alerta,
  Boton,
  Campo,
  Cargando,
  Entrada,
  Modal,
  Seleccion,
  Tarjeta,
  Vacio
} from '../componentes/Diseno';
import { InsigniaActiva } from '../componentes/Insignias';
import { IconoBasura, IconoEditar, IconoFiltro, IconoBuscar } from '../componentes/Iconos';
import { Paginacion } from '../componentes/Paginacion';
import { ErrorApi } from '../lib/api';
import {
  useActualizarEntidadMutation,
  useActualizarOpcionCatalogoMutation,
  useConsultarEntidades,
  useCrearEntidadMutation,
  useCrearOpcionCatalogoMutation,
  useEliminarEntidadMutation,
  useEliminarOpcionCatalogoMutation,
  useOpcionesEntidades
} from '../lib/consultas';
import { formatearFecha } from '../lib/formato';
import type {
  CrearEntidad,
  EntidadGubernamental,
  OpcionesFiltroEntidades,
  TipoCatalogoEntidad
} from '../lib/tipos';
import { useSesion } from '../sesion/ContextoSesion';

interface Filtros {
  texto: string;
  categoria: string;
  poderDelEstado: string;
  sector: string;
  activa: '' | 'true' | 'false';
}

const FILTROS_VACIOS: Filtros = {
  texto: '',
  categoria: '',
  poderDelEstado: '',
  sector: '',
  activa: ''
};

interface PropiedadesModalEntidad {
  abierta: boolean;
  entidad: EntidadGubernamental | null;
  opciones: OpcionesFiltroEntidades | null;
  onCerrar: () => void;
  onGuardada: (mensaje: string) => void;
  onError: (mensaje: string) => void;
}

const FORMULARIO_VACIO: CrearEntidad = {
  nombre: '',
  categoria: '',
  poderDelEstado: '',
  sector: '',
  activa: true
};

function ModalEntidad({
  abierta,
  entidad,
  opciones,
  onCerrar,
  onGuardada,
  onError
}: PropiedadesModalEntidad) {
  const mutacionCrear = useCrearEntidadMutation();
  const mutacionActualizar = useActualizarEntidadMutation();

  const [formulario, setFormulario] = useState<CrearEntidad>(() =>
    entidad
      ? {
          nombre: entidad.nombre,
          categoria: entidad.categoria,
          poderDelEstado: entidad.poderDelEstado,
          sector: entidad.sector,
          activa: entidad.activa
        }
      : FORMULARIO_VACIO
  );
  const [errores, setErrores] = useState<Record<string, string>>({});
  const [customCat, setCustomCat] = useState(false);
  const [customPoder, setCustomPoder] = useState(false);
  const [customSector, setCustomSector] = useState(false);

  const actualizar = <C extends keyof CrearEntidad>(campo: C, valor: CrearEntidad[C]) => {
    setFormulario((actual) => ({ ...actual, [campo]: valor }));
    setErrores((actuales) => {
      if (!actuales[campo]) {
        return actuales;
      }

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

    if (!formulario.categoria.trim()) {
      nuevos.categoria = 'Debe seleccionar o indicar una categoría.';
    }

    if (!formulario.poderDelEstado.trim()) {
      nuevos.poderDelEstado = 'Debe seleccionar o indicar el poder del estado.';
    }

    if (!formulario.sector.trim()) {
      nuevos.sector = 'Debe seleccionar o indicar un sector.';
    }

    setErrores(nuevos);

    if (Object.keys(nuevos).length > 0) {
      return;
    }

    try {
      const peticion: CrearEntidad = {
        nombre: formulario.nombre.trim(),
        categoria: formulario.categoria.trim(),
        poderDelEstado: formulario.poderDelEstado.trim(),
        sector: formulario.sector.trim(),
        activa: formulario.activa
      };

      if (entidad) {
        await mutacionActualizar.mutateAsync({ id: entidad.id, peticion });
        onGuardada('Entidad actualizada correctamente.');
      } else {
        await mutacionCrear.mutateAsync(peticion);
        onGuardada('Entidad registrada correctamente.');
      }
    } catch (excepcion) {
      onError(excepcion instanceof ErrorApi ? excepcion.message : 'No fue posible guardar la entidad.');
    }
  };

  const guardando = mutacionCrear.isPending || mutacionActualizar.isPending;

  return (
    <Modal
      abierto={abierta}
      titulo={entidad ? 'Editar entidad' : 'Nueva entidad gubernamental'}
      descripcion="El catálogo se almacena en el archivo de texto plano del proyecto."
      onCerrar={onCerrar}
      ancho="amplio"
      pie={
        <>
          <Boton variante="secundario" type="button" onClick={onCerrar}>
            Cancelar
          </Boton>
          <Boton type="submit" form="formulario-entidad" cargando={guardando}>
            {entidad ? 'Guardar cambios' : 'Registrar entidad'}
          </Boton>
        </>
      }
    >
      <form id="formulario-entidad" className="space-y-5" onSubmit={enviar} noValidate>
        <Campo etiqueta="Nombre" para="entidad-nombre" requerido error={errores.nombre}>
          <Entrada
            id="entidad-nombre"
            value={formulario.nombre}
            onChange={(evento) => actualizar('nombre', evento.target.value)}
            maxLength={200}
            placeholder="Nombre oficial de la entidad"
          />
        </Campo>

        <div className="grid gap-5 sm:grid-cols-2">
          <Campo
            etiqueta="Categoría"
            acciones={
              <button
                type="button"
                onClick={() => setCustomCat(!customCat)}
                className="text-xs font-normal text-acento hover:underline"
              >
                {customCat ? 'Elegir de lista' : '+ Nueva opción'}
              </button>
            }
            para="entidad-categoria"
            requerido
            error={errores.categoria}
          >
            {customCat ? (
              <Entrada
                id="entidad-categoria"
                value={formulario.categoria}
                onChange={(evento) => actualizar('categoria', evento.target.value)}
                maxLength={120}
                placeholder="Escriba nueva categoría..."
                autoFocus
              />
            ) : (
              <Seleccion
                id="entidad-categoria"
                value={formulario.categoria}
                onChange={(evento) => actualizar('categoria', evento.target.value)}
                disabled={!opciones}
              >
                <option value="">— Seleccione una categoría —</option>
                {opciones?.categorias.map((categoria) => (
                  <option key={categoria} value={categoria}>
                    {categoria}
                  </option>
                ))}
                {formulario.categoria && !opciones?.categorias.includes(formulario.categoria) && (
                  <option value={formulario.categoria}>{formulario.categoria}</option>
                )}
              </Seleccion>
            )}
          </Campo>

          <Campo
            etiqueta="Poder del estado"
            acciones={
              <button
                type="button"
                onClick={() => setCustomPoder(!customPoder)}
                className="text-xs font-normal text-acento hover:underline"
              >
                {customPoder ? 'Elegir de lista' : '+ Nueva opción'}
              </button>
            }
            para="entidad-poder"
            requerido
            error={errores.poderDelEstado}
          >
            {customPoder ? (
              <Entrada
                id="entidad-poder"
                value={formulario.poderDelEstado}
                onChange={(evento) => actualizar('poderDelEstado', evento.target.value)}
                maxLength={80}
                placeholder="Escriba nuevo poder..."
                autoFocus
              />
            ) : (
              <Seleccion
                id="entidad-poder"
                value={formulario.poderDelEstado}
                onChange={(evento) => actualizar('poderDelEstado', evento.target.value)}
                disabled={!opciones}
              >
                <option value="">— Seleccione el poder —</option>
                {opciones?.poderesDelEstado.map((poder) => (
                  <option key={poder} value={poder}>
                    {poder}
                  </option>
                ))}
                {formulario.poderDelEstado && !opciones?.poderesDelEstado.includes(formulario.poderDelEstado) && (
                  <option value={formulario.poderDelEstado}>{formulario.poderDelEstado}</option>
                )}
              </Seleccion>
            )}
          </Campo>
        </div>

        <div className="grid gap-5 sm:grid-cols-2">
          <Campo
            etiqueta="Sector"
            acciones={
              <button
                type="button"
                onClick={() => setCustomSector(!customSector)}
                className="text-xs font-normal text-acento hover:underline"
              >
                {customSector ? 'Elegir de lista' : '+ Nueva opción'}
              </button>
            }
            para="entidad-sector"
            requerido
            error={errores.sector}
          >
            {customSector ? (
              <Entrada
                id="entidad-sector"
                value={formulario.sector}
                onChange={(evento) => actualizar('sector', evento.target.value)}
                maxLength={120}
                placeholder="Escriba nuevo sector..."
                autoFocus
              />
            ) : (
              <Seleccion
                id="entidad-sector"
                value={formulario.sector}
                onChange={(evento) => actualizar('sector', evento.target.value)}
                disabled={!opciones}
              >
                <option value="">— Seleccione un sector —</option>
                {opciones?.sectores.map((sector) => (
                  <option key={sector} value={sector}>
                    {sector}
                  </option>
                ))}
                {formulario.sector && !opciones?.sectores.includes(formulario.sector) && (
                  <option value={formulario.sector}>{formulario.sector}</option>
                )}
              </Seleccion>
            )}
          </Campo>

          <Campo etiqueta="Estado" para="entidad-activa">
            <label className="flex h-[42px] items-center gap-2 rounded-lg border border-slate-300 bg-white px-3 text-sm text-slate-700">
              <input
                id="entidad-activa"
                type="checkbox"
                className="h-4 w-4 rounded border-slate-300 text-acento focus:ring-acento/40"
                checked={formulario.activa}
                onChange={(evento) => actualizar('activa', evento.target.checked)}
              />
              Entidad activa
            </label>
          </Campo>
        </div>
      </form>
    </Modal>
  );
}

interface PropiedadesModalGestionCatalogos {
  abierta: boolean;
  opciones: OpcionesFiltroEntidades | null;
  onCerrar: () => void;
  onAviso: (mensaje: string) => void;
}

function ModalGestionCatalogos({
  abierta,
  opciones,
  onCerrar,
  onAviso
}: PropiedadesModalGestionCatalogos) {
  const [tipoActivo, setTipoActivo] = useState<TipoCatalogoEntidad>('categorias');
  const [nuevoValor, setNuevoValor] = useState('');
  const [editando, setEditando] = useState<{ original: string; nuevo: string } | null>(null);
  const [porEliminar, setPorEliminar] = useState<string | null>(null);
  const [errorLocal, setErrorLocal] = useState<string | null>(null);

  const mutacionCrear = useCrearOpcionCatalogoMutation();
  const mutacionActualizar = useActualizarOpcionCatalogoMutation();
  const mutacionEliminar = useEliminarOpcionCatalogoMutation();

  const items =
    tipoActivo === 'categorias'
      ? opciones?.categorias ?? []
      : tipoActivo === 'poderes-del-estado'
      ? opciones?.poderesDelEstado ?? []
      : opciones?.sectores ?? [];

  const etiquetaTipo =
    tipoActivo === 'categorias'
      ? 'Categoría'
      : tipoActivo === 'poderes-del-estado'
      ? 'Poder del Estado'
      : 'Sector';

  const agregar = async (e: FormEvent) => {
    e.preventDefault();
    if (!nuevoValor.trim() || nuevoValor.trim().length < 2) {
      setErrorLocal('El valor debe tener al menos 2 caracteres.');
      return;
    }
    setErrorLocal(null);
    try {
      await mutacionCrear.mutateAsync({ tipo: tipoActivo, valor: nuevoValor.trim() });
      setNuevoValor('');
      onAviso(`${etiquetaTipo} agregada correctamente.`);
    } catch (err) {
      setErrorLocal(err instanceof ErrorApi ? err.message : 'No fue posible agregar el elemento.');
    }
  };

  const guardarEdicion = async () => {
    if (!editando || !editando.nuevo.trim() || editando.nuevo.trim().length < 2) {
      setErrorLocal('El nuevo valor debe tener al menos 2 caracteres.');
      return;
    }
    setErrorLocal(null);
    try {
      await mutacionActualizar.mutateAsync({
        tipo: tipoActivo,
        peticion: { valorActual: editando.original, nuevoValor: editando.nuevo.trim() }
      });
      setEditando(null);
      onAviso(`${etiquetaTipo} actualizada correctamente.`);
    } catch (err) {
      setErrorLocal(err instanceof ErrorApi ? err.message : 'No fue posible actualizar el elemento.');
    }
  };

  const confirmarEliminar = async () => {
    if (!porEliminar) return;
    setErrorLocal(null);
    try {
      await mutacionEliminar.mutateAsync({ tipo: tipoActivo, valor: porEliminar });
      setPorEliminar(null);
      onAviso(`${etiquetaTipo} eliminada correctamente.`);
    } catch (err) {
      setErrorLocal(err instanceof ErrorApi ? err.message : 'No fue posible eliminar el elemento.');
      setPorEliminar(null);
    }
  };

  return (
    <Modal
      abierto={abierta}
      titulo="Gestión de catálogos de entidades"
      descripcion="Administre las opciones de los dropdowns (Categorías, Poderes y Sectores)."
      onCerrar={onCerrar}
      ancho="amplio"
      pie={
        <Boton variante="secundario" type="button" onClick={onCerrar}>
          Cerrar
        </Boton>
      }
    >
      <div className="space-y-4">
        {errorLocal && <Alerta tono="error">{errorLocal}</Alerta>}

        <div className="flex border-b border-slate-200">
          {(
            [
              { id: 'categorias', etiqueta: 'Categorías', count: opciones?.categorias.length ?? 0 },
              { id: 'poderes-del-estado', etiqueta: 'Poderes del Estado', count: opciones?.poderesDelEstado.length ?? 0 },
              { id: 'sectores', etiqueta: 'Sectores', count: opciones?.sectores.length ?? 0 }
            ] as const
          ).map((tab) => (
            <button
              key={tab.id}
              type="button"
              onClick={() => {
                setTipoActivo(tab.id);
                setEditando(null);
                setErrorLocal(null);
              }}
              className={`flex items-center gap-2 border-b-2 px-4 py-2.5 text-sm font-medium transition ${
                tipoActivo === tab.id
                  ? 'border-acento text-acento font-semibold'
                  : 'border-transparent text-slate-500 hover:text-slate-800'
              }`}
            >
              <span>{tab.etiqueta}</span>
              <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-600">
                {tab.count}
              </span>
            </button>
          ))}
        </div>

        <form onSubmit={agregar} className="flex gap-2">
          <Entrada
            value={nuevoValor}
            onChange={(e) => setNuevoValor(e.target.value)}
            placeholder={`Nueva ${etiquetaTipo.toLowerCase()}...`}
            maxLength={120}
          />
          <Boton type="submit" cargando={mutacionCrear.isPending} className="shrink-0">
            Agregar
          </Boton>
        </form>

        <div className="max-h-80 overflow-y-auto divide-y divide-slate-100 rounded-lg border border-slate-200 bg-white">
          {items.length === 0 ? (
            <div className="p-4 text-center text-sm text-slate-400">No hay elementos registrados.</div>
          ) : (
            items.map((item) => (
              <div key={item} className="flex items-center justify-between p-3 transition hover:bg-slate-50">
                {editando?.original === item ? (
                  <div className="flex flex-1 items-center gap-2">
                    <Entrada
                      value={editando.nuevo}
                      onChange={(e) => setEditando({ ...editando, nuevo: e.target.value })}
                      className="py-1 text-sm"
                      maxLength={120}
                      autoFocus
                    />
                    <Boton tamano="sm" onClick={guardarEdicion} cargando={mutacionActualizar.isPending}>
                      Guardar
                    </Boton>
                    <Boton tamano="sm" variante="secundario" onClick={() => setEditando(null)}>
                      Cancelar
                    </Boton>
                  </div>
                ) : (
                  <>
                    <span className="text-sm font-medium text-sb-900">{item}</span>
                    <div className="flex items-center gap-1">
                      <button
                        type="button"
                        onClick={() => setEditando({ original: item, nuevo: item })}
                        className="rounded p-1 text-slate-400 hover:text-sb-800"
                        title="Renombrar en todo el catálogo"
                      >
                        <IconoEditar className="h-4 w-4" />
                      </button>
                      <button
                        type="button"
                        onClick={() => setPorEliminar(item)}
                        className="rounded p-1 text-slate-400 hover:text-red-600"
                        title="Eliminar de las opciones"
                      >
                        <IconoBasura className="h-4 w-4" />
                      </button>
                    </div>
                  </>
                )}
              </div>
            ))
          )}
        </div>

        {porEliminar && (
          <Modal
            abierto={Boolean(porEliminar)}
            titulo={`Eliminar ${etiquetaTipo.toLowerCase()}`}
            descripcion="Esta acción retirará la opción del catálogo."
            onCerrar={() => setPorEliminar(null)}
            pie={
              <>
                <Boton variante="secundario" type="button" onClick={() => setPorEliminar(null)}>
                  Cancelar
                </Boton>
                <Boton
                  variante="peligro"
                  type="button"
                  cargando={mutacionEliminar.isPending}
                  onClick={confirmarEliminar}
                >
                  Eliminar definitivamente
                </Boton>
              </>
            }
          >
            <p className="text-sm text-slate-600">
              ¿Está seguro de que desea eliminar <strong className="text-sb-900">{porEliminar}</strong> de las opciones?
            </p>
          </Modal>
        )}
      </div>
    </Modal>
  );
}

export function Entidades() {
  const { usuario } = useSesion();
  const esAdministrador = usuario?.rol === 'Administrador';

  const [filtros, setFiltros] = useState<Filtros>(FILTROS_VACIOS);
  const [pagina, setPagina] = useState(1);
  const [aviso, setAviso] = useState<string | null>(null);
  const [errorEliminacion, setErrorEliminacion] = useState<string | null>(null);
  const [modal, setModal] = useState<{ entidad: EntidadGubernamental | null } | null>(null);
  const [modalCatalogos, setModalCatalogos] = useState(false);
  const [porEliminar, setPorEliminar] = useState<EntidadGubernamental | null>(null);

  const { data: opciones = null } = useOpcionesEntidades();

  const filtrosConsulta = {
    texto: filtros.texto,
    categoria: filtros.categoria,
    poderDelEstado: filtros.poderDelEstado,
    sector: filtros.sector,
    activa: (filtros.activa === '' ? '' : filtros.activa === 'true') as boolean | '' | undefined,
    pagina,
    tamanoPagina: 10
  };

  const {
    data: datos,
    isLoading: cargandoInicial,
    isFetching: cargando,
    error: errorQuery
  } = useConsultarEntidades(filtrosConsulta);

  const mutacionEliminar = useEliminarEntidadMutation();

  const error = errorQuery
    ? errorQuery instanceof ErrorApi
      ? errorQuery.message
      : 'No fue posible consultar las entidades.'
    : null;

  const actualizarFiltro = <C extends keyof Filtros>(campo: C, valor: Filtros[C]) => {
    setFiltros((actual) => ({ ...actual, [campo]: valor }));
    setPagina(1);
  };

  const limpiar = () => {
    setFiltros(FILTROS_VACIOS);
    setPagina(1);
  };

  const eliminar = async () => {
    if (!porEliminar) {
      return;
    }

    try {
      await mutacionEliminar.mutateAsync(porEliminar.id);
      setPorEliminar(null);
      setErrorEliminacion(null);
      setAviso('Entidad eliminada del catálogo.');
    } catch (excepcion) {
      setErrorEliminacion(
        excepcion instanceof ErrorApi ? excepcion.message : 'No fue posible eliminar la entidad.'
      );
    }
  };

  const hayFiltros = Boolean(
    filtros.texto || filtros.categoria || filtros.poderDelEstado || filtros.sector || filtros.activa
  );

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <p className="max-w-2xl text-sm text-slate-500">
          Mantenimiento del catálogo oficial de la República Dominicana persistido en archivo plano. {datos?.totalRegistros ?? '—'} registros
          {hayFiltros ? ' filtrados' : ' en total'}.
        </p>
        {esAdministrador && (
          <div className="flex items-center gap-2">
            <Boton variante="secundario" className="shrink-0" onClick={() => setModalCatalogos(true)}>
              Gestionar catálogos
            </Boton>
            <Boton className="shrink-0" onClick={() => setModal({ entidad: null })}>
              Nueva entidad
            </Boton>
          </div>
        )}
      </div>

      {aviso && <Alerta tono="exito">{aviso}</Alerta>}
      {error && <Alerta tono="error">{error}</Alerta>}

      <Tarjeta>
        <div className="mb-4 flex items-center justify-between">
          <div className="flex items-center gap-2 text-sm font-semibold text-sb-900">
            <IconoFiltro className="h-4 w-4 text-acento" />
            Filtros del catálogo
          </div>
          {cargando && !cargandoInicial && (
            <span className="text-xs text-slate-400 animate-pulse">Actualizando catálogo…</span>
          )}
        </div>

        <div className="grid gap-4 lg:grid-cols-12">
          <div className="lg:col-span-3">
            <label htmlFor="entidad-texto" className="mb-1.5 block text-sm font-medium text-sb-900">
              Buscar
            </label>
            <div className="relative">
              <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400">
                <IconoBuscar className="h-4 w-4" />
              </span>
              <Entrada
                id="entidad-texto"
                className="pl-9"
                value={filtros.texto}
                onChange={(evento) => actualizarFiltro('texto', evento.target.value)}
                placeholder="Nombre de la entidad…"
              />
            </div>
          </div>

          <div className="lg:col-span-3">
            <label htmlFor="entidad-filtro-categoria" className="mb-1.5 block text-sm font-medium text-sb-900">
              Categoría
            </label>
            <Seleccion
              id="entidad-filtro-categoria"
              value={filtros.categoria}
              onChange={(evento) => actualizarFiltro('categoria', evento.target.value)}
            >
              <option value="">Todas</option>
              {opciones?.categorias.map((categoria) => (
                <option key={categoria} value={categoria}>
                  {categoria}
                </option>
              ))}
            </Seleccion>
          </div>

          <div className="lg:col-span-2">
            <label htmlFor="entidad-filtro-poder" className="mb-1.5 block text-sm font-medium text-sb-900">
              Poder
            </label>
            <Seleccion
              id="entidad-filtro-poder"
              value={filtros.poderDelEstado}
              onChange={(evento) => actualizarFiltro('poderDelEstado', evento.target.value)}
            >
              <option value="">Todos</option>
              {opciones?.poderesDelEstado.map((poder) => (
                <option key={poder} value={poder}>
                  {poder}
                </option>
              ))}
            </Seleccion>
          </div>

          <div className="lg:col-span-2">
            <label htmlFor="entidad-filtro-sector" className="mb-1.5 block text-sm font-medium text-sb-900">
              Sector
            </label>
            <Seleccion
              id="entidad-filtro-sector"
              value={filtros.sector}
              onChange={(evento) => actualizarFiltro('sector', evento.target.value)}
            >
              <option value="">Todos</option>
              {opciones?.sectores.map((sector) => (
                <option key={sector} value={sector}>
                  {sector}
                </option>
              ))}
            </Seleccion>
          </div>

          <div className="lg:col-span-2">
            <label htmlFor="entidad-filtro-activa" className="mb-1.5 block text-sm font-medium text-sb-900">
              Estado
            </label>
            <Seleccion
              id="entidad-filtro-activa"
              value={filtros.activa}
              onChange={(evento) => actualizarFiltro('activa', evento.target.value as Filtros['activa'])}
            >
              <option value="">Todas</option>
              <option value="true">Activas</option>
              <option value="false">Inactivas</option>
            </Seleccion>
          </div>

          {hayFiltros && (
            <div className="flex items-end lg:col-span-12">
              <Boton variante="fantasma" tamano="sm" onClick={limpiar}>
                Limpiar filtros
              </Boton>
            </div>
          )}
        </div>
      </Tarjeta>

      <Tarjeta className="overflow-hidden">
        {cargandoInicial && !datos ? (
          <Cargando mensaje="Consultando el catálogo…" />
        ) : !datos || datos.elementos.length === 0 ? (
          <Vacio
            titulo="Sin entidades"
            descripcion={
              hayFiltros
                ? 'Ninguna entidad coincide con los filtros aplicados.'
                : 'El catálogo está vacío.'
            }
            accion={
              hayFiltros ? (
                <Boton variante="secundario" onClick={limpiar}>
                  Limpiar filtros
                </Boton>
              ) : undefined
            }
          />
        ) : (
          <>
            <div className="-mx-6 -my-5 overflow-x-auto">
              <table className="w-full min-w-[860px] table-fixed text-left text-sm">
                <thead>
                  <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
                    <th className="px-6 py-3 font-semibold">Entidad</th>
                    <th className="w-[150px] px-4 py-3 font-semibold">Categoría</th>
                    <th className="w-[96px] px-4 py-3 font-semibold">Poder</th>
                    <th className="w-[148px] px-4 py-3 font-semibold">Sector</th>
                    <th className="w-[84px] px-4 py-3 font-semibold">Estado</th>
                    <th className="w-[108px] px-4 py-3 font-semibold">Actualizada</th>
                    {esAdministrador && (
                      <th className="w-[78px] px-2 py-3 text-right font-semibold">
                        <span className="sr-only">Acciones</span>
                      </th>
                    )}
                  </tr>
                </thead>
                <tbody className={`divide-y divide-slate-100 ${cargando ? 'opacity-60' : ''}`}>
                  {datos.elementos.map((entidad) => (
                    <tr key={entidad.id} className="transition hover:bg-slate-50/80">
                      <td className="truncate px-6 py-3.5 font-medium text-sb-900" title={entidad.nombre}>
                        {entidad.nombre}
                      </td>
                      <td className="truncate px-4 py-3.5 text-slate-600" title={entidad.categoria}>
                        {entidad.categoria}
                      </td>
                      <td className="truncate px-4 py-3.5 text-slate-600" title={entidad.poderDelEstado}>
                        {entidad.poderDelEstado}
                      </td>
                      <td className="truncate px-4 py-3.5 text-slate-600" title={entidad.sector}>
                        {entidad.sector}
                      </td>
                      <td className="px-4 py-3.5">
                        <InsigniaActiva activa={entidad.activa} />
                      </td>
                      <td className="whitespace-nowrap ${esAdministrador ? 'px-4' : 'px-6'} py-3.5 text-slate-500">
                        {formatearFecha(entidad.fechaActualizacion)}
                      </td>
                      {esAdministrador && (
                        <td className="whitespace-nowrap px-2 py-3.5 text-right">
                          <div className="inline-flex items-center gap-0.5">
                            <button
                              type="button"
                              onClick={() => setModal({ entidad })}
                              className="rounded-lg p-1.5 text-slate-500 transition hover:bg-sb-900/5 hover:text-sb-900"
                              aria-label={`Editar ${entidad.nombre}`}
                              title="Editar"
                            >
                              <IconoEditar className="h-4 w-4" />
                            </button>
                            <button
                              type="button"
                              onClick={() => {
                                setAviso(null);
                                setErrorEliminacion(null);
                                setPorEliminar(entidad);
                              }}
                              className="rounded-lg p-1.5 text-slate-500 transition hover:bg-red-50 hover:text-red-600"
                              aria-label={`Eliminar ${entidad.nombre}`}
                              title="Eliminar"
                            >
                              <IconoBasura className="h-4 w-4" />
                            </button>
                          </div>
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="-mx-6 -mb-5">
              <Paginacion
                pagina={datos.pagina}
                totalPaginas={datos.totalPaginas}
                totalRegistros={datos.totalRegistros}
                tamanoPagina={datos.tamanoPagina}
                onCambiar={setPagina}
              />
            </div>
          </>
        )}
      </Tarjeta>

      {esAdministrador && (
        <>
          {modal && (
            <ModalEntidad
              abierta={modal !== null}
              entidad={modal.entidad}
              opciones={opciones}
              onCerrar={() => setModal(null)}
              onGuardada={(mensaje) => {
                setModal(null);
                setAviso(mensaje);
              }}
              onError={(mensaje) => setAviso(mensaje)}
            />
          )}

          {modalCatalogos && (
            <ModalGestionCatalogos
              abierta={modalCatalogos}
              opciones={opciones}
              onCerrar={() => setModalCatalogos(false)}
              onAviso={(mensaje) => setAviso(mensaje)}
            />
          )}

          <Modal
            abierto={porEliminar !== null}
            titulo="Eliminar entidad"
            descripcion="Esta acción no se puede deshacer."
            onCerrar={() => {
              setPorEliminar(null);
              setErrorEliminacion(null);
            }}
            pie={
              <>
                <Boton
                  variante="secundario"
                  type="button"
                  onClick={() => {
                    setPorEliminar(null);
                    setErrorEliminacion(null);
                  }}
                >
                  Cancelar
                </Boton>
                <Boton
                  variante="peligro"
                  type="button"
                  cargando={mutacionEliminar.isPending}
                  onClick={() => void eliminar()}
                >
                  Eliminar definitivamente
                </Boton>
              </>
            }
          >
            <p className="text-sm text-slate-600">
              Se eliminará <strong className="text-sb-900">{porEliminar?.nombre}</strong> del catálogo de
              entidades gubernamentales.
            </p>
            {errorEliminacion && <Alerta tono="error">{errorEliminacion}</Alerta>}
          </Modal>
        </>
      )}
    </div>
  );
}
