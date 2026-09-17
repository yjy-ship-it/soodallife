import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { navigate } from '../auth/routing'
import { ProviderAppLayout } from './ProviderAppLayout'
import * as api from './api'
import { providerRoute, providerStatusLabel } from './providerDisplay'
import type { ProviderHubWorkItem, ProviderOperationsHub } from './types'
import './providerHub.css'

const cacheKey = 'soodal-provider-operations-hub:v189'
const cacheMaxAgeMs = 5 * 60 * 1000
const autoRefreshMs = 60 * 1000
type HubCache = { savedAt:number; value:ProviderOperationsHub }
const hubRequests = new Map<string, Promise<ProviderOperationsHub>>()
const queryKey = (query:{group?:string;domain?:string;activeOnly?:boolean;page?:number;pageSize?:number}) => `${cacheKey}:${query.group ?? ''}:${query.domain ?? ''}:${query.activeOnly?'active':''}:${query.page ?? 1}:${query.pageSize ?? 20}`
const readCache = (key:string):ProviderOperationsHub|null => { try { const cached=JSON.parse(localStorage.getItem(key) ?? 'null') as HubCache|null; return cached && Date.now()-cached.savedAt <= cacheMaxAgeMs ? cached.value : null } catch { return null } }
const requestHub = (key:string,query:{group?:string;domain?:string;activeOnly?:boolean;page?:number;pageSize?:number}) => { const current=hubRequests.get(key); if(current)return current; const created=api.getProviderOperationsHub(query).finally(()=>hubRequests.delete(key)); hubRequests.set(key,created); return created }
const priorityOrder = ['URGENT', 'TODAY', 'ACTION_REQUIRED', 'WAITING_CUSTOMER', 'IN_PROGRESS', 'ISSUE', 'NEW', 'OPERATIONS']
const domainOptions = [['', '전체'], ['GENERAL', '일반 서비스'], ['CARE', '수달 케어'], ['INTERIOR', '수달 인테리어'], ['EMERGENCY', '긴급출동']] as const
const inboxDomainOptions = [...domainOptions, ['PROPOSALS', '제안·공동모집'], ['REVIEWS', '고객 리뷰'], ['AFTER_SERVICE', '사후관리'], ['DISPUTE', '분쟁'], ['CHAT', '채팅'], ['VERIFICATION', '승인·증빙'], ['WALLET', '이용료 잔액']] as const
const groupOptions = [['', '전체'], ['URGENT', '긴급'], ['TODAY', '오늘'], ['ACTION_REQUIRED', '응답 필요'], ['WAITING_CUSTOMER', '고객 확인 대기'], ['IN_PROGRESS', '진행 중'], ['ISSUE', '사후관리·분쟁'], ['OPERATIONS', '운영']] as const
const summaryRoute=(key:string,route:string)=>key==='new-care'?'/provider/care/requests':key==='care-progress'?'/provider/care/contracts':providerRoute(route)
const summaryLabel=(key:string,label:string)=>key==='care-progress'?'수달 케어 이용 중 계약':label

function useHub(query: { group?:string; domain?:string; activeOnly?:boolean; page?:number; pageSize?:number } = {}) {
  const group = query.group, domain = query.domain, activeOnly = query.activeOnly, page = query.page, pageSize = query.pageSize
  const key = queryKey({ group, domain, activeOnly, page, pageSize })
  const initial = useRef(readCache(key)).current
  const [value, setValue] = useState<ProviderOperationsHub | null>(initial)
  const valueRef = useRef<ProviderOperationsHub | null>(initial)
  const [error, setError] = useState('')
  const [stale, setStale] = useState(false)
  const [loading, setLoading] = useState(!initial)
  const load = useCallback(async () => {
    if (!valueRef.current) setLoading(true); setError('')
    try {
      const next = await requestHub(key, { group, domain, activeOnly, page, pageSize })
      valueRef.current = next; setValue(next); setStale(false)
      localStorage.setItem(key, JSON.stringify({ savedAt:Date.now(), value:next } satisfies HubCache))
    } catch (reason) {
      if (reason instanceof api.ProviderApiError && reason.status === 401) {
        localStorage.removeItem(key)
        navigate(`/login?returnUrl=${encodeURIComponent(window.location.pathname + window.location.search)}`)
        return
      }
      if (valueRef.current) setStale(true)
      setError(reason instanceof Error ? reason.message : '운영 정보를 불러오지 못했습니다.')
    } finally { setLoading(false) }
  }, [activeOnly, domain, group, key, page, pageSize])
  useEffect(() => { void load() }, [load])
  useEffect(() => {
    const refreshWhenActive = () => { if (document.visibilityState === 'visible' && navigator.onLine) void load() }
    const timer = window.setInterval(refreshWhenActive, autoRefreshMs)
    window.addEventListener('focus', refreshWhenActive)
    window.addEventListener('online', refreshWhenActive)
    document.addEventListener('visibilitychange', refreshWhenActive)
    return () => { window.clearInterval(timer); window.removeEventListener('focus', refreshWhenActive); window.removeEventListener('online', refreshWhenActive); document.removeEventListener('visibilitychange', refreshWhenActive) }
  }, [load])
  return { value, error, stale, loading, load }
}

