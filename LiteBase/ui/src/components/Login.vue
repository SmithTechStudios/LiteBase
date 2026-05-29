<script setup lang="ts">
import { ref } from 'vue'
import { signInWithPopup, GoogleAuthProvider } from 'firebase/auth'
import { auth } from '../firebase'

const isLoading = ref(false)
const error = ref<string | null>(null)

const handleGoogleSignIn = async () => {
  isLoading.value = true
  error.value = null
  try {
    const provider = new GoogleAuthProvider()
    await signInWithPopup(auth, provider)
  } catch (err: unknown) {
    error.value = err instanceof Error ? err.message : 'Authentication failed. Please try again.'
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100 dark:from-neutral-900 dark:to-neutral-800">
    <div class="w-full max-w-md p-8 bg-white dark:bg-neutral-800 rounded-lg shadow-xl">
      <div class="text-center mb-8">
        <h1 class="text-3xl font-bold text-neutral-800 dark:text-neutral-100 mb-2">Welcome</h1>
        <p class="text-neutral-600 dark:text-neutral-400">
          Sign in with Google to access the database explorer
        </p>
      </div>
      <div class="space-y-6">
        <UAlert v-if="error" color="error" :description="error" class="mb-4" />
        <UButton size="xl" class="w-full justify-center" :loading="isLoading" @click="handleGoogleSignIn">
          <UIcon name="devicon:google" />
          Sign in with Google
        </UButton>
      </div>
    </div>
  </div>
</template>
