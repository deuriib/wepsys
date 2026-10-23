import { useEffect } from 'react';
import type { ReactNode } from 'react';
import { Navigate, Route, Routes, useLocation } from 'react-router-dom';
import { Disposicion } from './componentes/Disposicion';
import { Consulta } from './paginas/Consulta';
import { CrearRegistro } from './paginas/CrearRegistro';
import { DetalleSolicitud } from './paginas/DetalleSolicitud';
import { Entidades } from './paginas/Entidades';
import { Catalogos } from './paginas/Catalogos';
import { Inicio } from './paginas/Inicio';
import { IniciarSesion } from './paginas/IniciarSesion';
import { NoEncontrada } from './paginas/NoEncontrada';
import { Notificaciones } from './paginas/Notificaciones';
import { Usuarios } from './paginas/Usuarios';
import { useSesion } from './sesion/ContextoSesion';

/** Exige una sesión activa y conserva el destino original para después del login. */
function RutaPrivada() {
  const { sesion } = useSesion();
  const ubicacion = useLocation();

  if (!sesion) {
    return <Navigate to="/ingresar" replace state={{ desde: ubicacion.pathname }} />;
  }

  return <Disposicion />;
}

/** Restringe una pantalla al rol administrador. */
function RutaAdministrador({ children }: { children: ReactNode }) {
  const { usuario } = useSesion();

  if (usuario?.rol !== 'Administrador') {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}

export function App() {
  const ubicacion = useLocation();

  useEffect(() => {
    window.scrollTo({ top: 0, behavior: 'instant' as ScrollBehavior });
  }, [ubicacion.pathname]);

  return (
    <Routes>
      <Route path="/ingresar" element={<IniciarSesion />} />

      <Route element={<RutaPrivada />}>
        <Route path="/" element={<Inicio />} />
        <Route path="/consulta" element={<Consulta />} />
        <Route path="/crear" element={<CrearRegistro />} />
        <Route path="/solicitudes/:id" element={<DetalleSolicitud />} />
        <Route path="/entidades" element={<Entidades />} />
        <Route
          path="/catalogos"
          element={
            <RutaAdministrador>
              <Catalogos />
            </RutaAdministrador>
          }
        />
        <Route path="/notificaciones" element={<Notificaciones />} />
        <Route
          path="/usuarios"
          element={
            <RutaAdministrador>
              <Usuarios />
            </RutaAdministrador>
          }
        />
        <Route path="*" element={<NoEncontrada />} />
      </Route>
    </Routes>
  );
}
