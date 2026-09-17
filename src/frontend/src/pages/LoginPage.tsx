import { useState } from 'react'
import type { FormEvent } from 'react'
import { useAuthentication } from '../auth/AuthenticationContext'
import { AuthenticationApiError } from '../auth/api'
import { getInitialAuthenticatedPath, getSafeReturnUrl, navigate } from '../auth/routing'
import { BrandLogo } from '../components/BrandLogo'

export function LoginPage() {
  const { login } = useAuthentication()
  const hostname = window.location.hostname.toLowerCase()
  const returnUrl = getSafeReturnUrl()
  const isAdminLogin = hostname.startsWith('admin.') || returnUrl?.startsWith('/admin') === true
  const isProviderLogin = !isAdminLogin && (hostname.startsWith('partner.') || returnUrl?.startsWith('/provider') === true)
  const rememberedLoginStorageKey = isAdminLogin ? 'soodal.rememberedAdminLoginId' : 'soodal.rememberedLoginId'
  const [loginOrEmail, setLoginOrEmail] = useState(() => localStorage.getItem(rememberedLoginStorageKey) ?? '')
  const [password, setPassword] = useState('')
  const [mfaCode, setMfaCode] = useState('')
  const [rememberLoginId, setRememberLoginId] = useState(() => Boolean(localStorage.getItem(rememberedLoginStorageKey)))
  const [keepSignedIn, setKeepSignedIn] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)
    setSubmitting(true)

    try {
      const user = await login(loginOrEmail, password, isAdminLogin ? 'ADMIN' : undefined, isAdminLogin ? mfaCode : undefined, !isAdminLogin && keepSignedIn)
      if (rememberLoginId) localStorage.setItem(rememberedLoginStorageKey, loginOrEmail.trim())
      else localStorage.removeItem(rememberedLoginStorageKey)
      navigate(getSafeReturnUrl() ?? getInitialAuthenticatedPath(user), true)
    } catch (requestError) {
      setError(
        requestError instanceof AuthenticationApiError
          ? requestError.message
          : '로그인 중 오류가 발생했습니다. 잠시 후 다시 시도해 주세요.',
      )
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="loginShell">
      <section className="loginIntro" aria-labelledby="login-title">
        <div>
          <BrandLogo />
          <p className="eyebrow">수달 라이프</p>
          <h1 id="login-title">일상의 문제를<br />믿을 수 있는 전문가와.</h1>
          <p className="introCopy">
            고객, 전문가, 관리자가 하나의 안전한 서비스 흐름에서 만나는 수달 라이프입니다.
          </p>
        </div>
        <p className="introFootnote">수리부터 생활 서비스까지, 필요한 순간을 연결합니다.</p>
      </section>

      <section className="loginPanel" aria-label="로그인">
        <div className="loginCard">
          <div className="mobileBrand"><BrandLogo /></div>
          <h2>{isAdminLogin ? '관리자 로그인' : isProviderLogin ? '전문가 로그인' : '로그인'}</h2>
          <p className="panelDescription">{isAdminLogin ? '등록된 본사 관리자 계정으로 로그인해 주세요.' : isProviderLogin ? '전문가 계정으로 서비스를 시작하세요.' : '등록된 고객 계정으로 서비스를 시작하세요.'}</p>

          <form onSubmit={handleSubmit} noValidate>
            <label htmlFor="loginOrEmail">아이디</label>
            <input
              id="loginOrEmail"
              name="loginOrEmail"
              type="text"
              autoComplete="username"
              value={loginOrEmail}
              onChange={(event) => setLoginOrEmail(event.target.value)}
              required
            />

            <label className="rememberLoginId" htmlFor="rememberLoginId">
              <input id="rememberLoginId" type="checkbox" checked={rememberLoginId} onChange={(event) => setRememberLoginId(event.target.checked)} />
              아이디 저장
            </label>
            <small className="loginAutofillNote">비밀번호는 사이트가 저장하지 않으며, 브라우저의 안전한 비밀번호 저장 기능으로 자동 입력할 수 있습니다.</small>

            {!isAdminLogin && <label className="rememberLoginId" htmlFor="keepSignedIn">
              <input id="keepSignedIn" type="checkbox" checked={keepSignedIn} onChange={(event) => setKeepSignedIn(event.target.checked)} />
              로그인 상태 유지 <small>(최대 30일)</small>
            </label>}

            <label htmlFor="password">비밀번호</label>
            <input
              id="password"
              name="password"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              required
            />

            {isAdminLogin && <><label htmlFor="mfaCode">관리자 MFA 코드 <small>(설정한 계정만 입력)</small></label><input id="mfaCode" name="mfaCode" type="text" inputMode="numeric" autoComplete="one-time-code" maxLength={6} value={mfaCode} onChange={(event) => setMfaCode(event.target.value.replace(/\D/g, '').slice(0, 6))} placeholder="인증 앱 6자리" /></>}

            {error && <div className="errorMessage" role="alert">{error}</div>}

            <button className="primaryButton" type="submit" disabled={submitting || !loginOrEmail || !password}>
              {submitting ? '로그인 중…' : '로그인'}
            </button>
          </form>
          <div className={`loginLinks${isAdminLogin ? ' adminLoginLinks' : ''}`}>
            {!isAdminLogin && <button type="button" onClick={() => navigate(isProviderLogin ? '/provider/signup' : `/signup${returnUrl ? `?returnUrl=${encodeURIComponent(returnUrl)}` : ''}`)}>{isProviderLogin ? '전문가 회원가입' : '고객 회원가입'}</button>}
            <button type="button" onClick={() => navigate('/password-reset')}>비밀번호를 잊으셨나요?</button>
          </div>
          <p className="securityNote">계정 정보는 암호화된 연결을 통해 안전하게 전송됩니다.</p>
        </div>
      </section>
    </main>
  )
}
