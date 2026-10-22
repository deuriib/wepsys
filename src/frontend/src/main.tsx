import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { App } from './App';
import { ProveedorSesion } from './sesion/ContextoSesion';
import './index.css';

const contenedor = document.getElementById('root');

if (!contenedor) {
  throw new Error('No se encontró el elemento raíz de la aplicación.');
}

createRoot(contenedor).render(
  <StrictMode>
    <BrowserRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
      <ProveedorSesion>
        <App />
      </ProveedorSesion>
    </BrowserRouter>
  </StrictMode>
);
