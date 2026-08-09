import { useState } from 'react'
import type { FormEvent } from 'react'
import { useAuthentication } from '../auth/AuthenticationContext'
import { AuthenticationApiError } from '../auth/api'
import { getInitialAuthenticatedPath, navigate } from '../auth/routing'

export function LoginPage() {
  const { login } = useAuthentication()
  const [loginOrEmail, setLoginOrEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)
    setSubmitting(true)

    try {
      const user = await login(loginOrEmail, password)
      navigate(getInitialAuthenticatedPath(user), true)
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
          <p className="eyebrow">SOODAL LIFE</p>
          <h1 id="login-title">일상의 문제를<br />믿을 수 있는 전문가와.</h1>
          <p className="introCopy">
            고객, 공급자, 관리자가 하나의 안전한 서비스 흐름에서 만나는 수달 라이프입니다.
          </p>
        </div>
        <p className="introFootnote">수리부터 생활 서비스까지, 필요한 순간을 연결합니다.</p>
      </section>

      <section className="loginPanel" aria-label="로그인">
        <div className="loginCard">
          <div className="mobileBrand" aria-hidden="true">SOODAL LIFE</div>
          <h2>로그인</h2>
          <p className="panelDescription">등록된 계정으로 서비스를 시작하세요.</p>

          <form onSubmit={handleSubmit} noValidate>
            <label htmlFor="loginOrEmail">아이디 또는 이메일</label>
            <input
              id="loginOrEmail"
              name="loginOrEmail"
              type="text"
              autoComplete="username"
              value={loginOrEmail}
              onChange={(event) => setLoginOrEmail(event.target.value)}
              required
            />

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

            {error && <div className="errorMessage" role="alert">{error}</div>}

            <button className="primaryButton" type="submit" disabled={submitting || !loginOrEmail || !password}>
              {submitting ? '로그인 중…' : '로그인'}
            </button>
          </form>
          <p className="securityNote">계정 정보는 암호화된 연결을 통해 안전하게 전송됩니다.</p>
        </div>
      </section>
    </main>
  )
}
