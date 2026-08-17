import { useCallback, useEffect, useRef, useState } from 'react'
import type { PropsWithChildren } from 'react'
import { navigate } from '../auth/routing'
import { CustomerAppLayout } from '../customer/CustomerAppLayout'
import { publicCatalogApi } from '../customer/api'
import type { PublicServiceDetail } from '../customer/types'
import { AuthenticatedFilePreview } from '../components/AuthenticatedFilePreview'
import * as requestApi from '../requests/api'
import { DynamicFieldInput } from '../requests/DynamicFieldInput'
import type { DynamicValue } from '../requests/DynamicFieldInput'
import type { AdministrativeArea, Category, RequestField, RequestFile, ServiceRequestDetail, ServiceRequestListItem } from '../requests/types'
import { CustomerQuotesPanel } from '../quotes/QuotePanels'
import { loadDefaultAddress } from '../customer/defaultAddress'
import './customerRequestFlow.css'

const steps = ['서비스', '요청 정보·지역', '일정', '사진·파일', '확인', '공개']

export function NewCustomerRequestPage() {
  const [step, setStep] = useState(0), [draftId, setDraftId] = useState<string | null>(null)
  const [majors, setMajors] = useState<Category[]>([]), [middles, setMiddles] = useState<Category[]>([]), [services, setServices] = useState<Category[]>([])
  const [sidos, setSidos] = useState<AdministrativeArea[]>([]), [areas, setAreas] = useState<AdministrativeArea[]>([]), [fields, setFields] = useState<RequestField[]>([])
  const [majorId, setMajorId] = useState(''), [middleId, setMiddleId] = useState(''), [serviceId, setServiceId] = useState(''), [sidoId, setSidoId] = useState(''), [areaId, setAreaId] = useState('')
  const [serviceDetail, setServiceDetail] = useState<PublicServiceDetail | null>(null)
  const [title, setTitle] = useState(''), [description, setDescription] = useState('')
  const emergencyMode = new URLSearchParams(window.location.search).get('emergency') === '1'
  const [isUrgent, setIsUrgent] = useState(emergencyMode), [answers, setAnswers] = useState<Record<string, DynamicValue>>({}), [files, setFiles] = useState<RequestFile[]>([])
  const [error, setError] = useState(''), [notice, setNotice] = useState(''), [busy, setBusy] = useState(false)
  const idempotencyKey = useRef(crypto.randomUUID())
  useEffect(() => { if (emergencyMode && !isUrgent) setIsUrgent(true) }, [emergencyMode, isUrgent])
  useEffect(() => {
    if (new URLSearchParams(window.location.search).has('draft')) return
    void loadDefaultAddress().then(value => {
      if (!value?.area) return
      setSidoId(current => current || value.sidoId)
      setAreaId(current => current || value.area!.id)
    }).catch(() => undefined)
  }, [])

  useEffect(() => {
    let cancelled = false
    const selectedId = new URLSearchParams(window.location.search).get('service')
    const load = async () => {
      setError('')
      const [majorResult, sidoResult, areaResult] = await Promise.allSettled([
        requestApi.getMajorCategories(emergencyMode),
        requestApi.getSidoAreas(),
        requestApi.getAdministrativeAreas(),
      ])
      if (cancelled) return
      if (majorResult.status === 'fulfilled') setMajors(majorResult.value)
      if (sidoResult.status === 'fulfilled') setSidos(sidoResult.value)
      if (areaResult.status === 'fulfilled') setAreas(areaResult.value)

      if (!selectedId) {
        const failed = [majorResult, sidoResult, areaResult].find(result => result.status === 'rejected')
        if (failed?.status === 'rejected') setError(failed.reason instanceof Error ? failed.reason.message : '요청에 필요한 기본 정보를 불러오지 못했습니다.')
        return
      }

      try {
        const selected = await publicCatalogApi.detail(selectedId)
        if (cancelled) return
        if (emergencyMode && !selected.emergencyRequestAllowed) {
          setError('선택한 서비스는 긴급출동을 지원하지 않습니다. 긴급출동 가능한 서비스를 선택해 주세요.')
          return
        }
        setMajorId(selected.majorId)
        setMiddleId(selected.middleId)
        setServiceId(selected.id)
        setServiceDetail(selected)

        const [middleItems, serviceItems, fieldItems] = await Promise.all([
          requestApi.getMiddleCategories(selected.majorId, emergencyMode),
          requestApi.getServiceCategories(selected.middleId, emergencyMode),
          requestApi.getRequestFields(selected.id),
        ])
        if (cancelled) return
        setMiddles(middleItems)
        setServices(serviceItems.some(item => item.id === selected.id) ? serviceItems : [...serviceItems, { id: selected.id, name: selected.name, level: 'SERVICE', externalCode: selected.code, sortOrder: Number.MAX_SAFE_INTEGER }])
        setFields(fieldItems)
        applyServiceDefaults(selected, fieldItems, setTitle, setAnswers)
        setStep(1)
        if (sidoResult.status === 'rejected' || areaResult.status === 'rejected') {
          setError('서비스 선택은 유지했지만 지역 목록을 불러오지 못했습니다. 새로고침 후 다시 선택해 주세요.')
        }
      } catch (reason) {
        if (!cancelled) setError(requestErrorMessage(reason, '선택한 서비스의 요청 항목을 불러오지 못했습니다. 잠시 후 다시 시도해 주세요.'))
      }
    }
    void load()
    return () => { cancelled = true }
  }, [emergencyMode])
  useEffect(() => { const resumeId = new URLSearchParams(window.location.search).get('draft'); if (!resumeId) return; requestApi.getMyRequest(resumeId).then(async draft => { if (!draft.canEdit) throw new Error('수정할 수 없는 요청입니다.'); const [middleItems, serviceItems, fieldItems, detail] = await Promise.all([requestApi.getMiddleCategories(draft.majorCategoryId, emergencyMode), requestApi.getServiceCategories(draft.middleCategoryId, emergencyMode), requestApi.getRequestFields(draft.serviceCategoryId), publicCatalogApi.detail(draft.serviceCategoryId)]); if (emergencyMode && !detail.emergencyRequestAllowed) throw new Error('이 임시 요청의 서비스는 현재 긴급출동을 지원하지 않습니다.'); setDraftId(draft.id); setMajorId(draft.majorCategoryId); setMiddleId(draft.middleCategoryId); setServiceId(draft.serviceCategoryId); setServiceDetail(detail); setMiddles(middleItems); setServices(serviceItems); setFields(fieldItems); setAreaId(draft.administrativeAreaId ?? ''); setSidoId(areas.find(item => item.id === draft.administrativeAreaId)?.parentId ?? ''); setTitle(draft.title === '작성 중인 요청' ? '' : draft.title); setDescription(draft.description ?? ''); setIsUrgent(draft.isUrgent); const restoredAnswers: Record<string, DynamicValue> = {}; const expiredScheduleLabels: string[] = []; for (const answer of draft.answers) { const field = fieldItems.find(candidate => candidate.id === answer.fieldId); if (field?.inputType === 'DATETIME') { const parsed = new Date(String(answer.value)); if (field.validationRule.includes('현재 이후') && !Number.isNaN(parsed.getTime()) && parsed.getTime() <= Date.now()) { expiredScheduleLabels.push(field.label); continue } restoredAnswers[answer.fieldId] = toLocalDateTimeValue(answer.value); continue } restoredAnswers[answer.fieldId] = answer.value as DynamicValue } setAnswers(restoredAnswers); setFiles(draft.files); setNotice(expiredScheduleLabels.length > 0 ? `지난 희망일시는 비웠습니다. 일정 단계에서 ${expiredScheduleLabels.join(', ')}을(를) 다시 선택해 주세요.` : '임시저장한 요청을 이어서 작성합니다.') }).catch((reason: Error) => setError(reason.message)) }, [areas, emergencyMode])
  const chooseMajor = async (id: string) => { setMajorId(id); setMiddleId(''); setServiceId(''); setMiddles(id ? await requestApi.getMiddleCategories(id, emergencyMode) : []); setServices([]); setFields([]) }
  const chooseMiddle = async (id: string) => { setMiddleId(id); setServiceId(''); setServices(id ? await requestApi.getServiceCategories(id, emergencyMode) : []); setFields([]) }
  const chooseService = async (id: string) => { setServiceId(id); setAnswers({}); setDescription(''); setError(''); if (!id) { setFields([]); setServiceDetail(null); return }; const [fieldItems, detail] = await Promise.all([requestApi.getRequestFields(id), publicCatalogApi.detail(id)]); setFields(fieldItems); setServiceDetail(detail); applyServiceDefaults(detail, fieldItems, setTitle, setAnswers); if (emergencyMode && !detail.emergencyRequestAllowed) setError('이 서비스는 긴급출동을 지원하지 않습니다. 다른 하위 서비스를 선택해 주세요.') }
  const valueAnswers = () => fields.filter(field => field.inputType !== 'FILE' && !isRetiredStructuralDuplicate(field)).flatMap(field => {
    const sourceBudget = fields.find(isInteriorPrimaryBudgetField)
    const rawValue = isInteriorService && isInteriorBudgetDuplicate(field)
      ? (sourceBudget ? answers[sourceBudget.id] : undefined) ?? 0
      : answers[field.id]
    if (isEmptyValue(rawValue)) return []
    const value = field.inputType === 'NUMBER' || field.inputType === 'MONEY'
      ? Number(rawValue)
      : field.inputType === 'DATETIME'
        ? new Date(String(rawValue)).toISOString()
        : rawValue
    return [{ fieldId: field.id, value }]
  })
  const draftPayload = () => ({ administrativeAreaId: areaId || null, title: title.trim() || null, description: description.trim() || null, detailAddress: null, isUrgent, answers: valueAnswers() })
  const saveDraft = async () => { if (!serviceId) throw new Error('서비스를 먼저 선택해 주세요.'); const id = draftId ?? (await requestApi.createServiceRequest({ categoryId: serviceId, idempotencyKey: idempotencyKey.current, ...draftPayload() })).id; if (!draftId) setDraftId(id); await requestApi.updateServiceRequest(id, draftPayload()); setNotice('현재 단계까지 안전하게 임시저장했습니다.'); return id }
  const validateCurrentStep = () => {
    if (step === 0 && !serviceId) return '하위 서비스를 선택해 주세요.'
    if (step === 1) {
      if (!title.trim()) return '요청 제목을 입력해 주세요.'
      const missing = detailFields.find(field => field.required && isEmptyValue(answers[field.id]))
      if (missing) return `${missing.label} 항목을 입력하거나 선택해 주세요.`
      if (!sidoId) return '서비스 받을 시·도를 선택해 주세요.'
      if (!areaId) return '서비스 받을 시·군·구를 선택해 주세요.'
    }
    if (step === 2) {
      const missing = scheduleFields.find(field => field.required && isEmptyValue(answers[field.id]))
      if (missing) return `${missing.label} 항목을 입력해 주세요.`
      const pastDateTime = scheduleFields.find(field => field.inputType === 'DATETIME' && field.validationRule.includes('현재 이후') && !isFutureDateTime(answers[field.id]))
      if (pastDateTime) return `${pastDateTime.label}은(는) 현재 이후의 날짜와 시간을 선택해 주세요.`
    }
    if (step === 3) {
      const missing = fileFields.find(field => field.required && !files.some(file => file.requestFieldId === field.id))
      if (missing) return `${missing.label} 파일을 첨부해 주세요.`
    }
    return null
  }
  const next = async () => { setBusy(true); setError(''); setNotice(''); try { const validationMessage = validateCurrentStep(); if (validationMessage) { setError(validationMessage); return } if (step === 0 && isUrgent && serviceDetail && !serviceDetail.emergencyRequestAllowed) throw new Error('긴급출동이 허용된 서비스를 선택해 주세요.'); await saveDraft(); setStep(value => Math.min(value + 1, steps.length - 1)) } catch (reason) { setError(requestErrorMessage(reason, '임시저장하지 못했습니다.')) } finally { setBusy(false) } }
  const upload = async (selected: FileList | null, requestFieldId?: string) => { if (!selected) return; const selectedFiles = Array.from(selected); if (selectedFiles.some(file => file.size > 5 * 1024 * 1024)) { setError('사진과 PDF 파일은 각각 5MB 이하만 업로드할 수 있습니다.'); return } setBusy(true); setError(''); try { const id = await saveDraft(); for (const file of selectedFiles) { const uploaded = await requestApi.uploadRequestFile(id, file, requestFieldId); setFiles(current => [...current, uploaded]) } setNotice('파일은 비공개 저장되었습니다. 악성코드 검사와 사진 개인정보 보호 처리는 아직 연동되지 않아 공급자 공개가 제한됩니다.') } catch (reason) { setError(reason instanceof Error ? reason.message : '파일을 업로드하지 못했습니다.') } finally { setBusy(false) } }
  const removeFile = async (file: RequestFile) => { if (!draftId) return; await requestApi.deleteRequestFile(draftId, file.id); setFiles(current => current.filter(item => item.id !== file.id)) }
  const publish = async () => { if (!draftId) return; setBusy(true); setError(''); try { if (isUrgent && serviceDetail && !serviceDetail.emergencyRequestAllowed) throw new Error('긴급출동이 허용된 서비스를 선택해 주세요.'); await saveDraft(); const result = await requestApi.publishServiceRequest(draftId); setNotice(result.customerMessage); setTimeout(() => navigate(`/customer/requests/${draftId}`, true), 500) } catch (reason) { setError(reason instanceof Error ? reason.message : '요청을 공개하지 못했습니다.') } finally { setBusy(false) } }
  const isInteriorService = serviceDetail?.majorName === '인테리어'
  const visibleFields = fields.filter(field => !isRetiredStructuralDuplicate(field) && !(isInteriorService && isInteriorBudgetDuplicate(field)))
  const scheduleFields = visibleFields.filter(field => ['DATETIME', 'PERIOD', 'RECURRENCE'].includes(field.inputType))
  const dateTimeFields = scheduleFields.filter(field => field.inputType === 'DATETIME')
  const detailFields = visibleFields.filter(field => !['DATETIME', 'PERIOD', 'RECURRENCE', 'FILE'].includes(field.inputType))
  const fileFields = visibleFields.filter(field => field.inputType === 'FILE')
  const selectedService = services.find(item => item.id === serviceId)?.name

  return <CustomerAppLayout><div className={`requestJourney${emergencyMode ? ' emergencyRequestJourney' : ''}`}><header className="journeyHeader"><p className="eyebrow">{emergencyMode ? 'SOODAL EMERGENCY' : 'SERVICE REQUEST'}</p><h1>{emergencyMode ? '🚨 긴급출동 요청하기' : '서비스 요청하기'}</h1><p>{emergencyMode ? '일반 요청과 같은 안전한 절차로 등록하며, 긴급출동이 허용된 서비스와 현재 출동 가능한 공급자만 연결합니다.' : '필요한 내용만 단계별로 묻고, 매 단계 서버에 임시저장합니다.'}</p>{emergencyMode && <div className="emergencySafetyInline"><strong>즉시 위험하면 119 등 공공 긴급대응을 먼저 이용하세요.</strong><span>GPS 자동 추적과 자동 ETA는 아직 연동되지 않았으며 공급자가 직접 도착예정시간을 안내합니다.</span></div>}</header><ol className="journeySteps">{steps.map((label, index) => <li key={label} className={index === step ? 'active' : index < step ? 'done' : ''}><span>{index + 1}</span><small>{label}</small></li>)}</ol>
  {error && <div className="errorBanner" role="alert">{error}</div>}{notice && <div className="successBanner" role="status">{notice}</div>}
  <section className="journeyCard">
    {step === 0 && <><h2>어떤 서비스가 필요하세요?</h2>{emergencyMode && <p className="privacyNote">현재 정책에서 긴급출동이 허용된 서비스만 표시합니다.</p>}<div className="formGrid threeColumns"><SelectField label="대분류" value={majorId} items={majors} onChange={chooseMajor}/><SelectField label="중분류" value={middleId} items={middles} onChange={chooseMiddle} disabled={!majorId}/><SelectField label="하위 서비스" value={serviceId} items={services} onChange={chooseService} disabled={!middleId}/></div>{emergencyMode && majors.length === 0 && <p className="emptyState">현재 긴급출동 요청이 가능한 서비스가 없습니다.</p>}</>}
    {step === 1 && <><h2>{selectedService} 요청을 알려주세요</h2><div className="formGrid"><div className="formField fullWidth"><label>요청 제목 *</label><input maxLength={200} value={title} onChange={event => setTitle(event.target.value)} /><small>선택한 하위 서비스에 맞춰 자연스러운 제목을 자동으로 채웠습니다. 필요하면 수정해 주세요.</small></div>{detailFields.map(field => <GuidedRequestField key={field.id} field={field} value={answers[field.id]} service={serviceDetail} interior={isInteriorService} onChange={value => setAnswers(current => ({ ...current, [field.id]: value }))}/>) }<h3 className="regionFieldHeading fullWidth">서비스 받을 시·도/시·군·구</h3><div className="formField"><label>시·도 *</label><select value={sidoId} onChange={event => { setSidoId(event.target.value); setAreaId('') }}><option value="">선택하세요</option>{sidos.map(area => <option key={area.id} value={area.id}>{area.name}</option>)}</select></div><div className="formField"><label>시·군·구 *</label><select value={areaId} disabled={!sidoId} onChange={event => setAreaId(event.target.value)}><option value="">선택하세요</option>{areas.filter(area => area.parentId === sidoId).map(area => <option key={area.id} value={area.id}>{area.name}</option>)}</select></div><p className="privacyNote fullWidth">요청할 때는 시·도와 시·군·구만 저장합니다. 상세주소는 견적 채택 시 입력하고 선택된 공급자에게만 공개합니다.</p><div className="formField fullWidth"><label>추가 설명</label><textarea rows={5} maxLength={20000} value={description} onChange={event => setDescription(event.target.value)} placeholder="공급자가 알아야 할 내용이나 요청사항을 자유롭게 입력해 주세요."/><small>선택 항목에 없는 내용만 자유롭게 적어 주세요.</small></div>{!isInteriorService && <label className="emergencyChoice fullWidth"><input type="checkbox" checked={isUrgent} onChange={event => setIsUrgent(event.target.checked)}/><span aria-hidden="true">🚨</span><strong>긴급요청</strong><small>즉시 위험한 상황은 119 등 공공 긴급대응을 먼저 이용하세요.</small></label>}</div></>}
    {step === 2 && <><h2>희망 일정을 알려주세요</h2>{scheduleFields.length ? <div className="formGrid">{scheduleFields.map(field => { const dateIndex = dateTimeFields.findIndex(item => item.id === field.id); const heading = field.inputType === 'DATETIME' && dateTimeFields.length > 1 ? `희망일시 ${dateIndex + 1}순위` : field.label; return <div key={field.id} className="scheduleChoice"><strong>{heading}</strong><DynamicFieldInput field={{ ...field, label: '', helperText: field.inputType === 'DATETIME' ? dateIndex === 0 ? '현재 이후' : '앞선 희망일시와 다른 날짜·시간을 선택해 주세요.' : field.helperText }} value={answers[field.id]} onChange={value => setAnswers(current => ({ ...current, [field.id]: value }))}/></div> })}</div> : <p className="emptyState">이 서비스에는 별도 일정 질문이 없습니다. 공급자와 견적 단계에서 조율할 수 있습니다.</p>}</>}
    {step === 3 && <><h2>현장 사진이나 참고 파일이 있나요?</h2><p>JPG, JPEG, PNG, PDF 파일은 각각 5MB 이하만 올릴 수 있습니다. 외부 검사가 연동되기 전에는 공급자 공개가 차단됩니다.</p>{fileFields.map(field => <label className="fileDrop" key={field.id}><input type="file" required={field.required} accept=".jpg,.jpeg,.png,.pdf,image/jpeg,image/png,application/pdf" multiple onChange={event => void upload(event.target.files, field.id)}/><span>{field.label}{field.required ? ' *' : ''}</span></label>)}<label className="fileDrop"><input type="file" accept=".jpg,.jpeg,.png,.pdf,image/jpeg,image/png,application/pdf" multiple onChange={event => void upload(event.target.files)}/><span>추가 사진·PDF 선택</span></label><div className="attachmentPreviewGrid">{files.map(file => <FilePreview key={file.id} file={file} onRemove={() => void removeFile(file)} />)}</div></>}
    {step === 4 && <><h2>요청 내용을 모두 확인하세요</h2><ReviewSection title="서비스" onEdit={() => setStep(0)}><p>{majors.find(item => item.id === majorId)?.name} › {middles.find(item => item.id === middleId)?.name} › {selectedService}</p></ReviewSection><ReviewSection title="요청 정보·지역" onEdit={() => setStep(1)}><dl className="reviewList"><dt>제목</dt><dd>{title || '미입력'}</dd><dt>지역</dt><dd>{sidos.find(item => item.id === sidoId)?.name} {areas.find(item => item.id === areaId)?.name}</dd>{!isInteriorService && <><dt>긴급요청</dt><dd>{isUrgent ? '예' : '아니오'}</dd></>}{detailFields.map(field => <span className="reviewPair" key={field.id}><dt>{field.label}</dt><dd>{formatFieldAnswer(field, answers[field.id])}</dd></span>)}<dt>추가 설명</dt><dd>{description || '입력 없음'}</dd></dl></ReviewSection><ReviewSection title="일정" onEdit={() => setStep(2)}><dl className="reviewList">{scheduleFields.map(field => { const dateIndex = dateTimeFields.findIndex(item => item.id === field.id); const label = field.inputType === 'DATETIME' && dateTimeFields.length > 1 ? `${dateIndex + 1}순위` : field.label; return <span className="reviewPair" key={field.id}><dt>{label}</dt><dd>{formatAnswer(answers[field.id])}</dd></span> })}</dl></ReviewSection><ReviewSection title={`사진·문서 ${files.length}개`} onEdit={() => setStep(3)}><div className="attachmentPreviewGrid">{files.map(file => <FilePreview key={file.id} file={file} />)}</div></ReviewSection><p className="privacyNote">공개 후 조건이 맞는 승인 공급자에게만 요청이 전달됩니다. 상세주소와 연락처는 채택 전까지 저장·공개하지 않습니다.</p><button className="secondaryButton" type="button" disabled={busy} onClick={() => void saveDraft()}>임시저장</button></>}
    {step === 5 && <><h2>공급자에게 요청을 공개할까요?</h2><p>공개하면 승인 상태, 서비스 분야, 활동지역, 필수 자격요건을 모두 충족한 공급자만 견적을 보낼 수 있습니다.</p><button className="primaryButton publishButton" type="button" disabled={busy} onClick={() => void publish()}>{busy ? '공개 중…' : '요청 공개하기'}</button></>}
  </section><div className="journeyActions"><button className="secondaryButton" type="button" disabled={busy} onClick={() => step === 0 ? navigate('/services') : setStep(value => value - 1)}>이전</button>{step < 5 && <button className="primaryButton" type="button" disabled={busy || (step === 0 && !serviceId)} onClick={() => void next()}>{busy ? '저장 중…' : '저장하고 다음'}</button>}</div></div></CustomerAppLayout>
}

