import { useCallback, useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { createAdminPricePolicy, createAdminPricePolicyOption, createAdminPricePolicySurcharge, getAdminPricePolicies, updateAdminPricePolicy } from './pricePolicyApi'
import type { AdminPricePolicy, AdminPricePolicyList } from './pricePolicyTypes'

const statusLabels = { CURRENT: '현재 적용 중', SCHEDULED: '적용 예정', ENDED: '적용 종료', INACTIVE: '비활성' } as const
const won = (value: number | null) => value === null ? '원본 의미 확인 필요' : `${value.toLocaleString('ko-KR')}원`
const digits = (value: string) => value.replace(/[^0-9]/g, '')
const formatted = (value: string) => value ? Number(value).toLocaleString('ko-KR') : ''

export function AdminPricePoliciesPanel({ serviceId, serviceName }: { serviceId: string; serviceName: string }) {
  const [data, setData] = useState<AdminPricePolicyList | null>(null)
  const [selected, setSelected] = useState<AdminPricePolicy | null>(null)
  const [creating, setCreating] = useState(false)
  const [version, setVersion] = useState('')
  const [method, setMethod] = useState('')
  const [basePrice, setBasePrice] = useState('')
  const [minimumBudget, setMinimumBudget] = useState('')
  const [unit, setUnit] = useState('')
  const [vatRule, setVatRule] = useState('')
  const [effectiveFrom, setEffectiveFrom] = useState('')
  const [effectiveTo, setEffectiveTo] = useState('')
  const [isActive, setIsActive] = useState(true)
  const [optionName, setOptionName] = useState('')
  const [optionAmount, setOptionAmount] = useState('')
  const [surchargeName, setSurchargeName] = useState('')
  const [surchargeType, setSurchargeType] = useState<'AMOUNT' | 'RATE'>('AMOUNT')
  const [surchargeValue, setSurchargeValue] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const fillForm = useCallback((policy: AdminPricePolicy, asNew = false) => {
    setSelected(policy); setCreating(asNew); setVersion(asNew ? '' : policy.policyVersion); setMethod(policy.priceMethod)
    setBasePrice(String(policy.basePriceAmount)); setMinimumBudget(policy.minimumBudgetAmount === null ? '' : String(policy.minimumBudgetAmount)); setUnit(policy.unit ?? '')
    setVatRule(policy.vatRule); setEffectiveFrom(asNew ? '' : policy.effectiveFrom); setEffectiveTo(asNew ? '' : policy.effectiveTo ?? '')
    setIsActive(policy.isActive)
    setError(null); setNotice(null)
  }, [])

  const load = useCallback(async (preferredId?: string) => {
    const result = await getAdminPricePolicies(serviceId)
    setData(result)
    const preferred = result.policies.find((policy) => policy.id === preferredId)
      ?? result.policies.find((policy) => policy.id === result.currentPolicyId)
      ?? result.policies[0]
    if (preferred) fillForm(preferred)
  }, [fillForm, serviceId])

  useEffect(() => {
    setLoading(true); setError(null)
    load().catch((requestError: unknown) => setError(requestError instanceof Error ? requestError.message : '가격정책을 불러오지 못했습니다.'))
      .finally(() => setLoading(false))
  }, [load])

  const save = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); if (!selected) return
    if (effectiveTo && effectiveTo <= effectiveFrom) { setError('적용 종료일은 시작일보다 뒤여야 합니다.'); return }
    const input = {
      policyVersion: version.trim(), priceMethod: method,
      basePriceAmount: method === '견적형' ? null : Number(basePrice || 0),
      minimumBudgetAmount: method === '견적형' ? null : Number(minimumBudget || 0),
      unit: unit.trim() || null, vatRule, effectiveFrom, effectiveTo: effectiveTo || null, isActive,
    }
    if (creating && !window.confirm(`'${serviceName}'의 새 가격정책 버전을 등록합니다. 기존 정책의 적용 종료일이 조정될 수 있습니다. 계속할까요?`)) return
    setSaving(true); setError(null); setNotice(null)
    try {
      const saved = creating ? await createAdminPricePolicy(serviceId, input) : await updateAdminPricePolicy(serviceId, selected.id, input)
      await load(saved.id); setNotice(creating ? '새 가격정책 버전을 등록했습니다.' : '적용 예정 가격정책을 수정했습니다.')
    } catch (requestError) { setError(requestError instanceof Error ? requestError.message : '가격정책을 저장하지 못했습니다.') }
    finally { setSaving(false) }
  }

  const addOption = async () => {
    if (!selected || !optionName.trim()) return
    setSaving(true); setError(null)
    try {
      const saved = await createAdminPricePolicyOption(serviceId, selected.id, { optionName: optionName.trim(), additionalAmount: Number(optionAmount || 0), displayOrder: selected.options.length + 1, isActive: true })
      setOptionName(''); setOptionAmount(''); await load(saved.id); setNotice('가격 옵션을 등록했습니다.')
    } catch (requestError) { setError(requestError instanceof Error ? requestError.message : '가격 옵션을 등록하지 못했습니다.') }
    finally { setSaving(false) }
  }

  const addSurcharge = async () => {
    if (!selected || !surchargeName.trim() || !surchargeValue) return
    setSaving(true); setError(null)
    try {
      const numericValue = Number(surchargeValue)
      const saved = await createAdminPricePolicySurcharge(serviceId, selected.id, { surchargeName: surchargeName.trim(), calculationTypeCode: surchargeType,
        amount: surchargeType === 'AMOUNT' ? numericValue : null, rate: surchargeType === 'RATE' ? numericValue : null, displayOrder: selected.surcharges.length + 1, isActive: true })
      setSurchargeName(''); setSurchargeValue(''); await load(saved.id); setNotice('할증 정책을 등록했습니다.')
    } catch (requestError) { setError(requestError instanceof Error ? requestError.message : '할증 정책을 등록하지 못했습니다.') }
    finally { setSaving(false) }
  }

  if (loading) return <div className="pricePolicyLoading">가격정책을 불러오는 중입니다.</div>
  if (!data || data.policies.length === 0 || !selected) return <div className="categoryFutureTab"><strong>가격정책</strong><p>등록된 가격정책이 없습니다. 원본 정책 데이터 확인이 필요합니다.</p></div>
  const current = data.policies.find((policy) => policy.id === data.currentPolicyId)

  return <div className="pricePolicyManager">
    <section className="pricePolicyOverview">
      <div><span>현재 가격정책</span><strong>{current?.priceMethod ?? '적용 정책 없음'}</strong><p>{current ? `${won(current.basePriceAmount)} · 최소 희망예산 ${won(current.minimumBudgetAmount)}` : '현재 적용기간에 해당하는 정책이 없습니다.'}</p></div>
      <button type="button" onClick={() => fillForm(current ?? selected, true)}>새 버전 등록</button>
    </section>
    <div className="pricePolicyWorkspace">
      <section className="pricePolicyHistory" aria-label="가격정책 이력">
        <h3>정책 이력 <small>{data.policies.length}건</small></h3>
        {data.policies.map((policy) => <button type="button" className={selected.id === policy.id && !creating ? 'selected' : ''} key={policy.id} onClick={() => fillForm(policy)}>
          <span><strong>{policy.policyVersion}</strong><small>{policy.priceMethod}</small></span>
          <em className={policy.effectiveStatus.toLowerCase()}>{statusLabels[policy.effectiveStatus]}</em>
          <small>{policy.effectiveFrom} ~ {policy.effectiveTo ?? '종료일 없음'}</small>
        </button>)}
      </section>
      <form className="pricePolicyForm" onSubmit={save}>
        <header><div><span>{creating ? '새 정책 버전' : statusLabels[selected.effectiveStatus]}</span><h3>{creating ? '가격정책 등록' : selected.policyVersion}</h3></div>{selected.isReferenced && !creating && <em>요청 사용 이력 있음</em>}</header>
        {(error || notice) && <div className={error ? 'adminError' : 'categorySuccess'} role="status">{error ?? notice}</div>}
        {!creating && !selected.canEdit && <div className="pricePolicyProtection">이미 적용이 시작된 정책은 과거 기준을 보호하기 위해 수정할 수 없습니다. 가격 변경은 새 버전으로 등록해 주세요.</div>}
        <div className="pricePolicyGrid">
          <label>정책버전<input required maxLength={30} disabled={!creating && !selected.canEdit} value={version} onChange={(event) => setVersion(event.target.value)} placeholder="원본 정책 체계에 맞는 버전 입력" /></label>
          <label>가격방식<select disabled={!creating && !selected.canEdit} value={method} onChange={(event) => setMethod(event.target.value)}>{data.allowedPriceMethods.map((value) => <option key={value}>{value}</option>)}</select></label>
          {method !== '견적형' && <><label>기본요금<div className="wonInput"><input inputMode="numeric" disabled={!creating && !selected.canEdit} value={formatted(basePrice)} onChange={(event) => setBasePrice(digits(event.target.value))} /><span>원</span></div></label>
            <label>최소 희망예산<div className="wonInput"><input inputMode="numeric" disabled={!creating && !selected.canEdit} value={formatted(minimumBudget)} onChange={(event) => setMinimumBudget(digits(event.target.value))} /><span>원</span></div></label></>}
          {method === '견적형' && <div className="pricePolicyQuoteNotice">협의견적형 정책은 금액 입력을 강제하지 않으며 기존 정책값을 보존합니다.</div>}
          <label>표준 작업단위<input maxLength={100} disabled={!creating && !selected.canEdit} value={unit} onChange={(event) => setUnit(event.target.value)} /></label>
          <label>VAT 정책<select disabled={!creating && !selected.canEdit} value={vatRule} onChange={(event) => setVatRule(event.target.value)}>{data.allowedVatRules.map((value) => <option key={value}>{value}</option>)}</select></label>
          <label>적용 시작일<input required type="date" disabled={!creating && !selected.canEdit} value={effectiveFrom} onChange={(event) => setEffectiveFrom(event.target.value)} /></label>
          <label>적용 종료일<input type="date" disabled={!creating && !selected.canEdit} value={effectiveTo} onChange={(event) => setEffectiveTo(event.target.value)} /><small>비워 두면 종료일 없음</small></label>
          <label className="requestFieldCheck"><input type="checkbox" disabled={!creating && !selected.canEdit} checked={isActive} onChange={(event) => setIsActive(event.target.checked)} /><span>활성 정책</span></label>
        </div>
        {(creating || selected.canEdit) && <button className="categorySaveButton" disabled={saving || !version.trim() || !effectiveFrom} type="submit">{saving ? '저장 중…' : creating ? '새 버전 등록' : '적용 예정 정책 수정'}</button>}
        <section className="pricePolicyExtras"><h4>가격 옵션</h4>{selected.options.length === 0 ? <p>등록된 가격 옵션이 없습니다.</p> : selected.options.map((option) => <div key={option.id}><strong>{option.optionName}</strong><span>추가 {won(option.additionalAmount)} · {option.isActive ? '활성' : '비활성'}</span></div>)}
          {!creating && selected.canEdit && <div className="pricePolicyExtraForm"><input aria-label="가격 옵션명" value={optionName} onChange={(event) => setOptionName(event.target.value)} placeholder="옵션명" /><input aria-label="옵션 추가금액" inputMode="numeric" value={formatted(optionAmount)} onChange={(event) => setOptionAmount(digits(event.target.value))} placeholder="추가금액" /><button type="button" disabled={saving || !optionName.trim()} onClick={addOption}>옵션 등록</button></div>}
        </section>
        <section className="pricePolicyExtras"><h4>할증 정책</h4>{selected.surcharges.length === 0 ? <p>등록된 할증 정책이 없습니다.</p> : selected.surcharges.map((surcharge) => <div key={surcharge.id}><strong>{surcharge.surchargeName}</strong><span>{surcharge.calculationTypeCode === 'AMOUNT' ? won(surcharge.amount) : `비율 ${surcharge.rate}`} · {surcharge.isActive ? '활성' : '비활성'}</span></div>)}
          {!creating && selected.canEdit && <div className="pricePolicyExtraForm"><input aria-label="할증 정책명" value={surchargeName} onChange={(event) => setSurchargeName(event.target.value)} placeholder="할증명" /><select aria-label="할증 계산방식" value={surchargeType} onChange={(event) => setSurchargeType(event.target.value as 'AMOUNT' | 'RATE')}><option value="AMOUNT">금액</option><option value="RATE">비율</option></select><input aria-label="할증 값" inputMode="decimal" value={surchargeValue} onChange={(event) => setSurchargeValue(event.target.value.replace(/[^0-9.]/g, ''))} placeholder={surchargeType === 'AMOUNT' ? '금액' : '0~1 비율'} /><button type="button" disabled={saving || !surchargeName.trim() || !surchargeValue} onClick={addSurcharge}>할증 등록</button></div>}
        </section>
        <div className="pricePolicyLimitations"><p><strong>Legacy 가격방식</strong> 기존 예약가·견적형 원문을 보존하며 표준 가격유형을 임의 지정하지 않습니다.</p><p><strong>최소 희망예산</strong> 원본 의미가 확정되지 않아 권장 최소가격으로 바꾸지 않습니다.</p></div>
      </form>
    </div>
  </div>
}
