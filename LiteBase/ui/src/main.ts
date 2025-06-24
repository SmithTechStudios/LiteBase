import { createApp } from 'vue'
import { VueQueryPlugin } from '@tanstack/vue-query'
import './index.css'
import App from './App.vue'


import PrimeVue from 'primevue/config';
import Aura from '@primeuix/themes/aura';


const app = createApp(App);
app.use(PrimeVue, {
    theme: {
        preset: Aura
    }
});

// Add VueQueryPlugin to your app
app.use(VueQueryPlugin);

app.mount('#app')
