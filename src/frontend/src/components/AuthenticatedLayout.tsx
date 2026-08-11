import type { PropsWithChildren } from 'react'
import { navigate } from '../auth/routing'
import { useAuthentication } from '../auth/AuthenticationContext'
import { ServiceFooter } from './ServiceFooter'
import { CustomerAppLayout } from '../customer/CustomerAppLayout'

export function AuthenticatedLayout({ children }: PropsWithChildren) {
  const { user, logout } = useAuthentication()
  if (window.location.pathname.startsWith('/customer')) {
    return <CustomerAppLayout>{children}</CustomerAppLayout>
  }
  const footerVariant = window.location.pathname.startsWith('/provider') ? 'provider' : null

  const handleLogout = async () => {
    await logout()
    navigate('/login', true)
  }

  return (
    <div className="appShell">
      <header className="topBar">
        <button className="brandButton" type="button" onClick={() => navigate('/roles')}>
          <span className="brandMark" aria-hidden="true">S</span>
          <span>SOODAL LIFE</span>
        </button>
        <div className="accountArea">
          <span className="accountName">{user?.loginId}</span>
          <button className="secondaryButton" type="button" onClick={handleLogout}>로그아웃</button>
        </div>
      </header>
      <main className="dashboardMain">{children}</main>
      {footerVariant && <ServiceFooter variant={footerVariant} />}
    </div>
  )
}
