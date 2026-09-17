import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from 'react'
import { useAuthentication } from '../auth/AuthenticationContext'
import { createLoginPath, navigate } from '../auth/routing'
import { PasswordInput } from '../auth/PasswordInput'
import { accountPasswordGuidance, isAccountPasswordValid } from '../auth/passwordPolicy'
import { BrandLogo } from '../components/BrandLogo'
import { ServiceFooter } from '../components/ServiceFooter'
import { AuthenticatedLayout } from '../components/AuthenticatedLayout'
import { customerAccountApi } from '../customer/accountApi'
import { apiUrl } from '../config/apiEndpoint'
import * as api from './api'
import type { ProviderDashboard, ProviderDocument, ProviderDocumentType, ProviderLegalDocument, ProviderProfile, ProviderRequirement, ProviderServiceCategory } from './types'
import '../customer/customerAccount.css'
import './provider.css'
import './providerEnhancements.css'
import { soodalPrompt } from '../components/soodalDialog'

const approvalLabel: Record<string, string> = { PENDING: '등록 기준 확인 중', APPROVED: '승인 완료', REJECTED: '보완 또는 재확인 필요', SUSPENDED: '이용 정지' }
const activityLabel: Record<string, string> = { ACTIVE: '활동 중', INACTIVE: '활동 전', SUSPENDED: '활동 정지', EXIT_PENDING: '활동 종료 처리 중', EXITED: '활동 종료' }
const message = (error: unknown) => error instanceof Error ? error.message : '요청을 처리하지 못했습니다.'

export function ProviderPublicStartPage() {
  return <div className="providerPublicShell"><div className="providerPublicHero"><section className="providerPublicIntro"><BrandLogo /><p>수달 전문가</p><h1>수달 라이프<br />전문가 등록</h1><p>서비스 분야와 활동지역을 직접 설정하고, 법령이나 안전정책상 필요한 경우에만 증빙을 등록합니다. 등록 기준 확인 전에는 요청 수신, 견적 제출, 고객 연락처 조회가 제한됩니다.</p><div className="providerActions"><button className="providerPrimary" onClick={() => navigate('/provider/signup')}>전문가 등록 시작</button><button className="providerSecondary" onClick={() => navigate(createLoginPath('/provider'))}>전문가 로그인</button></div></section><section className="providerPublicSteps"><h2>등록 절차</h2><ol><li>계정과 전문가 기본정보 입력</li><li>실제 제공 서비스 선택</li><li>서비스별 활동지역 또는 전국 비대면 설정</li><li>법령·안전정책상 필요한 증빙 제출</li><li>등록 기준 확인 및 필요한 항목 보완</li><li>확인된 서비스부터 이용</li></ol><p>서비스 범위와 활동지역은 전문가가 직접 관리합니다. 수익 또는 수수료 금액은 서비스 정책과 실제 거래 조건에 따라 달라지며 이 화면에서 보장하지 않습니다.</p></section></div><ServiceFooter variant="provider" /></div>
}

