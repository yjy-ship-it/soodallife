import { useEffect, useState, type PropsWithChildren } from 'react'
import { navigate } from '../auth/routing'
import { ProviderAppLayout } from './ProviderAppLayout'
import { providerInteriorApi as api } from './interiorApi'
import type { InteriorDashboard, InteriorDetail, InteriorProject, InteriorStage } from './interiorTypes'
import './providerInterior.css'

const date = (value?: string | null) => value ? new Date(value).toLocaleString('ko-KR') : '미정'
const money = (value: number) => `${new Intl.NumberFormat('ko-KR').format(value)}원`

function Layout({ children }: PropsWithChildren) {
  const path = window.location.pathname
  const links = [['/provider/interior', '대시보드'], ['/provider/interior/projects', '내 프로젝트'], ['/provider/after-services', '하자·A/S'], ['/provider/disputes', '분쟁']]
  return <ProviderAppLayout><section className="interiorHero"><p>SOODAL INTERIOR</p><h1>수달 인테리어</h1><span>배정된 역할 범위에서 실측·설계·시공·검사 업무를 처리합니다.</span></section><nav className="interiorTabs">{links.map(([to, label]) => <button className={path === to ? 'active' : ''} key={to} onClick={() => navigate(to)}>{label}</button>)}</nav>{children}</ProviderAppLayout>
}
function Empty({ children }: PropsWithChildren) { return <p className="interiorEmpty">{children}</p> }

export function ProviderInteriorHomePage() {
  const [value, setValue] = useState<InteriorDashboard | null>(null)
  useEffect(() => { api.home().then(setValue).catch(() => setValue(null)) }, [])
  const cards: Array<[keyof InteriorDashboard, string, string]> = [['siteVisitCount', '실측 일정', 'SITE_SURVEY'], ['siteVisitResultPendingCount', '실측 결과 대기', 'SITE_SURVEY'], ['designCount', '설계 업무', 'DESIGN'], ['contractCount', '계약 진행', 'PRIMARY_CONTRACTOR'], ['activeProjectCount', '진행 프로젝트', ''], ['todayStageCount', '오늘 공정', 'TRADE_CONTRACTOR'], ['inspectionCount', '검사 업무', 'INSPECTION'], ['completionPendingCount', '완료보고 대기', 'PRIMARY_CONTRACTOR'], ['afterServiceCount', '하자·A/S', 'AFTER_SERVICE'], ['disputeCount', '분쟁', '']]
  return <Layout><section className="interiorGrid">{cards.map(([key, label, role]) => <button key={key} onClick={() => navigate(`/provider/interior/projects${role ? `?role=${role}` : ''}`)}><span>{label}</span><strong>{value?.[key] ?? 0}건</strong></button>)}</section><section className="interiorPanel"><h2>권한 안내</h2><p>프로젝트 참여만으로 전체 정보가 열리지 않습니다. 현재 역할, 배정 공정, 유효기간과 프로젝트 상태를 함께 확인합니다.</p></section></Layout>
}

export function ProviderInteriorProjectsPage({ id }: { id?: string }) {
  const role = new URLSearchParams(location.search).get('role') ?? ''
  const [items, setItems] = useState<InteriorProject[]>([]), [detail, setDetail] = useState<InteriorDetail | null>(null), [error, setError] = useState('')
  useEffect(() => { if (id) api.detail(id).then(setDetail).catch((e: Error) => setError(e.message)); else api.projects(role).then(setItems).catch((e: Error) => setError(e.message)) }, [id, role])
  if (id) return <Layout>{error && <p className="interiorError">{error}</p>}{detail ? <ProjectDetail value={detail} reload={() => api.detail(id).then(setDetail)} /> : !error && <Empty>프로젝트를 불러오는 중입니다.</Empty>}</Layout>
  return <Layout><section className="interiorPanel"><h2>내 인테리어 프로젝트</h2>{items.map(item => <button className="interiorProjectCard" key={item.id} onClick={() => navigate(`/provider/interior/projects/${item.id}`)}><span><small>{item.roles.join(' · ')}</small><b>{item.serviceName}</b><em>{item.areaName} · {item.statusCode}</em></span><span><strong>{item.nextAction}</strong><small>미처리 {item.pendingActionCount}건</small></span></button>)}{!items.length && <Empty>배정된 프로젝트가 없습니다.</Empty>}</section></Layout>
}

