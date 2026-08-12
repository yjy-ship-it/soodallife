import type { AuditFilters, AuditLogResponse, SystemStatusResponse } from './systemTypes'

async function call<T>(path: string): Promise<T> {
  const response = await fetch(path, { credentials: 'include' })
  if (!response.ok) {
    const body = await response.json().catch(() => null) as { message?: string } | null
    throw new Error(body?.message ?? '감사·시스템 정보를 불러오지 못했습니다.')
  }
  return response.json() as Promise<T>
}

async function command(path: string) {
  const response = await fetch(path, { method: 'POST', credentials: 'include' })
  if (!response.ok) throw new Error((await response.json().catch(() => null) as { message?: string } | null)?.message ?? '작업을 요청하지 못했습니다.')
}

export function getSystemStatus() {
  return call<SystemStatusResponse>('/api/v1/admin/system/status')
}

export function getAuditLogs(filters: AuditFilters) {
  const query = new URLSearchParams()
  if (filters.from) query.set('from', filters.from)
  if (filters.to) query.set('to', filters.to)
  if (filters.adminId) query.set('adminId', filters.adminId)
  if (filters.area) query.set('area', filters.area)
  if (filters.action) query.set('action', filters.action)
  query.set('page', String(filters.page ?? 1))
  return call<AuditLogResponse>(`/api/v1/admin/audit-logs?${query.toString()}`)
}

export function retryOutbox(id: string) { return command(`/api/v1/admin/system/outbox/${id}/retry`) }
