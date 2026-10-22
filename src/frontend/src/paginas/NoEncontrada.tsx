import { Link } from 'react-router-dom';
import { Boton, Tarjeta } from '../componentes/Diseno';

export function NoEncontrada() {
  return (
    <Tarjeta className="mx-auto max-w-xl">
      <div className="flex flex-col items-center gap-3 py-8 text-center">
        <p className="text-5xl font-bold tracking-tight text-sb-900/15">404</p>
        <h2 className="text-xl font-bold text-sb-900">Página no encontrada</h2>
        <p className="max-w-sm text-sm text-slate-500">
          La dirección solicitada no existe o ya no está disponible. Regrese al inicio para continuar
          trabajando.
        </p>
        <Link to="/" className="mt-2">
          <Boton>Volver al inicio</Boton>
        </Link>
      </div>
    </Tarjeta>
  );
}
