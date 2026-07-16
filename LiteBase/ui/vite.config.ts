import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import ui from '@nuxt/ui/vite'

export default defineConfig({
  base: '/litebase/ui/',
  plugins: [
    vue(),
    ui({ router: false }),
  ],
  build: {
    rollupOptions: {
      output: {
        entryFileNames: 'litebase.js',
        chunkFileNames: 'litebase.js',
        assetFileNames: 'litebase.css',
      },
    },
  },
})
