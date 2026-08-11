import { navigate } from '../auth/routing'

type BrandLogoProps = {
  compact?: boolean
}

export function BrandLogo({ compact = false }: BrandLogoProps) {
  return (
    <button className="brandLogo" type="button" onClick={() => navigate('/')} aria-label="수달 라이프 홈">
      <span className="brandLogoPlaceholder" aria-label="승인 로고 적용 예정">개발용<br />로고 자리</span>
      {!compact && <span className="brandLogoText"><strong>수달 라이프</strong><small>SOODAL LIFE</small></span>}
    </button>
  )
}
