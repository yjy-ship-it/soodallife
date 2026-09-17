import { useEffect, useState } from 'react'
import './serviceVisual.css'

export type ServiceVisualSettings = { imagesEnabled: boolean; iconsEnabled: boolean; bannersEnabled: boolean }
type ManifestItem = { major: string; middle: string; service: string; image: string }
type Manifest = Record<string, ManifestItem>

const defaults: ServiceVisualSettings = { imagesEnabled: false, iconsEnabled: false, bannersEnabled: false }
let settings = defaults
let manifest: Manifest = {}
let loadPromise: Promise<void> | null = null
const listeners = new Set<() => void>()
const notify = () => listeners.forEach(listener => listener())
const load = () => {
  if (!loadPromise) loadPromise = Promise.all([
    fetch('/api/v1/service-visuals/settings', { credentials: 'include' }).then(response => response.ok ? response.json() as Promise<ServiceVisualSettings> : defaults),
    fetch('/service-visuals/manifest.json').then(response => response.ok ? response.json() as Promise<Manifest> : {}),
  ]).then(([nextSettings, nextManifest]) => { settings = nextSettings; manifest = nextManifest; notify() }).catch(() => undefined)
  return loadPromise
}

export function useServiceVisualSettings() {
  const [, update] = useState(0)
  useEffect(() => { const listener = () => update(value => value + 1); listeners.add(listener); void load(); return () => { listeners.delete(listener) } }, [])
  return settings
}

export function refreshServiceVisualSettings(next?: ServiceVisualSettings) {
  if (next) { settings = next; notify(); return }
  loadPromise = null
  void load()
}

function findItem(code?: string | null, name?: string | null) {
  const normalizedCode = code?.trim().toUpperCase()
  if (normalizedCode && manifest[normalizedCode]) return manifest[normalizedCode]
  const cleanName = name?.split(/[›>]/).at(-1)?.trim()
  return Object.values(manifest).find(item => item.service === cleanName)
}

export function ServiceThumbnail({ code, name, className = '' }: { code?: string | null; name?: string | null; className?: string }) {
  const current = useServiceVisualSettings()
  const item = findItem(code ?? undefined, name ?? undefined)
  const [failedImage, setFailedImage] = useState<string | null>(null)
  if (!current.imagesEnabled || !item || failedImage === item.image) return current.iconsEnabled ? <CategoryIcon name={item?.major ?? name ?? ''} className={className} /> : null
  return <img className={`serviceThumbnail ${className}`} src={item.image} alt={`${item.service} 서비스 대표 이미지`} loading="lazy" decoding="async" onError={() => setFailedImage(item.image)} />
}

export function CategoryIcon({ name, className = '' }: { name: string; className?: string }) {
  const current = useServiceVisualSettings()
  if (!current.iconsEnabled) return null
  const key = name.includes('집수리') ? 'repair' : name.includes('인테리어') ? 'interior' : name.includes('청소') || name.includes('위생') ? 'clean' : name.includes('제품') ? 'product' : name.includes('구독') ? 'subscription' : 'living'
  const paths: Record<string, string> = {
    repair: 'M14.7 6.3a4 4 0 0 0-5-5L7.2 3.8l3 3L6.3 10.7l-3-3L.8 10.2a4 4 0 0 0 5 5l6.8 6.8 3.4-3.4-6.8-6.8a4 4 0 0 0 5.5-5.5Z',
    interior: 'M3 21h18M5 18V8l7-5 7 5v10M9 18v-6h6v6',
    clean: 'M12 3v18M3 12h18M5.6 5.6l12.8 12.8M18.4 5.6 5.6 18.4',
    product: 'M5 4h14v16H5zM8 8h8M8 12h8M8 16h5',
    subscription: 'M4 6h16v14H4zM8 3v6M16 3v6M8 14l2.5 2.5L16 11',
    living: 'M12 3 3 10v11h18V10zM9 21v-7h6v7',
  }
  return <span className={`categoryVectorIcon ${className}`} aria-hidden="true"><svg viewBox="0 0 24 24"><path d={paths[key]} /></svg></span>
}

export function ServiceBanner({ major }: { major?: string | null }) {
  const current = useServiceVisualSettings()
  const bannerIndexes = { '집수리': 1, '인테리어': 2, '청소·위생': 3, '제품수리·설치': 4, '생활서비스': 5, '정기구독': 6 } as Record<string, number>
  const index = major ? bannerIndexes[major] : undefined
  const image = index ? `/service-visuals/banners/banner-${String(index).padStart(2, '0')}.webp` : null
  const [failedImage, setFailedImage] = useState<string | null>(null)
  if (!current.bannersEnabled || !major || !image) return null
  return <section className={`serviceMajorBanner${failedImage === image ? ' imageUnavailable' : ''}`}>{failedImage !== image && <img src={image} alt={`${major} 주요 서비스 이미지`} onError={() => setFailedImage(image)} />}<div><small>수달 라이프 주요 서비스</small><strong>{major}</strong><span>필요한 서비스를 분류별로 편리하게 찾아보세요.</span></div></section>
}

export const serviceNameFromPath = (value: string) => value.split(/[›>]/).at(-1)?.trim() || value
