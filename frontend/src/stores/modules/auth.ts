import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi } from '@/api/auth'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<{ id: number; username: string; nickname?: string; avatarUrl?: string } | null>(
    JSON.parse(localStorage.getItem('user') || 'null')
  )
  const isLoggedIn = computed(() => !!user.value)

  async function login(username: string, password: string) {
    const res = await authApi.login({ username, password })
    localStorage.setItem('access_token', res.accessToken)
    localStorage.setItem('refresh_token', res.refreshToken)
    localStorage.setItem('user', JSON.stringify(res.user))
    user.value = res.user
  }

  async function register(username: string, password: string, email?: string) {
    await authApi.register({ username, password, email })
  }

  function logout() {
    user.value = null
    localStorage.removeItem('access_token')
    localStorage.removeItem('refresh_token')
    localStorage.removeItem('user')
  }

  return { user, isLoggedIn, login, register, logout }
})
