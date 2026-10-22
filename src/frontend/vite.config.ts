import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

/**
 * The UI always calls `/api` on its own origin. In development and in the E2E
 * preview the proxy forwards those calls to the API, so the same build works
 * behind a reverse proxy in production without hardcoding a host.
 */
const proxy = {
  '/api': {
    target: 'http://localhost:5080',
    changeOrigin: true
  },
  '/salud': {
    target: 'http://localhost:5080',
    changeOrigin: true
  }
};

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
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
});
