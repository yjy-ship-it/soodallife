import { useCallback, useEffect, useMemo, useState } from 'react'
import type { PropsWithChildren } from 'react'
import * as authenticationApi from './api'
import { AuthenticationContext } from './AuthenticationContext'
import type { AuthenticationStatus } from './AuthenticationContext'
import type { AuthenticatedUser } from './types'

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

  const login = useCallback(async (loginOrEmail: string, password: string) => {
    const authenticatedUser = await authenticationApi.login(loginOrEmail, password)
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

  const value = useMemo(() => ({ status, user, login, logout }), [status, user, login, logout])
  return <AuthenticationContext.Provider value={value}>{children}</AuthenticationContext.Provider>
}
