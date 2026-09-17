import { useEffect, useState } from 'react'
import { AuthenticationProvider } from '../auth/AuthContext'
import { lazy, Suspense, type ComponentType } from 'react'
import { useAuthentication } from '../auth/AuthenticationContext'
import { createLoginPath, getInitialAuthenticatedPath, getSafeReturnUrl, navigate } from '../auth/routing'
import type { RoleCode } from '../auth/types'
import { findAdminMenu } from '../admin/menu'
import { serviceDomains } from '../config/serviceCompany'
import './App.css'

function lazyComponent<TModule, TComponent extends ComponentType<any>>(loader: () => Promise<TModule>, select: (module: TModule) => TComponent) {
  return lazy(async () => ({ default: select(await loader()) }))
}

function ErrorFocusManager() {
  useEffect(() => {
    let frame = 0
    let lastTarget: HTMLElement | null = null
    let lastText = ''
    const moveToLatestError = () => {
      const targets = Array.from(document.querySelectorAll<HTMLElement>('[role="alert"], .errorBanner, .accountError, .validationError, .providerAlert.error, .adminError'))
        .filter(element => element.isConnected && element.offsetParent !== null && Boolean(element.textContent?.trim()))
      const target = targets.at(-1)
      if (!target) return
      const text = target.textContent?.trim() ?? ''
      if (target === lastTarget && text === lastText) return
      lastTarget = target
      lastText = text
      if (!target.hasAttribute('tabindex')) target.setAttribute('tabindex', '-1')
      cancelAnimationFrame(frame)
      frame = requestAnimationFrame(() => {
        target.scrollIntoView({ behavior: 'smooth', block: 'center' })
        target.focus({ preventScroll: true })
      })
    }
    const observer = new MutationObserver(moveToLatestError)
    observer.observe(document.body, { childList: true, subtree: true, characterData: true })
    moveToLatestError()
    return () => { observer.disconnect(); cancelAnimationFrame(frame) }
  }, [])
  return null
}

