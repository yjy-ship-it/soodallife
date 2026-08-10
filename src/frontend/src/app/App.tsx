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
import { AdminServiceCategoriesPage } from '../admin/AdminServiceCategoriesPage'
import { AdminProviderRequirementStandardsPage } from '../admin/AdminProviderRequirementStandardsPage'
import { AdminCustomersPage } from '../admin/AdminCustomersPage'
import { AdminProvidersPage } from '../admin/AdminProvidersPage'
import { AdminWalletsPage } from '../admin/AdminWalletsPage'
import { AdminRequestsPage, AdminTransactionsPage } from '../admin/AdminOperationsPages'
import { AdminAdvertisingContentPage } from '../admin/AdminAdvertisingContentPage'
import { AdminAfterServiceDisputePage } from '../admin/AdminAfterServiceDisputePage'
import { AdminTrustPage } from '../admin/AdminTrustPage'
import { AdminReviewsPage } from '../admin/AdminReviewsPage'
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
  const adminCustomerMatch = pathname.match(/^\/admin\/customers\/([0-9a-f-]+)$/i)
  const adminProviderMatch = pathname.match(/^\/admin\/providers\/([0-9a-f-]+)$/i)
  const adminWalletMatch = pathname.match(/^\/admin\/credits\/([0-9a-f-]+)$/i)
  const adminRequestMatch = pathname.match(/^\/admin\/requests\/([0-9a-f-]+)$/i)
  const adminTransactionMatch = pathname.match(/^\/admin\/transactions\/([0-9a-f-]+)$/i)
  const adminTrustMatch = pathname.match(/^\/admin\/trust\/([0-9a-f-]+)$/i)
  const adminReviewMatch = pathname.match(/^\/admin\/reviews\/([0-9a-f-]+)$/i)
  const requiredRole = protectedRoutes[pathname] ?? (pathname.startsWith('/customer/') ? 'CUSTOMER' : pathname.startsWith('/provider/') ? 'PROVIDER' : pathname.startsWith('/admin/') ? 'ADMIN' : undefined)
  if (requiredRole) {
    if (!user.roles.includes(requiredRole)) return <AccessDeniedPage />
    if (pathname === '/admin') return <AdminDashboardPage pathname={pathname} />
    if (pathname === '/admin/services') return <AdminServiceCategoriesPage pathname={pathname} />
    if (pathname === '/admin/provider-requirement-standards') return <AdminProviderRequirementStandardsPage pathname={pathname} />
    if (pathname === '/admin/customers') return <AdminCustomersPage pathname={pathname} />
    if (adminCustomerMatch) return <AdminCustomersPage pathname={pathname} customerId={adminCustomerMatch[1]} />
    if (pathname === '/admin/providers') return <AdminProvidersPage pathname={pathname} />
    if (adminProviderMatch) return <AdminProvidersPage pathname={pathname} providerId={adminProviderMatch[1]} />
    if (pathname === '/admin/credits') return <AdminWalletsPage pathname={pathname} />
    if (adminWalletMatch) return <AdminWalletsPage pathname={pathname} providerId={adminWalletMatch[1]} />
    if (pathname === '/admin/requests') return <AdminRequestsPage pathname={pathname} />
    if (adminRequestMatch) return <AdminRequestsPage pathname={pathname} requestId={adminRequestMatch[1]} />
    if (pathname === '/admin/transactions') return <AdminTransactionsPage pathname={pathname} />
    if (adminTransactionMatch) return <AdminTransactionsPage pathname={pathname} transactionId={adminTransactionMatch[1]} />
    if (pathname === '/admin/content') return <AdminAdvertisingContentPage pathname={pathname} />
    if (pathname === '/admin/disputes') return <AdminAfterServiceDisputePage pathname={pathname} />
    if (pathname === '/admin/trust') return <AdminTrustPage pathname={pathname} />
    if (adminTrustMatch) return <AdminTrustPage pathname={pathname} providerId={adminTrustMatch[1]} />
    if (pathname === '/admin/reviews') return <AdminReviewsPage pathname={pathname} />
    if (adminReviewMatch) return <AdminReviewsPage pathname={pathname} reviewId={adminReviewMatch[1]} />
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
