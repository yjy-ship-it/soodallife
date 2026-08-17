import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from 'react'
import { useAuthentication } from '../auth/AuthenticationContext'
import { createLoginPath, navigate } from '../auth/routing'
import { BrandLogo } from '../components/BrandLogo'
import { ServiceFooter } from '../components/ServiceFooter'
import { AuthenticatedLayout } from '../components/AuthenticatedLayout'
import { customerAccountApi } from '../customer/accountApi'
import { apiUrl } from '../config/apiEndpoint'
import * as api from './api'
import type { ProviderDashboard, ProviderDocument, ProviderDocumentType, ProviderLegalDocument, ProviderProfile, ProviderRequirement } from './types'
import './providerEnhancements.css'

const approvalLabel: Record<string, string> = { PENDING: '본사 심사 대기', APPROVED: '승인 완료', REJECTED: '보완 또는 재심사 필요', SUSPENDED: '이용 정지' }
const message = (error: unknown) => error instanceof Error ? error.message : '요청을 처리하지 못했습니다.'

export function ProviderPublicStartPage() {
  return <div className="providerPublicShell"><div className="providerPublicHero"><section className="providerPublicIntro"><BrandLogo /><p>SOODAL PARTNERS</p><h1>수달 라이프<br />공급자 등록</h1><p>서비스 분야와 활동지역, 필요한 증빙을 등록하면 본사 심사를 거쳐 공급자로 활동할 수 있습니다. 승인 전에는 요청 수신, 견적 제출, 고객 연락처 조회가 제한됩니다.</p><div className="providerActions"><button className="providerPrimary" onClick={() => navigate('/provider/signup')}>공급자 등록 시작</button><button className="providerSecondary" onClick={() => navigate(createLoginPath('/provider'))}>공급자 로그인</button></div></section><section className="providerPublicSteps"><h2>등록 절차</h2><ol><li>계정과 공급자 기본정보 입력</li><li>실제 제공 서비스 선택</li><li>서비스별 시·군·구 활동지역 설정</li><li>정책상 필요한 자격·증빙 제출</li><li>본사 심사 및 보완</li><li>승인된 서비스부터 이용</li></ol><p>수익 또는 수수료 금액은 서비스 정책과 실제 거래 조건에 따라 달라지며, 이 화면에서 보장하지 않습니다.</p></section></div><ServiceFooter variant="provider" /></div>
}

