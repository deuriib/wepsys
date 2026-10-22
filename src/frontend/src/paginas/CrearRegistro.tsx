import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Alerta, AreaTexto, Boton, Campo, Entrada, Seleccion, Tarjeta } from '../componentes/Diseno';
import { IconoVolver } from '../componentes/Iconos';
import { ErrorApi } from '../lib/api';
import { desdeEntradaFecha } from '../lib/formato';
import type { Area, PrioridadSolicitud, TipoSolicitud } from '../lib/tipos';
import { useSesion } from '../sesion/ContextoSesion';

const PRIORIDADES: Array<{ valor: PrioridadSolicitud; etiqueta: string }> = [
  { valor: 'Baja', etiqueta: 'Baja' },
  { valor: 'Media', etiqueta: 'Media' },
  { valor: 'Alta', etiqueta: 'Alta' },
  { valor: 'Critica', etiqueta: 'Crítica' }
];

const ETAPAS = [
  'La solicitud se crea en estado Registrada y recibe un código único.',
  'Un analista la toma, la asigna y la mueve a En análisis o En progreso.',
  'Si necesita información del solicitante, pasa a En espera del solicitante.',
  'Al resolverse se marca Resuelta; el cierre requiere un comentario de confirmación.'
];

interface Formulario {
  titulo: string;
  descripcion: string;
  prioridad: PrioridadSolicitud;
  areaId: string;
  tipoSolicitudId: string;
  fechaCompromiso: string;
  urlEvidencia: string;
}

function fechaPorDefecto(): string {
  const fecha = new Date();
  fecha.setDate(fecha.getDate() + 5);
  return fecha.toISOString().slice(0, 10);
}

function mensajeDeCampo(errores: Record<string, string[]>, campo: string): string | undefined {
  const clave = Object.keys(errores).find((nombre) => nombre.toLowerCase() === campo.toLowerCase());
  return clave ? errores[clave]?.[0] : undefined;
}

