import { useEffect, useState, type PropsWithChildren, type ReactNode } from 'react'
import { useAuthentication } from '../auth/AuthenticationContext'
import { navigate } from '../auth/routing'
import { BrandLogo } from '../components/BrandLogo'
import { ServiceFooter } from '../components/ServiceFooter'
import './provider.css'

const desktop = [
  ['/provider', '홈'], ['/provider/onboarding', '가입 프로필'], ['/provider/services', '서비스 관리'],
  ['/provider/areas', '활동지역'], ['/provider/documents', '증빙·자격'], ['/provider/approval', '승인상태'],
] as const
const mobile = [['/provider', '홈'], ['/provider/matched-requests', '요청'], ['/provider/work', '진행'], ['/provider/schedule', '일정'], ['/provider/onboarding', '마이수달']] as const

export function ProviderAppLayout({ children, actions }: PropsWithChildren<{ actions?: ReactNode }>) {
  const { user, logout } = useAuthentication()
  const [unread, setUnread] = useState(0)
  const pathname = window.location.pathname
  useEffect(() => { fetch('/api/v1/notifications/unread-count', { credentials: 'include' }).then(r => r.ok ? r.json() as Promise<{ count: number }> : Promise.reject()).then(v => setUnread(v.count)).catch(() => setUnread(0)) }, [pathname])
  const signOut = async () => { await logout(); navigate('/provider/start') }
  return <div className="providerAppShell">
    <a className="skipLink" href="#provider-main">본문으로 바로가기</a>
    <header className="providerHeader"><div className="providerHeaderInner"><BrandLogo />
      <span className="providerBrandLabel">수달 파트너스</span>
      <nav className="providerDesktopNav" aria-label="공급자 주요 메뉴">{desktop.map(([path, label]) => <button className={pathname === path ? 'isActive' : ''} key={path} onClick={() => navigate(path)}>{label}</button>)}</nav>
      <div className="providerHeaderActions"><button className="providerBell" onClick={() => navigate('/provider/notifications')} aria-label={`읽지 않은 알림 ${unread}개`}>알림{unread > 0 && <span>{unread > 99 ? '99+' : unread}</span>}</button><button onClick={() => navigate('/roles')}>{user?.loginId}</button><button onClick={() => void signOut()}>로그아웃</button></div>
    </div></header>
    <main id="provider-main" className="providerMain">{actions}{children}</main>
    <ServiceFooter variant="provider" />
    <nav className="providerBottomNav" aria-label="모바일 공급자 메뉴">{mobile.map(([path, label]) => { const enabled = ['/provider', '/provider/onboarding'].includes(path); const active = pathname === path; return <button key={path} className={active ? 'isActive' : ''} disabled={!enabled} title={!enabled ? '다음 개발 단계에서 제공됩니다.' : undefined} onClick={() => enabled && navigate(path)}><span aria-hidden="true">{label === '홈' ? '⌂' : label === '마이수달' ? '○' : '·'}</span>{label}</button> })}</nav>
  </div>
}
