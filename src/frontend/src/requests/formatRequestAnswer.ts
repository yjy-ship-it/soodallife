const isoDateTimePattern = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d{1,7})?)?(?:Z|[+-]\d{2}:?\d{2})?$/

export function formatRequestDateTime(value: unknown) {
  const parsed = new Date(String(value))
  if (Number.isNaN(parsed.getTime())) return String(value)

  return new Intl.DateTimeFormat('ko-KR', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    weekday: 'short',
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
    timeZone: 'Asia/Seoul',
  }).format(parsed)
}

export function formatRequestAnswer(value: unknown, inputType?: string): string {
  if (value === null || value === undefined || value === '') return '입력 없음'
  if (Array.isArray(value)) return value.map(item => formatRequestAnswer(item)).join(', ')
  if (typeof value === 'object' && 'value' in value) return formatRequestAnswer(value.value, inputType)
  if (typeof value === 'object') return JSON.stringify(value)
  if (typeof value === 'boolean') return value ? '예' : '아니오'

  const text = String(value)
  if (inputType?.toUpperCase() === 'DATETIME' || isoDateTimePattern.test(text)) {
    return formatRequestDateTime(text)
  }

  return text
}

export function isZeroRequestAnswer(value: unknown): boolean {
  let current = value
  while (current !== null && typeof current === 'object' && !Array.isArray(current) && 'value' in current) {
    current = current.value
  }
  return current === 0 || typeof current === 'string' && current.trim() !== '' && Number(current) === 0
}

type RequestAnswerLabelSource = { fieldId: string; label: string; inputType: string }

export function formatRequestAnswerLabel<T extends RequestAnswerLabelSource>(answers: T[], answer: T): string {
  if (answer.inputType.toUpperCase() !== 'DATETIME') return answer.label
  const dateTimeAnswers = answers.filter(item => item.inputType.toUpperCase() === 'DATETIME')
  if (dateTimeAnswers.length <= 1) return answer.label
  const rank = dateTimeAnswers.findIndex(item => item.fieldId === answer.fieldId) + 1
  const baseLabel = answer.label.replace(/\s*\d+순위\s*$/, '').trim() || '희망일시'
  return `${baseLabel} ${rank}순위`
}
