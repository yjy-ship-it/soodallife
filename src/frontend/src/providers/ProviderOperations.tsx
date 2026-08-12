import { useEffect, useState } from 'react'
import { navigate } from '../auth/routing'
import { AuthenticatedLayout } from '../components/AuthenticatedLayout'
import * as quoteApi from '../quotes/api'
import type { QuoteListItem } from '../quotes/types'
import * as providerApi from './api'
import type { ProviderOperationsDashboard } from './types'

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
  return <section className="providerPanel"><div className="sectionHeading"><div><p className="eyebrow">TODAY</p><h2>업무 대시보드</h2></div><span>실제 업무 데이터 기준</span></div><div className="providerGrid operationsGrid">{cards.map(([key, label, path]) => <button className="providerCard" key={key} onClick={() => navigate(path)}><small>{label}</small><strong>{value?.[key] ?? 0}건</strong><p>{value?.[key] ? '확인할 업무가 있습니다.' : '현재 항목이 없습니다.'}</p></button>)}</div></section>
}

export function ProviderOperationsHomePage() {
  return <AuthenticatedLayout><section className="providerHero"><div><p>SOODAL PARTNERS</p><h1>공급자 업무 홈</h1><span>매칭 요청부터 견적, 일정, 작업완료까지 지금 처리할 실제 업무를 확인합니다.</span></div></section><ProviderOperationsPanel /><section className="providerPanel"><h2>빠른 업무</h2><div className="providerActions"><button className="providerPrimary" onClick={() => navigate('/provider/matched-requests')}>매칭 요청 확인</button><button className="providerSecondary" onClick={() => navigate('/provider/quotes')}>내 견적</button><button className="providerSecondary" onClick={() => navigate('/provider/work')}>거래·작업</button><button className="providerSecondary" onClick={() => navigate('/provider/onboarding')}>공급자 정보</button></div></section></AuthenticatedLayout>
}

export function ProviderQuoteListPage() {
  const [items, setItems] = useState<QuoteListItem[]>([]), [status, setStatus] = useState(''), [error, setError] = useState('')
  useEffect(() => { quoteApi.getProviderQuotes().then(setItems).catch((reason: Error) => setError(reason.message)) }, [])
  const visible = items.filter(item => !status || item.status === status)
  return <AuthenticatedLayout><section className="providerHero"><div><p>QUOTES</p><h1>내 견적</h1><span>초안, 제출, 고객 선택 결과와 최신 Revision을 한곳에서 확인합니다.</span></div></section>{error && <div className="errorBanner">{error}</div>}<nav className="workFilters" aria-label="견적 상태">{[['','전체'],['DRAFT','작성 중'],['SUBMITTED','선택 대기'],['ACCEPTED','채택'],['NOT_SELECTED','미채택']].map(([code,label]) => <button key={code} className={status===code?'active':''} onClick={() => setStatus(code)}>{label}</button>)}</nav><section className="workList">{visible.map(item => <button key={item.id} onClick={() => navigate(item.transactionId ? `/provider/work/${item.transactionId}` : `/provider/matched-requests/${item.requestId}`)}><span className={`workStatus s-${item.status}`}>{item.status}</span><h2>{item.requestTitle}</h2><p>{item.categoryPath}</p><dl><div><dt>최신 Revision</dt><dd>{item.revisionNo}차</dd></div><div><dt>견적 금액</dt><dd>{money(item.totalAmount)}</dd></div></dl></button>)}</section>{visible.length===0 && <p className="workEmpty">조건에 맞는 견적이 없습니다.</p>}<p className="privacyNote">다른 공급자의 견적과 금액은 공급자 화면에 공개되지 않습니다.</p></AuthenticatedLayout>
}

const money = (value:number) => new Intl.NumberFormat('ko-KR',{style:'currency',currency:'KRW',maximumFractionDigits:0}).format(value)
