import { useState } from 'react';
import type { FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import logo from '../assets/logo-sb.png';
import { Alerta, Boton, Campo, Entrada } from '../componentes/Diseno';
import { ErrorApi } from '../lib/api';
import { useSesion } from '../sesion/ContextoSesion';

interface CredencialDemo {
  etiqueta: string;
  correo: string;
  contrasena: string;
  detalle: string;
}

const CREDENCIALES_DEMO: CredencialDemo[] = [
  {
    etiqueta: 'Administrador',
    correo: 'admin@sb.gob.do',
    contrasena: 'Admin123!',
    detalle: 'Acceso total y mantenimiento'
  },
  {
    etiqueta: 'Analista',
    correo: 'analista@sb.gob.do',
    contrasena: 'Analista123!',
    detalle: 'Bandeja técnica asignada'
  },
  {
    etiqueta: 'Solicitante',
    correo: 'solicitante@sb.gob.do',
    contrasena: 'Solicitud123!',
    detalle: 'Solo sus propias solicitudes'
  }
];

export function IniciarSesion() {
  const { sesion, iniciarSesion } = useSesion();
  const navegar = useNavigate();
  const ubicacion = useLocation();
  const [correo, setCorreo] = useState('admin@sb.gob.do');
  const [contrasena, setContrasena] = useState('Admin123!');
  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (sesion) {
    return <Navigate to="/" replace />;
  }

  const destino = (ubicacion.state as { desde?: string } | null)?.desde ?? '/';

  const enviar = async (evento: FormEvent<HTMLFormElement>) => {
    evento.preventDefault();
    setError(null);
    setCargando(true);

    try {
      await iniciarSesion(correo.trim(), contrasena);
      navegar(destino, { replace: true });
    } catch (excepcion) {
      if (excepcion instanceof ErrorApi) {
        setError(
          excepcion.estado === 401
            ? 'Las credenciales no son válidas. Verifique el correo y la contraseña.'
            : excepcion.message
        );
      } else {
        setError('Ocurrió un error inesperado al iniciar sesión.');
      }
    } finally {
      setCargando(false);
    }
  };

  const usarCredencial = (credencial: CredencialDemo) => {
    setCorreo(credencial.correo);
    setContrasena(credencial.contrasena);
    setError(null);
  };

  return (
    <div className="grid min-h-screen lg:grid-cols-[1.05fr_1fr]">
      <section className="relative hidden overflow-hidden bg-gradient-to-br from-sb-700 via-sb-800 to-sb-950 px-12 py-10 lg:flex lg:flex-col lg:justify-between">
        <div
          aria-hidden="true"
          className="pointer-events-none absolute -right-24 top-24 h-80 w-80 rounded-full border border-white/10"
        />
        <div
          aria-hidden="true"
          className="pointer-events-none absolute -bottom-28 -left-20 h-96 w-96 rounded-full bg-acento/10 blur-3xl"
        />

        <img
          src={logo}
          alt="Superintendencia de Bancos de la República Dominicana"
          className="h-60 w-auto brightness-0 invert"
        />

        <div className="relative max-w-lg">
          <p className="mb-4 inline-flex items-center gap-2 rounded-full border border-white/15 bg-white/5 px-3 py-1 text-xs font-medium text-slate-200">
            <span className="h-1.5 w-1.5 rounded-full bg-acento" />
            Plataforma interna de gestión
          </p>
          <h1 className="text-4xl font-bold leading-tight text-white">
            Solicitudes internas, con trazabilidad de principio a fin.
          </h1>
          <p className="mt-5 text-base leading-relaxed text-slate-300">
            Registre requerimientos, asigne responsables y siga cada cambio de estado en un solo
            lugar. El historial completo queda disponible para auditoría.
          </p>

          <dl className="mt-10 grid grid-cols-3 gap-4 border-t border-white/10 pt-6">
            <div>
              <dt className="text-xs uppercase tracking-wide text-slate-400">Flujo</dt>
              <dd className="mt-1 text-sm font-medium text-white">6 estados</dd>
            </div>
            <div>
              <dt className="text-xs uppercase tracking-wide text-slate-400">Perfiles</dt>
              <dd className="mt-1 text-sm font-medium text-white">3 roles</dd>
            </div>
            <div>
              <dt className="text-xs uppercase tracking-wide text-slate-400">Seguridad</dt>
              <dd className="mt-1 text-sm font-medium text-white">JWT + roles</dd>
            </div>
          </dl>
        </div>

        <p className="text-xs text-slate-400">
          Superintendencia de Bancos de la República Dominicana · Uso interno
        </p>
      </section>

      <section className="flex items-center justify-center bg-lienzo px-5 py-10 sm:px-8">
        <div className="w-full max-w-md">
          <div className="mb-6 flex justify-center lg:hidden">
            <img
              src={logo}
              alt="Superintendencia de Bancos"
              className="h-24 w-auto"
            />
          </div>

          <div className="rounded-panel border border-white bg-white p-7 shadow-flotante sm:p-9">
            <h2 className="text-2xl font-bold text-sb-900">Iniciar sesión</h2>
            <p className="mt-2 text-sm text-slate-500">
              Ingrese con su cuenta institucional para acceder al sistema.
            </p>

            <form className="mt-7 space-y-5" onSubmit={enviar}>
              {error && <Alerta tono="error">{error}</Alerta>}

              <Campo etiqueta="Correo institucional" para="correo" requerido>
                <Entrada
                  id="correo"
                  name="correo"
                  type="email"
                  autoComplete="username"
                  placeholder="usuario@sb.gob.do"
                  value={correo}
                  onChange={(evento) => setCorreo(evento.target.value)}
                  required
                />
              </Campo>

              <Campo etiqueta="Contraseña" para="contrasena" requerido>
                <Entrada
                  id="contrasena"
                  name="contrasena"
                  type="password"
                  autoComplete="current-password"
                  placeholder="••••••••"
                  value={contrasena}
                  onChange={(evento) => setContrasena(evento.target.value)}
                  required
                />
              </Campo>

              <Boton type="submit" cargando={cargando} className="w-full">
                Entrar al sistema
              </Boton>
            </form>

            <div className="mt-8 border-t border-slate-100 pt-6">
              <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">
                Credenciales de prueba
              </p>
              <div className="mt-3 space-y-2">
                {CREDENCIALES_DEMO.map((credencial) => (
                  <button
                    key={credencial.correo}
                    type="button"
                    onClick={() => usarCredencial(credencial)}
                    className="flex w-full items-center justify-between gap-3 rounded-lg border border-slate-200 px-3 py-2 text-left transition hover:border-acento/40 hover:bg-acento-suave"
                  >
                    <span>
                      <span className="block text-sm font-semibold text-sb-900">{credencial.etiqueta}</span>
                      <span className="block text-xs text-slate-500">{credencial.detalle}</span>
                    </span>
                    <span className="shrink-0 text-xs font-medium text-acento">Usar</span>
                  </button>
                ))}
              </div>
            </div>
          </div>

          <p className="mt-6 text-center text-xs text-slate-500">
            ¿Problemas para acceder? Contacte a la Dirección de Tecnología.
          </p>
        </div>
      </section>
    </div>
  );
}
