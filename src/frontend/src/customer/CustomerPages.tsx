import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { useAuthentication } from '../auth/AuthenticationContext'
import { createLoginPath, navigate } from '../auth/routing'
import { serviceCompany } from '../config/serviceCompany'
import { CustomerAppLayout } from './CustomerAppLayout'
import { publicCatalogApi } from './api'
import type { PublicCategory, PublicContent, PublicPromotion, PublicServiceDetail, PublicServiceSummary } from './types'

function money(value: number, currency = 'KRW') {
  return currency === 'KRW' ? `${value.toLocaleString('ko-KR')}원` : `${value.toLocaleString('ko-KR')} ${currency}`
}

function Price({ value }: { value: PublicServiceSummary['price'] }) {
  if (!value) return <span className="servicePrice isPending">가격 안내 준비 중</span>
  const range = value.recommendedMinAmount !== null && value.recommendedMaxAmount !== null
    ? `${money(value.recommendedMinAmount, value.currency)} ~ ${money(value.recommendedMaxAmount, value.currency)}`
    : value.referenceAmount !== null ? money(value.referenceAmount, value.currency) : null
  return <span className="servicePrice"><b>{value.priceDisplayLabel}</b>{range && <> · {range}</>}</span>
}

function ServiceCard({ service }: { service: PublicServiceSummary }) {
  return <button className="serviceCard" type="button" onClick={() => navigate(`/services/${service.id}`)}><span>{service.majorName} · {service.middleName}</span><strong>{service.name}</strong><Price value={service.price} /><small>{service.description ?? '서비스 상세에서 이용 조건과 요청 항목을 확인해 주세요.'}</small></button>
}

function SearchBox({ initial = '' }: { initial?: string }) {
  const [query, setQuery] = useState(initial)
  const submit = (event: FormEvent) => { event.preventDefault(); if (query.trim()) navigate(`/services/search?q=${encodeURIComponent(query.trim())}`) }
  return <form className="customerSearchBox" role="search" onSubmit={submit}><label className="srOnly" htmlFor="service-search">필요한 서비스 검색</label><input id="service-search" value={query} onChange={event => setQuery(event.target.value)} placeholder="어떤 도움이 필요하세요? 예: 수도꼭지 교체" /><button type="submit">서비스 찾기</button></form>
}

