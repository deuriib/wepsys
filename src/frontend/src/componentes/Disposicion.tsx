import { useState } from 'react';
import { Outlet } from 'react-router-dom';
import { BarraLateral } from './BarraLateral';
import { BarraSuperior } from './BarraSuperior';

/**
 * Marco visual de la aplicación: barra lateral y encabezado sobre un mismo
 * fondo navy continuo, con un lienzo gris de esquinas superiores redondeadas
 * donde viven las tarjetas de cada página.
 */
export function Disposicion() {
  const [menuAbierto, setMenuAbierto] = useState(false);

  return (
    <div className="min-h-screen">
      {/* Marco navy continuo: sidebar y encabezado comparten esta capa fija. */}
      <div
        aria-hidden="true"
        className="fixed inset-0 -z-10 bg-sb-800/90"
      />

      <BarraLateral abierta={menuAbierto} onCerrar={() => setMenuAbierto(false)} />

      <div className="lg:pl-[280px]">
        <BarraSuperior onAbrirMenu={() => setMenuAbierto(true)} />

        <main className="min-h-[calc(100vh-88px)] rounded-t-[32px] bg-lienzo px-4 pb-12 pt-6 sm:px-6 lg:px-9">
          <div className="mx-auto w-full max-w-[1320px] animate-aparecer">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
