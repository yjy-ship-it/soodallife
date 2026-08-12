import { useEffect, useState } from 'react'
import { AuthenticationProvider } from '../auth/AuthContext'
import { useAuthentication } from '../auth/AuthenticationContext'
import { createLoginPath, getInitialAuthenticatedPath, getSafeReturnUrl, navigate } from '../auth/routing'
import type { RoleCode } from '../auth/types'
import { LoginPage } from '../pages/LoginPage'
import { AccessDeniedPage, RoleHomePage, RoleSelectionPage } from '../pages/RolePages'
import { CustomerRequestDetailPage, CustomerRequestListPage, NewCustomerRequestPage } from '../pages/CustomerRequestPages'
import { ProviderAreaSettingsPage, ProviderMatchedRequestDetailPage, ProviderMatchedRequestListPage, ProviderServiceSettingsPage } from '../pages/ProviderPages'
import { ProviderApprovalPage, ProviderDocumentsPage, ProviderProfilePage, ProviderPublicStartPage, ProviderSignupPage } from '../providers/ProviderOnboardingPages'
import { ProviderQuoteListPage } from '../providers/ProviderOperations'
import { ProviderInboxPage, ProviderOperationsHubHomePage, ProviderSchedulePage } from '../providers/ProviderOperationsHubPages'
import { ProviderAfterServiceDetailPage, ProviderAfterServiceListPage, ProviderDisputeDetailPage, ProviderDisputeListPage } from '../providers/ProviderAftercarePages'
import { ProviderWalletPage } from '../providers/ProviderWalletPage'
import { ProviderCareApplicationsPage, ProviderCareContractsPage, ProviderCareHomePage, ProviderCareRequestsPage, ProviderCareScheduleChangesPage, ProviderCareVisitsPage } from '../providers/ProviderCarePages'
import { ProviderInteriorHomePage, ProviderInteriorProjectsPage } from '../providers/ProviderInteriorPages'
import { CustomerWorkDetailPage, ProviderWorkDetailPage, WorkTransactionListPage } from '../pages/WorkPages'
import { CustomerDisputesPage, MyReviewsPage } from '../pages/CustomerWorkHistoryPages'
import { AfterServiceDisputeFormPage, AfterServicesPage, CustomerReportsPage, ServiceHistoryPage } from '../pages/CustomerAftercarePages'
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
import { AdminCaseManagementPage } from '../admin/AdminCaseManagementPage'
import { AdminSubscriptionsPage } from '../admin/AdminSubscriptionsPage'
import { AdminInteriorPage } from '../admin/AdminInteriorPage'
import { AdminNotificationsPage } from '../admin/AdminNotificationsPage'
import { AdminSystemPage } from '../admin/AdminSystemPage'
import {
  CustomerHomePage,
  CompanyInfoPage,
  CustomerNotFoundPage,
  CustomerSupportPage,
  PublicContentPage,
  ServiceCatalogPage,
  ServiceDetailPage,
  ServiceSearchPage,
} from '../customer/CustomerPages'
import {
  CustomerAddressesPage,
  CustomerConsentsPage,
  CustomerNotificationSettingsPage,
  CustomerNotificationCenterPage,
  CustomerProfilePage,
  CustomerSecurityPage,
  CustomerSignupPage,
  LegalDocumentsPage,
  MySoodalPage,
  PasswordResetRequestPage,
} from '../customer/CustomerAccountPages'
import {
  CustomerCareContractsPage,
  CustomerCareHomePage,
  CustomerCarePaymentsPage,
  NewCustomerCareRequestPage,
  CustomerCareRequestsPage,
  CustomerCareVisitsPage,
  CustomerProgressPage,
} from '../customer/CustomerCarePages'
import { CustomerInteriorHomePage, CustomerInteriorProjectPage, CustomerInteriorProjectsPage, NewCustomerInteriorProjectPage } from '../customer/CustomerInteriorPages'
import { ChatRoomListPage, ChatRoomPage } from '../chat/ChatPages'
import { CustomerEmergencyHomePage, CustomerEmergencyProgressPage, ProviderEmergencyPage, ProviderEmergencyProgressPage } from '../emergency/EmergencyPages'
import './App.css'

