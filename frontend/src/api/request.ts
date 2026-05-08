import axios from 'axios'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '@/stores/modules/auth'
import router from '@/router'

const request = axios.create({
  baseURL: '',
  timeout: 15000,
})

request.interceptors.request.use((config) => {
  const token = localStorage.getItem('access_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

let isRefreshing = false
let pendingQueue: Array<(token: string) => void> = []

request.interceptors.response.use(
  (res) => res.data,
  async (err) => {
    const original = err.config
    if (err.response?.status === 401 && !original._retry) {
      if (isRefreshing) {
        return new Promise((resolve) => {
          pendingQueue.push((token) => {
            original.headers.Authorization = `Bearer ${token}`
            resolve(request(original))
          })
        })
      }
      original._retry = true
      isRefreshing = true
      try {
        const refreshToken = localStorage.getItem('refresh_token')
        const data: any = await axios.post(
          `/api/auth/refresh`,
          { refreshToken }
        )
        localStorage.setItem('access_token', data.accessToken)
        pendingQueue.forEach((cb) => cb(data.accessToken))
        pendingQueue = []
        original.headers.Authorization = `Bearer ${data.accessToken}`
        return request(original)
      } catch {
        useAuthStore().logout()
        router.push('/login')
        ElMessage.error('登录已过期，请重新登录')
        return Promise.reject(err)
      } finally {
        isRefreshing = false
      }
    }

    const msg = err.response?.data?.message || err.message || '请求失败'
    if (err.response?.status !== 401) ElMessage.error(msg)
    return Promise.reject(err)
  }
)

export default request
