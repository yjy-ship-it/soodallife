import type { AuthenticatedUser, RoleCode } from './types'

export const roleHomePaths: Record<RoleCode, string> = {
  CUSTOMER: '/customer',
  PROVIDER: '/provider',
  ADMIN: '/admin',
}

export function getInitialAuthenticatedPath(user: AuthenticatedUser): string {
  return user.roles.length === 1 ? roleHomePaths[user.roles[0]] : '/roles'
}

export function navigate(path: string, replace = false): void {
  const method = replace ? 'replaceState' : 'pushState'
  window.history[method]({}, '', path)
  window.dispatchEvent(new PopStateEvent('popstate'))
}
