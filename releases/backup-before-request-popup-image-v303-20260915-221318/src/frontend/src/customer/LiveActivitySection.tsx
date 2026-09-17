import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { navigate } from '../auth/routing'
import { publicCatalogApi } from './api'
import type { PublicActivityFeed, PublicActivityItem, PublicArea, PublicPromotion } from './types'
import { ServiceThumbnail } from '../serviceVisuals/ServiceVisual'

const LIVE_FEED_PLACEMENT = 'CUSTOMER_LIVE_ACTIVITY_FEED'
const AD_INSERT_AFTER = [4, 10, 16]
const FEED_CACHE_KEY = 'soodal-public-activity:v289'
const publicRequestLabels = ['평수', '환경', '희망금액', '현장조건', '수량'] as const
type FeedPromotion = PublicPromotion & { feedAreaId?: string }

function PublicRequestFacts({ item }: { item: PublicActivityItem }) {
  return <>{publicRequestLabels.map(label => <div key={label}><dt>{label}</dt><dd>{item.requestDetails?.find(value => value.label === label)?.value || '입력되지 않음'}</dd></div>)}</>
}

function cachedFeed(){try{return JSON.parse(sessionStorage.getItem(FEED_CACHE_KEY)??'null') as PublicActivityFeed|null}catch{return null}}