type SignupState = { loginId: string; password: string; passwordConfirmation: string; email: string; phone: string; phoneVerificationToken: string; businessName: string; representativeName: string; contactName: string; businessRegistrationNumber: string; businessAddress: string; businessTypeText: string; businessItemText: string; introduction: string }
const emptySignup: SignupState = { loginId: '', password: '', passwordConfirmation: '', email: '', phone: '', phoneVerificationToken: '', businessName: '', representativeName: '', contactName: '', businessRegistrationNumber: '', businessAddress: '', businessTypeText: '', businessItemText: '', introduction: '' }
const signupText = (value: unknown) => typeof value === 'string' ? value : ''
const signupTrimmed = (value: unknown) => signupText(value).trim()
const formatMobilePhone = (value: string) => { const digits = value.replace(/\D/g, '').slice(0, 11); if (digits.length <= 3) return digits; if (digits.length <= 7) return `${digits.slice(0, 3)}-${digits.slice(3)}`; return `${digits.slice(0, 3)}-${digits.slice(3, 7)}-${digits.slice(7)}` }
const formatBusinessNumber = (value: string) => { const digits = value.replace(/\D/g, '').slice(0, 10); if (digits.length <= 3) return digits; if (digits.length <= 5) return `${digits.slice(0, 3)}-${digits.slice(3)}`; return `${digits.slice(0, 3)}-${digits.slice(3, 5)}-${digits.slice(5)}` }
const validEmail = (value: string) => /^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(value.trim())
const emailDomains = ['naver.com', 'gmail.com', 'daum.net', 'hanmail.net', 'kakao.com', 'nate.com'] as const
const publicIntroductionMaxBytes = 8000
const utf8ByteLength = (value: string) => new TextEncoder().encode(value).length
const normalizePublicUrl = (value: string | null | undefined) => { const trimmed = value?.trim() ?? ''; if (!trimmed) return null; if (/^https:\/\//i.test(trimmed)) return trimmed; if (/^http:\/\//i.test(trimmed)) return `https://${trimmed.slice(7)}`; return `https://${trimmed}` }
const friendlyStatus = (value: string | null | undefined) => (({ NOT_INTEGRATED: '외부 검사 연동 준비 중', PENDING: '확인 대기', PROCESSING: '확인 중', CLEAN: '안전 확인', SAFE: '안전 확인', VERIFIED: '확인 완료', APPROVED: '승인 완료', REJECTED: '보완 필요', FAILED: '확인 실패' } as Record<string,string>)[value ?? ''] ?? value ?? '확인 전')
type AreaOption = { id: string; name: string; code: string; parentId: string | null; parentName: string | null }
const validBusinessNumber = (value: string) => { const digits = value.replace(/\D/g, ''); if (digits.length !== 10) return false; const n = [...digits].map(Number), w = [1,3,7,1,3,7,1,3,5]; const sum = w.reduce((v, x, i) => v + x * n[i], 0) + Math.floor(n[8] * 5 / 10); return (10 - sum % 10) % 10 === n[9] }
const providerIntroductionExamples = [
  { label: '생활수리·설비 예시', value: '생활수리 및 설비 분야에서 10년 이상 현장 경험을 쌓았습니다. 수도·배관·전기 등 생활수리를 전문으로 하며, 방문 전 작업 범위와 예상 비용을 안내하고 현장 확인 후 변경 사항을 설명드립니다. 고객 문의에는 신속하고 친절하게 응대하며, 시공 후 발생한 동일 하자는 확인 후 책임 있게 A/S하겠습니다.' },
  { label: '청소·생활관리 예시', value: '입주청소와 생활관리 서비스를 다년간 제공해 왔습니다. 현장 오염도와 공간 특성을 먼저 확인하고 구역별 작업 계획을 세워 꼼꼼하게 진행합니다. 작업 전후 사진과 완료 내용을 안내하며, 고객 요청사항을 경청하고 미흡한 부분은 확인 후 신속하게 보완하겠습니다.' },
  { label: '인테리어 예시', value: '인테리어 시공 경험을 바탕으로 도배·바닥·부분수리 작업을 전문적으로 제공합니다. 현장 실측과 상담 후 공정·자재·일정을 투명하게 설명하고, 승인된 범위에 따라 안전하게 시공합니다. 진행 상황을 성실히 공유하며 계약한 하자보수 기준에 따라 책임 있게 A/S하겠습니다.' },
] as const

export function ProviderSignupPage() {
  const { status, user, refresh } = useAuthentication()
  const existing = status === 'authenticated' && !!user
  const [step, setStep] = useState(1)
  const [form, setForm] = useState<SignupState>(() => ({ ...emptySignup }))
  const [legal, setLegal] = useState<ProviderLegalDocument[]>([])
  const [consents, setConsents] = useState<Record<string, boolean>>({})
  const [phoneChecked, setPhoneChecked] = useState(false)
  const [identityDocument, setIdentityDocument] = useState<File | null>(null)
  const documentInput = useRef<HTMLInputElement>(null)
  const [sidos, setSidos] = useState<AreaOption[]>([]), [sigungu, setSigungu] = useState<AreaOption[]>([])
  const [areaError, setAreaError] = useState('')
  const [sidoId, setSidoId] = useState(''), [sigunguId, setSigunguId] = useState(''), [addressDetail, setAddressDetail] = useState('')
  const [businessValidation, setBusinessValidation] = useState('')
  const [verifiedBusinessNumber, setVerifiedBusinessNumber] = useState('')
  const [loginChecked, setLoginChecked] = useState(false)
  const [error, setError] = useState('')
  const [done, setDone] = useState(false)
  const [continuing, setContinuing] = useState(false)
  useEffect(() => { api.getProviderLegalDocuments().then(setLegal).catch(reason => setError(message(reason))) }, [])
  const loadSidos = useCallback(async () => {
    setAreaError('')
    try {
      const values = await customerAccountApi.sidos()
      setSidos(values)
      if (values.length === 0) setAreaError('등록된 시·도 목록이 없습니다. 잠시 후 다시 시도해 주세요.')
    } catch (reason) {
      console.error('Provider signup SIDO load failed.', reason)
      setSidos([])
      setAreaError('사업장 시·도 목록을 불러오지 못했습니다. 다시 시도해 주세요.')
    }
  }, [])
  const loadSigungu = useCallback(async (parentId: string) => {
    setAreaError('')
    try {
      const values = await customerAccountApi.sigungu(parentId)
      setSigungu(values)
      if (values.length === 0) setAreaError('선택한 시·도의 시·군·구 목록이 없습니다. 다시 시도해 주세요.')
    } catch (reason) {
      console.error('Provider signup SIGUNGU load failed.', reason)
      setSigungu([])
      setAreaError('사업장 시·군·구 목록을 불러오지 못했습니다. 다시 시도해 주세요.')
    }
  }, [])
  useEffect(() => { void loadSidos() }, [loadSidos])
  useEffect(() => {
    setSigunguId('')
    if (!sidoId) { setSigungu([]); return }
    void loadSigungu(sidoId)
  }, [loadSigungu, sidoId])
  useEffect(() => { const sido = sidos.find(x => x.id === sidoId)?.name ?? '', gu = sigungu.find(x => x.id === sigunguId)?.name ?? ''; set('businessAddress', [sido, gu, signupTrimmed(addressDetail)].filter(Boolean).join(' ')) }, [sidoId, sigunguId, addressDetail, sidos, sigungu])
  const set = (key: keyof SignupState, value: string) => setForm(current => ({ ...emptySignup, ...(current ?? {}), [key]: signupText(value) }))
  const checkPhone = async () => { setError(''); setPhoneChecked(false); try { const result = await api.getPhoneAvailability(form.phone); if (!result.available) { set('phoneVerificationToken', ''); setError('이미 등록된 휴대전화 번호입니다. 기존 계정으로 로그인해 주세요.'); return } set('phone', signupTrimmed(result.normalizedValue) || formatMobilePhone(form.phone)); set('phoneVerificationToken', ''); setPhoneChecked(true) } catch (reason) { setError(message(reason)) } }
  const checkLogin = async () => { setError(''); setLoginChecked(false); const loginId = signupTrimmed(form?.loginId); if (!loginId) { setError('아이디를 입력해 주세요.'); return } try { const result = await api.getLoginAvailability(loginId); if (!result.available) { setError('이미 사용 중인 아이디입니다.'); return } set('loginId', signupTrimmed(result.normalizedValue) || loginId); setLoginChecked(true) } catch (reason) { setError(message(reason)) } }
  const verifyBusinessNumber = async () => {
    const normalized = form.businessRegistrationNumber.replace(/\D/g, '')
    setBusinessValidation('')
    setVerifiedBusinessNumber('')
    if (!validBusinessNumber(normalized)) { setBusinessValidation('올바른 사업자등록번호 10자리를 입력해 주세요.'); return }
    try {
      const result = await api.getBusinessRegistrationAvailability(normalized)
      if (!result.valid) { setBusinessValidation('유효하지 않은 사업자등록번호입니다.'); return }
      if (!result.available) { setBusinessValidation('이미 등록된 사업자등록번호입니다.'); return }
      set('businessRegistrationNumber', formatBusinessNumber(result.normalizedValue))
      setVerifiedBusinessNumber(result.normalizedValue)
      setBusinessValidation('사업자등록번호 형식과 중복 가입 여부를 확인했습니다.')
    } catch (reason) { setBusinessValidation(message(reason)) }
  }
  const submit = async () => { setError(''); if (!identityDocument) { setError('사업자등록증을 업로드해야 전문가 회원가입을 진행할 수 있습니다.'); return } try { const consentValues = legal.map(item => ({ legalDocumentVersionId: item.versionId, agreed: !!consents[item.versionId] })); const profile = { providerTypeCode: 'BUSINESS', businessName: form.businessName, representativeName: form.representativeName, contactName: form.contactName, businessRegistrationNumber: form.businessRegistrationNumber || null, businessAddress: form.businessAddress || null, businessTypeText: form.businessTypeText || null, businessItemText: form.businessItemText || null, introduction: form.introduction || null, consents: consentValues }; if (existing) await api.addProviderRole(profile); else await api.registerProvider({ ...form, email: form.email.trim(), phone: form.phone || null, ...profile }); try { const types=await api.getProviderDocumentTypes(); const type=types.find(value=>value.code==='BUSINESS_REGISTRATION_CERTIFICATE')??types.find(value=>value.name.includes('사업자')); if(!type)throw new Error('사업자등록증 문서 유형이 설정되지 않았습니다.'); await api.uploadProviderDocument(type.id,identityDocument,form.businessRegistrationNumber) } catch(reason) { setError(`계정과 전문가 정보는 등록되었지만 사업자등록증은 저장하지 못했습니다. 증빙 관리에서 다시 제출해 주세요. (${message(reason)})`); return } setDone(true) } catch (reason) { setError(message(reason)) } }
  const continueRegistration = async () => { setError(''); setContinuing(true); try { await refresh(); navigate('/provider/onboarding') } catch (reason) { setError(message(reason)); setContinuing(false) } }
  if (done) return <div className="signupShell"><header><BrandLogo /></header><main className="signupMain"><section className="signupCard"><p className="accountEyebrow">등록 접수 완료</p><h1>본사 심사를 준비해 주세요</h1><p>고객·전문가 통합 계정이 생성되었습니다. 전문가 홍보 프로필·서비스 분야·활동 지역을 등록하세요.</p>{error && <p className="signupError" role="alert">{error}</p>}<button className="accountPrimary" disabled={continuing} onClick={() => void continueRegistration()}>{continuing ? '이동 중…' : '등록하러 가기'}</button></section></main><ServiceFooter variant="provider" /></div>
  const password = signupText(form?.password)
  const passwordConfirmation = signupText(form?.passwordConfirmation)
  const passwordValid = isAccountPasswordValid(password)
  const passwordMismatch = !!passwordConfirmation && password !== passwordConfirmation
  const workflowStep = existing ? step : step - 1
  const steps = existing ? ['계정확인', '기본정보', '등록정보', '약관·동의', '최종확인'] : ['휴대전화 확인', '계정정보', '기본정보', '등록정보', '약관·동의', '최종확인']
  const maxStep = steps.length
  const nextValidationMessage = () => {
    if (!existing && step === 1) {
      if (!/^010-\d{4}-\d{4}$/.test(signupText(form?.phone))) return '휴대전화 번호를 010-1234-5678 형식으로 입력해 주세요.'
      if (!phoneChecked) return '휴대전화 번호 중복확인을 완료해 주세요.'
    }
    if (!existing && workflowStep === 1) {
      if (!signupTrimmed(form?.loginId)) return '아이디를 입력해 주세요.'
      if (!loginChecked) return '아이디 중복확인을 완료해 주세요.'
      if (!passwordValid) return accountPasswordGuidance
      if (!passwordConfirmation || password !== passwordConfirmation) return '비밀번호를 다시 확인하세요.'
      if (!signupTrimmed(form?.email)) return '이메일을 입력해 주세요.'
      if (!validEmail(form.email)) return '이메일 아이디와 도메인을 모두 확인해 주세요.'
    }
    if (workflowStep === 2) {
      if (!signupTrimmed(form?.businessName)) return '전문가 표시명 또는 상호를 입력해 주세요.'
      if (!signupTrimmed(form?.representativeName)) return '대표자명 또는 본인 성명을 입력해 주세요.'
      if (!signupTrimmed(form?.contactName)) return '현장 연락 담당자를 입력해 주세요.'
      if (!identityDocument) return '사업자등록증을 업로드해 주세요.'
    }
    if (workflowStep === 3) {
      if (!validBusinessNumber(form.businessRegistrationNumber) || verifiedBusinessNumber !== form.businessRegistrationNumber.replace(/\D/g, '')) return '사업자등록번호를 확인하고 번호 검증을 완료해 주세요.'
      if (!sidoId || !sigunguId) return '사업장 시·도와 시·군·구를 선택해 주세요.'
      if (!signupTrimmed(addressDetail)) return '사업장 상세주소를 입력해 주세요.'
      if (!signupTrimmed(form?.businessTypeText) || !signupTrimmed(form?.businessItemText)) return '사업자등록증의 업태와 업종을 입력해 주세요.'
    }
    if (workflowStep === 4) {
      if (legal.length === 0) return '전문가 등록 약관을 불러오지 못했습니다. 잠시 후 다시 시도해 주세요.'
      if (!legal.filter(x => x.requirementCode === 'REQUIRED').every(x => consents[x.versionId])) return '필수 약관에 모두 동의해 주세요.'
    }
    return ''
  }
  const goNext = () => {
    try {
      const validationMessage = nextValidationMessage()
      if (validationMessage) { setError(validationMessage); return }
      setError('')
      setStep(value => Math.min(value + 1, maxStep))
    } catch (reason) {
      console.error('Provider signup step validation failed.', reason)
      setError('입력 내용을 확인한 뒤 다시 시도해 주세요.')
    }
  }
  return <div className="signupShell"><header><BrandLogo /><button onClick={() => navigate('/provider/start')}>등록 안내</button></header><main className="signupMain providerSignupMain"><ol className="signupSteps">{steps.map((label, index) => <li className={step >= index + 1 ? 'isActive' : ''} key={label}><span>{index + 1}</span>{label}</li>)}</ol><section className="signupCard providerSignupCard"><p className="accountEyebrow">전문가 등록 {step}/{maxStep}</p><h1>{existing ? '기존 고객 계정에 전문가 역할 추가' : '수달 전문가 회원가입'}</h1>
    {!existing && step === 1 && <div className="providerFormGrid"><label className="wide">휴대전화<input value={form.phone} onChange={e => { set('phone', formatMobilePhone(e.target.value)); set('phoneVerificationToken', ''); setPhoneChecked(false) }} placeholder="010-1234-5678" inputMode="tel" autoComplete="tel" /></label><div className="wide"><button className="providerSecondary" type="button" disabled={!/^010-\d{4}-\d{4}$/.test(form.phone)} onClick={() => void checkPhone()}>휴대전화 번호 중복확인</button><div className="notIntegratedNotice"><strong>휴대전화 번호 중복확인</strong><span>{phoneChecked ? '가입할 수 있는 휴대전화 번호입니다.' : '본인인증 연동 없이 이미 등록된 번호인지 확인합니다. 중복확인을 완료하면 다음 단계로 진행할 수 있습니다.'}</span></div></div></div>}
    {workflowStep === 1 && (existing ? <div className="notIntegratedNotice"><strong>{user?.loginId}</strong><span>기존 계정과 고객 역할은 유지하고 전문가 역할을 추가합니다.</span></div> : <div className="providerFormGrid providerAccountGrid"><label className="wide">휴대전화<input value={form.phone} disabled /></label><label className="wide">아이디<div className="providerInlineField"><input value={form.loginId} onChange={e => { set('loginId', e.target.value); setLoginChecked(false) }} autoComplete="username" /><button type="button" onClick={() => void checkLogin()}>중복확인</button></div>{loginChecked && <small className="validationOk">사용할 수 있는 아이디입니다.</small>}</label><label>비밀번호<PasswordInput value={form.password} onChange={value => set('password', value)} ariaLabel="비밀번호" /><small>{accountPasswordGuidance}</small>{form.password && !passwordValid && <small className="validationError" role="alert">{accountPasswordGuidance}</small>}</label><label>비밀번호 확인<PasswordInput value={form.passwordConfirmation} onChange={value => set('passwordConfirmation', value)} ariaLabel="비밀번호 확인" />{passwordMismatch && <small className="validationError" role="alert">비밀번호를 다시 확인하세요.</small>}</label><label className="wide">연락 이메일<EmailAddressInput value={form.email} onChange={value=>set('email',value)} /><small>가입 후 본사 연락과 고객 공개 이메일의 기본값으로 저장됩니다.</small></label></div>)}
    {workflowStep === 2 && <div><div className="providerGuidance"><strong>사업자 전문가 전용 회원가입</strong><p>개인사업자 또는 법인사업자만 전문가로 가입할 수 있습니다. 사업자등록증을 업로드하지 않으면 다음 단계로 진행할 수 없습니다.</p></div><div className="providerDocumentScan"><strong>사업자등록증 업로드 *</strong><p>상호, 대표자명, 사업자등록번호, 사업장 주소, 업태와 업종을 확인합니다.</p><input ref={documentInput} type="file" accept=".pdf,.jpg,.jpeg,.png" onChange={e => setIdentityDocument(e.target.files?.[0] ?? null)} />{identityDocument && <div className="providerSelectedFile"><strong>{identityDocument.name}</strong><span>선택한 파일은 가입 완료 시 안전하게 업로드됩니다.</span><button type="button" className="providerDocumentDelete" onClick={() => { setIdentityDocument(null); if (documentInput.current) documentInput.current.value = '' }}>업로드 삭제</button></div>}</div><div className="providerFormGrid"><label>전문가 표시명/상호<input value={form.businessName} onChange={e => set('businessName', e.target.value)} placeholder="사업자등록증의 상호" /></label><label>대표자명<input value={form.representativeName} onChange={e => set('representativeName', e.target.value)} /></label><label>현장 연락 담당자<input value={form.contactName} onChange={e => set('contactName', e.target.value)} placeholder="본인이면 대표자명" /></label><label className="wide">전문가 소개<select defaultValue="" onChange={e => { const selected = providerIntroductionExamples.find(item => item.label === e.target.value); if (selected) set('introduction', selected.value) }}><option value="">예시를 선택하거나 직접 입력하세요</option>{providerIntroductionExamples.map(item => <option key={item.label} value={item.label}>{item.label}</option>)}</select><textarea value={form.introduction} onChange={e => set('introduction', e.target.value)} placeholder="서비스 경력, 전문 분야, 작업 방식, 고객 응대와 A/S 원칙을 자세히 적어 주세요." /><small>예시를 선택한 뒤 실제 경력과 서비스에 맞게 수정하거나 직접 입력해 주세요. 등록 후 전문가 기본정보에서 홍보용 소개와 로고·사진·연락처·웹사이트를 추가할 수 있습니다.</small></label></div></div>}
    {workflowStep === 3 && (
      <div className="providerFormGrid">
        <>
            <label>사업자등록번호<div className="providerInlineField"><input inputMode="numeric" value={form.businessRegistrationNumber} onChange={e => { set('businessRegistrationNumber', formatBusinessNumber(e.target.value)); setBusinessValidation(''); setVerifiedBusinessNumber('') }} placeholder="000-00-00000" /><button type="button" onClick={() => void verifyBusinessNumber()}>번호 검증</button></div>{businessValidation && <small className={verifiedBusinessNumber ? 'validationOk' : 'validationError'}>{businessValidation}</small>}</label>
            <label>사업장 시·도<select value={sidoId} onChange={e => setSidoId(e.target.value)}><option value="">선택</option>{sidos.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></label>
            <label>사업장 시·군·구<select value={sigunguId} onChange={e => setSigunguId(e.target.value)} disabled={!sidoId}><option value="">선택</option>{sigungu.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></label>
            {areaError && <div className="notIntegratedNotice wide" role="alert"><strong>행정구역 목록 오류</strong><span>{areaError}</span><button type="button" className="providerDocumentDelete" onClick={() => void (sidoId ? loadSigungu(sidoId) : loadSidos())}>다시 불러오기</button></div>}
            <label className="wide">사업장 상세주소<input value={addressDetail} onChange={e => setAddressDetail(e.target.value)} placeholder="도로명·건물번호와 상세주소" /></label>
            <label>업태<input value={form.businessTypeText} onChange={e => set('businessTypeText', e.target.value)} /></label>
            <label>업종<input value={form.businessItemText} onChange={e => set('businessItemText', e.target.value)} /></label>
        </>
        <p className="wide">제출 정보는 본사 심사 후 승인됩니다. 자동 판독 결과가 제공되더라도 전문가가 확인한 값만 등록 정보로 사용합니다.</p>
      </div>
    )}
    {workflowStep === 4 && <div><div className="notIntegratedNotice"><strong>전문가 등록 약관 및 동의</strong><span>전문가 활동, 개인정보 처리, 선택적 마케팅 수신에 관한 문서입니다. 필수 문서는 동의해야 등록할 수 있습니다.</span></div>{legal.length === 0 ? <p>현재 적용할 전문가 전용 약관을 불러오지 못했습니다. 잠시 후 다시 시도해 주세요.</p> : legal.map(item => <article className="providerLegalCard" key={item.versionId}><label><span><strong>{item.title}</strong><small>{item.requirementCode === 'REQUIRED' ? '필수' : '선택'}</small></span><input type="checkbox" checked={!!consents[item.versionId]} onChange={e => setConsents(current => ({ ...current, [item.versionId]: e.target.checked }))} /></label><details><summary>내용 보기</summary><p>{item.content}</p></details></article>)}</div>}
    {workflowStep === 5 && <div className="providerReview"><div className="providerReviewGrid">
      <article><span className="providerReviewIcon" aria-hidden="true">▣</span><div><strong>등록 유형</strong><p>사업자 전문가</p></div></article>
      <article><span className="providerReviewIcon" aria-hidden="true">◆</span><div><strong>전문가명</strong><p>{form.businessName}</p></div></article>
      <article><span className="providerReviewIcon" aria-hidden="true">●</span><div><strong>대표자·담당자</strong><p>{form.representativeName} / {form.contactName}</p></div></article>
      <article><span className="providerReviewIcon" aria-hidden="true">▤</span><div><strong>등록 증빙</strong><p>{identityDocument?.name ?? '등록 후 비공개 증빙 관리에서 제출'}</p></div></article>
      <article><span className="providerReviewIcon" aria-hidden="true">↔</span><div><strong>통합 역할</strong><p>고객 + 전문가</p></div></article>
      <article><span className="providerReviewIcon" aria-hidden="true">✓</span><div><strong>등록 후 상태</strong><p>본사 심사 대기 / 활동 전</p></div></article>
    </div><aside className="providerNextStep"><span className="providerNextStepIcon" aria-hidden="true">→</span><div><strong>다음 단계</strong><p>전문가 등록 접수 후 서비스·활동지역·증빙을 등록하고 본사 심사를 진행해 주세요.</p></div></aside></div>}
    {error && <div className="providerAlert error" role="alert">{error}</div>}<div className="providerActions">{step > 1 && <button className="providerSecondary" type="button" onClick={() => { setError(''); setStep(value => value - 1) }}>이전</button>}{step < maxStep ? <button className="providerPrimary" type="button" onClick={goNext}>다음</button> : <button className="providerPrimary" type="button" onClick={() => void submit()}>전문가 등록 접수</button>}</div></section></main><ServiceFooter variant="provider" /></div>
}

export function ProviderHomePage() {
  const [dashboard, setDashboard] = useState<ProviderDashboard | null>(null), [profile, setProfile] = useState<ProviderProfile | null>(null), [error, setError] = useState('')
  useEffect(() => { Promise.all([api.getProviderDashboard(), api.getProviderProfile()]).then(([d, p]) => { setDashboard(d); setProfile(p) }).catch(reason => setError(message(reason))) }, [])
  const progress = dashboard ? [!!profile?.representativeName, dashboard.registeredServiceCount > 0, dashboard.activeAreaCount > 0, dashboard.requiredEvidenceCount === 0 || dashboard.submittedEvidenceCount === dashboard.requiredEvidenceCount, dashboard.approvalStatus === 'APPROVED'] : []
  return <AuthenticatedLayout><section className="providerHero"><div><p>수달 전문가</p><h1>{profile?.businessName ?? '전문가 홈'}</h1><span>서비스 등록부터 본사 승인까지 실제 상태를 확인합니다. 승인 전에는 견적과 고객 개인정보 접근이 제한됩니다.</span></div><span className="providerStatusPill">{approvalLabel[dashboard?.approvalStatus ?? 'PENDING']}</span></section>{error && <div className="providerAlert error">{error}</div>}<ol className="providerSteps">{['기본정보', '서비스', '활동지역', '증빙', '본사 승인', '이용 시작'].map((label, index) => <li className={progress[index] ? 'done' : ''} key={label}>{label}</li>)}</ol><section className="providerGrid"><StatusCard label="등록 서비스" value={`${dashboard?.registeredServiceCount ?? 0}개`} detail={`승인 ${dashboard?.approvedServiceCount ?? 0} · 심사 ${dashboard?.pendingServiceCount ?? 0} · 반려 ${dashboard?.rejectedServiceCount ?? 0}`} path="/provider/services" /><StatusCard label="활동지역" value={`${dashboard?.activeAreaCount ?? 0}개`} detail="서비스별 시·군·구 기준" path="/provider/areas" /><StatusCard label="필수 증빙" value={`${dashboard?.submittedEvidenceCount ?? 0}/${dashboard?.requiredEvidenceCount ?? 0}`} detail={`확인 완료 ${dashboard?.approvedEvidenceCount ?? 0}`} path="/provider/documents" /><StatusCard label="신뢰도" value={profile?.trustScore == null ? '평가 전' : `${profile.trustScore}`} detail={friendlyStatus(profile?.trustDisplayStatus)} path="/provider/approval" /></section><section className="providerPanel"><h2>다음 해야 할 일</h2>{dashboard?.nextActions.length ? <ul>{dashboard.nextActions.map(item => <li key={item}>{item}</li>)}</ul> : <p>현재 온보딩 준비가 완료되었습니다.</p>}{dashboard?.rejectionReason && <div className="providerAlert error">공개 가능한 반려 사유: {dashboard.rejectionReason}</div>}</section></AuthenticatedLayout>
}
function StatusCard({ label, value, detail, path }: { label: string; value: string; detail: string; path: string }) { return <button className="providerCard" onClick={() => navigate(path)}><small>{label}</small><strong>{value}</strong><p>{detail}</p></button> }

function ProviderIntroductionEditor({value,onChange}:{value:string;onChange:(value:string)=>void}){
  const[mode,setMode]=useState<'VISUAL'|'HTML'>('VISUAL'),[preview,setPreview]=useState(false),[limitError,setLimitError]=useState('')
  const editor=useRef<HTMLDivElement>(null)
  useEffect(()=>{if(mode==='VISUAL'&&editor.current&&editor.current.innerHTML!==value)editor.current.innerHTML=value},[mode,value])
  const commit=(candidate:string)=>{const bytes=utf8ByteLength(candidate);if(bytes>publicIntroductionMaxBytes){setLimitError(`전문가 상세 소개는 최대 ${publicIntroductionMaxBytes.toLocaleString()}바이트까지 입력할 수 있습니다.`);if(editor.current&&editor.current.innerHTML!==value)editor.current.innerHTML=value;return false}setLimitError('');onChange(candidate);return true}
  const command=(name:string,argument?:string)=>{editor.current?.focus();document.execCommand(name,false,argument);if(editor.current)commit(editor.current.innerHTML)}
  const addLink=async ()=>{const address=await soodalPrompt('연결할 HTTPS 주소를 입력해 주세요.','https://');if(address&&/^https:\/\//i.test(address))command('createLink',address)}
  return <section className="providerIntroEditor wide"><div className="providerIntroHeading"><div><strong>전문가 상세 소개</strong><small>굵게·목록·링크 등 제한된 HTML 서식을 사용할 수 있습니다.</small></div><div><button type="button" className={mode==='VISUAL'?'active':''} onClick={()=>setMode('VISUAL')}>일반 편집</button><button type="button" className={mode==='HTML'?'active':''} onClick={()=>setMode('HTML')}>HTML 직접 입력</button><button type="button" className={preview?'active':''} onClick={()=>setPreview(current=>!current)}>고객 화면 미리보기</button></div></div>{mode==='VISUAL'?<><div className="providerIntroToolbar" aria-label="소개 서식 도구"><button type="button" onClick={()=>command('bold')}><b>굵게</b></button><button type="button" onClick={()=>command('italic')}><i>기울임</i></button><button type="button" onClick={()=>command('formatBlock','h3')}>소제목</button><button type="button" onClick={()=>command('insertUnorderedList')}>목록</button><button type="button" onClick={addLink}>링크</button></div><div ref={editor} className="providerRichEditor" contentEditable suppressContentEditableWarning onInput={event=>commit(event.currentTarget.innerHTML)}/></>:<textarea className="providerHtmlSource" value={value} onChange={event=>commit(event.target.value)} spellCheck={false}/>}<div className="providerIntroByteStatus" aria-live="polite"><span className={limitError?'overLimit':''}>{utf8ByteLength(value).toLocaleString()} / {publicIntroductionMaxBytes.toLocaleString()}바이트</span><small>한글과 서식 태그를 포함해 서버에 저장되는 용량 기준입니다.</small></div>{limitError&&<p className="validationError" role="alert">{limitError}</p>}<p className="providerHtmlNotice">허용: 문단, 줄바꿈, 제목, 굵게, 기울임, 목록, HTTPS 링크 · 금지: 스크립트, iframe, 스타일, 이벤트 속성</p>{preview&&<div className="providerIntroPreview"><span>고객에게 보이는 미리보기</span>{value?<div dangerouslySetInnerHTML={{__html:sanitizeIntro(value)}}/>:<p>등록된 소개 내용이 없습니다.</p>}</div>}</section>
}

function EmailAddressInput({value,onChange}:{value:string;onChange:(value:string)=>void}) {
  const split = value.lastIndexOf('@')
  const local = split >= 0 ? value.slice(0, split) : value
  const domain = split >= 0 ? value.slice(split + 1) : ''
  const knownDomain = emailDomains.includes(domain as typeof emailDomains[number])
  const [customDomain, setCustomDomain] = useState(() => !!domain && !knownDomain)
  useEffect(() => { if (domain && !emailDomains.includes(domain as typeof emailDomains[number])) setCustomDomain(true) }, [domain])
  const changeLocal = (next: string) => onChange(domain ? `${next}@${domain}` : next)
  const chooseDomain = (next: string) => {
    if (next === 'CUSTOM') { setCustomDomain(true); onChange(`${local}@`); return }
    setCustomDomain(false)
    onChange(next ? `${local}@${next}` : local)
  }
  return <div className="providerEmailAddress"><input aria-label="이메일 아이디" value={local} onChange={event=>changeLocal(event.target.value.replace(/@/g,''))} placeholder="이메일 아이디" /><span aria-hidden="true">@</span><select aria-label="이메일 도메인" value={customDomain?'CUSTOM':domain} onChange={event=>chooseDomain(event.target.value)}><option value="">도메인 선택</option>{emailDomains.map(item=><option key={item} value={item}>{item}</option>)}<option value="CUSTOM">직접 입력</option></select>{customDomain&&<input aria-label="직접 입력 도메인" value={domain} onChange={event=>onChange(`${local}@${event.target.value.replace(/@/g,'')}`)} placeholder="회사 도메인" />}</div>
}

function sanitizeIntro(value:string){const parsed=new DOMParser().parseFromString(value,'text/html');const allowed=new Set(['P','BR','STRONG','B','EM','I','UL','OL','LI','H2','H3','A']);parsed.body.querySelectorAll('*').forEach(element=>{if(!allowed.has(element.tagName)){element.replaceWith(...Array.from(element.childNodes));return}for(const attribute of Array.from(element.attributes)){if(attribute.name.toLowerCase()==='href'&&element.tagName==='A'&&/^https:\/\//i.test(attribute.value)){element.setAttribute('target','_blank');element.setAttribute('rel','noopener noreferrer')}else element.removeAttribute(attribute.name)}});return parsed.body.innerHTML}

export function ProviderProfilePage() {
  const [profile, setProfile] = useState<ProviderProfile | null>(null), [error, setError] = useState(''), [saved, setSaved] = useState('')
  const [step, setStep] = useState(1), [saving, setSaving] = useState(false), [showServiceGuide, setShowServiceGuide] = useState(false)
  const [logoFile, setLogoFile] = useState<File | null>(null), [photoFiles, setPhotoFiles] = useState<File[]>([])
  const [photoAppendMode, setPhotoAppendMode] = useState(false)
  const [logoPreview, setLogoPreview] = useState(''), [photoPreviews, setPhotoPreviews] = useState<string[]>([])
  const logoInput = useRef<HTMLInputElement>(null), photosInput = useRef<HTMLInputElement>(null)
  const publicIntroductionHtml = profile?.publicIntroductionHtml ?? ''
  useEffect(() => { api.getProviderProfile().then(value => { const email = value.email ?? value.publicEmail ?? ''; const publicPhone = value.publicPhone?.trim() ? value.publicPhone : value.phone ?? ''; const publicAddress = value.publicAddress?.trim() ? value.publicAddress : value.businessAddress ?? ''; setProfile({ ...value, email, publicEmail: email, publicPhone, publicAddress }) }).catch(reason => setError(message(reason))) }, [])
  useEffect(() => { if (!logoFile) { setLogoPreview(''); return }; const url = URL.createObjectURL(logoFile); setLogoPreview(url); return () => URL.revokeObjectURL(url) }, [logoFile])
  useEffect(() => { const urls = photoFiles.map(file => URL.createObjectURL(file)); setPhotoPreviews(urls); return () => urls.forEach(url => URL.revokeObjectURL(url)) }, [photoFiles])
  const update = <K extends keyof ProviderProfile>(key: K, value: ProviderProfile[K]) => setProfile(current => current ? { ...current, [key]: value } : current)
  useEffect(() => { setProfile(current => { if (!current) return current; const formatted = formatMobilePhone(current.publicPhone ?? ''); return formatted === (current.publicPhone ?? '') ? current : { ...current, publicPhone: formatted } }) }, [profile?.publicPhone])
  const updateEmail = (value:string) => setProfile(current => current ? { ...current, email:value, publicEmail:value } : current)
  const validateImage = (file: File) => { if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) return 'JPG, PNG, WEBP 이미지만 등록할 수 있습니다.'; if (file.size > 5 * 1024 * 1024) return '이미지는 파일당 5MB 이하만 등록할 수 있습니다.'; return '' }
  const selectLogo = (file: File | null) => { if (!file) return; const validation = validateImage(file); if (validation) { setError(validation); if (logoInput.current) logoInput.current.value = ''; return }; setError(''); setSaved(''); setLogoFile(file) }
  const selectPhotos = (files: File[]) => { if (files.length > 5) { setError('홍보 사진은 최대 5개까지 등록할 수 있습니다.'); if (photosInput.current) photosInput.current.value = ''; return }; const validation = files.map(validateImage).find(Boolean); if (validation) { setError(validation); if (photosInput.current) photosInput.current.value = ''; return }; setError(''); setSaved(''); setPhotoAppendMode(false); setPhotoFiles(files) }
  const normalizedProfile = () => profile ? { ...profile, email: profile.email?.trim() ?? '', publicEmail: profile.email?.trim() ?? '', publicIntroductionHtml: sanitizeIntro(profile.publicIntroductionHtml ?? ''), publicBlogUrl: normalizePublicUrl(profile.publicBlogUrl), publicWebsiteUrl: normalizePublicUrl(profile.publicWebsiteUrl) } : null
  const saveProfile = async () => { const value=normalizedProfile(); if(!value)throw new Error('전문가 정보를 불러오지 못했습니다.'); const current=await api.updateProviderProfile(value); setProfile(current); return current }
  const next = async () => { if (!profile || saving) return; if (step === 1 && (!profile.businessName.trim() || !profile.representativeName?.trim() || !profile.contactName?.trim())) { setError('전문가 표시명, 대표자·본인 성명, 담당자명을 모두 입력해 주세요.'); return }; if(step===1&&!profile.email?.trim()){setError('이메일을 입력해 주세요.');return}; if(step===1&&!validEmail(profile.email??'')){setError('이메일 아이디와 도메인을 모두 확인해 주세요.');return}; if(step===1&&(profile.introduction?.trim().length??0)<50){setError('최종 심사를 위해 심사용 소개를 50자 이상 입력해 주세요.');return}; if(step===2&&utf8ByteLength(profile.publicIntroductionHtml??'')>publicIntroductionMaxBytes){setError(`전문가 상세 소개는 최대 ${publicIntroductionMaxBytes.toLocaleString()}바이트까지 입력할 수 있습니다.`);return}; setError(''); setSaved(''); setSaving(true); try{await saveProfile();setSaved(`${step===1?'기본정보와 이메일':'고객 공개정보'}가 저장되었습니다.`);setStep(value => Math.min(3, value + 1));window.scrollTo({ top: 0, behavior: 'smooth' })}catch(reason){setError(message(reason))}finally{setSaving(false)} }
  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!profile || step !== 3) return
    setError(''); setSaved(''); setSaving(true)
    try {
      if (logoFile || photoFiles.length) await api.getProviderPromotionStorageStatus()
      if (!profile.email?.trim()) throw new Error('이메일을 입력해 주세요.')
      if (!validEmail(profile.email)) throw new Error('이메일 아이디와 도메인을 모두 확인해 주세요.')
      if (utf8ByteLength(profile.publicIntroductionHtml ?? '') > publicIntroductionMaxBytes) throw new Error(`전문가 상세 소개는 최대 ${publicIntroductionMaxBytes.toLocaleString()}바이트까지 입력할 수 있습니다.`)
      let current = await saveProfile()
      setProfile(current)
      if (logoFile) {
        try {
          current = await api.uploadProviderPromotionLogo(logoFile)
          setProfile(current); setLogoFile(null)
          if (logoInput.current) logoInput.current.value = ''
        } catch (reason) {
          setError(`기본정보는 저장되었지만 로고 이미지를 저장하지 못했습니다. 선택한 로고를 확인한 후 다시 저장해 주세요. (${message(reason)})`)
          return
        }
      }
      if (photoFiles.length) {
        try {
          let remaining = [...photoFiles], append = photoAppendMode
          while (remaining.length) {
            current = await api.uploadProviderPromotionPhoto(remaining[0], !append)
            setProfile(current); append = true; setPhotoAppendMode(true)
            remaining = remaining.slice(1); setPhotoFiles(remaining)
          }
          setPhotoAppendMode(false)
          if (photosInput.current) photosInput.current.value = ''
        } catch (reason) {
          setError(`기본정보와 로고는 저장되었지만 홍보 사진을 저장하지 못했습니다. 선택한 사진을 확인한 후 다시 저장해 주세요. (${message(reason)})`)
          return
        }
      }
      setSaved('기본정보와 홍보 프로필이 저장되었습니다.')
      setShowServiceGuide(true)
    } catch (reason) {
      if (reason instanceof api.ProviderApiError && reason.status === 409) {
        try { setProfile(await api.getProviderProfile()) } catch { /* keep the current form when refresh also fails */ }
      }
      setError(message(reason))
    } finally { setSaving(false) }
  }
  const steps = ['기본정보', '고객 공개정보', '이미지·확인']
  const currentPhotos = photoPreviews.length ? photoPreviews : profile?.publicPhotoUrls ?? []
  return <AuthenticatedLayout><section className="providerHero"><div><p>나의 수달</p><h1>전문가 기본정보·홍보 프로필</h1><span>{profile?.providerType==='BUSINESS'?'사업자 전문가':'개인 전문가'} 기준에 맞는 항목만 입력합니다.</span></div></section>{error && !profile && <div className="providerAlert error">{error}</div>}{profile && <><ol className="providerProfileSteps" aria-label="프로필 입력 단계">{steps.map((label, index) => <li className={step === index + 1 ? 'current' : step > index + 1 ? 'done' : ''} key={label}><span>{step > index + 1 ? '✓' : index + 1}</span><b>{label}</b></li>)}</ol><form className="providerPanel providerProfileWizard" onSubmit={event => void submit(event)}><div className="providerProfileStepHeading"><span>{step}단계 / 전체 3단계</span><h2>{steps[step - 1]}</h2></div>{step === 1 && <><div className="providerGuidance"><strong>{profile.providerType==='BUSINESS'?'사업자 전문가':'개인 전문가'} 본사 확인용 정보</strong>가입 시 입력한 값을 불러와 표시합니다.</div><div className="providerFormGrid providerPrivateProfileGrid"><label>전문가 표시명{profile.providerType==='BUSINESS'?'/상호':''}<input required value={profile.businessName} onChange={e => update('businessName', e.target.value)} /></label><label>{profile.providerType==='BUSINESS'?'대표자명':'본인 성명'}<input required value={profile.representativeName ?? ''} onChange={e => update('representativeName', e.target.value)} /></label><label>담당자명<input required value={profile.contactName ?? ''} onChange={e => update('contactName', e.target.value)} /></label><label>가입 휴대전화<input readOnly value={profile.phone ?? ''} /></label><label>연락·공개 이메일<EmailAddressInput value={profile.email ?? ''} onChange={updateEmail} /><small>본사 연락과 고객 공개에 같은 이메일 한 개를 사용합니다.</small></label>{profile.providerType==='BUSINESS'&&<><label className="providerBusinessNumberField">사업자등록번호<input readOnly value={profile.businessRegistrationNumber ?? '미등록'} /><small>가입 시 검증한 번호입니다. 다시 입력하거나 검증할 필요가 없습니다.</small></label><label className="wide">사업장 주소<input value={profile.businessAddress ?? ''} onChange={e => update('businessAddress', e.target.value)} /></label><label>업태<input value={profile.businessTypeText ?? ''} onChange={e => update('businessTypeText', e.target.value)} /></label><label>업종<input value={profile.businessItemText ?? ''} onChange={e => update('businessItemText', e.target.value)} /></label></>}<label className="wide">본사 승인 검토용 소개<textarea required minLength={50} maxLength={1000} value={profile.introduction ?? ''} onChange={e => update('introduction', e.target.value)} /></label></div></>}{step === 2 && <div className="providerFormGrid"><ProviderIntroductionEditor value={publicIntroductionHtml} onChange={value=>update('publicIntroductionHtml',value)}/><label>공개 전화번호<input value={profile.publicPhone ?? ''} onChange={e => update('publicPhone', e.target.value)} /></label><div className="providerEmailSummary"><strong>연락·공개 이메일</strong><span>{profile.email || '등록하지 않음'}</span><small>기본정보에서 한 번만 입력하며 고객 공개에도 같은 주소를 사용합니다.</small></div><label className="wide">공개 주소<input value={profile.publicAddress ?? ''} onChange={e => update('publicAddress', e.target.value)} /></label><label>블로그 주소<input type="url" value={profile.publicBlogUrl ?? ''} onChange={e => update('publicBlogUrl', e.target.value)} /></label><label>홈페이지 주소<input type="url" value={profile.publicWebsiteUrl ?? ''} onChange={e => update('publicWebsiteUrl', e.target.value)} /></label></div>}{step === 3 && <div className="providerUploadGrid"><section className="providerImageUploadCard"><h3>로고 이미지</h3><input ref={logoInput} className="providerHiddenFileInput" type="file" accept="image/jpeg,image/png,image/webp" onChange={e => selectLogo(e.target.files?.[0] ?? null)} /><div className="providerFilePicker"><button type="button" className="providerFileSelectButton" onClick={()=>logoInput.current?.click()}>로고 파일 선택</button><span>{logoFile?.name ?? (profile.publicLogoUrl?'기존 로고 이미지':'선택한 파일 없음')}</span></div><div className="providerLogoPreview"><img src={logoPreview?logoPreview:profile.publicLogoUrl?apiUrl(profile.publicLogoUrl):'/brand/soodal-life-mark.png'} alt="로고 미리보기"/></div></section><section className="providerImageUploadCard"><h3>홍보 사진</h3><input ref={photosInput} className="providerHiddenFileInput" type="file" multiple accept="image/jpeg,image/png,image/webp" onChange={e => selectPhotos(Array.from(e.target.files ?? []))} /><div className="providerFilePicker"><button type="button" className="providerFileSelectButton" onClick={()=>photosInput.current?.click()}>홍보사진 파일 선택</button><span>{photoFiles.length?`${photoFiles.length}개 파일 선택`:(profile.publicPhotoUrls.length?`기존 홍보사진 ${profile.publicPhotoUrls.length}장`:'선택한 파일 없음')}</span></div><div className="providerPhotoPreviews">{currentPhotos.map((url, index) => <figure key={`${url}-${index}`}><img src={apiUrl(url)} alt={`홍보 사진 ${index + 1}`} /></figure>)}</div></section></div>}{error && <div className="providerAlert error">{error}</div>}{saved && <div className="providerAlert success">{saved}</div>}<div className="providerActions providerWizardActions">{step > 1 && <button type="button" className="providerSecondary" disabled={saving} onClick={() => setStep(value => value - 1)}>이전</button>}{step < 3 ? <button type="button" className="providerPrimary" onClick={next}>다음 단계</button> : <button className="providerPrimary" disabled={saving}>{saving ? '저장 중…' : '전체 저장'}</button>}</div></form></>}{showServiceGuide&&<div className="providerNextStepBackdrop" role="presentation"><section className="providerNextStepDialog" role="dialog" aria-modal="true" aria-labelledby="provider-next-step-title"><span className="providerNextStepIcon" aria-hidden="true">✓</span><h2 id="provider-next-step-title">서비스 분야와 활동 지역을 설정하세요.</h2><p>기본정보와 홍보 프로필을 모두 저장했습니다. 고객 요청을 받으려면 제공할 서비스 분야를 선택하고 서비스별 활동 지역을 등록해 주세요.</p><button type="button" className="providerPrimary" autoFocus onClick={()=>{setShowServiceGuide(false);navigate('/provider/services')}}>확인하고 서비스 분야로 이동</button></section></div>}</AuthenticatedLayout>
}

export function ProviderDocumentsPage() {
  const [requirements, setRequirements] = useState<ProviderRequirement[]>([]), [documents, setDocuments] = useState<ProviderDocument[]>([]), [types, setTypes] = useState<ProviderDocumentType[]>([]), [typeId, setTypeId] = useState(''), [file, setFile] = useState<File | null>(null), [error, setError] = useState(''), [saved, setSaved] = useState('')
  const load = useCallback(() => Promise.all([api.getProviderRequirements(), api.getProviderDocuments(), api.getProviderDocumentTypes()]).then(([r, d, t]) => { setRequirements(r); setDocuments(d); setTypes(t); setTypeId(current => current || t[0]?.id || '') }), [])
  useEffect(() => { load().catch(reason => setError(message(reason))) }, [load])
  const upload = async () => { if (!file || !typeId) return; setError(''); try { await api.uploadProviderDocument(typeId, file); await load(); setSaved('증빙이 안전하게 등록되었습니다.'); setFile(null) } catch (reason) { setError(message(reason)) } }
  const link = async (verificationId: string, documentId: string) => { try { await api.linkProviderEvidence(verificationId, documentId); await load() } catch (reason) { setError(message(reason)) } }
  return <AuthenticatedLayout><section className="providerHero"><div><p>증빙 관리</p><h1>증빙·자격 관리</h1><span>서비스별 정책에 정의된 증빙만 제출합니다. 원본 파일은 본인과 권한 있는 관리자만 열 수 있습니다.</span></div></section>{error && <div className="providerAlert error">{error}</div>}{saved && <div className="providerAlert success">{saved}</div>}<section className="providerPanel"><h2>증빙 파일 등록</h2><div className="providerFormGrid"><label>증빙 유형<select value={typeId} onChange={e => setTypeId(e.target.value)}>{types.map(item => <option value={item.id} key={item.id}>{item.name}</option>)}</select></label><label>PDF/JPG/PNG, 최대 10MB<input type="file" accept=".pdf,.jpg,.jpeg,.png" onChange={e => setFile(e.target.files?.[0] ?? null)} /></label></div><p className="providerSafetyNotice">파일 안전 검사는 외부 검사 서비스 연결 후 자동 적용됩니다. 현재는 허용된 파일 형식과 용량을 먼저 확인합니다.</p><div className="providerActions"><button className="providerPrimary" disabled={!file || !typeId} onClick={() => void upload()}>비공개 증빙 업로드</button></div></section><section className="providerPanel"><h2>서비스별 필수 요건</h2>{requirements.length === 0 ? <p>선택한 서비스에 등록된 요건이 없습니다.</p> : requirements.map(item => { const acceptedCodes = new Set(item.acceptedEvidenceTypes.map(x => x.code)); const available = documents.filter(doc => acceptedCodes.has(doc.documentTypeCode)); return <article className="providerRequirement" key={item.verificationId}><div><h3>{item.requirementName} {item.isRequired && <span className="providerStatusPill">필수</span>}</h3><p>{item.categoryPath}</p><p>상태: {friendlyStatus(item.verificationStatus)}{item.rejectionReason ? ` · ${item.rejectionReason}` : ''}</p><p>인정 증빙: {item.acceptedEvidenceTypes.map(x => x.name).join(', ') || '정책 확인 필요'}</p>{item.documentName && <p className="providerFileMeta">제출 파일: {item.documentName}</p>}</div><select value={item.documentId ?? ''} onChange={e => e.target.value && void link(item.verificationId, e.target.value)}><option value="">제출 증빙 선택</option>{available.map(doc => <option key={doc.fileId} value={doc.fileId}>{doc.originalFileName} ({friendlyStatus(doc.malwareStatus)})</option>)}</select></article> })}</section><section className="providerPanel"><h2>내 증빙</h2>{documents.map(doc => <article className="providerRequirement" key={doc.fileId}><div><h3>{doc.documentTypeName}</h3><p>{doc.originalFileName} · {(doc.sizeBytes / 1024).toFixed(1)}KB</p><p>서류 {friendlyStatus(doc.verificationStatus)} · 파일 {friendlyStatus(doc.malwareStatus)}</p></div><a className="providerSecondary" href={apiUrl(`/api/v1/providers/me/documents/${doc.fileId}/content`)} target="_blank" rel="noreferrer">본인 원본 열기</a></article>)}</section></AuthenticatedLayout>
}

export function ProviderApprovalPage() {
  const [dashboard, setDashboard] = useState<ProviderDashboard | null>(null), [profile, setProfile] = useState<ProviderProfile | null>(null), [requirements, setRequirements] = useState<ProviderRequirement[]>([]), [services, setServices] = useState<ProviderServiceCategory[]>([]), [note, setNote] = useState(''), [saving, setSaving] = useState(false), [error, setError] = useState(''), [success, setSuccess] = useState('')
  const load = useCallback(() => Promise.all([api.getProviderDashboard(), api.getProviderProfile(), api.getProviderRequirements(), api.getProviderServices()]).then(([d, p, r, s]) => { setDashboard(d); setProfile(p); setRequirements(r); setServices(s) }), [])
  useEffect(() => { load().catch(reason => setError(message(reason))) }, [load])
  const missing = useMemo(() => requirements.filter(x => x.isRequired && !x.documentId), [requirements])
  const resubmitService = async (categoryId:string) => { setSaving(true); setError(''); try { await api.resubmitProviderService(categoryId); await load() } catch(reason) { setError(message(reason)) } finally { setSaving(false) } }
  const applyResubmissionResult = (updated: ProviderDashboard) => {
    setDashboard(updated)
    setProfile(current => current ? { ...current, approvalStatus: updated.approvalStatus, activityStatus: updated.activityStatus, rejectionReason: null } : current)
    setNote('')
    setSuccess('보완 내용이 제출되었습니다. 본사에서 재심사를 진행합니다.')
  }
  const resubmitAll = async () => {
    if (!profile) return
    setSaving(true)
    setError('')
    setSuccess('')
    try {
      const updated = await api.resubmitProviderApproval({ note: note.trim() || null, concurrencyToken: profile.concurrencyToken })
      applyResubmissionResult(updated)
      try { await load() }
      catch { setError('보완 제출은 완료되었지만 최신 화면을 불러오지 못했습니다. 잠시 후 새로고침해 주세요.') }
    } catch(reason) {
      if (reason instanceof api.ProviderApiError && reason.status === 0) {
        try {
          await new Promise(resolve => window.setTimeout(resolve, 500))
          const latest = await api.getProviderDashboard()
          if (latest.approvalStatus === 'PENDING' && !latest.rejectionReason) {
            applyResubmissionResult(latest)
            return
          }
        } catch { /* The original connection error is more useful below. */ }
      }
      setError(message(reason))
    } finally {
      setSaving(false)
    }
  }
  const rejectedServices = services.filter(x => x.approvalStatus === 'REJECTED')
  return <AuthenticatedLayout><section className="providerHero"><div><p>승인 현황</p><h1>{approvalLabel[dashboard?.approvalStatus ?? 'PENDING']}</h1><span>전문가 전체 승인과 서비스별 승인은 서로 구분됩니다.</span></div><span className="providerStatusPill">{activityLabel[dashboard?.activityStatus ?? 'INACTIVE'] ?? '상태 확인 중'}</span></section><section className="providerGrid"><StatusCard label="전체 전문가" value={approvalLabel[dashboard?.approvalStatus ?? 'PENDING'] ?? '상태 확인 중'} detail="본사 전문가 심사" path="/provider/onboarding" /><StatusCard label="서비스 승인" value={`${dashboard?.approvedServiceCount ?? 0}/${dashboard?.registeredServiceCount ?? 0}`} detail="서비스별 별도 심사" path="/provider/services" /><StatusCard label="미제출 증빙" value={`${missing.length}건`} detail="정책상 필수 요건" path="/provider/documents" /><StatusCard label="신뢰도" value={profile?.trustScore == null ? '평가 전' : `${profile.trustScore}`} detail="확정값만 표시" path="/provider/approval" /></section>{success && <div className="providerAlert success">{success}</div>}{error && <div className="providerAlert error">{error}</div>}{dashboard?.rejectionReason && <section className="providerPanel"><h2>전체 심사 반려 사유</h2><div className="providerAlert error">{dashboard.rejectionReason}</div><p>아래 항목을 보완한 뒤 서비스 재심사와 전체 재심사를 순서대로 요청해 주세요.</p><div className="providerActions"><button className="providerSecondary" onClick={()=>navigate('/provider/onboarding')}>기본정보 보완</button><button className="providerSecondary" onClick={()=>navigate('/provider/services')}>서비스 분야 보완</button><button className="providerSecondary" onClick={()=>navigate('/provider/areas')}>활동지역 보완</button><button className="providerSecondary" onClick={()=>navigate('/provider/documents')}>자격·증빙 보완</button></div></section>}{rejectedServices.length > 0 && <section className="providerPanel"><h2>서비스 반려·보완 제출</h2>{rejectedServices.map(service=><article className="providerRequirement" key={service.categoryId}><div><h3>{service.categoryPath}</h3><p>반려 사유: {service.decisionReason || '관리자에게 문의해 주세요.'}</p><p>필수 요건 승인 {service.approvedRequirementCount}/{service.requiredRequirementCount}</p></div><button className="providerPrimary" disabled={saving} onClick={()=>void resubmitService(service.categoryId)}>보완 완료·재심사 요청</button></article>)}</section>}{dashboard?.approvalStatus === 'REJECTED' && <section className="providerPanel providerResubmitPanel"><h2>전체 보완 제출</h2><p>반려된 서비스가 있다면 먼저 각 서비스의 재심사를 요청해야 합니다. 제출하면 관리자 화면에 ‘보완 제출’로 표시됩니다.</p><div className="providerResubmitForm"><label htmlFor="provider-resubmit-note"><strong>보완 내용</strong><span>관리자가 확인할 수 있도록 보완한 항목과 내용을 적어 주세요.</span></label><textarea id="provider-resubmit-note" maxLength={1000} rows={5} value={note} onChange={e=>setNote(e.target.value)} placeholder="예: 자격증 사본을 새로 등록하고 활동지역을 수정했습니다." /><small>{note.length.toLocaleString()} / 1,000자</small><button className="providerPrimary" disabled={saving || rejectedServices.length > 0} onClick={()=>void resubmitAll()}>{saving?'처리 중…':'전체 보완 제출·재심사 요청'}</button></div></section>}<section className="providerPanel"><h2>승인 전 제한</h2><ul><li>신규 고객 요청 수신 제한</li><li>견적 제출 제한</li><li>고객 전화번호와 상세주소 조회 차단</li><li>작업 시작 제한</li></ul><p>관리자 내부 메모는 공개하지 않으며, 전문가에게 공개된 반려 사유만 표시합니다.</p><div className="providerActions"><button className="providerSecondary" onClick={() => navigate('/provider/exit')}>전문가 활동 종료·탈퇴</button></div></section></AuthenticatedLayout>
}
