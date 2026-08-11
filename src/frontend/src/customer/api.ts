import type { PublicCategory, PublicContent, PublicPromotion, PublicServiceDetail, PublicServiceSummary } from './types'

async function get<T>(path: string): Promise<T> {
  const response = await fetch(path, { credentials: 'include' })
  if (!response.ok) throw new Error('서비스 정보를 불러오지 못했습니다.')
  return response.json() as Promise<T>
}

export const publicCatalogApi = {
  majors: () => get<PublicCategory[]>('/api/v1/public/catalog/categories/majors'),
  children: (id: string) => get<PublicCategory[]>(`/api/v1/public/catalog/categories/${id}/children`),
  services: (take = 12) => get<PublicServiceSummary[]>(`/api/v1/public/catalog/services?take=${take}`),
  servicesByMiddle: (id: string) => get<PublicServiceSummary[]>(`/api/v1/public/catalog/services?middleId=${id}&take=100`),
  search: (query: string) => get<PublicServiceSummary[]>(`/api/v1/public/catalog/services/search?q=${encodeURIComponent(query)}`),
  detail: (id: string) => get<PublicServiceDetail>(`/api/v1/public/catalog/services/${id}`),
  contents: (type: string) => get<PublicContent[]>(`/api/v1/public/contents?type=${type}&audience=CUSTOMER`),
  promotions: () => get<PublicPromotion[]>('/api/v1/public/advertising?audience=CUSTOMER&placement=CUSTOMER_HOME'),
}
