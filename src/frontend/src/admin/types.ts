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
