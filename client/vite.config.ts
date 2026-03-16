import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from "@tailwindcss/vite";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss() ],
  resolve: {
    alias: {
      '@core': '/src/core',
      '@utils': '/src/utils',
      '@ui': '/src/ui',
    },
  },
  server: {
    port: parseInt(process.env.VITE_CLIENT_PORT || '5173'),
    proxy: {
      '/api': {
        target: process.env.VITE_API_HOST || 'http://localhost:5285',
        changeOrigin: true,
        secure: process.env.VITE_SECURE === 'true',
        rewrite: (path) => path.replace(/^\/api/, '/api'),
      },
    },
  },
})
