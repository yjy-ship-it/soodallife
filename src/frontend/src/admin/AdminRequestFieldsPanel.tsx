import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import {
  getAdminRequestField,
  getAdminRequestFields,
  updateAdminRequestFieldAssignment,
  updateAdminRequestFieldDefinition,
  updateAdminRequestFieldOption,
} from './requestFieldApi'
import type { AdminRequestField, AdminRequestFieldOption, RequestFieldDefinitionStatus, RequestFieldInputType } from './requestFieldTypes'

const inputTypeLabels: Record<RequestFieldInputType, string> = {
  ADDRESS: '주소', DATETIME: '일시', FILE: '파일·사진', LONG_TEXT: '장문', MONEY: '금액',
  NUMBER: '숫자', PERIOD: '기간', RECURRENCE: '반복 일정', SELECT: '선택', TEXT: '텍스트',
}
const inputTypes = Object.entries(inputTypeLabels) as Array<[RequestFieldInputType, string]>

export function AdminRequestFieldsPanel({ serviceId, serviceName }: { serviceId: string; serviceName: string }) {
  const [fields, setFields] = useState<AdminRequestField[]>([])
  const [selected, setSelected] = useState<AdminRequestField | null>(null)
  const [search, setSearch] = useState('')
  const [label, setLabel] = useState('')
  const [inputType, setInputType] = useState<RequestFieldInputType>('TEXT')
  const [required, setRequired] = useState(false)
  const [displayOrder, setDisplayOrder] = useState(0)
  const [definitionStatus, setDefinitionStatus] = useState<RequestFieldDefinitionStatus>('ACTIVE')
  const [serviceEnabled, setServiceEnabled] = useState(true)
  const [unit, setUnit] = useState('')
  const [validationRule, setValidationRule] = useState('')
  const [options, setOptions] = useState<AdminRequestFieldOption[]>([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const selectField = (field: AdminRequestField) => {
    setSelected(field); setLabel(field.label); setInputType(field.inputType); setRequired(field.required)
    setDisplayOrder(field.displayOrder); setDefinitionStatus(field.definitionStatus); setServiceEnabled(field.serviceEnabled)
    setUnit(field.unit ?? ''); setValidationRule(field.validationRule); setOptions(field.options); setError(null); setNotice(null)
  }

  useEffect(() => {
    setLoading(true)
    getAdminRequestFields(serviceId)
      .then((result) => { setFields(result); if (result.length > 0) selectField(result[0]) })
      .catch((requestError: unknown) => setError(requestError instanceof Error ? requestError.message : '요청 항목을 불러오지 못했습니다.'))
      .finally(() => setLoading(false))
  }, [serviceId])

  const filteredFields = useMemo(() => {
    const term = search.trim().toLocaleLowerCase('ko-KR')
    return term ? fields.filter((field) => field.label.toLocaleLowerCase('ko-KR').includes(term) || inputTypeLabels[field.inputType].includes(term)) : fields
  }, [fields, search])

  const replaceField = (updated: AdminRequestField) => {
    setFields((current) => current.map((field) => field.id === updated.id ? updated : field)
      .sort((left, right) => left.displayOrder - right.displayOrder || left.label.localeCompare(right.label, 'ko-KR')))
    selectField(updated)
  }
  const refreshSelected = async (fieldId: string) => replaceField(await getAdminRequestField(serviceId, fieldId))

  const saveDefinition = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); if (!selected) return
    let confirmSharedChange = selected.affectedServiceCount <= 1
    if (!confirmSharedChange) confirmSharedChange = window.confirm(`이 질문 정의는 같은 중분류의 서비스 ${selected.affectedServiceCount}곳에 공통 적용됩니다. 계속할까요?`)
    if (!confirmSharedChange) return
    setSaving(true); setError(null); setNotice(null)
    try {
      const updated = await updateAdminRequestFieldDefinition(serviceId, selected.id, {
        label: label.trim(), inputType, statusCode: definitionStatus, unit: unit.trim() || null,
        validationRule: validationRule.trim(), confirmSharedChange,
      })
      await refreshSelected(updated.id); setNotice('질문 정의를 저장했습니다.')
    } catch (requestError) { setError(requestError instanceof Error ? requestError.message : '질문 정의를 저장하지 못했습니다.') }
    finally { setSaving(false) }
  }

  const saveAssignment = async () => {
    if (!selected) return
    if (!serviceEnabled && !window.confirm(`이 질문을 '${serviceName}' 고객 요청 화면에서 사용하지 않도록 설정할까요?`)) return
    setSaving(true); setError(null); setNotice(null)
    try {
      replaceField(await updateAdminRequestFieldAssignment(serviceId, selected.id, { isActive: serviceEnabled, isRequired: required, displayOrder }))
      setNotice('이 서비스의 필수 여부, 표시 순서, 사용 상태를 저장했습니다.')
    } catch (requestError) { setError(requestError instanceof Error ? requestError.message : '서비스별 설정을 저장하지 못했습니다.') }
    finally { setSaving(false) }
  }

  const saveOption = async (option: AdminRequestFieldOption) => {
    if (!selected) return
    setSaving(true); setError(null); setNotice(null)
    try {
      replaceField(await updateAdminRequestFieldOption(serviceId, selected.id, option.id, {
        label: option.label.trim(), displayOrder: option.displayOrder, isActive: option.isActive,
      })); setNotice('선택값을 저장했습니다.')
    } catch (requestError) { setError(requestError instanceof Error ? requestError.message : '선택값을 저장하지 못했습니다.') }
    finally { setSaving(false) }
  }

  const updateOption = (id: string, change: Partial<AdminRequestFieldOption>) =>
    setOptions((current) => current.map((option) => option.id === id ? { ...option, ...change } : option))

  return <div className="requestFieldManager">
    <section className="requestFieldListPane">
      <header><div><strong>고객 요청 항목</strong><span>{fields.length}개 배정</span></div>
        <input aria-label="요청 항목 검색" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="질문명 또는 입력방식 검색" /></header>
      <div className="requestFieldResultCount">검색 결과 {filteredFields.length}개</div>
      <div className="requestFieldList">{loading ? <p>요청 항목을 불러오는 중입니다.</p> : filteredFields.length === 0 ? <p>조건에 맞는 요청 항목이 없습니다.</p> : filteredFields.map((field) =>
        <button className={selected?.id === field.id ? 'selected' : ''} key={field.id} type="button" onClick={() => selectField(field)}>
          <span className="requestFieldOrder">{field.displayOrder}</span><span className="requestFieldListText"><strong>{field.label}</strong>
          <small>{inputTypeLabels[field.inputType]} · {field.required ? '필수' : '선택'} · {field.hasOptions ? `선택값 ${field.options.length}개` : field.unit ? `단위 ${field.unit}` : '선택값 없음'}</small></span>
          <em className={field.serviceEnabled && field.definitionStatus === 'ACTIVE' ? 'using' : 'stopped'}>{field.serviceEnabled && field.definitionStatus === 'ACTIVE' ? '사용 중' : '미사용'}</em>
        </button>)}</div>
    </section>
    <section className="requestFieldEditPane">{!selected ? <div className="requestFieldEmpty"><strong>요청 항목을 선택해 주세요.</strong></div> : <>
      <header><div><span>{selected.assignmentScope === 'SERVICE_OVERRIDE' ? '서비스별 설정' : '중분류 공통 배정'}</span><h3>{selected.label}</h3></div><span className="requestFieldImpact">영향 서비스 {selected.affectedServiceCount}곳</span></header>
      {(error || notice) && <div className={error ? 'adminError requestFieldMessage' : 'categorySuccess requestFieldMessage'} role="status">{error ?? notice}</div>}
      <section className="serviceUsageBox requestFieldAssignmentBox">
        <div><strong>이 서비스에서의 적용 설정</strong><p>필수 여부, 표시 순서, 사용 상태는 선택한 서비스에만 적용됩니다.</p></div>
        <label className="requestFieldCheck"><input type="checkbox" checked={required} onChange={(event) => setRequired(event.target.checked)} /><span>필수 질문</span></label>
        <label>표시 순서<input type="number" min="0" value={displayOrder} onChange={(event) => setDisplayOrder(Number(event.target.value))} /></label>
        <label className="requestFieldSwitch"><input type="checkbox" checked={serviceEnabled} onChange={(event) => setServiceEnabled(event.target.checked)} /><span>{serviceEnabled ? '사용' : '미사용'}</span></label>
        <button type="button" disabled={saving} onClick={saveAssignment}>서비스별 설정 저장</button>
      </section>
      <form className="requestFieldDefinitionForm" onSubmit={saveDefinition}>
        <div className="sharedDefinitionNotice"><strong>질문 공통 정의</strong><span>아래 항목은 같은 질문을 사용하는 서비스에 공통 적용됩니다.</span></div>
        <label>고객에게 보여줄 질문<input required maxLength={200} value={label} onChange={(event) => setLabel(event.target.value)} /></label>
        <div className="requestFieldTwoColumns"><label>입력 방식<select value={inputType} onChange={(event) => setInputType(event.target.value as RequestFieldInputType)}>{inputTypes.map(([code, text]) => <option key={code} value={code}>{text}</option>)}</select></label>
          <label>질문 상태<select value={definitionStatus} onChange={(event) => setDefinitionStatus(event.target.value as RequestFieldDefinitionStatus)}><option value="ACTIVE">사용 중</option><option value="INACTIVE">전체 비활성</option></select></label></div>
        {inputType !== 'SELECT' && <label>단위 또는 안내말<input maxLength={2000} value={unit} onChange={(event) => setUnit(event.target.value)} /></label>}
        <label>검증 안내<input maxLength={1000} value={validationRule} onChange={(event) => setValidationRule(event.target.value)} /></label>
        <div className="requestFieldProtection">원본 추적 정보와 시스템 식별값은 이 화면에서 변경하지 않습니다.</div>
        <button className="categorySaveButton" type="submit" disabled={saving || !label.trim()}>{saving ? '저장 중…' : '질문 정의 저장'}</button>
      </form>
      {selected.inputType === 'SELECT' && <section className="requestFieldOptions"><div className="sharedDefinitionNotice"><strong>선택값 관리</strong><span>저장값은 보호되며 표시 문구, 순서, 활성 상태만 변경할 수 있습니다.</span></div>
        {options.length === 0 ? <p>등록된 선택값이 없습니다. 원본 데이터 보완이 필요합니다.</p> : options.map((option) => <div className="requestFieldOptionRow" key={option.id}>
          <label>저장값<input value={option.value} readOnly aria-label="변경할 수 없는 저장값" /></label>
          <label>표시 문구<input required maxLength={1000} value={option.label} onChange={(event) => updateOption(option.id, { label: event.target.value })} /></label>
          <label>순서<input type="number" min="0" value={option.displayOrder} onChange={(event) => updateOption(option.id, { displayOrder: Number(event.target.value) })} /></label>
          <label className="requestFieldCheck"><input type="checkbox" checked={option.isActive} onChange={(event) => updateOption(option.id, { isActive: event.target.checked })} /><span>활성</span></label>
          <button type="button" disabled={saving || !option.label.trim()} onClick={() => saveOption(option)}>선택값 저장</button>
        </div>)}</section>}
    </>}</section>
  </div>
}
