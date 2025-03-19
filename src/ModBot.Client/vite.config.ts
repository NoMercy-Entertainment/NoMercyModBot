import {fileURLToPath, URL} from 'node:url';

import {defineConfig} from 'vite';
import vue from '@vitejs/plugin-vue';
import vueJsx from '@vitejs/plugin-vue-jsx';
import vueDevTools from 'vite-plugin-vue-devtools';
import tailwindcss from '@tailwindcss/vite';

// https://vite.dev/config/
export default defineConfig({
    plugins: [
        tailwindcss(),
        vue(),
        vueJsx(),
        vueDevTools(),
    ],
    server: {
        host: '0.0.0.0',
        port: 5250,
        hmr: {
            // port: 5250,
        },
        allowedHosts: [
            'modbot.nomercy.tv',
        ],
        proxy: {
            '/api': {
                target: 'http://localhost:5251',
                changeOrigin: true,
                secure: false
            }
        }
    },
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        },
    },
})
