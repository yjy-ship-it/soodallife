import { useCallback, useEffect, useState } from 'react'
import { publicCatalogApi } from './api'
import type { LivingHome } from './types'

const livingHomeRequests = new Map<string, Promise<LivingHome>>()

function storedLivingHome(key: string | null) {
  if (!key) return null
  const cached = localStorage.getItem(`soodal-living-home:${key}`)
  if (!cached) return null
  try {
    return JSON.parse(cached) as LivingHome
  } catch {
    return null
  }
}

function requestLivingHome(key: string) {
  const current = livingHomeRequests.get(key)
  if (current) return current
  const request = publicCatalogApi.livingHome().finally(() => livingHomeRequests.delete(key))
  livingHomeRequests.set(key, request)
  return request
}

export function useLivingHome(cacheScope: string | null) {
  const [value, setValue] = useState<LivingHome | null>(() => storedLivingHome(cacheScope))
  const [error, setError] = useState('')
  const load = useCallback(() => {
    if (!cacheScope) return
    setError('')
    void requestLivingHome(cacheScope)
      .then((next) => {
        setValue(next)
        localStorage.setItem(`soodal-living-home:${cacheScope}`, JSON.stringify(next))
      })
      .catch(() => setError('생활관리 정보를 잠시 불러오지 못했습니다.'))
  }, [cacheScope])
  useEffect(() => {
    setValue(storedLivingHome(cacheScope))
    if (cacheScope) load()
  }, [cacheScope, load])
  return { value, error, reload: load }
}