const protectedRoutes: Record<string, RoleCode> = {
  '/customer': 'CUSTOMER',
  '/provider': 'PROVIDER',
  '/admin': 'ADMIN',
}

function usePathname() {
  const [locationKey, setLocationKey] = useState(`${window.location.pathname}${window.location.search}`)
  useEffect(() => {
    const updatePath = () => setLocationKey(`${window.location.pathname}${window.location.search}`)
    window.addEventListener('popstate', updatePath)
    return () => window.removeEventListener('popstate', updatePath)
  }, [])
  return locationKey.split('?')[0]
}

function ApplicationRoutes() {
  const pathname = usePathname()
  const { status, user } = useAuthentication()

  useEffect(() => {
    if (status === 'authenticated' && user && pathname === '/login') {
      navigate(getSafeReturnUrl() ?? getInitialAuthenticatedPath(user), true)
    }
  }, [pathname, status, user])

  const serviceDetailMatch = pathname.match(/^\/services\/([0-9a-f-]+)$/i)
  const noticeDetailMatch = pathname.match(/^\/notices\/([0-9a-f-]+)$/i)
  const faqDetailMatch = pathname.match(/^\/faq\/([0-9a-f-]+)$/i)
  if (pathname === '/') return <CustomerHomePage />
  if (pathname === '/care') return <CustomerCareHomePage />
  if (pathname === '/interior') return <CustomerInteriorHomePage />
  if (pathname === '/emergency') return <CustomerEmergencyHomePage />
  if (pathname === '/services') return <ServiceCatalogPage />
  if (pathname === '/services/search') return <ServiceSearchPage />
  if (serviceDetailMatch) return <ServiceDetailPage id={serviceDetailMatch[1]} />
  if (pathname === '/notices') return <PublicContentPage type="NOTICE" />
  if (noticeDetailMatch) return <PublicContentPage type="NOTICE" id={noticeDetailMatch[1]} />
  if (pathname === '/faq') return <PublicContentPage type="FAQ" />
  if (faqDetailMatch) return <PublicContentPage type="FAQ" id={faqDetailMatch[1]} />
  if (pathname === '/support') return <CustomerSupportPage />
  if (pathname === '/company') return <CompanyInfoPage />
  if (pathname === '/policies') return <LegalDocumentsPage />
  if (pathname === '/policies/terms') return <LegalDocumentsPage code="TERMS_OF_SERVICE" />
  if (pathname === '/policies/privacy') return <LegalDocumentsPage code="PRIVACY_POLICY" />
  if (pathname === '/policies/location') return <LegalDocumentsPage code="LOCATION_SERVICE_TERMS" />
  if (pathname === '/policies/electronic-finance') return <LegalDocumentsPage code="ELECTRONIC_FINANCE_GUIDE" />
  if (pathname === '/signup') return <CustomerSignupPage />
  if (pathname === '/password-reset') return <PasswordResetRequestPage />
  if (pathname === '/provider/start') return <ProviderPublicStartPage />
  if (pathname === '/provider/signup') return <ProviderSignupPage />

  if (status === 'loading') {
    return <main className="loadingScreen" aria-live="polite">인증 상태를 확인하고 있습니다…</main>
  }

  if (status === 'anonymous') {
    if (pathname === '/login') return <LoginPage />
    const currentPath = `${pathname}${window.location.search}`
    navigate(createLoginPath(currentPath), true)
    return <main className="loadingScreen" aria-live="polite">로그인 화면으로 이동하고 있습니다.</main>
  }

  if (!user) {
    return null
  }

  if (pathname === '/roles') {
    return <RoleSelectionPage />
  }

  const customerRequestMatch = pathname.match(/^\/customer\/requests\/([0-9a-f-]+)$/i)
  const customerCareRequestMatch = pathname.match(/^\/customer\/care\/requests\/([0-9a-f-]+)$/i)
  const customerCareContractMatch = pathname.match(/^\/customer\/care\/contracts\/([0-9a-f-]+)$/i)
  const customerCareVisitMatch = pathname.match(/^\/customer\/care\/visits\/([0-9a-f-]+)$/i)
  const customerInteriorProjectMatch = pathname.match(/^\/customer\/interior\/projects\/([0-9a-f-]+)$/i)
  const providerRequestMatch = pathname.match(/^\/provider\/matched-requests\/([0-9a-f-]+)$/i)
  const providerWorkMatch = pathname.match(/^\/provider\/work\/([0-9a-f-]+)$/i)
  const providerAfterServiceMatch = pathname.match(/^\/provider\/after-services\/([0-9a-f-]+)$/i)
  const providerDisputeMatch = pathname.match(/^\/provider\/disputes\/([0-9a-f-]+)$/i)
  const providerCareContractMatch = pathname.match(/^\/provider\/care\/contracts\/([0-9a-f-]+)$/i)
  const providerCareVisitMatch = pathname.match(/^\/provider\/care\/visits\/([0-9a-f-]+)$/i)
  const providerInteriorProjectMatch = pathname.match(/^\/provider\/interior\/projects\/([0-9a-f-]+)$/i)
  const providerChatMatch = pathname.match(/^\/provider\/messages\/([0-9a-f-]+)$/i)
  const providerEmergencyMatch = pathname.match(/^\/provider\/emergency\/([0-9a-f-]+)$/i)
  const customerChatMatch = pathname.match(/^\/customer\/messages\/([0-9a-f-]+)$/i)
  const customerTransactionMatch = pathname.match(/^\/customer\/transactions\/([0-9a-f-]+)$/i)
  const customerEmergencyMatch = pathname.match(/^\/customer\/emergency\/([0-9a-f-]+)$/i)
  const customerDisputeMatch = pathname.match(/^\/customer\/disputes\/([0-9a-f-]+)$/i)
  const customerHistoryMatch = pathname.match(/^\/customer\/service-history\/([0-9a-f-]+)$/i)
  const customerAfterServiceMatch = pathname.match(/^\/customer\/after-services\/([0-9a-f-]+)$/i)
  const customerAfterServiceDisputeMatch = pathname.match(/^\/customer\/after-services\/([0-9a-f-]+)\/dispute$/i)
  const customerReportMatch = pathname.match(/^\/customer\/reports\/([0-9a-f-]+)$/i)
  const adminCustomerMatch = pathname.match(/^\/admin\/customers\/([0-9a-f-]+)$/i)
  const adminProviderMatch = pathname.match(/^\/admin\/providers\/([0-9a-f-]+)$/i)
  const adminWalletMatch = pathname.match(/^\/admin\/credits\/([0-9a-f-]+)$/i)
  const adminRequestMatch = pathname.match(/^\/admin\/requests\/([0-9a-f-]+)$/i)
  const adminTransactionMatch = pathname.match(/^\/admin\/transactions\/([0-9a-f-]+)$/i)
  const adminTrustMatch = pathname.match(/^\/admin\/trust\/([0-9a-f-]+)$/i)
  const adminReviewMatch = pathname.match(/^\/admin\/reviews\/([0-9a-f-]+)$/i)
  const adminReportMatch = pathname.match(/^\/admin\/reports\/([0-9a-f-]+)$/i)
  const adminSanctionMatch = pathname.match(/^\/admin\/sanctions\/([0-9a-f-]+)$/i)
  const adminInteriorMatch = pathname.match(/^\/admin\/interior\/([0-9a-f-]+)$/i)
  const requiredRole = protectedRoutes[pathname] ?? (pathname.startsWith('/customer/') ? 'CUSTOMER' : pathname.startsWith('/provider/') ? 'PROVIDER' : pathname.startsWith('/admin/') ? 'ADMIN' : undefined)
  if (requiredRole) {
    if (!user.roles.includes(requiredRole)) return <AccessDeniedPage />
    if (pathname === '/customer') return <MySoodalPage />
    if (pathname === '/customer/profile') return <CustomerProfilePage />
    if (pathname === '/customer/addresses') return <CustomerAddressesPage />
    if (pathname === '/customer/security') return <CustomerSecurityPage />
    if (pathname === '/customer/consents') return <CustomerConsentsPage />
    if (pathname === '/customer/notification-settings') return <CustomerNotificationSettingsPage />
    if (pathname === '/customer/notifications') return <CustomerNotificationCenterPage />
    if (pathname === '/customer/messages') return <ChatRoomListPage audience="customer" />
    if (customerChatMatch) return <ChatRoomPage audience="customer" id={customerChatMatch[1]} />
    if (pathname === '/customer/progress') return <CustomerProgressPage />
    if (pathname === '/customer/care/request/new') return <NewCustomerCareRequestPage />
    if (pathname === '/customer/care/requests') return <CustomerCareRequestsPage />
    if (customerCareRequestMatch) return <CustomerCareRequestsPage id={customerCareRequestMatch[1]} />
    if (pathname === '/customer/care/contracts') return <CustomerCareContractsPage />
    if (customerCareContractMatch) return <CustomerCareContractsPage id={customerCareContractMatch[1]} />
    if (pathname === '/customer/care/visits') return <CustomerCareVisitsPage />
    if (customerCareVisitMatch) return <CustomerCareVisitsPage id={customerCareVisitMatch[1]} />
    if (pathname === '/customer/care/payments') return <CustomerCarePaymentsPage />
    if (pathname === '/customer/interior/projects') return <CustomerInteriorProjectsPage />
    if (pathname === '/customer/interior/projects/new') return <NewCustomerInteriorProjectPage />
    if (customerInteriorProjectMatch) return <CustomerInteriorProjectPage id={customerInteriorProjectMatch[1]} />
    if (pathname === '/admin' || pathname === '/admin/analytics') return <AdminDashboardPage pathname={pathname} />
    if (pathname === '/admin/services' || pathname === '/admin/pricing') return <AdminServiceCategoriesPage pathname={pathname} />
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
    if (pathname === '/admin/reports') return <AdminCaseManagementPage pathname={pathname} mode="reports" />
    if (adminReportMatch) return <AdminCaseManagementPage pathname={pathname} mode="reports" id={adminReportMatch[1]} />
    if (pathname === '/admin/sanctions') return <AdminCaseManagementPage pathname={pathname} mode="sanctions" />
    if (adminSanctionMatch) return <AdminCaseManagementPage pathname={pathname} mode="sanctions" id={adminSanctionMatch[1]} />
    if (pathname === '/admin/case-policies') return <AdminCaseManagementPage pathname={pathname} mode="policies" />
    if (pathname === '/admin/subscriptions') return <AdminSubscriptionsPage pathname={pathname} />
    if (pathname === '/admin/interior') return <AdminInteriorPage pathname={pathname} />
    if (adminInteriorMatch) return <AdminInteriorPage pathname={pathname} projectId={adminInteriorMatch[1]} />
    if (pathname === '/admin/notifications') return <AdminNotificationsPage pathname={pathname} />
    if (pathname === '/admin/system') return <AdminSystemPage pathname={pathname} />
    if (requiredRole === 'ADMIN' && findAdminMenu(pathname)) return <AdminPlaceholderPage pathname={pathname} />
    if (pathname === '/customer/requests/new') return <NewCustomerRequestPage />
    if (pathname === '/customer/requests') return <CustomerRequestListPage />
    if (customerRequestMatch) return <CustomerRequestDetailPage requestId={customerRequestMatch[1]} />
    if (pathname === '/customer/transactions') return <WorkTransactionListPage audience="customer" />
    if (customerTransactionMatch) return <CustomerWorkDetailPage transactionId={customerTransactionMatch[1]} />
    if (customerEmergencyMatch) return <CustomerEmergencyProgressPage id={customerEmergencyMatch[1]} />
    if (pathname === '/customer/reviews') return <MyReviewsPage />
    if (pathname === '/customer/service-history') return <ServiceHistoryPage />
    if (customerHistoryMatch) return <ServiceHistoryPage id={customerHistoryMatch[1]} />
    if (pathname === '/customer/after-services') return <AfterServicesPage />
    if (pathname === '/customer/after-services/new') return <AfterServicesPage mode="new" />
    if (customerAfterServiceDisputeMatch) return <AfterServiceDisputeFormPage id={customerAfterServiceDisputeMatch[1]} />
    if (customerAfterServiceMatch) return <AfterServicesPage id={customerAfterServiceMatch[1]} />
    if (pathname === '/customer/disputes') return <CustomerDisputesPage />
    if (customerDisputeMatch) return <CustomerDisputesPage id={customerDisputeMatch[1]} />
    if (pathname === '/customer/reports') return <CustomerReportsPage />
    if (pathname === '/customer/reports/new') return <CustomerReportsPage mode="new" />
    if (customerReportMatch) return <CustomerReportsPage id={customerReportMatch[1]} />
    if (pathname === '/provider/services') return <ProviderServiceSettingsPage />
    if (pathname === '/provider/areas') return <ProviderAreaSettingsPage />
    if (pathname === '/provider') return <ProviderOperationsHubHomePage />
    if (pathname === '/provider/inbox') return <ProviderInboxPage />
    if (pathname === '/provider/progress') return <ProviderInboxPage progressOnly />
    if (pathname === '/provider/schedule') return <ProviderSchedulePage />
    if (pathname === '/provider/quotes') return <ProviderQuoteListPage />
    if (pathname === '/provider/wallet') return <ProviderWalletPage />
    if (pathname === '/provider/care') return <ProviderCareHomePage />
    if (pathname === '/provider/care/requests') return <ProviderCareRequestsPage />
    if (pathname === '/provider/care/applications') return <ProviderCareApplicationsPage />
    if (pathname === '/provider/care/contracts') return <ProviderCareContractsPage />
    if (providerCareContractMatch) return <ProviderCareContractsPage id={providerCareContractMatch[1]} />
    if (pathname === '/provider/care/visits') return <ProviderCareVisitsPage />
    if (providerCareVisitMatch) return <ProviderCareVisitsPage id={providerCareVisitMatch[1]} />
    if (pathname === '/provider/care/schedule-changes') return <ProviderCareScheduleChangesPage />
    if (pathname === '/provider/interior') return <ProviderInteriorHomePage />
    if (pathname === '/provider/emergency') return <ProviderEmergencyPage />
    if (providerEmergencyMatch) return <ProviderEmergencyProgressPage id={providerEmergencyMatch[1]} />
    if (pathname === '/provider/interior/projects') return <ProviderInteriorProjectsPage />
    if (providerInteriorProjectMatch) return <ProviderInteriorProjectsPage id={providerInteriorProjectMatch[1]} />
    if (pathname === '/provider/onboarding') return <ProviderProfilePage />
    if (pathname === '/provider/documents') return <ProviderDocumentsPage />
    if (pathname === '/provider/approval') return <ProviderApprovalPage />
    if (pathname === '/provider/notifications') return <CustomerNotificationCenterPage />
    if (pathname === '/provider/messages') return <ChatRoomListPage audience="provider" />
    if (providerChatMatch) return <ChatRoomPage audience="provider" id={providerChatMatch[1]} />
    if (pathname === '/provider/matched-requests') return <ProviderMatchedRequestListPage />
    if (providerRequestMatch) return <ProviderMatchedRequestDetailPage requestId={providerRequestMatch[1]} />
    if (pathname === '/provider/work') return <WorkTransactionListPage audience="provider" />
    if (providerWorkMatch) return <ProviderWorkDetailPage transactionId={providerWorkMatch[1]} />
    if (pathname === '/provider/after-services') return <ProviderAfterServiceListPage />
    if (providerAfterServiceMatch) return <ProviderAfterServiceDetailPage id={providerAfterServiceMatch[1]} />
    if (pathname === '/provider/disputes') return <ProviderDisputeListPage />
    if (providerDisputeMatch) return <ProviderDisputeDetailPage id={providerDisputeMatch[1]} />
    return <RoleHomePage role={requiredRole} />
  }

  return <CustomerNotFoundPage />
}

function App() {
  return (
    <AuthenticationProvider>
      <ApplicationRoutes />
    </AuthenticationProvider>
  )
}

export default App