const LoginPage = lazyComponent(() => import('../pages/LoginPage'), module => module.LoginPage)
const AccessDeniedPage = lazyComponent(() => import('../pages/RolePages'), module => module.AccessDeniedPage)
const RoleHomePage = lazyComponent(() => import('../pages/RolePages'), module => module.RoleHomePage)
const RoleSelectionPage = lazyComponent(() => import('../pages/RolePages'), module => module.RoleSelectionPage)
const CustomerRequestDetailPage = lazyComponent(() => import('../pages/CustomerRequestPages'), module => module.CustomerRequestDetailPage)
const CustomerRequestListPage = lazyComponent(() => import('../pages/CustomerRequestPages'), module => module.CustomerRequestListPage)
const NewCustomerRequestPage = lazyComponent(() => import('../pages/CustomerRequestPages'), module => module.NewCustomerRequestPage)
const ProviderAreaSettingsPage = lazyComponent(() => import('../pages/ProviderPages'), module => module.ProviderAreaSettingsPage)
const ProviderMatchedRequestDetailPage = lazyComponent(() => import('../pages/ProviderPages'), module => module.ProviderMatchedRequestDetailPage)
const ProviderMatchedRequestListPage = lazyComponent(() => import('../pages/ProviderPages'), module => module.ProviderMatchedRequestListPage)
const ProviderServiceSettingsPage = lazyComponent(() => import('../pages/ProviderPages'), module => module.ProviderServiceSettingsPage)
const ProviderApprovalPage = lazyComponent(() => import('../providers/ProviderOnboardingPages'), module => module.ProviderApprovalPage)
const ProviderDocumentsPage = lazyComponent(() => import('../providers/ProviderOnboardingPages'), module => module.ProviderDocumentsPage)
const ProviderProfilePage = lazyComponent(() => import('../providers/ProviderOnboardingPages'), module => module.ProviderProfilePage)
const ProviderPublicStartPage = lazyComponent(() => import('../providers/ProviderOnboardingPages'), module => module.ProviderPublicStartPage)
const ProviderSignupPage = lazyComponent(() => import('../providers/ProviderOnboardingPages'), module => module.ProviderSignupPage)
const ProviderQuoteListPage = lazyComponent(() => import('../providers/ProviderOperations'), module => module.ProviderQuoteListPage)
const ProviderInboxPage = lazyComponent(() => import('../providers/ProviderOperationsHubPages'), module => module.ProviderInboxPage)
const ProviderOperationsHubHomePage = lazyComponent(() => import('../providers/ProviderOperationsHubPages'), module => module.ProviderOperationsHubHomePage)
const ProviderSchedulePage = lazyComponent(() => import('../providers/ProviderOperationsHubPages'), module => module.ProviderSchedulePage)
const ProviderAfterServiceDetailPage = lazyComponent(() => import('../providers/ProviderAftercarePages'), module => module.ProviderAfterServiceDetailPage)
const ProviderAfterServiceListPage = lazyComponent(() => import('../providers/ProviderAftercarePages'), module => module.ProviderAfterServiceListPage)
const ProviderDisputeDetailPage = lazyComponent(() => import('../providers/ProviderAftercarePages'), module => module.ProviderDisputeDetailPage)
const ProviderDisputeListPage = lazyComponent(() => import('../providers/ProviderAftercarePages'), module => module.ProviderDisputeListPage)
const ProviderWalletPage = lazyComponent(() => import('../providers/ProviderWalletPage'), module => module.ProviderWalletPage)
const ProviderTrustPage = lazyComponent(() => import('../providers/ProviderTrustPage'), module => module.ProviderTrustPage)
const ProviderExitPage = lazyComponent(() => import('../providers/ProviderExitPage'), module => module.ProviderExitPage)
const ProviderBlocksPage = lazyComponent(() => import('../providers/ProviderBlocksPage'), module => module.ProviderBlocksPage)
const ProviderSupportPage = lazyComponent(() => import('../providers/ProviderSupportPage'), module => module.ProviderSupportPage)
const ProviderAdvertisingPage = lazyComponent(() => import('../providers/ProviderAdvertisingPage'), module => module.ProviderAdvertisingPage)
const CustomerReviewConversationPage = lazyComponent(() => import('../reviews/ReviewConversationPages'), module => module.CustomerReviewConversationPage)
const ProviderReviewConversationPage = lazyComponent(() => import('../reviews/ReviewConversationPages'), module => module.ProviderReviewConversationPage)
const CustomerCommunicationHubPage = lazyComponent(() => import('../reviews/CommunicationHubPages'), module => module.CustomerCommunicationHubPage)
const ProviderCommunicationHubPage = lazyComponent(() => import('../reviews/CommunicationHubPages'), module => module.ProviderCommunicationHubPage)
const ProviderCareApplicationsPage = lazyComponent(() => import('../providers/ProviderCarePages'), module => module.ProviderCareApplicationsPage)
const ProviderCareContractsPage = lazyComponent(() => import('../providers/ProviderCarePages'), module => module.ProviderCareContractsPage)
const ProviderCareHomePage = lazyComponent(() => import('../providers/ProviderCarePages'), module => module.ProviderCareHomePage)
const ProviderCareRequestsPage = lazyComponent(() => import('../providers/ProviderCarePages'), module => module.ProviderCareRequestsPage)
const ProviderCareScheduleChangesPage = lazyComponent(() => import('../providers/ProviderCarePages'), module => module.ProviderCareScheduleChangesPage)
const ProviderCareVisitsPage = lazyComponent(() => import('../providers/ProviderCarePages'), module => module.ProviderCareVisitsPage)
const ProviderInteriorHomePage = lazyComponent(() => import('../providers/ProviderInteriorPages'), module => module.ProviderInteriorHomePage)
const ProviderInteriorProjectsPage = lazyComponent(() => import('../providers/ProviderInteriorPages'), module => module.ProviderInteriorProjectsPage)
const CustomerWorkDetailPage = lazyComponent(() => import('../pages/WorkPages'), module => module.CustomerWorkDetailPage)
const ProviderWorkDetailPage = lazyComponent(() => import('../pages/WorkPages'), module => module.ProviderWorkDetailPage)
const WorkTransactionListPage = lazyComponent(() => import('../pages/WorkPages'), module => module.WorkTransactionListPage)
const CustomerDisputesPage = lazyComponent(() => import('../pages/CustomerWorkHistoryPages'), module => module.CustomerDisputesPage)
const AfterServiceDisputeFormPage = lazyComponent(() => import('../pages/CustomerAftercarePages'), module => module.AfterServiceDisputeFormPage)
const AfterServicesPage = lazyComponent(() => import('../pages/CustomerAftercarePages'), module => module.AfterServicesPage)
const CustomerReportsPage = lazyComponent(() => import('../pages/CustomerAftercarePages'), module => module.CustomerReportsPage)
const ServiceHistoryPage = lazyComponent(() => import('../pages/CustomerAftercarePages'), module => module.ServiceHistoryPage)
const AdminDashboardPage = lazyComponent(() => import('../admin/AdminPages'), module => module.AdminDashboardPage)
const AdminPlaceholderPage = lazyComponent(() => import('../admin/AdminPages'), module => module.AdminPlaceholderPage)
const AdminServiceCategoriesPage = lazyComponent(() => import('../admin/AdminServiceCategoriesPage'), module => module.AdminServiceCategoriesPage)
const AdminProviderRequirementStandardsPage = lazyComponent(() => import('../admin/AdminProviderRequirementStandardsPage'), module => module.AdminProviderRequirementStandardsPage)
const AdminCustomersPage = lazyComponent(() => import('../admin/AdminCustomersPage'), module => module.AdminCustomersPage)
const AdminCustomerWithdrawalPage = lazyComponent(() => import('../admin/AdminCustomerWithdrawalPage'), module => module.AdminCustomerWithdrawalPage)
const AdminUserBlocksPage = lazyComponent(() => import('../admin/AdminUserBlocksPage'), module => module.AdminUserBlocksPage)
const AdminProvidersPage = lazyComponent(() => import('../admin/AdminProvidersPage'), module => module.AdminProvidersPage)
const AdminWalletsPage = lazyComponent(() => import('../admin/AdminWalletsPage'), module => module.AdminWalletsPage)
const AdminProviderExitPage = lazyComponent(() => import('../admin/AdminProviderExitPage'), module => module.AdminProviderExitPage)
const AdminRequestsPage = lazyComponent(() => import('../admin/AdminOperationsPages'), module => module.AdminRequestsPage)
const AdminTransactionsPage = lazyComponent(() => import('../admin/AdminOperationsPages'), module => module.AdminTransactionsPage)
const AdminAdvertisingContentPage = lazyComponent(() => import('../admin/AdminAdvertisingContentPage'), module => module.AdminAdvertisingContentPage)
const AdminProviderCampaignsPage = lazyComponent(() => import('../admin/AdminProviderCampaignsPage'), module => module.AdminProviderCampaignsPage)
const AdminAfterServiceDisputePage = lazyComponent(() => import('../admin/AdminAfterServiceDisputePage'), module => module.AdminAfterServiceDisputePage)
const AdminTrustPage = lazyComponent(() => import('../admin/AdminTrustPage'), module => module.AdminTrustPage)
const AdminReviewsPage = lazyComponent(() => import('../admin/AdminReviewsPage'), module => module.AdminReviewsPage)
const AdminCaseManagementPage = lazyComponent(() => import('../admin/AdminCaseManagementPage'), module => module.AdminCaseManagementPage)
const AdminSubscriptionsPage = lazyComponent(() => import('../admin/AdminSubscriptionsPage'), module => module.AdminSubscriptionsPage)
const AdminInteriorPage = lazyComponent(() => import('../admin/AdminInteriorPage'), module => module.AdminInteriorPage)
const AdminNotificationsV114Page = lazyComponent(() => import('../admin/AdminNotificationsV114Page'), module => module.AdminNotificationsV114Page)
const AdminSystemPage = lazyComponent(() => import('../admin/AdminSystemPage'), module => module.AdminSystemPage)
const AdminMatchingPage = lazyComponent(() => import('../admin/AdminMatchingPage'), module => module.AdminMatchingPage)
const AdminSettlementOperationsPage = lazyComponent(() => import('../admin/AdminSettlementOperationsPage'), module => module.AdminSettlementOperationsPage)
const CustomerHomePage = lazyComponent(() => import('../customer/CustomerPages'), module => module.CustomerHomePage)
const CompanyInfoPage = lazyComponent(() => import('../customer/CustomerPages'), module => module.CompanyInfoPage)
const CustomerNotFoundPage = lazyComponent(() => import('../customer/CustomerPages'), module => module.CustomerNotFoundPage)
const CustomerSupportPage = lazyComponent(() => import('../customer/CustomerPages'), module => module.CustomerSupportPage)
const PublicContentPage = lazyComponent(() => import('../customer/CustomerPages'), module => module.PublicContentPage)
const ServiceCatalogPage = lazyComponent(() => import('../customer/CustomerPages'), module => module.ServiceCatalogPage)
const ServiceDetailPage = lazyComponent(() => import('../customer/CustomerPages'), module => module.ServiceDetailPage)
const ServiceSearchPage = lazyComponent(() => import('../customer/CustomerPages'), module => module.ServiceSearchPage)
const CustomerAddressesPage = lazyComponent(() => import('../customer/CustomerAccountPages'), module => module.CustomerAddressesPage)
const CustomerConsentsPage = lazyComponent(() => import('../customer/CustomerAccountPages'), module => module.CustomerConsentsPage)
const CustomerNotificationSettingsPage = lazyComponent(() => import('../customer/CustomerAccountPages'), module => module.CustomerNotificationSettingsPage)
const CustomerNotificationCenterPage = lazyComponent(() => import('../customer/CustomerAccountPages'), module => module.CustomerNotificationCenterPage)
const CustomerProfilePage = lazyComponent(() => import('../customer/CustomerAccountPages'), module => module.CustomerProfilePage)
const CustomerSecurityPage = lazyComponent(() => import('../customer/CustomerAccountPages'), module => module.CustomerSecurityPage)
const CustomerSignupPage = lazyComponent(() => import('../customer/CustomerAccountPages'), module => module.CustomerSignupPage)
const LegalDocumentsPage = lazyComponent(() => import('../customer/CustomerAccountPages'), module => module.LegalDocumentsPage)
const MySoodalPage = lazyComponent(() => import('../customer/CustomerAccountPages'), module => module.MySoodalPage)
const CustomerInterestedServicesPage = lazyComponent(() => import('../customer/CustomerAccountPages'), module => module.CustomerInterestedServicesPage)
const CustomerProviderBlocksPage = lazyComponent(() => import('../customer/CustomerAccountPages'), module => module.CustomerProviderBlocksPage)
const PasswordResetRequestPage = lazyComponent(() => import('../customer/CustomerAccountPages'), module => module.PasswordResetRequestPage)
const CustomerWithdrawalPage = lazyComponent(() => import('../customer/CustomerWithdrawalPage'), module => module.CustomerWithdrawalPage)
const CustomerCareContractsPage = lazyComponent(() => import('../customer/CustomerCarePages'), module => module.CustomerCareContractsPage)
const CustomerCareHomePage = lazyComponent(() => import('../customer/CustomerCarePages'), module => module.CustomerCareHomePage)
const CustomerCarePaymentsPage = lazyComponent(() => import('../customer/CustomerCarePages'), module => module.CustomerCarePaymentsPage)
const NewCustomerCareRequestPage = lazyComponent(() => import('../customer/CustomerCarePages'), module => module.NewCustomerCareRequestPage)
const CustomerCareRequestsPage = lazyComponent(() => import('../customer/CustomerCarePages'), module => module.CustomerCareRequestsPage)
const CustomerCareVisitsPage = lazyComponent(() => import('../customer/CustomerCarePages'), module => module.CustomerCareVisitsPage)
const CustomerProgressPage = lazyComponent(() => import('../customer/CustomerCarePages'), module => module.CustomerProgressPage)
const CustomerInteriorHomePage = lazyComponent(() => import('../customer/CustomerInteriorPages'), module => module.CustomerInteriorHomePage)
const CustomerInteriorProjectPage = lazyComponent(() => import('../customer/CustomerInteriorPages'), module => module.CustomerInteriorProjectPage)
const CustomerInteriorProjectsPage = lazyComponent(() => import('../customer/CustomerInteriorPages'), module => module.CustomerInteriorProjectsPage)
const NewCustomerInteriorProjectPage = lazyComponent(() => import('../customer/CustomerInteriorPages'), module => module.NewCustomerInteriorProjectPage)
const ChatRoomListPage = lazyComponent(() => import('../chat/ChatPages'), module => module.ChatRoomListPage)
const ChatRoomPage = lazyComponent(() => import('../chat/ChatPages'), module => module.ChatRoomPage)
const CustomerEmergencyHomePage = lazyComponent(() => import('../emergency/EmergencyPages'), module => module.CustomerEmergencyHomePage)
const CustomerEmergencyProgressPage = lazyComponent(() => import('../emergency/EmergencyPages'), module => module.CustomerEmergencyProgressPage)
const ProviderEmergencyPage = lazyComponent(() => import('../emergency/EmergencyPages'), module => module.ProviderEmergencyPage)
const ProviderEmergencyProgressPage = lazyComponent(() => import('../emergency/EmergencyPages'), module => module.ProviderEmergencyProgressPage)
const PublicProviderProfilePage = lazyComponent(() => import('../customer/PublicProviderProfilePage'), module => module.PublicProviderProfilePage)
const CustomerProposalsPage = lazyComponent(() => import('../proposals/ProposalPages'), module => module.CustomerProposalsPage)
const ProviderProposalsPage = lazyComponent(() => import('../proposals/ProposalPages'), module => module.ProviderProposalsPage)
const HelpRoomPage = lazyComponent(() => import('../help/HelpRoomPages'), module => module.HelpRoomPage)
const NewHelpPostPage = lazyComponent(() => import('../help/HelpRoomPages'), module => module.NewHelpPostPage)
const SuggestionBoxPage = lazyComponent(() => import('../help/HelpRoomPages'), module => module.SuggestionBoxPage)
const AdminSuggestionsPage = lazyComponent(() => import('../admin/AdminSuggestionsPage'), module => module.AdminSuggestionsPage)
const AdminHelpRoomPhotosPage = lazyComponent(() => import('../admin/AdminHelpRoomPhotosPage'), module => module.AdminHelpRoomPhotosPage)

