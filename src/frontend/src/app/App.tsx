import { useEffect, useState } from 'react'
import { AuthenticationProvider } from '../auth/AuthContext'
import { useAuthentication } from '../auth/AuthenticationContext'
import { getInitialAuthenticatedPath, navigate } from '../auth/routing'
import type { RoleCode } from '../auth/types'
import { LoginPage } from '../pages/LoginPage'
import { AccessDeniedPage, RoleHomePage, RoleSelectionPage } from '../pages/RolePages'
import { CustomerRequestDetailPage, CustomerRequestListPage, NewCustomerRequestPage } from '../pages/CustomerRequestPages'
import { ProviderAreaSettingsPage, ProviderMatchedRequestDetailPage, ProviderMatchedRequestListPage, ProviderServiceSettingsPage } from '../pages/ProviderPages'
import { CustomerWorkDetailPage, ProviderWorkDetailPage, WorkTransactionListPage } from '../pages/WorkPages'
import { AdminDashboardPage, AdminPlaceholderPage } from '../admin/AdminPages'
import { findAdminMenu } from '../admin/menu'
import './App.css'

const protectedRoutes: Record<string, RoleCode> = {
  '/customer': 'CUSTOMER',
  '/provider': 'PROVIDER',
  '/admin': 'ADMIN',
}

function usePathname() {
  const [pathname, setPathname] = useState(window.location.pathname)
  useEffect(() => {
    const updatePath = () => setPathname(window.location.pathname)
    window.addEventListener('popstate', updatePath)
    return () => window.removeEventListener('popstate', updatePath)
  }, [])
  return pathname
}

function ApplicationRoutes() {
  const pathname = usePathname()
  const { status, user } = useAuthentication()

  useEffect(() => {
    if (status === 'anonymous' && pathname !== '/login') {
      navigate('/login', true)
    } else if (status === 'authenticated' && user && (pathname === '/' || pathname === '/login')) {
      navigate(getInitialAuthenticatedPath(user), true)
    }
  }, [pathname, status, user])

  if (status === 'loading') {
    return <main className="loadingScreen" aria-live="polite">인증 상태를 확인하고 있습니다…</main>
  }

  if (status === 'anonymous') {
    return <LoginPage />
  }

  if (!user) {
    return null
  }

  if (pathname === '/roles') {
    return <RoleSelectionPage />
  }

  const customerRequestMatch = pathname.match(/^\/customer\/requests\/([0-9a-f-]+)$/i)
  const providerRequestMatch = pathname.match(/^\/provider\/matched-requests\/([0-9a-f-]+)$/i)
  const providerWorkMatch = pathname.match(/^\/provider\/work\/([0-9a-f-]+)$/i)
  const customerTransactionMatch = pathname.match(/^\/customer\/transactions\/([0-9a-f-]+)$/i)
  const requiredRole = protectedRoutes[pathname] ?? (pathname.startsWith('/customer/') ? 'CUSTOMER' : pathname.startsWith('/provider/') ? 'PROVIDER' : pathname.startsWith('/admin/') ? 'ADMIN' : undefined)
  if (requiredRole) {
    if (!user.roles.includes(requiredRole)) return <AccessDeniedPage />
    if (pathname === '/admin') return <AdminDashboardPage pathname={pathname} />
    if (requiredRole === 'ADMIN' && findAdminMenu(pathname)) return <AdminPlaceholderPage pathname={pathname} />
    if (pathname === '/customer/requests/new') return <NewCustomerRequestPage />
    if (pathname === '/customer/requests') return <CustomerRequestListPage />
    if (customerRequestMatch) return <CustomerRequestDetailPage requestId={customerRequestMatch[1]} />
    if (pathname === '/customer/transactions') return <WorkTransactionListPage audience="customer" />
    if (customerTransactionMatch) return <CustomerWorkDetailPage transactionId={customerTransactionMatch[1]} />
    if (pathname === '/provider/services') return <ProviderServiceSettingsPage />
    if (pathname === '/provider/areas') return <ProviderAreaSettingsPage />
    if (pathname === '/provider/matched-requests') return <ProviderMatchedRequestListPage />
    if (providerRequestMatch) return <ProviderMatchedRequestDetailPage requestId={providerRequestMatch[1]} />
    if (pathname === '/provider/work') return <WorkTransactionListPage audience="provider" />
    if (providerWorkMatch) return <ProviderWorkDetailPage transactionId={providerWorkMatch[1]} />
    return <RoleHomePage role={requiredRole} />
  }

  return <RoleSelectionPage />
}

function App() {
  return (
    <AuthenticationProvider>
      <ApplicationRoutes />
    </AuthenticationProvider>
  )
}

export default App
