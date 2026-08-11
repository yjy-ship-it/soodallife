import { useCallback, useEffect, useRef, useState } from 'react'
import * as quoteApi from './api'
import type { CustomerProviderProfile, CustomerQuoteComparison, CustomerQuoteDetail, QuoteDetail, QuoteItemInput, SaveQuoteRevisionInput } from './types'

const emptyItem = (): QuoteItemInput => ({ itemName: '', description: null, quantity: 1, unitText: '식', unitPriceAmount: 0 })

export function ProviderQuotePanel({ requestId, requestExpiresAt }: { requestId: string; requestExpiresAt: string }) {
  const [quote, setQuote] = useState<QuoteDetail | null>(null)
  const [summary, setSummary] = useState(''), [terms, setTerms] = useState(''), [vatAmount, setVatAmount] = useState(0)
  const [duration, setDuration] = useState(''), [availableStartAt, setAvailableStartAt] = useState('')
  const [validUntil, setValidUntil] = useState(() => toLocalInput(requestExpiresAt))
  const [revisionReason, setRevisionReason] = useState(''), [items, setItems] = useState<QuoteItemInput[]>([emptyItem()])
  const [error, setError] = useState(''), [message, setMessage] = useState(''), [saving, setSaving] = useState(false)
  const idempotencyKey = useRef(crypto.randomUUID())

  useEffect(() => {
    quoteApi.getProviderQuote(requestId).then((current) => {
      setQuote(current)
      if (current) loadRevision(current)
    }).catch((reason: Error) => setError(reason.message))
  }, [requestId])

  const loadRevision = (current: QuoteDetail) => {
    setSummary(current.revision.summary)
    setTerms(current.revision.terms ?? '')
    setVatAmount(current.revision.vatAmount)
    setDuration(current.revision.estimatedDurationText ?? '')
    setAvailableStartAt(toLocalInput(current.revision.availableStartAt))
    setValidUntil(toLocalInput(current.revision.validUntil))
    setRevisionReason('')
    setItems(current.revision.items.map((item) => ({ itemName: item.itemName, description: item.description, quantity: item.quantity, unitText: item.unitText, unitPriceAmount: item.unitPriceAmount })))
  }

  const updateItem = (index: number, patch: Partial<QuoteItemInput>) =>
    setItems((current) => current.map((item, itemIndex) => itemIndex === index ? { ...item, ...patch } : item))
  const subtotal = items.reduce((sum, item) => sum + Number(item.quantity || 0) * Number(item.unitPriceAmount || 0), 0)
  const editable = quote?.canEdit ?? true

  const save = async () => {
    setSaving(true); setError(''); setMessage('')
    try {
      const payload: SaveQuoteRevisionInput = {
        summary,
        terms: terms || null,
        vatAmount: Number(vatAmount),
        estimatedDurationText: duration || null,
        availableStartAt: availableStartAt ? new Date(availableStartAt).toISOString() : null,
        validUntil: new Date(validUntil).toISOString(),
        revisionReason: revisionReason || null,
        idempotencyKey: idempotencyKey.current,
        items: items.map((item) => ({ ...item, quantity: Number(item.quantity), unitPriceAmount: Number(item.unitPriceAmount), description: item.description || null, unitText: item.unitText || null })),
      }
      const saved = quote
        ? await quoteApi.addQuoteRevision(quote.id, payload)
        : await quoteApi.createProviderQuote(requestId, payload)
      setQuote(saved); loadRevision(saved); idempotencyKey.current = crypto.randomUUID()
      setMessage(saved.status === 'SUBMITTED' ? '새 revision이 제출되었습니다.' : '견적이 임시저장되었습니다.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : '견적을 저장하지 못했습니다.')
    } finally { setSaving(false) }
  }

  const submit = async () => {
    if (!quote) return
    setSaving(true); setError(''); setMessage('')
    try {
      const submitted = await quoteApi.submitQuote(quote.id)
      setQuote(submitted); loadRevision(submitted); setMessage('견적이 고객에게 제출되었습니다.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : '견적을 제출하지 못했습니다.')
    } finally { setSaving(false) }
  }

  return <section className="detailCard quotePanel">
    <div className="sectionHeading"><div><p className="eyebrow">QUOTE</p><h2>견적 작성</h2></div>{quote && <span className="statusBadge">{quote.status} · REV {quote.revision.revisionNo}</span>}</div>
    {error && <div className="errorBanner">{error}</div>}{message && <div className="successBanner">{message}</div>}
    <div className="formGrid">
      <div className="formField fullWidth"><label>견적 요약 *</label><textarea rows={3} maxLength={1000} value={summary} disabled={!editable} onChange={(event) => setSummary(event.target.value)} /></div>
      <div className="formField fullWidth"><label>조건·설명</label><textarea rows={4} value={terms} disabled={!editable} onChange={(event) => setTerms(event.target.value)} /></div>
      <div className="formField"><label>예상 작업기간</label><input maxLength={200} value={duration} disabled={!editable} onChange={(event) => setDuration(event.target.value)} /></div>
      <div className="formField"><label>작업 가능 시작일</label><input type="datetime-local" value={availableStartAt} disabled={!editable} onChange={(event) => setAvailableStartAt(event.target.value)} /></div>
      <div className="formField"><label>견적 유효기간 *</label><input type="datetime-local" required value={validUntil} disabled={!editable} max={toLocalInput(requestExpiresAt)} onChange={(event) => setValidUntil(event.target.value)} /></div>
      <div className="formField"><label>부가세 금액</label><input type="number" min="0" step="0.0001" value={vatAmount} disabled={!editable} onChange={(event) => setVatAmount(Number(event.target.value))} /></div>
      {quote && <div className="formField fullWidth"><label>revision 수정 사유</label><input maxLength={1000} value={revisionReason} disabled={!editable} onChange={(event) => setRevisionReason(event.target.value)} /></div>}
    </div>
    <div className="quoteItems">
      <div className="sectionHeading"><h3>견적 항목</h3>{editable && <button className="secondaryButton inlineButton" type="button" onClick={() => setItems((current) => [...current, emptyItem()])}>항목 추가</button>}</div>
      {items.map((item, index) => <div className="quoteItemEditor" key={index}>
        <div className="formField"><label>항목명 *</label><input maxLength={200} value={item.itemName} disabled={!editable} onChange={(event) => updateItem(index, { itemName: event.target.value })} /></div>
        <div className="formField"><label>설명</label><input maxLength={1000} value={item.description ?? ''} disabled={!editable} onChange={(event) => updateItem(index, { description: event.target.value })} /></div>
        <div className="formField"><label>수량 *</label><input type="number" min="0.0001" step="0.0001" value={item.quantity} disabled={!editable} onChange={(event) => updateItem(index, { quantity: Number(event.target.value) })} /></div>
        <div className="formField"><label>단위</label><input maxLength={50} value={item.unitText ?? ''} disabled={!editable} onChange={(event) => updateItem(index, { unitText: event.target.value })} /></div>
        <div className="formField"><label>단가 *</label><input type="number" min="0" step="0.0001" value={item.unitPriceAmount} disabled={!editable} onChange={(event) => updateItem(index, { unitPriceAmount: Number(event.target.value) })} /></div>
        <div className="lineTotal"><span>항목 금액</span><strong>{formatMoney(item.quantity * item.unitPriceAmount)}</strong>{editable && items.length > 1 && <button type="button" onClick={() => setItems((current) => current.filter((_, itemIndex) => itemIndex !== index))}>삭제</button>}</div>
      </div>)}
    </div>
    <div className="quoteTotals"><span>소계 {formatMoney(subtotal)}</span><span>부가세 {formatMoney(vatAmount)}</span><strong>총액 {formatMoney(subtotal + vatAmount)}</strong></div>
    {editable && <div className="formActions"><button className="secondaryButton" type="button" disabled={saving || !summary.trim() || items.some((item) => !item.itemName.trim())} onClick={() => void save()}>{quote?.status === 'SUBMITTED' ? '수정 견적 제출' : '임시저장'}</button>{quote?.canSubmit && <button className="primaryButton" type="button" disabled={saving} onClick={() => void submit()}>견적 제출</button>}</div>}
    {quote?.transactionId && <p className="successBanner">선택된 견적입니다. 거래번호: {quote.transactionId}</p>}
  </section>
}

export function CustomerQuotesPanel({ requestId }: { requestId: string }) {
  const [quotes, setQuotes] = useState<CustomerQuoteComparison[]>([]), [selected, setSelected] = useState<CustomerQuoteDetail | null>(null)
  const [profile, setProfile] = useState<CustomerProviderProfile | null>(null), [accepting, setAccepting] = useState(false)
  const [error, setError] = useState(''), [message, setMessage] = useState(''), [loading, setLoading] = useState(true)
  const load = useCallback(() => quoteApi.getCustomerQuotes(requestId).then(setQuotes).catch((reason: Error) => setError(reason.message)).finally(() => setLoading(false)), [requestId])
  useEffect(() => { void load() }, [load])
  const show = async (quoteId: string) => { setError(''); setProfile(null); try { setSelected(await quoteApi.getCustomerQuote(quoteId)) } catch (reason) { setError(reason instanceof Error ? reason.message : '견적을 불러오지 못했습니다.') } }
  const showProfile = async () => { if (!selected) return; setError(''); try { setProfile(await quoteApi.getCustomerProviderProfile(selected.providerId, requestId)) } catch (reason) { setError(reason instanceof Error ? reason.message : '공급자 정보를 불러오지 못했습니다.') } }
  const accept = async () => {
    if (!selected) return
    if (!window.confirm(`${selected.providerName}의 견적 ${formatMoney(selected.revision.totalAmount)}을 선택할까요? 선택 후에는 다른 견적을 선택할 수 없습니다.`)) return
    setError(''); setAccepting(true)
    try {
      const result = await quoteApi.acceptQuote(selected.id)
      setMessage(`견적을 선택했습니다. 거래번호: ${result.transactionId}`)
      setSelected(await quoteApi.getCustomerQuote(selected.id)); await load()
    } catch (reason) { setError(reason instanceof Error ? reason.message : '견적을 선택하지 못했습니다.') } finally { setAccepting(false) }
  }
  return <section className="detailCard quotePanel">
    <div className="sectionHeading"><div><p className="eyebrow">RECEIVED QUOTES</p><h2>받은 견적</h2></div><span>{quotes.length}건</span></div>
    {error && <div className="errorBanner">{error}</div>}{message && <div className="successBanner">{message}</div>}
    {loading ? <p className="emptyState">견적을 불러오는 중입니다.</p> : quotes.length === 0 ? <p className="emptyState">아직 제출된 견적이 없습니다. 조건에 맞는 공급자에게 요청을 전달하고 있습니다.</p> : <><p className="privacyNote">Trust 현재값이 높은 순으로 표시합니다. 플랫폼이 특정 공급자를 추천하는 순위는 아닙니다.</p><div className="quoteCompareGrid">{quotes.map(item => <button key={item.id} className={selected?.id === item.id ? 'quoteCompareCard selectedQuote' : 'quoteCompareCard'} type="button" onClick={() => void show(item.id)}><span className="statusBadge">{item.isSelected ? '선택됨' : item.status}</span><h3>{item.providerName}</h3><strong>{formatMoney(item.totalAmount)}</strong><p>{item.trustDisplay} · 공개 리뷰 {item.publicReviewCount}건</p><small>{item.estimatedDurationText || '작업기간 협의'} · A/S 기준 {item.defaultWarrantyDays}일</small></button>)}</div></>}
    {selected && <div className="quoteDetail"><div className="sectionHeading"><div><h3>{selected.providerName}</h3><p>{selected.revision.summary}</p></div><strong>{formatMoney(selected.revision.totalAmount)}</strong></div><button className="secondaryButton inlineButton" type="button" onClick={() => void showProfile()}>공급자 정보 보기</button><dl><dt>Trust</dt><dd>{selected.comparison.trustDisplay}</dd><dt>승인 상태</dt><dd>공급자 {selected.comparison.providerApprovalStatus} · 서비스 {selected.comparison.serviceApprovalStatus}</dd><dt>필수 자격</dt><dd>{selected.comparison.requiredEvidenceSatisfied ? '확인됨' : '확인 중'}</dd><dt>제출일</dt><dd>{selected.submittedAt ? formatDate(selected.submittedAt) : '-'}</dd><dt>유효기간</dt><dd>{formatDate(selected.revision.validUntil)}</dd><dt>작업 가능일</dt><dd>{selected.revision.availableStartAt ? formatDate(selected.revision.availableStartAt) : '협의'}</dd><dt>예상 작업기간</dt><dd>{selected.revision.estimatedDurationText || '협의'}</dd><dt>A/S 기준</dt><dd>{selected.comparison.defaultWarrantyDays}일</dd><dt>조건·메모</dt><dd>{selected.revision.terms || '-'}</dd></dl><div className="ratingBars">{selected.comparison.ratingItemAverages.map(item => <span key={item.itemId}>{item.itemName} {item.averageValue.toFixed(1)} / {item.maxValue} ({item.ratingCount}건)</span>)}</div><div className="quoteItemTable">{selected.revision.items.map(item => <div key={item.lineNo}><span>{item.itemName}</span><span>{item.quantity} {item.unitText ?? ''}</span><span>{formatMoney(item.unitPriceAmount)}</span><strong>{formatMoney(item.lineTotalAmount)}</strong></div>)}</div><div className="quoteTotals"><span>소계 {formatMoney(selected.revision.subtotalAmount)}</span><span>부가세 {formatMoney(selected.revision.vatAmount)}</span><strong>총액 {formatMoney(selected.revision.totalAmount)}</strong></div>{selected.status === 'SUBMITTED' && <div className="formActions"><button className="primaryButton" disabled={accepting} type="button" onClick={() => void accept()}>{accepting ? '선택 처리 중…' : '이 견적 선택'}</button></div>}{selected.status === 'ACCEPTED' && <p className="successBanner">선택된 견적입니다. 상세주소와 업무 연락처는 이 공급자에게만 공개됩니다.</p>}</div>}
    {profile && <aside className="providerProfile"><div className="sectionHeading"><h3>{profile.businessName}</h3><button type="button" onClick={() => setProfile(null)}>닫기</button></div><p>{profile.trustDisplay} · 완료 서비스 {profile.completedServiceCount}건 · 공개 리뷰 {profile.publicReviewCount}건</p><p>승인 서비스: {profile.activeServices.join(', ') || '없음'}</p><p>필수 증빙 {profile.approvedEvidenceCount}/{profile.requiredEvidenceCount} 확인</p>{profile.recentReviews.map(review => <blockquote key={review.id}>{review.bodyText}<small>{formatDate(review.submittedAt)}</small></blockquote>)}</aside>}
  </section>
}

const formatMoney = (value: number) => new Intl.NumberFormat('ko-KR', { style: 'currency', currency: 'KRW', maximumFractionDigits: 4 }).format(value)
const formatDate = (value: string) => new Intl.DateTimeFormat('ko-KR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
function toLocalInput(value: string | null) { if (!value) return ''; const date = new Date(value); const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000); return local.toISOString().slice(0, 16) }
