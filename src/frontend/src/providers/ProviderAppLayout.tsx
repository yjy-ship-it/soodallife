import { useEffect, useState, type PropsWithChildren, type ReactNode } from 'react'
import { useAuthentication } from '../auth/AuthenticationContext'
import { navigate } from '../auth/routing'
import { BrandLogo } from '../components/BrandLogo'
import { ServiceFooter } from '../components/ServiceFooter'
import { BusinessChatShortcut } from '../chat/BusinessChatShortcut'
import { TransactionDirectPaymentPanel } from '../work/TransactionDirectPaymentPanel'
import { getProviderWallet } from './walletApi'
import { getProviderTrust } from './trustApi'
import { getProviderDashboard } from './api'
import { ProviderRequestStatusPopup } from './ProviderRequestStatusPopup'
import { FriendShareButton } from '../components/FriendShareButton'
import { useServiceVisualSettings } from '../serviceVisuals/ServiceVisual'
import { MenuIcon, menuIconForPath, type MenuIconName } from '../components/MenuIcon'
import './provider.css'

const desktop = [
  ['/provider', '홈'], ['/provider/matched-requests', '받은 요청'], ['/provider/quotes', '내 견적 결과'], ['/provider/progress', '진행 업무'],
  ['/provider/schedule', '일정'],
] as const
const serviceMenu = [['/provider/care', '수달 케어·맞춤 제안'], ['/provider/interior', '수달 인테리어'], ['/provider/emergency', '긴급출동'], ['/provider/after-services', '사후관리·분쟁'], ['/provider/proposals', '제안·공동모집'], ['/provider/advertising', '광고 신청']] as const
const communityMenu = [['/help-room', '수달 도움방'], ['/suggestions', '수달 제안함']] as const
const accountMenu = [['/provider/onboarding', '기본정보·홍보'], ['/provider/services', '서비스 분야'], ['/provider/areas', '활동 지역'], ['/provider/documents', '자격·증빙'], ['/provider/approval', '승인 현황'], ['/provider/blocks', '고객 차단 현황'], ['/provider/support', '고객센터'], ['/provider/exit', '활동 종료']] as const
const mySoodalMenu = accountMenu.filter(([path]) => path !== '/provider/exit')
type MobileNavIconName = 'home' | 'request' | 'progress' | 'chat' | 'account'
const mobile: ReadonlyArray<readonly [string, string, MobileNavIconName]> = [['/provider', '홈', 'home'], ['/provider/matched-requests', '요청', 'request'], ['/provider/progress', '진행', 'progress'], ['/provider/communications', '소통', 'chat'], ['/provider/onboarding', '마이', 'account']]

