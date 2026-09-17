import { useEffect, useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import { useCallback } from 'react'
import { navigate } from '../auth/routing'
import { AdminLayout } from './AdminLayout'
import { getAdminCustomer, searchAdminCustomers, setAdminCustomerRequestAbuseExclusion } from './customerApi'
import type { AdminCustomerDetail, AdminCustomerList } from './customerTypes'
import { soodalAlert, soodalPrompt } from '../components/soodalDialog'

const roleLabel: Record<string, string> = { CUSTOMER: '고객', PROVIDER: '전문가', ADMIN: '관리자' }
const statusLabel: Record<string, string> = {
  ACTIVE: '정상', SUSPENDED: '정지', WITHDRAWN: '탈퇴·철회', DRAFT: '작성 중', OPEN: '견적 접수 중', ACCEPTED: '채택 완료',
  EXPIRED: '기간 만료', CANCELLED: '취소', SUBMITTED: '제출', NOT_SELECTED: '미채택', INVALIDATED: '무효',
  CREATED: '거래 생성', IN_PROGRESS: '진행 중', COMPLETION_SUBMITTED: '완료 확인 중', REVISION_REQUESTED: '보완 요청', COMPLETED: '완료', DISPUTED: '분쟁',
  RECEIVED: '접수', AFTER_SERVICE_RECEIVED: 'A/S 접수', AFTER_SERVICE_STARTED: 'A/S 진행', AFTER_SERVICE_COMPLETED: 'A/S 완료', COMPLETION: '작업 완료',
}
const customerTabs = ['기본정보', '주소', '요청', '견적·채택', '거래', '후기·A/S', '수리·서비스 이력', '동의·상태', '관리이력'] as const
type CustomerTab = (typeof customerTabs)[number]

const date = (value: string | null) => value ? new Intl.DateTimeFormat('ko-KR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : '-'
const amount = (value: number | null, currency = 'KRW') => value === null ? '기록 없음' : `${new Intl.NumberFormat('ko-KR').format(value)} ${currency === 'KRW' ? '원' : currency}`
const label = (code: string) => statusLabel[code] ?? code
const Empty = ({ children }: { children: ReactNode }) => <div className="customerEmpty">{children}</div>

export function AdminCustomersPage({ pathname, customerId }: { pathname: string; customerId?: string }) {
  return <AdminLayout pathname={pathname}>{customerId ? <CustomerDetail customerId={customerId} /> : <CustomerList />}</AdminLayout>
}

function CustomerList() {
  const [result, setResult] = useState<AdminCustomerList | null>(null)
  const [searchInput, setSearchInput] = useState(''); const [search, setSearch] = useState('')
  const [status, setStatus] = useState(''); const [hasRequests, setHasRequests] = useState(''); const [hasTransactions, setHasTransactions] = useState('')
  const [joinedFrom, setJoinedFrom] = useState(''); const [joinedTo, setJoinedTo] = useState(''); const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true); const [error, setError] = useState<string | null>(null)
  useEffect(() => {
    setLoading(true); setError(null)
    searchAdminCustomers({ search, status, hasRequests, hasTransactions, joinedFrom, joinedTo, page, pageSize: 20 })
      .then(setResult).catch((reason: unknown) => setError(reason instanceof Error ? reason.message : '고객목록을 불러오지 못했습니다.')).finally(() => setLoading(false))
  }, [search, status, hasRequests, hasTransactions, joinedFrom, joinedTo, page])
  const submit = (event: FormEvent) => { event.preventDefault(); setPage(1); setSearch(searchInput.trim()) }
  const reset = () => { setSearchInput(''); setSearch(''); setStatus(''); setHasRequests(''); setHasTransactions(''); setJoinedFrom(''); setJoinedTo(''); setPage(1) }
  const lastPage = Math.max(1, Math.ceil((result?.totalCount ?? 0) / 20))
  return <>
    <section className="adminPageHeading"><div><p>고객 상담·운영</p><h1>고객 관리</h1></div><span>고객의 요청부터 거래와 A/S까지 업무 흐름을 한곳에서 확인합니다.</span><button onClick={() => navigate('/admin/customers/withdrawals')}>고객 탈퇴 Queue</button></section>
    <form className="customerFilters" onSubmit={submit}>
      <label className="customerSearch">고객 찾기<input value={searchInput} onChange={(event) => setSearchInput(event.target.value)} placeholder="이름, 휴대전화, 이메일, 고객번호" /></label>
      <label>회원상태<select value={status} onChange={(event) => { setStatus(event.target.value); setPage(1) }}><option value="">전체</option><option value="ACTIVE">정상</option><option value="SUSPENDED">정지</option><option value="WITHDRAWN">탈퇴</option></select></label>
      <label>요청 이용<select value={hasRequests} onChange={(event) => { setHasRequests(event.target.value); setPage(1) }}><option value="">전체</option><option value="true">요청 있음</option><option value="false">요청 없음</option></select></label>
      <label>거래 이용<select value={hasTransactions} onChange={(event) => { setHasTransactions(event.target.value); setPage(1) }}><option value="">전체</option><option value="true">거래 있음</option><option value="false">거래 없음</option></select></label>
      <label>가입 시작일<input type="date" value={joinedFrom} onChange={(event) => { setJoinedFrom(event.target.value); setPage(1) }} /></label>
      <label>가입 종료일<input type="date" value={joinedTo} onChange={(event) => { setJoinedTo(event.target.value); setPage(1) }} /></label>
      <div className="customerFilterActions"><button type="submit">검색</button><button type="button" onClick={reset}>초기화</button></div>
    </form>
    {error && <div className="adminError" role="alert">{error}</div>}
    <section className="customerListCard">
      <header><div><h2>고객 목록</h2><span>현재 조건 {result?.totalCount ?? 0}명</span></div><small>연락처는 목록에서 마스킹됩니다.</small></header>
      {loading ? <Empty>고객목록을 불러오는 중입니다.</Empty> : !result?.items.length ? <Empty>조건에 맞는 고객이 없습니다.</Empty> : <div className="customerTableWrap"><table><thead><tr><th>고객</th><th>연락처</th><th>상태</th><th>가입일</th><th>역할</th><th>요청</th><th>거래</th><th>최근 이용</th><th></th></tr></thead><tbody>{result.items.map((customer) => <tr key={customer.id}><td><strong>{customer.name}</strong></td><td><span>{customer.maskedPhone ?? '미등록'}</span><small>{customer.maskedEmail ?? '미등록'}</small></td><td><span className={`customerStatus status-${customer.statusCode.toLowerCase()}`}>{label(customer.statusCode)}</span></td><td>{date(customer.joinedAt)}</td><td><div className="customerRoles">{customer.roles.map((role) => <span key={role}>{roleLabel[role] ?? role}</span>)}</div></td><td>{customer.requestCount}건</td><td>{customer.transactionCount}건</td><td>{date(customer.lastUsedAt)}</td><td><button type="button" onClick={() => navigate(`/admin/customers/${customer.id}`)}>상세보기</button></td></tr>)}</tbody></table></div>}
      <footer className="customerPagination"><button type="button" disabled={page <= 1} onClick={() => setPage((current) => current - 1)}>이전</button><span>{page} / {lastPage}</span><button type="button" disabled={page >= lastPage} onClick={() => setPage((current) => current + 1)}>다음</button></footer>
    </section>
  </>
}

function CustomerDetail({ customerId }: { customerId: string }) {
  const [customer, setCustomer] = useState<AdminCustomerDetail | null>(null); const [tab, setTab] = useState<CustomerTab>('기본정보')
  const [error, setError] = useState<string | null>(null)
  const load = useCallback(() => getAdminCustomer(customerId).then(setCustomer), [customerId])
  useEffect(() => { load().catch((reason: unknown) => setError(reason instanceof Error ? reason.message : '고객 상세를 불러오지 못했습니다.')) }, [load])
  if (error) return <><button className="customerBack" type="button" onClick={() => navigate('/admin/customers')}>← 고객 목록</button><div className="adminError">{error}</div></>
  if (!customer) return <Empty>고객 상세정보를 불러오는 중입니다.</Empty>
  return <>
    <button className="customerBack" type="button" onClick={() => navigate('/admin/customers')}>← 고객 목록</button>
    <section className="customerDetailHero"><div><p>고객 360°</p><h1>{customer.basic.name}</h1><div className="customerRoles">{customer.basic.roles.map((role) => <span key={role}>{roleLabel[role] ?? role}</span>)}</div></div><dl><div><dt>회원상태</dt><dd>{label(customer.basic.statusCode)}</dd></div><div><dt>가입일</dt><dd>{date(customer.basic.joinedAt)}</dd></div><div><dt>최근 이용</dt><dd>{date(customer.usage.lastUsedAt)}</dd></div></dl></section>
    <section className="customerUsageCards"><article><span>전체 요청</span><strong>{customer.usage.totalRequestCount}</strong></article><article><span>진행 중 요청</span><strong>{customer.usage.inProgressRequestCount}</strong></article><article><span>완료 거래</span><strong>{customer.usage.completedTransactionCount}</strong></article><article><span>진행 중 A/S</span><strong>{customer.usage.inProgressAfterServiceCount}</strong></article></section>
    <nav className="customerTabs" aria-label="고객 상세 메뉴">{customerTabs.map((item) => <button type="button" key={item} className={tab === item ? 'active' : ''} onClick={() => setTab(item)}>{item}</button>)}</nav>
    <section className="customerTabPanel">{renderCustomerTab(customer, tab, load)}</section>
  </>
}

function renderCustomerTab(customer: AdminCustomerDetail, tab: CustomerTab, reload: () => Promise<void>) {
  if (tab === '기본정보') return <div className="customerInfoGrid"><Info label="이름" value={customer.basic.name} /><Info label="휴대전화" value={customer.basic.phone ?? '미등록'} /><Info label="이메일" value={customer.basic.email ?? '미등록'} /><Info label="가입일" value={date(customer.basic.joinedAt)} /><Info label="최근 로그인" value={date(customer.basic.lastLoginAt)} /><Info label="본인인증" value={customer.basic.identityVerificationStatus} /><Info label="계정상태" value={label(customer.basic.statusCode)} /><Info label="보유 역할" value={customer.basic.roles.map((role) => roleLabel[role] ?? role).join(', ')} /></div>
  if (tab === '주소') return customer.addresses.isSupported ? <div>{customer.addresses.items.map((item) => <article key={`${item.alias}-${item.registeredAt}`}>{item.alias} {item.address} {item.detailAddress}</article>)}</div> : <Empty>{customer.addresses.message}</Empty>
  if (tab === '요청') return customer.requests.length ? <FlowTable headers={['등록일','서비스·요청','상태','요청지역','견적','채택','제한 집계']} rows={customer.requests.map((item) => [date(item.createdAt), <><strong>{item.serviceName}</strong><small>{item.title}</small></>, label(item.statusCode), <>{item.areaName}<small>{item.detailAddress ?? '상세주소 없음'}</small></>, `${item.quoteCount}건`, item.hasAcceptedQuote ? '채택' : '미채택', <><strong>{item.abuseCountExcluded ? '집계 제외' : '정상 집계'}</strong>{item.abuseExclusionReason && <small>{item.abuseExclusionReason}</small>}<button type="button" onClick={async () => { const excluded = !item.abuseCountExcluded; const reason = await soodalPrompt(excluded ? '허위요청·시스템 오류 등 집계 제외 사유를 입력하세요.' : '집계 제외를 해제하는 사유를 입력하세요.'); if (!reason?.trim()) return; void setAdminCustomerRequestAbuseExclusion(customer.basic.id, item.id, excluded, reason.trim()).then(reload).catch((error: unknown) => void soodalAlert(error instanceof Error ? error.message : '변경하지 못했습니다.')) }}>{item.abuseCountExcluded ? '제외 해제' : '집계 제외'}</button></>])} /> : <Empty>등록된 요청이 없습니다.</Empty>
  if (tab === '견적·채택') return customer.quotes.length ? <FlowTable headers={['제출일','요청','전문가','금액','상태','채택일']} rows={customer.quotes.map((item) => [date(item.submittedAt), item.requestTitle, item.providerName, amount(item.totalAmount, item.currencyCode), label(item.statusCode), item.isAccepted ? date(item.acceptedAt) : '미채택'])} /> : <Empty>받은 견적이 없습니다.</Empty>
  if (tab === '거래') return customer.transactions.length ? <FlowTable headers={['서비스','전문가','상태','견적금액','실제금액','시작·완료']} rows={customer.transactions.map((item) => [item.serviceName, item.providerName, label(item.statusCode), amount(item.agreedAmount, item.currencyCode), amount(item.actualAmount, item.currencyCode), <>{date(item.startedAt)}<small>{date(item.completedAt)}</small></>])} /> : <Empty>거래 이력이 없습니다.</Empty>
  if (tab === '후기·A/S') return <div className="customerSplit"><section><h2>후기</h2><Empty>{customer.reviews.message}</Empty></section><section><h2>A/S</h2>{customer.afterServices.length ? customer.afterServices.map((item) => <article className="customerHistoryCard" key={item.id}><strong>{item.subject}</strong><span>{label(item.statusCode)} · {date(item.receivedAt)}</span><p>{item.processingResult ?? '처리결과 기록 없음'}</p></article>) : <Empty>접수된 A/S가 없습니다.</Empty>}</section></div>
  if (tab === '수리·서비스 이력') return customer.serviceHistory.length ? <div className="customerHistoryList">{customer.serviceHistory.map((item) => <article className="customerHistoryCard" key={item.id}><div><strong>{item.title}</strong><span>{label(item.eventTypeCode)} · {date(item.occurredAt)}</span></div><p>{item.summary}</p><dl><div><dt>서비스</dt><dd>{item.categoryName ?? '기록 없음'}</dd></div><div><dt>전문가</dt><dd>{item.providerName ?? '기록 없음'}</dd></div><div><dt>금액</dt><dd>{amount(item.totalAmount, item.currencyCode ?? 'KRW')}</dd></div><div><dt>보증 종료일</dt><dd>{item.warrantyEndDate ?? '기록 없음'}</dd></div></dl></article>)}</div> : <Empty>수리·서비스 이력이 없습니다.</Empty>
  if (tab === '동의·상태') return <div className="customerSplit"><section><h2>약관·동의</h2><Empty>{customer.consents.message}</Empty></section><section><h2>회원상태와 역할</h2><p className="customerNotice">현재 상태: <strong>{label(customer.status.currentStatusCode)}</strong></p>{customer.status.roleHistory.map((role) => <article className="customerRoleHistory" key={`${role.roleCode}-${role.grantedAt}`}><strong>{roleLabel[role.roleCode] ?? role.roleCode}</strong><span>{date(role.grantedAt)} 부여 · {role.revokedAt ? `${date(role.revokedAt)} 종료` : '현재 보유'}</span></article>)}<p className="customerWarning">{customer.status.withdrawalMessage}</p></section></div>
  return customer.managementHistory.length ? <FlowTable headers={['일시','작업','대상','결과','관리자 역할','사유']} rows={customer.managementHistory.map((item) => [date(item.occurredAt), item.actionCode, item.entityType, item.resultCode === 'SUCCESS' ? '성공' : '실패', item.actorRoleCode ? roleLabel[item.actorRoleCode] ?? item.actorRoleCode : '-', item.reason ?? '-'])} /> : <Empty>이 고객을 대상으로 기록된 관리이력이 없습니다. 개인정보 조회 자체는 현재 감사로그 대상이 아닙니다.</Empty>
}

function Info({ label: title, value }: { label: string; value: string }) { return <div><span>{title}</span><strong>{value}</strong></div> }
function FlowTable({ headers, rows }: { headers: string[]; rows: ReactNode[][] }) { return <div className="customerTableWrap"><table><thead><tr>{headers.map((header) => <th key={header}>{header}</th>)}</tr></thead><tbody>{rows.map((row, index) => <tr key={index}>{row.map((cell, cellIndex) => <td key={cellIndex}>{cell}</td>)}</tr>)}</tbody></table></div> }
