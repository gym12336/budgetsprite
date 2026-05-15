import request from './request'

export const recurringApi = {
  list: () => request.get<any, any>('/api/recurring'),
  create: (data: object) => request.post('/api/recurring', data),
  update: (id: number, data: object) => request.put(`/api/recurring/${id}`, data),
  remove: (id: number) => request.delete(`/api/recurring/${id}`),
  toggle: (id: number) => request.patch(`/api/recurring/${id}/toggle`),
}
