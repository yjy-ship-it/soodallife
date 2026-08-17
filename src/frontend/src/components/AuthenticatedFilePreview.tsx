import { useEffect, useState } from 'react'
import { apiUrl } from '../config/apiEndpoint'
import './authenticatedFilePreview.css'

export interface AuthenticatedPreviewFile {
  fileName: string
  contentType: string
  sizeBytes: number
  downloadUrl: string
}

export function AuthenticatedFilePreview({ file, statusText, onRemove }: { file: AuthenticatedPreviewFile; statusText?: string; onRemove?: () => void }) {
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [previewError, setPreviewError] = useState(false)
  const isImage = file.contentType.startsWith('image/')
  const isPdf = file.contentType === 'application/pdf'

  useEffect(() => {
    if (!isImage && !isPdf) return
    let objectUrl: string | null = null
    let active = true
    setPreviewError(false)
    setPreviewUrl(null)
    fetch(apiUrl(file.downloadUrl), { credentials: 'include' })
      .then(response => {
        if (!response.ok) throw new Error(`preview failed: ${response.status}`)
        return response.blob()
      })
      .then(blob => {
        if (!active) return
        objectUrl = URL.createObjectURL(blob)
        setPreviewUrl(objectUrl)
      })
      .catch(() => { if (active) setPreviewError(true) })
    return () => {
      active = false
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [file.downloadUrl, isImage, isPdf])

  return <article className="authenticatedFilePreview">
    <div className="authenticatedFileVisual">
      {previewUrl && isImage ? <img src={previewUrl} alt={`${file.fileName} 미리보기`} />
        : previewUrl && isPdf ? <iframe src={previewUrl} title={`${file.fileName} 미리보기`} />
          : previewError ? <span>미리보기를 불러오지 못했습니다.</span>
            : isImage || isPdf ? <span>미리보기 불러오는 중…</span> : <span aria-hidden="true">📄</span>}
    </div>
    <strong title={file.fileName}>{file.fileName}</strong>
    <small>{(file.sizeBytes / 1024 / 1024).toFixed(1)}MB{statusText ? ` · ${statusText}` : ''}</small>
    <div className="authenticatedFileActions">
      <a href={previewUrl ?? apiUrl(file.downloadUrl)} target="_blank" rel="noreferrer">크게 보기</a>
      {onRemove && <button type="button" onClick={onRemove}>삭제</button>}
    </div>
  </article>
}
