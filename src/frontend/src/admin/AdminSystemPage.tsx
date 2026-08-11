import { useCallback, useEffect, useState } from 'react'
import { navigate } from '../auth/routing'
import { AdminLayout } from './AdminLayout'
import { getAuditLogs, getSystemStatus } from './systemApi'
import type { AuditFilters, AuditLogResponse, SystemStatusResponse } from './systemTypes'

const tabs = ['운영현황', '감사로그', '관리자 계정', '설정·보안'] as const
const dateTime = (value: string | null) => value ? new Intl.DateTimeFormat('ko-KR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value)) : '-'

export function AdminSystemPage({ pathname }: { pathname: string }) {
  const [tab, setTab] = useState<(typeof tabs)[number]>('운영현황')
  const [status, setStatus] = useState<SystemStatusResponse | null>(null)
  const [audit, setAudit] = useState<AuditLogResponse | null>(null)
  const [filters, setFilters] = useState<AuditFilters>({ page: 1 })
  const [error, setError] = useState('')
  const loadStatus = useCallback(async () => { try { setError(''); setStatus(await getSystemStatus()) } catch (e) { setError(e instanceof Error ? e.message : '시스템 상태를 불러오지 못했습니다.') } }, [])
  const loadAudit = useCallback(async () => { try { setError(''); setAudit(await getAuditLogs(filters)) } catch (e) { setError(e instanceof Error ? e.message : '감사로그를 불러오지 못했습니다.') } }, [filters])
  useEffect(() => { void loadStatus() }, [loadStatus])
  useEffect(() => { void loadAudit() }, [loadAudit])
  const setFilter = (key: keyof AuditFilters, value: string | number | undefined) => setFilters(current => ({ ...current, [key]: value || undefined, page: key === 'page' ? Number(value) : 1 }))

  return <AdminLayout pathname={pathname}>
    <section className="adminPageHeading"><div><p>운영·보안 기반</p><h1>감사·보안·시스템 관리</h1></div><span>읽기 전용 운영상태와 append-only 감사기록을 확인합니다.</span></section>
    {error && <div className="adminError" role="alert">{error}</div>}
    <nav className="systemTabs">{tabs.map(value => <button type="button" className={tab === value ? 'active' : ''} onClick={() => setTab(value)} key={value}>{value}</button>)}</nav>
    {tab === '운영현황' && <Operations status={status} reload={loadStatus} />}
    {tab === '감사로그' && <AuditPanel value={audit} filters={filters} setFilter={setFilter} />}
    {tab === '관리자 계정' && <Accounts status={status} />}
    {tab === '설정·보안' && <Security status={status} />}
  </AdminLayout>
}

function Operations({ status, reload }: { status: SystemStatusResponse | null; reload: () => Promise<void> }) {
  if (!status) return <div className="analyticsLoading">운영 상태를 확인하고 있습니다.</div>
  return <>
    <section className="systemHealth"><div><span>DB 연결</span><strong className={status.database.connection === 'CONNECTED' ? 'ok' : 'critical'}>{status.database.connection}</strong></div><div><span>Migration</span><strong>{status.database.migrationStatus}</strong><small>대기 {status.database.pendingMigrationCount}건</small></div><div><span>기준시각</span><strong>{dateTime(status.generatedAt)}</strong></div><button type="button" onClick={() => void reload()}>새로고침</button></section>
    <section className="systemMetricGrid">{status.attention.map(item => <button type="button" key={item.code} onClick={() => navigate(item.path)}><span className={item.severity.toLowerCase()}>{item.severity}</span><strong>{item.count.toLocaleString('ko-KR')}건</strong><b>{item.label}</b></button>)}</section>
    <div className="systemColumns"><section className="systemPanel"><h2>Outbox 현황</h2><div className="systemStatusList">{status.outbox.map(item => <div key={item.status}><span>{item.status}</span><strong>{item.count.toLocaleString('ko-KR')}건</strong></div>)}</div><p className="systemNotice">재처리는 멱등성과 중복 실행 안전성이 검증되지 않아 제공하지 않습니다.</p></section><section className="systemPanel"><h2>최근 Outbox 실패</h2>{status.recentOutboxFailures.length ? status.recentOutboxFailures.map(item => <article className="outboxFailure" key={item.id}><strong>{item.eventType}</strong><span>{item.aggregateType} · 시도 {item.attemptCount}회 · {dateTime(item.lastAttemptAt)}</span><p>{item.lastError ?? '오류 메시지 없음'}</p></article>) : <p className="systemEmpty">실패 Event가 없습니다.</p>}</section></div>
    <section className="systemPanel"><h2>외부연동 상태</h2><div className="integrationGrid">{status.integrations.map(item => <article key={item.code}><span>{item.label}</span><strong>{item.status === 'UNINTEGRATED' ? '미연동' : item.status}</strong><small>{item.note}</small></article>)}</div></section>
  </>
}

function AuditPanel({ value, filters, setFilter }: { value: AuditLogResponse | null; filters: AuditFilters; setFilter: (key: keyof AuditFilters, value: string | number | undefined) => void }) {
  return <section className="systemPanel"><div className="systemPanelHeading"><div><h2>관리자 감사로그</h2><p>변경 전·후 값은 개인정보와 Secret을 마스킹해 표시합니다.</p></div><strong>{value?.totalCount ?? 0}건</strong></div>
    <div className="auditFilters"><label>시작일<input type="date" value={filters.from ?? ''} onChange={e => setFilter('from', e.target.value)} /></label><label>종료일<input type="date" value={filters.to ?? ''} onChange={e => setFilter('to', e.target.value)} /></label><label>관리자<select value={filters.adminId ?? ''} onChange={e => setFilter('adminId', e.target.value)}><option value="">전체</option>{value?.administrators.map(item => <option value={item.value} key={item.value}>{item.label}</option>)}</select></label><label>업무영역<select value={filters.area ?? ''} onChange={e => setFilter('area', e.target.value)}><option value="">전체</option>{value?.areas.map(item => <option value={item.value} key={item.value}>{item.label}</option>)}</select></label><label>Action<select value={filters.action ?? ''} onChange={e => setFilter('action', e.target.value)}><option value="">전체</option>{value?.actions.map(item => <option value={item.value} key={item.value}>{item.label}</option>)}</select></label></div>
    <div className="customerTableWrap"><table className="customerTable"><thead><tr><th>발생일시</th><th>관리자·역할</th><th>업무영역</th><th>Action</th><th>대상</th><th>사유</th><th>Correlation ID</th><th>상세</th></tr></thead><tbody>{value?.items.map((item, index) => <tr key={`${item.occurredAt}-${index}`}><td>{dateTime(item.occurredAt)}</td><td>{item.adminLoginId}<br/><small>{item.actorRole ?? '-'}</small></td><td>{item.area}</td><td><code>{item.action}</code></td><td>{item.targetType}<br/><small>{item.targetId ?? '-'}</small></td><td>{item.reason ?? '-'}</td><td><small>{item.correlationId ?? '-'}</small></td><td><details><summary>전후값</summary><div className="auditDiff"><pre>{item.beforeJson ?? '변경 전 없음'}</pre><pre>{item.afterJson ?? '변경 후 없음'}</pre></div></details></td></tr>)}</tbody></table></div>
    {!value?.items.length && <p className="systemEmpty">조건에 맞는 감사로그가 없습니다.</p>}
    <footer className="customerPagination"><button type="button" disabled={(filters.page ?? 1) <= 1} onClick={() => setFilter('page', (filters.page ?? 1) - 1)}>이전</button><span>{filters.page ?? 1}</span><button type="button" disabled={!value || (filters.page ?? 1) * value.pageSize >= value.totalCount} onClick={() => setFilter('page', (filters.page ?? 1) + 1)}>다음</button></footer>
  </section>
}

function Accounts({ status }: { status: SystemStatusResponse | null }) {
  return <section className="systemPanel"><div className="systemPanelHeading"><div><h2>관리자 계정</h2><p>비밀번호 원문은 조회하지 않습니다. 현재 Schema는 ADMIN 단일 역할만 지원합니다.</p></div><strong>{status?.administrators.length ?? 0}명</strong></div><div className="customerTableWrap"><table className="customerTable"><thead><tr><th>계정</th><th>상태</th><th>역할</th><th>생성일</th><th>마지막 로그인</th></tr></thead><tbody>{status?.administrators.map(item => <tr key={item.id}><td>{item.loginId}</td><td>{item.status}</td><td>{item.roles.join(', ')}</td><td>{dateTime(item.createdAt)}</td><td>{dateTime(item.lastLoginAt)}</td></tr>)}</tbody></table></div><p className="systemNotice">원 설계의 세부 권한과 승인 절차가 현재 Schema에 없어 계정 추가·권한변경·비활성화 Write API는 만들지 않았습니다.</p></section>
}

function Security({ status }: { status: SystemStatusResponse | null }) {
  return <><section className="systemPanel"><h2>기존 업무별 설정 원본</h2><div className="managedSettingList">{status?.managedSettings.map(item => <button type="button" key={item.area} onClick={() => navigate(item.path)}><strong>{item.area}</strong><span>{item.source}</span><small>{item.note}</small></button>)}</div><p className="systemNotice">범용 key/value 설정 테이블을 중복 생성하지 않았습니다.</p></section><section className="systemPanel securityLimitations"><h2>보안 제한사항</h2><ul>{status?.securityLimitations.map(item => <li key={item}>{item}</li>)}</ul></section></>
}
