import { useEffect, useState } from 'react'
import { getAdminDashboardSummary } from './api'
import { AdminLayout } from './AdminLayout'
import { findAdminMenu } from './menu'
import type { AdminDashboardSummary, AdminMetric } from './types'

const dashboardCards: Array<{ key: keyof AdminDashboardSummary; label: string; description: string; unit: string }> = [
  { key: 'totalCustomers', label: '전체 고객', description: '등록된 고객 프로필', unit: '명' },
  { key: 'totalProviders', label: '전체 공급자', description: '등록된 공급자 프로필', unit: '곳' },
  { key: 'pendingProviders', label: '승인 대기 공급자', description: '심사 대기 상태', unit: '곳' },
  { key: 'activeRequests', label: '진행 중 요청', description: '서비스 요청 현황', unit: '건' },
  { key: 'activeTransactions', label: '진행 중 거래', description: '거래 및 작업 현황', unit: '건' },
  { key: 'unresolvedAfterServiceCases', label: '미처리 A/S·분쟁', description: '접수 후 처리 대기 현황', unit: '건' },
]

function MetricValue({ metric, unit }: { metric: AdminMetric; unit: string }) {
  if (metric.value === null) {
    return <span className="adminMetricPending">{metric.unavailableReason ?? '집계 보류'}</span>
  }
  return <strong className="adminMetricValue">{metric.value.toLocaleString('ko-KR')}<small>{unit}</small></strong>
}

export function AdminDashboardPage({ pathname }: { pathname: string }) {
  const [summary, setSummary] = useState<AdminDashboardSummary | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    getAdminDashboardSummary()
      .then((result) => active && setSummary(result))
      .catch((requestError: unknown) => active && setError(requestError instanceof Error ? requestError.message : '현황을 불러오지 못했습니다.'))
    return () => { active = false }
  }, [])

  return (
    <AdminLayout pathname={pathname}>
      <section className="adminPageHeading">
        <div><p>운영 현황</p><h1>대시보드</h1></div>
        <span>오늘의 주요 운영 지표를 확인합니다.</span>
      </section>
      {error && <div className="adminError" role="alert">{error}</div>}
      <section className="adminMetricGrid" aria-label="운영 요약">
        {dashboardCards.map((card) => (
          <article className="adminMetricCard" key={card.key}>
            <div><span>{card.label}</span><small>{card.description}</small></div>
            {summary ? <MetricValue metric={summary[card.key]} unit={card.unit} /> : <span className="adminMetricLoading">불러오는 중…</span>}
          </article>
        ))}
      </section>
      <section className="adminGuidePanel">
        <div><span className="adminSectionLabel">업무 안내</span><h2>관리자 공통 기반이 준비되었습니다.</h2></div>
        <p>좌측 메뉴에서 본사 업무 영역을 확인할 수 있습니다. 세부 관리 기능은 단계별로 연결됩니다.</p>
      </section>
    </AdminLayout>
  )
}

export function AdminPlaceholderPage({ pathname }: { pathname: string }) {
  const menu = findAdminMenu(pathname)
  return (
    <AdminLayout pathname={pathname}>
      <section className="adminPageHeading">
        <div><p>본사 업무</p><h1>{menu?.label ?? '관리자 업무'}</h1></div>
      </section>
      <section className="adminComingSoon">
        <span aria-hidden="true">준비</span>
        <h2>준비 중인 기능입니다.</h2>
        <p>업무 정책과 관리 기준을 확정한 뒤 순차적으로 제공하겠습니다.</p>
      </section>
    </AdminLayout>
  )
}
