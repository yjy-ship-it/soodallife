import type { InteriorDashboard, InteriorDetail, InteriorProject } from './interiorTypes'

async function call<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    credentials: 'include', ...options,
    headers: options?.body instanceof FormData ? options.headers : options?.body ? { 'Content-Type': 'application/json', ...options.headers } : options?.headers,
  })
  if (!response.ok) {
    let message = '인테리어 업무를 처리하지 못했습니다.'
    try { message = ((await response.json()) as { message?: string }).message ?? message } catch { /* response body optional */ }
    throw new Error(message)
  }
  return response.json() as Promise<T>
}

const post = (body: unknown): RequestInit => ({ method: 'POST', body: JSON.stringify(body) })
const key = (name: string) => `${name}-${crypto.randomUUID()}`

export const providerInteriorApi = {
  home: () => call<InteriorDashboard>('/api/v1/providers/me/interior/home'),
  projects: (role = '') => call<InteriorProject[]>(`/api/v1/providers/me/interior/projects${role ? `?role=${role}` : ''}`),
  detail: (id: string) => call<InteriorDetail>(`/api/v1/providers/me/interior/projects/${id}`),
  proposeVisit: (projectId: string, body: { scheduledStartAt: string; scheduledEndAt: string | null; proposedVisitFee: number; proposalTerms: string }) => call<InteriorDetail>(`/api/v1/providers/me/interior/projects/${projectId}/site-visit-proposals`, post({ ...body, idempotencyKey: key('interior-visit-proposal') })),
  upload: async (id: string, file: File) => {
    const body = new FormData(); body.append('file', file)
    return call<{ fileId: string }>(`/api/v1/providers/me/interior/projects/${id}/files`, { method: 'POST', body })
  },
  startVisit: (visit: { id: string; rowVersion: string }) => call<InteriorDetail>(`/api/v1/providers/me/interior/site-visits/${visit.id}/start`, post({ idempotencyKey: key('interior-visit-start'), rowVersion: visit.rowVersion })),
  completeVisit: (visit: { id: string; rowVersion: string }, body: { measurementSummary: string; constraint: string; riskNote: string; fileIds: string[] }) => call<InteriorDetail>(`/api/v1/providers/me/interior/site-visits/${visit.id}/completion`, post({ ...body, measurements: [], idempotencyKey: key('interior-visit-complete'), rowVersion: visit.rowVersion })),
  design: (projectId: string, body: { title: string; description: string; fileIds: string[] }) => call<InteriorDetail>(`/api/v1/providers/me/interior/projects/${projectId}/designs`, post({ ...body, idempotencyKey: key('interior-design') })),
  registerContract: (projectId: string, body: { contractAmount: number; currencyCode: string; scopeSnapshotJson: string; scheduleSnapshotJson: string; warrantySnapshotJson: string; contractSignedDate: string; plannedStartDate: string; plannedCompletionDate: string; paymentPlans: Array<{ sequenceNo: number; name: string; amount: number; dueDate: string | null; condition: string }>; contractFileIds: string[] }) => call<InteriorDetail>(`/api/v1/providers/me/interior/projects/${projectId}/contract-records`, post({ ...body, idempotencyKey: key('interior-contract-record') })),
  agree: (_projectId: string, contract: { id: string }, rowVersion = '') => call<InteriorDetail>(`/api/v1/providers/me/interior/contracts/${contract.id}/agreement`, post({ idempotencyKey: key('interior-contract-agree'), rowVersion })),
  payment: (plan: { id: string; amount: number }) => call<string>(`/api/v1/providers/me/interior/payment-plans/${plan.id}/confirmations`, post({ confirmationTypeCode: 'PROVIDER_REPORTED', amount: plan.amount, confirmedAt: new Date().toISOString(), evidenceFileId: null, note: '고객 직접 지급 수령 확인', idempotencyKey: key('interior-payment-confirm') })),
  createStage: (projectId: string, body: { sequenceNo: number; name: string; plannedStartDate: string; plannedEndDate: string }) => call<InteriorDetail>(`/api/v1/providers/me/interior/projects/${projectId}/stages`, post({ ...body, idempotencyKey: key('interior-stage-create') })),
  stage: (stage: { id: string; rowVersion: string }, progressPercent: number, updateText: string, fileIds: string[]) => call<InteriorDetail>(`/api/v1/providers/me/interior/stages/${stage.id}/updates`, post({ progressPercent, updateText, fileIds, idempotencyKey: key('interior-stage-update'), rowVersion: stage.rowVersion })),
  inspect: (stage: { id: string; rowVersion: string }, statusCode: string, result: string, correction: string, fileIds: string[]) => call<InteriorDetail>(`/api/v1/providers/me/interior/stages/${stage.id}/inspections`, post({ statusCode, result, correction, fileIds, idempotencyKey: key('interior-inspection'), rowVersion: stage.rowVersion })),
  change: (contractId: string, body: { reason: string; scopeChange: string; amountDelta: number; scheduleImpactDays: number | null; fileIds: string[] }) => call<InteriorDetail>(`/api/v1/providers/me/interior/contracts/${contractId}/changes`, post({ ...body, changeTypeCode: 'SCOPE_CHANGE', idempotencyKey: key('interior-change') })),
  completion: (project: { id: string; rowVersion: string }, summary: string) => call<InteriorDetail>(`/api/v1/providers/me/interior/projects/${project.id}/completion`, post({ completionSummary: summary, finalChecklistJson: '{}', idempotencyKey: key('interior-completion'), rowVersion: project.rowVersion })),
}
