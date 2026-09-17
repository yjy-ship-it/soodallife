export interface AdminMetric {
  value: number | null
  unavailableReason: string | null
}

export interface AdminDashboardSummary {
  totalCustomers: AdminMetric
  totalProviders: AdminMetric
  pendingProviders: AdminMetric
  activeRequests: AdminMetric
  activeTransactions: AdminMetric
  unresolvedAfterServiceCases: AdminMetric
}

export interface AdminAnalyticsMetric {
  code: string
  label: string
  value: number | null
  unit: string
  previousValue: number | null
  changeRate: number | null
  note: string | null
}

export interface AdminAnalyticsSection {
  code: string
  title: string
  metrics: AdminAnalyticsMetric[]
}

export interface AdminAnalyticsBreakdown {
  code: string
  label: string
  value: number
  unit: string
}

export interface AdminAnalyticsTrendPoint {
  date: string
  newCustomers: number
  newProviders: number
  requests: number
  transactions: number
  feeChargedAmount: number
}

export interface AdminAnalyticsAttention {
  code: string
  label: string
  count: number
  severity: 'WARNING' | 'CRITICAL'
  path: string
  description: string
}

export interface AdminAnalyticsFilterOption {
  id: string
  label: string
  parentLabel: string | null
}

export interface AdminAnalyticsDashboard {
  appliedFilter: {
    range: string
    from: string
    to: string
    previousFrom: string
    previousTo: string
    categoryId: string | null
    categoryName: string | null
    areaId: string | null
    areaName: string | null
    providerId: string | null
  }
  kpis: AdminAnalyticsMetric[]
  trend: AdminAnalyticsTrendPoint[]
  requestsByCategory: AdminAnalyticsBreakdown[]
  requestsByRegion: AdminAnalyticsBreakdown[]
  sections: AdminAnalyticsSection[]
  attention: AdminAnalyticsAttention[]
  categories: AdminAnalyticsFilterOption[]
  regions: AdminAnalyticsFilterOption[]
  providers: AdminAnalyticsFilterOption[]
  unavailableMetrics: string[]
  generatedAt: string
}

export interface AdminAnalyticsFilters {
  range: string
  from?: string
  to?: string
  categoryId?: string
  areaId?: string
  providerId?: string
}