function HubState({ error, stale, loading, retry }: { error:string; stale:boolean; loading:boolean; retry:()=>void }) {
  return <>{stale && <div className="hubOffline" role="status">오프라인 또는 연결 불안정 상태입니다. 표시된 정보는 최신 정보가 아닐 수 있습니다. 업무 처리는 온라인에서만 가능합니다.</div>}{error && !stale && <div className="hubError" role="alert"><span>{error}</span><button onClick={retry}>다시 시도</button></div>}{loading && <p className="hubLoading" aria-live="polite">오늘의 업무를 확인하고 있습니다…</p>}</>
}

function WorkCard({ item }: { item:ProviderHubWorkItem }) {
  return <button className={`hubWorkCard priority-${item.priorityGroup.toLowerCase()}`} onClick={() => navigate(providerRoute(item.route))}>
    <span className="hubWorkMeta"><span><i className={`hubDomain domain-${item.domain.toLowerCase()}`}>{domainLabel(item.domain)}</i><b>{item.badge}</b></span>{item.actionDueAt ? <time>응답 {date(item.actionDueAt)}</time> : item.scheduledAt && <time>{date(item.scheduledAt)}</time>}</span>
    <strong>{item.title}</strong><small>{item.description}</small><p>{item.nextAction}</p>
  </button>
}

export function ProviderOperationsHubHomePage() {
  const hub = useHub({ pageSize: 20 })
  const todayItems = useMemo(() => hub.value?.inbox.items.filter(x => ['URGENT','TODAY','ACTION_REQUIRED'].includes(x.priorityGroup)).sort((a,b) => priorityOrder.indexOf(a.priorityGroup) - priorityOrder.indexOf(b.priorityGroup)).slice(0, 8) ?? [], [hub.value])
  return <ProviderAppLayout><section className="hubHero"><div><p>수달 전문가</p><h1>오늘의 운영 허브</h1><span>지금 응답하고 진행하고 완료해야 할 실제 업무를 우선순위로 모았습니다.</span>{hub.value && <small className="hubUpdated">최근 갱신 {date(hub.value.generatedAt)} · 1분마다 자동 갱신</small>}</div><button onClick={() => void hub.load()}>새로고침</button></section>
    <HubState error={hub.error} stale={hub.stale} loading={hub.loading} retry={() => void hub.load()} />
    {hub.value && <>
      <section className="hubUrgentStrip"><div><span>긴급출동</span><strong>{hub.value.emergency.isEnabled ? hub.value.emergency.isCurrentlyAvailable ? '현재 출동 가능' : '현재 출동 불가' : '긴급출동 사용 안 함'}</strong><small>{hub.value.emergency.todayAvailability}{!hub.value.emergency.isCurrentlyAvailable && ` · ${availabilityLabel(hub.value.emergency.availabilityReason)}`} · 신규 {hub.value.emergency.newRequestCount}건 · 출동 중 {hub.value.emergency.activeAssignmentCount}건</small></div><button onClick={() => navigate(providerRoute(hub.value!.emergency.route))}>긴급업무 확인</button></section>
      <section className="hubNextSchedule"><div><span>다음 일정</span><strong>{hub.value.operationalHealth.nextSchedule ? hub.value.operationalHealth.nextSchedule.title : '확정된 다음 일정 없음'}</strong><small>{hub.value.operationalHealth.nextSchedule ? `${date(hub.value.operationalHealth.nextSchedule.scheduledAt)} · ${providerStatusLabel(hub.value.operationalHealth.nextSchedule.status)}` : '일정이 확정되면 가장 가까운 업무를 표시합니다.'}</small></div>{hub.value.operationalHealth.nextSchedule && <button onClick={() => navigate(providerRoute(hub.value!.operationalHealth.nextSchedule!.route))}>일정 확인</button>}</section>
      <section className="hubSection"><header><div><p>오늘</p><h2>오늘 할 일</h2></div><button onClick={() => navigate('/provider/inbox')}>전체 업무 보기</button></header><div className="hubWorkGrid">{todayItems.map(item => <WorkCard item={item} key={`${item.type}-${item.publicId}`} />)}</div>{!todayItems.length && <Empty text="현재 바로 처리할 업무가 없습니다." action="새 요청과 알림은 1분마다 자동으로 확인합니다." />}</section>
      <section className="hubSummary">{hub.value.summary.map(group => <article key={group.key}><h2>{group.title}</h2>{group.items.map(item => <button key={item.key} className={item.tone === 'urgent' ? 'urgent' : ''} onClick={() => navigate(summaryRoute(item.key,item.route))}><span>{summaryLabel(item.key,item.label)}</span><b>{item.count.toLocaleString()}건</b></button>)}</article>)}</section>
      <div className="hubColumns"><section className="hubSection"><header><div><p>최근 채팅</p><h2>최근 채팅</h2></div><button onClick={() => navigate('/provider/messages')}>전체</button></header>{hub.value.recentChats.map(chat => <button className="hubRow" key={chat.roomId} onClick={() => navigate(providerRoute(chat.route))}><span><b>{chat.counterpartyDisplayName}</b><small>{chat.serviceName}</small></span><span>{chat.unreadCount > 0 && <em>{chat.unreadCount}</em>}<small>{date(chat.lastMessageAt)}</small></span></button>)}{!hub.value.recentChats.length && <Empty text="진행 중인 채팅이 없습니다." action="거래가 생성되면 선택 고객과 채팅을 주고받을 수 있습니다." />}</section>
      <aside><section className="hubOpsCard"><h2>이용료 잔액</h2><strong>{money(hub.value.wallet.availableBalance)}</strong><span>{providerStatusLabel(hub.value.wallet.status)}</span><button onClick={() => navigate(providerRoute(hub.value!.wallet.route))}>원장 확인</button></section><section className="hubOpsCard"><h2>승인·증빙</h2><strong>{providerStatusLabel(hub.value.approval.approvalStatus)}</strong><span>대기 {hub.value.approval.pendingServiceCount} · 반려 {hub.value.approval.rejectedServiceCount} · 증빙 보완 {hub.value.approval.missingEvidenceCount + hub.value.approval.rejectedEvidenceCount + hub.value.approval.expiredEvidenceCount}</span><button onClick={() => navigate(providerRoute(hub.value!.approval.route))}>마이수달 확인</button></section></aside></div>
    </>}
  </ProviderAppLayout>
}

