import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { crearCliente, configurarExpiracionSesion } from '../lib/api';
import type { ClienteApi } from '../lib/api';
import type { RespuestaAutenticacion, UsuarioSesion } from '../lib/tipos';

const CLAVE_ALMACENAMIENTO = 'sb.gestion-solicitudes.sesion';

interface SesionGuardada {
  token: string;
  expiraEn: string;
  usuario: UsuarioSesion;
}

interface ValorSesion {
  sesion: SesionGuardada | null;
  usuario: UsuarioSesion | null;
  token: string | null;
  cliente: ClienteApi;
  iniciarSesion: (correo: string, contrasena: string) => Promise<RespuestaAutenticacion>;
  cerrarSesion: () => void;
}

const ContextoSesion = createContext<ValorSesion | null>(null);

function leerSesionGuardada(): SesionGuardada | null {
  const crudo = window.localStorage.getItem(CLAVE_ALMACENAMIENTO);

  if (!crudo) {
    return null;
  }

  try {
    const sesion = JSON.parse(crudo) as SesionGuardada;

    if (!sesion.token || !sesion.usuario || new Date(sesion.expiraEn).getTime() <= Date.now()) {
      window.localStorage.removeItem(CLAVE_ALMACENAMIENTO);
      return null;
    }

    return sesion;
  } catch {
    window.localStorage.removeItem(CLAVE_ALMACENAMIENTO);
    return null;
  }
}

export function ProveedorSesion({ children }: { children: ReactNode }) {
  const [sesion, setSesion] = useState<SesionGuardada | null>(() => leerSesionGuardada());

  const cerrarSesion = useCallback(() => {
    window.localStorage.removeItem(CLAVE_ALMACENAMIENTO);
    setSesion(null);
  }, []);

  useEffect(() => {
    configurarExpiracionSesion(cerrarSesion);
    return () => configurarExpiracionSesion(null);
  }, [cerrarSesion]);

  const iniciarSesion = useCallback(async (correo: string, contrasena: string) => {
    const clienteAnonimo = crearCliente(null);
    const respuesta = await clienteAnonimo.iniciarSesion(correo, contrasena);
    const nuevaSesion: SesionGuardada = {
      token: respuesta.token,
      expiraEn: respuesta.expiraEn,
      usuario: respuesta.usuario
    };

    window.localStorage.setItem(CLAVE_ALMACENAMIENTO, JSON.stringify(nuevaSesion));
    setSesion(nuevaSesion);
    return respuesta;
  }, []);

  const cliente = useMemo(() => crearCliente(sesion?.token ?? null), [sesion?.token]);

  const valor = useMemo<ValorSesion>(
    () => ({
      sesion,
      usuario: sesion?.usuario ?? null,
      token: sesion?.token ?? null,
      cliente,
      iniciarSesion,
      cerrarSesion
    }),
    [sesion, cliente, iniciarSesion, cerrarSesion]
  );

  return <ContextoSesion.Provider value={valor}>{children}</ContextoSesion.Provider>;
}

export function useSesion(): ValorSesion {
  const contexto = useContext(ContextoSesion);

  if (!contexto) {
    throw new Error('useSesion debe usarse dentro de ProveedorSesion.');
  }

  return contexto;
}
