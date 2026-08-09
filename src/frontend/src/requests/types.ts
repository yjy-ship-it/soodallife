export interface Category {
  id: string
  name: string
  level: 'MAJOR' | 'MIDDLE' | 'SERVICE'
  externalCode: string | null
  sortOrder: number
}

export interface RequestField {
  id: string
  fieldKey: string
  label: string
  inputType: string
  required: boolean
  placeholder: string | null
  options: string[]
  helperText: string | null
  validationRule: string
  sortOrder: number
}

export interface AdministrativeArea { id: string; name: string; areaCode: string }

export interface ServiceRequestListItem {
  id: string
  title: string
  status: string
  categoryPath: string
  createdAt: string
  desiredAt: string | null
}

export interface ServiceRequestAnswer {
  fieldId: string
  fieldKey: string
  label: string
  inputType: string
  value: unknown
}

export interface ServiceRequestDetail extends ServiceRequestListItem {
  description: string | null
  isUrgent: boolean
  administrativeAreaId: string
  administrativeAreaName: string
  detailAddress: string | null
  answers: ServiceRequestAnswer[]
}

export interface ApiErrorBody {
  businessCode: string
  message: string
  fieldErrors?: Record<string, string[]>
  traceId: string
}
