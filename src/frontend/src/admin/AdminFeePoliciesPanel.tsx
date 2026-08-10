import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { createAdminFeePolicy, getAdminFeePolicies, updateAdminFeePolicy } from './feePolicyApi'
import type { AdminFeePolicy, AdminFeePolicyList, SaveAdminFeePolicyInput } from './feePolicyTypes'

const statusLabels = { CURRENT: '현재 적용', SCHEDULED: '적용 예정', ENDED: '적용 종료', INACTIVE: '비활성' } as const
const policyKindLabels: Record<string, string> = { QUOTE: '견적 채택 수수료', PROJECT: '프로젝트 수수료', SUPPORT: '정기구독 수수료' }
const transactionTypeLabels: Record<string, string> = { ONE_TIME: '일회성 거래', PROJECT: '프로젝트 거래', SUBSCRIPTION: '정기구독' }
const numberOrNull = (value: string) => value === '' ? null : Number(value)
const digits = (value: string) => value.replace(/[^0-9.]/g, '')
const won = (value: number | null, currency = 'KRW') => value === null ? '설정되지 않음' : `${value.toLocaleString('ko-KR')} ${currency === 'KRW' ? '원' : currency}`
const dateText = (value: string | null) => value ? value.replaceAll('-', '.') : '종료일 없음'

