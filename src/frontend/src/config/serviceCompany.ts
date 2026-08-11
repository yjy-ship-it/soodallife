// Development-only placeholder information. Replace every placeholder value and
// policy destination with verified production information before launch.
export const serviceCompany = {
  isPlaceholder: true,
  serviceName: '수달 라이프',
  serviceNameEnglish: 'SOODAL LIFE',
  companyName: '주식회사 디에이치',
  ceoName: '홍길동',
  businessRegistrationNumber: '123-45-67890',
  ecommerceRegistrationNumber: '제2026-대구○○-0001호',
  address: '대구광역시 ○○구 ○○로 123, 4층',
  representativePhone: '1588-0000',
  customerServiceHours: [
    '평일 09:00 ~ 18:00',
    '점심시간 12:00 ~ 13:00',
    '토·일·공휴일 휴무',
  ],
  customerServiceEmail: 'help@soodallife.co.kr',
  partnershipEmail: 'partner@soodallife.co.kr',
  privacyEmail: 'privacy@soodallife.co.kr',
  platformNotice:
    '수달 라이프는 고객과 서비스 공급자를 연결하는 생활서비스 플랫폼입니다. 서비스 계약 및 작업 수행의 당사자 관계는 개별 서비스의 거래 형태와 계약 조건에 따라 달라질 수 있습니다.',
  copyright: '© 2026 주식회사 디에이치. All rights reserved.',
} as const

type ServicePolicyLink = {
  label: string
  href: string | null
  emphasized?: boolean
}

export const servicePolicyLinks: readonly ServicePolicyLink[] = [
  { label: '회사소개', href: null },
  { label: '이용약관', href: null },
  { label: '개인정보처리방침', href: null, emphasized: true },
  { label: '위치기반서비스 이용약관', href: null },
  { label: '전자금융거래 안내', href: null },
  { label: '고객센터', href: '/support' },
  { label: '공지사항', href: '/notices' },
  { label: 'FAQ', href: '/faq' },
] as const

export const businessInformationVerificationUrl: string | null = null
