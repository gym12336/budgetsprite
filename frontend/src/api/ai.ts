import request from './request'

export const aiApi = {
  analysis: (year: number, month: number) =>
    request.get<any, any>('/api/ai/analysis', { params: { year, month } }),
}