export function CustomerHomePage() {
  const { user } = useAuthentication()
  const customer = user?.roles.includes('CUSTOMER') ?? false
  const [majors, setMajors] = useState<PublicCategory[]>([])
  const [services, setServices] = useState<PublicServiceSummary[]>([])
  const [notices, setNotices] = useState<PublicContent[]>([])
  const [promotions, setPromotions] = useState<PublicPromotion[]>([])
  const [error, setError] = useState('')
  useEffect(() => {
    Promise.all([publicCatalogApi.majors(), publicCatalogApi.services(8), publicCatalogApi.contents('NOTICE'), publicCatalogApi.promotions()])
      .then(([majorItems, serviceItems, noticeItems, promotionItems]) => { setMajors(majorItems); setServices(serviceItems); setNotices(noticeItems.slice(0, 3)); setPromotions(promotionItems.slice(0, 2)) })
      .catch(() => setError('서비스 정보를 잠시 불러오지 못했습니다. 다시 시도해 주세요.'))
  }, [])
  const protectedPath = (path: string) => navigate(customer ? path : createLoginPath(path))
  return <CustomerAppLayout><section className="customerHero"><div><p>생활서비스를 더 쉽고 안심되게</p><h1>생활의 불편을<br />수달 라이프가 해결해 드려요</h1><span>필요한 서비스를 찾고, 여러 견적과 조건을 확인해 보세요.</span><SearchBox /></div><aside aria-label="서비스 이용 순서"><strong>간단한 이용 흐름</strong><ol><li>서비스 찾기</li><li>요청 작성</li><li>견적 비교</li><li>공급자 선택</li></ol></aside></section>
    {customer && <section className="signedInWelcome"><div><p>고객으로 로그인했습니다</p><h2>필요한 업무를 이어서 확인하세요.</h2><span>집계 API가 연결되기 전까지 가짜 진행 건수는 표시하지 않습니다.</span></div><div><button onClick={() => navigate('/customer/requests')}>내 요청</button><button onClick={() => navigate('/customer/transactions')}>진행 거래</button></div></section>}
    {error && <p className="customerPageError" role="alert">{error}</p>}
    <section className="customerSection"><header><div><p>서비스 카테고리</p><h2>어떤 도움이 필요하세요?</h2></div><button onClick={() => navigate('/services')}>전체 서비스 보기</button></header><div className="majorGrid">{majors.map(major => <button key={major.id} onClick={() => navigate(`/services?major=${major.id}`)}><strong>{major.name}</strong><span>{major.childCount}개 분야</span></button>)}</div></section>
    <section className="customerSection sectionTint"><header><div><p>대표 서비스</p><h2>생활 속 필요한 서비스를 찾아보세요</h2><span>인기 통계가 아닌 카테고리 표시순서 기준입니다.</span></div></header><div className="serviceGrid">{services.map(service => <ServiceCard service={service} key={service.id} />)}</div></section>
    <section className="customerSection entryGrid"><article className="careEntry"><p>SOODAL CARE</p><h2>정기적으로 이용하는 생활서비스</h2><span>청소·점검 등 반복해서 필요한 서비스를 살펴보세요.</span><button onClick={() => navigate('/care')}>수달 케어 서비스 보기</button></article><article className="interiorEntry"><p>SOODAL INTERIOR</p><h2>실측부터 공사 완료까지</h2><span>실측·견적·계약·공정·하자관리까지 한 프로젝트로 확인하세요.</span><button onClick={() => navigate('/interior')}>수달 인테리어 시작</button></article></section>
    {(promotions.length > 0 || notices.length > 0) && <section className="customerSection newsGrid">{promotions.length > 0 && <div><p className="sectionEyebrow">프로모션</p>{promotions.map(item => <article className="noticeCard" key={item.creativeId}><strong>{item.title}</strong><span>{item.subtitle ?? item.bodyText}</span></article>)}</div>}<div><p className="sectionEyebrow">공지·안전 안내</p>{notices.length ? notices.map(item => <button className="noticeCard" key={item.id} onClick={() => navigate('/notices')}><strong>{item.title}</strong><span>{item.bodyText}</span></button>) : <p className="customerEmptyText">현재 게시된 공지가 없습니다.</p>}</div></section>}
    <section className="customerCta"><div><p>원하는 서비스를 찾으셨나요?</p><h2>{customer ? '원하는 서비스를 선택하고 견적 요청을 시작해 보세요.' : '로그인하고 견적 요청을 시작해 보세요.'}</h2></div><button onClick={() => protectedPath('/customer/requests/new')}>견적 요청하기</button></section>
  </CustomerAppLayout>
}

export function ServiceCatalogPage() {
  const params = new URLSearchParams(window.location.search)
  const [majors, setMajors] = useState<PublicCategory[]>([])
  const [middles, setMiddles] = useState<PublicCategory[]>([])
  const [services, setServices] = useState<PublicServiceSummary[]>([])
  const [majorId, setMajorId] = useState(params.get('major') ?? '')
  const [middleId, setMiddleId] = useState('')
  const [error, setError] = useState('')
  useEffect(() => { publicCatalogApi.majors().then(items => { setMajors(items); setMajorId(current => current || items[0]?.id || '') }).catch(() => setError('카테고리를 불러오지 못했습니다.')) }, [])
  useEffect(() => { if (!majorId) return; publicCatalogApi.children(majorId).then(items => { setMiddles(items); setMiddleId(items[0]?.id ?? '') }).catch(() => setError('중분류를 불러오지 못했습니다.')) }, [majorId])
  useEffect(() => { if (!middleId) { setServices([]); return }; publicCatalogApi.servicesByMiddle(middleId).then(setServices).catch(() => setError('서비스를 불러오지 못했습니다.')) }, [middleId])
  return <CustomerAppLayout><section className="pageHeading"><p>전체 서비스</p><h1>필요한 생활서비스를 찾아보세요</h1><SearchBox /></section>{error && <p className="customerPageError">{error}</p>}<div className="catalogExplorer"><section aria-label="대분류"><h2>대분류</h2>{majors.map(item => <button className={majorId === item.id ? 'isActive' : ''} onClick={() => setMajorId(item.id)} key={item.id}>{item.name}<span>{item.childCount}</span></button>)}</section><section aria-label="중분류"><h2>중분류</h2>{middles.map(item => <button className={middleId === item.id ? 'isActive' : ''} onClick={() => setMiddleId(item.id)} key={item.id}>{item.name}<span>{item.childCount}</span></button>)}</section><section className="catalogServices" aria-label="하위 서비스"><h2>서비스</h2>{services.length ? services.map(service => <ServiceCard service={service} key={service.id} />) : <p className="customerEmptyText">표시할 서비스가 없습니다.</p>}</section></div></CustomerAppLayout>
}

