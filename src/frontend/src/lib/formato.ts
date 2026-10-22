const FECHA_HORA = new Intl.DateTimeFormat('es-DO', {
  day: '2-digit',
  month: 'short',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit'
});

const FECHA = new Intl.DateTimeFormat('es-DO', {
  day: '2-digit',
  month: 'short',
  year: 'numeric'
});

export function formatearFechaHora(valor: string | null | undefined): string {
  if (!valor) {
    return '—';
  }

  const fecha = new Date(valor);
  return Number.isNaN(fecha.getTime()) ? '—' : FECHA_HORA.format(fecha);
}

export function formatearFecha(valor: string | null | undefined): string {
  if (!valor) {
    return '—';
  }

  const fecha = new Date(valor);
  return Number.isNaN(fecha.getTime()) ? '—' : FECHA.format(fecha);
}

/** Convierte una fecha ISO al valor que espera un <input type="date">. */
export function paraEntradaFecha(valor: string | null | undefined): string {
  if (!valor) {
    return '';
  }

  const fecha = new Date(valor);

  if (Number.isNaN(fecha.getTime())) {
    return '';
  }

  const mes = String(fecha.getMonth() + 1).padStart(2, '0');
  const dia = String(fecha.getDate()).padStart(2, '0');
  return `${fecha.getFullYear()}-${mes}-${dia}`;
}

/** Convierte el valor de un <input type="date"> a un instante ISO. */
export function desdeEntradaFecha(valor: string): string | null {
  if (!valor) {
    return null;
  }

  const fecha = new Date(`${valor}T23:59:00`);
  return Number.isNaN(fecha.getTime()) ? null : fecha.toISOString();
}

export function iniciales(nombre: string): string {
  return nombre
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((parte) => parte[0]?.toUpperCase() ?? '')
    .join('');
}

export function tituloEstado(estado: string): string {
  const etiquetas: Record<string, string> = {
    Registrada: 'Registrada',
    EnAnalisis: 'En análisis',
    EnProgreso: 'En progreso',
    EnEsperaDelSolicitante: 'En espera del solicitante',
    Resuelta: 'Resuelta',
    Cerrada: 'Cerrada'
  };

  return etiquetas[estado] ?? estado;
}

export function tituloPrioridad(prioridad: string): string {
  const etiquetas: Record<string, string> = {
    Baja: 'Baja',
    Media: 'Media',
    Alta: 'Alta',
    Critica: 'Crítica'
  };

  return etiquetas[prioridad] ?? prioridad;
}

export function tituloRol(rol: string): string {
  const etiquetas: Record<string, string> = {
    Administrador: 'Administrador',
    Analista: 'Analista',
    Solicitante: 'Solicitante'
  };

  return etiquetas[rol] ?? rol;
}
