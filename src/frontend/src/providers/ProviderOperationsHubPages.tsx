import { useCallback, useEffect, useMemo, useState } from 'react'
import { navigate } from '../auth/routing'
import { ProviderAppLayout } from './ProviderAppLayout'
import * as api from './api'
import { providerRoute, providerStatusLabel } from './providerDisplay'
import type { ProviderHubWorkItem, ProviderOperationsHub } from './types'
import './providerHub.css'

const cacheKey = 'soodal-provider-operations-hub'
const priorityOrder = ['URGENT', 'TODAY', 'ACTION_REQUIRED', 'WAITING_CUSTOMER', 'IN_PROGRESS', 'ISSUE', 'NEW', 'OPERATIONS']
const domainOptions = [['', '전체'], ['GENERAL', '일반'], ['CARE', 'Care'], ['INTERIOR', 'Interior'], ['EMERGENCY', 'Emergency']] as const
const groupOptions = [['', '전체'], ['URGENT', '긴급'], ['TODAY', '오늘'], ['ACTION_REQUIRED', '응답 필요'], ['WAITING_CUSTOMER', '고객 확인 대기'], ['ISSUE', 'A/S·분쟁'], ['OPERATIONS', '운영']] as const

function useHub(query: { group?:string; domain?:string; page?:number; pageSize?:number } = {}) {
  const group = query.group, domain = query.domain, page = query.page, pageSize = query.pageSize
  const [value, setValue] = useState<ProviderOperationsHub | null>(null)
  const [error, setError] = useState('')
  const [stale, setStale] = useState(false)
  const [loading, setLoading] = useState(true)
  const load = useCallback(async () => {
    setLoading(true); setError('')
    try {
      const next = await api.getProviderOperationsHub({ group, domain, page, pageSize })
      setValue(next); setStale(false)
      localStorage.setItem(cacheKey, JSON.stringify(next))
    } catch (reason) {
      const cached = localStorage.getItem(cacheKey)
      if (cached) { try { setValue(JSON.parse(cached) as ProviderOperationsHub); setStale(true) } catch { /* invalid cache */ } }
      setError(reason instanceof Error ? reason.message : '운영 정보를 불러오지 못했습니다.')
    } finally { setLoading(false) }
  }, [domain, group, page, pageSize])
  useEffect(() => { void load() }, [load])
  return { value, error, stale, loading, load }
}

function HubState({ error, stale, loading, retry }: { error:string; stale:boolean; loading:boolean; retry:()=>void }) {
  return <>{stale && <div className="hubOffline" role="status">오프라인 또는 연결 불안정 상태입니다. 표시된 정보는 최신 정보가 아닐 수 있습니다. 업무 처리는 온라인에서만 가능합니다.</div>}{error && !stale && <div className="hubError" role="alert"><span>{error}</span><button onClick={retry}>다시 시도</button></div>}{loading && <p className="hubLoading" aria-live="polite">오늘의 업무를 확인하고 있습니다…</p>}</>
}

function WorkCard({ item }: { item:ProviderHubWorkItem }) {
  return <button className={`hubWorkCard priority-${item.priorityGroup.toLowerCase()}`} onClick={() => navigate(providerRoute(item.route))}>
    <span className="hubWorkMeta"><b>{item.badge}</b>{item.scheduledAt && <time>{date(item.scheduledAt)}</time>}</span>
    <strong>{item.title}</strong><small>{item.description}</small><p>{item.nextAction}</p>
  </button>
}

