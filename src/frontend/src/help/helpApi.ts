export type Suggestion = {
  id: string
  typeCode: string
  statusCode: string
  title: string
  body: string
  pageUrl: string | null
  deviceInfo: string | null
  appVersion: string | null
  adminReply: string | null
  releaseVersion: string | null
  createdAt: string
  updatedAt: string
  rowVersion: string
}

export async function helpRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    credentials: 'include',
    ...init,
    headers:
      init?.body && !(init.body instanceof FormData)
        ? { 'Content-Type': 'application/json', ...init.headers }
        : init?.headers,
  })
  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as { message?: string } | null
    throw new Error(body?.message ?? '요청을 처리하지 못했습니다.')
  }
  return response.status === 204 ? (undefined as T) : (response.json() as Promise<T>)
}
