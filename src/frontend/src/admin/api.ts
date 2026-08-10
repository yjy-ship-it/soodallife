import type { AdminDashboardSummary } from './types'

export async function getAdminDashboardSummary(): Promise<AdminDashboardSummary> {
  const response = await fetch('/api/v1/admin/dashboard/summary', { credentials: 'include' })
  if (!response.ok) {
    throw new Error('관리자 현황을 불러오지 못했습니다.')
  }
  return response.json() as Promise<AdminDashboardSummary>
}
