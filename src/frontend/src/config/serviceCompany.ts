export const serviceDomains = {
  customer: 'https://soodallife.kr',
  provider: 'https://partner.soodallife.kr',
  admin: 'https://admin.soodallife.kr',
  api: 'https://api.soodallife.kr',
} as const

export const serviceCompany = {
  isPlaceholder: false,
  serviceName: '수달 라이프',
  serviceNameEnglish: 'SOODAL LIFE',
  companyName: '주식회사 디에이치',
  ceoName: '하정숙',
  businessRegistrationNumber: '896-81-02432',
  ecommerceRegistrationNumber: null as string | null,
  address: '대구광역시 동구 효동로 72-1, 3층(효목동)',
  representativePhone: null as string | null,
  customerServiceHours: null as readonly string[] | null,
  customerServiceEmail: null as string | null,
  partnershipEmail: null as string | null,
  privacyEmail: null as string | null,
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
  { label: '회사소개', href: '/company' },
  { label: '이용약관', href: '/policies/terms' },
  { label: '개인정보처리방침', href: '/policies/privacy', emphasized: true },
  { label: '위치기반서비스 이용약관', href: '/policies/location' },
  { label: '전자금융거래 안내', href: '/policies/electronic-finance' },
  { label: '고객센터', href: '/support' },
  { label: '공지사항', href: '/notices' },
  { label: 'FAQ', href: '/faq' },
] as const

export const businessInformationVerificationUrl: string | null = null