export function ServiceSearchPage() {
  const query = new URLSearchParams(window.location.search).get('q')?.trim() ?? ''
  const [items, setItems] = useState<PublicServiceSummary[]>([])
  const [loading, setLoading] = useState(Boolean(query))
  const [error, setError] = useState('')
  useEffect(() => { if (!query) { setItems([]); setLoading(false); return }; setLoading(true); publicCatalogApi.search(query).then(setItems).catch(() => setError('검색 결과를 불러오지 못했습니다.')).finally(() => setLoading(false)) }, [query])
  return <CustomerAppLayout><section className="pageHeading"><p>서비스 검색</p><h1>{query ? `“${query}” 검색 결과` : '어떤 서비스가 필요하세요?'}</h1><SearchBox initial={query} /></section><section className="customerSection searchResults">{error && <p className="customerPageError">{error}</p>}{loading ? <p className="customerEmptyText">검색 중입니다…</p> : items.length ? <><p>{items.length}개의 서비스를 찾았습니다.</p><div className="serviceGrid">{items.map(item => <ServiceCard service={item} key={item.id} />)}</div></> : <p className="customerEmptyText">{query ? '일치하는 서비스가 없습니다. 다른 표현으로 검색해 주세요.' : '서비스명이나 카테고리명을 입력해 주세요.'}</p>}</section></CustomerAppLayout>
}

export function ServiceDetailPage({ id }: { id: string }) {
  const { user } = useAuthentication()
  const [value, setValue] = useState<PublicServiceDetail | null>(null)
  const [error, setError] = useState('')
  useEffect(() => { publicCatalogApi.detail(id).then(setValue).catch(() => setError('서비스 상세를 찾을 수 없습니다.')) }, [id])
  const request = () => navigate(user?.roles.includes('CUSTOMER') ? `/customer/requests/new?service=${id}` : createLoginPath(`/customer/requests/new?service=${id}`))
  if (!value) return <CustomerAppLayout><section className="pageHeading"><h1>{error || '서비스 정보를 불러오는 중입니다…'}</h1></section></CustomerAppLayout>
  return <CustomerAppLayout><article className="serviceDetail"><nav aria-label="현재 위치"><button onClick={() => navigate(`/services?major=${value.majorId}`)}>{value.majorName}</button><span>›</span><button onClick={() => navigate(`/services?major=${value.majorId}`)}>{value.middleName}</button></nav><header><div><p>{value.code ?? '생활서비스'}</p><h1>{value.name}</h1><span>{value.description ?? '서비스 범위와 현장 조건을 확인한 뒤 견적을 안내합니다.'}</span></div><button className="detailRequestButton" onClick={request}>견적 요청하기</button></header><div className="detailColumns"><div><section><h2>가격 안내</h2>{value.price ? <><div className="pricePrimary"><span>{value.price.priceDisplayLabel}</span><strong>{value.price.referenceAmount !== null ? money(value.price.referenceAmount, value.price.currency) : '상담 후 안내'}</strong></div><p className="standardPriceNotice">표시된 금액은 전국 공통 기준의 참고용 표준가격입니다. 실제 비용은 작업 범위와 현장 조건을 확인한 뒤 공급자와 견적 상담 후 결정하면 됩니다.</p>{value.price.workUnit && <p>작업 단위: {value.price.workUnit}</p>}<p>VAT 표시: {value.price.vatDisplayText}</p><small>{value.price.guidanceText}</small></> : <p>가격 안내를 준비하고 있습니다.</p>}</section><section><h2>이용 안내</h2><dl className="serviceFacts"><div><dt>현장방문</dt><dd>{value.onsiteRequirement}</dd></div><div><dt>긴급 요청</dt><dd>{value.emergencyRequestAllowed ? '가능' : '지원하지 않음'}</dd></div><div><dt>정기구독</dt><dd>{value.subscriptionAvailable ? '이용 가능' : '일회성 서비스'}</dd></div><div><dt>기본 A/S</dt><dd>{value.defaultWarrantyDays > 0 ? `${value.defaultWarrantyDays}일` : '견적 조건에서 확인'}</dd></div></dl></section></div><aside><h2>요청 전 확인할 항목</h2><p>{value.requestGuide}</p><ul>{value.requestFields.map(field => <li key={field.id}><strong>{field.label}</strong>{field.required && <span>필수</span>}{field.unit && <small>{field.unit}</small>}</li>)}</ul>{value.providerRequirementGuide && <div className="providerGuide"><strong>공급자 확인 안내</strong><p>{value.providerRequirementGuide}</p></div>}</aside></div></article></CustomerAppLayout>
}

