import type { ApiError, AuthenticatedUser } from './types'

export class AuthenticationApiError extends Error {
  readonly status: number
  readonly businessCode?: string

  constructor(
    message: string,
    status: number,
    businessCode?: string,
  ) {
    super(message)
    this.status = status
    this.businessCode = businessCode
  }
}

async function parseError(response: Response): Promise<AuthenticationApiError> {
  try {
    const error = (await response.json()) as ApiError
    return new AuthenticationApiError(error.message, response.status, error.businessCode)
  } catch {
    return new AuthenticationApiError('요청을 처리하지 못했습니다. 잠시 후 다시 시도해 주세요.', response.status)
  }
}

export async function login(loginOrEmail: string, password: string): Promise<AuthenticatedUser> {
  const response = await fetch('/api/v1/auth/login', {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ loginOrEmail, password }),
  })

  if (!response.ok) {
    throw await parseError(response)
  }

  return (await response.json()) as AuthenticatedUser
}

export async function getCurrentUser(): Promise<AuthenticatedUser | null> {
  const response = await fetch('/api/v1/me', { credentials: 'include' })
  if (response.status === 401) {
    return null
  }

  if (!response.ok) {
    throw await parseError(response)
  }

  return (await response.json()) as AuthenticatedUser
}

export async function logout(): Promise<void> {
  const response = await fetch('/api/v1/auth/logout', {
    method: 'POST',
    credentials: 'include',
  })

  if (!response.ok && response.status !== 401) {
    throw await parseError(response)
  }
}

export async function refresh(): Promise<AuthenticatedUser> {
  const response = await fetch('/api/v1/auth/refresh', { method: 'POST', credentials: 'include' })
  if (!response.ok) throw await parseError(response)
  return (await response.json()) as AuthenticatedUser
}
