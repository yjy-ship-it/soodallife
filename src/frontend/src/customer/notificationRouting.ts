import { getCustomerQuote } from '../quotes/api'
import type { CustomerNotification } from './accountTypes'

export async function safeNotificationTarget(item: CustomerNotification) {
  if (item.targetTypeCode === 'RequestDispatch') return '/customer/requests'
  if (item.targetTypeCode === 'SUGGESTION') return '/suggestions'
  if (!item.targetPublicId || !/^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(item.targetPublicId)) return null
  if (item.targetTypeCode === 'Quote') return `/customer/requests/${(await getCustomerQuote(item.targetPublicId)).requestId}`
  if (item.targetTypeCode === 'SubscriptionRequest') return `/customer/care/requests/${item.targetPublicId}`
  if (item.targetTypeCode === 'SubscriptionContract') return `/customer/care/contracts/${item.targetPublicId}`
  if (item.targetTypeCode === 'SubscriptionVisitSchedule') return `/customer/care/visits/${item.targetPublicId}`
  if (item.targetTypeCode === 'InteriorProject') return `/customer/interior/projects/${item.targetPublicId}`
  if (item.targetTypeCode === 'ChatRoom') return `/customer/messages/${item.targetPublicId}`
  if (item.targetTypeCode === 'Review') return `/customer/reviews?review=${item.targetPublicId}`
  const routes: Record<string, string> = {
    ServiceRequest: 'requests',
    Transaction: 'transactions',
    AfterServiceCase: 'after-services',
    AfterService: 'after-services',
    DisputeCase: 'disputes',
    Dispute: 'disputes',
    Report: 'reports',
  }
  const segment = item.targetTypeCode ? routes[item.targetTypeCode] : null
  return segment ? `/customer/${segment}/${item.targetPublicId}` : null
}

export function safeProviderNotificationTarget(item: CustomerNotification) {
  const id = item.targetPublicId
  const hasId = Boolean(id && /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(id))
  if (item.targetTypeCode === 'RequestDispatch' || item.targetTypeCode === 'Quote') return '/provider/matched-requests'
  if (item.targetTypeCode === 'ProviderProfile') return '/provider/onboarding'
  if (item.targetTypeCode === 'ProviderService' || item.targetTypeCode === 'ProviderServiceCategory') return '/provider/services'
  if (item.targetTypeCode === 'ProviderServiceArea') return '/provider/areas'
  if (!hasId) return '/provider'
  if (item.targetTypeCode === 'Review') return `/provider/reviews?review=${id}`
  const routes: Record<string, string> = {
    ServiceRequest: 'matched-requests', Transaction: 'work', AfterServiceCase: 'after-services', AfterService: 'after-services',
    DisputeCase: 'disputes', Dispute: 'disputes', SubscriptionRequest: 'care/requests', SubscriptionContract: 'care/contracts',
    SubscriptionVisitSchedule: 'care/visits', InteriorProject: 'interior/projects', ChatRoom: 'messages',
  }
  const segment = item.targetTypeCode ? routes[item.targetTypeCode] : null
  return segment ? `/provider/${segment}/${id}` : '/provider'
}
