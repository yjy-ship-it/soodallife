const configuredApiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').trim().replace(/\/+$/, '')
const productionApiBaseUrl = /(^|\.)soodallife\.kr$/i.test(window.location.hostname)
  ? 'https://api.soodallife.kr'
  : ''
const apiBaseUrl = configuredApiBaseUrl || productionApiBaseUrl

export function apiUrl(path: string): string {
  if (!apiBaseUrl || !path.startsWith('/')) return path
  return `${apiBaseUrl}${path}`
}

export function installApiEndpoint(): void {
  if (!apiBaseUrl) return

  const originalFetch = window.fetch.bind(window)
  window.fetch = ((input: RequestInfo | URL, init?: RequestInit) => {
    if (typeof input === 'string' && input.startsWith('/api/')) {
      return originalFetch(apiUrl(input), init)
    }
    return originalFetch(input, init)
  }) as typeof window.fetch
}
