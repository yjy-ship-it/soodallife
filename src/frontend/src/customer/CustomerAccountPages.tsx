import { useEffect, useState } from 'react'
import type { FormEvent, PropsWithChildren } from 'react'
import { useAuthentication } from '../auth/AuthenticationContext'
import { getSafeReturnUrl, navigate } from '../auth/routing'
import { BrandLogo } from '../components/BrandLogo'
import { ServiceFooter } from '../components/ServiceFooter'
import { customerAccountApi, CustomerAccountApiError } from './accountApi'
import type { AdministrativeArea, Consent, CustomerAddress, CustomerProfile, LegalDocument, NotificationPreference } from './accountTypes'
import { CustomerAppLayout } from './CustomerAppLayout'
import './customerAccount.css'

const message = (error: unknown) => error instanceof CustomerAccountApiError ? error.message : '요청을 처리하지 못했습니다.'
const date = (value: string | null) => value ? new Intl.DateTimeFormat('ko-KR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : '없음'

export function CustomerSignupPage() {
  const { login } = useAuthentication()
  const [step, setStep] = useState(1)
  const [form, setForm] = useState({ loginId: '', name: '', email: '', phone: '', password: '', passwordConfirmation: '' })
  const [documents, setDocuments] = useState<LegalDocument[]>([])
  const [agreed, setAgreed] = useState<Record<string, boolean>>({})
  const [loginChecked, setLoginChecked] = useState(false)
  const [emailChecked, setEmailChecked] = useState(false)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  useEffect(() => { customerAccountApi.legalDocuments().then(setDocuments).catch(error => setError(message(error))) }, [])
  const update = (name: keyof typeof form, value: string) => { setForm(current => ({ ...current, [name]: value })); if (name === 'loginId') setLoginChecked(false); if (name === 'email') setEmailChecked(false) }
  const checkLogin = async () => { try { const value = await customerAccountApi.loginAvailability(form.loginId); setLoginChecked(value.available); setError(value.available ? '' : '이미 사용 중인 아이디입니다.') } catch (error) { setError(message(error)) } }
  const checkEmail = async () => { try { const value = await customerAccountApi.emailAvailability(form.email); setEmailChecked(value.available); setError(value.available ? '' : '이미 사용 중인 이메일입니다.') } catch (error) { setError(message(error)) } }
  const next = () => {
    setError('')
    if (step === 1 && (!loginChecked || !form.name || !form.password || form.password !== form.passwordConfirmation)) return setError('아이디 중복확인과 이름·비밀번호 확인을 완료해 주세요.')
    if (step === 2 && (!emailChecked || !/^010-\d{4}-\d{4}$/.test(form.phone))) return setError('이메일 중복확인과 010 휴대전화 형식을 확인해 주세요.')
    if (step === 3 && documents.some(item => item.requirementCode === 'REQUIRED' && !agreed[item.versionId])) return setError('필수 약관에 동의해 주세요.')
    setStep(current => Math.min(4, current + 1))
  }
  const allAgreed = documents.length > 0 && documents.every(item => agreed[item.versionId])
  const register = async () => {
    setBusy(true); setError('')
    try {
      await customerAccountApi.register({ ...form, consents: documents.map(item => ({ legalDocumentVersionId: item.versionId, agreed: Boolean(agreed[item.versionId]) })) })
      await login(form.loginId, form.password)
      setStep(5)
    } catch (error) { setError(message(error)) } finally { setBusy(false) }
  }
  const finish = () => navigate(getSafeReturnUrl() ?? '/customer', true)
  return <div className="signupShell"><header><BrandLogo /><button onClick={() => navigate('/login')}>로그인</button></header><main className="signupMain"><ol className="signupSteps" aria-label="회원가입 단계">{['계정정보', '연락처', '약관동의', '정보확인', '가입완료'].map((label, index) => <li className={step >= index + 1 ? 'isActive' : ''} key={label}><span>{index + 1}</span>{label}</li>)}</ol><section className="signupCard" aria-live="polite">
    {step === 1 && <><p className="accountEyebrow">1단계</p><h1>사용할 계정을 만들어 주세요</h1><label>로그인 아이디<div className="inlineField"><input value={form.loginId} onChange={event => update('loginId', event.target.value)} autoComplete="username" /><button onClick={() => void checkLogin()}>중복확인</button></div></label>{loginChecked && <small className="successText">사용할 수 있는 아이디입니다.</small>}<label>이름<input value={form.name} onChange={event => update('name', event.target.value)} autoComplete="name" /></label><label>비밀번호<input type="password" value={form.password} onChange={event => update('password', event.target.value)} autoComplete="new-password" /></label><small>10자 이상, 영문 대·소문자, 숫자, 특수문자를 포함해 주세요.</small><label>비밀번호 확인<input type="password" value={form.passwordConfirmation} onChange={event => update('passwordConfirmation', event.target.value)} autoComplete="new-password" /></label></>}
    {step === 2 && <><p className="accountEyebrow">2단계</p><h1>연락처를 입력해 주세요</h1><label>이메일<div className="inlineField"><input type="email" value={form.email} onChange={event => update('email', event.target.value)} autoComplete="email" /><button onClick={() => void checkEmail()}>중복확인</button></div></label>{emailChecked && <small className="successText">사용할 수 있는 이메일입니다.</small>}<label>휴대전화<input value={form.phone} onChange={event => update('phone', event.target.value)} placeholder="010-1234-5678" inputMode="tel" autoComplete="tel" /></label><div className="notIntegratedNotice"><strong>본인인증 미연동</strong><span>현재 SMS나 외부 본인인증을 수행하지 않으며 VERIFIED로 처리하지 않습니다.</span></div></>}
    {step === 3 && <><p className="accountEyebrow">3단계</p><h1>약관을 확인해 주세요</h1><label className="consentAll"><input type="checkbox" checked={allAgreed} onChange={event => setAgreed(Object.fromEntries(documents.map(item => [item.versionId, event.target.checked])))} />전체 동의</label><div className="signupConsents">{documents.map(item => <label key={item.versionId}><span><input type="checkbox" checked={Boolean(agreed[item.versionId])} onChange={event => setAgreed(current => ({ ...current, [item.versionId]: event.target.checked }))} /><b>[{item.requirementCode === 'REQUIRED' ? '필수' : '선택'}]</b> {item.title}</span><details><summary>내용 보기</summary><p>{item.content}</p>{item.isPlaceholder && <strong>운영 전 승인 문서로 교체해야 하는 개발용 문서입니다.</strong>}</details></label>)}</div></>}
    {step === 4 && <><p className="accountEyebrow">4단계</p><h1>가입 정보를 확인해 주세요</h1><dl className="confirmationList"><div><dt>아이디</dt><dd>{form.loginId}</dd></div><div><dt>이름</dt><dd>{form.name}</dd></div><div><dt>이메일</dt><dd>{form.email}</dd></div><div><dt>휴대전화</dt><dd>{form.phone}</dd></div><div><dt>선택 동의</dt><dd>{documents.filter(item => item.requirementCode === 'OPTIONAL' && agreed[item.versionId]).map(item => item.title).join(', ') || '없음'}</dd></div></dl></>}
    {step === 5 && <div className="signupComplete"><span aria-hidden="true">✓</span><h1>가입이 완료되었습니다</h1><p>수달 라이프에서 필요한 생활서비스를 찾아보세요.</p><button className="accountPrimary" onClick={finish}>시작하기</button></div>}
    {error && <p className="accountError" role="alert">{error}</p>}{step < 5 && <div className="signupActions">{step > 1 && <button onClick={() => setStep(current => current - 1)}>이전</button>}{step < 4 ? <button className="accountPrimary" onClick={next}>다음</button> : <button className="accountPrimary" disabled={busy} onClick={() => void register()}>{busy ? '가입 처리 중…' : '가입하기'}</button>}</div>}
  </section></main><ServiceFooter variant="customer" /></div>
}

export function PasswordResetRequestPage() {
  const [identifier, setIdentifier] = useState(''); const [sent, setSent] = useState(false); const [error, setError] = useState('')
  const submit = async (event: FormEvent) => { event.preventDefault(); try { await customerAccountApi.requestPasswordReset(identifier); setSent(true); setError('') } catch (error) { setError(message(error)) } }
  return <div className="signupShell"><header><BrandLogo /><button onClick={() => navigate('/login')}>로그인으로</button></header><main className="signupMain"><section className="signupCard"><p className="accountEyebrow">로그인·보안</p><h1>비밀번호 재설정 요청</h1>{sent ? <div className="notIntegratedNotice"><strong>요청이 접수되었습니다</strong><span>현재 SMS/Email 발송 Adapter가 미연동되어 재설정 메시지는 발송되지 않습니다.</span></div> : <form onSubmit={event => void submit(event)}><label>아이디 또는 이메일<input value={identifier} onChange={event => setIdentifier(event.target.value)} /></label><button className="accountPrimary" disabled={!identifier}>재설정 요청</button></form>}{error && <p className="accountError">{error}</p>}</section></main><ServiceFooter variant="customer" /></div>
}

const myMenus = [
  ['/customer', '마이수달'], ['/customer/profile', '내 정보'], ['/customer/addresses', '주소 관리'], ['/customer/security', '로그인·보안'],
  ['/customer/consents', '약관·동의'], ['/customer/notification-settings', '알림 설정'],
] as const
function MySoodalLayout({ title, description, children }: PropsWithChildren<{ title: string; description: string }>) {
  const path = window.location.pathname
  return <CustomerAppLayout><section className="mySoodalHeading"><p>MY SOODAL</p><h1>{title}</h1><span>{description}</span></section><div className="mySoodalLayout"><nav aria-label="마이수달 메뉴">{myMenus.map(([href, label]) => <button className={path === href ? 'isActive' : ''} onClick={() => navigate(href)} key={href}>{label}</button>)}<hr />{[['/customer/requests', '요청·견적'], ['/customer/transactions', '진행 거래'], ['/support', '고객센터']].map(([href, label]) => <button onClick={() => navigate(href)} key={href}>{label}</button>)}</nav><section className="mySoodalContent">{children}</section></div></CustomerAppLayout>
}

export function MySoodalPage() {
  const [profile, setProfile] = useState<CustomerProfile | null>(null)
  useEffect(() => { customerAccountApi.profile().then(setProfile).catch(() => undefined) }, [])
  return <MySoodalLayout title="마이수달" description="계정과 개인정보를 안전하게 관리하세요."><div className="accountSummary"><p>안녕하세요</p><h2>{profile?.name ?? '고객'}님</h2><span>{profile?.loginId}</span></div><div className="myCardGrid">{myMenus.slice(1).map(([href, label]) => <button onClick={() => navigate(href)} key={href}><strong>{label}</strong><span>{label === '주소 관리' ? '서비스에 사용할 주소를 관리합니다.' : label === '로그인·보안' ? '비밀번호와 탈퇴 신청을 관리합니다.' : '내 계정 설정을 확인합니다.'}</span></button>)}</div><section className="futureMenu"><h2>다음 단계에서 연결됩니다</h2><p>이용내역 · 서비스 이력 · 결제 · A/S/분쟁</p></section></MySoodalLayout>
}

export function CustomerProfilePage() {
  const [profile, setProfile] = useState<CustomerProfile | null>(null); const [form, setForm] = useState({ name: '', email: '', phone: '' }); const [notice, setNotice] = useState(''); const [error, setError] = useState('')
  useEffect(() => { customerAccountApi.profile().then(value => { setProfile(value); setForm({ name: value.name, email: value.email ?? '', phone: value.phone ?? '' }) }).catch(error => setError(message(error))) }, [])
  const save = async (event: FormEvent) => { event.preventDefault(); try { const value = await customerAccountApi.updateProfile(form); setProfile(value); setNotice('내 정보가 변경되었습니다.'); setError('') } catch (error) { setError(message(error)) } }
  return <MySoodalLayout title="내 정보" description="본인 계정의 기본정보만 조회하고 수정할 수 있습니다."><form className="accountForm" onSubmit={event => void save(event)}><label>로그인 아이디<input value={profile?.loginId ?? ''} disabled /></label><label>이름<input value={form.name} onChange={event => setForm({ ...form, name: event.target.value })} /></label><label>이메일<input type="email" value={form.email} onChange={event => setForm({ ...form, email: event.target.value })} /></label><label>휴대전화<input value={form.phone} onChange={event => setForm({ ...form, phone: event.target.value })} placeholder="010-1234-5678" /></label><div className="accountMeta"><span>가입일 {date(profile?.createdAt ?? null)}</span><span>최근 로그인 {date(profile?.lastLoginAt ?? null)}</span><span>상태 {profile?.accountStatus}</span><span>이메일/휴대전화 인증: 외부연동 전</span></div>{notice && <p className="successText">{notice}</p>}{error && <p className="accountError">{error}</p>}<button className="accountPrimary">변경 저장</button></form></MySoodalLayout>
}

type AddressForm = { addressName: string; recipientName: string; postalCode: string; roadAddress: string; detailAddress: string; administrativeAreaId: string; latitude: number | null; longitude: number | null; isDefault: boolean; concurrencyToken: string }
const emptyAddress: AddressForm = { addressName: '', recipientName: '', postalCode: '', roadAddress: '', detailAddress: '', administrativeAreaId: '', latitude: null, longitude: null, isDefault: false, concurrencyToken: '' }
export function CustomerAddressesPage() {
  const [items, setItems] = useState<CustomerAddress[]>([]); const [editingId, setEditingId] = useState<string | null>(null); const [form, setForm] = useState(emptyAddress); const [error, setError] = useState('')
  const [sidos, setSidos] = useState<AdministrativeArea[]>([]); const [sigungu, setSigungu] = useState<AdministrativeArea[]>([]); const [sidoId, setSidoId] = useState('')
  const load = () => customerAccountApi.addresses().then(setItems).catch(error => setError(message(error)))
  useEffect(() => { void load(); customerAccountApi.sidos().then(setSidos).catch(() => undefined) }, [])
  useEffect(() => { if (!sidoId) { setSigungu([]); return }; customerAccountApi.sigungu(sidoId).then(setSigungu).catch(() => setSigungu([])) }, [sidoId])
  const edit = (item: CustomerAddress) => { setEditingId(item.id); setForm({ addressName: item.addressName, recipientName: item.recipientName ?? '', postalCode: item.postalCode, roadAddress: item.roadAddress, detailAddress: item.detailAddress, administrativeAreaId: item.administrativeAreaId ?? '', latitude: item.latitude, longitude: item.longitude, isDefault: item.isDefault, concurrencyToken: item.concurrencyToken }) }
  const save = async (event: FormEvent) => { event.preventDefault(); try { const value = { ...form, administrativeAreaId: form.administrativeAreaId || null }; if (editingId) await customerAccountApi.updateAddress(editingId, value); else await customerAccountApi.createAddress(value); setEditingId(null); setForm(emptyAddress); setError(''); await load() } catch (error) { setError(message(error)) } }
  const remove = async (item: CustomerAddress) => { try { await customerAccountApi.deleteAddress(item.id, item.concurrencyToken); await load() } catch (error) { setError(message(error)) } }
  return <MySoodalLayout title="주소 관리" description="상세주소는 본인에게만 표시되며 주소록 자체를 공급자에게 공개하지 않습니다."><div className="addressLayout"><div className="addressList">{items.length ? items.map(item => <article key={item.id}><header><strong>{item.addressName}</strong>{item.isDefault && <span>기본주소</span>}</header><p>{item.postalCode} {item.roadAddress}<br />{item.detailAddress}</p><small>{item.recipientName}</small><div><button onClick={() => edit(item)}>수정</button><button onClick={() => void remove(item)}>삭제</button></div></article>) : <p className="accountEmpty">등록된 주소가 없습니다.</p>}</div><form className="accountForm addressForm" onSubmit={event => void save(event)}><h2>{editingId ? '주소 수정' : '새 주소'}</h2><label>주소 별칭<input value={form.addressName} onChange={event => setForm({ ...form, addressName: event.target.value })} placeholder="우리집" /></label><label>받는 분<input value={form.recipientName} onChange={event => setForm({ ...form, recipientName: event.target.value })} /></label><div className="areaFields"><label>시·도<select value={sidoId} onChange={event => { setSidoId(event.target.value); setForm({ ...form, administrativeAreaId: '' }) }}><option value="">선택</option>{sidos.map(item => <option value={item.id} key={item.id}>{item.name}</option>)}</select></label><label>시·군·구<select value={form.administrativeAreaId} onChange={event => setForm({ ...form, administrativeAreaId: event.target.value })}><option value="">선택</option>{sigungu.map(item => <option value={item.id} key={item.id}>{item.name}</option>)}</select></label></div><label>우편번호<input value={form.postalCode} onChange={event => setForm({ ...form, postalCode: event.target.value })} /></label><label>도로명 주소<input value={form.roadAddress} onChange={event => setForm({ ...form, roadAddress: event.target.value })} /></label><label>상세주소<input value={form.detailAddress} onChange={event => setForm({ ...form, detailAddress: event.target.value })} /></label><small>외부 주소검색은 아직 연동하지 않았으며 현재는 기존 행정구역과 수동 주소 입력을 사용합니다.</small><label className="checkLabel"><input type="checkbox" checked={form.isDefault} onChange={event => setForm({ ...form, isDefault: event.target.checked })} />기본주소로 설정</label>{error && <p className="accountError">{error}</p>}<div className="formButtons">{editingId && <button type="button" onClick={() => { setEditingId(null); setForm(emptyAddress) }}>취소</button>}<button className="accountPrimary">저장</button></div></form></div></MySoodalLayout>
}

export function CustomerSecurityPage() {
  const [form, setForm] = useState({ currentPassword: '', newPassword: '', newPasswordConfirmation: '' }); const [notice, setNotice] = useState(''); const [error, setError] = useState(''); const [withdrawal, setWithdrawal] = useState('')
  const change = async (event: FormEvent) => { event.preventDefault(); try { await customerAccountApi.changePassword(form); setForm({ currentPassword: '', newPassword: '', newPasswordConfirmation: '' }); setNotice('비밀번호가 변경되었습니다.'); setError('') } catch (error) { setError(message(error)) } }
  const withdraw = async () => { try { await customerAccountApi.requestWithdrawal('CUSTOMER_ROLE'); setWithdrawal('고객 역할 탈퇴 신청이 접수되었습니다. 계정 전체 탈퇴와 분리되어 검토됩니다.') } catch (error) { setError(message(error)) } }
  return <MySoodalLayout title="로그인·보안" description="비밀번호 변경과 계정 관련 요청을 관리합니다."><form className="accountForm" onSubmit={event => void change(event)}><h2>비밀번호 변경</h2><label>현재 비밀번호<input type="password" value={form.currentPassword} onChange={event => setForm({ ...form, currentPassword: event.target.value })} /></label><label>새 비밀번호<input type="password" value={form.newPassword} onChange={event => setForm({ ...form, newPassword: event.target.value })} /></label><label>새 비밀번호 확인<input type="password" value={form.newPasswordConfirmation} onChange={event => setForm({ ...form, newPasswordConfirmation: event.target.value })} /></label><small>10자 이상, 영문 대·소문자, 숫자, 특수문자를 포함해야 합니다.</small>{notice && <p className="successText">{notice}</p>}{error && <p className="accountError">{error}</p>}<button className="accountPrimary">비밀번호 변경</button></form><section className="withdrawalPanel"><h2>회원탈퇴 기반</h2><p>진행 중인 요청·거래·A/S·분쟁·구독·인테리어·결제 상태 검토가 필요하므로 즉시 계정을 삭제하지 않습니다.</p><button onClick={() => void withdraw()}>고객 역할 탈퇴 신청</button>{withdrawal && <p>{withdrawal}</p>}</section></MySoodalLayout>
}

export function CustomerConsentsPage() {
  const [items, setItems] = useState<Consent[]>([]); const [error, setError] = useState(''); const load = () => customerAccountApi.consents().then(setItems).catch(error => setError(message(error))); useEffect(() => { void load() }, [])
  const toggle = async (item: Consent, agreed: boolean) => { try { await customerAccountApi.updateConsent(item.legalDocumentVersionId, agreed); await load() } catch (error) { setError(message(error)) } }
  return <MySoodalLayout title="약관·동의" description="동의한 정확한 문서 버전과 선택 동의 상태를 확인합니다."><div className="consentList">{items.map(item => <article key={item.legalDocumentVersionId}><div><b>[{item.requirementCode === 'REQUIRED' ? '필수' : '선택'}]</b><strong>{item.title}</strong><span>버전 {item.versionNo} · {item.consentStatus === 'CONSENTED' ? `동의 ${date(item.consentedAt)}` : '미동의/철회'}</span>{item.isPlaceholder && <small>개발용 Placeholder 문서</small>}</div><label><input type="checkbox" checked={item.consentStatus === 'CONSENTED'} disabled={item.requirementCode === 'REQUIRED'} onChange={event => void toggle(item, event.target.checked)} />{item.consentStatus === 'CONSENTED' ? '동의' : '미동의'}</label></article>)}</div>{error && <p className="accountError">{error}</p>}</MySoodalLayout>
}

export function CustomerNotificationSettingsPage() {
  const [items, setItems] = useState<NotificationPreference[]>([]); const [error, setError] = useState(''); useEffect(() => { customerAccountApi.notificationPreferences().then(setItems).catch(error => setError(message(error))) }, [])
  const toggle = async (item: NotificationPreference) => { try { const value = await customerAccountApi.updateNotificationPreference({ ...item, webEnabled: !item.webEnabled }); setItems(current => current.map(candidate => candidate.eventGroupCode === value.eventGroupCode ? value : candidate)) } catch (error) { setError(message(error)) } }
  return <MySoodalLayout title="알림 설정" description="현재 연결된 Web 알림 수신 설정을 관리합니다."><div className="consentList">{items.length ? items.map(item => <article key={item.eventGroupCode}><div><strong>{item.eventGroupCode}</strong><span>카카오·SMS·Email·Push는 외부연동 전입니다.</span></div><label><input type="checkbox" checked={item.webEnabled} onChange={() => void toggle(item)} />Web 알림</label></article>) : <p className="accountEmpty">저장된 알림 설정이 없습니다. 알림 이벤트가 생성되면 이곳에서 관리합니다.</p>}</div>{error && <p className="accountError">{error}</p>}</MySoodalLayout>
}
