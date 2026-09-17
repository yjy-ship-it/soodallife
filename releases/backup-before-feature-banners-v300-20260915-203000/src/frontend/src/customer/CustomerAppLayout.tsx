import { useEffect, useState, type PropsWithChildren, type ReactNode } from 'react'
import { useAuthentication } from '../auth/AuthenticationContext'
import { createLoginPath, navigate } from '../auth/routing'
import { BrandLogo } from '../components/BrandLogo'
import { ServiceFooter } from '../components/ServiceFooter'
import { BusinessChatShortcut } from '../chat/BusinessChatShortcut'
import { TransactionDirectPaymentPanel } from '../work/TransactionDirectPaymentPanel'
import { getProviderProfile } from '../providers/api'
import { ProviderAppLayout } from '../providers/ProviderAppLayout'
import { AdminLayout } from '../admin/AdminLayout'
import { isSharedPortalPublicPath } from '../components/PublicPortalLayout'
import { FriendShareButton } from '../components/FriendShareButton'
import { DEFAULT_ADDRESS_CHANGED_EVENT, loadDefaultAddress } from './defaultAddress'
import { useServiceVisualSettings } from '../serviceVisuals/ServiceVisual'
import { MenuIcon, menuIconForPath, type MenuIconName } from '../components/MenuIcon'
import './customer.css'

type CustomerAppLayoutProps = PropsWithChildren<{ actions?: ReactNode }>

const navItems = [
  { path: '/', label: '홈', icon: 'home', public: true },
  { path: '/services', label: '서비스', icon: 'grid', public: true },
  { path: '/customer/requests', label: '요청·견적', icon: 'document', public: false },
  { path: '/customer/progress', label: '진행', icon: 'clock', public: false },
  { path: '/customer', label: '마이수달', icon: 'user', public: false },
] as const

const customerServiceMenu = [['/services', '서비스 전체'], ['/care', '수달 케어'], ['/interior', '수달 인테리어'], ['/emergency', '긴급출동']] as const
const customerCommunityMenu = [['/help-room', '수달 도움방'], ['/suggestions', '수달 제안함'], ['/support', '고객센터']] as const
const customerWorkMenu = [['/customer/requests', '요청·견적'], ['/customer/proposals', '전문가 제안·공동모집'], ['/customer/progress', '진행 업무'], ['/customer/care/contracts', '내 구독'], ['/customer/interior/projects', '내 인테리어'], ['/customer/service-history', '서비스 이력'], ['/customer/after-services', '사후관리'], ['/customer/disputes', '분쟁'], ['/customer/reports', '신고']] as const
const customerAccountLinks = [['/customer', '마이수달'], ['/customer/interested-services', '관심 서비스'], ['/customer/profile', '내 정보'], ['/customer/addresses', '주소 관리'], ['/customer/security', '로그인·보안'], ['/customer/notifications', '알림센터'], ['/customer/notification-settings', '알림 설정'], ['/customer/consents', '약관·동의'], ['/customer/provider-blocks', '차단한 전문가']] as const

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

function CustomerMenuSection({ title, pathname, items, onSelect }: { title: string; pathname: string; items: ReadonlyArray<readonly [string, string]>; onSelect: (path: string) => void }) {
  return <section><h2>{title}</h2><div className="customerMobileMenuGrid">{items.map(([path, label]) => { const active = pathname === path || pathname.startsWith(`${path}/`); return <button type="button" className={active ? 'isActive' : ''} aria-current={active ? 'page' : undefined} key={path} onClick={() => onSelect(path)}>{label}</button> })}</div></section>
}

function CustomerNavGroup({ label, icon, pathname, items, iconsEnabled }: { label: string; icon: MenuIconName; pathname: string; items: ReadonlyArray<readonly [string, string]>; iconsEnabled: boolean }) {
  const active = items.some(([path]) => pathname === path || pathname.startsWith(`${path}/`))
  return <details className={`customerNavGroup${active ? ' isActive' : ''}`}><summary>{iconsEnabled && <MenuIcon name={icon} />}{label}</summary><div><header><strong>{label}</strong><button type="button" aria-label={`${label} 메뉴 닫기`} onClick={closeCustomerHeaderMenus}>×</button></header>{items.map(([path, itemLabel]) => <button type="button" className={pathname === path || pathname.startsWith(`${path}/`) ? 'isActive' : ''} key={path} onClick={() => { closeCustomerHeaderMenus(); navigate(path) }}>{iconsEnabled && <MenuIcon name={menuIconForPath(path)} />}{itemLabel}</button>)}</div></details>
}

