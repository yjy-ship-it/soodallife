import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { navigate } from '../auth/routing'
import { publicCatalogApi } from './api'
import type { PublicActivityFeed, PublicActivityItem, PublicPromotion } from './types'
import { ServiceThumbnail } from '../serviceVisuals/ServiceVisual'
import { loadDefaultAddress } from './defaultAddress'

const LIVE_FEED_PLACEMENT = 'CUSTOMER_LIVE_ACTIVITY_FEED'
const AD_INSERT_AFTER = [4, 10, 16]
const FEED_CACHE_KEY = 'soodal-public-activity:v306'
const publicRequestLabels = ['평수', '환경', '희망금액', '현장조건', '수량'] as const
type FeedPromotion = PublicPromotion & { feedAreaId?: string; feedCategoryId: string; feedAfter: number }

function PublicRequestFacts({ item }: { item: PublicActivityItem }) {
  return <>{publicRequestLabels.map(label => <div key={label}><dt>{label}</dt><dd>{item.requestDetails?.find(value => value.label === label)?.value || '입력되지 않음'}</dd></div>)}</>
}

function cachedFeed(region: string) { try { return JSON.parse(sessionStorage.getItem(`${FEED_CACHE_KEY}:${region || 'ALL'}`) ?? 'null') as PublicActivityFeed | null } catch { return null } }

