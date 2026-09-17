import { useEffect, useState } from 'react'
import { getAdminAnalyticsDashboard } from './api'
import { AdminLayout } from './AdminLayout'
import { findAdminMenu } from './menu'
import type { AdminAnalyticsBreakdown, AdminAnalyticsDashboard, AdminAnalyticsFilters, AdminAnalyticsMetric } from './types'

const rangeOptions = [
  ['TODAY', '오늘'],
  ['LAST_7_DAYS', '최근 7일'],
  ['LAST_30_DAYS', '최근 30일'],
  ['THIS_MONTH', '이번 달'],
  ['LAST_MONTH', '지난 달'],
  ['CUSTOM', '사용자 지정'],
] as const

const numberFormatter = new Intl.NumberFormat('ko-KR', { maximumFractionDigits: 1 })
const currencyFormatter = new Intl.NumberFormat('ko-KR', { maximumFractionDigits: 0 })
const dateFormatter = new Intl.DateTimeFormat('ko-KR', { month: 'short', day: 'numeric' })

function formatMetric(metric: AdminAnalyticsMetric) {
  if (metric.value === null) return '집계 불가'
  const formatted = metric.unit === '원' ? currencyFormatter.format(metric.value) : numberFormatter.format(metric.value)
  return `${formatted}${metric.unit}`
}

function ChangeBadge({ metric }: { metric: AdminAnalyticsMetric }) {
  if (metric.changeRate === null) return null
  const direction = metric.changeRate > 0 ? 'up' : metric.changeRate < 0 ? 'down' : 'flat'
  return <span className={`analyticsChange ${direction}`}>이전 기간 대비 {metric.changeRate > 0 ? '+' : ''}{metric.changeRate}%</span>
}

const detailPath = (code: string) => code.includes('provider') ? '/admin/providers' : code.includes('request') || code.includes('quote') || code.includes('transaction') ? '/admin/requests' : code.includes('wallet') || code.includes('fee') || code.includes('payment') || code.includes('settlement') ? '/admin/settlements' : code.includes('review') || code.includes('rating') ? '/admin/reviews' : code.includes('dispute') || code.includes('after_service') ? '/admin/disputes' : code.includes('notification') || code.includes('deliver') ? '/admin/notifications' : code.includes('subscription') ? '/admin/subscriptions' : code.includes('interior') ? '/admin/interior' : ''

function MetricCard({ metric, open }: { metric: AdminAnalyticsMetric; open?: (path: string) => void }) {
  const path = detailPath(metric.code)
  return (
    <article className={`analyticsKpiCard${path ? ' clickable' : ''}`} onClick={() => path && open?.(path)}>
      <span>{metric.label}</span>
      <strong className={metric.value === null ? 'unavailable' : ''}>{formatMetric(metric)}</strong>
      <ChangeBadge metric={metric} />
      {metric.note && <small>{metric.note}</small>}
    </article>
  )
}

function BreakdownList({ title, items }: { title: string; items: AdminAnalyticsBreakdown[] }) {
  const maximum = Math.max(1, ...items.map((item) => item.value))
  return (
    <section className="analyticsPanel">
      <div className="analyticsPanelHeading"><h2>{title}</h2><span>상위 {Math.min(20, items.length)}개</span></div>
      {items.length === 0 ? <p className="analyticsEmpty">선택한 조건의 데이터가 없습니다.</p> : (
        <div className="analyticsBars">
          {items.map((item) => (
            <div className="analyticsBarRow" key={item.code}>
              <span>{item.label}</span><div><i style={{ width: `${Math.max(3, item.value / maximum * 100)}%` }} /></div><strong>{numberFormatter.format(item.value)}{item.unit}</strong>
            </div>
          ))}
        </div>
      )}
    </section>
  )
}