type SignupState = { loginId: string; password: string; passwordConfirmation: string; phone: string; phoneVerificationToken: string; businessName: string; representativeName: string; contactName: string; businessRegistrationNumber: string; businessAddress: string; businessTypeText: string; businessItemText: string; introduction: string }
const emptySignup: SignupState = { loginId: '', password: '', passwordConfirmation: '', phone: '', phoneVerificationToken: '', businessName: '', representativeName: '', contactName: '', businessRegistrationNumber: '', businessAddress: '', businessTypeText: '', businessItemText: '', introduction: '' }
const signupText = (value: unknown) => typeof value === 'string' ? value : ''
const signupTrimmed = (value: unknown) => signupText(value).trim()
const formatMobilePhone = (value: string) => { const digits = value.replace(/\D/g, '').slice(0, 11); if (digits.length <= 3) return digits; if (digits.length <= 7) return `${digits.slice(0, 3)}-${digits.slice(3)}`; return `${digits.slice(0, 3)}-${digits.slice(3, 7)}-${digits.slice(7)}` }
type AreaOption = { id: string; name: string; code: string; parentId: string | null; parentName: string | null }
const validBusinessNumber = (value: string) => { const digits = value.replace(/\D/g, ''); if (digits.length !== 10) return false; const n = [...digits].map(Number), w = [1,3,7,1,3,7,1,3,5]; const sum = w.reduce((v, x, i) => v + x * n[i], 0) + Math.floor(n[8] * 5 / 10); return (10 - sum % 10) % 10 === n[9] }
const isValidKoreanBusinessNumber = validBusinessNumber
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
  const [verificationStatus, setVerificationStatus] = useState('확인 중')
  const [phoneChecked, setPhoneChecked] = useState(false)
  const [providerKind, setProviderKind] = useState<'BUSINESS' | 'INDIVIDUAL'>('BUSINESS')
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
  useEffect(() => { api.getProviderLegalDocuments().then(setLegal).catch(reason => setError(message(reason))) }, [])
  useEffect(() => { api.getIdentityVerificationStatus().then(value => setVerificationStatus(value.statusCode)).catch(() => setVerificationStatus('NOT_INTEGRATED')) }, [])
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
  const checkPhone = async () => { setError(''); setPhoneChecked(false); try { const result = await api.getPhoneAvailability(form.phone); if (!result.available) { set('phoneVerificationToken', ''); setError('이미 등록된 휴대전화 번호입니다. 기존 계정으로 로그인해 주세요.'); return } if (verificationStatus === 'NOT_INTEGRATED') { set('phone', signupTrimmed(result.normalizedValue) || formatMobilePhone(form.phone)); set('phoneVerificationToken', 'NOT_INTEGRATED'); setPhoneChecked(true); return } setError('휴대전화 본인인증 서비스 연동 후 인증 창이 제공됩니다.') } catch (reason) { setError(message(reason)) } }
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
      set('businessRegistrationNumber', result.normalizedValue)
      setVerifiedBusinessNumber(result.normalizedValue)
      setBusinessValidation('형식·체크섬·중복가입 확인 완료 · 국세청 사업자 상태조회 NOT_INTEGRATED')
    } catch (reason) { setBusinessValidation(message(reason)) }
  }
  const submit = async () => { setError(''); try { const consentValues = legal.map(item => ({ legalDocumentVersionId: item.versionId, agreed: !!consents[item.versionId] })); const profile = { businessName: form.businessName, representativeName: form.representativeName, contactName: form.contactName, businessRegistrationNumber: form.businessRegistrationNumber || null, businessAddress: form.businessAddress || null, businessTypeText: form.businessTypeText || null, businessItemText: form.businessItemText || null, introduction: form.introduction || null, consents: consentValues }; if (existing) { await api.addProviderRole(profile); await refresh() } else await api.registerProvider({ ...form, email: null, phone: form.phone || null, ...profile }); setDone(true) } catch (reason) { setError(message(reason)) } }
  if (done) return <div className="signupShell"><header><BrandLogo /></header><main className="signupMain"><section className="signupCard"><p className="accountEyebrow">등록 접수 완료</p><h1>본사 심사를 준비해 주세요</h1><p>{existing ? '공급자 역할과 현재 로그인 권한이 갱신되었습니다. 공급자 등록 현황에서 서비스·지역·증빙을 이어서 등록해 주세요.' : '고객·공급자 통합 계정이 생성되었습니다. 로그인 후 서비스·지역·증빙을 등록해 주세요.'}</p><button className="accountPrimary" onClick={() => existing ? window.location.assign('https://partner.soodallife.kr/provider/onboarding') : navigate(createLoginPath('/provider'))}>{existing ? '공급자 등록 현황' : '로그인으로 이동'}</button></section></main><ServiceFooter variant="provider" /></div>
  const password = signupText(form?.password)
  const passwordConfirmation = signupText(form?.passwordConfirmation)
  const passwordValid = /^(?=.*[A-Za-z])(?=.*\d)(?=.*[^A-Za-z0-9\s])\S{6,}$/.test(password)
  const passwordMismatch = !!passwordConfirmation && password !== passwordConfirmation
  const workflowStep = existing ? step : step - 1
  const steps = existing ? ['계정확인', '기본정보', '등록정보', '약관·동의', '최종확인'] : ['휴대전화 확인', '계정정보', '기본정보', '등록정보', '약관·동의', '최종확인']
  const maxStep = steps.length
  const nextValidationMessage = () => {
    if (!existing && step === 1) {
      if (!/^010-\d{4}-\d{4}$/.test(signupText(form?.phone))) return '휴대전화 번호를 010-1234-5678 형식으로 입력해 주세요.'
      if (!phoneChecked || !signupText(form?.phoneVerificationToken)) return '휴대전화 본인인증 버튼을 눌러 번호 중복 여부를 확인해 주세요.'
    }
    if (!existing && workflowStep === 1) {
      if (!signupTrimmed(form?.loginId)) return '아이디를 입력해 주세요.'
      if (!loginChecked) return '아이디 중복확인을 완료해 주세요.'
      if (!passwordValid) return '비밀번호가 규칙에 맞지 않습니다.'
      if (!passwordConfirmation || password !== passwordConfirmation) return '비밀번호를 다시 확인하세요.'
    }
    if (workflowStep === 2) {
      if (!signupTrimmed(form?.businessName)) return '공급자 표시명 또는 상호를 입력해 주세요.'
      if (!signupTrimmed(form?.representativeName)) return '대표자명 또는 본인 성명을 입력해 주세요.'
      if (!signupTrimmed(form?.contactName)) return '현장 연락 담당자를 입력해 주세요.'
    }
    if (workflowStep === 3 && providerKind === 'BUSINESS') {
      if (!validBusinessNumber(form.businessRegistrationNumber) || verifiedBusinessNumber !== form.businessRegistrationNumber.replace(/\D/g, '')) return '사업자등록번호를 확인하고 번호 검증을 완료해 주세요.'
      if (!sidoId || !sigunguId) return '사업장 시·도와 시·군·구를 선택해 주세요.'
      if (!signupTrimmed(addressDetail)) return '사업장 상세주소를 입력해 주세요.'
      if (!signupTrimmed(form?.businessTypeText) || !signupTrimmed(form?.businessItemText)) return '사업자등록증의 업태와 업종을 입력해 주세요.'
    }
    if (workflowStep === 4) {
      if (legal.length === 0) return '공급자 등록 약관을 불러오지 못했습니다. 잠시 후 다시 시도해 주세요.'
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
  return <div className="signupShell"><header><BrandLogo /><button onClick={() => navigate('/provider/start')}>등록 안내</button></header><main className="signupMain providerSignupMain"><ol className="signupSteps">{steps.map((label, index) => <li className={step >= index + 1 ? 'isActive' : ''} key={label}><span>{index + 1}</span>{label}</li>)}</ol><section className="signupCard providerSignupCard"><p className="accountEyebrow">공급자 등록 {step}/{maxStep}</p><h1>{existing ? '기존 고객 계정에 공급자 역할 추가' : '수달 파트너스 공급자 회원가입'}</h1>
    {!existing && step === 1 && <div className="providerFormGrid"><label className="wide">휴대전화<input value={form.phone} onChange={e => { set('phone', formatMobilePhone(e.target.value)); set('phoneVerificationToken', ''); setPhoneChecked(false) }} placeholder="010-1234-5678" inputMode="tel" autoComplete="tel" /></label><div className="wide"><button className="providerSecondary" type="button" disabled={!/^010-\d{4}-\d{4}$/.test(form.phone)} onClick={() => void checkPhone()}>휴대전화 본인인증</button><div className="notIntegratedNotice"><strong>휴대전화 본인인증 · {verificationStatus}</strong><span>{phoneChecked ? '중복되지 않은 번호로 확인되었습니다. 외부 인증 연동 전까지 NOT_INTEGRATED 상태로 다음 단계 진행이 가능합니다.' : '숫자만 입력해도 하이픈이 자동 적용됩니다. 이미 등록된 번호는 새 계정에 사용할 수 없습니다.'}</span></div></div></div>}
    {workflowStep === 1 && (existing ? <div className="notIntegratedNotice"><strong>{user?.loginId}</strong><span>기존 계정과 고객 역할은 유지하고 공급자 역할을 추가합니다.</span></div> : <div className="providerFormGrid providerAccountGrid"><label className="wide">휴대전화<input value={form.phone} disabled /></label><label className="wide">아이디<div className="providerInlineField"><input value={form.loginId} onChange={e => { set('loginId', e.target.value); setLoginChecked(false) }} autoComplete="username" /><button type="button" onClick={() => void checkLogin()}>중복확인</button></div>{loginChecked && <small className="validationOk">사용할 수 있는 아이디입니다.</small>}</label><label>비밀번호<input type="password" minLength={6} value={form.password} onChange={e => set('password', e.target.value)} autoComplete="new-password" /><small>6자 이상이며 영문자, 숫자, 특수문자를 각각 포함해 주세요.</small>{form.password && !passwordValid && <small className="validationError" role="alert">비밀번호가 규칙에 맞지 않습니다.</small>}</label><label>비밀번호 확인<input type="password" minLength={6} value={form.passwordConfirmation} onChange={e => set('passwordConfirmation', e.target.value)} autoComplete="new-password" />{passwordMismatch && <small className="validationError" role="alert">비밀번호를 다시 확인하세요.</small>}</label></div>)}
    {workflowStep === 2 && <div><div className="providerGuidance"><strong>정확하고 자세한 정보를 입력해 주세요</strong><p>사실과 다른 정보는 본사 심사에서 반려될 수 있습니다. 공급자 표시명과 소개는 고객이 견적을 비교하고 공급자를 선택하는 중요한 판단 자료입니다.</p></div><div className="providerKindCards"><button type="button" className={providerKind === 'BUSINESS' ? 'selected' : ''} onClick={() => { setProviderKind('BUSINESS'); setIdentityDocument(null); if (documentInput.current) documentInput.current.value = '' }}><strong>사업자 공급자</strong><span>개인사업자·법인사업자</span></button><button type="button" className={providerKind === 'INDIVIDUAL' ? 'selected' : ''} onClick={() => { setProviderKind('INDIVIDUAL'); setIdentityDocument(null); if (documentInput.current) documentInput.current.value = ''; set('businessRegistrationNumber', ''); set('businessAddress', ''); set('businessTypeText', ''); set('businessItemText', ''); setSidoId(''); setSigunguId(''); setAddressDetail(''); setBusinessValidation(''); setVerifiedBusinessNumber('') }}><strong>개인 공급자</strong><span>사업자등록 없이 개인으로 활동</span></button></div><div className="providerDocumentScan"><strong>{providerKind === 'BUSINESS' ? '사업자등록증 업로드' : '신분증 앞면 업로드'}</strong><p>{providerKind === 'BUSINESS' ? '상호, 대표자명, 사업자등록번호, 사업장 주소, 업태와 업종을 확인합니다.' : '이름 확인에 필요한 최소 정보만 확인합니다. 주민등록번호 뒷자리는 가린 뒤 올려 주세요.'}</p><input ref={documentInput} type="file" accept=".pdf,.jpg,.jpeg,.png" onChange={e => setIdentityDocument(e.target.files?.[0] ?? null)} />{identityDocument && <div className="notIntegratedNotice"><strong>{identityDocument.name}</strong><span>OCR 자동 판독: NOT_INTEGRATED · 외부 OCR 승인 전에는 값을 임의로 채우지 않습니다.</span><button type="button" className="providerDocumentDelete" onClick={() => { setIdentityDocument(null); if (documentInput.current) documentInput.current.value = '' }}>업로드 삭제</button></div>}</div><div className="providerFormGrid"><label>{providerKind === 'BUSINESS' ? '공급자 표시명/상호' : '공급자 표시명'}<input value={form.businessName} onChange={e => set('businessName', e.target.value)} placeholder={providerKind === 'BUSINESS' ? '사업자등록증의 상호' : '고객에게 표시할 이름'} /></label><label>{providerKind === 'BUSINESS' ? '대표자명' : '본인 성명'}<input value={form.representativeName} onChange={e => set('representativeName', e.target.value)} /></label><label>현장 연락 담당자<input value={form.contactName} onChange={e => set('contactName', e.target.value)} placeholder="본인이면 본인 성명" /></label><label className="wide">공급자 소개<select defaultValue="" onChange={e => { const selected = providerIntroductionExamples.find(item => item.label === e.target.value); if (selected) set('introduction', selected.value) }}><option value="">예시를 선택하거나 직접 입력하세요</option>{providerIntroductionExamples.map(item => <option key={item.label} value={item.label}>{item.label}</option>)}</select><textarea value={form.introduction} onChange={e => set('introduction', e.target.value)} placeholder="서비스 경력, 전문 분야, 작업 방식, 고객 응대와 A/S 원칙을 자세히 적어 주세요." /><small>예시를 선택한 뒤 실제 경력과 서비스에 맞게 수정하거나 직접 입력해 주세요. 등록 후 공급자 기본정보에서 홍보용 소개와 로고·사진·연락처·웹사이트를 추가할 수 있습니다.</small></label></div></div>}
    {workflowStep === 3 && (
      <div className="providerFormGrid">
        {providerKind === 'BUSINESS' ? (
          <>
            <label>사업자등록번호<div className="providerInlineField"><input inputMode="numeric" value={form.businessRegistrationNumber} onChange={e => { set('businessRegistrationNumber', e.target.value.replace(/\D/g, '').slice(0, 10)); setBusinessValidation(''); setVerifiedBusinessNumber('') }} placeholder="숫자 10자리" /><button type="button" onClick={() => void verifyBusinessNumber()}>번호 검증</button></div>{businessValidation && <small className={verifiedBusinessNumber ? 'validationOk' : 'validationError'}>{businessValidation}</small>}</label>
            <label>사업장 시·도<select value={sidoId} onChange={e => setSidoId(e.target.value)}><option value="">선택</option>{sidos.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></label>
            <label>사업장 시·군·구<select value={sigunguId} onChange={e => setSigunguId(e.target.value)} disabled={!sidoId}><option value="">선택</option>{sigungu.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></label>
            {areaError && <div className="notIntegratedNotice wide" role="alert"><strong>행정구역 목록 오류</strong><span>{areaError}</span><button type="button" className="providerDocumentDelete" onClick={() => void (sidoId ? loadSigungu(sidoId) : loadSidos())}>다시 불러오기</button></div>}
            <label className="wide">사업장 상세주소<input value={addressDetail} onChange={e => setAddressDetail(e.target.value)} placeholder="도로명·건물번호와 상세주소" /></label>
            <label>업태<input value={form.businessTypeText} onChange={e => set('businessTypeText', e.target.value)} /></label>
            <label>업종<input value={form.businessItemText} onChange={e => set('businessItemText', e.target.value)} /></label>
          </>
        ) : (
          <div className="notIntegratedNotice wide"><strong>개인 공급자 등록</strong><span>사업자등록번호·사업장 주소·업태·업종은 입력하지 않습니다. 본인확인 및 신분증 검증 결과는 본사 심사에만 사용하며 고객에게 공개하지 않습니다.</span></div>
        )}
        <p className="wide">제출 정보는 본사 심사 후 승인됩니다. 자동 판독 결과가 제공되더라도 공급자가 확인한 값만 등록 정보로 사용합니다.</p>
      </div>
    )}
    {workflowStep === 4 && <div><div className="notIntegratedNotice"><strong>공급자 등록 약관 및 동의</strong><span>공급자 활동, 개인정보 처리, 선택적 마케팅 수신에 관한 문서입니다. 필수 문서는 동의해야 등록할 수 있습니다.</span></div>{legal.length === 0 ? <p>현재 적용할 공급자 전용 약관을 불러오지 못했습니다. 잠시 후 다시 시도해 주세요.</p> : legal.map(item => <article className="providerLegalCard" key={item.versionId}><label><span><strong>{item.title}</strong><small>{item.requirementCode === 'REQUIRED' ? '필수' : '선택'}</small></span><input type="checkbox" checked={!!consents[item.versionId]} onChange={e => setConsents(current => ({ ...current, [item.versionId]: e.target.checked }))} /></label><details><summary>내용 보기</summary><p>{item.content}</p></details></article>)}</div>}
    {workflowStep === 5 && <div className="providerReview"><div className="providerReviewGrid">
      <article><span className="providerReviewIcon" aria-hidden="true">▣</span><div><strong>등록 유형</strong><p>{providerKind === 'BUSINESS' ? '사업자 공급자' : '개인 공급자'}</p></div></article>
      <article><span className="providerReviewIcon" aria-hidden="true">◆</span><div><strong>공급자명</strong><p>{form.businessName}</p></div></article>
      <article><span className="providerReviewIcon" aria-hidden="true">●</span><div><strong>대표자·담당자</strong><p>{form.representativeName} / {form.contactName}</p></div></article>
      <article><span className="providerReviewIcon" aria-hidden="true">▤</span><div><strong>등록 증빙</strong><p>{identityDocument?.name ?? '등록 후 비공개 증빙 관리에서 제출'}</p></div></article>
      <article><span className="providerReviewIcon" aria-hidden="true">↔</span><div><strong>통합 역할</strong><p>고객 + 공급자</p></div></article>
      <article><span className="providerReviewIcon" aria-hidden="true">✓</span><div><strong>등록 후 상태</strong><p>본사 심사 대기 / 활동 전</p></div></article>
    </div><aside className="providerNextStep"><span className="providerNextStepIcon" aria-hidden="true">→</span><div><strong>다음 단계</strong><p>공급자 등록 접수 후 서비스·활동지역·증빙을 등록하고 본사 심사를 진행해 주세요.</p></div></aside></div>}
    {error && <div className="providerAlert error" role="alert">{error}</div>}<div className="providerActions">{step > 1 && <button className="providerSecondary" type="button" onClick={() => { setError(''); setStep(value => value - 1) }}>이전</button>}{step < maxStep ? <button className="providerPrimary" type="button" onClick={goNext}>다음</button> : <button className="providerPrimary" type="button" onClick={() => void submit()}>공급자 등록 접수</button>}</div></section></main><ServiceFooter variant="provider" /></div>
}

export function ProviderHomePage() {
  const [dashboard, setDashboard] = useState<ProviderDashboard | null>(null), [profile, setProfile] = useState<ProviderProfile | null>(null), [error, setError] = useState('')
  useEffect(() => { Promise.all([api.getProviderDashboard(), api.getProviderProfile()]).then(([d, p]) => { setDashboard(d); setProfile(p) }).catch(reason => setError(message(reason))) }, [])
  const progress = dashboard ? [!!profile?.representativeName, dashboard.registeredServiceCount > 0, dashboard.activeAreaCount > 0, dashboard.requiredEvidenceCount === 0 || dashboard.submittedEvidenceCount === dashboard.requiredEvidenceCount, dashboard.approvalStatus === 'APPROVED'] : []
  return <AuthenticatedLayout><section className="providerHero"><div><p>SOODAL PARTNERS</p><h1>{profile?.businessName ?? '공급자 홈'}</h1><span>서비스 등록부터 본사 승인까지 실제 상태를 확인합니다. 승인 전에는 견적과 고객 개인정보 접근이 제한됩니다.</span></div><span className="providerStatusPill">{approvalLabel[dashboard?.approvalStatus ?? 'PENDING']}</span></section>{error && <div className="providerAlert error">{error}</div>}<ol className="providerSteps">{['기본정보', '서비스', '활동지역', '증빙', '본사 승인', '이용 시작'].map((label, index) => <li className={progress[index] ? 'done' : ''} key={label}>{label}</li>)}</ol><section className="providerGrid"><StatusCard label="등록 서비스" value={`${dashboard?.registeredServiceCount ?? 0}개`} detail={`승인 ${dashboard?.approvedServiceCount ?? 0} · 심사 ${dashboard?.pendingServiceCount ?? 0} · 반려 ${dashboard?.rejectedServiceCount ?? 0}`} path="/provider/services" /><StatusCard label="활동지역" value={`${dashboard?.activeAreaCount ?? 0}개`} detail="서비스별 시·군·구 기준" path="/provider/areas" /><StatusCard label="필수 증빙" value={`${dashboard?.submittedEvidenceCount ?? 0}/${dashboard?.requiredEvidenceCount ?? 0}`} detail={`확인 완료 ${dashboard?.approvedEvidenceCount ?? 0}`} path="/provider/documents" /><StatusCard label="Trust" value={profile?.trustScore == null ? '평가 전' : `${profile.trustScore}`} detail={profile?.trustDisplayStatus ?? '평가 전'} path="/provider/approval" /></section><section className="providerPanel"><h2>다음 해야 할 일</h2>{dashboard?.nextActions.length ? <ul>{dashboard.nextActions.map(item => <li key={item}>{item}</li>)}</ul> : <p>현재 온보딩 준비가 완료되었습니다.</p>}{dashboard?.rejectionReason && <div className="providerAlert error">공개 가능한 반려 사유: {dashboard.rejectionReason}</div>}</section></AuthenticatedLayout>
}
function StatusCard({ label, value, detail, path }: { label: string; value: string; detail: string; path: string }) { return <button className="providerCard" onClick={() => navigate(path)}><small>{label}</small><strong>{value}</strong><p>{detail}</p></button> }

export function ProviderProfilePage() {
  const [profile, setProfile] = useState<ProviderProfile | null>(null), [error, setError] = useState(''), [saved, setSaved] = useState(''), [businessValidation, setBusinessValidation] = useState('')
  const [step, setStep] = useState(1), [saving, setSaving] = useState(false)
  const [logoFile, setLogoFile] = useState<File | null>(null), [photoFiles, setPhotoFiles] = useState<File[]>([])
  const [photoAppendMode, setPhotoAppendMode] = useState(false)
  const [logoPreview, setLogoPreview] = useState(''), [photoPreviews, setPhotoPreviews] = useState<string[]>([])
  const introRef = useRef<HTMLDivElement>(null)
  const logoInput = useRef<HTMLInputElement>(null), photosInput = useRef<HTMLInputElement>(null)
  const publicIntroductionHtml = profile?.publicIntroductionHtml ?? ''
  useEffect(() => { api.getProviderProfile().then(setProfile).catch(reason => setError(message(reason))) }, [])
  useEffect(() => { if (introRef.current && introRef.current.innerHTML !== publicIntroductionHtml) introRef.current.innerHTML = publicIntroductionHtml }, [publicIntroductionHtml, step])
  useEffect(() => { if (!logoFile) { setLogoPreview(''); return }; const url = URL.createObjectURL(logoFile); setLogoPreview(url); return () => URL.revokeObjectURL(url) }, [logoFile])
  useEffect(() => { const urls = photoFiles.map(file => URL.createObjectURL(file)); setPhotoPreviews(urls); return () => urls.forEach(url => URL.revokeObjectURL(url)) }, [photoFiles])
  const update = <K extends keyof ProviderProfile>(key: K, value: ProviderProfile[K]) => setProfile(current => current ? { ...current, [key]: value } : current)
  const validateImage = (file: File) => { if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) return 'JPG, PNG, WEBP 이미지만 등록할 수 있습니다.'; if (file.size > 5 * 1024 * 1024) return '이미지는 파일당 5MB 이하만 등록할 수 있습니다.'; return '' }
  const selectLogo = (file: File | null) => { if (!file) return; const validation = validateImage(file); if (validation) { setError(validation); if (logoInput.current) logoInput.current.value = ''; return }; setError(''); setSaved(''); setLogoFile(file) }
  const selectPhotos = (files: File[]) => { if (files.length > 5) { setError('홍보 사진은 최대 5개까지 등록할 수 있습니다.'); if (photosInput.current) photosInput.current.value = ''; return }; const validation = files.map(validateImage).find(Boolean); if (validation) { setError(validation); if (photosInput.current) photosInput.current.value = ''; return }; setError(''); setSaved(''); setPhotoAppendMode(false); setPhotoFiles(files) }
  const next = () => { if (!profile) return; if (step === 1 && (!profile.businessName.trim() || !profile.representativeName?.trim() || !profile.contactName?.trim())) { setError('공급자 표시명, 대표자명, 담당자명을 모두 입력해 주세요.'); return }; setError(''); setSaved(''); setStep(value => Math.min(3, value + 1)); window.scrollTo({ top: 0, behavior: 'smooth' }) }
  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!profile || step !== 3) return
    setError(''); setSaved(''); setSaving(true)
    try {
      if (logoFile || photoFiles.length) await api.getProviderPromotionStorageStatus()
      let current = await api.updateProviderProfile(profile)
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
    } catch (reason) {
      if (reason instanceof api.ProviderApiError && reason.status === 409) {
        try { setProfile(await api.getProviderProfile()) } catch { /* keep the current form when refresh also fails */ }
      }
      setError(message(reason))
    } finally { setSaving(false) }
  }
  const steps = ['기본정보', '고객 공개정보', '이미지·확인']
  const currentPhotos = photoPreviews.length ? photoPreviews : profile?.publicPhotoUrls ?? []
  return <AuthenticatedLayout><section className="providerHero"><div><p>MY SOODAL</p><h1>공급자 기본정보·홍보 프로필</h1><span>한 단계씩 필요한 정보만 입력하고 마지막 단계에서 전체 내용을 저장합니다.</span></div></section>{error && !profile && <div className="providerAlert error">{error}</div>}{profile && <><ol className="providerProfileSteps" aria-label="프로필 입력 단계">{steps.map((label, index) => <li className={step === index + 1 ? 'current' : step > index + 1 ? 'done' : ''} key={label}><span>{step > index + 1 ? '✓' : index + 1}</span><b>{label}</b></li>)}</ol><form className="providerPanel providerProfileWizard" onSubmit={event => void submit(event)}><div className="providerProfileStepHeading"><span>STEP {step} / 3</span><h2>{steps[step - 1]}</h2><p>{step === 1 ? '본사 심사와 공급자 확인에 필요한 기본정보를 입력해 주세요.' : step === 2 ? '고객이 견적과 함께 확인할 공개 소개와 연락 정보를 작성해 주세요.' : '로고와 홍보 사진을 직접 선택하고 미리보기를 확인해 주세요.'}</p></div>{step === 1 && <><div className="providerGuidance"><strong>정확한 사업자·담당자 정보를 입력해 주세요.</strong>필수 항목은 다음 단계로 이동하기 전에 확인합니다.</div><div className="providerFormGrid"><label>공급자 표시명/상호<input required value={profile.businessName} onChange={e => update('businessName', e.target.value)} /></label><label>대표자명<input required value={profile.representativeName ?? ''} onChange={e => update('representativeName', e.target.value)} /></label><label>담당자명<input required value={profile.contactName ?? ''} onChange={e => update('contactName', e.target.value)} /></label><label>심사용 전화번호<input value={profile.phone ?? ''} onChange={e => update('phone', e.target.value)} /></label><label>심사용 이메일<input type="email" value={profile.email ?? ''} onChange={e => update('email', e.target.value)} /></label><label>사업자등록번호<div className="providerInlineActions"><input inputMode="numeric" value={profile.businessRegistrationNumber ?? ''} onChange={e => { update('businessRegistrationNumber', e.target.value.replace(/\D/g, '').slice(0, 10)); setBusinessValidation('') }} /><button className="providerSecondary" type="button" onClick={() => setBusinessValidation(isValidKoreanBusinessNumber(profile.businessRegistrationNumber ?? '') ? '검증번호 형식이 유효합니다.' : '사업자등록번호 10자리를 확인해 주세요.')}>번호 검증</button></div>{businessValidation && <span className={`providerValidation ${businessValidation.startsWith('검증') ? 'ok' : 'error'}`}>{businessValidation} 국세청 사업자 상태조회는 아직 연동되지 않았습니다.</span>}</label><label className="wide">사업장 주소<input value={profile.businessAddress ?? ''} onChange={e => update('businessAddress', e.target.value)} /></label><label>업태<input value={profile.businessTypeText ?? ''} onChange={e => update('businessTypeText', e.target.value)} /></label><label>업종<input value={profile.businessItemText ?? ''} onChange={e => update('businessItemText', e.target.value)} /></label><label className="wide">심사용 소개<textarea value={profile.introduction ?? ''} onChange={e => update('introduction', e.target.value)} /></label></div><p className="providerPromoHelp">공급자 유형은 등록 시 선택한 사업자 또는 개인 공급자 기준을 유지합니다.</p></>}{step === 2 && <><div className="providerGuidance"><strong>고객이 공급자를 선택할 때 확인하는 정보입니다.</strong>견적을 받은 고객이 상세정보를 열었을 때 표시됩니다.</div><div className="providerFormGrid"><label className="wide">공급자 상세 소개<div ref={introRef} className="providerRichEditor" contentEditable suppressContentEditableWarning onInput={e => update('publicIntroductionHtml', e.currentTarget.innerHTML)} /></label><label>공개 전화번호<input value={profile.publicPhone ?? ''} onChange={e => update('publicPhone', e.target.value)} /></label><label>공개 이메일<input type="email" value={profile.publicEmail ?? ''} onChange={e => update('publicEmail', e.target.value)} /></label><label className="wide">공개 주소·지도 표시용 주소<input value={profile.publicAddress ?? ''} onChange={e => update('publicAddress', e.target.value)} /></label><label>블로그 주소<input type="url" placeholder="https://" value={profile.publicBlogUrl ?? ''} onChange={e => update('publicBlogUrl', e.target.value)} /></label><label>홈페이지 주소<input type="url" placeholder="https://" value={profile.publicWebsiteUrl ?? ''} onChange={e => update('publicWebsiteUrl', e.target.value)} /></label></div><p className="providerPromoHelp">소개 편집란에는 안전한 글자 서식·목록·링크를 사용할 수 있습니다.</p></>}{step === 3 && <><div className="providerUploadGrid"><section className="providerImageUploadCard"><div><span className="providerUploadNumber">1</span><h3>로고 이미지</h3><p>JPG·PNG·WEBP, 5MB 이하, 1개</p></div><input ref={logoInput} type="file" accept="image/jpeg,image/png,image/webp" onChange={e => selectLogo(e.target.files?.[0] ?? null)} /><div className="providerLogoPreview">{(logoPreview || profile.publicLogoUrl) ? <img src={apiUrl(logoPreview || profile.publicLogoUrl || '')} alt="공급자 로고 미리보기" /> : <span>로고 미리보기</span>}</div>{logoFile && <button type="button" className="providerSecondary" onClick={() => { setLogoFile(null); if (logoInput.current) logoInput.current.value = '' }}>새 로고 선택 취소</button>}</section><section className="providerImageUploadCard"><div><span className="providerUploadNumber">2</span><h3>홍보 사진</h3><p>JPG·PNG·WEBP, 각 5MB 이하, 최대 5개</p></div><input ref={photosInput} type="file" multiple accept="image/jpeg,image/png,image/webp" onChange={e => selectPhotos(Array.from(e.target.files ?? []))} /><div className="providerPhotoPreviews">{currentPhotos.map((url, index) => <figure key={`${url}-${index}`}><img src={apiUrl(url)} alt={`홍보 사진 ${index + 1} 미리보기`} /><figcaption>{index + 1}</figcaption></figure>)}{!currentPhotos.length && <p>선택한 홍보 사진이 여기에 표시됩니다.</p>}</div>{photoFiles.length > 0 && <button type="button" className="providerSecondary" onClick={() => { setPhotoFiles([]); if (photosInput.current) photosInput.current.value = '' }}>새 홍보 사진 선택 취소</button>}<small>새 사진을 선택하면 저장 시 기존 홍보 사진을 전체 교체합니다.</small></section></div><section className="providerProfileReview"><h3>저장 전 확인</h3><dl><div><dt>공급자 표시명</dt><dd>{profile.businessName}</dd></div><div><dt>공개 연락처</dt><dd>{profile.publicPhone || profile.publicEmail || '미입력'}</dd></div><div><dt>로고</dt><dd>{logoFile ? '새 이미지 선택됨' : profile.publicLogoUrl ? '등록됨' : '미등록'}</dd></div><div><dt>홍보 사진</dt><dd>{photoFiles.length || profile.publicPhotoUrls.length}개</dd></div></dl></section></>}{error && <div className="providerAlert error">{error}</div>}{saved && <div className="providerAlert success">{saved}</div>}<div className="providerActions providerWizardActions">{step > 1 && <button type="button" className="providerSecondary" disabled={saving} onClick={() => { setError(''); setStep(value => value - 1) }}>이전</button>}{step < 3 ? <button type="button" className="providerPrimary" onClick={next}>다음 단계</button> : <button className="providerPrimary" disabled={saving}>{saving ? '저장 중…' : '전체 저장'}</button>}</div></form></>}</AuthenticatedLayout>
}

