import request from './request'

export const recordsApi = {
  list: (params: object) => request.get<any, any>('/api/records', { params }),
  create: (data: object) => request.post('/api/records', data),
  update: (id: number, data: object) => request.put(`/api/records/${id}`, data),
  remove: (id: number) => request.delete(`/api/records/${id}`),
  batchRemove: (ids: number[]) => request.delete('/api/records/batch', { data: { ids } }),
  importFile: (file: File) => {
    const form = new FormData()
    form.append('file', file)
    return request.post('/api/records/import', form)
  },
  exportFile: (params: object) =>
    request.get('/api/records/export', { params, responseType: 'blob' }),
}