function closeCustomerHeaderMenus() {
  document.querySelectorAll<HTMLDetailsElement>('.customerHeader details[open]').forEach(details => details.removeAttribute('open'))
}

export function CustomerAppLayout({ children, actions }: CustomerAppLayoutProps) {
  const { status, user, logout } = useAuthentication()
  const visualSettings = useServiceVisualSettings()
  const pathname = window.location.pathname
  const isCustomer = user?.roles.includes('CUSTOMER') ?? false
  const [unreadCount, setUnreadCount] = useState(0)
  const [chatUnreadCount, setChatUnreadCount] = useState(0)
  const [reviewUnreadCount, setReviewUnreadCount] = useState(0)
  const [defaultRegion, setDefaultRegion] = useState('')
  const [providerSwitch, setProviderSwitch] = useState<{ label: string; href: string }>({ label: '전문가 등록', href: 'https://partner.soodallife.kr/provider/start' })
  const [online, setOnline] = useState(navigator.onLine)
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)

  useEffect(() => { const update = () => setOnline(navigator.onLine); window.addEventListener('online', update); window.addEventListener('offline', update); return () => { window.removeEventListener('online', update); window.removeEventListener('offline', update) } }, [])

  useEffect(() => {
    if (!isCustomer) return
    fetch('/api/v1/notifications/unread-count', { credentials: 'include' })
      .then(response => response.ok ? response.json() as Promise<{ count: number }> : Promise.reject())
      .then(value => setUnreadCount(value.count)).catch(() => setUnreadCount(0))
  }, [isCustomer, pathname])

  useEffect(() => {
    if (status !== 'authenticated') return
    if (!user?.roles.includes('PROVIDER')) {
      setProviderSwitch({ label: '전문가 등록', href: 'https://partner.soodallife.kr/provider/signup' })
      return
    }
    getProviderProfile()
      .then(profile => setProviderSwitch(profile.approvalStatus === 'APPROVED' && profile.activityStatus === 'ACTIVE'
        ? { label: '전문가 홈', href: 'https://partner.soodallife.kr/provider' }
        : { label: '전문가 등록 현황', href: 'https://partner.soodallife.kr/provider/onboarding' }))
      .catch(() => setProviderSwitch({ label: '전문가 등록 현황', href: 'https://partner.soodallife.kr/provider/onboarding' }))
  }, [status, user?.roles])

  useEffect(() => {
    if (!isCustomer) return
    fetch('/api/v1/chat/unread-count', { credentials: 'include' })
      .then(response => response.ok ? response.json() as Promise<{ count: number }> : Promise.reject())
      .then(value => setChatUnreadCount(value.count)).catch(() => setChatUnreadCount(0))
  }, [isCustomer, pathname])

  useEffect(() => {
    if (!isCustomer) return
    const refresh = () => { void fetch('/api/v1/customers/me/review-conversations/unread-count', { credentials: 'include' }).then(response => response.ok ? response.json() as Promise<{ count:number }> : Promise.reject()).then(value => setReviewUnreadCount(value.count)).catch(() => setReviewUnreadCount(0)) }
    refresh(); const timer=window.setInterval(refresh,30000); window.addEventListener('soodal-review-conversations-read', refresh)
    return () => { window.clearInterval(timer); window.removeEventListener('soodal-review-conversations-read', refresh) }
  }, [isCustomer, pathname])

  useEffect(() => {
    if (!isCustomer) { setDefaultRegion(''); return }
    const refresh = () => { void loadDefaultAddress().then(value => setDefaultRegion(value?.regionLabel ?? '')).catch(() => setDefaultRegion('')) }
    refresh()
    window.addEventListener(DEFAULT_ADDRESS_CHANGED_EVENT, refresh)
    return () => window.removeEventListener(DEFAULT_ADDRESS_CHANGED_EVENT, refresh)
  }, [isCustomer, pathname])

  useEffect(() => {
    const closeOutside = (event: PointerEvent) => {
      document.querySelectorAll<HTMLDetailsElement>('.customerHeader details[open]').forEach(details => {
        if (event.target instanceof Node && !details.contains(event.target)) details.removeAttribute('open')
      })
    }
    const closeWithEscape = (event: KeyboardEvent) => {
      if (event.key !== 'Escape') return
      closeCustomerHeaderMenus()
      setMobileMenuOpen(false)
    }
    document.addEventListener('pointerdown', closeOutside)
    document.addEventListener('keydown', closeWithEscape)
    return () => { document.removeEventListener('pointerdown', closeOutside); document.removeEventListener('keydown', closeWithEscape) }
  }, [])
  useEffect(() => { setMobileMenuOpen(false); closeCustomerHeaderMenus() }, [pathname])
  useEffect(() => {
    if (!mobileMenuOpen) return
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    return () => { document.body.style.overflow = previousOverflow }
  }, [mobileMenuOpen])

  const go = (path: string, isPublic: boolean) => {
    navigate(isPublic || isCustomer ? path : createLoginPath(path))
  }
  const signOut = async () => { await logout(); navigate('/') }
  const goProviderHome = () => providerSwitch.href.startsWith('http') ? window.location.assign(providerSwitch.href) : navigate(providerSwitch.href)
  const goFromMenu = (path: string, publicPath = false) => { setMobileMenuOpen(false); go(path, publicPath) }
  const communicationMenu = [['/customer/communications', '소통 홈'], ['/customer/messages', `업무 채팅${chatUnreadCount ? ` (${chatUnreadCount > 99 ? '99+' : chatUnreadCount})` : ''}`], ['/customer/reviews', `내 리뷰·댓글${reviewUnreadCount ? ` (${reviewUnreadCount > 99 ? '99+' : reviewUnreadCount})` : ''}`]] as const

  const sharedPublicPath = isSharedPortalPublicPath(pathname)
  const hostname = window.location.hostname.toLowerCase()
  if (sharedPublicPath && hostname.startsWith('partner.')) {
    return <ProviderAppLayout>{children}</ProviderAppLayout>
  }
  if (sharedPublicPath && hostname.startsWith('admin.')) {
    return <AdminLayout pathname={pathname}>{children}</AdminLayout>
  }

  return (
    <div className="customerAppShell">
      <a className="skipLink" href="#customer-main">본문으로 바로가기</a>
      <header className="customerHeader">
        <div className="customerHeaderInner">
          <BrandLogo />
          <nav className="customerDesktopNav" aria-label="고객 주요 메뉴">
            <button type="button" onClick={() => navigate('/services')}>{visualSettings.iconsEnabled && <MenuIcon name="services" />}서비스</button>
            <button type="button" onClick={() => navigate('/care')}>{visualSettings.iconsEnabled && <MenuIcon name="care" />}수달 케어</button>
            <button type="button" onClick={() => navigate('/interior')}>{visualSettings.iconsEnabled && <MenuIcon name="interior" />}수달 인테리어</button>
            <button type="button" className="emergencyNavButton" onClick={() => navigate('/emergency')}>{visualSettings.iconsEnabled && <MenuIcon name="emergency" />}긴급출동</button>
            {isCustomer && <button type="button" className={pathname.startsWith('/customer/communications') || pathname.startsWith('/customer/messages') || pathname.startsWith('/customer/reviews') ? 'isActive' : undefined} onClick={() => { closeCustomerHeaderMenus(); navigate('/customer/communications') }}>{visualSettings.iconsEnabled && <MenuIcon name="communication" />}{`소통${chatUnreadCount + reviewUnreadCount ? ` (${Math.min(99, chatUnreadCount + reviewUnreadCount)}${chatUnreadCount + reviewUnreadCount > 99 ? '+' : ''})` : ''}`}</button>}
            {isCustomer && <CustomerNavGroup label="내 업무" icon="work" pathname={pathname} items={customerWorkMenu} iconsEnabled={visualSettings.iconsEnabled} />}
            <CustomerNavGroup label="커뮤니티·지원" icon="community" pathname={pathname} items={customerCommunityMenu} iconsEnabled={visualSettings.iconsEnabled} />
          </nav>
          <div className="customerHeaderActions">
            {isCustomer && <button type="button" className="headerRegion" title={`기본주소 지역: ${defaultRegion || '미설정'} · 주소 관리로 이동`} onClick={() => navigate('/customer/addresses')}>{visualSettings.iconsEnabled && <MenuIcon name="address" />}{defaultRegion || '기본지역 미설정'}</button>}
            <button className="headerIconButton" type="button" onClick={() => navigate('/services/search')} aria-label="서비스 검색"><Icon name="search" /></button>
            {isCustomer && <button className="headerIconButton notificationBell" type="button" onClick={() => navigate('/customer/notifications')} aria-label={`알림 ${unreadCount}개`}><Icon name="bell" />{unreadCount > 0 && <span>{unreadCount > 99 ? '99+' : unreadCount}</span>}</button>}
            <FriendShareButton className="headerFriendShareButton" label="친구 추전" title="수달 라이프 회원가입" text="생활서비스가 필요할 때 수달 라이프를 이용해 보세요." url="https://soodallife.kr/signup" />
            <button className="customerProviderHomeButton" type="button" onClick={goProviderHome}>{visualSettings.iconsEnabled && <MenuIcon name="work" />}전문가 홈</button>
            {status === 'authenticated' ? <div className="customerAccountMenu"><button type="button" onClick={() => navigate(isCustomer ? '/customer' : '/roles')}>{visualSettings.iconsEnabled && <MenuIcon name="account" />}{isCustomer ? '마이수달' : '역할 선택'}</button></div> : <button className="customerLoginButton" type="button" onClick={() => navigate(createLoginPath(pathname))}>{visualSettings.iconsEnabled && <MenuIcon name="account" />}로그인</button>}
            <button className="customerMenuToggle" type="button" aria-expanded={mobileMenuOpen} aria-controls="customer-mobile-menu" aria-label={mobileMenuOpen ? '전체 메뉴 닫기' : '전체 메뉴 열기'} onClick={() => setMobileMenuOpen(value => !value)}>☰<span>전체 메뉴</span></button>
          </div>
        </div>
      </header>
      {mobileMenuOpen && <><button className="customerMenuBackdrop" type="button" aria-label="전체 메뉴 닫기" onClick={() => setMobileMenuOpen(false)} /><aside id="customer-mobile-menu" className="customerMobileMenu" aria-label="고객 전체 메뉴"><header><strong>수달 라이프 전체 메뉴</strong><button type="button" aria-label="전체 메뉴 닫기" onClick={() => setMobileMenuOpen(false)}>×</button></header><CustomerMenuSection title="서비스" items={customerServiceMenu} pathname={pathname} onSelect={path => goFromMenu(path, true)} />{isCustomer && <CustomerMenuSection title="소통" items={communicationMenu} pathname={pathname} onSelect={goFromMenu} />}<CustomerMenuSection title="커뮤니티·지원" items={customerCommunityMenu} pathname={pathname} onSelect={path => goFromMenu(path, true)} />{isCustomer && <CustomerMenuSection title="내 업무" items={customerWorkMenu} pathname={pathname} onSelect={goFromMenu} />}{isCustomer && <CustomerMenuSection title="마이수달" items={customerAccountLinks} pathname={pathname} onSelect={goFromMenu} />}<section><h2>추천·계정</h2><div className="customerMobileMenuGrid"><button type="button" onClick={goProviderHome}>전문가 홈</button><FriendShareButton title="수달 라이프 회원가입" text="생활서비스가 필요할 때 수달 라이프를 이용해 보세요." url="https://soodallife.kr/signup" />{status === 'authenticated' ? <button type="button" onClick={() => void signOut()}>로그아웃</button> : <button type="button" onClick={() => goFromMenu(createLoginPath(pathname), true)}>로그인</button>}</div></section></aside></>}
      {!online && <div className="customerOfflineNotice" role="status">인터넷 연결이 필요합니다. 저장·변경 작업은 온라인에서 다시 시도해 주세요.</div>}
      <main id="customer-main" className="customerMain" data-route-focus tabIndex={-1}>{actions}<BusinessChatShortcut />{children}<TransactionDirectPaymentPanel /></main>
      <ServiceFooter variant="customer" />
      <nav className="customerBottomNav" aria-label="모바일 고객 메뉴">
        {navItems.map(item => {
          const active = item.path === '/' ? pathname === '/' : pathname === item.path || pathname.startsWith(`${item.path}/`)
          return <button key={item.path} className={active ? 'isActive' : undefined} type="button" onClick={() => go(item.path, item.public)} aria-current={active ? 'page' : undefined}>{visualSettings.iconsEnabled && <Icon name={item.icon} />}<span>{item.label}</span></button>
        })}
      </nav>
    </div>
  )
}