export function ProviderDocumentsPage() {
  const [requirements, setRequirements] = useState<ProviderRequirement[]>([]), [documents, setDocuments] = useState<ProviderDocument[]>([]), [types, setTypes] = useState<ProviderDocumentType[]>([]), [typeId, setTypeId] = useState(''), [file, setFile] = useState<File | null>(null), [error, setError] = useState(''), [saved, setSaved] = useState('')
  const load = useCallback(() => Promise.all([api.getProviderRequirements(), api.getProviderDocuments(), api.getProviderDocumentTypes()]).then(([r, d, t]) => { setRequirements(r); setDocuments(d); setTypes(t); setTypeId(current => current || t[0]?.id || '') }), [])
  useEffect(() => { load().catch(reason => setError(message(reason))) }, [load])
  const upload = async () => { if (!file || !typeId) return; setError(''); try { await api.uploadProviderDocument(typeId, file); await load(); setSaved('증빙이 등록되었습니다. Malware Scanner는 아직 미연동 상태입니다.'); setFile(null) } catch (reason) { setError(message(reason)) } }
  const link = async (verificationId: string, documentId: string) => { try { await api.linkProviderEvidence(verificationId, documentId); await load() } catch (reason) { setError(message(reason)) } }
  return <AuthenticatedLayout><section className="providerHero"><div><p>EVIDENCE</p><h1>증빙·자격 관리</h1><span>서비스별 정책에 정의된 증빙만 제출합니다. 원본 파일은 본인과 권한 있는 관리자만 열 수 있습니다.</span></div></section>{error && <div className="providerAlert error">{error}</div>}{saved && <div className="providerAlert success">{saved}</div>}<section className="providerPanel"><h2>증빙 파일 등록</h2><div className="providerFormGrid"><label>증빙 유형<select value={typeId} onChange={e => setTypeId(e.target.value)}>{types.map(item => <option value={item.id} key={item.id}>{item.name}</option>)}</select></label><label>PDF/JPG/PNG, 최대 10MB<input type="file" accept=".pdf,.jpg,.jpeg,.png" onChange={e => setFile(e.target.files?.[0] ?? null)} /></label></div><p>악성코드 검사: NOT_INTEGRATED. CLEAN 상태를 임의 생성하지 않습니다.</p><div className="providerActions"><button className="providerPrimary" disabled={!file || !typeId} onClick={() => void upload()}>비공개 증빙 업로드</button></div></section><section className="providerPanel"><h2>서비스별 필수 요건</h2>{requirements.length === 0 ? <p>선택한 서비스에 등록된 요건이 없습니다.</p> : requirements.map(item => { const acceptedCodes = new Set(item.acceptedEvidenceTypes.map(x => x.code)); const available = documents.filter(doc => acceptedCodes.has(doc.documentTypeCode)); return <article className="providerRequirement" key={item.verificationId}><div><h3>{item.requirementName} {item.isRequired && <span className="providerStatusPill">필수</span>}</h3><p>{item.categoryPath}</p><p>상태: {item.verificationStatus}{item.rejectionReason ? ` · ${item.rejectionReason}` : ''}</p><p>인정 증빙: {item.acceptedEvidenceTypes.map(x => x.name).join(', ') || '정책 확인 필요'}</p>{item.documentName && <p className="providerFileMeta">제출 파일: {item.documentName}</p>}</div><select value={item.documentId ?? ''} onChange={e => e.target.value && void link(item.verificationId, e.target.value)}><option value="">제출 증빙 선택</option>{available.map(doc => <option key={doc.fileId} value={doc.fileId}>{doc.originalFileName} ({doc.malwareStatus})</option>)}</select></article> })}</section><section className="providerPanel"><h2>내 증빙</h2>{documents.map(doc => <article className="providerRequirement" key={doc.fileId}><div><h3>{doc.documentTypeName}</h3><p>{doc.originalFileName} · {(doc.sizeBytes / 1024).toFixed(1)}KB</p><p>검증 {doc.verificationStatus} · Malware {doc.malwareStatus}</p></div><a className="providerSecondary" href={`/api/v1/providers/me/documents/${doc.fileId}/content`}>본인 원본 열기</a></article>)}</section></AuthenticatedLayout>
}

