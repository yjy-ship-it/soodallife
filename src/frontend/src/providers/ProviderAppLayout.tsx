import { useEffect, useState, type PropsWithChildren, type ReactNode } from 'react'
import { useAuthentication } from '../auth/AuthenticationContext'
import { navigate } from '../auth/routing'
import { BrandLogo } from '../components/BrandLogo'
import { ServiceFooter } from '../components/ServiceFooter'
import './provider.css'

const desktop = [
  ['/provider', '홈'], ['/provider/matched-requests', '요청·견적'], ['/provider/progress', '진행 중 업무'],
  ['/provider/schedule', '일정'], ['/provider/messages', '메시지'], ['/provider/care', '수달 케어'],
  ['/provider/interior', '수달 인테리어'], ['/provider/emergency', '긴급출동'],
  ['/provider/after-services', 'A/S·분쟁'], ['/provider/wallet', '충전금'], ['/provider/onboarding', '마이수달'],
] as const
const mobile: ReadonlyArray<readonly [string, string]> = [['/provider', '홈'], ['/provider/matched-requests', '요청'], ['/provider/progress', '진행'], ['/provider/messages', '메시지'], ['/provider/onboarding', '마이']]

export function ProviderAppLayout({ children, actions }: PropsWithChildren<{ actions?: ReactNode }>) {
  const { user, logout } = useAuthentication()
  const [unread, setUnread] = useState(0)
  const [chatUnread, setChatUnread] = useState(0)
  const pathname = window.location.pathname
  useEffect(() => { fetch('/api/v1/notifications/unread-count', { credentials: 'include' }).then(r => r.ok ? r.json() as Promise<{ count: number }> : Promise.reject()).then(v => setUnread(v.count)).catch(() => setUnread(0)) }, [pathname])
  useEffect(() => { fetch('/api/v1/chat/unread-count', { credentials: 'include' }).then(r => r.ok ? r.json() as Promise<{ count: number }> : Promise.reject()).then(v => setChatUnread(v.count)).catch(() => setChatUnread(0)) }, [pathname])
  const signOut = async () => { await logout(); navigate('/provider/start') }
  return <div className="providerAppShell">
    <a className="skipLink" href="#provider-main">본문으로 바로가기</a>
    <header className="providerHeader"><div className="providerHeaderInner"><BrandLogo />
      <span className="providerBrandLabel">수달 파트너스</span>
      <nav className="providerDesktopNav" aria-label="공급자 주요 메뉴">{desktop.map(([path, label]) => <button className={isActive(pathname, path) ? 'isActive' : ''} key={path} onClick={() => navigate(path)}>{label}</button>)}</nav>
      <div className="providerHeaderActions"><button className="providerBell" onClick={() => navigate('/provider/messages')} aria-label={`읽지 않은 메시지 ${chatUnread}개`}>메시지{chatUnread > 0 && <span>{chatUnread > 99 ? '99+' : chatUnread}</span>}</button><button className="providerBell" onClick={() => navigate('/provider/notifications')} aria-label={`읽지 않은 알림 ${unread}개`}>알림{unread > 0 && <span>{unread > 99 ? '99+' : unread}</span>}</button><button onClick={() => navigate('/roles')}>{user?.loginId}</button><button onClick={() => void signOut()}>로그아웃</button></div>
    </div></header>
    <main id="provider-main" className="providerMain">{actions}{children}</main>
    <ServiceFooter variant="provider" />
    <nav className="providerBottomNav" aria-label="모바일 공급자 메뉴">{mobile.map(([path, label]) => <button key={path} className={isActive(pathname, path) ? 'isActive' : ''} onClick={() => navigate(path)}><span aria-hidden="true">{label === '홈' ? '⌂' : label === '마이' ? '○' : label === '메시지' ? '✉' : '·'}</span>{label}{label === '메시지' && chatUnread > 0 && <b className="providerNavBadge">{chatUnread > 99 ? '99+' : chatUnread}</b>}</button>)}</nav>
  </div>
}

function isActive(pathname:string, path:string) {
  if (path === '/provider') return pathname === path
  if (path === '/provider/matched-requests') return pathname.startsWith('/provider/matched-requests') || pathname.startsWith('/provider/quotes')
  if (path === '/provider/progress') return pathname.startsWith('/provider/progress') || pathname.startsWith('/provider/work')
  if (path === '/provider/after-services') return pathname.startsWith('/provider/after-services') || pathname.startsWith('/provider/disputes')
  return pathname === path || pathname.startsWith(`${path}/`)
}
