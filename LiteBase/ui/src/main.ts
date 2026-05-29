import { createApp } from 'vue'
import { VueQueryPlugin } from '@tanstack/vue-query'
import ui from '@nuxt/ui/vue-plugin'
import './index.css'
import App from './App.vue'

const app = createApp(App)
app.use(VueQueryPlugin)
app.use(ui)
app.mount('#app')