export function ProviderInboxPage({ progressOnly = false }: { progressOnly?:boolean }) {
  const query = new URLSearchParams(window.location.search)
  const [domain, setDomain] = useState(query.get('domain') ?? '')
  const [group, setGroup] = useState(query.get('group') ?? '')
  const [page, setPage] = useState(1)
  const hub = useHub({ domain, group, activeOnly: progressOnly, page, pageSize: 20 })
  const items = hub.value?.inbox.items ?? []
  return <ProviderAppLayout><section className="hubPageHead"><p>{progressOnly ? '진행 업무' : '통합 업무함'}</p><h1>{progressOnly ? '진행 중 업무' : '통합 업무함'}</h1><span>{progressOnly ? '일반 서비스·수달 케어·수달 인테리어·긴급출동 업무를 한곳에 모았습니다.' : '모든 업무의 현재 상태와 다음 할 일을 한곳에서 확인합니다.'}</span></section>
    <HubState error={hub.error} stale={hub.stale} loading={hub.loading} retry={() => void hub.load()} />
    <nav className="hubFilter" aria-label="업무 영역">{inboxDomainOptions.map(([code,label]) => <button className={domain === code ? 'active' : ''} key={code} onClick={() => { setDomain(code); setPage(1) }}>{label}</button>)}</nav>
    <nav className="hubFilter secondary" aria-label="업무 우선순위">{groupOptions.filter(([code])=>!progressOnly||!['NEW','OPERATIONS'].includes(code)).map(([code,label]) => <button className={group === code ? 'active' : ''} key={code} onClick={() => { setGroup(code); setPage(1) }}>{label}</button>)}</nav>
    <section className="hubList">{items.map(item => <WorkCard item={item} key={`${item.type}-${item.publicId}`} />)}</section>
    {!items.length && !hub.loading && <Empty text="현재 조건에 맞는 업무가 없습니다." action="필터를 바꾸거나 각 업무의 새 요청 화면을 확인해 주세요." />}
    {hub.value && <footer className="hubPager"><button disabled={page <= 1} onClick={() => setPage(x => x - 1)}>이전</button><span>{hub.value.inbox.page} / {hub.value.inbox.totalPages} · {hub.value.inbox.totalCount}건</span><button disabled={page >= hub.value.inbox.totalPages} onClick={() => setPage(x => x + 1)}>다음</button></footer>}
  </ProviderAppLayout>
}

