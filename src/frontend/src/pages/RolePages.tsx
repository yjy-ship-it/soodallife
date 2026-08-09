import type { RoleCode } from '../auth/types'
import { roleHomePaths, navigate } from '../auth/routing'
import { useAuthentication } from '../auth/AuthenticationContext'
import { AuthenticatedLayout } from '../components/AuthenticatedLayout'

const roleContent: Record<RoleCode, { eyebrow: string; title: string; description: string }> = {
  CUSTOMER: {
    eyebrow: 'CUSTOMER',
    title: '고객 홈',
    description: '서비스 요청과 진행 현황을 한곳에서 확인할 수 있는 고객 대시보드가 준비됩니다.',
  },
  PROVIDER: {
    eyebrow: 'PROVIDER',
    title: '공급자 홈',
    description: '배포된 요청과 견적, 작업 일정을 관리하는 공급자 대시보드가 준비됩니다.',
  },
  ADMIN: {
    eyebrow: 'ADMIN',
    title: '관리자 홈',
    description: '공급자 승인과 서비스 운영 현황을 관리하는 관리자 대시보드가 준비됩니다.',
  },
}

export function RoleHomePage({ role }: { role: RoleCode }) {
  const content = roleContent[role]
  return (
    <AuthenticatedLayout>
      <section className="heroCard">
        <p className="eyebrow">{content.eyebrow}</p>
        <h1>{content.title}</h1>
        <p>{content.description}</p>
        <div className="readyBadge"><span aria-hidden="true" /> 인증 및 권한 확인 완료</div>
      </section>
      <section className="placeholderGrid" aria-label="향후 대시보드 영역">
        {role === 'CUSTOMER' ? <>
          <button type="button" onClick={() => navigate('/customer/requests/new')}><strong>서비스 요청하기</strong><span>카테고리를 선택하고 요청을 등록합니다.</span></button>
          <button type="button" onClick={() => navigate('/customer/requests')}><strong>내 요청</strong><span>등록한 요청과 상세 답변을 확인합니다.</span></button>
        </> : role === 'PROVIDER' ? <>
          <button type="button" onClick={() => navigate('/provider/services')}><strong>서비스 설정</strong><span>제공할 실제 하위 서비스를 선택합니다.</span></button>
          <button type="button" onClick={() => navigate('/provider/areas')}><strong>출장지역 설정</strong><span>서비스별 시·군·구 출장지역을 저장합니다.</span></button>
          <button type="button" onClick={() => navigate('/provider/matched-requests')}><strong>받은 요청</strong><span>나에게 실제 배포된 고객 요청을 확인합니다.</span></button>
        </> : <>
          <article><strong>진행 현황</strong><span>다음 개발 단계에서 연결됩니다.</span></article>
          <article><strong>최근 활동</strong><span>아직 표시할 업무 데이터가 없습니다.</span></article>
        </>}
        {role !== 'PROVIDER' && <article><strong>빠른 메뉴</strong><span>역할별 기능이 순차적으로 추가됩니다.</span></article>}
      </section>
    </AuthenticatedLayout>
  )
}

export function RoleSelectionPage() {
  const { user } = useAuthentication()
  return (
    <AuthenticatedLayout>
      <section className="heroCard compactHero">
        <p className="eyebrow">SELECT ROLE</p>
        <h1>사용할 역할을 선택하세요</h1>
        <p>하나의 계정에 부여된 역할별 화면과 권한은 서로 분리됩니다.</p>
      </section>
      <section className="roleGrid">
        {user?.roles.map((role) => (
          <button key={role} type="button" onClick={() => navigate(roleHomePaths[role])}>
            <span>{roleContent[role].title}</span>
            <small>{role}</small>
          </button>
        ))}
      </section>
    </AuthenticatedLayout>
  )
}

export function AccessDeniedPage() {
  return (
    <AuthenticatedLayout>
      <section className="messageCard">
        <p className="errorCode">403</p>
        <h1>이 화면에 접근할 권한이 없습니다.</h1>
        <p>현재 계정에 부여된 역할의 홈 화면을 이용해 주세요.</p>
        <button className="primaryButton inlineButton" type="button" onClick={() => navigate('/roles', true)}>
          내 역할 보기
        </button>
      </section>
    </AuthenticatedLayout>
  )
}