const protectedRoutes: Record<string, RoleCode> = {
  '/customer': 'CUSTOMER',
  '/provider': 'PROVIDER',
  '/admin': 'ADMIN',
}

function usePathname() {
  const [locationKey, setLocationKey] = useState(`${window.location.pathname}${window.location.search}`)
  useEffect(() => {
    const updatePath = () => setLocationKey(`${window.location.pathname}${window.location.search}`)
    const restorePath = (event: PageTransitionEvent) => { if (event.persisted) updatePath() }
    window.addEventListener('popstate', updatePath)
    window.addEventListener('pageshow', restorePath)
    return () => { window.removeEventListener('popstate', updatePath); window.removeEventListener('pageshow', restorePath) }
  }, [])
  return locationKey.split('?')[0]
}

function ApplicationRoutes() {
  const pathname = usePathname()
  const { status, user, logout } = useAuthentication()
  const hostname = window.location.hostname.toLowerCase()
  const isAdminHost = hostname.startsWith('admin.')
  const isPartnerHost = hostname.startsWith('partner.')
  const isPartnerRoot = pathname === '/' && hostname.startsWith('partner.')
  const isAdminRoot = pathname === '/' && isAdminHost
  const hasAdminHostRoleMismatch = status === 'authenticated' && Boolean(user) && isAdminHost && !user!.roles.includes('ADMIN')
  const hasProviderRole = status === 'authenticated' && Boolean(user?.roles.includes('PROVIDER'))
  const isProviderRegistrationPath = pathname === '/provider/start' || pathname === '/provider/signup'
  const isPartnerCommunityPath = pathname === '/help-room' || pathname.startsWith('/help-room/') || pathname === '/suggestions'
  const isAllowedPartnerPath = pathname === '/login' || pathname === '/password-reset' || pathname.startsWith('/provider') || isPartnerCommunityPath

  useEffect(() => {
    if (isPartnerHost && !isAllowedPartnerPath) {
      window.location.replace(`${serviceDomains.customer}${pathname}${window.location.search}`)
    }
  }, [isAllowedPartnerPath, isPartnerHost, pathname])

  useEffect(() => {
    const frame = window.requestAnimationFrame(() => {
      window.scrollTo({ top: 0, left: 0, behavior: 'auto' })
      document.querySelector<HTMLElement>('main[data-route-focus]')?.focus({ preventScroll: true })
    })
    return () => window.cancelAnimationFrame(frame)
  }, [pathname])

  // The customer, provider, and admin sites share one frontend bundle. Keep
  // each domain's root entry on the correct service instead of rendering the
  // customer home for every host. Perform navigation in an effect so every
  // render executes the same hooks and React cannot abort with a hook-order
  // error while the root path changes.
  useEffect(() => {
    if (isPartnerRoot) {
      if (status === 'authenticated' && user?.roles.includes('PROVIDER')) {
        navigate('/provider', true)
      } else if (status !== 'loading') {
        navigate('/provider/start', true)
      }
    } else if (isAdminRoot) {
      navigate('/admin', true)
    }
  }, [isAdminRoot, isPartnerRoot, status, user])

  useEffect(() => {
    if (hasProviderRole && isProviderRegistrationPath) {
      navigate('/provider', true)
    }
  }, [hasProviderRole, isProviderRegistrationPath])

  useEffect(() => {
    if (!hasAdminHostRoleMismatch) return
    void logout().finally(() => navigate('/login?returnUrl=%2Fadmin', true))
  }, [hasAdminHostRoleMismatch, logout])

  useEffect(() => {
    if (status === 'authenticated' && user && pathname === '/login' && !hasAdminHostRoleMismatch) {
      navigate(getSafeReturnUrl() ?? getInitialAuthenticatedPath(user), true)
    }
  }, [hasAdminHostRoleMismatch, pathname, status, user])

  if (isPartnerRoot) {
    return <main className="loadingScreen" aria-live="polite">전문가 등록 여부를 확인하고 있습니다.</main>
  }
  if (isPartnerHost && !isAllowedPartnerPath) {
    return <main className="loadingScreen" aria-live="polite">고객 서비스 화면으로 이동하고 있습니다.</main>
  }
  if (isAdminRoot) {
    return <main className="loadingScreen" aria-live="polite">관리자 서비스로 이동하고 있습니다.</main>
  }
  if (hasAdminHostRoleMismatch) {
    return <main className="loadingScreen" aria-live="polite">관리자 계정을 확인하고 있습니다.</main>
  }
  if (hasProviderRole && isProviderRegistrationPath) {
    return <main className="loadingScreen" aria-live="polite">이미 등록된 전문가 계정입니다. 전문가 홈으로 이동하고 있습니다.</main>
  }

  const serviceDetailMatch = pathname.match(/^\/services\/([0-9a-f-]+)$/i)
  const serviceSlugMatch = pathname.match(/^\/services\/([a-z0-9][a-z0-9-]{1,219})$/)
  const publicProviderMatch = pathname.match(/^\/providers\/([0-9a-f-]+)$/i)
  const noticeDetailMatch = pathname.match(/^\/notices\/([0-9a-f-]+)$/i)
  const faqDetailMatch = pathname.match(/^\/faq\/([0-9a-f-]+)$/i)
  const proposalDetailMatch = pathname.match(/^\/proposals\/([0-9a-f-]+)$/i)
  const helpDetailMatch = pathname.match(/^\/help-room\/([0-9a-f-]+)$/i)
  if (pathname === '/') return <CustomerHomePage />
  if (pathname === '/care') return <CustomerCareHomePage />
  if (pathname === '/customer/care') return <CustomerCareHomePage />
  if (pathname === '/interior') return <CustomerInteriorHomePage />
  if (pathname === '/emergency') return <CustomerEmergencyHomePage />
  if (pathname === '/proposals') return <CustomerProposalsPage />
  if (pathname === '/customer/proposals') return <CustomerProposalsPage mineOnly />
  if (proposalDetailMatch) return <CustomerProposalsPage id={proposalDetailMatch[1]} />
  if (pathname === '/services') return <ServiceCatalogPage />
  if (pathname === '/services/search') return <ServiceSearchPage />
  if (serviceDetailMatch && serviceDetailMatch[1].length >= 32) return <ServiceDetailPage id={serviceDetailMatch[1]} />
  if (serviceSlugMatch) return <ServiceDetailPage slug={serviceSlugMatch[1]} />
  if (publicProviderMatch) return <PublicProviderProfilePage providerId={publicProviderMatch[1]} />
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
  if (pathname === '/policies/general-payment-refund') return <LegalDocumentsPage code="GENERAL_PAYMENT_REFUND_POLICY" />
  if (pathname === '/policies/subscription-refund') return <LegalDocumentsPage code="SUBSCRIPTION_REFUND_POLICY" />
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
  if (pathname === '/help-room') return <HelpRoomPage />
  if (pathname === '/help-room/new') return user.roles.includes('CUSTOMER') ? <NewHelpPostPage /> : <AccessDeniedPage />
  if (helpDetailMatch) return <HelpRoomPage id={helpDetailMatch[1]} />
  if (pathname === '/suggestions') return <SuggestionBoxPage />

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
  const adminCustomerWithdrawalMatch = pathname.match(/^\/admin\/customers\/withdrawals\/([0-9a-f-]+)$/i)
  const adminProviderMatch = pathname.match(/^\/admin\/providers\/(?:business\/|individual\/)?([0-9a-f-]+)$/i)
  const adminWalletMatch = pathname.match(/^\/admin\/credits\/([0-9a-f-]+)$/i)
  const adminProviderExitMatch = pathname.match(/^\/admin\/provider-exits\/([0-9a-f-]+)$/i)
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
    if (pathname === '/customer/interested-services') return <CustomerInterestedServicesPage />
    if (pathname === '/customer/profile') return <CustomerProfilePage />
    if (pathname === '/customer/addresses') return <CustomerAddressesPage />
    if (pathname === '/customer/security') return <CustomerSecurityPage />
    if (pathname === '/customer/security/withdrawal') return <CustomerWithdrawalPage />
    if (pathname === '/customer/consents') return <CustomerConsentsPage />
    if (pathname === '/customer/notification-settings') return <CustomerNotificationSettingsPage />
    if (pathname === '/customer/provider-blocks') return <CustomerProviderBlocksPage />
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
    if (pathname === '/admin/customers/withdrawals') return <AdminCustomerWithdrawalPage pathname={pathname} />
    if (pathname === '/admin/user-blocks') return <AdminUserBlocksPage pathname={pathname} />
    if (adminCustomerWithdrawalMatch) return <AdminCustomerWithdrawalPage pathname={pathname} id={adminCustomerWithdrawalMatch[1]} />
    if (adminCustomerMatch) return <AdminCustomersPage pathname={pathname} customerId={adminCustomerMatch[1]} />
    if (pathname === '/admin/providers' || pathname === '/admin/providers/business' || pathname === '/admin/providers/individual') return <AdminProvidersPage pathname={pathname} />
    if (adminProviderMatch) return <AdminProvidersPage pathname={pathname} providerId={adminProviderMatch[1]} />
    if (pathname === '/admin/credits') return <AdminWalletsPage pathname={pathname} />
    if (adminWalletMatch) return <AdminWalletsPage pathname={pathname} providerId={adminWalletMatch[1]} />
    if (pathname === '/admin/provider-exits') return <AdminProviderExitPage pathname={pathname} />
    if (adminProviderExitMatch) return <AdminProviderExitPage pathname={pathname} id={adminProviderExitMatch[1]} />
    if (pathname === '/admin/requests') return <AdminRequestsPage pathname={pathname} />
    if (adminRequestMatch) return <AdminRequestsPage pathname={pathname} requestId={adminRequestMatch[1]} />
    if (pathname === '/admin/transactions') return <AdminTransactionsPage pathname={pathname} />
    if (adminTransactionMatch) return <AdminTransactionsPage pathname={pathname} transactionId={adminTransactionMatch[1]} />
    if (pathname === '/admin/content') return <AdminAdvertisingContentPage pathname={pathname} />
    if (pathname === '/admin/provider-campaigns') return <AdminProviderCampaignsPage pathname={pathname} />
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
    if (pathname === '/admin/notifications') return <AdminNotificationsV114Page pathname={pathname} />
    if (pathname === '/admin/system') return <AdminSystemPage pathname={pathname} />
    if (pathname === '/admin/matching') return <AdminMatchingPage pathname={pathname} />
    if (pathname === '/admin/settlements') return <AdminSettlementOperationsPage pathname={pathname} />
    if (pathname === '/admin/suggestions') return <AdminSuggestionsPage pathname={pathname} />
    if (pathname === '/admin/help-room-photos') return <AdminHelpRoomPhotosPage pathname={pathname} />
    if (requiredRole === 'ADMIN' && findAdminMenu(pathname)) return <AdminPlaceholderPage pathname={pathname} />
    if (pathname === '/customer/requests/new') return <NewCustomerRequestPage />
    if (pathname === '/customer/requests') return <CustomerRequestListPage />
    if (customerRequestMatch) return <CustomerRequestDetailPage requestId={customerRequestMatch[1]} />
    if (pathname === '/customer/transactions') return <WorkTransactionListPage audience="customer" />
    if (customerTransactionMatch) return <CustomerWorkDetailPage transactionId={customerTransactionMatch[1]} />
    if (customerEmergencyMatch) return <CustomerEmergencyProgressPage id={customerEmergencyMatch[1]} />
    if (pathname === '/customer/communications') return <CustomerCommunicationHubPage />
    if (pathname === '/customer/reviews') return <CustomerReviewConversationPage />
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
    if (pathname === '/provider/advertising') return <ProviderAdvertisingPage />
    if (pathname === '/provider/proposals') return <ProviderProposalsPage />
    if (pathname === '/provider/trust') return <ProviderTrustPage />
    if (pathname === '/provider/exit') return <ProviderExitPage />
    if (pathname === '/provider/blocks') return <ProviderBlocksPage />
    if (pathname === '/provider/support') return <ProviderSupportPage />
    if (pathname === '/provider/communications') return <ProviderCommunicationHubPage />
    if (pathname === '/provider/reviews') return <ProviderReviewConversationPage />
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
    if (pathname === '/provider/notifications') return <CustomerNotificationCenterPage audience="provider" />
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
      <ErrorFocusManager />
      <Suspense fallback={<main className="loadingScreen" aria-live="polite">화면을 준비하고 있습니다.</main>}>
        <ApplicationRoutes />
      </Suspense>
    </AuthenticationProvider>
  )
}

export default App
