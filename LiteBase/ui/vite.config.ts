import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  /* @ts-ignore */
    plugins: [vue(), tailwindcss()],
  build: {
    rollupOptions: {
      output: {
        entryFileNames: `litebase.js`,
        assetFileNames: `litebase.[ext]`
      }
    }
  },
})
