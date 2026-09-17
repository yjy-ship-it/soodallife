import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'

const root = resolve(import.meta.dirname, '..')
const read = path => readFileSync(resolve(root, path), 'utf8')
const failures = []

const displayFiles = [
  'src/providers/ProviderTrustPage.tsx',
  'src/providers/ProviderInteriorPages.tsx',
  'src/providers/ProviderAdvertisingPage.tsx',
  'src/providers/ProviderWalletPage.tsx',
]

const koreanOnlyFiles = [
  'src/customer/CustomerAccountPages.tsx',
  'src/customer/CustomerPages.tsx',
  'src/customer/LivingHomeSections.tsx',
  'src/chat/ChatPages.tsx',
  'src/emergency/EmergencyPages.tsx',
  'src/help/HelpRoomPages.tsx',
  'src/proposals/ProposalPages.tsx',
  'src/providers/ProviderOnboardingPages.tsx',
  'src/providers/ProviderOperationsHubPages.tsx',
  'src/reviews/ReviewConversationPages.tsx',
  'src/reviews/CommunicationHubPages.tsx',
  'src/providers/ProviderAdvertisingPage.tsx',
]
const forbiddenVisibleText = [
  'EXPERT OFFER', 'SOODAL TOGETHER', 'SOODAL EMERGENCY', 'LIVE STATUS',
  'EMERGENCY DESK', 'FIELD MODE', 'SOODAL COMMUNICATION', 'ASK FOR ADVICE',
  'SOODAL SUGGESTION', '긴급요청 Inbox', '>FAQ<', '>STEP ', '>ON<', '>OFF<',
  'SOODAL TALK', 'SMS/Email', 'WEB 알림', 'Push 알림', 'ETA(분)',
]

for (const file of koreanOnlyFiles) {
  const source = read(file)
  for (const text of forbiddenVisibleText) {
    if (source.includes(text)) failures.push(`${file}: 사용자에게 보이는 영문 문구가 남아 있습니다: ${text}`)
  }
  if (/status\[[^\]]+\]\s*\?\?\s*[^}\n]*(?:StatusCode|statusCode)/.test(source)) {
    failures.push(`${file}: 알 수 없는 상태 코드가 화면에 직접 표시될 수 있습니다.`)
  }
}

for (const file of displayFiles) {
  const source = read(file)
  if (/\?\?\s*[\w.]+(?:statusCode|roleCode|eventTypeCode)/.test(source)) {
    failures.push(`${file}: 알 수 없는 내부 상태 코드가 화면에 직접 표시될 수 있습니다.`)
  }
}

const commonDisplay = read('src/shared/userDisplay.ts')
if (!commonDisplay.includes("'상태 확인 중'")) failures.push('공통 상태 표시의 알 수 없는 값 문구가 없습니다.')
if (!commonDisplay.includes("'지역 확인 중'")) failures.push('공통 지역 표시의 알 수 없는 값 문구가 없습니다.')
if (!commonDisplay.includes('parent') || !commonDisplay.includes('name') || !commonDisplay.includes('`${parent} ${name}`')) failures.push('광역시도와 시군구를 합치는 공통 지역 표시가 없습니다.')

const backendRoot = resolve(root, '../backend/SoodalLife.Api/Features')
for (const file of [
  'CustomerAccounts/CustomerAccountService.cs',
  'HelpRoom/HelpRoomService.cs',
  'Work/WorkService.cs',
  'Emergency/EmergencyWorkflowService.cs',
  'Subscriptions/CustomerCareSubscriptionService.cs',
  'Interior/CustomerInteriorService.cs',
]) {
  const source = readFileSync(resolve(backendRoot, file), 'utf8')
  if (!source.includes('Parent') && !source.includes('parent')) failures.push(`${file}: 광역시도 결합 처리를 확인할 수 없습니다.`)
}

if (failures.length) {
  console.error(failures.join('\n'))
  process.exit(1)
}

console.log('사용자 화면의 한글 상태 표시와 전체 지역명 표시 검사를 통과했습니다.')