export function AdminFeePoliciesPanel({ serviceId, serviceName }: { serviceId: string; serviceName: string }) {
  const [data, setData] = useState<AdminFeePolicyList | null>(null)
  const [selected, setSelected] = useState<AdminFeePolicy | null>(null)
  const [creating, setCreating] = useState(false)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [version, setVersion] = useState('')
  const [policyKind, setPolicyKind] = useState('')
  const [transactionType, setTransactionType] = useState('')
  const [calculationMethod, setCalculationMethod] = useState('')
  const [feeAmount, setFeeAmount] = useState('')
  const [minBaseAmount, setMinBaseAmount] = useState('')
  const [maxBaseAmount, setMaxBaseAmount] = useState('')
  const [rate, setRate] = useState('')
  const [monthlyAmount, setMonthlyAmount] = useState('')
  const [perVisitAmount, setPerVisitAmount] = useState('')
  const [currency, setCurrency] = useState('KRW')
  const [chargeTiming, setChargeTiming] = useState('')
  const [restoreRule, setRestoreRule] = useState('')
  const [effectiveFrom, setEffectiveFrom] = useState('')
  const [effectiveTo, setEffectiveTo] = useState('')
  const [isActive, setIsActive] = useState(true)

  const current = useMemo(() => data?.policies.find((policy) => policy.id === data.currentPolicyId) ?? null, [data])

  const fillForm = (policy: AdminFeePolicy, asNew = false) => {
    setSelected(policy); setCreating(asNew); setVersion(asNew ? '' : policy.policyVersion)
    setPolicyKind(policy.policyKindCode); setTransactionType(policy.transactionTypeCode); setCalculationMethod(policy.calculationMethod ?? '')
    setFeeAmount(policy.feeAmount?.toString() ?? ''); setMinBaseAmount(policy.minBaseAmount?.toString() ?? ''); setMaxBaseAmount(policy.maxBaseAmount?.toString() ?? '')
    setRate(policy.rate?.toString() ?? ''); setMonthlyAmount(policy.monthlyAmount?.toString() ?? ''); setPerVisitAmount(policy.perVisitAmount?.toString() ?? '')
    setCurrency(policy.currencyCode); setChargeTiming(policy.chargeTiming); setRestoreRule(policy.restoreRule ?? '')
    setEffectiveFrom(asNew ? '' : policy.effectiveFrom); setEffectiveTo(asNew ? '' : policy.effectiveTo ?? ''); setIsActive(policy.isActive)
    setError(null); setNotice(null)
  }

  const load = async (preferredId?: string) => {
    const result = await getAdminFeePolicies(serviceId)
    setData(result)
    const preferred = result.policies.find((policy) => policy.id === preferredId)
      ?? result.policies.find((policy) => policy.id === result.currentPolicyId)
      ?? result.policies[0]
    if (preferred) fillForm(preferred)
  }

  useEffect(() => {
    setLoading(true)
    load().catch((requestError: unknown) => setError(requestError instanceof Error ? requestError.message : '수수료정책을 불러오지 못했습니다.'))
      .finally(() => setLoading(false))
  }, [serviceId]) // eslint-disable-line react-hooks/exhaustive-deps

  const save = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!selected) return
    if (!effectiveFrom) { setError('적용 시작일을 입력해 주세요.'); return }
    if (effectiveTo && effectiveTo <= effectiveFrom) { setError('적용 종료일은 시작일보다 뒤여야 합니다.'); return }
    const input: SaveAdminFeePolicyInput = {
      policyVersion: version.trim(), policyKindCode: policyKind, transactionTypeCode: transactionType,
      calculationMethod: calculationMethod || null, feeAmount: numberOrNull(feeAmount), minBaseAmount: numberOrNull(minBaseAmount),
      maxBaseAmount: numberOrNull(maxBaseAmount), rate: numberOrNull(rate), monthlyAmount: numberOrNull(monthlyAmount),
      perVisitAmount: numberOrNull(perVisitAmount), currencyCode: currency, chargeTiming,
      restoreRule: restoreRule.trim() || null, effectiveFrom, effectiveTo: effectiveTo || null, isActive,
    }
    if (creating && !window.confirm(`'${serviceName}'의 새 수수료정책 버전을 등록합니다. 기존 적용 정책의 종료일이 조정될 수 있습니다. 계속할까요?`)) return
    setSaving(true); setError(null); setNotice(null)
    try {
      const saved = creating ? await createAdminFeePolicy(serviceId, input) : await updateAdminFeePolicy(serviceId, selected.id, input)
      await load(saved.id)
      setNotice(creating ? '새 수수료정책 버전을 등록했습니다.' : '적용 예정 수수료정책을 수정했습니다.')
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : '수수료정책을 저장하지 못했습니다.')
    } finally { setSaving(false) }
  }

  if (loading) return <div className="pricePolicyLoading">수수료정책을 불러오는 중입니다.</div>
  if (!data || data.policies.length === 0 || !selected) return <div className="categoryFutureTab"><strong>수수료</strong><p>등록된 수수료정책이 없습니다. 원본 정책 데이터 확인이 필요합니다.</p></div>

  const readOnly = !creating && !selected.canEdit
  const previewAmount = numberOrNull(feeAmount)
  return <div className="pricePolicyManager feePolicyManager">
    {(error || notice) && <div className={error ? 'adminError' : 'categorySuccess'} role="status">{error ?? notice}</div>}
    <section className="pricePolicyOverview">
      <div><span>서비스</span><strong>{serviceName}</strong><p>현재 수수료 {won(current?.feeAmount ?? null, current?.currencyCode)} · {current ? dateText(current.effectiveFrom) : '현재 정책 없음'}부터</p></div>
      <button type="button" onClick={() => fillForm(current ?? selected, true)}>새 버전 등록</button>
    </section>
    <div className="pricePolicyWorkspace">
      <nav className="pricePolicyHistory" aria-label="수수료정책 이력"><h3>정책 이력 <small>{data.policies.length}건</small></h3>
        {data.policies.map((policy) => <button type="button" className={selected.id === policy.id && !creating ? 'selected' : ''} key={policy.id} onClick={() => fillForm(policy)}>
          <span><strong>{policy.policyVersion}</strong><b>{won(policy.feeAmount, policy.currencyCode)}</b></span>
          <small>{dateText(policy.effectiveFrom)} ~ {dateText(policy.effectiveTo)}</small>
          <em className={policy.effectiveStatus.toLowerCase()}>{statusLabels[policy.effectiveStatus]}</em>
        </button>)}
      </nav>
      <form className="pricePolicyForm" onSubmit={save}>
        <header><div><span>{creating ? '새 정책 버전' : statusLabels[selected.effectiveStatus]}</span><h3>{creating ? '수수료정책 등록' : selected.policyVersion}</h3></div>{selected.sourcePolicyCode && <em>원본 {selected.sourcePolicyCode}</em>}</header>
        {readOnly && <div className="pricePolicyProtection">이미 적용이 시작된 정책은 과거 거래 기준을 보호하기 위해 수정할 수 없습니다. 변경은 새 버전으로 등록해 주세요.</div>}
        {creating && <section className="feePolicyPreview" aria-label="수수료 변경 미리보기"><div><span>현재 수수료</span><strong>{won(current?.feeAmount ?? null, current?.currencyCode)}</strong></div><b>→</b><div><span>신규 수수료</span><strong>{won(previewAmount, currency)}</strong></div><p>기존 진행 거래는 기존 정책을 유지하고, 적용 시작 이후 신규 거래부터 새 정책을 조회합니다.</p></section>}
        <div className="pricePolicyGrid">
          <label>정책버전<input required maxLength={30} disabled={readOnly} value={version} onChange={(event) => setVersion(event.target.value)} placeholder="승인된 정책버전 입력" /></label>
          <label>정책 유형<select disabled={readOnly} value={policyKind} onChange={(event) => setPolicyKind(event.target.value)}>{data.allowedPolicyKinds.map((value) => <option key={value} value={value}>{policyKindLabels[value] ?? value}</option>)}</select></label>
          <label>거래 유형<select disabled={readOnly} value={transactionType} onChange={(event) => setTransactionType(event.target.value)}>{data.allowedTransactionTypes.map((value) => <option key={value} value={value}>{transactionTypeLabels[value] ?? value}</option>)}</select></label>
          <label>계산방식<select disabled={readOnly || data.allowedCalculationMethods.length === 0} value={calculationMethod} onChange={(event) => setCalculationMethod(event.target.value)}><option value="">설정되지 않음</option>{data.allowedCalculationMethods.map((value) => <option key={value}>{value}</option>)}</select><small>원본 데이터의 승인된 값만 제공합니다.</small></label>
          <label>예상·기준 수수료<div className="wonInput"><input inputMode="decimal" disabled={readOnly} value={feeAmount} onChange={(event) => setFeeAmount(digits(event.target.value))} /><span>원</span></div></label>
          <label>통화<select disabled={readOnly} value={currency} onChange={(event) => setCurrency(event.target.value)}>{data.allowedCurrencies.map((value) => <option key={value}>{value}</option>)}</select></label>
          <label>최소 기준금액<input inputMode="decimal" disabled={readOnly} value={minBaseAmount} onChange={(event) => setMinBaseAmount(digits(event.target.value))} placeholder="설정되지 않음" /></label>
          <label>최대 기준금액<input inputMode="decimal" disabled={readOnly} value={maxBaseAmount} onChange={(event) => setMaxBaseAmount(digits(event.target.value))} placeholder="설정되지 않음" /></label>
          <label>요율<input inputMode="decimal" disabled={readOnly} value={rate} onChange={(event) => setRate(digits(event.target.value))} placeholder="설정되지 않음" /><small>0 이상 1 이하</small></label>
          <label>월 정액<input inputMode="decimal" disabled={readOnly} value={monthlyAmount} onChange={(event) => setMonthlyAmount(digits(event.target.value))} placeholder="설정되지 않음" /></label>
          <label>회차 정액<input inputMode="decimal" disabled={readOnly} value={perVisitAmount} onChange={(event) => setPerVisitAmount(digits(event.target.value))} placeholder="설정되지 않음" /></label>
          <label>차감시점<select disabled={readOnly} value={chargeTiming} onChange={(event) => setChargeTiming(event.target.value)}>{data.allowedChargeTimings.map((value) => <option key={value}>{value}</option>)}</select></label>
          <label className="feePolicyWide">복원조건<textarea maxLength={1000} disabled={readOnly} value={restoreRule} onChange={(event) => setRestoreRule(event.target.value)} placeholder="설정되지 않음" /></label>
          <label>적용 시작일<input required type="date" disabled={readOnly} value={effectiveFrom} onChange={(event) => setEffectiveFrom(event.target.value)} /></label>
          <label>적용 종료일<input type="date" disabled={readOnly} value={effectiveTo} onChange={(event) => setEffectiveTo(event.target.value)} /><small>비워 두면 종료일 없음</small></label>
          <label className="requestFieldCheck"><input type="checkbox" disabled={readOnly} checked={isActive} onChange={(event) => setIsActive(event.target.checked)} /><span>활성 정책</span></label>
        </div>
        {!readOnly && <button className="categorySaveButton" disabled={saving || !version.trim() || !effectiveFrom || !chargeTiming} type="submit">{saving ? '저장 중…' : creating ? '새 버전 등록' : '적용 예정 정책 수정'}</button>}
        <div className="pricePolicyLimitations"><p><strong>실제 차감</strong> 이번 단계에서는 정책만 관리하며 공급자 충전금 차감이나 원장 복원은 실행하지 않습니다.</p><p><strong>거래 보존</strong> 거래 시점 정책 Snapshot 연결은 후속 거래 통합 단계에서 구현합니다.</p></div>
      </form>
    </div>
  </div>
}
