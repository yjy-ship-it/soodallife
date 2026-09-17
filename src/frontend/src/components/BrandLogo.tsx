import { navigate } from '../auth/routing'
import './BrandLogo.css'

type BrandLogoProps = {
  compact?: boolean
  homePath?: string
}

function AudienceIcon({ audience }: { audience: string }) {
  return audience === '전문가' ? (
    <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M8 7V5.5A2.5 2.5 0 0 1 10.5 3h3A2.5 2.5 0 0 1 16 5.5V7"/><rect x="3" y="7" width="18" height="13" rx="2"/><path d="M3 12.5c5.4 2 12.6 2 18 0M10 13h4"/></svg>
  ) : audience === '고객' ? (
    <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="8" r="3.5"/><path d="M5.5 20a6.5 6.5 0 0 1 13 0M17.5 5.5l1 1 2-2"/></svg>
  ) : (
    <svg viewBox="0 0 24 24" aria-hidden="true"><rect x="5" y="4" width="14" height="16" rx="2"/><path d="M9 8h6M9 12h6M9 16h4"/></svg>
  )
}

export function BrandLogo({ compact = false, homePath = '/' }: BrandLogoProps) {
  const hostname = window.location.hostname.toLowerCase()
  const pathname = window.location.pathname.toLowerCase()
  const audience = hostname.startsWith('partner.') || pathname.startsWith('/provider') || homePath.startsWith('/provider')
    ? '전문가'
    : hostname.startsWith('admin.') || pathname.startsWith('/admin')
      ? '관리자'
      : '고객'
  return (
    <button className="brandLogo" type="button" onClick={() => navigate(homePath)} aria-label={`수달 라이프 ${audience} 홈`}>
      <img className="brandLogoMark" src="/brand/soodal-life-mark.png" alt="" aria-hidden="true" />
      {!compact && <span className="brandLogoText"><strong>수달 라이프</strong><small><AudienceIcon audience={audience} />{audience}</small></span>}
    </button>
  )
}