export function PublicContentPage({ type, id }: { type: 'NOTICE' | 'FAQ'; id?: string }) {
  const [items, setItems] = useState<PublicContent[]>([])
  const [query, setQuery] = useState('')
  const [error, setError] = useState('')
  useEffect(() => { publicCatalogApi.contents(type).then(setItems).catch(() => setError('게시물을 불러오지 못했습니다.')) }, [type])
  const title = type === 'NOTICE' ? '공지사항' : '자주 묻는 질문'
  const selected = id ? items.find(item => item.id === id) : null
  const filtered = items.filter(item => `${item.title} ${item.questionText ?? ''} ${item.bodyText ?? ''} ${item.answerText ?? ''}`.toLowerCase().includes(query.trim().toLowerCase()))
  if (id) return <CustomerAppLayout><section className="pageHeading"><p>고객지원 · {title}</p><h1>{selected?.questionText ?? selected?.title ?? '게시물을 찾을 수 없습니다.'}</h1></section><article className="publicContentDetail">{selected ? <><span>게시일 {new Intl.DateTimeFormat('ko-KR', { dateStyle: 'long' }).format(new Date(selected.publishedAt))} · 대상 {selected.audienceTypeCode === 'ALL' ? '전체 사용자' : '고객'} · 버전 {selected.versionNo}</span><p>{selected.answerText ?? selected.bodyText}</p><button onClick={() => navigate(type === 'NOTICE' ? '/notices' : '/faq')}>목록으로</button></> : <p className="customerEmptyText">현재 공개된 게시물이 아닙니다.</p>}</article></CustomerAppLayout>
  return <CustomerAppLayout><section className="pageHeading"><p>고객지원</p><h1>{title}</h1><label className="contentSearch">게시물 검색<input value={query} onChange={event => setQuery(event.target.value)} placeholder="제목과 내용에서 검색" /></label></section><section className="contentListPage">{error && <p className="customerPageError">{error}</p>}{filtered.length ? filtered.map(item => type === 'FAQ' ? <details key={item.id}><summary>{item.questionText ?? item.title}</summary><p>{item.answerText ?? item.bodyText}</p><small>게시일 {new Intl.DateTimeFormat('ko-KR').format(new Date(item.publishedAt))}</small></details> : <button className="publicNoticeRow" key={item.id} onClick={() => navigate(`/notices/${item.id}`)}><span>{new Intl.DateTimeFormat('ko-KR').format(new Date(item.publishedAt))}</span><strong>{item.title}</strong><small>상세 보기</small></button>) : <p className="customerEmptyText">현재 게시된 {title}이 없습니다.</p>}</section></CustomerAppLayout>
}

