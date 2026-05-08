import request from './request'

export const categoriesApi = {
  list: () => request.get<any, any>('/api/categories'),
  create: (data: object) => request.post('/api/categories', data),
  update: (id: number, data: object) => request.put(`/api/categories/${id}`, data),
  remove: (id: number) => request.delete(`/api/categories/${id}`),
}

export const accountsApi = {
  list: () => request.get<any, any>('/api/accounts'),
  create: (data: object) => request.post('/api/accounts', data),
  update: (id: number, data: object) => request.put(`/api/accounts/${id}`, data),
  remove: (id: number) => request.delete(`/api/accounts/${id}`),
  transfer: (data: object) => request.post('/api/accounts/transfer', data),
}

export const budgetsApi = {
  list: (yearMonth: string) => request.get<any, any>('/api/budgets', { params: { yearMonth } }),
  upsert: (data: object) => request.post('/api/budgets', data),
  remove: (id: number) => request.delete(`/api/budgets/${id}`),
}
