export type ServiceCategoryStatus = 'ACTIVE' | 'PAUSED' | 'REVIEW'

export interface AdminCategorySummary {
  majorCount: number
  middleCount: number
  serviceCount: number
  activeServiceCount: number
}

export interface AdminCategoryOption {
  id: string
  name: string
  externalCode: string | null
  statusCode: ServiceCategoryStatus
  sortOrder: number
}

export interface AdminServiceCategoryListItem extends AdminCategoryOption {
  majorId: string
  majorName: string
  middleId: string
  middleName: string
}

export interface AdminServiceCategoryList {
  totalCount: number
  items: AdminServiceCategoryListItem[]
}

export interface AdminServiceCategoryDetail extends AdminServiceCategoryListItem {}

export interface UpdateAdminServiceCategoryInput {
  name: string
  statusCode: ServiceCategoryStatus
  sortOrder: number
}
