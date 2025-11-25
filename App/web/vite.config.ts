import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite'
import path from 'path';

// https://vitejs.dev/config/
export default defineConfig({
	plugins: [
		plugin(),
		tailwindcss(),
	],
	server: {
		port: 53663,
		host: "0.0.0.0",
		allowedHosts: ["host.docker.internal"]
	},
	resolve: {
		alias: {
			"@": path.resolve(__dirname, "./src"),
		},
	}
});