export function ProviderAppLayout({ children, actions }: PropsWithChildren<{ actions?: ReactNode }>) {
  const { user, logout } = useAuthentication()
  const visualSettings = useServiceVisualSettings()
  const [unread, setUnread] = useState(0)
  const [chatUnread, setChatUnread] = useState(0)
  const [reviewUnread, setReviewUnread] = useState(0)
  const [walletBalance, setWalletBalance] = useState<number | null>(null)
  const [walletCurrency, setWalletCurrency] = useState('KRW')
  const [trust, setTrust] = useState<{ score:number|null; gradeLabel:string } | null>(null)
  const [approval, setApproval] = useState<{ approvalStatus:string; activityStatus:string; nextActions:string[]; rejectionReason:string|null } | null>(null)
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)
  const pathname = window.location.pathname
  const isFullyApproved = approval?.approvalStatus === 'APPROVED' && approval.activityStatus === 'ACTIVE'
  const isMySoodal = mySoodalMenu.some(([path]) => pathname === path || pathname.startsWith(`${path}/`))
  useEffect(() => { fetch('/api/v1/notifications/unread-count', { credentials: 'include' }).then(r => r.ok ? r.json() as Promise<{ count: number }> : Promise.reject()).then(v => setUnread(v.count)).catch(() => setUnread(0)) }, [pathname])
  useEffect(() => { fetch('/api/v1/chat/unread-count', { credentials: 'include' }).then(r => r.ok ? r.json() as Promise<{ count: number }> : Promise.reject()).then(v => setChatUnread(v.count)).catch(() => setChatUnread(0)) }, [pathname])
  useEffect(() => { const refresh=()=>{void fetch('/api/v1/providers/me/review-conversations/unread-count',{credentials:'include'}).then(r=>r.ok?r.json() as Promise<{count:number}>:Promise.reject()).then(v=>setReviewUnread(v.count)).catch(()=>setReviewUnread(0))};refresh();const timer=window.setInterval(refresh,30000);window.addEventListener('soodal-review-conversations-read',refresh);return()=>{window.clearInterval(timer);window.removeEventListener('soodal-review-conversations-read',refresh)}}, [pathname])
  useEffect(() => { getProviderWallet().then(wallet => { setWalletBalance(wallet.availableBalance); setWalletCurrency(wallet.currencyCode) }).catch(() => setWalletBalance(null)) }, [pathname])
  useEffect(() => { getProviderTrust().then(value => setTrust({ score:value.score, gradeLabel:value.gradeLabel })).catch(() => setTrust(null)) }, [pathname])
  useEffect(() => { getProviderDashboard().then(value => setApproval({ approvalStatus:value.approvalStatus, activityStatus:value.activityStatus, nextActions:value.nextActions, rejectionReason:value.rejectionReason })).catch(() => setApproval(null)) }, [pathname])
  useEffect(() => {
    const closeOutside = (event: PointerEvent) => {
      document.querySelectorAll<HTMLDetailsElement>('.providerHeader details[open]').forEach(details => {
        if (event.target instanceof Node && !details.contains(event.target)) details.removeAttribute('open')
      })
    }
    const closeWithEscape = (event: KeyboardEvent) => {
      if (event.key !== 'Escape') return
      closeProviderHeaderMenus()
      setMobileMenuOpen(false)
    }
    document.addEventListener('pointerdown', closeOutside)
    document.addEventListener('keydown', closeWithEscape)
    return () => { document.removeEventListener('pointerdown', closeOutside); document.removeEventListener('keydown', closeWithEscape) }
  }, [])
  useEffect(() => { setMobileMenuOpen(false); closeProviderHeaderMenus() }, [pathname])
  useEffect(() => {
    if (!mobileMenuOpen) return
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    return () => { document.body.style.overflow = previousOverflow }
  }, [mobileMenuOpen])
  const signOut = async () => { await logout(); navigate('/provider/start') }
  const goCustomerHome = () => window.location.assign('https://soodallife.kr/')
  const goFromMenu = (path: string) => { setMobileMenuOpen(false); navigate(path) }
  const communicationMenu = [['/provider/communications','소통 홈'],['/provider/messages',`업무 채팅${chatUnread?` (${chatUnread>99?'99+':chatUnread})`:''}`],['/provider/reviews',`고객 리뷰·답글${reviewUnread?` (${reviewUnread>99?'99+':reviewUnread})`:''}`]] as const
  return <div className={`providerAppShell${visualSettings.bannersEnabled?' serviceFeatureBannersEnabled':''}`}>
    <a className="skipLink" href="#provider-main">본문으로 바로가기</a>
    <header className="providerHeader"><div className="providerHeaderInner"><BrandLogo homePath="/provider" />
      <div className="providerBrandMeta"><span className="providerBrandLabel">수달 전문가</span><div className="providerBrandApprovalDesktop"><ApprovalStatus approval={approval}/></div>{isFullyApproved?<button className="providerBrandMobileWallet" type="button" onClick={()=>navigate('/provider/wallet')} aria-label={`이용료 잔액 ${formatHeaderBalance(walletBalance,walletCurrency)}`}>{formatHeaderBalance(walletBalance,walletCurrency)}</button>:<div className="providerBrandApprovalMobile"><ApprovalStatus approval={approval}/></div>}</div>
      <nav className="providerDesktopNav" aria-label="전문가 주요 메뉴">{desktop.map(([path, label]) => <button className={isDesktopActive(pathname, path) ? 'isActive' : ''} key={path} onClick={() => navigate(path)}>{visualSettings.iconsEnabled && <MenuIcon name={menuIconForPath(path)} />}{label}</button>)}
        <NavGroup label={`소통${chatUnread+reviewUnread?` (${Math.min(99,chatUnread+reviewUnread)}${chatUnread+reviewUnread>99?'+':''})`:''}`} icon="communication" pathname={pathname} items={communicationMenu} iconsEnabled={visualSettings.iconsEnabled} />
        <NavGroup label="서비스" icon="services" pathname={pathname} items={serviceMenu} iconsEnabled={visualSettings.iconsEnabled} />
        <NavGroup label="커뮤니티·지원" icon="community" pathname={pathname} items={communityMenu} iconsEnabled={visualSettings.iconsEnabled} />
        <NavGroup label="마이수달" icon="account" pathname={pathname} items={accountMenu} iconsEnabled={visualSettings.iconsEnabled} onLogout={() => void signOut()} />
      </nav>
      <ProviderRequestStatusPopup />
      <div className="providerHeaderActions">
        <button className="providerTrustBadge" type="button" onClick={() => navigate('/provider/trust')} aria-label={`나의 신뢰도 페이지로 이동, ${formatHeaderTrust(trust)}`}>{visualSettings.iconsEnabled && <MenuIcon name="trust" />}<span className="providerHeaderMetricText"><span>신뢰도</span><strong>{formatHeaderTrust(trust)}</strong></span></button>
        <button className="providerWalletBalance" type="button" onClick={() => navigate('/provider/wallet')} aria-label={`마이 수달 이용료 잔액으로 이동, 현재 잔액 ${formatHeaderBalance(walletBalance, walletCurrency)}`}>{visualSettings.iconsEnabled && <MenuIcon name="wallet" />}<span className="providerHeaderMetricText"><span>이용료 잔액</span><strong>{formatHeaderBalance(walletBalance, walletCurrency)}</strong></span></button>
        <button className="providerBell" onClick={() => navigate(window.matchMedia('(max-width: 680px)').matches?'/provider/schedule':'/provider/notifications')} aria-label={window.matchMedia('(max-width: 680px)').matches?'월간 일정 열기':`읽지 않은 알림 ${unread}개`}>{visualSettings.iconsEnabled && <MenuIcon name="notification" />}<span className="providerBellDesktopLabel">알림</span><span className="providerBellMobileLabel">일정</span>{unread > 0 && <span className="providerUnreadBadge">{unread > 99 ? '99+' : unread}</span>}</button>
        <FriendShareButton className="headerFriendShareButton" label="친구 추전" title="수달 라이프 전문가 등록" text="수달 라이프에서 전문가로 활동해 보세요." url="https://partner.soodallife.kr/provider/signup" />
        <button className="providerCustomerHomeButton" type="button" title={`로그인 계정: ${user?.loginId ?? ''}`} onClick={goCustomerHome}>{visualSettings.iconsEnabled && <MenuIcon name="home" />}고객 홈</button>
        <button className="providerMenuToggle" type="button" aria-expanded={mobileMenuOpen} aria-controls="provider-mobile-menu" aria-label={mobileMenuOpen ? '전체 메뉴 닫기' : '전체 메뉴 열기'} onClick={() => setMobileMenuOpen(value => !value)}>☰<span>전체 메뉴</span></button>
      </div>
    </div></header>
    {mobileMenuOpen && <><button className="providerMenuBackdrop" type="button" aria-label="전체 메뉴 닫기" onClick={() => setMobileMenuOpen(false)} /><aside id="provider-mobile-menu" className="providerMobileMenu" aria-label="전문가 전체 메뉴"><header><strong>수달 전문가 전체 메뉴</strong><button type="button" aria-label="전체 메뉴 닫기" onClick={() => setMobileMenuOpen(false)}>×</button></header><MenuSection title="업무" items={desktop} pathname={pathname} onSelect={goFromMenu} /><MenuSection title="소통" items={communicationMenu} pathname={pathname} onSelect={goFromMenu} /><MenuSection title="서비스" items={serviceMenu} pathname={pathname} onSelect={goFromMenu} /><MenuSection title="커뮤니티·지원" items={communityMenu} pathname={pathname} onSelect={goFromMenu} /><MenuSection title="마이수달" items={accountMenu} pathname={pathname} onSelect={goFromMenu} /><section><h2>추천·계정</h2><div className="providerMobileMenuGrid"><button type="button" onClick={() => goFromMenu('/provider/notifications')}>알림 {unread > 0 ? `${unread}개` : ''}</button><button type="button" onClick={() => goFromMenu('/provider/trust')}>신뢰도</button><button type="button" onClick={() => goFromMenu('/provider/wallet')}>이용료 잔액</button><button type="button" onClick={goCustomerHome}>고객 홈</button><FriendShareButton title="수달 라이프 전문가 등록" text="수달 라이프에서 전문가로 활동해 보세요." url="https://partner.soodallife.kr/provider/signup" /><button type="button" onClick={() => void signOut()}>로그아웃</button></div></section></aside></>}
    {isMySoodal && <nav className="providerMySoodalNav" aria-label="마이수달 설정 메뉴">{mySoodalMenu.map(([path,label])=><button type="button" className={isActive(pathname,path)?'isActive':''} key={path} onClick={()=>navigate(path)}>{visualSettings.iconsEnabled && <MenuIcon name={menuIconForPath(path)} />}{label}</button>)}</nav>}
    <main id="provider-main" className="providerMain" data-route-focus tabIndex={-1}>{actions}<BusinessChatShortcut />{children}<TransactionDirectPaymentPanel /></main>
    <ServiceFooter variant="provider" />
    <nav className="providerBottomNav" aria-label="모바일 전문가 메뉴">{mobile.map(([path, label, icon]) => <button key={path} className={isActive(pathname, path) ? 'isActive' : ''} aria-current={isActive(pathname,path)?'page':undefined} onClick={() => navigate(path)}>{visualSettings.iconsEnabled && <MobileNavIcon name={icon}/>}<span className="providerBottomNavLabel">{label}</span>{label === '소통' && chatUnread + reviewUnread > 0 && <b className="providerNavBadge">{chatUnread + reviewUnread > 99 ? '99+' : chatUnread + reviewUnread}</b>}</button>)}</nav>
  </div>
}

