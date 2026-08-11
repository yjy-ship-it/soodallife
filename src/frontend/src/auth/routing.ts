import type { AuthenticatedUser, RoleCode } from './types'

export const roleHomePaths: Record<RoleCode, string> = {
  CUSTOMER: '/customer',
  PROVIDER: '/provider',
  ADMIN: '/admin',
}

export function getInitialAuthenticatedPath(user: AuthenticatedUser): string {
  return user.roles.length === 1 ? roleHomePaths[user.roles[0]] : '/roles'
}

export function isSafeInternalReturnUrl(value: string | null): value is string {
  return Boolean(value && value.startsWith('/') && !value.startsWith('//') && !value.includes('\\'))
}

export function createLoginPath(returnUrl: string): string {
  const safeReturnUrl = isSafeInternalReturnUrl(returnUrl) ? returnUrl : '/'
  return `/login?returnUrl=${encodeURIComponent(safeReturnUrl)}`
}

export function getSafeReturnUrl(search = window.location.search): string | null {
  const value = new URLSearchParams(search).get('returnUrl')
  return isSafeInternalReturnUrl(value) ? value : null
}

export function navigate(path: string, replace = false): void {
  const method = replace ? 'replaceState' : 'pushState'
  window.history[method]({}, '', path)
  window.dispatchEvent(new PopStateEvent('popstate'))
}
