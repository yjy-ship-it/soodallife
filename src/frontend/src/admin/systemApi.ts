import type { AuditFilters, AuditLogResponse, SecurityAccount, SystemStatusResponse } from './systemTypes'

async function call<T>(path: string): Promise<T> {
  const response = await fetch(path, { credentials: 'include' })
  if (!response.ok) {
    const body = await response.json().catch(() => null) as { message?: string } | null
    throw new Error(body?.message ?? '감사·시스템 정보를 불러오지 못했습니다.')
  }
  return response.json() as Promise<T>
}

async function command<T=void>(path: string, method='POST', body?:unknown, reauthToken?:string):Promise<T> {
  const response = await fetch(path, { method, credentials: 'include', headers:{...(body?{'Content-Type':'application/json'}:{}),...(reauthToken?{'X-Admin-Reauth-Token':reauthToken}:{})}, body:body?JSON.stringify(body):undefined })
  if (!response.ok) throw new Error((await response.json().catch(() => null) as { message?: string } | null)?.message ?? '작업을 요청하지 못했습니다.')
  return response.status===204?undefined as T:response.json() as Promise<T>
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

export function retryOutbox(id: string, reason:string, token:string) { return command(`/api/v1/admin/system/outbox/${id}/retry`,'POST',{reason},token) }
export function reauthenticate(password:string,mfaCode:string){return command<{token:string;expiresAt:string}>('/api/v1/admin/security/reauthenticate','POST',{password,mfaCode:mfaCode||null})}
export function getSecurityAccounts(){return call<SecurityAccount[]>('/api/v1/admin/security/accounts')}
export function createSecurityAccount(input:{loginId:string;password:string;detailRoleCode:string},token:string){return command<SecurityAccount>('/api/v1/admin/security/accounts','POST',input,token)}
export function updateSecurityAccount(id:string,input:{statusCode:string;detailRoleCode:string},token:string){return command(`/api/v1/admin/security/accounts/${id}`,'PUT',input,token)}
export function beginMfa(){return command<{secret:string;otpAuthUri:string}>('/api/v1/admin/security/mfa/enrollment')}
export function confirmMfa(code:string){return command('/api/v1/admin/security/mfa/confirm','POST',{code})}
export function updateRetention(id:string,input:{actionCode:string;retentionDays:number;legalHoldDays:number;isEnabled:boolean;dryRun:boolean},token:string){return command(`/api/v1/admin/system/retention/${id}`,'PUT',input,token)}
export function assessRetention(id:string,token:string){return command(`/api/v1/admin/system/retention/${id}/assess`,'POST',undefined,token)}
