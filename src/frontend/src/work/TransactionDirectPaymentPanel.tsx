import { useCallback, useEffect, useState, type FormEvent } from 'react'
import * as api from './api'
import type { DirectPaymentContext } from './types'

export function TransactionDirectPaymentPanel() {
  const match = window.location.pathname.match(/^\/(customer\/transactions|provider\/work)\/([0-9a-f-]{36})$/i)
  const transactionId = match?.[2]
  const [context, setContext] = useState<DirectPaymentContext | null>(null)
  const [loaded, setLoaded] = useState(false)
  const [error, setError] = useState('')
  const [amount, setAmount] = useState(0)
  const [method, setMethod] = useState('BANK_TRANSFER')
  const [paidAt, setPaidAt] = useState(() => new Date().toISOString().slice(0, 16))
  const [note, setNote] = useState('')
  const [reason, setReason] = useState('')
  const [evidence, setEvidence] = useState<File | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [message, setMessage] = useState('')
  const load = useCallback(async () => {
    if (!transactionId) return
    try { const value = await api.getDirectPayment(transactionId); setContext(value); if (value) setAmount(value.agreedAmount); setError('') }
    catch (e) { setError((e as Error).message) }
    finally { setLoaded(true) }
  }, [transactionId])
  useEffect(() => { void load() }, [load])
  if (!transactionId) return null
  if (loaded && !context && !error) return null

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!context) return
    if (!paidAt) { setError('실제 지급일시를 입력해 주세요.'); return }
    try {
      setSubmitting(true); setError(''); setMessage('')
      await api.registerDirectPayment(transactionId, { amount, paymentMethod: method, paidAt: new Date(paidAt).toISOString(), note, transactionRowVersion: context.transactionRowVersion, evidence })
      setMessage('지급 사실을 등록했습니다. 이제 상대방의 확인을 기다립니다.')
      await load()
    } catch (e) { setError((e as Error).message) }
    finally { setSubmitting(false) }
  }
  const decide = async (decision: 'CONFIRM' | 'REJECT') => {
    if (!context?.payment) return
    try { await api.decideDirectPayment(transactionId, context.payment.id, decision, decision === 'REJECT' ? reason : null, context.payment.rowVersion); await load() }
    catch (e) { setError((e as Error).message) }
  }
  const payment = context?.payment
  return <section className="workPanel directPaymentPanel" aria-labelledby="direct-payment-title">
    <h2 id="direct-payment-title">직접지급 확인</h2>
    <p className="privacyNote">서비스 대금은 고객이 전문가에게 직접 지급합니다. 이 화면은 지급 사실 확인 기록이며 PG·송금·에스크로 기능이 아닙니다.</p>
    <div className="directPaymentFlow" role="note"><strong>지급 사실 등록은 완료 처리를 막는 필수 단계가 아닙니다.</strong><span>전문가 완료 자료 제출 → 고객의 작업 완료 확인 → 리뷰 또는 A/S 관리 순서로 진행됩니다. 지급 기록은 실제로 돈을 지급한 뒤 당사자가 확인하기 위한 별도 증빙입니다.</span></div>
    {error && <div className="errorBanner">{error}</div>}
    {message && <div className="successBanner">{message}</div>}
    {!context && !error && <p>지급 확인 정보를 불러오는 중입니다.</p>}
    {context && !payment && ['IN_PROGRESS', 'COMPLETION_SUBMITTED', 'REVISION_REQUESTED', 'COMPLETED'].includes(context.transactionStatus) && <form onSubmit={submit}>
      <label>합의금액<input required min="1" type="number" value={amount || ''} onChange={e => setAmount(Number(e.target.value))} /></label>
      <label>지급수단<select value={method} onChange={e => setMethod(e.target.value)}><option value="BANK_TRANSFER">계좌이체</option><option value="ON_SITE_CARD">현장 카드</option><option value="CASH">현금</option><option value="OTHER">기타</option></select></label>
      <label>실제 지급일시<input required type="datetime-local" value={paidAt} onChange={e => setPaidAt(e.target.value)} /></label>
      <label>메모<textarea maxLength={1000} value={note} onChange={e => setNote(e.target.value)} /></label>
      <label>증빙 이미지(선택)<input type="file" accept="image/jpeg,image/png,image/webp" onChange={e => setEvidence(e.target.files?.[0] ?? null)} />{evidence&&<small className="selectedEvidenceName">선택한 파일: {evidence.name}</small>}</label>
      <button className="primary" type="submit" disabled={submitting}>{submitting?'등록 중…':'지급 사실 등록'}</button>
    </form>}
    {context && !payment && !['IN_PROGRESS', 'COMPLETION_SUBMITTED', 'REVISION_REQUESTED', 'COMPLETED'].includes(context.transactionStatus) && <p className="privacyNote">작업 시작 이후 지급 사실을 등록할 수 있습니다.</p>}
    {payment && <div>
      <dl className="customerInfoGrid"><div><dt>상태</dt><dd>{statusLabel(payment.status)}</dd></div><div><dt>금액</dt><dd>{money(payment.amount)}</dd></div><div><dt>지급수단</dt><dd>{methodLabel(payment.paymentMethod)}</dd></div><div><dt>지급일시</dt><dd>{date(payment.paidAt)}</dd></div></dl>
      {payment.note && <p>{payment.note}</p>}
      {payment.evidence && (payment.evidence.downloadUrl ? <a href={payment.evidence.downloadUrl}>지급 증빙 보기</a> : <p className="privacyNote">{payment.evidence.publicationMessage ?? '안전 확인 전에는 상대방에게 공개되지 않습니다.'}</p>)}
      {payment.canDecide && <div className="directPaymentDecision"><button className="primary" onClick={() => void decide('CONFIRM')}>지급 사실 확인</button><label>거절 사유<textarea required value={reason} onChange={e => setReason(e.target.value)} /></label><button className="danger" disabled={!reason.trim()} onClick={() => void decide('REJECT')}>확인 거절</button></div>}
      {payment.status === 'REJECTED' && <p className="workWarning">거절 사유: {payment.rejectionReason}</p>}
    </div>}
  </section>
}

const money = (value: number) => new Intl.NumberFormat('ko-KR', { style: 'currency', currency: 'KRW', maximumFractionDigits: 0 }).format(value)
const date = (value: string) => new Intl.DateTimeFormat('ko-KR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
const statusLabel = (value: string) => ({ REGISTERED: '상대방 확인 대기', COUNTERPART_CONFIRMED: '상대방 확인 완료', REJECTED: '확인 거절' }[value] ?? value)
const methodLabel = (value: string) => ({ BANK_TRANSFER: '계좌이체', ON_SITE_CARD: '현장 카드', CASH: '현금', OTHER: '기타' }[value] ?? value)
