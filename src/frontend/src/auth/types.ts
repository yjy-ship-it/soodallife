export const roleCodes = ['CUSTOMER', 'PROVIDER', 'ADMIN'] as const

export type RoleCode = (typeof roleCodes)[number]

export interface AuthenticatedUser {
  publicId: string
  loginId: string
  roles: RoleCode[]
}

export interface ApiError {
  businessCode: string
  message: string
  fieldErrors?: Record<string, string[]>
  traceId: string
}
