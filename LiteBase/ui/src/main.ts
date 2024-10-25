import { createApp } from 'vue'
import './index.css'
import App from './App.vue'


import PrimeVue from 'primevue/config';
import Aura from '@primevue/themes/aura';
import Button from 'primevue/button'
import ProgressSpinner from 'primevue/progressspinner'
import InputText from 'primevue/inputtext'

import 'primeicons/primeicons.css'

const app = createApp(App);
app.use(PrimeVue, {
    theme: {
        preset: Aura
    }
});

app.component('Button', Button);
app.component('ProgressSpinner', ProgressSpinner);
app.component('InputText', InputText);
app.mount('#app')