export function LiveActivitySection({ signedIn }: { signedIn: boolean }) {
  const [feed, setFeed] = useState<PublicActivityFeed | null>(()=>cachedFeed())
  const [selected, setSelected] = useState<PublicActivityItem | null>(null)
  const [region, setRegion] = useState('')
  const [eventType, setEventType] = useState('')
  const [newCount, setNewCount] = useState(0)
  const [error, setError] = useState('')
  const [areas, setAreas] = useState<PublicArea[]>([])
  const [promotions, setPromotions] = useState<FeedPromotion[]>([])
  const [nearViewport, setNearViewport] = useState(false)
  const sectionRef = useRef<HTMLElement | null>(null)
  const knownIds = useRef<Set<string>>(new Set())
  const recordedImpressions = useRef<Set<string>>(new Set())

  useEffect(() => {
    const section = sectionRef.current
    if (!section) return
    const observer = new IntersectionObserver(entries => {
      if (entries.some(entry => entry.isIntersecting)) { setNearViewport(true); observer.disconnect() }
    }, { rootMargin: '500px 0px' })
    observer.observe(section)
    return () => observer.disconnect()
  }, [])

  useEffect(() => {
    let active = true
    let timer: number | undefined
    let loading = false
    const schedule = (seconds: number) => { if (active && document.visibilityState === 'visible') timer = window.setTimeout(load, seconds * 1000) }
    const load = () => {
      if (!active || loading || document.visibilityState !== 'visible') return
      loading = true
      void publicCatalogApi.activity(undefined, undefined, 30).then(value => {
      if (!active) return
      const newlyArrived = value.items.filter(item => knownIds.current.size > 0 && !knownIds.current.has(item.id)).length
      if (newlyArrived) setNewCount(count => count + newlyArrived)
      knownIds.current = new Set(value.items.map(item => item.id))
      setFeed(value)
      try{sessionStorage.setItem(FEED_CACHE_KEY,JSON.stringify(value))}catch{/* storage unavailable */}
      setError('')
      schedule(Math.max(15, value.nextRefreshSeconds))
    }).catch(() => {
      if (!active) return
      setError('실시간 거래 현황을 잠시 불러오지 못했습니다.')
      schedule(60)
    }).finally(() => { loading = false })
    }
    const visibilityChanged = () => { if (timer) window.clearTimeout(timer); if (document.visibilityState === 'visible') load() }
    document.addEventListener('visibilitychange', visibilityChanged)
    load()
    return () => { active = false; if (timer) window.clearTimeout(timer); document.removeEventListener('visibilitychange', visibilityChanged) }
  }, [])

  useEffect(() => {
    if (nearViewport) publicCatalogApi.sidos().then(setAreas).catch(() => setAreas([]))
  }, [nearViewport])

  const regions = useMemo(() => Array.from(new Set(feed?.items.map(item => item.regionName.split(' ')[0]) ?? [])).sort(), [feed])
  const items = useMemo(() => feed?.items.filter(item => (!region || item.regionName.startsWith(region)) && (!eventType || item.eventTypeCode === eventType)) ?? [], [feed, region, eventType])
  const selectedAreaId = useMemo(() => areas.find(item => item.name === region)?.id, [areas, region])
  const promotionAreaIds = useMemo(() => {
    if (region) return selectedAreaId ? [selectedAreaId] : []
    const provinceNames = Array.from(new Set(items.map(item => item.regionName.split(' ')[0])))
    return provinceNames.map(name => areas.find(area => area.name === name)?.id).filter((id): id is string => Boolean(id)).slice(0, 3)
  }, [areas, items, region, selectedAreaId])

  useEffect(() => {
    let active = true
    if (!nearViewport) return () => { active = false }
    if (region && !selectedAreaId) { setPromotions([]); return () => { active = false } }
    const targetAreaIds: Array<string | undefined> = promotionAreaIds.length ? promotionAreaIds : [undefined]
    Promise.all(targetAreaIds.map(areaId => publicCatalogApi.promotions(undefined, areaId, LIVE_FEED_PLACEMENT).then(value => value.map(item => ({ ...item, feedAreaId: areaId })))))
      .then(value => { if (active) setPromotions(uniqueProviders(value.flat()).slice(0, 3)) })
      .catch(() => { if (active) setPromotions([]) })
    return () => { active = false }
  }, [nearViewport, promotionAreaIds, region, selectedAreaId])

  const rows = useMemo(() => interleaveRows(items, promotions), [items, promotions])
  const recordImpression = useCallback((promotion: FeedPromotion) => {
    if (recordedImpressions.current.has(promotion.creativeId)) return
    recordedImpressions.current.add(promotion.creativeId)
    void publicCatalogApi.advertisingEvent(promotion.creativeId, 'IMPRESSION', undefined, promotion.feedAreaId, LIVE_FEED_PLACEMENT).catch(() => undefined)
  }, [])
  const openPromotion = useCallback((promotion: FeedPromotion) => {
    void publicCatalogApi.advertisingEvent(promotion.creativeId, 'CLICK', undefined, promotion.feedAreaId, LIVE_FEED_PLACEMENT).catch(() => undefined)
    if (promotion.destinationTypeCode === 'EXTERNAL_URL' && promotion.destinationValue) { window.open(promotion.destinationValue, '_blank', 'noopener,noreferrer'); return }
    if (promotion.destinationTypeCode === 'INTERNAL_PATH' && promotion.destinationValue) navigate(promotion.destinationValue)
  }, [])

  return <section ref={sectionRef} id="live-activity" className="customerSection liveActivitySection customerHomePerformanceV172"><header><div><p>수달 실시간</p><h2>전국에서 지금 진행 중인 서비스</h2><span>{signedIn ? '내 관심 서비스와 비교하며 실제 거래 흐름을 확인해 보세요.' : '로그인 전에도 개인정보를 제외한 실제 거래 흐름을 확인할 수 있습니다.'}</span></div><i><b aria-hidden="true" />실제 거래 기반 · 광고 별도 표시</i></header>
    <div className="liveActivityToolbar"><label>지역<select value={region} onChange={event => setRegion(event.target.value)}><option value="">전국</option>{regions.map(item => <option key={item}>{item}</option>)}</select></label><label>진행 단계<select value={eventType} onChange={event => setEventType(event.target.value)}><option value="">전체</option><option value="REQUEST_OPENED">견적 요청</option><option value="QUOTE_RECEIVED">견적 도착</option><option value="PROVIDER_SELECTED">전문가 선택</option><option value="WORK_STARTED">서비스 진행</option><option value="WORK_COMPLETED">서비스 완료</option><option value="REVIEW_PUBLISHED">후기 등록</option></select></label>{newCount > 0 && <button type="button" onClick={() => { setNewCount(0); document.querySelector('.liveActivityList')?.scrollIntoView({ behavior: 'smooth', block: 'start' }) }}>새 거래 {newCount}건</button>}</div>
    {error && <p className="liveActivityError">{error}</p>}
    <div className="liveActivityList">{rows.map(row => row.kind === 'activity' ? <button type="button" key={row.item.id} onClick={() => setSelected(row.item)}><ServiceThumbnail code={row.item.serviceCode} name={row.item.serviceName} className={`liveActivityIcon step-${row.item.activeStep}`} /><span><small>{relativeTime(row.item.occurredAt)} · {row.item.regionName}</small><strong>{row.item.serviceName}</strong><em>{row.item.statusLabel}</em></span><b>요청·견적 보기</b></button> : <LiveActivityAdvertisement key={`ad-${row.item.creativeId}`} promotion={row.item} onVisible={recordImpression} onOpen={openPromotion} />)}{feed && items.length === 0 && <p>조건에 맞는 공개 가능한 최근 거래가 없습니다. 실제 거래가 확인되면 이곳에 표시됩니다.</p>}{!feed && !error && <p>{nearViewport?'실제 거래 현황을 확인하고 있습니다…':'화면에 가까워지면 실시간 현황을 불러옵니다…'}</p>}</div>
    {feed && <small className="liveActivityPrivacy">{feed.privacyNotice}</small>}
    {selected && <div className="liveActivityBackdrop" role="presentation" onMouseDown={event => { if (event.target === event.currentTarget) setSelected(null) }}><article className="liveActivityModal" role="dialog" aria-modal="true" aria-labelledby="live-activity-title"><header><div><small>{relativeTime(selected.occurredAt)} · {selected.regionName}</small><h3 id="live-activity-title">{selected.requestTitle || selected.serviceName}</h3><strong>{selected.serviceName}</strong></div><button type="button" aria-label="닫기" onClick={() => setSelected(null)}>×</button></header><section className="liveActivityRequestDetails"><h4>견적 요청 정보</h4><dl><div><dt>서비스</dt><dd>{selected.serviceName}</dd></div><div><dt>지역</dt><dd>{selected.regionName}</dd></div><div><dt>요청일</dt><dd>{selected.requestedAt ? formatDate(selected.requestedAt) : '확인 중'}</dd></div><PublicRequestFacts item={selected} /><div className="wide"><dt>요청 내용</dt><dd>{selected.requestSummary || '등록된 서비스 조건을 기준으로 전문가 견적을 받았습니다.'}</dd></div></dl></section><section className="liveActivityQuoteAmounts"><header><h4>전문가가 제출한 견적금액</h4><span>{selected.quoteAmounts.length}건</span></header>{selected.quoteAmounts.length > 0 ? <ol>{selected.quoteAmounts.map((quote, index) => <li key={`${quote.submittedAt}-${index}`}><span><b>견적 {index + 1}</b><time>{formatDate(quote.submittedAt)}</time></span><strong>{formatMoney(quote.amount, quote.currencyCode)}</strong></li>)}</ol> : <p>아직 공개할 수 있는 제출 견적이 없습니다.</p>}</section><p>고객·전문가의 개인정보와 상세주소, 전문가 업체명은 공개하지 않습니다.</p><footer><button type="button" onClick={() => navigate(`/customer/requests/new?service=${encodeURIComponent(selected.serviceId)}`)}>같은 서비스 요청하기</button></footer></article></div>}
  </section>
}

