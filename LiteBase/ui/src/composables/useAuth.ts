import { ref, onMounted } from 'vue'
import { onAuthStateChanged, signOut, type User } from 'firebase/auth'
import { auth } from '../firebase'
import { apiClient } from '../api/litebase'

export function useAuth() {
  const user = ref<User | null>(null)
  const authLoading = ref(true)

  onMounted(() => {
    onAuthStateChanged(auth, async (firebaseUser) => {
      user.value = firebaseUser
      authLoading.value = false
      const token = await firebaseUser?.getIdToken()
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`
    })
  })

  const handleLogout = async () => {
    try {
      await signOut(auth)
      apiClient.defaults.headers.common['Authorization'] = ''
    } catch (error) {
      console.error('Error signing out:', error)
    }
  }

  return { user, authLoading, handleLogout }
}
