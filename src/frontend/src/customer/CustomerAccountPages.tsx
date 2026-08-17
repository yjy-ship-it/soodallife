import { useEffect, useState } from 'react'
import type { FormEvent, PropsWithChildren } from 'react'
import { useAuthentication } from '../auth/AuthenticationContext'
import { getSafeReturnUrl, navigate } from '../auth/routing'
import { BrandLogo } from '../components/BrandLogo'
import { ServiceFooter } from '../components/ServiceFooter'
import { findPublicPolicy, publicPolicyDocuments } from '../config/publicPolicyContent'
import { customerAccountApi, CustomerAccountApiError } from './accountApi'
import type { AdministrativeArea, Consent, ConsentHistory, CustomerAddress, CustomerNotification, CustomerProfile, LegalDocument, MySoodalSummary, NotificationPreference, ProviderBlock, WithdrawalReadiness } from './accountTypes'
import { CustomerAppLayout } from './CustomerAppLayout'
import { safeNotificationTarget } from './notificationRouting'
import { notifyDefaultAddressChanged } from './defaultAddress'
import './customerAccount.css'

const message = (error: unknown) => error instanceof CustomerAccountApiError ? error.message : '요청을 처리하지 못했습니다.'
const date = (value: string | null) => value ? new Intl.DateTimeFormat('ko-KR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : '없음'
const formatMobilePhone = (value: string) => {
  const digits = value.replace(/\D/g, '').slice(0, 11)
  if (digits.length <= 3) return digits
  if (digits.length <= 7) return `${digits.slice(0, 3)}-${digits.slice(3)}`
  return `${digits.slice(0, 3)}-${digits.slice(3, 7)}-${digits.slice(7)}`
}
const marketingDraft = `수달 라이프 선택적 마케팅 수신 동의 (법률검토용 초안)\n\n동의하지 않아도 회원가입과 기본 서비스 이용에는 제한이 없으며, 동의 후에도 언제든 철회할 수 있습니다.\n\n1. 수집·이용 목적\n신규 서비스, 혜택, 이벤트, 프로모션, 쿠폰, 이용자 맞춤형 서비스 추천 및 설문 안내를 위해 개인정보를 이용합니다.\n\n2. 이용 항목\n이름 또는 표시명, 이메일 주소, 휴대전화번호, 회원 역할, 선택 서비스·관심지역, 서비스 이용·거래 이력에서 도출한 관심정보를 이용할 수 있습니다. Push 기능 도입 시에는 Push 토큰, 채널별 수신설정과 수신·거부 이력을 처리할 수 있습니다.\n\n3. 전송 채널\n이메일, SMS·문자, 알림톡, 앱·웹 Push 및 서비스 내 알림을 이용할 수 있습니다. 외부 채널이 실제 연동되기 전에는 해당 채널로 전송하지 않습니다.\n\n4. 보유·이용기간\n마케팅 동의 철회 또는 회원탈퇴 시까지 이용합니다. 다만 동의·철회 및 광고 전송 이력은 법령 준수와 분쟁 대응에 필요한 기간 동안 별도로 보관할 수 있습니다.\n\n5. 동의 거부와 철회\n동의를 거부해도 회원가입, 요청, 견적, 거래 및 고객지원 등 기본 서비스는 이용할 수 있습니다. 알림 설정, 수신거부 방법 또는 고객센터를 통해 언제든 철회할 수 있습니다.\n\n6. 광고성 정보 전송 원칙\n광고성 정보에는 광고 표시, 발신자 정보와 무료 수신거부 방법을 포함합니다. 오후 9시부터 다음 날 오전 8시 사이에는 별도의 야간 광고 수신동의 없이 광고성 정보를 전송하지 않습니다.\n\n시행 예정일: 2026년 9월 1일\n문서 상태: 법률검토용 초안 - 게시 전 확정·검토 필요`
function signupLegalDocument(item: LegalDocument): LegalDocument {
  const policy = findPublicPolicy(item.code)
  if (policy) {
    const content = [policy.introduction, ...policy.sections.map(section => `${section.heading}\n${[...(section.paragraphs ?? []), ...(section.items ?? []).map(value => `• ${value}`)].join('\n')}`), policy.notice].join('\n\n')
    return { ...item, title: `${policy.title} (${policy.status})`, content }
  }
  if (item.code === 'MARKETING_CONSENT') return { ...item, title: '수달 라이프 선택적 마케팅 수신 동의 (법률검토용 초안)', content: marketingDraft }
  return item
}

export function CustomerSignupPage() {
  const { login } = useAuthentication()
  const [step, setStep] = useState(1)
  const [form, setForm] = useState({ loginId: '', name: '', email: null as string | null, phone: '', password: '', passwordConfirmation: '', phoneVerificationToken: '' })
  const [verificationStatus, setVerificationStatus] = useState('확인 중')
  const [documents, setDocuments] = useState<LegalDocument[]>([])
  const [agreed, setAgreed] = useState<Record<string, boolean>>({})
  const [loginChecked, setLoginChecked] = useState(false)
  const [phoneChecked, setPhoneChecked] = useState(false)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  useEffect(() => { customerAccountApi.legalDocuments().then(items => setDocuments(items.map(signupLegalDocument))).catch(error => setError(message(error))) }, [])
  useEffect(() => { customerAccountApi.identityVerificationStatus().then(value => setVerificationStatus(value.statusCode)).catch(() => setVerificationStatus('NOT_INTEGRATED')) }, [])
  const update = (name: keyof typeof form, value: string) => { setForm(current => ({ ...current, [name]: value })); if (name === 'loginId') setLoginChecked(false); if (name === 'phone') { setPhoneChecked(false); setForm(current => ({ ...current, phone: value, phoneVerificationToken: '' })) } }
  const checkLogin = async () => { try { const value = await customerAccountApi.loginAvailability(form.loginId); setLoginChecked(value.available); setError(value.available ? '' : '이미 사용 중인 아이디입니다.') } catch (error) { setError(message(error)) } }
  const checkPhone = async () => {
    if (!/^010-\d{4}-\d{4}$/.test(form.phone)) return setError('휴대전화 번호를 010-1234-5678 형식으로 입력해 주세요.')
    try {
      const value = await customerAccountApi.phoneAvailability(form.phone)
      if (!value.available) { setPhoneChecked(false); return setError('이미 등록된 휴대전화 번호입니다.') }
      if (verificationStatus !== 'NOT_INTEGRATED') return setError('휴대전화 본인인증 연동이 준비 중입니다. 실제 인증수단이 연결된 후 이용해 주세요.')
      setPhoneChecked(true); setForm(current => ({ ...current, phoneVerificationToken: 'NOT_INTEGRATED' })); setError('')
    } catch (error) { setError(message(error)) }
  }
  const next = () => {
    setError('')
    if (step === 1 && (!phoneChecked || !form.phoneVerificationToken)) return setError('휴대전화 번호 확인을 완료해 주세요.')
    if (step === 2 && !loginChecked) return setError('아이디 중복확인을 완료해 주세요.')
    if (step === 2 && !form.name) return setError('이름을 입력해 주세요.')
    if (step === 2 && !/^(?=.*[A-Za-z])(?=.*\d)(?=.*[^A-Za-z0-9\s])\S{6,}$/.test(form.password)) return setError('비밀번호가 규칙에 맞지 않습니다.')
    if (step === 2 && form.password !== form.passwordConfirmation) return setError('비밀번호를 다시 확인하세요.')
    if (step === 3 && documents.some(item => item.requirementCode === 'REQUIRED' && !agreed[item.versionId])) return setError('필수 약관에 동의해 주세요.')
    setStep(current => Math.min(4, current + 1))
  }
  const allAgreed = documents.length > 0 && documents.every(item => agreed[item.versionId])
  const register = async () => {
    setBusy(true); setError('')
    try {
      await customerAccountApi.register({ ...form, consents: documents.map(item => ({ legalDocumentVersionId: item.versionId, agreed: Boolean(agreed[item.versionId]) })) })
      await login(form.loginId, form.password)
      navigate(getSafeReturnUrl() ?? '/customer', true)
    } catch (error) { setError(message(error)) } finally { setBusy(false) }
  }
  const finish = () => navigate(getSafeReturnUrl() ?? '/customer', true)
  return <div className="signupShell"><header><BrandLogo /><button onClick={() => navigate('/login')}>로그인</button></header><main className="signupMain"><ol className="signupSteps" aria-label="회원가입 단계">{['휴대전화 확인', '계정정보', '약관동의', '정보확인', '가입완료'].map((label, index) => <li className={step >= index + 1 ? 'isActive' : ''} key={label}><span>{index + 1}</span>{label}</li>)}</ol><section className="signupCard" aria-live="polite">
    {step === 1 && <><p className="accountEyebrow">1단계</p><h1>휴대전화 번호를 확인해 주세요</h1><label>휴대전화<input value={form.phone} onChange={event => update('phone', formatMobilePhone(event.target.value))} placeholder="010-1234-5678" inputMode="tel" autoComplete="tel" /></label><small>숫자만 입력해도 010-1234-5678 형식으로 자동 변환됩니다.</small><button className="accountSecondary" type="button" onClick={() => void checkPhone()}>휴대전화 본인인증</button>{phoneChecked && <small className="successText">가입 가능한 휴대전화 번호입니다.</small>}<div className="notIntegratedNotice"><strong>휴대전화 본인인증 · {verificationStatus}</strong><span>{verificationStatus === 'NOT_INTEGRATED' ? '외부 본인인증 연동 전에는 번호 형식과 중복 여부만 확인하며, 본인인증 완료로 기록하지 않습니다. 확인 후 회원가입을 계속할 수 있습니다.' : '실제 본인인증을 완료하면 다음 단계로 진행할 수 있습니다.'}</span></div></>}
    {step === 2 && <><p className="accountEyebrow">2단계</p><h1>사용할 계정을 만들어 주세요</h1><label>휴대전화<input value={form.phone} disabled /></label><label>로그인 아이디<div className="inlineField"><input value={form.loginId} onChange={event => update('loginId', event.target.value)} autoComplete="username" /><button type="button" onClick={() => void checkLogin()}>중복확인</button></div></label>{loginChecked && <small className="successText">사용할 수 있는 아이디입니다.</small>}<label>이름<input value={form.name} onChange={event => update('name', event.target.value)} autoComplete="name" /></label><label>비밀번호<input type="password" minLength={6} value={form.password} onChange={event => update('password', event.target.value)} autoComplete="new-password" /></label><small>6자 이상이며 영문자, 숫자, 특수문자를 각각 포함해 주세요.</small>{form.password && !/^(?=.*[A-Za-z])(?=.*\d)(?=.*[^A-Za-z0-9\s])\S{6,}$/.test(form.password) && <small className="accountError" role="alert">비밀번호가 규칙에 맞지 않습니다.</small>}<label>비밀번호 확인<input type="password" minLength={6} value={form.passwordConfirmation} onChange={event => update('passwordConfirmation', event.target.value)} autoComplete="new-password" /></label>{form.passwordConfirmation && form.password !== form.passwordConfirmation && <small className="accountError" role="alert">비밀번호를 다시 확인하세요.</small>}<small>이메일은 가입 후 마이수달의 내 정보에서 등록할 수 있습니다.</small></>}
    {step === 3 && <><p className="accountEyebrow">3단계</p><h1>약관을 확인해 주세요</h1><label className="consentAll"><input type="checkbox" checked={allAgreed} onChange={event => setAgreed(Object.fromEntries(documents.map(item => [item.versionId, event.target.checked])))} />전체 동의</label><div className="signupConsents">{documents.map(item => <label key={item.versionId}><span><input type="checkbox" checked={Boolean(agreed[item.versionId])} onChange={event => setAgreed(current => ({ ...current, [item.versionId]: event.target.checked }))} /><b>[{item.requirementCode === 'REQUIRED' ? '필수' : '선택'}]</b> {item.title}</span><details><summary>내용 보기</summary><p>{item.content}</p>{item.isPlaceholder && <strong>운영 전 승인 문서로 교체해야 하는 개발용 문서입니다.</strong>}</details></label>)}</div></>}
    {step === 4 && <><p className="accountEyebrow">4단계</p><h1>가입 정보를 확인해 주세요</h1><dl className="confirmationList"><div><dt>아이디</dt><dd>{form.loginId}</dd></div><div><dt>이름</dt><dd>{form.name}</dd></div><div><dt>휴대전화</dt><dd>{form.phone}</dd></div><div><dt>본인인증 상태</dt><dd>{verificationStatus === 'NOT_INTEGRATED' ? '외부연동 전 번호 확인' : '본인인증 완료'}</dd></div><div><dt>선택 동의</dt><dd>{documents.filter(item => item.requirementCode === 'OPTIONAL' && agreed[item.versionId]).map(item => item.title).join(', ') || '없음'}</dd></div></dl></>}
    {step === 5 && <div className="signupComplete"><span aria-hidden="true">✓</span><h1>가입이 완료되었습니다</h1><p>수달 라이프에서 필요한 생활서비스를 찾아보세요.</p><button className="accountPrimary" onClick={finish}>시작하기</button></div>}
    {error && <p className="accountError" role="alert">{error}</p>}{step < 5 && <div className="signupActions">{step > 1 && <button onClick={() => setStep(current => current - 1)}>이전</button>}{step < 4 ? <button className="accountPrimary" onClick={next}>다음</button> : <button className="accountPrimary" disabled={busy} onClick={() => void register()}>{busy ? '가입 처리 중…' : '가입하기'}</button>}</div>}
  </section></main><ServiceFooter variant="customer" /></div>
}

export function PasswordResetRequestPage() {
  const [identifier, setIdentifier] = useState(''); const [sent, setSent] = useState(false); const [error, setError] = useState('')
  const submit = async (event: FormEvent) => { event.preventDefault(); try { await customerAccountApi.requestPasswordReset(identifier); setSent(true); setError('') } catch (error) { setError(message(error)) } }
  return <div className="signupShell"><header><BrandLogo /><button onClick={() => navigate('/login')}>로그인으로</button></header><main className="signupMain"><section className="signupCard"><p className="accountEyebrow">로그인·보안</p><h1>비밀번호 재설정 요청</h1>{sent ? <div className="notIntegratedNotice"><strong>요청이 접수되었습니다</strong><span>현재 SMS/Email 발송 Adapter가 미연동되어 재설정 메시지는 발송되지 않습니다.</span></div> : <form onSubmit={event => void submit(event)}><label>아이디<input value={identifier} onChange={event => setIdentifier(event.target.value)} autoComplete="username" /></label><button className="accountPrimary" disabled={!identifier}>재설정 요청</button></form>}{error && <p className="accountError">{error}</p>}</section></main><ServiceFooter variant="customer" /></div>
}

const myMenus = [
  ['/customer', '마이수달'], ['/customer/profile', '내 정보'], ['/customer/addresses', '주소 관리'], ['/customer/security', '로그인·보안'],
  ['/customer/notifications', '알림센터'], ['/customer/notification-settings', '알림 설정'], ['/customer/consents', '약관·동의'], ['/customer/provider-blocks', '차단한 공급자'], ['/customer/security/withdrawal', '계정 탈퇴'],
] as const

export function CustomerProviderBlocksPage() {
  const [items, setItems] = useState<ProviderBlock[]>([]); const [error, setError] = useState(''); const [busy, setBusy] = useState('')
  const load = () => customerAccountApi.providerBlocks().then(setItems).catch(reason => setError(message(reason)))
  useEffect(() => { void load() }, [])
  const release = async (item: ProviderBlock) => { if (!window.confirm(`${item.providerName} 공급자 차단을 해제할까요? 이후 새 요청에서 다시 매칭될 수 있습니다.`)) return; try { setBusy(item.id); await customerAccountApi.releaseProviderBlock(item.id, item.rowVersion); await load() } catch (reason) { setError(message(reason)) } finally { setBusy('') } }
  return <MySoodalLayout title="차단한 공급자" description="차단은 새로운 매칭만 제한하며 기존 거래·채팅·리뷰·A/S·분쟁 기록은 삭제하거나 취소하지 않습니다."><div className="providerBlockList">{items.map(item => <article key={item.id}><div><span>{item.status === 'ACTIVE' ? '차단 중' : '해제됨'}</span><h2>{item.providerName}</h2><small>차단 {date(item.createdAt)}{item.releasedAt ? ` · 해제 ${date(item.releasedAt)}` : ''}</small></div>{item.status === 'ACTIVE' && <button disabled={busy === item.id} onClick={() => void release(item)}>{busy === item.id ? '처리 중…' : '차단 해제'}</button>}</article>)}{items.length === 0 && <p className="accountEmpty">차단한 공급자가 없습니다.</p>}</div>{error && <p className="accountError">{error}</p>}<section className="notIntegratedNotice"><strong>공급자→고객 차단 정책 미확정</strong><span>이번 단계는 고객이 공급자를 차단하는 기능만 제공합니다. 차단 사유와 개인 메모는 공급자에게 공개하거나 알림으로 보내지 않습니다.</span></section></MySoodalLayout>
}
export function MySoodalLayout({ title, description, children }: PropsWithChildren<{ title: string; description: string }>) {
  const path = window.location.pathname
  return <CustomerAppLayout><section className="mySoodalHeading"><p>MY SOODAL</p><h1>{title}</h1><span>{description}</span></section><div className="mySoodalLayout"><nav aria-label="마이수달 메뉴">{myMenus.map(([href, label]) => <button className={path === href ? 'isActive' : ''} onClick={() => navigate(href)} key={href}>{label}</button>)}<hr />{[['/customer/requests', '요청·견적'], ['/customer/transactions', '진행 거래'], ['/customer/care/contracts', '내 구독'], ['/customer/interior/projects', '내 인테리어'], ['/customer/service-history', '서비스 이력'], ['/customer/reviews', '내 리뷰'], ['/customer/after-services', 'A/S'], ['/customer/disputes', '분쟁'], ['/customer/reports', '신고'], ['/support', '고객센터']].map(([href, label]) => <button onClick={() => navigate(href)} key={href}>{label}</button>)}</nav><section className="mySoodalContent">{children}</section></div></CustomerAppLayout>
}

export function MySoodalPage() {
  const [summary, setSummary] = useState<MySoodalSummary | null>(null); const [error, setError] = useState('')
  useEffect(() => { customerAccountApi.mySoodalSummary().then(setSummary).catch(reason => setError(message(reason))) }, [])
  const cards = [
    ['/customer/requests', '진행 중 요청', summary?.ongoingRequestCount], ['/customer/transactions', '진행 중 거래', summary?.ongoingTransactionCount],
    ['/customer/notifications', '읽지 않은 알림', summary?.unreadNotificationCount], ['/customer/reviews', '작성한 리뷰', summary?.reviewCount],
    ['/customer/after-services', '진행 중 A/S', summary?.activeAfterServiceCount], ['/customer/disputes', '진행 중 분쟁', summary?.activeDisputeCount],
    ['/customer/reports', '처리 중 신고', summary?.activeReportCount], ['/support', '고객센터', null],
  ] as const
  return <MySoodalLayout title="마이수달" description="계정과 진행 중인 서비스를 한곳에서 확인하세요.">{error && <p className="accountError">{error}</p>}<div className="accountSummary"><p>안녕하세요</p><h2>{summary?.name ?? '고객'}님</h2><span>{summary?.loginId}</span>{summary?.defaultAddress ? <small>기본주소 · {summary.defaultAddress.addressName} · {summary.defaultAddress.roadAddress} {summary.defaultAddress.detailAddress}</small> : <button onClick={() => navigate('/customer/addresses')}>기본주소 등록</button>}</div><div className="myStatusGrid">{cards.map(([href, label, count]) => <button onClick={() => navigate(href)} key={href}><span>{label}</span><strong>{count ?? (summary ? '바로가기' : '—')}</strong></button>)}</div><section className="recentHistory"><header><h2>최근 서비스 이력</h2><button onClick={() => navigate('/customer/service-history')}>전체 보기</button></header>{summary?.recentServiceHistory.length ? summary.recentServiceHistory.map(item => <button key={item.id} onClick={() => navigate(`/customer/service-history/${item.id}`)}><span>{item.categoryName ?? '생활서비스'}</span><strong>{item.title}</strong><small>{date(item.completedAt)}</small></button>) : <p className="accountEmpty">완료된 서비스 이력이 없습니다.</p>}</section><div className="myCardGrid">{myMenus.slice(1).map(([href, label]) => <button onClick={() => navigate(href)} key={href}><strong>{label}</strong><span>내 계정 설정과 이용정보를 확인합니다.</span></button>)}</div><section className="futureMenu"><h2>정기구독 결제</h2><p>수달 케어의 내부 결제 Workflow 상태만 확인할 수 있습니다. 실제 PG 결제는 아직 연동되지 않았습니다.</p><button onClick={() => navigate('/customer/care/payments')}>결제 준비상태 확인</button></section></MySoodalLayout>
}

export function CustomerProfilePage() {
  const [profile, setProfile] = useState<CustomerProfile | null>(null); const [form, setForm] = useState({ name: '', email: '', phone: '' }); const [notice, setNotice] = useState(''); const [error, setError] = useState('')
  useEffect(() => { customerAccountApi.profile().then(value => { setProfile(value); setForm({ name: value.name, email: value.email ?? '', phone: value.phone ?? '' }) }).catch(error => setError(message(error))) }, [])
  const save = async (event: FormEvent) => { event.preventDefault(); try { const value = await customerAccountApi.updateProfile(form); setProfile(value); setNotice('내 정보가 변경되었습니다.'); setError('') } catch (error) { setError(message(error)) } }
  return <MySoodalLayout title="내 정보" description="본인 계정의 기본정보만 조회하고 수정할 수 있습니다."><form className="accountForm" onSubmit={event => void save(event)}><label>로그인 아이디<input value={profile?.loginId ?? ''} disabled /></label><label>이름<input value={form.name} onChange={event => setForm({ ...form, name: event.target.value })} /></label><label>이메일<input type="email" value={form.email} onChange={event => setForm({ ...form, email: event.target.value })} /></label><label>휴대전화<input value={form.phone} onChange={event => setForm({ ...form, phone: formatMobilePhone(event.target.value) })} placeholder="010-1234-5678" inputMode="tel" /></label><div className="accountMeta"><span>가입일 {date(profile?.createdAt ?? null)}</span><span>최근 로그인 {date(profile?.lastLoginAt ?? null)}</span><span>상태 {profile?.accountStatus}</span><span>이메일/휴대전화 인증: 외부연동 전</span></div>{notice && <p className="successText">{notice}</p>}{error && <p className="accountError">{error}</p>}<button className="accountPrimary">변경 저장</button></form></MySoodalLayout>
}

type AddressForm = { addressName: string; recipientName: string; postalCode: string; roadAddress: string; detailAddress: string; administrativeAreaId: string; latitude: number | null; longitude: number | null; isDefault: boolean; concurrencyToken: string }
const emptyAddress: AddressForm = { addressName: '', recipientName: '', postalCode: '', roadAddress: '', detailAddress: '', administrativeAreaId: '', latitude: null, longitude: null, isDefault: false, concurrencyToken: '' }
export function CustomerAddressesPage() {
  const [items, setItems] = useState<CustomerAddress[]>([]); const [editingId, setEditingId] = useState<string | null>(null); const [form, setForm] = useState(emptyAddress); const [error, setError] = useState('')
  const [sidos, setSidos] = useState<AdministrativeArea[]>([]); const [sigungu, setSigungu] = useState<AdministrativeArea[]>([]); const [sidoId, setSidoId] = useState('')
  const load = () => customerAccountApi.addresses().then(setItems).catch(error => setError(message(error)))
  useEffect(() => { void load(); customerAccountApi.sidos().then(setSidos).catch(() => undefined) }, [])
  useEffect(() => { if (!sidoId) { setSigungu([]); return }; customerAccountApi.sigungu(sidoId).then(setSigungu).catch(() => setSigungu([])) }, [sidoId])
  const edit = (item: CustomerAddress) => {
    setEditingId(item.id)
    setSidoId('')
    setSigungu([])
    setForm({ addressName: item.addressName, recipientName: item.recipientName ?? '', postalCode: item.postalCode, roadAddress: item.roadAddress, detailAddress: item.detailAddress, administrativeAreaId: item.administrativeAreaId ?? '', latitude: item.latitude, longitude: item.longitude, isDefault: item.isDefault, concurrencyToken: item.concurrencyToken })
    if (item.administrativeAreaId) {
      void customerAccountApi.sigungu().then(areas => {
        const selected = areas.find(area => area.id === item.administrativeAreaId)
        if (!selected?.parentId) return
        setSidoId(selected.parentId)
        setSigungu(areas.filter(area => area.parentId === selected.parentId))
      }).catch(() => setError('저장된 시·도와 시·군·구를 불러오지 못했습니다.'))
    }
  }
  const save = async (event: FormEvent) => { event.preventDefault(); try { const value = { ...form, administrativeAreaId: form.administrativeAreaId || null }; if (editingId) await customerAccountApi.updateAddress(editingId, value); else await customerAccountApi.createAddress(value); if (form.isDefault) notifyDefaultAddressChanged(); setEditingId(null); setSidoId(''); setSigungu([]); setForm(emptyAddress); setError(''); await load() } catch (error) { setError(message(error)) } }
  const remove = async (item: CustomerAddress) => { if (item.isDefault) { setError('기본주소는 삭제할 수 없습니다. 다른 주소를 기본주소로 설정한 후 삭제해 주세요.'); return } try { await customerAccountApi.deleteAddress(item.id, item.concurrencyToken); await load() } catch (error) { setError(message(error)) } }
  return <MySoodalLayout title="주소 관리" description="상세주소는 본인에게만 표시되며 주소록 자체를 공급자에게 공개하지 않습니다.">
    <div className="addressLayout">
      <div className="addressList">
        {items.length ? items.map(item => <article key={item.id}>
          <header><strong>{item.addressName}</strong>{item.isDefault && <span>기본주소</span>}</header>
          {item.administrativeAreaName && <small>서비스 지역 · {item.administrativeAreaName}</small>}
          <p>{item.postalCode} {item.roadAddress}<br />{item.detailAddress}</p>
          {item.recipientName && <small>현장 연락받을 분 · {item.recipientName}</small>}
          <div><button onClick={() => edit(item)}>수정</button><button disabled={item.isDefault} title={item.isDefault ? '다른 주소를 기본주소로 설정한 후 삭제할 수 있습니다.' : undefined} onClick={() => void remove(item)}>{item.isDefault ? '기본주소 삭제 불가' : '삭제'}</button></div>
        </article>) : <p className="accountEmpty">등록된 주소가 없습니다.</p>}
      </div>
      <form className="accountForm addressForm" onSubmit={event => void save(event)}>
        <h2>{editingId ? '주소 수정' : '새 주소'}</h2>
        <label>주소 별칭<input value={form.addressName} onChange={event => setForm({ ...form, addressName: event.target.value })} placeholder="우리집" /></label>
        <label>현장 연락받을 분<input value={form.recipientName} onChange={event => setForm({ ...form, recipientName: event.target.value })} placeholder="본인 또는 현장 담당자 이름" /></label>
        <small>서비스 당일 공급자와 연락할 사람을 입력합니다. 본인이 이용하면 본인 이름을 입력해 주세요.</small>
        <div className="areaFields">
          <label>시·도<select required value={sidoId} onChange={event => { setSidoId(event.target.value); setForm({ ...form, administrativeAreaId: '' }) }}><option value="">선택</option>{sidos.map(item => <option value={item.id} key={item.id}>{item.name}</option>)}</select></label>
          <label>시·군·구<select required value={form.administrativeAreaId} onChange={event => setForm({ ...form, administrativeAreaId: event.target.value })}><option value="">선택</option>{sigungu.map(item => <option value={item.id} key={item.id}>{item.name}</option>)}</select></label>
        </div>
        <label>우편번호<input value={form.postalCode} onChange={event => setForm({ ...form, postalCode: event.target.value })} /></label>
        <label>도로명 주소<input value={form.roadAddress} onChange={event => setForm({ ...form, roadAddress: event.target.value })} /></label>
        <label>상세주소<input value={form.detailAddress} onChange={event => setForm({ ...form, detailAddress: event.target.value })} /></label>
        <small>외부 주소검색은 아직 연동하지 않았으며 현재는 기존 행정구역과 수동 주소 입력을 사용합니다.</small>
        <label className="checkLabel"><input type="checkbox" checked={form.isDefault} disabled={Boolean(editingId && items.find(item => item.id === editingId)?.isDefault)} onChange={event => setForm({ ...form, isDefault: event.target.checked })} />기본주소로 설정</label>
        {editingId && items.find(item => item.id === editingId)?.isDefault && <small>기본주소는 해제할 수 없습니다. 다른 주소를 기본주소로 설정하면 자동으로 변경됩니다.</small>}
        {error && <p className="accountError">{error}</p>}
        <div className="formButtons">
          {editingId && <button type="button" onClick={() => { setEditingId(null); setSidoId(''); setSigungu([]); setForm(emptyAddress) }}>취소</button>}
          <button className="accountPrimary">저장</button>
        </div>
      </form>
    </div>
  </MySoodalLayout>
}

export function CustomerSecurityPage() {
  const [form, setForm] = useState({ currentPassword: '', newPassword: '', newPasswordConfirmation: '' }); const [notice, setNotice] = useState(''); const [error, setError] = useState(''); const [readiness, setReadiness] = useState<WithdrawalReadiness | null>(null); const [profile, setProfile] = useState<CustomerProfile | null>(null)
  useEffect(() => { Promise.all([customerAccountApi.withdrawalReadiness(), customerAccountApi.profile()]).then(([nextReadiness, nextProfile]) => { setReadiness(nextReadiness); setProfile(nextProfile) }).catch(reason => setError(message(reason))) }, [])
  const change = async (event: FormEvent) => { event.preventDefault(); try { await customerAccountApi.changePassword(form); setForm({ currentPassword: '', newPassword: '', newPasswordConfirmation: '' }); setNotice('비밀번호가 변경되었습니다.'); setError('') } catch (error) { setError(message(error)) } }
  return <MySoodalLayout title="로그인·보안" description="현재 계정과 비밀번호, 본인인증 및 탈퇴 요청을 관리합니다."><section className="securityIdentity"><strong>{profile?.loginId ?? '현재 로그인 계정'}</strong><span>계정 상태 {profile?.accountStatus ?? '확인 중'}</span><span>본인인증 연동 준비 중</span><button onClick={() => navigate('/password-reset')}>비밀번호 재설정 요청</button></section><form className="accountForm" onSubmit={event => void change(event)}><h2>비밀번호 변경</h2><label>현재 비밀번호<input type="password" autoComplete="current-password" value={form.currentPassword} onChange={event => setForm({ ...form, currentPassword: event.target.value })} /></label><label>새 비밀번호<input type="password" minLength={6} autoComplete="new-password" value={form.newPassword} onChange={event => setForm({ ...form, newPassword: event.target.value })} /></label><label>새 비밀번호 확인<input type="password" minLength={6} autoComplete="new-password" value={form.newPasswordConfirmation} onChange={event => setForm({ ...form, newPasswordConfirmation: event.target.value })} /></label><small>6자 이상이며 영문자, 숫자, 특수문자를 각각 포함해야 합니다.</small>{notice && <p className="successText">{notice}</p>}{error && <p className="accountError">{error}</p>}<button className="accountPrimary">비밀번호 변경</button></form><section className="withdrawalPanel"><h2>계정·회원 탈퇴</h2><p>{readiness?.guidance ?? '진행 중 업무를 확인하고 있습니다.'}</p>{readiness?.blockers.slice(0, 3).map(item => <span className="withdrawalBlocker" key={item.code}>{item.label} {item.count}건</span>)}{readiness?.hasMultipleActiveRoles && <p>다른 역할이 있어 고객 역할 종료와 전체 계정 탈퇴를 구분해야 합니다.</p>}<button onClick={() => navigate('/customer/security/withdrawal')}>탈퇴 준비상태와 신청 확인</button></section></MySoodalLayout>
}

export function CustomerConsentsPage() {
  const [items, setItems] = useState<Consent[]>([]); const [history, setHistory] = useState<ConsentHistory[]>([]); const [error, setError] = useState(''); const load = () => Promise.all([customerAccountApi.consents(), customerAccountApi.consentHistory()]).then(([current, past]) => { setItems(current); setHistory(past) }).catch(error => setError(message(error))); useEffect(() => { void load() }, [])
  const toggle = async (item: Consent, agreed: boolean) => { try { await customerAccountApi.updateConsent(item.legalDocumentVersionId, agreed); await load() } catch (error) { setError(message(error)) } }
  return <MySoodalLayout title="약관·동의" description="현재 적용 문서와 내가 동의한 정확한 버전·철회 이력을 확인합니다."><div className="consentList">{items.map(item => <article key={item.legalDocumentVersionId}><div><b>[{item.requirementCode === 'REQUIRED' ? '필수' : item.requirementCode === 'OPTIONAL' ? '선택' : '안내'}]</b><strong>{item.title}</strong><span>버전 {item.versionNo} · 시행 {date(item.effectiveFrom)} · {item.consentStatus === 'CONSENTED' ? `동의 ${date(item.consentedAt)}` : '미동의/철회'}</span>{item.isPlaceholder && <small>개발용 임시 약관 · 운영 전 승인 문서로 교체 필요</small>}<details><summary>약관 본문 보기</summary><p>{item.content}</p></details></div><label><input type="checkbox" checked={item.consentStatus === 'CONSENTED'} disabled={!item.canWithdraw} onChange={event => void toggle(item, event.target.checked)} />{item.consentStatus === 'CONSENTED' ? '동의' : '미동의'}</label></article>)}</div><section className="consentHistory"><h2>동의 이력</h2>{history.length ? history.map(item => <article key={`${item.legalDocumentVersionId}-${item.consentedAt}-${item.withdrawnAt}`}><strong>{item.title} v{item.versionNo}</strong><span>{item.consentStatus === 'CONSENTED' ? `동의 ${date(item.consentedAt)}` : `철회 ${date(item.withdrawnAt)}`}</span><small>{item.sourceCode === 'WEB_SIGNUP' ? '회원가입' : '마이수달'}에서 처리</small></article>) : <p className="accountEmpty">동의 이력이 없습니다.</p>}</section>{error && <p className="accountError">{error}</p>}</MySoodalLayout>
}

export function LegalDocumentsPage({ code }: { code?: string }) {
  const selected = code ? findPublicPolicy(code) : undefined
  const visible = code ? (selected ? [selected] : []) : publicPolicyDocuments
  return <CustomerAppLayout><section className="pageHeading"><p>서비스 정책</p><h1>{code ? visible[0]?.title ?? '약관을 찾을 수 없습니다' : '약관·개인정보처리방침'}</h1>{code && visible[0] && <span>{visible[0].status} · {visible[0].effectiveDate}</span>}</section><section className="legalDocuments publicPolicies">{visible.map(document => document && <article key={document.code}><header><div><b>{document.status}</b><h2>{document.title}</h2><span>{document.effectiveDate}</span></div></header><p className="policyIntroduction">{document.introduction}</p>{document.sections.map(section => <section className="policySection" key={section.heading}><h3>{section.heading}</h3>{section.paragraphs?.map(paragraph => <p key={paragraph}>{paragraph}</p>)}{section.items && <ul>{section.items.map(item => <li key={item}>{item}</li>)}</ul>}</section>)}<aside>{document.notice}</aside></article>)}{visible.length === 0 && <p className="accountEmpty">요청하신 문서를 찾을 수 없습니다.</p>}</section></CustomerAppLayout>
}

export function CustomerNotificationSettingsPage() {
  const [items, setItems] = useState<NotificationPreference[]>([]); const [error, setError] = useState(''); useEffect(() => { customerAccountApi.notificationPreferences().then(values => setItems(values.length ? values : [{ eventGroupCode: 'BUSINESS', webEnabled: true, kakaoEnabled: false, smsEnabled: false, emailEnabled: false, pushEnabled: false, rowVersion: '' }, { eventGroupCode: 'MARKETING', webEnabled: true, kakaoEnabled: false, smsEnabled: false, emailEnabled: false, pushEnabled: false, rowVersion: '' }])).catch(error => setError(message(error))) }, [])
  const toggle = async (item: NotificationPreference) => { try { const value = await customerAccountApi.updateNotificationPreference({ ...item, webEnabled: !item.webEnabled }); setItems(current => current.map(candidate => candidate.eventGroupCode === value.eventGroupCode ? value : candidate)) } catch (error) { setError(message(error)) } }
  return <MySoodalLayout title="알림 설정" description="업무 필수 알림과 마케팅 동의를 구분하여 관리합니다."><div className="notificationPreferences">{items.map(item => <article key={item.eventGroupCode}><header><div><strong>{item.eventGroupCode === 'BUSINESS' ? '업무 알림' : item.eventGroupCode === 'MARKETING' ? '마케팅 알림' : item.eventGroupCode}</strong><span>{item.eventGroupCode === 'BUSINESS' ? '요청·거래 수행에 필요한 정보성 알림입니다.' : '마케팅 수신 동의는 약관·동의 메뉴에서 별도로 관리합니다.'}</span></div><label><input type="checkbox" checked={item.webEnabled} onChange={() => void toggle(item)} />WEB {item.webEnabled ? '사용' : '사용 안 함'}</label></header><dl><div><dt>WEB</dt><dd>사용 가능</dd></div><div><dt>카카오 알림톡</dt><dd>준비 중</dd></div><div><dt>SMS</dt><dd>준비 중</dd></div><div><dt>Email</dt><dd>준비 중</dd></div><div><dt>Push</dt><dd>준비 중</dd></div></dl></article>)}</div><p className="notIntegratedNotice">외부 채널 설정값은 실제 발송 성공을 의미하지 않습니다. 카카오·SMS·Email·Push는 현재 연동 준비 중입니다.</p><button className="accountSecondary" onClick={() => navigate('/customer/consents')}>마케팅 동의 확인</button>{error && <p className="accountError">{error}</p>}</MySoodalLayout>
}

export function CustomerNotificationCenterPage() {
  const [view, setView] = useState<'ALL' | 'UNREAD' | 'ARCHIVED'>('ALL'); const [items, setItems] = useState<CustomerNotification[]>([]); const [selected, setSelected] = useState<CustomerNotification | null>(null); const [unread, setUnread] = useState(0); const [error, setError] = useState('')
  const load = (nextView = view) => Promise.all([customerAccountApi.notifications(nextView), customerAccountApi.unreadNotificationCount()]).then(([values, count]) => { setItems(values); setUnread(count.count); setSelected(current => current && values.some(item => item.id === current.id) ? current : null) }).catch(reason => setError(message(reason)))
  useEffect(() => { void load(view) }, [view])
  const open = async (item: CustomerNotification) => { try { const detail = await customerAccountApi.notification(item.id); if (!detail.readAt) await customerAccountApi.readNotification(item.id); setSelected({ ...detail, readAt: detail.readAt ?? new Date().toISOString() }); await load(view) } catch (reason) { setError(message(reason)) } }
  const move = async (item: CustomerNotification) => { try { const target = await safeNotificationTarget(item); if (target) navigate(target); else setError('연결할 수 있는 안전한 업무 화면이 없습니다.') } catch (reason) { setError(message(reason)) } }
  const readAll = async () => { try { await customerAccountApi.readAllNotifications(); await load(view) } catch (reason) { setError(message(reason)) } }
  const archive = async (item: CustomerNotification) => { try { await customerAccountApi.archiveNotification(item.id); await load(view) } catch (reason) { setError(message(reason)) } }
  return <MySoodalLayout title="알림센터" description="알림 상태와 업무 분류를 확인하고 안전한 내부 화면으로 이동합니다."><div className="notificationToolbar"><div role="tablist" aria-label="알림 보기">{([['ALL', '전체'], ['UNREAD', '미읽음'], ['ARCHIVED', '보관']] as const).map(([code, label]) => <button role="tab" aria-selected={view === code} className={view === code ? 'isActive' : ''} onClick={() => setView(code)} key={code}>{label}</button>)}</div><span>읽지 않은 알림 <strong>{unread}</strong>개</span><button onClick={() => void readAll()}>모두 읽음</button></div>{error && <p className="accountError">{error}</p>}<div className="notificationWorkspace"><div className="notificationList">{items.length === 0 ? <p className="accountEmpty">조건에 맞는 알림이 없습니다.</p> : items.map(item => <article key={item.id} className={!item.readAt ? 'notificationUnread' : undefined}><button type="button" onClick={() => void open(item)}><span className="notificationState" aria-label={item.readAt ? '읽음' : '미읽음'}>{item.readAt ? '✓ 읽음' : '● 미읽음'}</span><small>{notificationGroup(item)} · {date(item.createdAt)}</small><strong>{item.title}</strong><span>{item.body}</span></button>{!item.isArchived && <button type="button" className="accountSecondary" onClick={() => void archive(item)}>보관</button>}</article>)}</div><aside className="notificationDetail">{selected ? <><span>{notificationGroup(selected)}</span><h2>{selected.title}</h2><p>{selected.body}</p><dl><div><dt>상태</dt><dd>{selected.readAt ? '읽음' : '미읽음'}</dd></div><div><dt>수신일</dt><dd>{date(selected.createdAt)}</dd></div></dl>{selected.targetPublicId && <button className="accountPrimary" onClick={() => void move(selected)}>업무화면 이동</button>}</> : <p className="accountEmpty">알림을 선택하면 상세내용을 확인할 수 있습니다.</p>}</aside></div><button className="accountSecondary" onClick={() => navigate('/customer/notification-settings')}>알림 설정</button></MySoodalLayout>
}

function notificationGroup(item: CustomerNotification) {
  const code = `${item.eventTypeCode} ${item.targetTypeCode ?? ''}`.toUpperCase()
  if (/REQUEST|QUOTE|DISPATCH/.test(code)) return '요청·견적'
  if (/TRANSACTION|APPOINTMENT/.test(code)) return '거래·일정'
  if (/COMPLETION|REVIEW/.test(code)) return '완료·리뷰'
  if (/AFTER_SERVICE|DISPUTE|REPORT/.test(code)) return 'A/S·분쟁'
  if (/SUBSCRIPTION/.test(code)) return '구독'
  if (/INTERIOR/.test(code)) return '인테리어'
  return '계정·시스템'
}
