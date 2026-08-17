import { useEffect, useState, type PropsWithChildren, type ReactNode } from 'react'
import { useAuthentication } from '../auth/AuthenticationContext'
import { navigate } from '../auth/routing'
import { BrandLogo } from '../components/BrandLogo'
import { ServiceFooter } from '../components/ServiceFooter'
import { BusinessChatShortcut } from '../chat/BusinessChatShortcut'
import { TransactionDirectPaymentPanel } from '../work/TransactionDirectPaymentPanel'
import { getProviderWallet } from './walletApi'
import { getProviderTrust } from './trustApi'
import './provider.css'

const desktop = [
  ['/provider', '홈'], ['/provider/matched-requests', '요청·견적'], ['/provider/progress', '진행 업무'],
  ['/provider/schedule', '일정'], ['/provider/messages', '채팅'],
] as const
const serviceMenu = [['/provider/care', '수달 케어'], ['/provider/interior', '수달 인테리어'], ['/provider/emergency', '긴급출동'], ['/provider/after-services', 'A/S·분쟁']] as const
const accountMenu = [['/provider/wallet', '충전금'], ['/provider/trust', '신뢰도'], ['/provider/onboarding', '마이수달'], ['/provider/exit', '활동 종료']] as const
const mobile: ReadonlyArray<readonly [string, string]> = [['/provider', '홈'], ['/provider/matched-requests', '요청'], ['/provider/progress', '진행'], ['/provider/messages', '채팅'], ['/provider/onboarding', '마이']]

export function ProviderAppLayout({ children, actions }: PropsWithChildren<{ actions?: ReactNode }>) {
  const { user, logout } = useAuthentication()
  const [unread, setUnread] = useState(0)
  const [chatUnread, setChatUnread] = useState(0)
  const [walletBalance, setWalletBalance] = useState<number | null>(null)
  const [walletCurrency, setWalletCurrency] = useState('KRW')
  const [trust, setTrust] = useState<{ score:number|null; gradeLabel:string } | null>(null)
  const pathname = window.location.pathname
  useEffect(() => { fetch('/api/v1/notifications/unread-count', { credentials: 'include' }).then(r => r.ok ? r.json() as Promise<{ count: number }> : Promise.reject()).then(v => setUnread(v.count)).catch(() => setUnread(0)) }, [pathname])
  useEffect(() => { fetch('/api/v1/chat/unread-count', { credentials: 'include' }).then(r => r.ok ? r.json() as Promise<{ count: number }> : Promise.reject()).then(v => setChatUnread(v.count)).catch(() => setChatUnread(0)) }, [pathname])
  useEffect(() => { getProviderWallet().then(wallet => { setWalletBalance(wallet.availableBalance); setWalletCurrency(wallet.currencyCode) }).catch(() => setWalletBalance(null)) }, [pathname])
  useEffect(() => { getProviderTrust().then(value => setTrust({ score:value.score, gradeLabel:value.gradeLabel })).catch(() => setTrust(null)) }, [pathname])
  const signOut = async () => { await logout(); navigate('/provider/start') }
  return <div className="providerAppShell">
    <a className="skipLink" href="#provider-main">본문으로 바로가기</a>
    <header className="providerHeader"><div className="providerHeaderInner"><BrandLogo />
      <span className="providerBrandLabel">수달 파트너스</span>
      <nav className="providerDesktopNav" aria-label="공급자 주요 메뉴">{desktop.map(([path, label]) => <button className={isActive(pathname, path) ? 'isActive' : ''} key={path} onClick={() => navigate(path)}>{label}</button>)}
        <NavGroup label="서비스" pathname={pathname} items={serviceMenu} />
        <NavGroup label="내 정보" pathname={pathname} items={accountMenu} />
      </nav>
      <div className="providerHeaderActions"><button className="providerTrustBadge" type="button" onClick={() => navigate('/provider/trust')} aria-label={`나의 신뢰도 페이지로 이동, ${formatHeaderTrust(trust)}`}><span>신뢰도</span><strong>{formatHeaderTrust(trust)}</strong></button><button className="providerWalletBalance" type="button" onClick={() => navigate('/provider/wallet')} aria-label={`마이 수달 충전금으로 이동, 현재 잔액 ${formatHeaderBalance(walletBalance, walletCurrency)}`}><span>충전금</span><strong>{formatHeaderBalance(walletBalance, walletCurrency)}</strong></button><button className="providerBell" onClick={() => navigate('/provider/notifications')} aria-label={`읽지 않은 알림 ${unread}개`}>알림{unread > 0 && <span>{unread > 99 ? '99+' : unread}</span>}</button>{user?.roles.includes('CUSTOMER') && <button type="button" onClick={() => window.location.assign('https://soodallife.kr/customer')}>고객 홈</button>}<button className="providerAccountButton" onClick={() => void signOut()}><span>{user?.loginId ?? '사용자'}</span><small>로그아웃</small></button></div>
    </div></header>
    <main id="provider-main" className="providerMain">{actions}<BusinessChatShortcut />{children}<TransactionDirectPaymentPanel /></main>
    <ServiceFooter variant="provider" />
    <nav className="providerBottomNav" aria-label="모바일 공급자 메뉴">{mobile.map(([path, label]) => <button key={path} className={isActive(pathname, path) ? 'isActive' : ''} onClick={() => navigate(path)}><span aria-hidden="true">{label === '홈' ? '⌂' : label === '마이' ? '○' : label === '채팅' ? '✉' : '·'}</span>{label}{label === '채팅' && chatUnread > 0 && <b className="providerNavBadge">{chatUnread > 99 ? '99+' : chatUnread}</b>}</button>)}</nav>
  </div>
}

function formatHeaderBalance(value: number | null, currency: string) {
  if (value === null) return '확인 중'
  return new Intl.NumberFormat('ko-KR', { style: 'currency', currency, maximumFractionDigits: 0 }).format(value)
}

function formatHeaderTrust(value: { score:number|null; gradeLabel:string } | null) {
  if (!value) return '확인 중'
  return value.score === null ? value.gradeLabel : `${new Intl.NumberFormat('ko-KR', { maximumFractionDigits: 1 }).format(value.score)}점 · ${value.gradeLabel}`
}

function NavGroup({ label, pathname, items }: { label: string; pathname: string; items: ReadonlyArray<readonly [string, string]> }) {
  const active = items.some(([path]) => isActive(pathname, path))
  return <details className={`providerNavGroup${active ? ' isActive' : ''}`}>
    <summary>{label}</summary>
    <div>{items.map(([path, itemLabel]) => <button type="button" className={isActive(pathname, path) ? 'isActive' : ''} key={path} onClick={() => navigate(path)}>{itemLabel}</button>)}</div>
  </details>
}

function isActive(pathname:string, path:string) {
  if (path === '/provider') return pathname === path
  if (path === '/provider/matched-requests') return pathname.startsWith('/provider/matched-requests') || pathname.startsWith('/provider/quotes')
  if (path === '/provider/progress') return pathname.startsWith('/provider/progress') || pathname.startsWith('/provider/work')
  if (path === '/provider/after-services') return pathname.startsWith('/provider/after-services') || pathname.startsWith('/provider/disputes')
  return pathname === path || pathname.startsWith(`${path}/`)
}
