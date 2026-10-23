import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

/**
 * The UI always calls `/api` on its own origin. In development and in the E2E
 * preview the proxy forwards those calls to the API, so the same build works
 * behind a reverse proxy in production without hardcoding a host.
 *
 * VITE_PROXY_TARGET lets the containerized dev server (compose.dev.yaml) point
 * the proxy at the `api` service instead of localhost, without touching code.
 */
export default defineConfig(({ mode }) => {
  const entorno = loadEnv(mode, '.', 'VITE_');
  const objetivoApi = entorno.VITE_PROXY_TARGET || 'http://localhost:5080';

  const proxy = {
    '/api': {
      target: objetivoApi,
      changeOrigin: true
    },
    '/salud': {
      target: objetivoApi,
      changeOrigin: true
    }
  };

  return {
    plugins: [react()],
    server: {
      port: 5173,
      strictPort: true,
      host: true,
      proxy
    },
    preview: {
      port: 4173,
      strictPort: true,
      proxy
    },
    build: {
      outDir: 'dist',
      sourcemap: false
    }
  };
});
