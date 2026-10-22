import { useEffect, useState } from 'react';
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
import { InsigniaRol } from '../componentes/Insignias';
import { IconoBuscar, IconoEditar } from '../componentes/Iconos';
import { ErrorApi } from '../lib/api';
import type { ClienteApi } from '../lib/api';
import { formatearFecha, tituloRol } from '../lib/formato';
import type { RolUsuario, Usuario } from '../lib/tipos';
import { useSesion } from '../sesion/ContextoSesion';

const ROLES: RolUsuario[] = ['Administrador', 'Analista', 'Solicitante'];

interface ValoresFormulario {
  nombre: string;
  correo: string;
  contrasena: string;
  rol: RolUsuario;
  activo: boolean;
}

const FORMULARIO_VACIO: ValoresFormulario = {
  nombre: '',
  correo: '',
  contrasena: '',
  rol: 'Solicitante',
  activo: true
};

interface PropiedadesModalUsuario {
  abierta: boolean;
  usuario: Usuario | null;
  cliente: ClienteApi;
  usuarioActualId: number;
  onCerrar: () => void;
  onGuardado: (mensaje: string) => void;
  onError: (mensaje: string) => void;
}

function ModalUsuario({
  abierta,
  usuario,
  cliente,
  usuarioActualId,
  onCerrar,
  onGuardado,
  onError
}: PropiedadesModalUsuario) {
  const [formulario, setFormulario] = useState<ValoresFormulario>(FORMULARIO_VACIO);
  const [errores, setErrores] = useState<Record<string, string>>({});
  const [guardando, setGuardando] = useState(false);

  useEffect(() => {
    if (!abierta) {
      return;
    }

    setFormulario(
      usuario
        ? {
            nombre: usuario.nombre,
            correo: usuario.correo,
            contrasena: '',
            rol: usuario.rol,
            activo: usuario.activo
          }
        : FORMULARIO_VACIO
    );
    setErrores({});
  }, [abierta, usuario]);

  const actualizar = <C extends keyof ValoresFormulario>(campo: C, valor: ValoresFormulario[C]) => {
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

    if (!usuario && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formulario.correo.trim())) {
      nuevos.correo = 'Indique un correo válido.';
    }

    if (formulario.contrasena.length > 0 && formulario.contrasena.length < 8) {
      nuevos.contrasena = 'La contraseña debe tener al menos 8 caracteres.';
    }

    if (!usuario && formulario.contrasena.length < 8) {
      nuevos.contrasena = 'La contraseña es obligatoria (mínimo 8 caracteres).';
    }

    setErrores(nuevos);

    if (Object.keys(nuevos).length > 0) {
      return;
    }

    setGuardando(true);

    try {
      if (usuario) {
        await cliente.actualizarUsuario(usuario.id, {
          nombre: formulario.nombre.trim(),
          rol: formulario.rol,
          activo: formulario.activo,
          contrasena: formulario.contrasena || undefined
        });
        onGuardado('Usuario actualizado correctamente.');
      } else {
        await cliente.crearUsuario({
          nombre: formulario.nombre.trim(),
          correo: formulario.correo.trim(),
          contrasena: formulario.contrasena,
          rol: formulario.rol
        });
        onGuardado('Usuario creado correctamente.');
      }
    } catch (excepcion) {
      onError(excepcion instanceof ErrorApi ? excepcion.message : 'No fue posible guardar el usuario.');
    } finally {
      setGuardando(false);
    }
  };

  const esUsuarioActual = usuario !== null && usuario.id === usuarioActualId;

  return (
    <Modal
      abierto={abierta}
      titulo={usuario ? 'Editar usuario' : 'Nuevo usuario'}
      descripcion={
        usuario
          ? 'Los cambios de rol se aplican la próxima vez que el usuario inicie sesión.'
          : 'La cuenta se crea activa y puede iniciar sesión de inmediato.'
      }
      onCerrar={onCerrar}
      ancho="amplio"
      pie={
        <>
          <Boton variante="secundario" type="button" onClick={onCerrar}>
            Cancelar
          </Boton>
          <Boton type="submit" form="formulario-usuario" cargando={guardando}>
            {usuario ? 'Guardar cambios' : 'Crear usuario'}
          </Boton>
        </>
      }
    >
      <form id="formulario-usuario" className="space-y-5" onSubmit={enviar} noValidate>
        <div className="grid gap-5 sm:grid-cols-2">
          <Campo etiqueta="Nombre completo" para="usuario-nombre" requerido error={errores.nombre}>
            <Entrada
              id="usuario-nombre"
              value={formulario.nombre}
              onChange={(evento) => actualizar('nombre', evento.target.value)}
              maxLength={150}
              placeholder="Nombre y apellidos"
            />
          </Campo>

          <Campo
            etiqueta="Correo institucional"
            para="usuario-correo"
            requerido={!usuario}
            error={errores.correo}
            ayuda={usuario ? 'El correo no se puede modificar.' : undefined}
          >
            <Entrada
              id="usuario-correo"
              type="email"
              value={formulario.correo}
              onChange={(evento) => actualizar('correo', evento.target.value)}
              maxLength={200}
              placeholder="usuario@sb.gob.do"
              disabled={Boolean(usuario)}
            />
          </Campo>
        </div>

        <div className="grid gap-5 sm:grid-cols-2">
          <Campo etiqueta="Rol" para="usuario-rol" requerido>
            <Seleccion
              id="usuario-rol"
              value={formulario.rol}
              onChange={(evento) => actualizar('rol', evento.target.value as RolUsuario)}
            >
              {ROLES.map((rol) => (
                <option key={rol} value={rol}>
                  {tituloRol(rol)}
                </option>
              ))}
            </Seleccion>
          </Campo>

          <Campo
            etiqueta={usuario ? 'Nueva contraseña' : 'Contraseña'}
            para="usuario-contrasena"
            requerido={!usuario}
            error={errores.contrasena}
            ayuda={usuario ? 'Déjela vacía para conservar la actual.' : 'Mínimo 8 caracteres.'}
          >
            <Entrada
              id="usuario-contrasena"
              type="password"
              autoComplete="new-password"
              value={formulario.contrasena}
              onChange={(evento) => actualizar('contrasena', evento.target.value)}
              maxLength={100}
              placeholder={usuario ? 'Sin cambios' : '••••••••'}
            />
          </Campo>
        </div>

        {usuario && (
          <label className="flex items-center gap-2 text-sm text-slate-700">
            <input
              type="checkbox"
              className="h-4 w-4 rounded border-slate-300 text-acento focus:ring-acento/40"
              checked={formulario.activo}
              onChange={(evento) => actualizar('activo', evento.target.checked)}
              disabled={esUsuarioActual}
            />
            Usuario activo
            {esUsuarioActual && (
              <span className="text-xs text-slate-400">No puede desactivar su propia cuenta.</span>
            )}
          </label>
        )}
      </form>
    </Modal>
  );
}