function MobileNavIcon({name}:{name:MobileNavIconName}) {
  const paths:Record<MobileNavIconName,ReactNode>={
    home:<><path d="M3.5 10.5 12 3.8l8.5 6.7"/><path d="M5.5 9.5v10h13v-10M9.2 19.5v-6h5.6v6"/></>,
    request:<><path d="M7 4.5h10a2 2 0 0 1 2 2v13H5v-13a2 2 0 0 1 2-2Z"/><path d="M9 4.5V3h6v1.5M8.5 10h7M8.5 14h5"/></>,
    progress:<><circle cx="12" cy="12" r="8.5"/><path d="M12 7.5V12l3.2 2M4.5 4.5l2 2M19.5 4.5l-2 2"/></>,
    chat:<><path d="M4 5.5h16v11H9l-5 3v-14Z"/><path d="M8 10h8M8 13h5"/></>,
    account:<><circle cx="12" cy="8" r="3.2"/><path d="M5.5 20c.6-4 2.8-6 6.5-6s5.9 2 6.5 6"/></>,
  }
  return <svg className="providerBottomNavIcon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">{paths[name]}</svg>
}

function ApprovalStatus({approval}:{approval:{approvalStatus:string;activityStatus:string;nextActions:string[];rejectionReason:string|null}|null}) {
  const code=approval?.approvalStatus??'LOADING';const active=approval?.activityStatus==='ACTIVE';const status=code==='APPROVED'?(active?'승인 완료':'승인 완료 · 활동 중지'):code==='REJECTED'?'승인 반려':code==='SUSPENDED'?'승인 정지':code==='PENDING'?'승인 심사 중':'승인 확인 중'
  const restrictions=code==='APPROVED'&&active?['승인 상태입니다. 서비스별 승인·활동지역·증빙·이용료 잔액 조건에 따라 견적을 제출할 수 있습니다.']:['고객 요청 매칭과 견적 제출·채택이 제한됩니다.','긴급출동 응답과 인테리어 전문가 참여가 제한됩니다.','신뢰도 최종 점수 산정은 전체 승인 후 진행됩니다.']
  return <details className={`providerApprovalStatus status-${code.toLowerCase()}`}><summary>{status}</summary><div className="providerApprovalPopover"><header><strong>전문가 승인 상태</strong><button type="button" aria-label="승인 상태 닫기" onClick={closeProviderHeaderMenus}>×</button></header><p>{approval?.rejectionReason?`반려 사유: ${approval.rejectionReason}`:status}</p><ul>{restrictions.map(item=><li key={item}>{item}</li>)}{approval?.nextActions.map(item=><li key={item}>{item}</li>)}</ul><button type="button" onClick={()=>navigate('/provider/approval')}>승인 현황</button></div></details>
}

