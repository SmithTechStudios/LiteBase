<script setup lang="ts">
import { ref } from 'vue'
import { signInWithPopup, GoogleAuthProvider } from 'firebase/auth'
import { auth } from '../firebase'
import Button from 'primevue/button'
import Message from 'primevue/message'
import { Icon } from '@iconify/vue'

const isLoading = ref(false)
const error = ref<string | null>(null)

const handleGoogleSignIn = async () => {
  isLoading.value = true
  error.value = null

  try {
    const provider = new GoogleAuthProvider()
    await signInWithPopup(auth, provider)
  } catch (err: any) {
    error.value = err.message || 'Authentication failed. Please try again.'
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100 dark:from-neutral-900 dark:to-neutral-800">
    <div class="w-full max-w-md p-8 bg-white dark:bg-neutral-800 rounded-lg shadow-xl">
      <div class="text-center mb-8">
        <h1 class="text-3xl font-bold text-neutral-800 dark:text-neutral-100 mb-2">
          Welcome
        </h1>
        <p class="text-neutral-600 dark:text-neutral-400">
          Sign in with Google to access the database explorer
        </p>
      </div>

      <div class="space-y-6">
        <div v-if="error" class="mb-4">
          <Message severity="error" :closable="false">{{ error }}</Message>
        </div>

        <Button size="lg" class="w-full justify-center text-lg transition-colors duration-300" @click="handleGoogleSignIn">
      <Icon icon="devicon:google" />
      Sign in with Google
    </Button>
      </div>
    </div>
  </div>
</template>

