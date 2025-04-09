import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import vueJsx from '@vitejs/plugin-vue-jsx';
import vueDevTools from 'vite-plugin-vue-devtools';
import tailwindcss from '@tailwindcss/vite';
import { VitePWA } from 'vite-plugin-pwa';
import { ViteCspPlugin } from 'vite-plugin-csp';

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    tailwindcss(),
    vue(),
    vueJsx(),
    vueDevTools(),
    VitePWA({
      registerType: 'autoUpdate',
      strategies: 'generateSW',
      workbox: {
        cleanupOutdatedCaches: true,
        clientsClaim: true,
        skipWaiting: true,
        sourcemap: true,
        globPatterns: ['**/*.{js,css,html,ico,png,svg,jpg,jpeg,gif,webp,woff,woff2,ttf,eot,json}'],
        globIgnores: ['**/*.webmanifest'],
        navigateFallback: 'index.html',
        runtimeCaching: [
          {
            urlPattern: /^https:\/\/app\.nomercy\.tv\/.*/i,
            handler: 'StaleWhileRevalidate',
            options: {
              cacheName: 'static-assets-v2',
              fetchOptions: {
                mode: 'cors',
                credentials: 'same-origin'
              },
              cacheableResponse: {
                statuses: [0, 200]
              }
            }
          },
          {
            urlPattern: /^https:\/\/(cdn|storage|cdn-dev)\.nomercy\.tv\/.*/i,
            handler: 'CacheFirst',
            options: {
              cacheName: 'cdn-assets-v1',
              expiration: {
                maxEntries: 1000,
                maxAgeSeconds: 60 * 60 * 24 * 30
              },
              cacheableResponse: {
                statuses: [0, 200]
              }
            }
          },
          {
            urlPattern: ({ url }) => {
              return /^api(-dev)?.nomercy\.tv/.test(url.hostname);
            },
            handler: 'NetworkFirst',
            options: {
              cacheName: 'nomercy-api-v1',
              backgroundSync: {
                name: 'nomercy-api-v1',
                options: { forceSyncFallback: true }
              }
            }
          },
          // {
          // 	// this should match urls like https://192-168-2-123.123abc-1234abc-123abc-123abc.nomercy.tv:7626/api
          // 	urlPattern: ({ url }) => {
          // 		return /^[^.]+\.[^.]+\.nomercy\.tv/.test(url.hostname)
          // 			&& url.pathname.includes('/api');
          // 	},
          // 	handler: 'NetworkFirst',
          // 	options: {
          // 		cacheName: 'mediaserver-api',
          // 		backgroundSync: {
          // 			name: 'mediaserver-api',
          // 			options: { forceSyncFallback: true }
          // 		}
          // 	}
          // },
          {
            // this should match urls like https://192-168-2-123.123abc-1234abc-123abc-123abc.nomercy.tv:7626/images
            urlPattern: ({ url }) => {
              const isApiServer = /^[^.]+\.[^.]+\.nomercy\.tv/.test(url.hostname);
              const isImageRequest = url.pathname.match(/\/images\/.*?\.(jpg|jpeg|png|gif|webp|avif)$/i);
              return isApiServer && isImageRequest;
            },
            handler: 'NetworkFirst',
            options: {
              cacheName: 'cover-images-v1',
              expiration: {
                maxEntries: 100000,
                maxAgeSeconds: 60 * 60 * 24 * 30 // 30 days
              },
              cacheableResponse: {
                statuses: [0, 200]
              }
            }
          },
        ],
        maximumFileSizeToCacheInBytes: 25 * 1024 * 1024 * 1024, // 25GB per item
      },
      manifest: {
        name: 'NoMercy TV',
        short_name: 'NoMercy TV',
        description:
          'Encode and archive all your movies and tv show\'s, and play them on all your devices.',
        categories: [
          'video',
          'encoder',
          'player',
          'library',
          'nomercy',
          'server',
        ],
        theme_color: '#000000',
        background_color: '#000000',
        // display_override: ['standalone', 'minimal-ui'],
        display: 'standalone',
        orientation: 'any',
        scope: '/',
        start_url: '/',
        display_override: [
          'standalone',
          'window-controls-overlay',
          'fullscreen'
        ],
        iarc_rating_id: '6',
        id: 'tv.nomercy.app',
        protocol_handlers: [
          {
            protocol: 'web+nomercy',
            url: '/%s',
          },
        ],
        prefer_related_applications: true,
        icons: [
          {
            src: '/img/icons/android-chrome-512x512.png',
            sizes: '512x512',
            purpose: 'maskable',
          },
          {
            src: '/img/icons/android-chrome-512x512.png',
            sizes: '512x512',
            purpose: 'any',
          },
          {
            src: '/img/icons/android-chrome-384x384.png',
            sizes: '384x384',
            purpose: 'any',
          },
          {
            src: '/img/icons/android-chrome-256x256.png',
            sizes: '256x256',
            purpose: 'any',
          },
          {
            src: '/img/icons/android-chrome-192x192.png',
            sizes: '192x192',
            purpose: 'any',
          },
          {
            src: '/img/icons/android-chrome-144x144.png',
            sizes: '144x144',
            purpose: 'any',
          },
          {
            src: '/img/icons/android-chrome-96x96.png',
            sizes: '96x96',
            purpose: 'any',
          },
          {
            src: '/img/icons/android-chrome-72x72.png',
            sizes: '72x72',
            purpose: 'any',
          },
          {
            src: '/img/icons/android-chrome-48x48.png',
            sizes: '48x48',
            purpose: 'any',
          },
          {
            src: '/img/icons/android-chrome-36x36.png',
            sizes: '36x36',
            purpose: 'any',
          },
        ],
        screenshots: [],
        shortcuts: [
          {
            name: 'Home',
            short_name: 'Home',
            url: '/',
            icons: [
              {
                src: '/img/icons/android-chrome-192x192.png',
                sizes: '192x192',
                type: 'image/png',
                purpose: 'any',
              },
            ],
          },
        ],
      },
      base: '/',
      devOptions: {
        enabled: false,
        type: 'module',
        navigateFallback: 'index.html',
        suppressWarnings: false,
      },
    }),
    ViteCspPlugin({
        'base-uri': [
          'self',
        ],
        'object-src': [
          'self',
          'blob:',
        ],
        'script-src': [
          'self',
          'unsafe-eval',
          'unsafe-inline',
          'unsafe-hashes',
          'https://*.nomercy.tv',
          'https://static.cloudflareinsights.com',
        ],
        'style-src': [
          'self',
          'unsafe-inline',
          'unsafe-eval',
          'https://cdn.nomercy.tv',
          'https://cdn-dev.nomercy.tv',
          'https://storage.nomercy.tv',
        ],
        'img-src': [
          'self',
          'blob:',
          'data:',
          'https://nomercy.tv',
          'https://*.nomercy.tv:*',
          'https://*.nomercy.tv',
          'https://static-cdn.jtvnw.net',
          'https://pub-a68768bb5b1045f296df9ea56bd53a7f.r2.dev',
          'wss://*.nomercy.tv:*',
        ],
        'connect-src': [
          'self',
          'blob:',
          'data:',
          'https://nomercy.tv',
          'https://*.nomercy.tv:*',
          'https://*.nomercy.tv',
          'ws://*.nomercy.tv:*',
          'wss://*.nomercy.tv:*',
          'https://pub-a68768bb5b1045f296df9ea56bd53a7f.r2.dev',
          'https://raw.githubusercontent.com',
          'https://usher.ttvnw.net',
          'ws://localhost:5251',
          'ws://192.168.2.201:5251',
        ],
        'frame-src': [
          'self',
          'https://nomercy.tv',
          'https://*.nomercy.tv:*',
          'https://*.twitch.tv',
        ],
        'font-src': [
          'self',
          'blob:',
          'data:',
          'https://cdn.nomercy.tv',
          'https://cdn-dev.nomercy.tv',
          'https://rsms.me',
        ],
        'media-src': [
          'self',
          'blob:',
          'data:',
          'https://nomercy.tv',
          'https://*.nomercy.tv',
          'https://*.nomercy.tv:*',
          'wss://*.nomercy.tv:*',
          'https://pub-a68768bb5b1045f296df9ea56bd53a7f.r2.dev',
          'https://usher.ttvnw.net',
        ],
        'worker-src': [
          'self',
          'blob:',
        ],
      },
      {
        enabled: true,
        hashingMethod: 'sha256',
        hashEnabled: {
          'script-src': true,
          'style-src': false,
          'script-src-attr': false,
          'style-src-attr': false,
        },
        nonceEnabled: {
          'script-src': false,
          'style-src': false,
        },
        // processFn: 'Nginx',
      }),
  ],
  server: {
    host: '0.0.0.0',
    port: 5250,
    hmr: {
      // port: 5250,
    },
    allowedHosts: [
      'modbot.nomercy.tv'
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
    }
  }
});
