import type { AdminAnalyticsDashboard, AdminAnalyticsFilters, AdminDashboardSummary } from './types'

export async function getAdminDashboardSummary(): Promise<AdminDashboardSummary> {
  const response = await fetch('/api/v1/admin/dashboard/summary', { credentials: 'include' })
  if (!response.ok) {
    throw new Error('관리자 현황을 불러오지 못했습니다.')
  }
  return response.json() as Promise<AdminDashboardSummary>
}

export async function getAdminAnalyticsDashboard(filters: AdminAnalyticsFilters): Promise<AdminAnalyticsDashboard> {
  const query = new URLSearchParams({ range: filters.range })
  if (filters.from) query.set('from', filters.from)
  if (filters.to) query.set('to', filters.to)
  if (filters.categoryId) query.set('categoryId', filters.categoryId)
  if (filters.areaId) query.set('areaId', filters.areaId)
  const response = await fetch(`/api/v1/admin/dashboard/management?${query.toString()}`, { credentials: 'include' })
  if (!response.ok) throw new Error('경영 대시보드를 불러오지 못했습니다.')
  return response.json() as Promise<AdminAnalyticsDashboard>
}
