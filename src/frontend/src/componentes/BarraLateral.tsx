import { NavLink } from 'react-router-dom';
import logo from '../assets/logo-sb.png';
import inicio from '../assets/home.svg';
import { useSesion } from '../sesion/ContextoSesion';
import { iniciales, tituloRol } from '../lib/formato';
import {
  IconoBuscar,
  IconoCatalogo,
  IconoCerrar,
  IconoCrear,
  IconoEntidades,
  IconoSalir,
  IconoUsuarios
} from './Iconos';

interface PropiedadesBarraLateral {
  abierta: boolean;
  onCerrar: () => void;
}

interface Enlace {
  ruta: string;
  etiqueta: string;
  icono: 'inicio' | 'consulta' | 'crear' | 'entidades' | 'usuarios' | 'catalogos';
  soloAdministrador?: boolean;
  exacto?: boolean;
}

const ENLACES_PRINCIPALES: Enlace[] = [
  { ruta: '/', etiqueta: 'Inicio', icono: 'inicio', exacto: true },
  { ruta: '/consulta', etiqueta: 'Consulta', icono: 'consulta' },
  { ruta: '/crear', etiqueta: 'Crear registro', icono: 'crear' }
];

const ENLACES_GESTION: Enlace[] = [
  { ruta: '/entidades', etiqueta: 'Entidades gubernamentales', icono: 'entidades' },
  { ruta: '/catalogos', etiqueta: 'Catálogos del sistema', icono: 'catalogos', soloAdministrador: true },
  { ruta: '/usuarios', etiqueta: 'Usuarios y roles', icono: 'usuarios', soloAdministrador: true }
];

function IconoEnlace({ tipo, activo }: { tipo: Enlace['icono']; activo: boolean }) {
  if (tipo === 'inicio') {
    return (
      <img
        src={inicio}
        alt=""
        aria-hidden="true"
        className={`h-5 w-5 ${activo ? '' : 'brightness-0 invert opacity-80'}`}
      />
    );
  }

  const clase = 'h-5 w-5';

  switch (tipo) {
    case 'consulta':
      return <IconoBuscar className={clase} />;
    case 'crear':
      return <IconoCrear className={clase} />;
    case 'entidades':
      return <IconoEntidades className={clase} />;
    case 'catalogos':
      return <IconoCatalogo className={clase} />;
    default:
      return <IconoUsuarios className={clase} />;
  }
}

function EnlaceLateral({ enlace, onCerrar }: { enlace: Enlace; onCerrar: () => void }) {
  return (
    <NavLink
      to={enlace.ruta}
      end={enlace.exacto}
      onClick={onCerrar}
      className={({ isActive }) =>
        [
          'group flex items-center gap-3 rounded-xl px-3.5 py-2.5 text-sm font-medium transition',
          isActive
            ? 'bg-white/10 text-white shadow-[inset_0_1px_0_rgba(255,255,255,0.06)]'
            : 'text-slate-300/90 hover:bg-white/5 hover:text-white'
        ].join(' ')
      }
    >
      {({ isActive }) => (
        <>
          <span className={isActive ? 'text-acento' : 'text-slate-300'}>
            <IconoEnlace tipo={enlace.icono} activo={isActive} />
          </span>
          <span className="truncate">{enlace.etiqueta}</span>
        </>
      )}
    </NavLink>
  );
}

export function BarraLateral({ abierta, onCerrar }: PropiedadesBarraLateral) {
  const { usuario, cerrarSesion } = useSesion();

  if (!usuario) {
    return null;
  }

  const esAdministrador = usuario.rol === 'Administrador';

  return (
    <>
      {abierta && (
        <div
          className="fixed inset-0 z-30 bg-sb-950/50 backdrop-blur-sm lg:hidden"
          onClick={onCerrar}
          aria-hidden="true"
        />
      )}

      <aside
        className={[
          'fixed inset-y-0 left-0 z-40 flex w-[280px] flex-col bg-gradient-to-b from-sb-700 via-sb-800 to-sb-900 lg:bg-none',
          'transition-transform duration-200 lg:translate-x-0',
          abierta ? 'translate-x-0' : '-translate-x-full'
        ].join(' ')}
        aria-label="Navegación principal"
      >
        <div className="flex items-start justify-between px-6 pb-6 pt-7">
          <img
            src={logo}
            alt="Superintendencia de Bancos de la República Dominicana"
            className="h-[74px] w-auto brightness-0 invert"
          />
          <button
            type="button"
            onClick={onCerrar}
            className="rounded-lg p-1.5 text-slate-300 transition hover:bg-white/10 hover:text-white lg:hidden"
            aria-label="Cerrar menú"
          >
            <IconoCerrar />
          </button>
        </div>

        <nav className="flex-1 space-y-1 overflow-y-auto px-4 pb-6">
          <p className="px-3.5 pb-2 text-[11px] font-semibold uppercase tracking-[0.14em] text-slate-400/80">
            Operación
          </p>
          {ENLACES_PRINCIPALES.map((enlace) => (
            <EnlaceLateral key={enlace.ruta} enlace={enlace} onCerrar={onCerrar} />
          ))}

          <p className="px-3.5 pb-2 pt-6 text-[11px] font-semibold uppercase tracking-[0.14em] text-slate-400/80">
            Administración
          </p>
          {ENLACES_GESTION.filter((enlace) => !enlace.soloAdministrador || esAdministrador).map((enlace) => (
            <EnlaceLateral key={enlace.ruta} enlace={enlace} onCerrar={onCerrar} />
          ))}
        </nav>

        <div className="border-t border-white/10 p-4">
          <div className="flex items-center gap-3 rounded-xl bg-white/5 px-3 py-3">
            <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-acento text-sm font-bold text-white">
              {iniciales(usuario.nombre)}
            </span>
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-semibold text-white">{usuario.nombre}</p>
              <p className="truncate text-xs text-slate-300">{tituloRol(usuario.rol)}</p>
            </div>
            <button
              type="button"
              onClick={cerrarSesion}
              title="Cerrar sesión"
              aria-label="Cerrar sesión"
              className="rounded-lg p-1.5 text-slate-300 transition hover:bg-white/10 hover:text-white"
            >
              <IconoSalir />
            </button>
          </div>
        </div>
      </aside>
    </>
  );
}