export function ProviderApprovalPage() {
  const [dashboard, setDashboard] = useState<ProviderDashboard | null>(null), [profile, setProfile] = useState<ProviderProfile | null>(null), [requirements, setRequirements] = useState<ProviderRequirement[]>([])
  useEffect(() => { Promise.all([api.getProviderDashboard(), api.getProviderProfile(), api.getProviderRequirements()]).then(([d, p, r]) => { setDashboard(d); setProfile(p); setRequirements(r) }) }, [])
  const missing = useMemo(() => requirements.filter(x => x.isRequired && !x.documentId), [requirements])
  return <AuthenticatedLayout><section className="providerHero"><div><p>APPROVAL</p><h1>{approvalLabel[dashboard?.approvalStatus ?? 'PENDING']}</h1><span>공급자 전체 승인과 서비스별 승인은 서로 구분됩니다.</span></div><span className="providerStatusPill">{dashboard?.activityStatus ?? 'INACTIVE'}</span></section><section className="providerGrid"><StatusCard label="전체 공급자" value={dashboard?.approvalStatus ?? 'PENDING'} detail="본사 공급자 심사" path="/provider/onboarding" /><StatusCard label="서비스 승인" value={`${dashboard?.approvedServiceCount ?? 0}/${dashboard?.registeredServiceCount ?? 0}`} detail="서비스별 별도 심사" path="/provider/services" /><StatusCard label="미제출 증빙" value={`${missing.length}건`} detail="정책상 필수 요건" path="/provider/documents" /><StatusCard label="Trust" value={profile?.trustScore == null ? '평가 전' : `${profile.trustScore}`} detail="확정값만 표시" path="/provider/approval" /></section>{dashboard?.rejectionReason && <div className="providerAlert error">공개 가능한 반려 사유: {dashboard.rejectionReason}</div>}<section className="providerPanel"><h2>승인 전 제한</h2><ul><li>신규 고객 요청 수신 제한</li><li>견적 제출 제한</li><li>고객 전화번호와 상세주소 조회 차단</li><li>작업 시작 제한</li></ul><p>보완이 필요한 경우 기본정보·서비스·지역·증빙을 수정한 후, 지원되는 서비스 재심사 기능을 사용합니다. 관리자 내부 메모는 공개하지 않습니다.</p><div className="providerActions"><button className="providerSecondary" onClick={() => navigate('/provider/exit')}>공급자 활동 종료·탈퇴</button></div></section></AuthenticatedLayout>
}
