import { Boton } from './Diseno';

interface PropiedadesPaginacion {
  pagina: number;
  totalPaginas: number;
  totalRegistros: number;
  tamanoPagina: number;
  onCambiar: (pagina: number) => void;
}

export function Paginacion({ pagina, totalPaginas, totalRegistros, tamanoPagina, onCambiar }: PropiedadesPaginacion) {
  if (totalRegistros === 0) {
    return null;
  }

  const desde = (pagina - 1) * tamanoPagina + 1;
  const hasta = Math.min(pagina * tamanoPagina, totalRegistros);

  return (
    <div className="flex flex-col gap-3 border-t border-slate-100 px-6 py-4 sm:flex-row sm:items-center sm:justify-between">
      <p className="text-sm text-slate-500">
        Mostrando <span className="font-medium text-sb-900">{desde}</span>–
        <span className="font-medium text-sb-900">{hasta}</span> de{' '}
        <span className="font-medium text-sb-900">{totalRegistros}</span> registros
      </p>
      <div className="flex items-center gap-2">
        <Boton
          variante="secundario"
          tamano="sm"
          onClick={() => onCambiar(pagina - 1)}
          disabled={pagina <= 1}
          aria-label="Página anterior"
        >
          Anterior
        </Boton>
        <span className="px-1 text-sm text-slate-600">
          Página {pagina} de {Math.max(totalPaginas, 1)}
        </span>
        <Boton
          variante="secundario"
          tamano="sm"
          onClick={() => onCambiar(pagina + 1)}
          disabled={pagina >= totalPaginas}
          aria-label="Página siguiente"
        >
          Siguiente
        </Boton>
      </div>
    </div>
  );
}
