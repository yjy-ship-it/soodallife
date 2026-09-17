import type { LivingHome, PublicActivityFeed, PublicArea, PublicCategory, PublicContent, PublicPromotion, PublicServiceDetail, PublicServiceSummary } from './types'

async function get<T>(path: string): Promise<T> {
  const response = await fetch(path, { credentials: 'include' })
  if (!response.ok) throw new Error('서비스 정보를 불러오지 못했습니다.')
  return response.json() as Promise<T>
}

export const publicCatalogApi = {
  majors: () => get<PublicCategory[]>('/api/v1/public/catalog/categories/majors'),
  children: (id: string) => get<PublicCategory[]>(`/api/v1/public/catalog/categories/${id}/children`),
  services: (take = 12) => get<PublicServiceSummary[]>(`/api/v1/public/catalog/services?take=${take}`),
  representativeServices: (take = 8) => get<PublicServiceSummary[]>(`/api/v1/public/catalog/services/representative?take=${take}&maxPerMiddle=2`),
  servicesByMiddle: (id: string) => get<PublicServiceSummary[]>(`/api/v1/public/catalog/services?middleId=${id}&take=100`),
  search: (query: string) => get<PublicServiceSummary[]>(`/api/v1/public/catalog/services/search?q=${encodeURIComponent(query)}`),
  detail: (id: string) => get<PublicServiceDetail>(`/api/v1/public/catalog/services/${id}`),
  detailBySlug: (slug: string) => get<PublicServiceDetail>(`/api/v1/public/catalog/services/by-slug/${encodeURIComponent(slug)}`),
  contents: (type: string) => get<PublicContent[]>(`/api/v1/public/contents?type=${type}&audience=CUSTOMER`),
  sidos: () => get<PublicArea[]>('/api/v1/administrative-areas/sidos'),
  promotions: (categoryId?:string,areaId?:string,placement='CUSTOMER_HOME') => {const query=new URLSearchParams({audience:'CUSTOMER',placement});if(categoryId)query.set('categoryId',categoryId);if(areaId)query.set('areaId',areaId);return get<PublicPromotion[]>(`/api/v1/public/advertising?${query}`)},
  advertisingEvent: async (creativeId:string,eventType:'IMPRESSION'|'CLICK',categoryId?:string,areaId?:string,placement='CUSTOMER_HOME') => { await fetch(`/api/v1/public/advertising/creatives/${creativeId}/events/${eventType}`,{method:'POST',credentials:'include',headers:{'Content-Type':'application/json'},body:JSON.stringify({audienceTypeCode:'CUSTOMER',placementCode:placement,categoryId:categoryId??null,areaId:areaId??null})}) },
  activity: (region?: string, eventType?: string, take = 20) => { const query = new URLSearchParams({ take: String(take) }); if (region) query.set('region', region); if (eventType) query.set('eventType', eventType); return get<PublicActivityFeed>(`/api/v1/public/activity?${query}`) },
  livingHome: () => get<LivingHome>('/api/v1/public/living-home'),
}
