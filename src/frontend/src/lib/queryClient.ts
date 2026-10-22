import { QueryClient } from '@tanstack/react-query';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 30, // 30 segundos de vigencia en caché
      gcTime: 1000 * 60 * 5, // 5 minutos de retención en memoria
      refetchOnWindowFocus: false,
      retry: (failureCount, error) => {
        // No reintentar ante errores definitivos de autenticación o autorización
        if (error && typeof error === 'object' && 'estado' in error) {
          const estado = (error as { estado?: number }).estado;
          if (estado === 401 || estado === 403 || estado === 404) {
            return false;
          }
        }
        return failureCount < 2;
      }
    }
  }
});