export function Usuarios() {
  const { cliente, usuario: usuarioActual } = useSesion();
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);
  const [texto, setTexto] = useState('');
  const [modal, setModal] = useState<{ usuario: Usuario | null } | null>(null);
  const [recarga, setRecarga] = useState(0);

  useEffect(() => {
    let vigente = true;
    setCargando(true);

    cliente
      .obtenerUsuarios()
      .then((datos) => {
        if (vigente) {
          setUsuarios(datos);
          setError(null);
        }
      })
      .catch((excepcion: unknown) => {
        if (vigente) {
          setError(
            excepcion instanceof ErrorApi ? excepcion.message : 'No fue posible consultar los usuarios.'
          );
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
  }, [cliente, recarga]);

  const filtrados = usuarios.filter((usuario) => {
    const termino = texto.trim().toLowerCase();

    if (!termino) {
      return true;
    }

    return (
      usuario.nombre.toLowerCase().includes(termino) ||
      usuario.correo.toLowerCase().includes(termino) ||
      usuario.rolDescripcion.toLowerCase().includes(termino)
    );
  });

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <p className="max-w-2xl text-sm text-slate-500">
          Administre las cuentas institucionales y el alcance de cada rol.
        </p>
        <Boton className="shrink-0" onClick={() => setModal({ usuario: null })}>
          Nuevo usuario
        </Boton>
      </div>

      {aviso && <Alerta tono="exito">{aviso}</Alerta>}
      {error && <Alerta tono="error">{error}</Alerta>}

      <Tarjeta
        titulo={`${filtrados.length} usuario${filtrados.length === 1 ? '' : 's'}`}
        descripcion="Administrador: acceso total. Analista: solicitudes asignadas o disponibles. Solicitante: solo las propias."
        className="overflow-hidden"
        acciones={
          <div className="relative">
            <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400">
              <IconoBuscar className="h-4 w-4" />
            </span>
            <Entrada
              className="w-56 pl-9 py-2"
              value={texto}
              onChange={(evento) => setTexto(evento.target.value)}
              placeholder="Buscar usuario…"
              aria-label="Buscar usuario"
            />
          </div>
        }
      >
        {cargando && usuarios.length === 0 ? (
          <Cargando mensaje="Consultando usuarios…" />
        ) : filtrados.length === 0 ? (
          <Vacio
            titulo="Sin usuarios"
            descripcion="Ningún usuario coincide con la búsqueda."
            accion={
              texto ? (
                <Boton variante="secundario" onClick={() => setTexto('')}>
                  Limpiar búsqueda
                </Boton>
              ) : undefined
            }
          />
        ) : (
          <div className="-mx-6 -my-5 overflow-x-auto">
            <table className="w-full min-w-[760px] text-left text-sm">
              <thead>
                <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
                  <th className="px-6 py-3 font-semibold">Nombre</th>
                  <th className="px-6 py-3 font-semibold">Correo</th>
                  <th className="px-6 py-3 font-semibold">Rol</th>
                  <th className="px-6 py-3 font-semibold">Estado</th>
                  <th className="px-6 py-3 font-semibold">Creado</th>
                  <th className="px-6 py-3 text-right font-semibold">Acciones</th>
                </tr>
              </thead>
              <tbody className={`divide-y divide-slate-100 ${cargando ? 'opacity-60' : ''}`}>
                {filtrados.map((usuario) => (
                  <tr key={usuario.id} className="transition hover:bg-slate-50/80">
                    <td className="px-6 py-3.5">
                      <span className="block font-medium text-sb-900">{usuario.nombre}</span>
                      {usuario.id === usuarioActual?.id && (
                        <span className="text-xs text-acento">Su cuenta</span>
                      )}
                    </td>
                    <td className="px-6 py-3.5 text-slate-600">{usuario.correo}</td>
                    <td className="px-6 py-3.5">
                      <InsigniaRol rol={usuario.rol} />
                    </td>
                    <td className="px-6 py-3.5">
                      <span
                        className={`inline-flex items-center gap-1.5 text-xs font-semibold ${
                          usuario.activo ? 'text-emerald-700' : 'text-slate-400'
                        }`}
                      >
                        <span
                          className={`h-1.5 w-1.5 rounded-full ${
                            usuario.activo ? 'bg-emerald-500' : 'bg-slate-300'
                          }`}
                        />
                        {usuario.activo ? 'Activo' : 'Inactivo'}
                      </span>
                    </td>
                    <td className="whitespace-nowrap px-6 py-3.5 text-slate-500">
                      {formatearFecha(usuario.fechaCreacion)}
                    </td>
                    <td className="whitespace-nowrap px-6 py-3.5 text-right">
                      <button
                        type="button"
                        onClick={() => setModal({ usuario })}
                        className="inline-flex items-center gap-1.5 rounded-lg px-2.5 py-1.5 text-sm font-semibold text-sb-700 transition hover:bg-sb-900/5 hover:text-sb-900"
                        aria-label={`Editar ${usuario.nombre}`}
                      >
                        <IconoEditar className="h-4 w-4" />
                        Editar
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Tarjeta>

      <ModalUsuario
        abierta={modal !== null}
        usuario={modal?.usuario ?? null}
        cliente={cliente}
        usuarioActualId={usuarioActual?.id ?? 0}
        onCerrar={() => setModal(null)}
        onGuardado={(mensaje) => {
          setModal(null);
          setAviso(mensaje);
          setRecarga((valor) => valor + 1);
        }}
        onError={(mensaje) => setError(mensaje)}
      />
    </div>
  );
}
