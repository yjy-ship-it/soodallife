import type { ChangeEvent } from 'react'
import type { RequestField } from './types'

export interface FileMetadata { name: string; sizeBytes: number; contentType: string }
export type DynamicValue = string | number | boolean | string[] | FileMetadata[]

export function DynamicFieldInput({ field, value, onChange }: {
  field: RequestField
  value: DynamicValue | undefined
  onChange: (value: DynamicValue) => void
}) {
  const id = `field-${field.id}`
  const common = { id, name: field.fieldKey, required: field.required }
  let control

  switch (field.inputType) {
    case 'LONG_TEXT': case 'TEXTAREA': case 'PERIOD': case 'RECURRENCE':
      control = <textarea {...common} rows={4} value={String(value ?? '')} onChange={(event) => onChange(event.target.value)} />
      break
    case 'NUMBER': case 'MONEY':
      control = <input {...common} type="number" min={field.validationRule.includes('0 이상') ? 0 : undefined} value={value === undefined ? '' : String(value)} onChange={(event) => onChange(event.target.value)} />
      break
    case 'SELECT':
      control = <select {...common} value={String(value ?? '')} onChange={(event) => onChange(event.target.value)}><option value="">선택하세요</option>{field.options.map((option) => <option key={option} value={option}>{option}</option>)}</select>
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
      control = <input {...common} type="text" placeholder={field.placeholder ?? undefined} value={String(value ?? '')} onChange={(event) => onChange(event.target.value)} />
  }

  return <div className="formField dynamicField"><label htmlFor={['RADIO', 'CHECKBOX'].includes(field.inputType) ? undefined : id}>{field.label}{field.required && <span className="requiredMark"> *</span>}</label>{control}{(field.helperText || field.validationRule) && <small>{field.helperText || field.validationRule}</small>}</div>
}
