import request from './request'

export const statsApi = {
  month: (year: number, month: number) =>
    request.get<any, any>('/api/stats/month', { params: { year, month } }),
  year: (year: number) =>
    request.get<any, any>('/api/stats/year', { params: { year } }),
}

export const dashboardApi = {
  get: () => request.get<any, any>('/api/dashboard'),
}
