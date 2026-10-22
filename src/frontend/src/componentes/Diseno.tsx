import { useEffect } from 'react';
import type {
  ButtonHTMLAttributes,
  InputHTMLAttributes,
  ReactNode,
  SelectHTMLAttributes,
  TextareaHTMLAttributes
} from 'react';

export const CLASE_ENTRADA =
  'w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-tinta shadow-sm transition placeholder:text-slate-400 ' +
  'focus:border-sb-500 focus:outline-none focus:ring-2 focus:ring-sb-500/20 disabled:cursor-not-allowed disabled:bg-slate-100 disabled:text-slate-500';

type VarianteBoton = 'primario' | 'secundario' | 'fantasma' | 'peligro';

interface PropiedadesBoton extends ButtonHTMLAttributes<HTMLButtonElement> {
  variante?: VarianteBoton;
  tamano?: 'sm' | 'md';
  cargando?: boolean;
  icono?: ReactNode;
}

const ESTILOS_BOTON: Record<VarianteBoton, string> = {
  primario: 'bg-acento text-white hover:bg-acento-fuerte shadow-sm disabled:bg-acento/60',
  secundario: 'border border-slate-300 bg-white text-sb-800 hover:border-sb-500 hover:text-sb-900',
  fantasma: 'text-sb-700 hover:bg-sb-900/5',
  peligro: 'bg-red-600 text-white hover:bg-red-700 disabled:bg-red-400'
};

export function Boton({
  variante = 'primario',
  tamano = 'md',
  cargando = false,
  icono,
  className = '',
  children,
  disabled,
  ...resto
}: PropiedadesBoton) {
  return (
    <button
      {...resto}
      disabled={disabled || cargando}
      className={[
        'inline-flex items-center justify-center gap-2 rounded-lg font-semibold transition',
        'disabled:cursor-not-allowed',
        tamano === 'sm' ? 'px-3 py-1.5 text-xs' : 'px-4 py-2.5 text-sm',
        ESTILOS_BOTON[variante],
        className
      ].join(' ')}
    >
      {cargando ? <Girando /> : icono}
      {children}
    </button>
  );
}

export function Girando({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg className={`animate-spin ${className}`} viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
      <path className="opacity-90" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
    </svg>
  );
}

interface PropiedadesTarjeta {
  children: ReactNode;
  className?: string;
  titulo?: ReactNode;
  descripcion?: ReactNode;
  acciones?: ReactNode;
}

export function Tarjeta({ children, className = '', titulo, descripcion, acciones }: PropiedadesTarjeta) {
  return (
    <section
      className={`rounded-panel border border-slate-200/80 bg-white shadow-panel ${className}`}
    >
      {(titulo || acciones) && (
        <header className="flex flex-col gap-3 border-b border-slate-100 px-6 py-5 sm:flex-row sm:items-center sm:justify-between">
          <div>
            {titulo && <h2 className="text-base font-semibold text-sb-900">{titulo}</h2>}
            {descripcion && <p className="mt-1 text-sm text-slate-500">{descripcion}</p>}
          </div>
          {acciones && <div className="flex shrink-0 items-center gap-2">{acciones}</div>}
        </header>
      )}
      <div className="px-6 py-5">{children}</div>
    </section>
  );
}

interface PropiedadesCampo {
  etiqueta: string;
  para: string;
  children: ReactNode;
  error?: string;
  ayuda?: string;
  requerido?: boolean;
  className?: string;
}

export function Campo({ etiqueta, para, children, error, ayuda, requerido, className = '' }: PropiedadesCampo) {
  return (
    <div className={className}>
      <label htmlFor={para} className="mb-1.5 block text-sm font-medium text-sb-900">
        {etiqueta}
        {requerido && <span className="ml-0.5 text-acento">*</span>}
      </label>
      {children}
      {error ? (
        <p id={`${para}-error`} className="mt-1.5 text-xs font-medium text-red-600">
          {error}
        </p>
      ) : (
        ayuda && <p className="mt-1.5 text-xs text-slate-500">{ayuda}</p>
      )}
    </div>
  );
}

export function Entrada(props: InputHTMLAttributes<HTMLInputElement>) {
  const { className = '', ...resto } = props;
  return <input {...resto} className={`${CLASE_ENTRADA} ${className}`} />;
}

export function AreaTexto(props: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  const { className = '', ...resto } = props;
  return <textarea {...resto} className={`${CLASE_ENTRADA} min-h-[110px] resize-y ${className}`} />;
}

