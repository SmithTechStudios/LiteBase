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
  <div class="min-h-screen flex items-center justify-center bg-(--ui-bg)">
    <UCard class="w-full max-w-md">
      <template #header>
        <div class="text-center">
          <h1 class="text-2xl font-bold">Welcome to Litebase</h1>
          <p class="text-(--ui-text-muted) mt-1">Sign in with Google to access the database explorer</p>
        </div>
      </template>
      <div class="flex flex-col gap-4">
        <UAlert v-if="error" color="error" :description="error" />
        <UButton block size="xl" :loading="isLoading" @click="handleGoogleSignIn">
          <UIcon name="devicon:google" />
          Sign in with Google
        </UButton>
      </div>
    </UCard>
  </div>
</template>
