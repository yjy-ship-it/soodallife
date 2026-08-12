const labels: Record<string, string> = {
  ACTIVE: '활동 중', INACTIVE: '비활동', PENDING: '확인 대기', APPROVED: '승인', REJECTED: '반려 · 보완 필요',
  EXPIRED: '만료 · 갱신 필요', OPEN: '모집 중', AVAILABLE: '응답 가능', SUBMITTED: '제출 완료', ACCEPTED: '채택',
  NOT_SELECTED: '미채택', CREATED: '거래 생성', PROPOSED: '일정 제안', CONFIRMED: '일정 확정', SCHEDULED: '일정 확정',
  RESCHEDULED: '변경 일정 확정', IN_PROGRESS: '진행 중', PROVIDER_COMPLETED: '완료보고 · 고객 확인 대기',
  COMPLETION_SUBMITTED: '완료보고 · 고객 확인 대기', REVISION_REQUESTED: '보완 요청', COMPLETED: '완료',
  TERMINATION_REQUESTED: '해지 처리 중', POLICY_PENDING: '정책 확정 전', RECEIVED: '접수 확인 필요',
  PROVIDER_CONFIRMED: '접수 확인 완료', VISIT_SCHEDULED: '방문 예정', RESOLVED: '해결', UNRESOLVED_CLOSED: '미해결 종료',
  DISPUTED: '분쟁 진행', DEPARTED: '출발', EN_ROUTE: '이동 중', ARRIVED: '현장 도착', CANCELLED: '취소',
}

export const providerStatusLabel = (code: string) => labels[code] ?? code.replaceAll('_', ' ')

const safeProviderRoute = /^\/provider(?:\/(?:matched-requests|quotes|work|schedule|progress|inbox|messages|care|interior|emergency|after-services|disputes|wallet|notifications|onboarding|services|areas|documents|approval)(?:\/[0-9a-f-]+)*)?(?:\?(?:group|domain)=[A-Z_]+)?$/i

export const providerRoute = (route: string) => safeProviderRoute.test(route) ? route : '/provider'