export function CustomerSupportPage() {
  const hours = useMemo(() => serviceCompany.customerServiceHours?.join(' · ') ?? '운영시간 확정 전', [])
  return <CustomerAppLayout><section className="pageHeading"><p>고객지원</p><h1>궁금한 점을 확인해 보세요</h1></section><section className="supportGrid"><button onClick={() => navigate('/notices')}><strong>공지사항</strong><span>서비스 운영 소식을 확인합니다.</span></button><button onClick={() => navigate('/faq')}><strong>FAQ</strong><span>자주 묻는 질문을 확인합니다.</span></button><article><strong>전국 대표번호</strong>{serviceCompany.representativePhone ? <a href={`tel:${serviceCompany.representativePhone.replaceAll('-', '')}`}>{serviceCompany.representativePhone}</a> : <b>확정 전</b>}<span>{hours}</span></article><article><strong>이메일 문의</strong>{serviceCompany.customerServiceEmail ? <a href={`mailto:${serviceCompany.customerServiceEmail}`}>{serviceCompany.customerServiceEmail}</a> : <b>확정 전</b>}<span>서비스 이용 문의를 이메일로 접수합니다.</span></article></section></CustomerAppLayout>
}

export function CompanyInfoPage() {
  return <CustomerAppLayout><section className="pageHeading companyHeading"><p>COMPANY</p><h1>기업의 내일을 기술로 연결합니다</h1><span>AI와 데이터, 현장을 이해하는 기술로 고객의 디지털 전환을 설계하고 실행합니다.</span></section><section className="companyIntroduction"><article className="companyLead"><p>주식회사 디에이치는 기업용 소프트웨어와 AI 솔루션, 스마트팩토리, IT 컨설팅을 기획부터 구축·운영까지 연결하는 기술 기업입니다.</p><a href="https://www.dh9.kr" target="_blank" rel="noopener noreferrer">디에이치 공식 홈페이지</a></article><div className="companyValues"><article><span>01</span><h2>고객 중심</h2><p>현장의 문제와 목표를 먼저 이해하고 실제 업무에 도움이 되는 결과를 만듭니다.</p></article><article><span>02</span><h2>실용적 혁신</h2><p>기술 자체보다 사용성과 운영 효과를 기준으로 지속 가능한 해법을 설계합니다.</p></article><article><span>03</span><h2>신뢰의 파트너십</h2><p>구축 이후의 안정적인 운영과 개선까지 함께하는 장기 파트너를 지향합니다.</p></article></div><section className="companyBusiness"><header><p>WHAT WE DO</p><h2>사업 영역</h2></header><div><article><h3>AI 솔루션</h3><p>AI 챗봇, RAG 기반 지식검색과 데이터 분석으로 업무 활용도를 높입니다.</p></article><article><h3>스마트팩토리</h3><p>MES·POP·IoT와 설비 연계, 실시간 모니터링으로 제조 현장을 연결합니다.</p></article><article><h3>기업용 소프트웨어</h3><p>B2B SaaS, 업무 시스템과 클라우드 전환을 통해 디지털 업무 기반을 구축합니다.</p></article><article><h3>IT 컨설팅</h3><p>디지털 전환 전략, 프로세스 혁신과 운영 지원으로 실행 가능한 변화를 만듭니다.</p></article></div></section><section className="companyBrand"><div><p>OUR SERVICE</p><h2>수달 라이프</h2><span>고객과 검증된 생활서비스 공급자를 안전하게 연결하고 요청·견적·선택·진행·사후관리 기록을 한 흐름으로 관리하는 생활서비스 플랫폼입니다.</span></div><dl><div><dt>법인명</dt><dd>{serviceCompany.companyName}</dd></div><div><dt>대표자</dt><dd>{serviceCompany.ceoName}</dd></div><div><dt>사업자등록번호</dt><dd>{serviceCompany.businessRegistrationNumber}</dd></div><div><dt>주소</dt><dd>{serviceCompany.address}</dd></div></dl></section></section></CustomerAppLayout>
}

export function CustomerNotFoundPage() {
  return <CustomerAppLayout><section className="pageHeading"><p>404</p><h1>요청하신 화면을 찾을 수 없습니다.</h1><button className="detailRequestButton" onClick={() => navigate('/')}>고객 홈으로</button></section></CustomerAppLayout>
}