function formatHeaderBalance(value: number | null, currency: string) {
  if (value === null) return '확인 중'
  return new Intl.NumberFormat('ko-KR', { style: 'currency', currency, maximumFractionDigits: 0 }).format(value)
}

function formatHeaderTrust(value: { score:number|null; gradeLabel:string } | null) {
  if (!value) return '확인 중'
  return value.score === null ? value.gradeLabel : `${new Intl.NumberFormat('ko-KR', { maximumFractionDigits: 1 }).format(value.score)}점 · ${value.gradeLabel}`
}

function NavGroup({ label, icon, pathname, items, iconsEnabled, onLogout }: { label: string; icon: MenuIconName; pathname: string; items: ReadonlyArray<readonly [string, string]>; iconsEnabled: boolean; onLogout?: () => void }) {
  const active = items.some(([path]) => isActive(pathname, path))
  return <details className={`providerNavGroup${active ? ' isActive' : ''}`}>
    <summary>{iconsEnabled && <MenuIcon name={icon} />}{label}</summary>
    <div><header><strong>{label}</strong><button type="button" aria-label={`${label} 메뉴 닫기`} onClick={closeProviderHeaderMenus}>×</button></header>{items.map(([path, itemLabel]) => <button type="button" className={isActive(pathname, path) ? 'isActive' : ''} key={path} onClick={() => navigate(path)}>{iconsEnabled && <MenuIcon name={menuIconForPath(path)} />}{itemLabel}</button>)}{onLogout && <button type="button" className="providerNavLogout" onClick={onLogout}>{iconsEnabled && <MenuIcon name="logout" />}로그아웃</button>}</div>
  </details>
}