export function CustomerRequestListPage() { const [items, setItems] = useState<ServiceRequestListItem[]>([]), [error, setError] = useState(''), [loading, setLoading] = useState(true); useEffect(() => { requestApi.getMyRequests().then(setItems).catch((reason: Error) => setError(reason.message)).finally(() => setLoading(false)) }, []); return <CustomerAppLayout><div className="requestJourney"><header className="journeyHeader"><p className="eyebrow">MY REQUESTS</p><h1>요청·견적</h1><button className="primaryButton inlineButton" onClick={() => navigate('/customer/requests/new')}>새 요청</button></header>{error && <div className="errorBanner">{error}</div>}{loading ? <p className="emptyState">불러오는 중…</p> : items.length === 0 ? <p className="emptyState">등록한 요청이 없습니다.</p> : <section className="requestList">{items.map(item => <button key={item.id} className="requestListItem" onClick={() => navigate(`/customer/requests/${item.id}`)}><div><span className="statusBadge">{item.displayStatus}</span><h2>{item.title}</h2><p>{item.categoryPath}</p></div><div className="requestMeta"><strong>견적 {item.quoteCount}건</strong><span>{formatDate(item.createdAt)}</span></div></button>)}</section>}</div></CustomerAppLayout> }

export function CustomerRequestDetailPage({ requestId }: { requestId: string }) {
  const [item, setItem] = useState<ServiceRequestDetail | null>(null), [error, setError] = useState('')
  const load = useCallback(() => requestApi.getMyRequest(requestId).then(setItem).catch((reason: Error) => setError(reason.message)), [requestId])
  useEffect(() => { void load() }, [load])
  const cancel = async () => {
    if (!item || !window.confirm('이 요청을 취소할까요?')) return
    await requestApi.cancelServiceRequest(item.id, '고객 요청 취소')
    await load()
  }
  return <CustomerAppLayout><div className="requestJourney">{error ? <div className="errorBanner">{error}</div> : !item ? <p className="emptyState">요청을 불러오는 중…</p> : <>
    <header className="journeyHeader"><span className="statusBadge">{item.displayStatus}</span><h1>{item.title}</h1><p>{item.categoryPath} · 견적 {item.quoteCount}건</p></header>
    <section className="detailCard"><dl><dt>등록일</dt><dd>{formatDate(item.createdAt)}</dd><dt>지역</dt><dd>{item.administrativeAreaName ?? '미입력'}</dd><dt>상세주소</dt><dd>{item.detailAddress || '견적 채택 시 입력'}<small className="privateTag"> 선택 공급자에게만 공개</small></dd><dt>선택 공급자</dt><dd>{item.selectedProviderName ?? '아직 선택하지 않음'}</dd><dt>추가 설명</dt><dd>{item.description || '입력 없음'}</dd></dl></section>
    {item.answers.length > 0 && <section className="detailCard"><h2>요청 답변</h2><dl>{item.answers.map(answer => <div key={answer.fieldId} className="answerRow"><dt>{answer.label}</dt><dd>{formatAnswer(answer.value)}</dd></div>)}</dl></section>}
    {item.files.length > 0 && <section className="detailCard"><h2>첨부파일</h2><div className="authenticatedFilePreviewGrid">{item.files.map(file => <FilePreview key={file.id} file={file} />)}</div></section>}
    <CustomerQuotesPanel requestId={requestId}/><div className="formActions"><button className="secondaryButton" onClick={() => navigate('/customer/requests')}>목록</button>{item.canEdit && <button className="primaryButton" onClick={() => navigate(`/customer/requests/new?draft=${item.id}`)}>이어서 작성</button>}{item.canCancel && <button className="dangerButton" onClick={() => void cancel()}>요청 취소</button>}</div>
  </>}</div></CustomerAppLayout>
}