export function LiveActivitySection({ signedIn }: { signedIn: boolean }) {
  const [feed, setFeed] = useState<PublicActivityFeed | null>(null)
  const [selected, setSelected] = useState<PublicActivityItem | null>(null)
  const [feedRegion, setFeedRegion] = useState('')
  const [promotionAreaIds, setPromotionAreaIds] = useState<string[]>([])
  const [regionReady, setRegionReady] = useState(!signedIn)
  const [newCount, setNewCount] = useState(0)
  const [error, setError] = useState('')
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
    if (!signedIn) { setFeedRegion(''); setPromotionAreaIds([]); setRegionReady(true); return () => { active = false } }
    setRegionReady(false)
    void loadDefaultAddress().then(value => {
      if (!active) return
      setFeedRegion(value?.sidoLabel ?? '')
      setPromotionAreaIds(Array.from(new Set([value?.address.administrativeAreaId, value?.sidoId].filter((id): id is string => Boolean(id)))))
    }).catch(() => { if (active) { setFeedRegion(''); setPromotionAreaIds([]) } }).finally(() => { if (active) setRegionReady(true) })
    return () => { active = false }
  }, [signedIn])

  useEffect(() => {
    if (!regionReady) return
    let active = true
    let timer: number | undefined
    let loading = false
    const schedule = (seconds: number) => { if (active && document.visibilityState === 'visible') timer = window.setTimeout(load, seconds * 1000) }
    const load = () => {
      if (!active || loading || document.visibilityState !== 'visible') return
      loading = true
      void publicCatalogApi.activity(feedRegion || undefined, 'REQUEST_OPENED', 20).then(value => {
        if (!active) return
        const newlyArrived = value.items.filter(item => knownIds.current.size > 0 && !knownIds.current.has(item.id)).length
        if (newlyArrived) setNewCount(count => count + newlyArrived)
        knownIds.current = new Set(value.items.map(item => item.id))
        setFeed(value)
        try { sessionStorage.setItem(`${FEED_CACHE_KEY}:${feedRegion || 'ALL'}`, JSON.stringify(value)) } catch { /* storage unavailable */ }
        setError('')
        schedule(Math.max(30, value.nextRefreshSeconds))
      }).catch(() => {
        if (!active) return
        setError('최근 요청을 잠시 불러오지 못했습니다.')
        schedule(60)
      }).finally(() => { loading = false })
    }
    const visibilityChanged = () => { if (timer) window.clearTimeout(timer); if (document.visibilityState === 'visible') load() }
    const cached = cachedFeed(feedRegion); if (cached) setFeed(cached)
    document.addEventListener('visibilitychange', visibilityChanged)
    load()
    return () => { active = false; if (timer) window.clearTimeout(timer); document.removeEventListener('visibilitychange', visibilityChanged) }
  }, [feedRegion, regionReady])

  const items = useMemo(() => feed?.items ?? [], [feed])

  useEffect(() => {
    let active = true
    if (!nearViewport) return () => { active = false }
    const targetAreaIds: Array<string | undefined> = promotionAreaIds.length ? promotionAreaIds : [undefined]
    const targets = AD_INSERT_AFTER.map(feedAfter => ({ feedAfter, item: items[feedAfter - 1] })).filter((value): value is { feedAfter: number; item: PublicActivityItem } => Boolean(value.item))
    Promise.all(targets.map(async target => {
      const rows = (await Promise.all(targetAreaIds.map(areaId => publicCatalogApi.promotions(target.item.serviceId, areaId, LIVE_FEED_PLACEMENT).then(value => value.map(item => ({ ...item, feedAreaId: areaId, feedCategoryId: target.item.serviceId, feedAfter: target.feedAfter })))))).flat()
      return uniqueProviders(rows)[0]
    }))
      .then(value => { if (active) setPromotions(uniqueProviders(value.filter((item): item is FeedPromotion => Boolean(item)))) })
      .catch(() => { if (active) setPromotions([]) })
    return () => { active = false }
  }, [items, nearViewport, promotionAreaIds])

  const rows = useMemo(() => interleaveRows(items, promotions), [items, promotions])
  const recordImpression = useCallback((promotion: FeedPromotion) => {
    if (recordedImpressions.current.has(promotion.creativeId)) return
    recordedImpressions.current.add(promotion.creativeId)
    void publicCatalogApi.advertisingEvent(promotion.creativeId, 'IMPRESSION', promotion.feedCategoryId, promotion.feedAreaId, LIVE_FEED_PLACEMENT).catch(() => undefined)
  }, [])
  const openPromotion = useCallback((promotion: FeedPromotion) => {
    void publicCatalogApi.advertisingEvent(promotion.creativeId, 'CLICK', promotion.feedCategoryId, promotion.feedAreaId, LIVE_FEED_PLACEMENT).catch(() => undefined)
    if (promotion.destinationTypeCode === 'EXTERNAL_URL' && promotion.destinationValue) { window.open(promotion.destinationValue, '_blank', 'noopener,noreferrer'); return }
    if (promotion.destinationTypeCode === 'INTERNAL_PATH' && promotion.destinationValue) navigate(promotion.destinationValue)
  }, [])

  return <section ref={sectionRef} id="live-activity" className="customerSection liveActivitySection customerHomePerformanceV305"><header><div><p>수달 실시간</p><h2>지금 진행 중인 서비스</h2><span>{feedRegion ? `${feedRegion} 기본지역을 기준으로 최근 요청을 보여드립니다.` : '기본지역이 없으면 전국의 최근 요청 20건을 보여드립니다.'}</span></div><i><b aria-hidden="true" />{feedRegion || '전국'} 최신 요청 · 광고 별도 표시</i></header>
    {newCount > 0 && <div className="liveActivityRefreshRow"><button type="button" onClick={() => { setNewCount(0); document.querySelector('.liveActivityList')?.scrollIntoView({ behavior: 'smooth', block: 'start' }) }}>새 요청 {newCount}건</button></div>}
    {error && <p className="liveActivityError">{error}</p>}
    <div className="liveActivityList">{rows.map(row => row.kind === 'activity' ? <button type="button" key={row.item.id} onClick={() => setSelected(row.item)}><ServiceThumbnail code={row.item.serviceCode} name={row.item.serviceName} className="liveActivityIcon" /><span><small>{relativeTime(row.item.requestedAt ?? row.item.occurredAt)} · {row.item.regionName}</small><strong>{row.item.serviceName}</strong><em className={`liveActivityDomain domain-${row.item.domainCode.toLowerCase()}`}>{row.item.domainLabel}</em></span><b>요청·견적 보기</b></button> : <LiveActivityAdvertisement key={`ad-${row.item.creativeId}`} promotion={row.item} onVisible={recordImpression} onOpen={openPromotion} />)}{feed && items.length === 0 && <p>{feedRegion ? `${feedRegion}의 공개 가능한 최근 요청이 없습니다.` : '공개 가능한 전국 최근 요청이 없습니다.'}</p>}{!feed && !error && <p>{regionReady ? '최근 요청을 확인하고 있습니다…' : '기본지역을 확인하고 있습니다…'}</p>}</div>
    {feed && <small className="liveActivityPrivacy">{feed.privacyNotice}</small>}
    {selected && <div className="liveActivityBackdrop" role="presentation" onMouseDown={event => { if (event.target === event.currentTarget) setSelected(null) }}><article className="liveActivityModal" role="dialog" aria-modal="true" aria-labelledby="live-activity-title"><header><ServiceThumbnail code={selected.serviceCode} name={selected.serviceName} className="liveActivityModalImage" /><div><small>{relativeTime(selected.requestedAt ?? selected.occurredAt)} · {selected.regionName}</small><h3 id="live-activity-title">{selected.requestTitle || selected.serviceName}</h3></div><button type="button" aria-label="닫기" onClick={() => setSelected(null)}>×</button></header><section className="liveActivityRequestDetails"><h4>견적 요청 정보</h4><dl><div><dt>서비스</dt><dd>{selected.serviceName}</dd></div><div><dt>구분</dt><dd>{selected.domainLabel}</dd></div><div><dt>지역</dt><dd>{selected.regionName}</dd></div><div><dt>요청일</dt><dd>{selected.requestedAt ? formatDate(selected.requestedAt) : '확인 중'}</dd></div><PublicRequestFacts item={selected} /><div className="wide"><dt>요청 내용</dt><dd>{selected.requestSummary || '등록된 서비스 조건을 기준으로 전문가 견적을 받았습니다.'}</dd></div></dl></section><section className="liveActivityQuoteAmounts"><header><h4>전문가가 제출한 견적금액</h4><span>{selected.quoteAmounts.length}건</span></header>{selected.quoteAmounts.length > 0 ? <ol>{selected.quoteAmounts.map((quote, index) => <li key={`${quote.submittedAt}-${index}`}><span><b>견적 {index + 1}</b><time>{formatDate(quote.submittedAt)}</time></span><strong>{formatMoney(quote.amount, quote.currencyCode)}</strong></li>)}</ol> : <p>아직 공개할 수 있는 제출 견적이 없습니다.</p>}</section><p>고객·전문가의 개인정보와 상세주소, 전문가 업체명은 공개하지 않습니다.</p><footer><button type="button" onClick={() => navigate(`/customer/requests/new?service=${encodeURIComponent(selected.serviceId)}`)}>같은 서비스 요청하기</button></footer></article></div>}
  </section>
}

