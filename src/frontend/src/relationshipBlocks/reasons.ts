export const providerBlockReasons = [
  ['CUSTOMER_PREFERENCE', '개인적인 이용 선호'],
  ['COMMUNICATION_DIFFICULTY', '의사소통이 원활하지 않음'],
  ['SERVICE_MISMATCH', '요청 서비스와 맞지 않음'],
  ['INAPPROPRIATE_BEHAVIOR', '부적절한 응대 또는 행동'],
  ['SAFETY_CONCERN', '안전 우려'],
  ['OTHER', '기타'],
] as const

export function providerBlockReasonLabel(code: string | null) {
  return providerBlockReasons.find(([value]) => value === code)?.[1] ?? (code || '사유 미입력(기존 기록)')
}
