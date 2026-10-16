import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import path from 'path';

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [
        plugin(),
        tailwindcss(),
    ],
    server: {
        port: 3000,
        host: "0.0.0.0",
        allowedHosts: ["host.docker.internal", "aspire.dev.internal"],
        proxy: {
            "/auth": {
                target: "http://localhost:4000",
                changeOrigin: true,
            },
            "/api": {
                target: "http://localhost:4000",
                changeOrigin: true,
            },
            "/band-api": {
                target: "http://localhost:4000",
                changeOrigin: true,
            },
            "/song-api": {
                target: "http://localhost:4000",
                changeOrigin: true,
            },
            "/playlist-api": {
                target: "http://localhost:4000",
                changeOrigin: true,
            },
            "/stem-api": {
                target: "http://localhost:4000",
                changeOrigin: true,
            },
            "/mixer-api": {
                target: "http://localhost:4000",
                changeOrigin: true,
            },
            "/files": {
                target: "http://localhost:4000",
                changeOrigin: true,
            },
        }
    },
    resolve: {
        alias: {
            "@": path.resolve(__dirname, "./src"),
        },
    }
});