type LiveActivityRow = { kind: 'activity'; item: PublicActivityItem; activityIndex: number } | { kind: 'advertisement'; item: FeedPromotion }

function interleaveRows(items: PublicActivityItem[], promotions: FeedPromotion[]): LiveActivityRow[] {
  const rows: LiveActivityRow[] = []
  let promotionIndex = 0
  items.forEach((item, index) => {
    rows.push({ kind: 'activity', item, activityIndex: index })
    if (promotionIndex < promotions.length && AD_INSERT_AFTER.includes(index + 1)) rows.push({ kind: 'advertisement', item: promotions[promotionIndex++] })
  })
  return rows
}

function uniqueProviders(items: FeedPromotion[]) {
  const owners = new Set<string>()
  return items.filter(item => {
    const owner = item.providerId ?? item.campaignId
    if (owners.has(owner)) return false
    owners.add(owner)
    return true
  })
}

function LiveActivityAdvertisement({ promotion, onVisible, onOpen }: { promotion: FeedPromotion; onVisible: (item: FeedPromotion) => void; onOpen: (item: FeedPromotion) => void }) {
  const element = useRef<HTMLButtonElement | null>(null)
  useEffect(() => {
    const target = element.current
    if (!target) return
    let timer: number | undefined
    const observer = new IntersectionObserver(entries => {
      if (entries.some(entry => entry.isIntersecting && entry.intersectionRatio >= .5)) timer = window.setTimeout(() => onVisible(promotion), 1000)
      else if (timer) window.clearTimeout(timer)
    }, { threshold: [.5] })
    observer.observe(target)
    return () => { if (timer) window.clearTimeout(timer); observer.disconnect() }
  }, [promotion, onVisible])
  const canOpen = promotion.destinationTypeCode !== 'NONE' && Boolean(promotion.destinationValue)
  return <button ref={element} type="button" className="liveActivityAdvertisement" onClick={() => canOpen && onOpen(promotion)} disabled={!canOpen} aria-label={`광고: ${promotion.title}`}><span className="liveActivityAdBadge">광고</span><span><small>{promotion.subtitle ?? '수달 라이프 추천 전문가'}</small><strong>{promotion.title}</strong>{promotion.bodyText && <em>{promotion.bodyText}</em>}</span><b>{canOpen ? promotion.buttonText || '자세히 보기' : '광고'}</b></button>
}

function relativeTime(value: string) {
  const seconds = Math.max(0, Math.floor((Date.now() - new Date(value).getTime()) / 1000))
  if (seconds < 60) return '방금 전'
  if (seconds < 3600) return `${Math.floor(seconds / 60)}분 전`
  if (seconds < 86400) return `${Math.floor(seconds / 3600)}시간 전`
  if (seconds < 604800) return `${Math.floor(seconds / 86400)}일 전`
  return new Intl.DateTimeFormat('ko-KR', { dateStyle: 'medium' }).format(new Date(value))
}

const formatDate = (value: string) => new Intl.DateTimeFormat('ko-KR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
const formatMoney = (value: number, currencyCode: string) => new Intl.NumberFormat('ko-KR', { style: 'currency', currency: currencyCode || 'KRW', maximumFractionDigits: 0 }).format(value)
