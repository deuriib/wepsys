import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Alerta, AreaTexto, Boton, Campo, Entrada, Seleccion, Tarjeta } from '../componentes/Diseno';
import { IconoVolver } from '../componentes/Iconos';
import { ErrorApi } from '../lib/api';
import { useAreas, useCrearSolicitudMutation, useTiposSolicitud } from '../lib/consultas';
import { desdeEntradaFecha } from '../lib/formato';
import type { PrioridadSolicitud } from '../lib/tipos';
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
  const { usuario } = useSesion();
  const navegar = useNavigate();

  const { data: areas = [], isLoading: cargandoAreas } = useAreas();
  const { data: tipos = [], isLoading: cargandoTipos } = useTiposSolicitud();
  const mutacionCrear = useCrearSolicitudMutation();

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

  useEffect(() => {
    if (areas.length > 0 && !formulario.areaId) {
      setFormulario((actual) => ({ ...actual, areaId: String(areas[0].id) }));
    }
  }, [areas, formulario.areaId]);

  useEffect(() => {
    if (tipos.length > 0 && !formulario.tipoSolicitudId) {
      setFormulario((actual) => ({ ...actual, tipoSolicitudId: String(tipos[0].id) }));
    }
  }, [tipos, formulario.tipoSolicitudId]);

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

    if (!formulario.titulo.trim()) {
      nuevos.titulo = 'El título es obligatorio.';
    } else if (formulario.titulo.trim().length < 5) {
      nuevos.titulo = 'El título debe tener al menos 5 caracteres.';
    } else if (formulario.titulo.trim().length > 200) {
      nuevos.titulo = 'El título no puede superar 200 caracteres.';
    }

    if (!formulario.descripcion.trim()) {
      nuevos.descripcion = 'La descripción es obligatoria.';
    } else if (formulario.descripcion.trim().length < 10) {
      nuevos.descripcion = 'La descripción debe tener al menos 10 caracteres.';
    } else if (formulario.descripcion.trim().length > 4000) {
      nuevos.descripcion = 'La descripción no puede superar 4000 caracteres.';
    }

    if (!formulario.areaId) {
      nuevos.areaId = 'Debe seleccionar un área.';
    }

    if (!formulario.tipoSolicitudId) {
      nuevos.tipoSolicitudId = 'Debe seleccionar un tipo de solicitud.';
    }

    if (!formulario.fechaCompromiso) {
      nuevos.fechaCompromiso = 'Debe indicar la fecha de compromiso esperada.';
    }

    if (formulario.urlEvidencia.trim()) {
      try {
        new URL(formulario.urlEvidencia.trim());
      } catch {
        nuevos.urlEvidencia = 'Ingrese una URL válida (ej. https://ejemplo.com/archivo.png).';
      }
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

    try {
      const creada = await mutacionCrear.mutateAsync({
        titulo: formulario.titulo.trim(),
        descripcion: formulario.descripcion.trim(),
        prioridad: formulario.prioridad,
        areaId: Number(formulario.areaId),
        tipoSolicitudId: Number(formulario.tipoSolicitudId),
        fechaCompromiso: desdeEntradaFecha(formulario.fechaCompromiso)!,
        urlEvidencia: formulario.urlEvidencia.trim() || null
      });

      navegar(`/solicitudes/${creada.id}`, { state: { creada: true } });
    } catch (excepcion: unknown) {
      if (excepcion instanceof ErrorApi) {
        if (excepcion.errores && Object.keys(excepcion.errores).length > 0) {
          const desdeBackend: Record<string, string> = {};
          ['titulo', 'descripcion', 'prioridad', 'areaId', 'tipoSolicitudId', 'fechaCompromiso', 'urlEvidencia'].forEach(
            (campo) => {
              const mensaje = mensajeDeCampo(excepcion.errores!, campo);
              if (mensaje) {
                desdeBackend[campo] = mensaje;
              }
            }
          );
          setErrores(desdeBackend);
        }
        setErrorGeneral(excepcion.message);
      } else {
        setErrorGeneral('Ocurrió un error inesperado al registrar la solicitud.');
      }
    }
  };

  const cargando = cargandoAreas || cargandoTipos;
  const enviando = mutacionCrear.isPending;

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <div className="flex items-center justify-between gap-4">
        <div>
          <Link
            to="/consulta"
            className="inline-flex items-center gap-1.5 text-xs font-semibold text-slate-500 hover:text-sb-800"
          >
            <IconoVolver className="h-4 w-4" />
            Volver a la consulta
          </Link>
          <h2 className="mt-1 text-2xl font-bold tracking-tight text-sb-900">
            Registrar nueva solicitud
          </h2>
          <p className="mt-1 text-sm text-slate-500">
            Complete los campos obligatorios para dar inicio al flujo de atención de la solicitud.
          </p>
        </div>
      </div>

      {errorGeneral && <Alerta tono="error">{errorGeneral}</Alerta>}

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <Tarjeta>
            <form onSubmit={enviar} className="space-y-5" noValidate>
              <Campo
                para="crear-titulo"
                etiqueta="Título de la solicitud"
                requerido
                error={errores.titulo}
                ayuda="Resuma la necesidad en una frase clara (entre 5 y 200 caracteres)."
              >
                <Entrada
                  id="crear-titulo"
                  value={formulario.titulo}
                  onChange={(evento) => actualizar('titulo', evento.target.value)}
                  placeholder="Ej. Acceso de solo lectura a la base de riesgos…"
                  maxLength={200}
                />
              </Campo>

              <Campo
                para="crear-descripcion"
                etiqueta="Descripción detallada"
                requerido
                error={errores.descripcion}
                ayuda="Explique el contexto, motivo y alcance de la solicitud (entre 10 y 4000 caracteres)."
              >
                <AreaTexto
                  id="crear-descripcion"
                  rows={5}
                  value={formulario.descripcion}
                  onChange={(evento) => actualizar('descripcion', evento.target.value)}
                  placeholder="Detalle el requerimiento con todos los datos necesarios para su resolución…"
                  maxLength={4000}
                />
              </Campo>

              <div className="grid gap-4 sm:grid-cols-2">
                <Campo para="crear-prioridad" etiqueta="Prioridad" requerido error={errores.prioridad}>
                  <Seleccion
                    id="crear-prioridad"
                    value={formulario.prioridad}
                    onChange={(evento) =>
                      actualizar('prioridad', evento.target.value as PrioridadSolicitud)
                    }
                  >
                    {PRIORIDADES.map((item) => (
                      <option key={item.valor} value={item.valor}>
                        {item.etiqueta}
                      </option>
                    ))}
                  </Seleccion>
                </Campo>

                <Campo para="crear-area" etiqueta="Área solicitante" requerido error={errores.areaId}>
                  <Seleccion
                    id="crear-area"
                    value={formulario.areaId}
                    onChange={(evento) => actualizar('areaId', evento.target.value)}
                    disabled={cargando}
                  >
                    {areas.map((area) => (
                      <option key={area.id} value={area.id}>
                        {area.nombre}
                      </option>
                    ))}
                  </Seleccion>
                </Campo>
              </div>

              <div className="grid gap-4 sm:grid-cols-2">
                <Campo para="crear-tipo" etiqueta="Tipo de solicitud" requerido error={errores.tipoSolicitudId}>
                  <Seleccion
                    id="crear-tipo"
                    value={formulario.tipoSolicitudId}
                    onChange={(evento) => actualizar('tipoSolicitudId', evento.target.value)}
                    disabled={cargando}
                  >
                    {tipos.map((tipo) => (
                      <option key={tipo.id} value={tipo.id}>
                        {tipo.nombre}
                      </option>
                    ))}
                  </Seleccion>
                </Campo>

                <Campo
                  para="crear-fecha"
                  etiqueta="Fecha compromiso esperada"
                  requerido
                  error={errores.fechaCompromiso}
                  ayuda="Fecha límite sugerida para la atención."
                >
                  <Entrada
                    id="crear-fecha"
                    type="date"
                    value={formulario.fechaCompromiso}
                    onChange={(evento) => actualizar('fechaCompromiso', evento.target.value)}
                  />
                </Campo>
              </div>

              <Campo
                para="crear-evidencia"
                etiqueta="Referencia textual o URL de evidencia"
                error={errores.urlEvidencia}
                ayuda="Enlace a repositorio de archivos o captura de soporte (opcional, máx. 500 caracteres)."
              >
                <Entrada
                  id="crear-evidencia"
                  type="url"
                  value={formulario.urlEvidencia}
                  onChange={(evento) => actualizar('urlEvidencia', evento.target.value)}
                  placeholder="https://ejemplo.sb.gob.do/evidencia/ticket-123.png"
                  maxLength={500}
                />
              </Campo>

              <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
                <Link to="/consulta">
                  <Boton type="button" variante="secundario">
                    Cancelar
                  </Boton>
                </Link>
                <Boton type="submit" cargando={enviando}>
                  Crear solicitud
                </Boton>
              </div>
            </form>
          </Tarjeta>
        </div>

        <div className="space-y-4">
          <Tarjeta titulo="Información del solicitante" descripcion="Datos asignados automáticamente">
            <dl className="space-y-3 text-xs">
              <div>
                <dt className="text-slate-400">Solicitante</dt>
                <dd className="font-semibold text-sb-900">{usuario?.nombre}</dd>
              </div>
              <div>
                <dt className="text-slate-400">Correo institucional</dt>
                <dd className="font-semibold text-sb-900">{usuario?.correo}</dd>
              </div>
              <div>
                <dt className="text-slate-400">Rol</dt>
                <dd className="font-semibold text-sb-900">{usuario?.rol}</dd>
              </div>
            </dl>
          </Tarjeta>

          <Tarjeta titulo="Ciclo de vida" descripcion="Flujo de atención técnica">
            <ol className="space-y-3 text-xs text-slate-600">
              {ETAPAS.map((etapa, indice) => (
                <li key={indice} className="flex gap-2">
                  <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-slate-100 font-semibold text-sb-800">
                    {indice + 1}
                  </span>
                  <span>{etapa}</span>
                </li>
              ))}
            </ol>
          </Tarjeta>
        </div>
      </div>
    </div>
  );
}
