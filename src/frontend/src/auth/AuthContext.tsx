import { useCallback, useEffect, useMemo, useState } from 'react'
import type { PropsWithChildren } from 'react'
import * as authenticationApi from './api'
import { AuthenticationContext } from './AuthenticationContext'
import type { AuthenticationStatus } from './AuthenticationContext'
import type { AuthenticatedUser, RoleCode } from './types'

export function AuthenticationProvider({ children }: PropsWithChildren) {
  const [status, setStatus] = useState<AuthenticationStatus>('loading')
  const [user, setUser] = useState<AuthenticatedUser | null>(null)

  useEffect(() => {
    let active = true
    authenticationApi
      .getCurrentUser()
      .then((currentUser) => {
        if (!active) return
        setUser(currentUser)
        setStatus(currentUser ? 'authenticated' : 'anonymous')
      })
      .catch(() => {
        if (!active) return
        setUser(null)
        setStatus('anonymous')
      })

    return () => {
      active = false
    }
  }, [])

  const login = useCallback(async (loginOrEmail: string, password: string, requiredRole?: RoleCode, mfaCode?: string, rememberMe = false) => {
    const authenticatedUser = await authenticationApi.login(loginOrEmail, password, mfaCode, rememberMe)
    if (requiredRole && !authenticatedUser.roles.includes(requiredRole)) {
      await authenticationApi.logout()
      throw new authenticationApi.AuthenticationApiError(
        requiredRole === 'ADMIN' ? '관리자 권한이 없는 계정입니다. 본사 관리자 아이디로 로그인해 주세요.' : '이 화면을 이용할 권한이 없는 계정입니다.',
        403,
        'REQUIRED_ROLE_MISSING',
      )
    }
    setUser(authenticatedUser)
    setStatus('authenticated')
    return authenticatedUser
  }, [])

  const logout = useCallback(async () => {
    try {
      await authenticationApi.logout()
    } finally {
      setUser(null)
      setStatus('anonymous')
    }
  }, [])

  const refresh = useCallback(async () => {
    const currentUser = await authenticationApi.refresh()
    setUser(currentUser)
    setStatus('authenticated')
    return currentUser
  }, [])

  const value = useMemo(() => ({ status, user, login, refresh, logout }), [status, user, login, refresh, logout])
  return <AuthenticationContext.Provider value={value}>{children}</AuthenticationContext.Provider>
}