function ProjectDetail({ value, reload }: { value: InteriorDetail; reload: () => Promise<void> }) {
  const [error, setError] = useState('')
  const run = async (action: () => Promise<unknown>) => { try { setError(''); await action(); await reload() } catch (reason) { setError((reason as Error).message) } }
  const upload = async (file: File) => (await api.upload(value.project.id, file)).fileId
  return <>{error && <p className="interiorError">{error}</p>}<section className="interiorSummary"><div><small>{value.project.roles.join(' · ')}</small><h2>{value.project.serviceName}</h2><p>{value.project.projectNumber} · {value.project.areaName}</p></div><strong>{value.project.statusCode}</strong></section><div className="interiorColumns">
    <div><Contact value={value} />{value.siteVisits.map(visit => <section className="interiorPanel" key={visit.id}><h3>실측 · {visit.statusCode}</h3><p>{date(visit.scheduledStartAt)}</p><p>{visit.measurementSummary ?? '측정 결과 미등록'}</p><Files files={visit.files} /><div className="interiorActions">{!visit.visitedAt && <button onClick={() => void run(() => api.startVisit(visit))}>실측 시작</button>}{!visit.completedAt && <FileAction label="결과·증빙 제출" onSubmit={async file => { const fileIds = file ? [await upload(file)] : []; await run(() => api.completeVisit(visit, { measurementSummary: '현장 실측 결과 등록', constraint: '', riskNote: '', fileIds })) }} />}</div></section>)}
      {value.quotes.map(quote => <section className="interiorPanel" key={quote.id}><h3>내 견적 v{quote.revisionNo}</h3><p>{quote.summary}</p><strong>{money(quote.totalAmount)}</strong>{quote.items.map(item => <small key={item}>{item}</small>)}</section>)}
      {value.designs.map(design => <section className="interiorPanel" key={design.id}><h3>설계 v{design.versionNo} · {design.statusCode}</h3><p>{design.title}</p><p>{design.description}</p><Files files={design.files} /></section>)}
      {value.project.roles.includes('DESIGN') && <FileAction label="새 설계 버전 제출" onSubmit={async file => { const fileIds = file ? [await upload(file)] : []; await run(() => api.design(value.project.id, { title: `설계 버전 ${value.designs.length + 1}`, description: '공급자 설계 제출', fileIds })) }} />}
    </div>
    <div><Contract value={value} run={run} />{value.stages.map(stage => <StageCard key={stage.id} stage={stage} run={run} upload={upload} />)}
      {value.inspections.map(item => <section className="interiorPanel" key={item.id}><h3>검사 · {item.statusCode}</h3><p>{item.result}</p><p>{item.correction}</p><span>{item.customerAcknowledged ? '고객 결과 확인 완료' : '고객 결과 확인 대기'}</span><Files files={item.files} /></section>)}
      {value.project.roles.includes('INSPECTION') && value.stages[0] && <FileAction label="검사 결과 제출" onSubmit={async file => { const fileIds = file ? [await upload(file)] : []; await run(() => api.inspect(value.stages[0], 'PASSED', '검사 결과 등록', '', fileIds)) }} />}
      {value.changes.map(change => <section className="interiorPanel" key={change.id}><h3>변경 #{change.changeNo} · {change.statusCode}</h3><p>{change.reason}</p><p>{change.scopeChange} · {money(change.amountDelta)}</p><Files files={change.files} /></section>)}
      {value.contract && value.project.roles.includes('PRIMARY_CONTRACTOR') && <FileAction label="추가공사·계약변경 요청" onSubmit={async file => { const fileIds = file ? [await upload(file)] : []; await run(() => api.change(value.contract!.id, { reason: '현장 변경 요청', scopeChange: '변경 범위 확인 필요', amountDelta: 0, scheduleImpactDays: null, fileIds })) }} />}
    </div><Side value={value} run={run} /></div></>
}

function Contact({ value }: { value: InteriorDetail }) { return <section className="interiorPanel"><h3>고객 업무정보</h3>{value.contactAvailable ? <><p>{value.customerPhone ?? '전화번호 비공개'}</p><p>{value.detailAddress ?? '상세주소 비공개'}</p></> : <p className="interiorPrivacy">{value.contactPolicy}</p>}</section> }
function Contract({ value, run }: { value: InteriorDetail; run: (a: () => Promise<unknown>) => Promise<void> }) {
  if (!value.contract) return null
  const contract = value.contract
  return <section className="interiorPanel"><h3>계약 v{contract.version}</h3><p>{money(contract.amount)} · {contract.statusCode}</p><p>{contract.plannedStartDate} ~ {contract.plannedCompletionDate}</p>{!contract.providerAgreedAt && <button onClick={() => void run(() => api.agree(value.project.id, contract))}>공급자 계약 동의</button>}<h4>직접 지급 계획</h4>{contract.paymentPlans.map(plan => <div key={plan.id}><p>{plan.sequenceNo}. {plan.name} · {money(plan.amount)} · 확인 {plan.confirmationCount}건</p><button onClick={() => void run(() => api.payment(plan))}>직접 지급 수령 확인</button></div>)}<small>실제 PG·에스크로·송금 기능이 아닙니다.</small></section>
}
function StageCard({ stage, run, upload }: { stage: InteriorStage; run: (a: () => Promise<unknown>) => Promise<void>; upload: (f: File) => Promise<string> }) { return <section className="interiorPanel"><h3>{stage.sequenceNo}. {stage.name}</h3><p>{stage.plannedStartDate} ~ {stage.plannedEndDate}</p><progress value={stage.progressPercent} max="100" /><span>{stage.progressPercent}% · {stage.statusCode}</span><Files files={stage.files} /><FileAction label="공정 업데이트" onSubmit={async file => { const ids = file ? [await upload(file)] : []; await run(() => api.stage(stage, Math.min(100, stage.progressPercent + 10), '현장 공정 업데이트', ids)) }} /></section> }
function Side({ value, run }: { value: InteriorDetail; run: (a: () => Promise<unknown>) => Promise<void> }) { return <aside><section className="interiorPanel"><h3>완료 상태</h3><p>고객 확인: {value.customerCompletionAcknowledged ? '완료' : '대기'}</p><p>관리자 최종완료: {value.adminCompleted ? '완료' : '대기'}</p>{value.project.roles.includes('PRIMARY_CONTRACTOR') && !value.adminCompleted && <button onClick={() => void run(() => api.completion(value.project, '공급자 공사 완료보고'))}>완료보고</button>}</section><section className="interiorPanel"><h3>하자·A/S·분쟁</h3>{value.afterServiceIds.map(id => <button key={id} onClick={() => navigate(`/provider/after-services/${id}`)}>A/S 상세</button>)}{value.disputeIds.map(id => <button key={id} onClick={() => navigate(`/provider/disputes/${id}`)}>분쟁 상세</button>)}{!value.afterServiceIds.length && !value.disputeIds.length && <Empty>연결된 업무가 없습니다.</Empty>}</section><section className="interiorPanel interiorTimeline"><h3>타임라인</h3>{value.timeline.map(item => <p key={item.id}><b>{item.eventTypeCode}</b><small>{date(item.occurredAt)}</small></p>)}</section></aside> }
function Files({ files }: { files: Array<{ id: string; fileName: string; downloadUrl: string|null; publicationMessage:string|null }> }) { return <div>{files.map(file => file.downloadUrl?<a className="interiorFile" key={file.id} href={file.downloadUrl}>{file.fileName}</a>:<span className="interiorFile" key={file.id}>{file.publicationMessage??"안전 확인 전에는 공개되지 않습니다."}</span>)}</div> }
function FileAction({ label, onSubmit }: { label: string; onSubmit: (file?: File) => Promise<void> }) { const [file, setFile] = useState<File>(); return <div className="interiorFileAction"><input aria-label={`${label} 파일`} type="file" accept=".jpg,.jpeg,.png,.pdf" onChange={event => setFile(event.target.files?.[0])} /><button onClick={() => void onSubmit(file)}>{label}</button></div> }
