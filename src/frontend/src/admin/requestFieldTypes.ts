export type RequestFieldInputType = 'ADDRESS' | 'DATETIME' | 'FILE' | 'LONG_TEXT' | 'MONEY' | 'NUMBER' | 'PERIOD' | 'RECURRENCE' | 'SELECT' | 'TEXT'
export type RequestFieldDefinitionStatus = 'ACTIVE' | 'INACTIVE'

export interface AdminRequestFieldOption {
  id: string
  value: string
  label: string
  displayOrder: number
  isActive: boolean
}

export interface AdminRequestField {
  id: string
  label: string
  inputType: RequestFieldInputType
  required: boolean
  displayOrder: number
  definitionStatus: RequestFieldDefinitionStatus
  serviceEnabled: boolean
  assignmentScope: 'MIDDLE_DEFAULT' | 'SERVICE_OVERRIDE'
  options: AdminRequestFieldOption[]
  unit: string | null
  validationRule: string
  hasOptions: boolean
  hasValidation: boolean
  affectedServiceCount: number
}

export interface UpdateAdminRequestFieldDefinitionInput {
  label: string
  inputType: RequestFieldInputType
  statusCode: RequestFieldDefinitionStatus
  unit: string | null
  validationRule: string
  confirmSharedChange: boolean
}

export interface UpdateAdminRequestFieldAssignmentInput {
  isActive: boolean
  isRequired: boolean
  displayOrder: number
}

export interface UpdateAdminRequestFieldOptionInput {
  label: string
  displayOrder: number
  isActive: boolean
}