function TrendChart({ dashboard }: { dashboard: AdminAnalyticsDashboard }) {
  const points = dashboard.trend
  const maximum = Math.max(1, ...points.flatMap((point) => [point.requests, point.transactions]))
  const width = 900
  const height = 190
  const line = (key: 'requests' | 'transactions') => points.map((point, index) => {
    const x = points.length === 1 ? width / 2 : index / (points.length - 1) * width
    const y = height - point[key] / maximum * (height - 24) - 8
    return `${x},${y}`
  }).join(' ')
  return (
    <section className="analyticsPanel analyticsTrendPanel">
      <div className="analyticsPanelHeading"><div><h2>기간별 운영 추이</h2><p>서버 집계 기준 일별 요청·거래</p></div><div className="analyticsLegend"><span className="request">요청</span><span className="transaction">거래</span></div></div>
      {points.length === 0 ? <p className="analyticsEmpty">표시할 추이가 없습니다.</p> : <>
        <svg className="analyticsTrend" viewBox={`0 0 ${width} ${height}`} role="img" aria-label="일별 요청과 거래 추이">
          <line x1="0" y1={height - 8} x2={width} y2={height - 8} />
          <polyline className="requestLine" points={line('requests')} />
          <polyline className="transactionLine" points={line('transactions')} />
        </svg>
        <div className="analyticsTrendDates"><span>{dateFormatter.format(new Date(`${points[0].date}T00:00:00+09:00`))}</span><span>{dateFormatter.format(new Date(`${points[points.length - 1].date}T00:00:00+09:00`))}</span></div>
      </>}
    </section>
  )
}