export function ProviderOperationsHubHomePage() {
  const hub = useHub({ pageSize: 20 })
  const todayItems = useMemo(() => hub.value?.inbox.items.filter(x => ['URGENT','TODAY','ACTION_REQUIRED'].includes(x.priorityGroup)).sort((a,b) => priorityOrder.indexOf(a.priorityGroup) - priorityOrder.indexOf(b.priorityGroup)).slice(0, 8) ?? [], [hub.value])
  return <ProviderAppLayout><section className="hubHero"><div><p>SOODAL PARTNERS</p><h1>오늘의 운영 허브</h1><span>지금 응답하고 방문하고 완료해야 할 실제 업무를 우선순위로 모았습니다.</span></div><button onClick={() => void hub.load()}>새로고침</button></section>
    <HubState error={hub.error} stale={hub.stale} loading={hub.loading} retry={() => void hub.load()} />
    {hub.value && <>
      <section className="hubUrgentStrip"><div><span>긴급출동</span><strong>{hub.value.emergency.isEnabled ? hub.value.emergency.isCurrentlyAvailable ? '현재 출동 가능' : '현재 출동 불가' : '긴급출동 OFF'}</strong><small>{hub.value.emergency.todayAvailability} · 신규 {hub.value.emergency.newRequestCount}건 · 출동 중 {hub.value.emergency.activeAssignmentCount}건</small></div><button onClick={() => navigate(providerRoute(hub.value!.emergency.route))}>긴급업무 확인</button></section>
      <section className="hubSection"><header><div><p>TODAY</p><h2>오늘 할 일</h2></div><button onClick={() => navigate('/provider/inbox')}>전체 업무 보기</button></header><div className="hubWorkGrid">{todayItems.map(item => <WorkCard item={item} key={`${item.type}-${item.publicId}`} />)}</div>{!todayItems.length && <Empty text="현재 바로 처리할 업무가 없습니다." action="새 요청과 알림은 자동 새로고침되지 않으므로 필요할 때 새로고침해 주세요." />}</section>
      <section className="hubSummary">{hub.value.summary.map(group => <article key={group.key}><h2>{group.title}</h2>{group.items.map(item => <button key={item.key} className={item.tone === 'urgent' ? 'urgent' : ''} onClick={() => navigate(providerRoute(item.route))}><span>{item.label}</span><b>{item.count.toLocaleString()}건</b></button>)}</article>)}</section>
      <div className="hubColumns"><section className="hubSection"><header><div><p>MESSAGES</p><h2>최근 메시지</h2></div><button onClick={() => navigate('/provider/messages')}>전체</button></header>{hub.value.recentChats.map(chat => <button className="hubRow" key={chat.roomId} onClick={() => navigate(providerRoute(chat.route))}><span><b>{chat.counterpartyDisplayName}</b><small>{chat.serviceName}</small></span><span>{chat.unreadCount > 0 && <em>{chat.unreadCount}</em>}<small>{date(chat.lastMessageAt)}</small></span></button>)}{!hub.value.recentChats.length && <Empty text="진행 중인 대화가 없습니다." action="거래가 생성되면 선택 고객과 메시지를 주고받을 수 있습니다." />}</section>
      <aside><section className="hubOpsCard"><h2>충전금</h2><strong>{money(hub.value.wallet.availableBalance)}</strong><span>{providerStatusLabel(hub.value.wallet.status)}</span><button onClick={() => navigate(providerRoute(hub.value!.wallet.route))}>원장 확인</button></section><section className="hubOpsCard"><h2>승인·증빙</h2><strong>{providerStatusLabel(hub.value.approval.approvalStatus)}</strong><span>대기 {hub.value.approval.pendingServiceCount} · 반려 {hub.value.approval.rejectedServiceCount} · 증빙 보완 {hub.value.approval.missingEvidenceCount + hub.value.approval.rejectedEvidenceCount + hub.value.approval.expiredEvidenceCount}</span><button onClick={() => navigate(providerRoute(hub.value!.approval.route))}>마이수달 확인</button></section></aside></div>
    </>}
  </ProviderAppLayout>
}

