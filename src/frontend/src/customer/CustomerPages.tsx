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
    <section className="customerSection entryGrid"><article className="careEntry"><p>SOODAL CARE</p><h2>정기적으로 이용하는 생활서비스</h2><span>청소·점검 등 반복해서 필요한 서비스를 살펴보세요.</span><button onClick={() => navigate('/services/search?q=정기구독')}>수달 케어 서비스 보기</button></article><article className="interiorEntry"><p>SOODAL INTERIOR</p><h2>실측부터 공사 완료까지</h2><span>상담과 프로젝트 관리가 필요한 인테리어 서비스를 살펴보세요.</span><button onClick={() => navigate('/services/search?q=인테리어')}>인테리어 서비스 보기</button></article></section>
    {(promotions.length > 0 || notices.length > 0) && <section className="customerSection newsGrid">{promotions.length > 0 && <div><p className="sectionEyebrow">프로모션</p>{promotions.map(item => <article className="noticeCard" key={item.creativeId}><strong>{item.title}</strong><span>{item.subtitle ?? item.bodyText}</span></article>)}</div>}<div><p className="sectionEyebrow">공지·안전 안내</p>{notices.length ? notices.map(item => <button className="noticeCard" key={item.id} onClick={() => navigate('/notices')}><strong>{item.title}</strong><span>{item.bodyText}</span></button>) : <p className="customerEmptyText">현재 게시된 공지가 없습니다.</p>}</div></section>}
    <section className="customerCta"><div><p>원하는 서비스를 찾으셨나요?</p><h2>로그인하고 견적 요청을 시작해 보세요.</h2></div><button onClick={() => protectedPath('/customer/requests/new')}>견적 요청하기</button></section>
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
  return <CustomerAppLayout><article className="serviceDetail"><nav aria-label="현재 위치"><button onClick={() => navigate(`/services?major=${value.majorId}`)}>{value.majorName}</button><span>›</span><button onClick={() => navigate(`/services?major=${value.majorId}`)}>{value.middleName}</button></nav><header><div><p>{value.code ?? '생활서비스'}</p><h1>{value.name}</h1><span>{value.description ?? '서비스 범위와 현장 조건을 확인한 뒤 견적을 안내합니다.'}</span></div><button className="detailRequestButton" onClick={request}>견적 요청하기</button></header><div className="detailColumns"><div><section><h2>가격 안내</h2>{value.price ? <><div className="pricePrimary"><span>{value.price.priceDisplayLabel}</span><strong>{value.price.referenceAmount !== null ? money(value.price.referenceAmount, value.price.currency) : '상담 후 안내'}</strong></div>{value.price.workUnit && <p>작업 단위: {value.price.workUnit}</p>}<p>VAT 표시: {value.price.vatDisplayText}</p><small>{value.price.guidanceText}</small></> : <p>가격 안내를 준비하고 있습니다.</p>}</section><section><h2>이용 안내</h2><dl className="serviceFacts"><div><dt>현장방문</dt><dd>{value.onsiteRequirement}</dd></div><div><dt>긴급 요청</dt><dd>{value.emergencyRequestAllowed ? '가능' : '지원하지 않음'}</dd></div><div><dt>정기구독</dt><dd>{value.subscriptionAvailable ? '이용 가능' : '일회성 서비스'}</dd></div><div><dt>기본 A/S</dt><dd>{value.defaultWarrantyDays > 0 ? `${value.defaultWarrantyDays}일` : '견적 조건에서 확인'}</dd></div></dl></section></div><aside><h2>요청 전 확인할 항목</h2><p>{value.requestGuide}</p><ul>{value.requestFields.map(field => <li key={field.id}><strong>{field.label}</strong>{field.required && <span>필수</span>}{field.unit && <small>{field.unit}</small>}</li>)}</ul>{value.providerRequirementGuide && <div className="providerGuide"><strong>공급자 확인 안내</strong><p>{value.providerRequirementGuide}</p></div>}</aside></div></article></CustomerAppLayout>
}

export function PublicContentPage({ type }: { type: 'NOTICE' | 'FAQ' }) {
  const [items, setItems] = useState<PublicContent[]>([])
  const [error, setError] = useState('')
  useEffect(() => { publicCatalogApi.contents(type).then(setItems).catch(() => setError('게시물을 불러오지 못했습니다.')) }, [type])
  const title = type === 'NOTICE' ? '공지사항' : '자주 묻는 질문'
  return <CustomerAppLayout><section className="pageHeading"><p>고객지원</p><h1>{title}</h1></section><section className="contentListPage">{error && <p className="customerPageError">{error}</p>}{items.length ? items.map(item => <details key={item.id} open={type === 'NOTICE'}><summary>{item.questionText ?? item.title}</summary><p>{item.answerText ?? item.bodyText}</p></details>) : <p className="customerEmptyText">현재 게시된 {title}이 없습니다.</p>}</section></CustomerAppLayout>
}

export function CustomerSupportPage() {
  const hours = useMemo(() => serviceCompany.customerServiceHours.join(' · '), [])
  return <CustomerAppLayout><section className="pageHeading"><p>고객지원</p><h1>궁금한 점을 확인해 보세요</h1></section><section className="supportGrid"><button onClick={() => navigate('/notices')}><strong>공지사항</strong><span>서비스 운영 소식을 확인합니다.</span></button><button onClick={() => navigate('/faq')}><strong>FAQ</strong><span>자주 묻는 질문을 확인합니다.</span></button><article><strong>전국 대표번호</strong><a href={`tel:${serviceCompany.representativePhone.replaceAll('-', '')}`}>{serviceCompany.representativePhone}</a><span>{hours}</span></article><article><strong>이메일 문의</strong><a href={`mailto:${serviceCompany.customerServiceEmail}`}>{serviceCompany.customerServiceEmail}</a><span>{serviceCompany.isPlaceholder ? '현재 개발용 임시 연락처입니다.' : '고객지원 이메일'}</span></article></section></CustomerAppLayout>
}

export function CustomerNotFoundPage() {
  return <CustomerAppLayout><section className="pageHeading"><p>404</p><h1>요청하신 화면을 찾을 수 없습니다.</h1><button className="detailRequestButton" onClick={() => navigate('/')}>고객 홈으로</button></section></CustomerAppLayout>
}