function closeProviderHeaderMenus() {
  document.querySelectorAll<HTMLDetailsElement>('.providerHeader details[open]').forEach(details => details.removeAttribute('open'))
}

function MenuSection({ title, pathname, items, onSelect }: { title: string; pathname: string; items: ReadonlyArray<readonly [string, string]>; onSelect: (path: string) => void }) {
  return <section><h2>{title}</h2><div className="providerMobileMenuGrid">{items.map(([path, label]) => <button type="button" className={isActive(pathname, path) ? 'isActive' : ''} aria-current={isActive(pathname, path) ? 'page' : undefined} key={path} onClick={() => onSelect(path)}>{label}</button>)}</div></section>
}

function isActive(pathname:string, path:string) {
  if (path === '/provider') return pathname === path
  if (path === '/provider/matched-requests') return pathname.startsWith('/provider/matched-requests') || pathname.startsWith('/provider/quotes')
  if (path === '/provider/progress') return pathname.startsWith('/provider/progress') || pathname.startsWith('/provider/work')
  if (path === '/provider/communications') return pathname.startsWith('/provider/communications') || pathname.startsWith('/provider/messages') || pathname.startsWith('/provider/reviews')
  if (path === '/provider/after-services') return pathname.startsWith('/provider/after-services') || pathname.startsWith('/provider/disputes')
  return pathname === path || pathname.startsWith(`${path}/`)
}

function isDesktopActive(pathname:string,path:string) {
  if(path==='/provider/matched-requests')return pathname.startsWith('/provider/matched-requests')
  if(path==='/provider/quotes')return pathname.startsWith('/provider/quotes')
  return isActive(pathname,path)
}