export function AdminDashboardPage({ pathname }: { pathname: string }) {
  const [filters, setFilters] = useState<AdminAnalyticsFilters>({ range: 'LAST_30_DAYS' })
  const [dashboard, setDashboard] = useState<AdminAnalyticsDashboard | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  useEffect(() => {
    let active = true
    setLoading(true)
    setError(null)
    getAdminAnalyticsDashboard(filters)
      .then((result) => { if (active) setDashboard(result) })
      .catch((requestError: unknown) => { if (active) setError(requestError instanceof Error ? requestError.message : '현황을 불러오지 못했습니다.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [filters])

  const updateFilter = (name: keyof AdminAnalyticsFilters, value: string) => setFilters((current) => ({ ...current, [name]: value || undefined }))
  const navigate = (path: string) => {
    window.history.pushState({}, '', path)
    window.dispatchEvent(new PopStateEvent('popstate'))
  }
  const exportExcel = () => {
    if (!dashboard) return
    const rows: (string | number)[][] = [['수달 라이프 통계·분석'], ['기간', `${dashboard.appliedFilter.from} ~ ${dashboard.appliedFilter.to}`], ['전문가', dashboard.appliedFilter.providerId ?? '전체'], [], ['구분', '지표', '값', '단위', '비고']]
    dashboard.kpis.forEach(item => rows.push(['핵심 KPI', item.label, item.value ?? '', item.unit, item.note ?? '']))
    dashboard.sections.forEach(section => section.metrics.forEach(item => rows.push([section.title, item.label, item.value ?? '', item.unit, item.note ?? ''])))
    const text='\uFEFF'+rows.map(row=>row.map(value=>String(value).replace(/\t|\r?\n/g,' ')).join('\t')).join('\r\n');const link=document.createElement('a');link.href=URL.createObjectURL(new Blob([text],{type:'application/vnd.ms-excel;charset=utf-8'}));link.download=`soodal-analytics-${dashboard.appliedFilter.from}-${dashboard.appliedFilter.to}.xls`;link.click();URL.revokeObjectURL(link.href)
  }

  return (
    <AdminLayout pathname={pathname}>
      <section className="adminPageHeading analyticsHeading">
        <div><p>본사 경영 현황</p><h1>통계·경영 대시보드</h1></div>
        <div><span>{dashboard ? `${dashboard.appliedFilter.from} ~ ${dashboard.appliedFilter.to} · ${new Date(dashboard.generatedAt).toLocaleString('ko-KR')} 기준` : '실제 업무 DB를 읽기 전용으로 집계합니다.'}</span>{dashboard && <button type="button" onClick={exportExcel}>Excel 내려받기</button>}</div>
      </section>

      <section className="analyticsFilterBar" aria-label="대시보드 필터">
        <div className="analyticsRangeButtons">
          {rangeOptions.map(([value, label]) => <button className={filters.range === value ? 'active' : ''} key={value} type="button" onClick={() => updateFilter('range', value)}>{label}</button>)}
        </div>
        {filters.range === 'CUSTOM' && <div className="analyticsCustomDates"><label>시작일<input type="date" value={filters.from ?? ''} onChange={(event) => updateFilter('from', event.target.value)} /></label><label>종료일<input type="date" value={filters.to ?? ''} onChange={(event) => updateFilter('to', event.target.value)} /></label></div>}
        <div className="analyticsSelects">
          <label>서비스<select value={filters.categoryId ?? ''} onChange={(event) => updateFilter('categoryId', event.target.value)}><option value="">전체 서비스</option>{dashboard?.categories.map((item) => <option key={item.id} value={item.id}>{item.parentLabel ? `${item.parentLabel} › ` : ''}{item.label}</option>)}</select></label>
          <label>지역<select value={filters.areaId ?? ''} onChange={(event) => updateFilter('areaId', event.target.value)}><option value="">전체 지역</option>{dashboard?.regions.map((item) => <option key={item.id} value={item.id}>{item.label}</option>)}</select></label>
          <label>전문가<select value={filters.providerId ?? ''} onChange={(event) => updateFilter('providerId', event.target.value)}><option value="">전체 전문가</option>{dashboard?.providers.map((item) => <option key={item.id} value={item.id}>{item.label}</option>)}</select></label>
        </div>
      </section>

      {error && <div className="adminError" role="alert">{error}</div>}
      {loading && !dashboard ? <section className="analyticsLoading">실제 운영 데이터를 집계하고 있습니다…</section> : dashboard && <>
        <section className="analyticsKpiGrid" aria-label="핵심 KPI">{dashboard.kpis.map((metric) => <MetricCard key={metric.code} metric={metric} open={navigate} />)}</section>
        <section className="analyticsAttentionPanel">
          <div className="analyticsPanelHeading"><div><span className="adminSectionLabel">ACTION REQUIRED</span><h2>지금 처리해야 할 일</h2></div><p>발생 건수는 귀책 또는 매출로 해석하지 않습니다.</p></div>
          <div className="analyticsAttentionGrid">{dashboard.attention.map((item) => <button key={item.code} type="button" onClick={() => navigate(item.path)}><span className={item.severity.toLowerCase()}>{item.severity === 'CRITICAL' ? '긴급' : '확인'}</span><strong>{item.count.toLocaleString('ko-KR')}건</strong><b>{item.label}</b><small>{item.description}</small></button>)}</div>
        </section>
        <TrendChart dashboard={dashboard} />
        <div className="analyticsTwoColumns"><BreakdownList title="서비스별 신규 요청" items={dashboard.requestsByCategory} /><BreakdownList title="지역별 신규 요청" items={dashboard.requestsByRegion} /></div>
        <section className="analyticsSectionGrid">{dashboard.sections.map((section) => <article className="analyticsSectionCard" key={section.code}><div className="analyticsPanelHeading"><h2>{section.title}</h2><span>{section.metrics.length}개 지표</span></div><dl>{section.metrics.map((metric) => { const path=detailPath(metric.code); return <div key={metric.code} className={path?'clickable':''} onClick={()=>path&&navigate(path)}><dt>{metric.label}{metric.note && <small>{metric.note}</small>}</dt><dd className={metric.value === null ? 'unavailable' : ''}>{formatMetric(metric)}<ChangeBadge metric={metric} /></dd></div> })}</dl></article>)}</section>
        <section className="analyticsUnavailable"><div><span className="adminSectionLabel">DATA LIMITATIONS</span><h2>현재 계산하지 않는 지표</h2></div><ul>{dashboard.unavailableMetrics.map((item) => <li key={item}>{item}</li>)}</ul></section>
      </>}
    </AdminLayout>
  )
}

export function AdminPlaceholderPage({ pathname }: { pathname: string }) {
  const menu = findAdminMenu(pathname)
  return <AdminLayout pathname={pathname}><section className="adminPageHeading"><div><p>본사 업무</p><h1>{menu?.label ?? '관리자 업무'}</h1></div></section><section className="adminComingSoon"><span aria-hidden="true">준비</span><h2>준비 중인 기능입니다.</h2><p>업무 정책과 관리 기준을 확정한 뒤 순차적으로 제공하겠습니다.</p></section></AdminLayout>
}
