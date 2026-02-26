import { defineConfig } from 'vite';

export default defineConfig({
    server: {
        port: 3010,
        open: true
    },
    build: {
        outDir: 'dist',
        sourcemap: true
    }
});
