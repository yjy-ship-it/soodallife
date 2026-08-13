import { useState } from 'react'
import type { PropsWithChildren } from 'react'
import { useAuthentication } from '../auth/AuthenticationContext'
import { navigate } from '../auth/routing'
import { ServiceFooter } from '../components/ServiceFooter'
import { adminMenuItems, findAdminMenu } from './menu'
import { AdminDirectPaymentStatus } from './AdminDirectPaymentStatus'

interface AdminLayoutProps extends PropsWithChildren {
  pathname: string
}

export function AdminLayout({ pathname, children }: AdminLayoutProps) {
  const { user, logout } = useAuthentication()
  const [menuOpen, setMenuOpen] = useState(false)
  const currentMenu = findAdminMenu(pathname)

  const goTo = (path: string) => {
    setMenuOpen(false)
    navigate(path)
  }

  const handleLogout = async () => {
    await logout()
    navigate('/login', true)
  }

  return (
    <div className="adminShell">
      <aside className={`adminSidebar ${menuOpen ? 'isOpen' : ''}`} aria-label="본사 관리자 업무 메뉴">
        <button className="adminBrand" type="button" onClick={() => goTo('/admin')}>
          <span className="adminBrandMark" aria-hidden="true">S</span>
          <span><strong>SOODAL LIFE</strong><small>수달 오피스</small></span>
        </button>
        <nav className="adminNav">
          {adminMenuItems
            .filter((item) => item.allowedRoles.some((role) => user?.roles.includes(role)))
            .map((item) => (
              <button
                className={item.path === currentMenu?.path ? 'active' : ''}
                key={item.path}
                type="button"
                onClick={() => goTo(item.path)}
              >
                <span className="adminNavMark" aria-hidden="true">{item.shortLabel.slice(0, 1)}</span>
                {item.label}
              </button>
            ))}
        </nav>
      </aside>

      {menuOpen && <button className="adminBackdrop" type="button" aria-label="메뉴 닫기" onClick={() => setMenuOpen(false)} />}

      <div className="adminWorkspace">
        <header className="adminTopBar">
          <div className="adminLocation">
            <button className="adminMenuToggle" type="button" aria-label="업무 메뉴 열기" onClick={() => setMenuOpen(true)}>메뉴</button>
            <span>수달 오피스</span>
            <strong>{currentMenu?.label ?? '관리자 업무'}</strong>
          </div>
          <div className="adminAccount">
            <button className="adminNotification" type="button" onClick={() => goTo('/admin/notifications')}>
              알림 <span>0</span>
            </button>
            <div className="adminIdentity">
              <strong>{user?.loginId}</strong>
              <span>본사 관리자</span>
            </div>
            <button className="adminLogout" type="button" onClick={handleLogout}>로그아웃</button>
          </div>
        </header>
        <main className="adminMain">{children}<AdminDirectPaymentStatus /></main>
        <ServiceFooter variant="admin" />
      </div>
    </div>
  )
}
