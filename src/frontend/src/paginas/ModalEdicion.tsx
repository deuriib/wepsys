import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { AreaTexto, Boton, Campo, Entrada, Modal, Seleccion } from '../componentes/Diseno';
import { ErrorApi } from '../lib/api';
import type { ClienteApi } from '../lib/api';
import { desdeEntradaFecha, paraEntradaFecha } from '../lib/formato';
import type { PrioridadSolicitud, SolicitudDetalle } from '../lib/tipos';

interface PropiedadesModalEdicion {
  abierta: boolean;
  solicitud: SolicitudDetalle;
  cliente: ClienteApi;
  onCerrar: () => void;
  onGuardada: (solicitud: SolicitudDetalle) => void;
  onError: (mensaje: string) => void;
}

interface FormularioEdicion {
  titulo: string;
  descripcion: string;
  prioridad: PrioridadSolicitud;
  fechaCompromiso: string;
  urlEvidencia: string;
}

const PRIORIDADES: Array<{ valor: PrioridadSolicitud; etiqueta: string }> = [
  { valor: 'Baja', etiqueta: 'Baja' },
  { valor: 'Media', etiqueta: 'Media' },
  { valor: 'Alta', etiqueta: 'Alta' },
  { valor: 'Critica', etiqueta: 'Crítica' }
];

export function ModalEdicion({
  abierta,
  solicitud,
  cliente,
  onCerrar,
  onGuardada,
  onError
}: PropiedadesModalEdicion) {
  const [formulario, setFormulario] = useState<FormularioEdicion>({
    titulo: solicitud.titulo,
    descripcion: solicitud.descripcion,
    prioridad: solicitud.prioridad,
    fechaCompromiso: paraEntradaFecha(solicitud.fechaCompromiso),
    urlEvidencia: solicitud.urlEvidencia ?? ''
  });
  const [errores, setErrores] = useState<Record<string, string>>({});
  const [guardando, setGuardando] = useState(false);

  useEffect(() => {
    if (!abierta) {
      return;
    }

    setFormulario({
      titulo: solicitud.titulo,
      descripcion: solicitud.descripcion,
      prioridad: solicitud.prioridad,
      fechaCompromiso: paraEntradaFecha(solicitud.fechaCompromiso),
      urlEvidencia: solicitud.urlEvidencia ?? ''
    });
    setErrores({});
  }, [abierta, solicitud]);

  const actualizar = <C extends keyof FormularioEdicion>(campo: C, valor: FormularioEdicion[C]) => {
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

    if (formulario.titulo.trim().length < 5) {
      nuevos.titulo = 'El título debe tener al menos 5 caracteres.';
    }

    if (formulario.descripcion.trim().length < 10) {
      nuevos.descripcion = 'La descripción debe tener al menos 10 caracteres.';
    }

    if (!formulario.fechaCompromiso) {
      nuevos.fechaCompromiso = 'Indique una fecha de compromiso.';
    }

    setErrores(nuevos);

    if (Object.keys(nuevos).length > 0) {
      return;
    }

    setGuardando(true);

    try {
      const fechaCompromiso = desdeEntradaFecha(formulario.fechaCompromiso);
      const actualizada = await cliente.actualizarSolicitud(solicitud.id, {
        titulo: formulario.titulo.trim(),
        descripcion: formulario.descripcion.trim(),
        prioridad: formulario.prioridad,
        fechaCompromiso: fechaCompromiso ?? undefined,
        urlEvidencia: formulario.urlEvidencia.trim() || null
      });

      onGuardada(actualizada);
    } catch (excepcion) {
      onError(excepcion instanceof ErrorApi ? excepcion.message : 'No fue posible actualizar la solicitud.');
    } finally {
      setGuardando(false);
    }
  };

  return (
    <Modal
      abierto={abierta}
      titulo="Editar solicitud"
      descripcion={`${solicitud.codigo} · los cambios quedan en el historial.`}
      onCerrar={onCerrar}
      ancho="amplio"
      pie={
        <>
          <Boton variante="secundario" type="button" onClick={onCerrar}>
            Cancelar
          </Boton>
          <Boton type="submit" form="formulario-edicion" cargando={guardando}>
            Guardar cambios
          </Boton>
        </>
      }
    >
      <form id="formulario-edicion" className="space-y-5" onSubmit={enviar} noValidate>
        <Campo etiqueta="Título" para="edicion-titulo" requerido error={errores.titulo}>
          <Entrada
            id="edicion-titulo"
            value={formulario.titulo}
            onChange={(evento) => actualizar('titulo', evento.target.value)}
            maxLength={200}
          />
        </Campo>

        <Campo etiqueta="Descripción" para="edicion-descripcion" requerido error={errores.descripcion}>
          <AreaTexto
            id="edicion-descripcion"
            value={formulario.descripcion}
            onChange={(evento) => actualizar('descripcion', evento.target.value)}
            maxLength={4000}
          />
        </Campo>

        <div className="grid gap-5 sm:grid-cols-2">
          <Campo etiqueta="Prioridad" para="edicion-prioridad" requerido>
            <Seleccion
              id="edicion-prioridad"
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

          <Campo etiqueta="Fecha de compromiso" para="edicion-fecha" requerido error={errores.fechaCompromiso}>
            <Entrada
              id="edicion-fecha"
              type="date"
              value={formulario.fechaCompromiso}
              onChange={(evento) => actualizar('fechaCompromiso', evento.target.value)}
            />
          </Campo>
        </div>

        <Campo
          etiqueta="Referencia de evidencia"
          para="edicion-evidencia"
          ayuda="Enlace o ruta del documento de respaldo."
        >
          <Entrada
            id="edicion-evidencia"
            value={formulario.urlEvidencia}
            onChange={(evento) => actualizar('urlEvidencia', evento.target.value)}
            maxLength={500}
            placeholder="https://…"
          />
        </Campo>
      </form>
    </Modal>
  );
}
