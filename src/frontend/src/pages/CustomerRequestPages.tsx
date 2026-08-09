import { useEffect, useMemo, useRef, useState } from 'react'
import { navigate } from '../auth/routing'
import { AuthenticatedLayout } from '../components/AuthenticatedLayout'
import * as requestApi from '../requests/api'
import { DynamicFieldInput } from '../requests/DynamicFieldInput'
import type { DynamicValue } from '../requests/DynamicFieldInput'
import type { AdministrativeArea, Category, RequestField, ServiceRequestDetail, ServiceRequestListItem } from '../requests/types'

export function NewCustomerRequestPage() {
  const [majors, setMajors] = useState<Category[]>([]), [middles, setMiddles] = useState<Category[]>([]), [services, setServices] = useState<Category[]>([])
  const [areas, setAreas] = useState<AdministrativeArea[]>([]), [fields, setFields] = useState<RequestField[]>([])
  const [majorId, setMajorId] = useState(''), [middleId, setMiddleId] = useState(''), [serviceId, setServiceId] = useState(''), [areaId, setAreaId] = useState('')
  const [title, setTitle] = useState(''), [description, setDescription] = useState(''), [detailAddress, setDetailAddress] = useState('')
  const [isUrgent, setIsUrgent] = useState(false), [answers, setAnswers] = useState<Record<string, DynamicValue>>({})
  const [error, setError] = useState(''), [loading, setLoading] = useState(true), [submitting, setSubmitting] = useState(false)
  const idempotencyKey = useRef(crypto.randomUUID())

  useEffect(() => { Promise.all([requestApi.getMajorCategories(), requestApi.getAdministrativeAreas()]).then(([categories, areaItems]) => { setMajors(categories); setAreas(areaItems) }).catch((reason: Error) => setError(reason.message)).finally(() => setLoading(false)) }, [])

  const chooseMajor = async (id: string) => { setMajorId(id); setMiddleId(''); setServiceId(''); setMiddles([]); setServices([]); setFields([]); setAnswers({}); if (id) setMiddles(await requestApi.getMiddleCategories(id)) }
  const chooseMiddle = async (id: string) => { setMiddleId(id); setServiceId(''); setServices([]); setFields([]); setAnswers({}); if (id) setServices(await requestApi.getServiceCategories(id)) }
  const chooseService = async (id: string) => { setServiceId(id); setFields([]); setAnswers({}); if (id) setFields(await requestApi.getRequestFields(id)) }
  const canSubmit = useMemo(() => Boolean(serviceId && areaId && title.trim() && !submitting), [serviceId, areaId, title, submitting])

  const submit = async (event: React.FormEvent) => {
    event.preventDefault(); if (!canSubmit) return; setSubmitting(true); setError('')
    try {
      const dynamicAnswers = fields.filter((field) => answers[field.id] !== undefined && answers[field.id] !== '').map((field) => {
        let value: DynamicValue = answers[field.id]
        if (field.inputType === 'NUMBER' || field.inputType === 'MONEY') value = Number(value)
        if (field.inputType === 'DATETIME') value = new Date(String(value)).toISOString()
        return { fieldId: field.id, value }
      })
      const created = await requestApi.createServiceRequest({ categoryId: serviceId, administrativeAreaId: areaId, title, description: description || null, detailAddress: detailAddress || null, isUrgent, idempotencyKey: idempotencyKey.current, answers: dynamicAnswers })
      navigate(`/customer/requests/${created.id}`, true)
    } catch (reason) { setError(reason instanceof Error ? reason.message : '요청 등록에 실패했습니다.') } finally { setSubmitting(false) }
  }

  return <AuthenticatedLayout><PageHeader eyebrow="NEW REQUEST" title="서비스 요청하기" description="카테고리를 선택하면 Excel 기준 질문이 자동으로 표시됩니다." />{error && <div className="errorBanner" role="alert">{error}</div>}{loading ? <p className="emptyState">기준데이터를 불러오고 있습니다…</p> : <form className="requestForm" onSubmit={submit}>
    <section className="formSection"><h2><span>1</span> 서비스 선택</h2><div className="formGrid threeColumns"><SelectField label="대분류" value={majorId} items={majors} onChange={chooseMajor} /><SelectField label="중분류" value={middleId} items={middles} onChange={chooseMiddle} disabled={!majorId} /><SelectField label="하위 서비스" value={serviceId} items={services} onChange={chooseService} disabled={!middleId} /></div></section>
    {serviceId && <section className="formSection"><h2><span>2</span> 카테고리별 질문</h2><div className="formGrid">{fields.map((field) => <DynamicFieldInput key={field.id} field={field} value={answers[field.id]} onChange={(value) => setAnswers((current) => ({ ...current, [field.id]: value }))} />)}</div></section>}
    <section className="formSection"><h2><span>3</span> 지역과 요청 정보</h2><div className="formGrid"><div className="formField"><label htmlFor="area">시·군·구 *</label><select id="area" required value={areaId} onChange={(event) => setAreaId(event.target.value)}><option value="">선택하세요</option>{areas.map((area) => <option key={area.id} value={area.id}>{area.name}</option>)}</select></div><div className="formField"><label htmlFor="detailAddress">상세주소</label><input id="detailAddress" value={detailAddress} onChange={(event) => setDetailAddress(event.target.value)} /></div><div className="formField fullWidth"><label htmlFor="title">요청 제목 *</label><input id="title" required maxLength={200} value={title} onChange={(event) => setTitle(event.target.value)} /></div><div className="formField fullWidth"><label htmlFor="description">추가 설명</label><textarea id="description" rows={5} value={description} onChange={(event) => setDescription(event.target.value)} /></div><label className="booleanField fullWidth"><input type="checkbox" checked={isUrgent} onChange={(event) => setIsUrgent(event.target.checked)} /> 긴급 요청</label></div></section>
    <div className="formActions"><button type="button" className="secondaryButton" onClick={() => navigate('/customer')}>취소</button><button type="submit" className="primaryButton" disabled={!canSubmit}>{submitting ? '저장 중…' : '요청 저장'}</button></div>
  </form>}</AuthenticatedLayout>
}

