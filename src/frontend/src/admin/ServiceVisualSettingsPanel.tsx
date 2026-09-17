import { useEffect, useState } from 'react'
import { refreshServiceVisualSettings } from '../serviceVisuals/ServiceVisual'
import type { ServiceVisualSettings } from '../serviceVisuals/ServiceVisual'

const fallback: ServiceVisualSettings = { imagesEnabled: true, iconsEnabled: true, bannersEnabled: true }

export function ServiceVisualSettingsPanel() {
  const [value, setValue] = useState(fallback)
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState('')
  useEffect(() => { void fetch('/api/v1/service-visuals/settings', { credentials: 'include' }).then(response => response.ok ? response.json() as Promise<ServiceVisualSettings> : fallback).then(setValue).catch(() => setMessage('표시 설정을 불러오지 못했습니다.')) }, [])
  const save = async () => {
    setSaving(true); setMessage('')
    try {
      const response = await fetch('/api/v1/service-visuals/settings', { method: 'PUT', credentials: 'include', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(value) })
      if (!response.ok) throw new Error()
      const updated = await response.json() as ServiceVisualSettings
      setValue(updated); refreshServiceVisualSettings(updated); setMessage('서비스 이미지와 아이콘 표시 설정을 저장했습니다.')
    } catch { setMessage('표시 설정을 저장하지 못했습니다.') } finally { setSaving(false) }
  }
  const toggle = (key: keyof ServiceVisualSettings) => setValue(current => ({ ...current, [key]: !current[key] }))
  return <section className="serviceVisualAdmin" aria-labelledby="service-visual-admin-title"><header><div><p>서비스 화면 꾸미기</p><h2 id="service-visual-admin-title">이미지·아이콘 표시 설정</h2></div><span>끄면 배포 전과 같은 글자 중심 화면으로 즉시 돌아갑니다.</span></header><div><label><input type="checkbox" checked={value.imagesEnabled} onChange={() => toggle('imagesEnabled')} /><span><b>목록·카드 대표 이미지</b><small>677개 하위 서비스 대표 이미지를 표시합니다.</small></span></label><label><input type="checkbox" checked={value.iconsEnabled} onChange={() => toggle('iconsEnabled')} /><span><b>메뉴·카테고리 아이콘</b><small>비용 없는 벡터 아이콘을 표시합니다.</small></span></label><label><input type="checkbox" checked={value.bannersEnabled} onChange={() => toggle('bannersEnabled')} /><span><b>주요 서비스 큰 배너</b><small>고품질 홍보 이미지를 표시합니다.</small></span></label></div><footer>{message && <span role="status">{message}</span>}<button type="button" disabled={saving} onClick={() => void save()}>{saving ? '저장 중…' : '표시 설정 저장'}</button></footer></section>
}
