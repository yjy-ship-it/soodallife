import { useEffect, useState } from 'react'
import { getAdminProviderRequirements, updateAdminMiddleOperationPolicies } from './providerRequirementApi'
import type { AdminProviderRequirement } from './providerRequirementTypes'
import { searchServiceCategories } from './serviceCategoryApi'
import type { AdminServiceCategoryDetail, AdminServiceCategoryListItem } from './serviceCategoryTypes'
import { soodalConfirm } from '../components/soodalDialog'

type ServicePolicy = { service: AdminServiceCategoryListItem; policy: AdminProviderRequirement | null }
type SafetyGrade = 'NORMAL' | 'MEDIUM' | 'HIGH'

export function AdminOperationPolicyPanel({ service }: { service: AdminServiceCategoryDetail }) {
  const [current, setCurrent] = useState<AdminProviderRequirement | null>(null)
  const [middlePolicies, setMiddlePolicies] = useState<ServicePolicy[]>([])
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [qualification, setQualification] = useState('')
  const [insurance, setInsurance] = useState('')
  const [safetyGrade, setSafetyGrade] = useState<SafetyGrade>('NORMAL')
  const [loading, setLoading] = useState(true), [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null), [notice, setNotice] = useState<string | null>(null)

  const load = async () => {
    const [selectedPolicies, middleServices] = await Promise.all([
      getAdminProviderRequirements(service.id),
      searchServiceCategories({ search: '', majorId: service.majorId, middleId: service.middleId, status: '', feeAmount: '', feeStatus: '', feeEffectiveFrom: '', feeEffectiveTo: '' }),
    ])
    const selectedPolicy = selectedPolicies.policies.find(policy => policy.id === selectedPolicies.currentPolicyId) ?? selectedPolicies.policies.find(policy => policy.isCurrentlyEffective) ?? null
    const rows = await Promise.all(middleServices.items.map(async item => {
      const policies = await getAdminProviderRequirements(item.id)
      return { service: item, policy: policies.policies.find(policy => policy.id === policies.currentPolicyId) ?? policies.policies.find(policy => policy.isCurrentlyEffective) ?? null }
    }))
    setCurrent(selectedPolicy); setMiddlePolicies(rows); setSelectedIds(new Set(rows.filter(row => row.policy).map(row => row.service.id)))
    if (selectedPolicy) { setQualification(selectedPolicy.qualificationAndLicenseRequirement); setInsurance(selectedPolicy.insuranceRequirement); setSafetyGrade(selectedPolicy.safetyGradeCode as SafetyGrade) }
  }

  useEffect(() => { setLoading(true); setError(null); load().catch(reason => setError(reason instanceof Error ? reason.message : '운영정책을 불러오지 못했습니다.')).finally(() => setLoading(false)) }, [service.id]) // eslint-disable-line react-hooks/exhaustive-deps

  const validate = () => {
    if (!qualification.trim()) { setError('자격·면허 요구사항을 입력해 주세요.'); return false }
    if (!insurance.trim()) { setError('보험 요구수준을 입력해 주세요.'); return false }
    return true
  }
  const saveMiddle = async () => {
    if (!validate()) return
    const targets = middlePolicies.filter(row => row.policy && selectedIds.has(row.service.id))
    if (!targets.length) { setError('일괄 적용할 하위 서비스를 선택해 주세요.'); return }
    if (!await soodalConfirm(`${service.middleName}의 선택한 하위 서비스 ${targets.length}개에 같은 운영정책을 적용하시겠습니까?`)) return
    setSaving(true); setError(null); setNotice(null)
    try {
      const updated = await updateAdminMiddleOperationPolicies(service.middleId, { qualificationAndLicenseRequirement: qualification.trim(), insuranceRequirement: insurance.trim(), safetyGradeCode: safetyGrade, services: targets.map(row => ({ serviceId: row.service.id, policyId: row.policy!.id, rowVersion: row.policy!.rowVersion })) })
      const byId = new Map(updated.map(policy => [policy.id, policy])); setMiddlePolicies(rows => rows.map(row => row.policy && byId.has(row.policy.id) ? { ...row, policy: byId.get(row.policy.id)! } : row))
      const selectedUpdated = targets.find(row => row.service.id === service.id)?.policy; if (selectedUpdated && byId.has(selectedUpdated.id)) setCurrent(byId.get(selectedUpdated.id)!)
      setNotice(`${service.middleName}의 선택한 하위 서비스 ${targets.length}개에 운영정책을 일괄 저장했습니다.`)
    } catch (reason) { setError(reason instanceof Error ? reason.message : '중분류 운영정책을 일괄 저장하지 못했습니다.') } finally { setSaving(false) }
  }
  const toggle = (id: string) => setSelectedIds(ids => { const next = new Set(ids); if (next.has(id)) next.delete(id); else next.add(id); return next })
  const availableIds = middlePolicies.filter(row => row.policy).map(row => row.service.id)

  if (loading) return <div className="pricePolicyLoading">운영정책을 불러오는 중입니다…</div>
  if (!current) return <div className="categoryFutureTab"><strong>현재 운영정책 없음</strong><p>이 하위 서비스에 현재 적용 중인 운영정책이 없습니다.</p></div>
  return <div className="operationPolicyManager">
    {(error || notice) && <div className={error ? 'adminError' : 'categorySuccess'} role="status">{error ?? notice}</div>}
    <section className="operationPolicyHeading"><div><span>중분류 운영정책</span><h3>{service.middleName}</h3><p>{service.majorName} › {service.middleName} · 대표 입력 화면: {service.name}</p></div><em>하위 서비스 일괄 반영</em></section>
    <div className="operationPolicyForm">
      <label>자격·면허 요구사항<textarea required maxLength={1000} value={qualification} onChange={event => setQualification(event.target.value)} placeholder="예: 관련 자격증·사업자등록증 확인"/><small>{qualification.length.toLocaleString('ko-KR')} / 1,000자</small></label>
      <label>보험 요구수준<input required maxLength={100} value={insurance} onChange={event => setInsurance(event.target.value)} placeholder="예: 배상책임보험 필수"/></label>
      <label>안전등급<select value={safetyGrade} onChange={event => setSafetyGrade(event.target.value as SafetyGrade)}><option value="NORMAL">일반</option><option value="MEDIUM">중</option><option value="HIGH">고</option></select></label>
    </div>
    <section className="middleOperationPolicy"><header><div><span>중분류 일괄 적용</span><h3>{service.middleName}</h3><p>선택한 하위 서비스에 위 입력값을 동일하게 적용합니다.</p></div><button type="button" onClick={() => setSelectedIds(selectedIds.size === availableIds.length ? new Set() : new Set(availableIds))}>{selectedIds.size === availableIds.length ? '전체 선택 해제' : '전체 선택'}</button></header>
      <div className="middleOperationPolicyServices">{middlePolicies.map(row => <label className={!row.policy ? 'disabled' : ''} key={row.service.id}><input type="checkbox" disabled={!row.policy || saving} checked={selectedIds.has(row.service.id)} onChange={() => toggle(row.service.id)}/><span><strong>{row.service.name}</strong><small>{row.policy ? `정책 ${row.policy.policyVersion} · ${row.policy.safetyGradeCode}` : '현재 적용 정책 없음'}</small></span></label>)}</div>
      <div className="operationPolicyBulkActions"><span>{selectedIds.size}개 선택</span><button type="button" disabled={saving || selectedIds.size === 0} onClick={() => void saveMiddle()}>{saving ? '저장 중…' : '중분류 운영정책 저장'}</button></div>
    </section>
  </div>
}