export function Seleccion(props: SelectHTMLAttributes<HTMLSelectElement>) {
  const { className = '', children, ...resto } = props;
  return (
    <select {...resto} className={`${CLASE_ENTRADA} ${className}`}>
      {children}
    </select>
  );
}

type TonoAlerta = 'error' | 'exito' | 'info' | 'aviso';

const TONOS_ALERTA: Record<TonoAlerta, string> = {
  error: 'border-red-200 bg-red-50 text-red-800',
  exito: 'border-emerald-200 bg-emerald-50 text-emerald-800',
  info: 'border-sky-200 bg-sky-50 text-sky-900',
  aviso: 'border-amber-200 bg-amber-50 text-amber-900'
};

export function Alerta({
  tono = 'info',
  children,
  className = ''
}: {
  tono?: TonoAlerta;
  children: ReactNode;
  className?: string;
}) {
  return (
    <div
      role={tono === 'error' ? 'alert' : 'status'}
      className={`rounded-lg border px-4 py-3 text-sm ${TONOS_ALERTA[tono]} ${className}`}
    >
      {children}
    </div>
  );
}

export function Cargando({ mensaje = 'Cargando información…' }: { mensaje?: string }) {
  return (
    <div className="flex items-center justify-center gap-3 py-16 text-sm text-slate-500">
      <Girando className="h-5 w-5 text-acento" />
      {mensaje}
    </div>
  );
}

export function Vacio({
  titulo,
  descripcion,
  accion
}: {
  titulo: string;
  descripcion?: string;
  accion?: ReactNode;
}) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 py-14 text-center">
      <div className="flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-slate-400">
        <svg viewBox="0 0 24 24" className="h-6 w-6" fill="none" stroke="currentColor" strokeWidth="1.7">
          <path d="M4 7h16M4 12h10M4 17h7" strokeLinecap="round" />
        </svg>
      </div>
      <p className="text-sm font-semibold text-sb-900">{titulo}</p>
      {descripcion && <p className="max-w-sm text-sm text-slate-500">{descripcion}</p>}
      {accion && <div className="mt-3">{accion}</div>}
    </div>
  );
}

interface PropiedadesModal {
  abierto: boolean;
  titulo: string;
  descripcion?: string;
  onCerrar: () => void;
  children: ReactNode;
  pie?: ReactNode;
  ancho?: 'normal' | 'amplio';
}

export function Modal({ abierto, titulo, descripcion, onCerrar, children, pie, ancho = 'normal' }: PropiedadesModal) {
  useEffect(() => {
    if (!abierto) {
      return;
    }

    const manejarTecla = (evento: KeyboardEvent) => {
      if (evento.key === 'Escape') {
        onCerrar();
      }
    };

    document.addEventListener('keydown', manejarTecla);
    document.body.style.overflow = 'hidden';

    return () => {
      document.removeEventListener('keydown', manejarTecla);
      document.body.style.overflow = '';
    };
  }, [abierto, onCerrar]);

  if (!abierto) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-sb-950/50 p-4 backdrop-blur-sm sm:items-center">
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="titulo-modal"
        className={`animate-aparecer w-full ${ancho === 'amplio' ? 'max-w-3xl' : 'max-w-xl'} rounded-panel bg-white shadow-flotante`}
      >
        <header className="flex items-start justify-between gap-4 border-b border-slate-100 px-6 py-5">
          <div>
            <h2 id="titulo-modal" className="text-lg font-semibold text-sb-900">
              {titulo}
            </h2>
            {descripcion && <p className="mt-1 text-sm text-slate-500">{descripcion}</p>}
          </div>
          <button
            type="button"
            onClick={onCerrar}
            aria-label="Cerrar"
            className="rounded-lg p-1.5 text-slate-400 transition hover:bg-slate-100 hover:text-slate-700"
          >
            <svg viewBox="0 0 24 24" className="h-5 w-5" fill="none" stroke="currentColor" strokeWidth="1.8">
              <path d="M6 6l12 12M18 6L6 18" strokeLinecap="round" />
            </svg>
          </button>
        </header>
        <div className="px-6 py-5">{children}</div>
        {pie && <footer className="flex flex-wrap justify-end gap-2 border-t border-slate-100 px-6 py-4">{pie}</footer>}
      </div>
    </div>
  );
}
