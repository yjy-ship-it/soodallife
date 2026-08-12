import { useEffect, useState, type PropsWithChildren, type ReactNode } from 'react'
import { useAuthentication } from '../auth/AuthenticationContext'
import { createLoginPath, navigate } from '../auth/routing'
import { BrandLogo } from '../components/BrandLogo'
import { ServiceFooter } from '../components/ServiceFooter'
import './customer.css'

type CustomerAppLayoutProps = PropsWithChildren<{ actions?: ReactNode }>

const navItems = [
  { path: '/', label: '홈', icon: 'home', public: true },
  { path: '/services', label: '서비스', icon: 'grid', public: true },
  { path: '/customer/requests', label: '요청·견적', icon: 'document', public: false },
  { path: '/customer/progress', label: '진행', icon: 'clock', public: false },
  { path: '/customer', label: '마이수달', icon: 'user', public: false },
] as const

function Icon({ name }: { name: string }) {
  const paths: Record<string, ReactNode> = {
    home: <><path d="M3 11.5 12 4l9 7.5"/><path d="M5.5 10.5V20h13v-9.5M9.5 20v-6h5v6"/></>,
    grid: <><rect x="4" y="4" width="6" height="6" rx="1"/><rect x="14" y="4" width="6" height="6" rx="1"/><rect x="4" y="14" width="6" height="6" rx="1"/><rect x="14" y="14" width="6" height="6" rx="1"/></>,
    document: <><path d="M7 3h7l4 4v14H7z"/><path d="M14 3v5h5M10 12h5M10 16h5"/></>,
    clock: <><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></>,
    user: <><circle cx="12" cy="8" r="4"/><path d="M4.5 21a7.5 7.5 0 0 1 15 0"/></>,
    search: <><circle cx="10.5" cy="10.5" r="6.5"/><path d="m16 16 4 4"/></>,
    bell: <><path d="M6 16h12l-1.5-2.5V10a4.5 4.5 0 0 0-9 0v3.5zM10 19h4"/></>,
  }
  return <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">{paths[name]}</svg>
}

export function CustomerAppLayout({ children, actions }: CustomerAppLayoutProps) {
  const { status, user, logout } = useAuthentication()
  const pathname = window.location.pathname
  const isCustomer = user?.roles.includes('CUSTOMER') ?? false
  const [unreadCount, setUnreadCount] = useState(0)
  const [chatUnreadCount, setChatUnreadCount] = useState(0)
  const [online, setOnline] = useState(navigator.onLine)

  useEffect(() => { const update = () => setOnline(navigator.onLine); window.addEventListener('online', update); window.addEventListener('offline', update); return () => { window.removeEventListener('online', update); window.removeEventListener('offline', update) } }, [])

  useEffect(() => {
    if (!isCustomer) return
    fetch('/api/v1/notifications/unread-count', { credentials: 'include' })
      .then(response => response.ok ? response.json() as Promise<{ count: number }> : Promise.reject())
      .then(value => setUnreadCount(value.count)).catch(() => setUnreadCount(0))
  }, [isCustomer, pathname])

  useEffect(() => {
    if (!isCustomer) return
    fetch('/api/v1/chat/unread-count', { credentials: 'include' })
      .then(response => response.ok ? response.json() as Promise<{ count: number }> : Promise.reject())
      .then(value => setChatUnreadCount(value.count)).catch(() => setChatUnreadCount(0))
  }, [isCustomer, pathname])

  const go = (path: string, isPublic: boolean) => {
    navigate(isPublic || isCustomer ? path : createLoginPath(path))
  }
  const signOut = async () => { await logout(); navigate('/') }

  return (
    <div className="customerAppShell">
      <a className="skipLink" href="#customer-main">본문으로 바로가기</a>
      <header className="customerHeader">
        <div className="customerHeaderInner">
          <BrandLogo />
          <nav className="customerDesktopNav" aria-label="고객 주요 메뉴">
            <button type="button" onClick={() => navigate('/services')}>서비스</button>
            <button type="button" onClick={() => navigate('/care')}>수달 케어</button>
            <button type="button" onClick={() => navigate('/interior')}>수달 인테리어</button>
            <button type="button" onClick={() => navigate('/emergency')}>긴급출동</button>
            <button type="button" onClick={() => navigate('/support')}>고객센터</button>
          </nav>
          <div className="customerHeaderActions">
            {isCustomer && <button className="customerLoginButton" type="button" onClick={() => navigate('/customer/messages')}>메시지{chatUnreadCount > 0 ? ` ${chatUnreadCount > 99 ? '99+' : chatUnreadCount}` : ''}</button>}
            <button className="headerLocation" type="button" disabled title="지역 선택 기능 준비 중">지역 선택</button>
            <button className="headerIconButton" type="button" onClick={() => navigate('/services/search')} aria-label="서비스 검색"><Icon name="search" /></button>
            {isCustomer && <button className="headerIconButton notificationBell" type="button" onClick={() => navigate('/customer/notifications')} aria-label={`알림 ${unreadCount}개`}><Icon name="bell" />{unreadCount > 0 && <span>{unreadCount > 99 ? '99+' : unreadCount}</span>}</button>}
            {status === 'authenticated' ? <div className="customerAccountMenu"><button type="button" onClick={() => navigate(isCustomer ? '/customer' : '/roles')}>{isCustomer ? '마이수달' : '역할 선택'}</button><button type="button" onClick={() => void signOut()}>로그아웃</button></div> : <button className="customerLoginButton" type="button" onClick={() => navigate(createLoginPath(pathname))}>로그인</button>}
          </div>
        </div>
      </header>
      {!online && <div className="customerOfflineNotice" role="status">인터넷 연결이 필요합니다. 저장·변경 작업은 온라인에서 다시 시도해 주세요.</div>}
      <main id="customer-main" className="customerMain">{actions}{children}</main>
      <ServiceFooter variant="customer" />
      <nav className="customerBottomNav" aria-label="모바일 고객 메뉴">
        {navItems.map(item => {
          const active = item.path === '/' ? pathname === '/' : pathname === item.path || pathname.startsWith(`${item.path}/`)
          return <button key={item.path} className={active ? 'isActive' : undefined} type="button" onClick={() => go(item.path, item.public)} aria-current={active ? 'page' : undefined}><Icon name={item.icon} /><span>{item.label}</span></button>
        })}
      </nav>
    </div>
  )
}
