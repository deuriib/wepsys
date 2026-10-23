interface PropiedadesIcono {
  className?: string;
}

const BASE = {
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 1.8,
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const,
  viewBox: '0 0 24 24',
  'aria-hidden': true
};

export function IconoBuscar({ className = 'h-5 w-5' }: PropiedadesIcono) {
  return (
    <svg {...BASE} className={className}>
      <circle cx="11" cy="11" r="7" />
      <path d="M20 20l-3.5-3.5" />
    </svg>
  );
}

export function IconoCrear({ className = 'h-5 w-5' }: PropiedadesIcono) {
  return (
    <svg {...BASE} className={className}>
      <circle cx="12" cy="12" r="9" />
      <path d="M12 8v8M8 12h8" />
    </svg>
  );
}

export function IconoEntidades({ className = 'h-5 w-5' }: PropiedadesIcono) {
  return (
    <svg {...BASE} className={className}>
      <path d="M3 21h18M5 21V8l7-4 7 4v13" />
      <path d="M9 21v-5h6v5M9 10h.01M15 10h.01M9 13h.01M15 13h.01" />
    </svg>
  );
}

export function IconoUsuarios({ className = 'h-5 w-5' }: PropiedadesIcono) {
  return (
    <svg {...BASE} className={className}>
      <circle cx="9" cy="8" r="3.2" />
      <path d="M3.5 20a5.5 5.5 0 0111 0" />
      <path d="M16 5.5a3 3 0 010 5.6M17.5 20a5.2 5.2 0 00-2-4.1" />
    </svg>
  );
}

export function IconoCampana({ className = 'h-5 w-5' }: PropiedadesIcono) {
  return (
    <svg {...BASE} className={className}>
      <path d="M18 9a6 6 0 10-12 0c0 5-2 6-2 6h16s-2-1-2-6" />
      <path d="M10.3 20a2 2 0 003.4 0" />
    </svg>
  );
}

export function IconoSalir({ className = 'h-5 w-5' }: PropiedadesIcono) {
  return (
    <svg {...BASE} className={className}>
      <path d="M15 4h3a2 2 0 012 2v12a2 2 0 01-2 2h-3" />
      <path d="M10 8l-4 4 4 4M6 12h9" />
    </svg>
  );
}

export function IconoMenu({ className = 'h-6 w-6' }: PropiedadesIcono) {
  return (
    <svg {...BASE} className={className}>
      <path d="M4 7h16M4 12h16M4 17h16" />
    </svg>
  );
}

export function IconoCerrar({ className = 'h-5 w-5' }: PropiedadesIcono) {
  return (
    <svg {...BASE} className={className}>
      <path d="M6 6l12 12M18 6L6 18" />
    </svg>
  );
}

export function IconoVolver({ className = 'h-5 w-5' }: PropiedadesIcono) {
  return (
    <svg {...BASE} className={className}>
      <path d="M15 6l-6 6 6 6" />
    </svg>
  );
}

export function IconoChevron({ className = 'h-4 w-4' }: PropiedadesIcono) {
  return (
    <svg {...BASE} className={className}>
      <path d="M6 9l6 6 6-6" />
    </svg>
  );
}

export function IconoReloj({ className = 'h-5 w-5' }: PropiedadesIcono) {
  return (
    <svg {...BASE} className={className}>
      <circle cx="12" cy="12" r="9" />
      <path d="M12 7v5l3 2" />
    </svg>
  );
}

export function IconoAlerta({ className = 'h-5 w-5' }: PropiedadesIcono) {
  return (
    <svg {...BASE} className={className}>
      <path d="M10.3 4.2L2.8 17a2 2 0 001.7 3h15a2 2 0 001.7-3L13.7 4.2a2 2 0 00-3.4 0z" />
      <path d="M12 9v4M12 17h.01" />
    </svg>
  );
}

export function IconoCheck({ className = 'h-5 w-5' }: PropiedadesIcono) {
  return (
    <svg {...BASE} className={className}>
      <path d="M20 6L9 17l-5-5" />
    </svg>
  );
}

export function IconoDocumento({ className = 'h-5 w-5' }: PropiedadesIcono) {
  return (
    <svg {...BASE} className={className}>
      <path d="M14 3H7a2 2 0 00-2 2v14a2 2 0 002 2h10a2 2 0 002-2V8z" />
      <path d="M14 3v5h5M9 13h6M9 17h4" />
    </svg>
  );
}

export function IconoEditar({ className = 'h-4 w-4' }: PropiedadesIcono) {
  return (
    <svg {...BASE} className={className}>
      <path d="M12 20h9" />
      <path d="M16.5 3.5a2.1 2.1 0 013 3L7 19l-4 1 1-4z" />
    </svg>
  );
}

export function IconoBasura({ className = 'h-4 w-4' }: PropiedadesIcono) {
  return (
    <svg {...BASE} className={className}>
      <path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6M10 11v5M14 11v5" />
    </svg>
  );
}

export function IconoFiltro({ className = 'h-4 w-4' }: PropiedadesIcono) {
  return (
    <svg {...BASE} className={className}>
      <path d="M3 5h18l-7 8v6l-4 2v-8z" />
    </svg>
  );
}

export function IconoCatalogo({ className = 'h-5 w-5' }: PropiedadesIcono) {
  return (
    <svg {...BASE} className={className}>
      <path d="M4 6h16M4 10h16M4 14h16M4 18h16" />
      <rect x="2" y="3" width="20" height="18" rx="2" />
    </svg>
  );
}