type LiveActivityRow = { kind: 'activity'; item: PublicActivityItem; activityIndex: number } | { kind: 'advertisement'; item: FeedPromotion }

function interleaveRows(items: PublicActivityItem[], promotions: FeedPromotion[]): LiveActivityRow[] {
  const rows: LiveActivityRow[] = []
  items.forEach((item, index) => {
    rows.push({ kind: 'activity', item, activityIndex: index })
    const promotion = promotions.find(value => value.feedAfter === index + 1)
    if (promotion) rows.push({ kind: 'advertisement', item: promotion })
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
  const templateCode = promotion.altText?.match(/^template:([a-z0-9-]+);/)?.[1]
  const style = templateCode ? { backgroundImage: `linear-gradient(90deg,rgba(255,255,255,.98) 0%,rgba(255,255,255,.92) 52%,rgba(255,255,255,.12) 78%),url('/advertising/templates/${templateCode}.webp')` } : undefined
  return <button ref={element} type="button" className={`liveActivityAdvertisement${templateCode ? ' hasTemplate' : ''}`} style={style} onClick={() => canOpen && onOpen(promotion)} disabled={!canOpen} aria-label={`광고: ${promotion.title}`}><span className="liveActivityAdBadge">광고</span><span><small>{promotion.subtitle ?? '수달 라이프 추천 전문가'}</small><strong>{promotion.title}</strong>{promotion.bodyText && <em>{promotion.bodyText}</em>}</span><b>{canOpen ? promotion.buttonText || '자세히 보기' : '광고'}</b></button>
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
