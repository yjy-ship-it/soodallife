import { useCallback, useEffect, useState } from 'react'
import { navigate } from '../auth/routing'
import { customerAccountApi } from './accountApi'
import type { WithdrawalDashboard } from './accountTypes'
import { MySoodalLayout } from './CustomerAccountPages'
import { soodalPrompt } from '../components/soodalDialog'

const terminal = new Set(['COMPLETED', 'REJECTED', 'CANCELLED'])
const labels: Record<string, string> = { REQUESTED: '신청 접수', UNDER_REVIEW: '관리자 검토 중', BLOCKED_BY_ACTIVE_WORK: '진행 업무 확인 필요', READY_TO_COMPLETE: '최종 확인 가능', COMPLETED: '고객 역할 종료', REJECTED: '신청 거절', CANCELLED: '신청 취소', CUSTOMER: '고객', PROVIDER: '전문가', ADMIN: '관리자' }
const label = (code: string) => labels[code] ?? code
const message = (value: unknown) => value instanceof Error ? value.message : '요청을 처리하지 못했습니다.'

export function CustomerWithdrawalPage() {
  const [data, setData] = useState<WithdrawalDashboard | null>(null); const [reason, setReason] = useState('')
  const [error, setError] = useState(''); const [busy, setBusy] = useState(false)
  const load = useCallback(() => customerAccountApi.withdrawal().then(setData).catch(value => setError(message(value))), [])
  useEffect(() => { void load() }, [load])
  const submit = async () => { if (!reason.trim()) return; setBusy(true); setError(''); try { await customerAccountApi.requestWithdrawal('CUSTOMER_ROLE', reason); setReason(''); await load() } catch (value) { setError(message(value)) } finally { setBusy(false) } }
  const cancel = async () => { if (!data?.request) return; const value = await soodalPrompt('탈퇴 신청 취소 사유를 입력해 주세요.'); if (!value?.trim()) return; setBusy(true); setError(''); try { await customerAccountApi.cancelWithdrawal(data.request.id, value, data.request.rowVersion); await load() } catch (failure) { setError(message(failure)) } finally { setBusy(false) } }
  const readiness = data?.readiness
  return <MySoodalLayout title="계정·회원 탈퇴" description="진행 업무와 기록 보존 원칙을 확인한 뒤 고객 역할 종료를 신청합니다.">
    <section className="withdrawalOverview"><header><div><span>현재 준비상태</span><strong>{readiness ? label(readiness.recommendedStatus) : '확인 중'}</strong></div><button className="accountSecondary" onClick={() => navigate('/customer/security')}>로그인·보안으로</button></header><p>{readiness?.guidance}</p><div className="withdrawalMetrics"><article><span>진행 업무</span><strong>{readiness?.activeWorkCount ?? '—'}건</strong></article><article><span>미종결 A/S</span><strong>{readiness?.openAfterServiceCount ?? '—'}건</strong></article><article><span>미종결 분쟁</span><strong>{readiness?.openDisputeCount ?? '—'}건</strong></article><article><span>금전 확인</span><strong>{readiness?.financialPendingCount ?? '—'}건</strong></article></div></section>
    {readiness?.blockers.length ? <section className="withdrawalPanel"><h2>먼저 처리할 업무</h2><div className="withdrawalBlockerList">{readiness.blockers.map(item => <article key={item.code}><b>{item.domain}</b><span>{item.label}</span><strong>{item.count}건</strong></article>)}</div></section> : <p className="withdrawalReady">현재 확인된 진행 업무 차단항목이 없습니다. 완료 시점에 서버가 다시 확인합니다.</p>}
    <section className="withdrawalPanel"><h2>역할과 기록 안내</h2><ul><li>현재 활성 역할: {readiness?.activeRoles.map(label).join(', ') || '확인 중'}</li><li>고객 역할만 종료하며 전문가 등 다른 역할은 종료하지 않습니다.</li><li>거래·서비스 이력·리뷰·채팅·감사 기록은 즉시 삭제하지 않습니다.</li><li>개인정보 보존·파기와 채팅 보존기간은 정책 확정 전 자동 처리하지 않습니다.</li><li>외부 PG 환불이나 송금을 완료한 것으로 처리하지 않습니다.</li></ul></section>
    {data?.request ? <section className="withdrawalPanel"><h2>내 탈퇴 신청</h2><dl><div><dt>상태</dt><dd>{label(data.request.status)}</dd></div><div><dt>신청 범위</dt><dd>{data.request.scope === 'CUSTOMER_ROLE' ? '고객 역할 종료' : '전체 계정 검토'}</dd></div><div><dt>관리자 처리</dt><dd>{data.request.decisionReason ?? '아직 처리되지 않았습니다.'}</dd></div></dl>{!terminal.has(data.request.status) && <button className="accountSecondary" disabled={busy} onClick={() => void cancel()}>탈퇴 신청 취소</button>}</section>
      : <section className="withdrawalPanel"><h2>고객 역할 탈퇴 신청</h2><p>신청 후 관리자가 진행 업무를 검토합니다. 진행 업무가 있더라도 신청은 접수되지만 최종 종료는 차단됩니다.</p><label>신청 사유<textarea maxLength={1000} value={reason} onChange={event => setReason(event.target.value)} placeholder="탈퇴 신청 사유를 입력해 주세요." /></label><button className="accountPrimary" disabled={busy || !reason.trim()} onClick={() => void submit()}>관리자 검토 요청</button></section>}
    {error && <p className="accountError">{error}</p>}
  </MySoodalLayout>
}
