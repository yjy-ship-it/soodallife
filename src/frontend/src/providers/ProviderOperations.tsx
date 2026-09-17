import { useEffect, useState } from 'react'
import { navigate } from '../auth/routing'
import { AuthenticatedLayout } from '../components/AuthenticatedLayout'
import * as quoteApi from '../quotes/api'
import type { QuoteListItem } from '../quotes/types'
import * as providerApi from './api'
import type { ProviderOperationsDashboard } from './types'
import { providerStatusLabel } from './providerDisplay'
import '../pages/workFlow.css'

const cards: Array<[keyof ProviderOperationsDashboard, string, string]> = [
  ['newMatchedRequestCount', '신규 매칭 요청', '/provider/matched-requests'],
  ['submittedQuoteCount', '제출한 견적', '/provider/quotes'],
  ['waitingSelectionQuoteCount', '고객 선택 대기', '/provider/quotes'],
  ['selectedTransactionCount', '채택된 거래', '/provider/work'],
  ['appointmentActionRequiredCount', '일정 응답 필요', '/provider/work'],
  ['todayAppointmentCount', '오늘 일정', '/provider/work'],
  ['inProgressWorkCount', '진행 중 작업', '/provider/work'],
  ['waitingCompletionConfirmationCount', '완료확인 대기', '/provider/work'],
  ['revisionRequestedCount', '보완 요청', '/provider/work'],
  ['unreadNotificationCount', '미읽은 알림', '/provider/notifications'],
]

export function ProviderOperationsPanel() {
  const [value, setValue] = useState<ProviderOperationsDashboard | null>(null)
  useEffect(() => { providerApi.getProviderOperationsDashboard().then(setValue).catch(() => setValue(null)) }, [])
  return <section className="providerPanel"><div className="sectionHeading"><div><p className="eyebrow">오늘 업무</p><h2>업무 대시보드</h2></div><span>실제 업무 데이터 기준</span></div><div className="providerGrid operationsGrid">{cards.map(([key, label, path]) => <button className="providerCard" key={key} onClick={() => navigate(path)}><small>{label}</small><strong>{value?.[key] ?? 0}건</strong><p>{value?.[key] ? '확인할 업무가 있습니다.' : '현재 항목이 없습니다.'}</p></button>)}</div></section>
}

export function ProviderOperationsHomePage() {
  return <AuthenticatedLayout><section className="providerHero"><div><p>수달 전문가</p><h1>전문가 업무 홈</h1><span>매칭 요청부터 견적, 일정, 작업완료까지 지금 처리할 실제 업무를 확인합니다.</span></div></section><ProviderOperationsPanel /><section className="providerPanel"><h2>빠른 업무</h2><div className="providerActions"><button className="providerPrimary" onClick={() => navigate('/provider/matched-requests')}>매칭 요청 확인</button><button className="providerSecondary" onClick={() => navigate('/provider/emergency')}>긴급출동</button><button className="providerSecondary" onClick={() => navigate('/provider/care')}>수달 케어</button><button className="providerSecondary" onClick={() => navigate('/provider/interior')}>수달 인테리어</button><button className="providerSecondary" onClick={() => navigate('/provider/quotes')}>내 견적</button><button className="providerSecondary" onClick={() => navigate('/provider/work')}>거래·작업</button><button className="providerSecondary" onClick={() => navigate('/provider/wallet')}>이용료·수수료</button><button className="providerSecondary" onClick={() => navigate('/provider/onboarding')}>전문가 정보</button></div></section></AuthenticatedLayout>
}

export function ProviderQuoteListPage() {
  const pageSize = 20
  const [items, setItems] = useState<QuoteListItem[]>([]), [status, setStatus] = useState(''), [error, setError] = useState(''), [page, setPage] = useState(1)
  useEffect(() => { quoteApi.getProviderQuotes().then(setItems).catch((reason: Error) => setError(reason.message)) }, [])
  const filtered = items.filter(item => !status || item.status === status).sort((a, b) => quoteSortValue(b) - quoteSortValue(a))
  const pageCount = Math.max(1, Math.ceil(filtered.length / pageSize))
  const currentPage = Math.min(page, pageCount)
  const visible = filtered.slice((currentPage - 1) * pageSize, currentPage * pageSize)
  const changeStatus = (code: string) => { setStatus(code); setPage(1) }
  const open = (item: QuoteListItem) => navigate(item.transactionId ? `/provider/work/${item.transactionId}` : `/provider/matched-requests/${item.requestId}`)
  return <AuthenticatedLayout><main className="providerQuotePage"><section className="providerHero providerQuoteHero"><div><p>견적 관리</p><h1>내 견적 결과</h1><span>제출일이 최신인 견적부터 고객의 선택 결과를 확인합니다.</span></div></section>{error && <div className="errorBanner">{error}</div>}<nav className="quoteStatusFilters" aria-label="견적 상태">{[['','전체'],['DRAFT','작성 중'],['SUBMITTED','선택 대기'],['ACCEPTED','채택'],['NOT_SELECTED','미채택']].map(([code,label]) => <button type="button" key={code} className={status===code?'active':''} onClick={() => changeStatus(code)}>{label}</button>)}</nav>{visible.length > 0 && <section className="providerQuoteList" aria-label="제출 견적 목록">{visible.map(item => <button type="button" className={`providerQuoteCard quote-${item.status.toLowerCase()}`} key={item.id} onClick={() => open(item)}><header><span className={`workStatus s-${item.status}`}>{providerStatusLabel(item.status)}</span><span className={`requestDomainBadge domain-${item.domain.toLowerCase()}`}>{quoteDomainLabel(item.domain)}</span></header><span className="providerQuoteSubject"><strong>{item.requestTitle}</strong><small>{item.categoryPath}</small></span><dl><div><dt>견적 제출일</dt><dd>{item.submittedAt ? formatQuoteDate(item.submittedAt) : '아직 제출하지 않음'}</dd></div><div><dt>제출한 견적 금액</dt><dd>{money(item.totalAmount)}</dd></div></dl><span className="providerQuoteOpen">상세 확인</span></button>)}</section>}{filtered.length===0 && <p className="workEmpty">조건에 맞는 견적이 없습니다.</p>}{filtered.length > pageSize && <nav className="quotePagination" aria-label="견적 목록 페이지"><button type="button" disabled={currentPage <= 1} onClick={() => setPage(value => Math.max(1, value - 1))}>이전</button><span>{currentPage} / {pageCount} · {filtered.length}건</span><button type="button" disabled={currentPage >= pageCount} onClick={() => setPage(value => Math.min(pageCount, value + 1))}>다음</button></nav>}<p className="privacyNote">한 페이지에 20개씩 표시합니다. 다른 전문가의 견적과 금액은 전문가 화면에 공개되지 않습니다.</p></main></AuthenticatedLayout>
}

const money = (value:number) => new Intl.NumberFormat('ko-KR',{style:'currency',currency:'KRW',maximumFractionDigits:0}).format(value)
const quoteSortValue = (item: QuoteListItem) => item.submittedAt ? new Date(item.submittedAt).getTime() : 0
const formatQuoteDate = (value: string) => new Intl.DateTimeFormat('ko-KR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
const quoteDomainLabel=(value:QuoteListItem['domain'])=>({GENERAL:'일반 서비스',INTERIOR:'수달 인테리어',EMERGENCY:'긴급출동'}[value])