export function ProviderSchedulePage() {
  const hub = useHub({ pageSize: 50 })
  const [domain, setDomain] = useState('')
  const [month,setMonth]=useState(()=>monthStart(new Date()))
  const items=(hub.value?.schedule??[]).filter(x=>(!domain||x.domain===domain)&&sameMonth(new Date(x.scheduledAt),month))
  const days=calendarDays(month)
  return <ProviderAppLayout><section className="hubPageHead"><p>일정 관리</p><h1>통합 일정 달력</h1><span>고객이 확정한 방문, 수달 케어, 수달 인테리어, 긴급출동 일정을 자동으로 모아 보여줍니다.</span></section><HubState error={hub.error} stale={hub.stale} loading={hub.loading} retry={() => void hub.load()} /><nav className="hubFilter">{domainOptions.map(([code,label])=><button className={domain===code?'active':''} key={code} onClick={()=>setDomain(code)}>{label}</button>)}</nav><section className="scheduleCalendar"><header><button onClick={()=>setMonth(addMonths(month,-1))}>이전 달</button><h2>{new Intl.DateTimeFormat('ko-KR',{year:'numeric',month:'long'}).format(month)}</h2><div><button onClick={()=>setMonth(monthStart(new Date()))}>오늘</button><button onClick={()=>setMonth(addMonths(month,1))}>다음 달</button></div></header><div className="scheduleWeekdays">{['일','월','화','수','목','금','토'].map(x=><b key={x}>{x}</b>)}</div><div className="scheduleMonthGrid">{days.map(day=>{const dayItems=items.filter(x=>dayKey(new Date(x.scheduledAt))===dayKey(day));return <article className={`${sameMonth(day,month)?'':'outside'}${dayKey(day)===dayKey(new Date())?' today':''}`} key={day.toISOString()}><time>{day.getDate()}</time><div>{dayItems.slice(0,3).map(item=><button className={`domain-${item.domain.toLowerCase()}`} title={`${item.title} · ${providerStatusLabel(item.status)}`} key={`${item.type}-${item.publicId}`} onClick={()=>navigate(providerRoute(item.route))}><span>{clock(item.scheduledAt)}</span>{item.title}</button>)}{dayItems.length>3&&<small>외 {dayItems.length-3}건</small>}</div></article>})}</div></section><section className="hubSection scheduleAgenda"><header><div><p>월간 일정</p><h2>{items.length}건의 자동 등록 일정</h2></div></header>{items.sort((a,b)=>Date.parse(a.scheduledAt)-Date.parse(b.scheduledAt)).map(item=><button className="hubRow" key={`${item.type}-${item.publicId}`} onClick={()=>navigate(providerRoute(item.route))}><span><b>{item.title}</b><small>{domainLabel(item.domain)} · {providerStatusLabel(item.status)}</small></span><time>{date(item.scheduledAt)}</time></button>)}{!items.length&&!hub.loading&&<Empty text="이 달에 표시할 일정이 없습니다." action="일정이 확정되면 달력에 자동으로 기록됩니다."/>}</section></ProviderAppLayout>
}
const domainLabel=(value:string)=>({GENERAL:'일반 서비스',CARE:'수달 케어',INTERIOR:'수달 인테리어',EMERGENCY:'긴급출동'}[value]??'기타 업무')
const monthStart=(value:Date)=>new Date(value.getFullYear(),value.getMonth(),1)
const addMonths=(value:Date,amount:number)=>new Date(value.getFullYear(),value.getMonth()+amount,1)
const sameMonth=(a:Date,b:Date)=>a.getFullYear()===b.getFullYear()&&a.getMonth()===b.getMonth()
const calendarDays=(month:Date)=>{const start=new Date(month.getFullYear(),month.getMonth(),1-month.getDay());return Array.from({length:42},(_,i)=>new Date(start.getFullYear(),start.getMonth(),start.getDate()+i))}
const dayKey=(value:Date)=>`${value.getFullYear()}-${value.getMonth()}-${value.getDate()}`
const clock=(value:string)=>new Intl.DateTimeFormat('ko-KR',{hour:'2-digit',minute:'2-digit'}).format(new Date(value))

function Empty({ text, action }: { text:string; action:string }) { return <div className="hubEmpty"><strong>{text}</strong><span>{action}</span></div> }
const date = (value:string|null) => value ? new Intl.DateTimeFormat('ko-KR',{month:'short',day:'numeric',hour:'2-digit',minute:'2-digit'}).format(new Date(value)) : '기록 없음'
const money = (value:number) => new Intl.NumberFormat('ko-KR',{style:'currency',currency:'KRW',maximumFractionDigits:0}).format(value)
const availabilityReasonLabels:Record<string,string> = { AVAILABLE:'운영시간 내', EMERGENCY_TEMPORARILY_UNAVAILABLE:'일시 중지', OUTSIDE_EMERGENCY_HOURS:'운영시간 외', NO_ENABLED_SERVICE:'사용 가능한 긴급 서비스 없음', AREA_NOT_CONFIGURED:'활동지역 미설정', EMERGENCY_AREA_MISMATCH:'활동지역 불일치' }
const availabilityLabel = (value:string) => availabilityReasonLabels[value] ?? '출동 설정 확인 필요'
