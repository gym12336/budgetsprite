import request from './request'

export const aiApi = {
  analysis: (year: number, month: number) =>
    request.get<any, any>('/api/ai/analysis', { params: { year, month } }),

  parseRecord: (text: string) =>
    request.post<any, any>('/api/ai/parse-record', { text }),

  trendAnalysis: (months: number = 3) =>
    request.get<any, any>('/api/ai/trend-analysis', { params: { months } }),
}