export function CustomerRequestListPage() {
  const [items, setItems] = useState<ServiceRequestListItem[]>([]), [error, setError] = useState(''), [loading, setLoading] = useState(true)
  useEffect(() => { requestApi.getMyRequests().then(setItems).catch((reason: Error) => setError(reason.message)).finally(() => setLoading(false)) }, [])
  return <AuthenticatedLayout><PageHeader eyebrow="MY REQUESTS" title="내 요청" description="내가 등록한 서비스 요청과 현재 상태입니다." />{error && <div className="errorBanner">{error}</div>}{loading ? <p className="emptyState">불러오는 중…</p> : items.length === 0 ? <div className="emptyState"><p>등록한 요청이 없습니다.</p><button className="primaryButton inlineButton" onClick={() => navigate('/customer/requests/new')}>첫 요청 등록</button></div> : <section className="requestList">{items.map((item) => <button key={item.id} className="requestListItem" onClick={() => navigate(`/customer/requests/${item.id}`)}><div><span className="statusBadge">{item.status}</span><h2>{item.title}</h2><p>{item.categoryPath}</p></div><div className="requestMeta"><span>{formatDate(item.createdAt)}</span>{item.desiredAt && <span>희망 {formatDate(item.desiredAt)}</span>}</div></button>)}</section>}</AuthenticatedLayout>
}

export function CustomerRequestDetailPage({ requestId }: { requestId: string }) {
  const [item, setItem] = useState<ServiceRequestDetail | null>(null), [error, setError] = useState('')
  useEffect(() => { requestApi.getMyRequest(requestId).then(setItem).catch((reason: Error) => setError(reason.message)) }, [requestId])
  return <AuthenticatedLayout>{error ? <div className="errorBanner">{error}</div> : !item ? <p className="emptyState">요청을 불러오고 있습니다…</p> : <><PageHeader eyebrow={item.status} title={item.title} description={item.categoryPath} /><section className="detailCard"><dl><dt>등록일</dt><dd>{formatDate(item.createdAt)}</dd><dt>지역</dt><dd>{item.administrativeAreaName}</dd><dt>상세주소</dt><dd>{item.detailAddress || '입력 없음'}</dd><dt>희망일시</dt><dd>{item.desiredAt ? formatDate(item.desiredAt) : '입력 없음'}</dd><dt>추가 설명</dt><dd>{item.description || '입력 없음'}</dd></dl></section><section className="detailCard"><h2>카테고리별 답변</h2><dl>{item.answers.map((answer) => <div key={answer.fieldId} className="answerRow"><dt>{answer.label}</dt><dd>{formatAnswer(answer.value)}</dd></div>)}</dl></section><div className="formActions"><button className="secondaryButton" onClick={() => navigate('/customer/requests')}>목록으로</button></div></>}</AuthenticatedLayout>
}

function PageHeader({ eyebrow, title, description }: { eyebrow: string; title: string; description: string }) { return <section className="heroCard compactHero"><p className="eyebrow">{eyebrow}</p><h1>{title}</h1><p>{description}</p></section> }
function SelectField({ label, value, items, onChange, disabled = false }: { label: string; value: string; items: Category[]; onChange: (id: string) => void; disabled?: boolean }) { return <div className="formField"><label>{label} *</label><select required disabled={disabled} value={value} onChange={(event) => void onChange(event.target.value)}><option value="">선택하세요</option>{items.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></div> }
function formatDate(value: string) { return new Intl.DateTimeFormat('ko-KR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) }
function formatAnswer(value: unknown) { if (value === null || value === undefined || value === '') return '입력 없음'; if (Array.isArray(value)) return value.map((item) => typeof item === 'object' && item && 'name' in item ? String(item.name) : String(item)).join(', '); if (typeof value === 'object') return JSON.stringify(value); if (typeof value === 'boolean') return value ? '예' : '아니오'; return String(value) }