function SelectField({ label, value, items, onChange, disabled = false }: { label: string; value: string; items: Category[]; onChange: (id: string) => void; disabled?: boolean }) { return <div className="formField"><label>{label} *</label><select disabled={disabled} value={value} onChange={event => void onChange(event.target.value)}><option value="">선택하세요</option>{items.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></div> }
function naturalRequestTitle(name: string) {
  if (name.includes('세면대') && name.includes('막힘')) return '세면대가 막혔어요'
  if (name.includes('변기') && name.includes('막힘')) return '변기가 막혔어요'
  if (name.includes('변기')) return '변기 점검이 필요해요'
  if (name.includes('누수')) return '누수가 발생했어요'
  if (name.includes('에어컨') && name.includes('청소')) return '에어컨 청소가 필요해요'
  if (name.includes('보일러')) return '보일러 점검이 필요해요'
  return `${name || '서비스'}이 필요해요`
}
function isRetiredStructuralDuplicate(field: RequestField) {
  const label = field.label.replace(/[^\p{L}\p{N}]/gu, '')
  return ['service_address', 'request_address'].includes(field.fieldKey.toLowerCase()) || label === '서비스주소'
}
function normalizedFieldLabel(field: RequestField) { return field.label.replace(/[^\p{L}\p{N}]/gu, '') }
function isInteriorBudgetDuplicate(field: RequestField) { return normalizedFieldLabel(field) === '희망예산' }
function isInteriorPrimaryBudgetField(field: RequestField) { return normalizedFieldLabel(field) === '희망비용' }
type GuidedFieldKind = 'CONTENT' | 'BUDGET' | 'SYMPTOM' | 'QUANTITY' | 'CONDITION' | 'OTHER'
function guidedFieldKind(field: RequestField): GuidedFieldKind {
  const key = field.fieldKey.toLowerCase()
  const label = field.label.replace(/[^\p{L}\p{N}]/gu, '')
  if (['request_detail', 'request_content'].includes(key) || label === '요청내용') return 'CONTENT'
  if (['desired_cost', 'desired_price'].includes(key) || label === '희망비용') return 'BUDGET'
  if (['symptom_detail', 'request_symptom_detail'].includes(key) || label === '요청증상상세') return 'SYMPTOM'
  if (key === 'quantity_scale' || label === '수량규모') return 'QUANTITY'
  if (key === 'site_condition' || label === '현장조건') return 'CONDITION'
  return 'OTHER'
}
function serviceRequestContent(name: string) {
  if (name.includes('세면대') && name.includes('막힘')) return '세면대 물이 원활하게 내려가지 않습니다. 막힘 원인을 확인하고 배수 상태를 정상화해 주세요.'
  if (name.includes('변기') && name.includes('막힘')) return '변기 물이 원활하게 내려가지 않습니다. 막힘 원인을 확인하고 정상적으로 사용할 수 있게 수리해 주세요.'
  if (name.includes('누수')) return '물이 새는 위치와 원인을 확인하고 필요한 보수 범위와 예상 비용을 안내해 주세요.'
  if (name.includes('에어컨') && name.includes('청소')) return '에어컨 내부 오염 상태를 확인하고 필터와 열교환기 등 필요한 부분을 청소해 주세요.'
  return `${name} 서비스가 필요합니다. 현재 상태를 확인하고 필요한 작업과 예상 비용을 안내해 주세요.`
}
function serviceSymptoms(name: string) {
  if (name.includes('세면대') && name.includes('막힘')) return ['물이 천천히 내려가요', '물이 전혀 내려가지 않아요', '물이 역류해요', '악취가 나요', '누수도 함께 있어요']
  if (name.includes('변기')) return ['물이 천천히 내려가요', '물이 전혀 내려가지 않아요', '물이 역류해요', '물이 계속 흘러요', '악취가 나요']
  if (name.includes('누수')) return ['물이 조금씩 새요', '물이 계속 흘러요', '벽·천장이 젖었어요', '바닥에 물이 고여요', '누수 위치를 모르겠어요']
  return [`${name} 기능이 작동하지 않아요`, `${name} 상태가 평소와 달라요`, '소음이나 냄새가 발생해요', '정확한 증상을 모르겠어요']
}
function quantityOptions(name: string, numeric = false) {
  if (numeric) return ['1', '2', '3', '4']
  const unit = name.includes('에어컨') ? '대' : name.includes('방') || name.includes('공간') ? '곳' : '개'
  return [`1${unit}`, `2${unit}`, `3${unit}`, `4${unit} 이상`, '수량을 잘 모르겠음']
}
const conditionOptions = ['좋음 - 작업 공간과 접근이 충분함', '보통 - 일반적인 작업 환경', '나쁨 - 공간이 좁거나 접근이 어려움', '아주 나쁨 - 심한 오염·장애물 등 추가 작업 예상', '잘 모르겠음']
function fieldOptions(field: RequestField, serviceName: string, kind: GuidedFieldKind) {
  if (field.options.length > 0) return field.options
  if (kind === 'SYMPTOM') return serviceSymptoms(serviceName)
  if (kind === 'QUANTITY') return quantityOptions(serviceName, ['NUMBER', 'MONEY'].includes(field.inputType))
  if (kind === 'CONDITION') return conditionOptions
  return []
}
function standardAmount(service: PublicServiceDetail | null) {
  return service?.price?.referenceAmount ?? service?.price?.recommendedMinAmount ?? service?.price?.recommendedMaxAmount ?? null
}
function applyServiceDefaults(service: PublicServiceDetail, fields: RequestField[], setTitle: (value: string) => void, setAnswers: (value: Record<string, DynamicValue>) => void) {
  const defaults: Record<string, DynamicValue> = {}
  for (const field of fields) {
    const kind = guidedFieldKind(field)
    if (kind === 'CONTENT') defaults[field.id] = serviceRequestContent(service.name)
    if (kind === 'BUDGET') {
      const amount = standardAmount(service)
      if (amount !== null) defaults[field.id] = amount
    }
    if (kind === 'SYMPTOM' || kind === 'QUANTITY' || kind === 'CONDITION') {
      const options = fieldOptions(field, service.name, kind)
      if (options.length > 0) defaults[field.id] = options[0]
    }
  }
  setTitle(naturalRequestTitle(service.name))
  setAnswers(defaults)
}
const interiorSpaceOptions = ['아파트·공동주택', '단독·다가구주택', '상가·매장', '사무실', '공장·창고', '공용공간', '기타', '잘 모름']
const interiorSiteOptions = ['공실·입주 전', '거주 중', '영업 중', '공사·철거 중', '신축 현장', '상태가 좋음', '보수가 필요한 상태', '상태가 매우 나쁨', '잘 모름']
function GuidedRequestField({ field, value, service, interior, onChange }: { field: RequestField; value: DynamicValue | undefined; service: PublicServiceDetail | null; interior?: boolean; onChange: (value: DynamicValue) => void }) {
  const normalizedLabel = normalizedFieldLabel(field)
  if (interior && normalizedLabel === '공간유형') return <GuidedSelectField field={field} value={value} options={interiorSpaceOptions} onChange={onChange} help="서비스를 받을 공간과 가장 가까운 유형을 선택해 주세요." />
  if (interior && normalizedLabel === '현장상태') return <GuidedSelectField field={field} value={value} options={interiorSiteOptions} onChange={onChange} help="현재 이용 상태와 현장 상태에 가장 가까운 항목을 선택해 주세요." />
  if (interior && normalizedLabel === '공사면적') return <InteriorAreaField field={field} value={value} onChange={onChange} />
  const kind = guidedFieldKind(field)
  if (kind === 'OTHER') return <DynamicFieldInput field={field} value={value} onChange={onChange}/>
  if (kind === 'CONTENT') return <div className="formField dynamicField fullWidth"><label>{field.label}{field.required && <span className="requiredMark"> *</span>}</label><textarea rows={5} required={field.required} value={String(value ?? '')} onChange={event => onChange(event.target.value)}/><small>하위 서비스 기준 추천 문구입니다. 현장 상황에 맞게 수정해 주세요.</small></div>
  if (kind === 'BUDGET') {
    const amount = standardAmount(service)
    const consultation = value !== undefined && value !== null && value !== '' && Number(value) === 0
    const restoreAmount = () => onChange(amount ?? '')
    return <div className="formField dynamicField budgetField"><div className="budgetFieldHeading"><label>{field.label}{field.required && <span className="requiredMark"> *</span>}</label><label className="budgetConsultation"><input type="checkbox" checked={consultation} onChange={event => event.target.checked ? onChange(0) : restoreAmount()} />견적상담 후 결정</label></div><input type="number" min="0" required={field.required && !consultation} disabled={consultation} value={consultation || value === undefined ? '' : String(value)} onChange={event => onChange(event.target.value)}/>{amount !== null ? <><div className="budgetPresets">{[100, 80, 50].map(rate => <button type="button" key={rate} disabled={consultation} className={Number(value) === Math.round(amount * rate / 100) ? 'selected' : ''} onClick={() => onChange(Math.round(amount * rate / 100))}>{rate === 100 ? '표준단가' : `${rate}%`} · {Math.round(amount * rate / 100).toLocaleString()}원</button>)}</div><small>{consultation ? '공급자의 견적을 확인한 뒤 비용을 결정합니다.' : '표준단가를 기본값으로 넣었습니다. 비율을 선택하거나 금액을 직접 수정할 수 있습니다.'}</small></> : <small>{consultation ? '공급자의 견적을 확인한 뒤 비용을 결정합니다.' : '등록된 표준단가가 없어 금액을 직접 입력해 주세요.'}</small>}</div>
  }
  const options = fieldOptions(field, service?.name ?? '', kind)
  return <div className="formField dynamicField"><label>{field.label}{field.required && <span className="requiredMark"> *</span>}</label><select required={field.required} value={String(value ?? '')} onChange={event => onChange(event.target.value)}><option value="">선택하세요</option>{options.map(option => <option key={option} value={option}>{kind === 'QUANTITY' && ['NUMBER', 'MONEY'].includes(field.inputType) ? `${option}개` : option}</option>)}</select><small>하위 서비스에 맞는 항목을 선택해 주세요.</small></div>
}
function GuidedSelectField({ field, value, options, help, onChange }: { field: RequestField; value: DynamicValue | undefined; options: string[]; help: string; onChange: (value: DynamicValue) => void }) {
  return <div className="formField dynamicField"><label>{field.label}{field.required && <span className="requiredMark"> *</span>}</label><select required={field.required} value={String(value ?? '')} onChange={event => onChange(event.target.value)}><option value="">선택하세요</option>{options.map(option => <option key={option} value={option}>{option}</option>)}</select><small>{help}</small></div>
}
function InteriorAreaField({ field, value, onChange }: { field: RequestField; value: DynamicValue | undefined; onChange: (value: DynamicValue) => void }) {
  const [width, setWidth] = useState('')
  const [height, setHeight] = useState('')
  const unknown = value !== undefined && value !== null && value !== '' && Number(value) === 0
  const updateArea = (nextWidth: string, nextHeight: string) => {
    const widthValue = Number(nextWidth)
    const heightValue = Number(nextHeight)
    onChange(widthValue > 0 && heightValue > 0 ? Math.round(widthValue * heightValue * 100) / 100 : '')
  }
  return <div className="formField dynamicField interiorAreaField"><label>{field.label}{field.required && <span className="requiredMark"> *</span>}</label><div className="interiorAreaInputs"><label><span>가로</span><input type="number" min="0.1" step="0.1" disabled={unknown} value={width} onChange={event => { setWidth(event.target.value); updateArea(event.target.value, height) }} /><small>미터(m)</small></label><span aria-hidden="true">×</span><label><span>세로</span><input type="number" min="0.1" step="0.1" disabled={unknown} value={height} onChange={event => { setHeight(event.target.value); updateArea(width, event.target.value) }} /><small>미터(m)</small></label></div><label className="interiorAreaUnknown"><input type="checkbox" checked={unknown} onChange={event => { if (event.target.checked) { setWidth(''); setHeight(''); onChange(0) } else onChange('') }} /> 면적을 잘 모름</label><small>{unknown ? '면적을 잘 모름으로 저장합니다.' : value ? `계산된 공사 면적: ${Number(value).toLocaleString()}㎡` : '가로와 세로 길이를 미터 단위로 입력하면 면적을 자동 계산합니다.'}</small></div>
}
function FilePreview({ file, onRemove }: { file: RequestFile; onRemove?: () => void }) {
  return <AuthenticatedFilePreview file={file} statusText={`악성코드 ${file.malwareScanStatus} · 개인정보 ${file.privacyInspectionStatus} · 공급자 공개 ${file.providerVisibilityStatus}`} onRemove={onRemove} />
}
function ReviewSection({ title, onEdit, children }: PropsWithChildren<{ title: string; onEdit: () => void }>) { return <section className="reviewSection"><header><h3>{title}</h3><button type="button" onClick={onEdit}>수정</button></header>{children}</section> }
function isEmptyValue(value: DynamicValue | undefined) { return value === undefined || value === '' || Array.isArray(value) && value.length === 0 || typeof value === 'object' && !Array.isArray(value) && 'value' in value && !String(value.value ?? '').trim() }
function isFutureDateTime(value: DynamicValue | undefined) {
  if (isEmptyValue(value)) return true
  const parsed = new Date(String(value))
  return !Number.isNaN(parsed.getTime()) && parsed.getTime() > Date.now()
}
function toLocalDateTimeValue(value: unknown) {
  const parsed = new Date(String(value))
  if (Number.isNaN(parsed.getTime())) return String(value ?? '')
  const local = new Date(parsed.getTime() - parsed.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 16)
}
function requestErrorMessage(reason: unknown, fallback: string) {
  if (reason instanceof requestApi.RequestApiError) {
    const details = Object.values(reason.fieldErrors ?? {}).flat().filter(Boolean)
    return details.length > 0 ? `${reason.message} ${details.join(' ')}` : reason.message
  }
  return reason instanceof Error ? reason.message : fallback
}
function formatDate(value: string) { return new Intl.DateTimeFormat('ko-KR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) }
function formatAnswer(value: unknown) { if (value === null || value === undefined || value === '') return '입력 없음'; if (Array.isArray(value)) return value.join(', '); if (typeof value === 'object' && 'value' in value && typeof value.value === 'string') return value.value || '입력 없음'; if (typeof value === 'object') return JSON.stringify(value); if (typeof value === 'boolean') return value ? '예' : '아니오'; return String(value) }
function formatFieldAnswer(field: RequestField, value: unknown) { if (guidedFieldKind(field) === 'BUDGET' && Number(value) === 0) return '견적상담 후 결정'; return normalizedFieldLabel(field) === '공사면적' ? Number(value) === 0 ? '잘 모름' : `${formatAnswer(value)}㎡` : formatAnswer(value) }
