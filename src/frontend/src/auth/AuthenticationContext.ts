import { createContext, useContext } from 'react'
import type { AuthenticatedUser } from './types'

export type AuthenticationStatus = 'loading' | 'authenticated' | 'anonymous'

export interface AuthenticationContextValue {
  status: AuthenticationStatus
  user: AuthenticatedUser | null
  login: (loginOrEmail: string, password: string) => Promise<AuthenticatedUser>
  refresh: () => Promise<AuthenticatedUser>
  logout: () => Promise<void>
}

export const AuthenticationContext = createContext<AuthenticationContextValue | null>(null)

export function useAuthentication(): AuthenticationContextValue {
  const context = useContext(AuthenticationContext)
  if (!context) {
    throw new Error('useAuthentication must be used inside AuthenticationProvider.')
  }

  return context
}