export function CrearRegistro() {
  const { cliente, usuario } = useSesion();
  const navegar = useNavigate();
  const [areas, setAreas] = useState<Area[]>([]);
  const [tipos, setTipos] = useState<TipoSolicitud[]>([]);
  const [formulario, setFormulario] = useState<Formulario>({
    titulo: '',
    descripcion: '',
    prioridad: 'Media',
    areaId: '',
    tipoSolicitudId: '',
    fechaCompromiso: fechaPorDefecto(),
    urlEvidencia: ''
  });
  const [errores, setErrores] = useState<Record<string, string>>({});
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    let vigente = true;

    Promise.all([cliente.obtenerAreas(), cliente.obtenerTiposSolicitud()])
      .then(([areasCargadas, tiposCargados]) => {
        if (!vigente) {
          return;
        }

        setAreas(areasCargadas);
        setTipos(tiposCargados);
        setFormulario((actual) => ({
          ...actual,
          areaId: actual.areaId || String(areasCargadas[0]?.id ?? ''),
          tipoSolicitudId: actual.tipoSolicitudId || String(tiposCargados[0]?.id ?? '')
        }));
      })
      .catch((excepcion: unknown) => {
        if (vigente) {
          setErrorGeneral(
            excepcion instanceof ErrorApi ? excepcion.message : 'No fue posible cargar los catálogos.'
          );
        }
      });

    return () => {
      vigente = false;
    };
  }, [cliente]);

  const actualizar = <C extends keyof Formulario>(campo: C, valor: Formulario[C]) => {
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

  const validar = (): boolean => {
    const nuevos: Record<string, string> = {};

    if (formulario.titulo.trim().length < 5) {
      nuevos.titulo = 'El título debe tener al menos 5 caracteres.';
    }

    if (formulario.descripcion.trim().length < 10) {
      nuevos.descripcion = 'La descripción debe tener al menos 10 caracteres.';
    }

    if (!formulario.areaId) {
      nuevos.areaId = 'Seleccione un área.';
    }

    if (!formulario.tipoSolicitudId) {
      nuevos.tipoSolicitudId = 'Seleccione un tipo de solicitud.';
    }

    if (!formulario.fechaCompromiso) {
      nuevos.fechaCompromiso = 'Indique una fecha de compromiso.';
    }

    if (formulario.urlEvidencia.length > 500) {
      nuevos.urlEvidencia = 'La referencia no puede superar 500 caracteres.';
    }

    setErrores(nuevos);
    return Object.keys(nuevos).length === 0;
  };

  const enviar = async (evento: FormEvent<HTMLFormElement>) => {
    evento.preventDefault();
    setErrorGeneral(null);

    if (!validar()) {
      return;
    }

    const fechaCompromiso = desdeEntradaFecha(formulario.fechaCompromiso);

    if (!fechaCompromiso) {
      setErrores((actuales) => ({ ...actuales, fechaCompromiso: 'La fecha no es válida.' }));
      return;
    }

    setEnviando(true);

    try {
      const creada = await cliente.crearSolicitud({
        titulo: formulario.titulo.trim(),
        descripcion: formulario.descripcion.trim(),
        prioridad: formulario.prioridad,
        areaId: Number(formulario.areaId),
        tipoSolicitudId: Number(formulario.tipoSolicitudId),
        fechaCompromiso,
        urlEvidencia: formulario.urlEvidencia.trim() || null
      });

      navegar(`/solicitudes/${creada.id}`, { state: { creada: true } });
    } catch (excepcion) {
      if (excepcion instanceof ErrorApi) {
        if (excepcion.errores) {
          setErrores({
            titulo: mensajeDeCampo(excepcion.errores, 'titulo') ?? '',
            descripcion: mensajeDeCampo(excepcion.errores, 'descripcion') ?? '',
            areaId: mensajeDeCampo(excepcion.errores, 'areaId') ?? '',
            tipoSolicitudId: mensajeDeCampo(excepcion.errores, 'tipoSolicitudId') ?? '',
            fechaCompromiso: mensajeDeCampo(excepcion.errores, 'fechaCompromiso') ?? '',
            urlEvidencia: mensajeDeCampo(excepcion.errores, 'urlEvidencia') ?? ''
          });
        }

        setErrorGeneral(excepcion.message);
      } else {
        setErrorGeneral('No fue posible registrar la solicitud.');
      }
    } finally {
      setEnviando(false);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <Link to="/consulta" className="mb-3 inline-flex items-center gap-1.5 text-sm font-medium text-slate-500 hover:text-sb-900">
          <IconoVolver className="h-4 w-4" />
          Volver a la consulta
        </Link>
        <p className="max-w-3xl text-sm text-slate-500">
          Complete la información. La solicitud se crea en estado <strong className="text-sb-800">Registrada</strong> y
          recibe un código consecutivo automáticamente.
        </p>
      </div>

      <div className="grid gap-6 xl:grid-cols-[1fr_360px]">
        <Tarjeta titulo="Datos de la solicitud" descripcion="Los campos marcados con * son obligatorios.">
          <form className="space-y-5" onSubmit={enviar} noValidate>
            {errorGeneral && <Alerta tono="error">{errorGeneral}</Alerta>}

            <Campo etiqueta="Título" para="titulo" requerido error={errores.titulo}>
              <Entrada
                id="titulo"
                value={formulario.titulo}
                onChange={(evento) => actualizar('titulo', evento.target.value)}
                placeholder="Resuma el requerimiento en una línea"
                maxLength={200}
                required
              />
            </Campo>

            <Campo
              etiqueta="Descripción"
              para="descripcion"
              requerido
              error={errores.descripcion}
              ayuda="Incluya el contexto, el impacto y los pasos para reproducirlo cuando aplique."
            >
              <AreaTexto
                id="descripcion"
                value={formulario.descripcion}
                onChange={(evento) => actualizar('descripcion', evento.target.value)}
                placeholder="Describa la situación con el mayor detalle posible…"
                maxLength={4000}
                required
              />
            </Campo>

            <div className="grid gap-5 sm:grid-cols-2">
              <Campo etiqueta="Área responsable" para="area" requerido error={errores.areaId}>
                <Seleccion
                  id="area"
                  value={formulario.areaId}
                  onChange={(evento) => actualizar('areaId', evento.target.value)}
                  required
                >
                  <option value="">Seleccione un área</option>
                  {areas.map((area) => (
                    <option key={area.id} value={area.id}>
                      {area.nombre}
                    </option>
                  ))}
                </Seleccion>
              </Campo>

              <Campo etiqueta="Tipo de solicitud" para="tipo" requerido error={errores.tipoSolicitudId}>
                <Seleccion
                  id="tipo"
                  value={formulario.tipoSolicitudId}
                  onChange={(evento) => actualizar('tipoSolicitudId', evento.target.value)}
                  required
                >
                  <option value="">Seleccione un tipo</option>
                  {tipos.map((tipo) => (
                    <option key={tipo.id} value={tipo.id}>
                      {tipo.nombre}
                    </option>
                  ))}
                </Seleccion>
              </Campo>

              <Campo etiqueta="Prioridad" para="prioridad" requerido error={errores.prioridad}>
                <Seleccion
                  id="prioridad"
                  value={formulario.prioridad}
                  onChange={(evento) => actualizar('prioridad', evento.target.value as PrioridadSolicitud)}
                >
                  {PRIORIDADES.map((opcion) => (
                    <option key={opcion.valor} value={opcion.valor}>
                      {opcion.etiqueta}
                    </option>
                  ))}
                </Seleccion>
              </Campo>

              <Campo
                etiqueta="Fecha de compromiso"
                para="fechaCompromiso"
                requerido
                error={errores.fechaCompromiso}
                ayuda="Fecha límite acordada para atender la solicitud."
              >
                <Entrada
                  id="fechaCompromiso"
                  type="date"
                  value={formulario.fechaCompromiso}
                  onChange={(evento) => actualizar('fechaCompromiso', evento.target.value)}
                  required
                />
              </Campo>
            </div>

            <Campo
              etiqueta="Referencia de evidencia"
              para="urlEvidencia"
              error={errores.urlEvidencia}
              ayuda="Opcional. Enlace o ruta del documento de respaldo."
            >
              <Entrada
                id="urlEvidencia"
                value={formulario.urlEvidencia}
                onChange={(evento) => actualizar('urlEvidencia', evento.target.value)}
                placeholder="https://…"
                maxLength={500}
              />
            </Campo>

            <div className="flex flex-col-reverse gap-3 border-t border-slate-100 pt-5 sm:flex-row sm:justify-end">
              <Link to="/consulta">
                <Boton variante="secundario" type="button" className="w-full sm:w-auto">
                  Cancelar
                </Boton>
              </Link>
              <Boton type="submit" cargando={enviando}>
                Registrar solicitud
              </Boton>
            </div>
          </form>
        </Tarjeta>

        <div className="space-y-6">
          <Tarjeta titulo="Cómo funciona el flujo">
            <ol className="space-y-4">
              {ETAPAS.map((etapa, indice) => (
                <li key={etapa} className="flex gap-3">
                  <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-acento-suave text-xs font-bold text-acento">
                    {indice + 1}
                  </span>
                  <p className="text-sm leading-relaxed text-slate-600">{etapa}</p>
                </li>
              ))}
            </ol>
          </Tarjeta>

          <Tarjeta titulo="Se registrará a nombre de">
            <p className="text-sm font-semibold text-sb-900">{usuario?.nombre}</p>
            <p className="mt-0.5 text-sm text-slate-500">{usuario?.correo}</p>
            <p className="mt-3 rounded-lg bg-slate-50 px-3 py-2 text-xs text-slate-500">
              El solicitante solo podrá consultar sus propias solicitudes. El equipo técnico asignado
              gestiona el estado y los comentarios internos.
            </p>
          </Tarjeta>
        </div>
      </div>
    </div>
  );
}