export function ProviderInboxPage({ progressOnly = false }: { progressOnly?:boolean }) {
  const query = new URLSearchParams(window.location.search)
  const [domain, setDomain] = useState(query.get('domain') ?? '')
  const [group, setGroup] = useState(progressOnly ? '' : query.get('group') ?? '')
  const [page, setPage] = useState(1)
  const hub = useHub({ domain, group: progressOnly ? undefined : group, page, pageSize: 20 })
  const items = progressOnly ? hub.value?.inbox.items.filter(x => ['TODAY','IN_PROGRESS','WAITING_CUSTOMER','ACTION_REQUIRED'].includes(x.priorityGroup)) ?? [] : hub.value?.inbox.items ?? []
  return <ProviderAppLayout><section className="hubPageHead"><p>{progressOnly ? 'ACTIVE WORK' : 'OPERATIONS INBOX'}</p><h1>{progressOnly ? '진행 중 업무' : '통합 업무 Inbox'}</h1><span>{progressOnly ? '일반·Care·Interior·Emergency 업무를 표시용 분류로만 모았습니다.' : '기존 업무 상태를 저장하지 않고 읽기 전용으로 모아 보여줍니다.'}</span></section>
    <HubState error={hub.error} stale={hub.stale} loading={hub.loading} retry={() => void hub.load()} />
    <nav className="hubFilter" aria-label="업무 영역">{domainOptions.map(([code,label]) => <button className={domain === code ? 'active' : ''} key={code} onClick={() => { setDomain(code); setPage(1) }}>{label}</button>)}</nav>
    {!progressOnly && <nav className="hubFilter secondary" aria-label="업무 우선순위">{groupOptions.map(([code,label]) => <button className={group === code ? 'active' : ''} key={code} onClick={() => { setGroup(code); setPage(1) }}>{label}</button>)}</nav>}
    <section className="hubList">{items.map(item => <WorkCard item={item} key={`${item.type}-${item.publicId}`} />)}</section>
    {!items.length && !hub.loading && <Empty text="현재 조건에 맞는 업무가 없습니다." action="필터를 바꾸거나 각 업무의 새 요청 화면을 확인해 주세요." />}
    {!progressOnly && hub.value && <footer className="hubPager"><button disabled={page <= 1} onClick={() => setPage(x => x - 1)}>이전</button><span>{hub.value.inbox.page} / {hub.value.inbox.totalPages} · {hub.value.inbox.totalCount}건</span><button disabled={page >= hub.value.inbox.totalPages} onClick={() => setPage(x => x + 1)}>다음</button></footer>}
  </ProviderAppLayout>
}

export function ProviderSchedulePage() {
  const hub = useHub({ pageSize: 50 })
  const [domain, setDomain] = useState('')
  const items = hub.value?.schedule.filter(x => !domain || x.domain === domain) ?? []
  return <ProviderAppLayout><section className="hubPageHead"><p>SCHEDULE</p><h1>통합 일정</h1><span>일반 Appointment, Care 방문, Interior 일정과 진행 중 긴급업무를 읽기 전용으로 확인합니다.</span></section><HubState error={hub.error} stale={hub.stale} loading={hub.loading} retry={() => void hub.load()} /><nav className="hubFilter">{domainOptions.map(([code,label]) => <button className={domain === code ? 'active' : ''} key={code} onClick={() => setDomain(code)}>{label}</button>)}</nav><section className="hubTimeline">{items.map(item => <button key={`${item.type}-${item.publicId}`} onClick={() => navigate(providerRoute(item.route))}><time>{date(item.scheduledAt)}</time><span><b>{item.title}</b><small>{item.domain} · {providerStatusLabel(item.status)}</small></span></button>)}</section>{!items.length && !hub.loading && <Empty text="표시할 일정이 없습니다." action="일정 제안이나 고객 확정 후 이곳에 함께 표시됩니다." />}</ProviderAppLayout>
}

function Empty({ text, action }: { text:string; action:string }) { return <div className="hubEmpty"><strong>{text}</strong><span>{action}</span></div> }
const date = (value:string|null) => value ? new Intl.DateTimeFormat('ko-KR',{month:'short',day:'numeric',hour:'2-digit',minute:'2-digit'}).format(new Date(value)) : '기록 없음'
const money = (value:number) => new Intl.NumberFormat('ko-KR',{style:'currency',currency:'KRW',maximumFractionDigits:0}).format(value)
