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
  const [expanded, setExpanded] = useState(false)
  const isImage = file.contentType.startsWith('image/')
  const isPdf = file.contentType === 'application/pdf'
  const accessibleUrl = previewUrl ?? apiUrl(file.downloadUrl)

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
      {previewUrl && isImage ? <button type="button" className="authenticatedFilePreviewButton" onClick={() => setExpanded(true)} aria-label={`${file.fileName} 크게 보기`}><img src={previewUrl} alt={`${file.fileName} 미리보기`} /></button>
        : previewUrl && isPdf ? <button type="button" className="authenticatedFilePreviewButton" onClick={() => setExpanded(true)} aria-label={`${file.fileName} 크게 보기`}><iframe src={previewUrl} title={`${file.fileName} 미리보기`} tabIndex={-1} /></button>
          : previewError ? <span>미리보기를 불러오지 못했습니다.</span>
            : isImage || isPdf ? <span>미리보기 불러오는 중…</span> : <span aria-hidden="true">📄</span>}
    </div>
    <strong title={file.fileName}>{file.fileName}</strong>
    <small>{(file.sizeBytes / 1024 / 1024).toFixed(1)}MB{statusText ? ` · ${statusText}` : ''}</small>
    <div className="authenticatedFileActions">
      <button type="button" disabled={!previewUrl} onClick={() => setExpanded(true)}>팝업으로 크게 보기</button>
      {isPdf && <a href={accessibleUrl} target="_blank" rel="noreferrer">새 창에서 열기</a>}
      {isPdf && <a href={accessibleUrl} download={file.fileName}>다운로드</a>}
      {onRemove && <button type="button" onClick={onRemove}>삭제</button>}
    </div>
    {expanded && previewUrl && <div className="authenticatedFileModal" role="dialog" aria-modal="true" aria-label={`${file.fileName} 확대 미리보기`} onMouseDown={() => setExpanded(false)}>
      <div className="authenticatedFileModalBody" onMouseDown={event => event.stopPropagation()}>
        <div className="authenticatedFileModalHead"><strong>{file.fileName}</strong><button type="button" autoFocus onClick={() => setExpanded(false)}>닫기</button></div>
        {isImage ? <img src={previewUrl} alt={`${file.fileName} 확대 미리보기`} /> : <div className="authenticatedPdfModalContent"><iframe src={previewUrl} title={`${file.fileName} 확대 미리보기`} /><aside className="authenticatedPdfHelp" role="note"><strong>PDF가 화면에 보이지 않나요?</strong><p>모바일 브라우저가 PDF 미리보기를 지원하지 않거나 기기에 PDF 뷰어가 없을 수 있습니다.</p><div><a href={accessibleUrl} target="_blank" rel="noreferrer">새 창에서 열기</a><a href={accessibleUrl} download={file.fileName}>PDF 다운로드</a></div><small>그래도 열리지 않으면 PDF 뷰어 앱을 설치한 뒤 다운로드한 파일을 열어 주세요.</small></aside></div>}
      </div>
    </div>}
  </article>
}
