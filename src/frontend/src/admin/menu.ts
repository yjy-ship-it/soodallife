export interface AdminMenuItem {
  label: string
  path: string
  shortLabel: string
  allowedRoles: readonly ['ADMIN']
}

const adminOnly = ['ADMIN'] as const

export const adminMenuItems: readonly AdminMenuItem[] = [
  { label: '대시보드', path: '/admin', shortLabel: '홈', allowedRoles: adminOnly },
  { label: '서비스 관리', path: '/admin/services', shortLabel: '서비스', allowedRoles: adminOnly },
  { label: '공급자 요건 기준정보', path: '/admin/provider-requirement-standards', shortLabel: '요건', allowedRoles: adminOnly },
  { label: '가격·수수료', path: '/admin/pricing', shortLabel: '가격', allowedRoles: adminOnly },
  { label: '고객 관리', path: '/admin/customers', shortLabel: '고객', allowedRoles: adminOnly },
  { label: '공급자 관리', path: '/admin/providers', shortLabel: '공급자', allowedRoles: adminOnly },
  { label: '공급자 활동 종료', path: '/admin/provider-exits', shortLabel: '종료', allowedRoles: adminOnly },
  { label: '지역·매칭', path: '/admin/matching', shortLabel: '매칭', allowedRoles: adminOnly },
  { label: '충전금 관리', path: '/admin/credits', shortLabel: '충전금', allowedRoles: adminOnly },
  { label: '요청·견적 관리', path: '/admin/requests', shortLabel: '견적', allowedRoles: adminOnly },
  { label: '거래·작업 관리', path: '/admin/transactions', shortLabel: '거래', allowedRoles: adminOnly },
  { label: 'A/S·분쟁', path: '/admin/disputes', shortLabel: '분쟁', allowedRoles: adminOnly },
  { label: '신뢰도 관리', path: '/admin/trust', shortLabel: '신뢰도', allowedRoles: adminOnly },
  { label: '리뷰·평점 관리', path: '/admin/reviews', shortLabel: '리뷰', allowedRoles: adminOnly },
  { label: '신고 관리', path: '/admin/reports', shortLabel: '신고', allowedRoles: adminOnly },
  { label: '제재·이의신청', path: '/admin/sanctions', shortLabel: '제재', allowedRoles: adminOnly },
  { label: '신고·제재 기준정보', path: '/admin/case-policies', shortLabel: '사건 기준', allowedRoles: adminOnly },
  { label: '광고·프로모션·콘텐츠', path: '/admin/content', shortLabel: '광고', allowedRoles: adminOnly },
  { label: '정기구독', path: '/admin/subscriptions', shortLabel: '구독', allowedRoles: adminOnly },
  { label: '인테리어', path: '/admin/interior', shortLabel: '인테리어', allowedRoles: adminOnly },
  { label: '알림 관리', path: '/admin/notifications', shortLabel: '알림', allowedRoles: adminOnly },
  { label: '결제·정산', path: '/admin/settlements', shortLabel: '정산', allowedRoles: adminOnly },
  { label: '통계·분석', path: '/admin/analytics', shortLabel: '통계', allowedRoles: adminOnly },
  { label: '시스템 관리', path: '/admin/system', shortLabel: '시스템', allowedRoles: adminOnly },
]

export function findAdminMenu(pathname: string): AdminMenuItem | undefined {
  return adminMenuItems.find((item) => item.path === pathname)
    ?? adminMenuItems.filter((item) => pathname.startsWith(`${item.path}/`)).sort((left, right) => right.path.length - left.path.length)[0]
}
