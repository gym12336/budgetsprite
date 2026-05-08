import request from './request'

export const authApi = {
  register: (data: { username: string; password: string; email?: string }) =>
    request.post('/api/auth/register', data),

  login: (data: { username: string; password: string }) =>
    request.post<any, any>('/api/auth/login', data),

  refresh: (refreshToken: string) =>
    request.post<any, any>('/api/auth/refresh', { refreshToken }),
}
