import type { ChangeEvent } from 'react'
import type { RequestField } from './types'

export interface FileMetadata { name: string; sizeBytes: number; contentType: string }
export interface StructuredTextValue { value: string }
export type DynamicValue = string | number | boolean | string[] | FileMetadata[] | StructuredTextValue

function structuredText(value: DynamicValue | undefined) {
  if (value && typeof value === 'object' && !Array.isArray(value) && 'value' in value) return String(value.value ?? '')
  return typeof value === 'string' ? value : ''
}

const defaultPeriodOptions = [
  '1일 이내',
  '2~3일',
  '4~7일',
  '1~2주',
  '2~4주',
  '1~2개월',
  '2~3개월',
  '3개월 이상',
  '잘 모름',
]

function normalizedFieldName(field: RequestField) {
  return `${field.fieldKey} ${field.label}`.toLowerCase().replace(/[^\p{L}\p{N}]/gu, '')
}

function isVisitFrequencyField(field: RequestField) {
  const name = normalizedFieldName(field)
  return name.includes('visitfrequency') || name.includes('방문주기')
}

function isParkingAccessField(field: RequestField) {
  const name = normalizedFieldName(field)
  return name.includes('parkingaccess') || name.includes('주차출입조건')
}

export function DynamicFieldInput({ field, value, onChange }: {
  field: RequestField
  value: DynamicValue | undefined
  onChange: (value: DynamicValue) => void
}) {
  const id = `field-${field.id}`
  const common = { id, name: field.fieldKey, required: field.required }
  const placeholder = isParkingAccessField(field)
    ? '예: 주차 가능 여부, 차량 출입 방법, 공동현관 출입 방법, 엘리베이터 이용 가능 여부'
    : field.placeholder ?? undefined
  let control

  switch (field.inputType) {
    case 'LONG_TEXT': case 'TEXTAREA':
      control = <textarea {...common} rows={4} placeholder={placeholder} value={String(value ?? '')} onChange={(event) => onChange(event.target.value)} />
      break
    case 'PERIOD': {
      const periodOptions = field.options.length > 0 ? field.options : defaultPeriodOptions
      control = <select {...common} value={structuredText(value)} onChange={(event) => onChange({ value: event.target.value })}><option value="">선택하세요</option>{periodOptions.map((option) => <option key={option} value={option}>{option}</option>)}</select>
      break
    }
    case 'RECURRENCE':
      control = <textarea {...common} rows={4} placeholder={placeholder} value={structuredText(value)} onChange={(event) => onChange({ value: event.target.value })} />
      break
    case 'NUMBER': case 'MONEY':
      control = <input {...common} type="number" min={field.validationRule.includes('0 이상') ? 0 : undefined} value={value === undefined ? '' : String(value)} onChange={(event) => onChange(event.target.value)} />
      break
    case 'SELECT':
      control = <select {...common} value={String(value ?? '')} onChange={(event) => onChange(event.target.value)}><option value="">{isVisitFrequencyField(field) ? '없음' : '선택하세요'}</option>{field.options.map((option) => <option key={option} value={option}>{option}</option>)}</select>
      break
    case 'RADIO':
      control = <div className="choiceGroup">{field.options.map((option) => <label key={option}><input type="radio" name={field.fieldKey} checked={value === option} onChange={() => onChange(option)} />{option}</label>)}</div>
      break
    case 'CHECKBOX': {
      const selected = Array.isArray(value) ? value as string[] : []
      control = <div className="choiceGroup">{field.options.map((option) => <label key={option}><input type="checkbox" checked={selected.includes(option)} onChange={(event) => onChange(event.target.checked ? [...selected, option] : selected.filter((item) => item !== option))} />{option}</label>)}</div>
      break
    }
    case 'DATE':
      control = <input {...common} type="date" value={String(value ?? '')} onChange={(event) => onChange(event.target.value)} />
      break
    case 'DATETIME':
      control = <input {...common} type="datetime-local" value={String(value ?? '')} onChange={(event) => onChange(event.target.value)} />
      break
    case 'FILE':
      control = <input {...common} type="file" multiple onChange={(event: ChangeEvent<HTMLInputElement>) => onChange(Array.from(event.target.files ?? []).map((file) => ({ name: file.name, sizeBytes: file.size, contentType: file.type || 'application/octet-stream' })))} />
      break
    case 'BOOLEAN':
      control = <label className="booleanField"><input type="checkbox" checked={Boolean(value)} onChange={(event) => onChange(event.target.checked)} /> 예</label>
      break
    default:
      control = <input {...common} type="text" placeholder={placeholder} value={String(value ?? '')} onChange={(event) => onChange(event.target.value)} />
  }

  return <div className="formField dynamicField"><label htmlFor={['RADIO', 'CHECKBOX'].includes(field.inputType) ? undefined : id}>{field.label}{field.required && <span className="requiredMark"> *</span>}</label>{control}{(field.helperText || field.validationRule) && <small>{field.helperText || field.validationRule}</small>}</div>
}
