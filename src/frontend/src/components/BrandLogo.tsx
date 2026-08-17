import { navigate } from '../auth/routing'

type BrandLogoProps = {
  compact?: boolean
}

export function BrandLogo({ compact = false }: BrandLogoProps) {
  return (
    <button className="brandLogo" type="button" onClick={() => navigate('/')} aria-label="수달 라이프 홈">
      <img className="brandLogoMark" src="/brand/soodal-life-mark.png" alt="" aria-hidden="true" />
      {!compact && <span className="brandLogoText"><strong>수달 라이프</strong><small>SOODAL LIFE</small></span>}
    </button>
  )
}
