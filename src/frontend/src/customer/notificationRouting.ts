import { getCustomerQuote } from '../quotes/api'
import type { CustomerNotification } from './accountTypes'

export async function safeNotificationTarget(item: CustomerNotification) {
  if (item.targetTypeCode === 'RequestDispatch') return '/customer/requests'
  if (!item.targetPublicId || !/^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(item.targetPublicId)) return null
  if (item.targetTypeCode === 'Quote') return `/customer/requests/${(await getCustomerQuote(item.targetPublicId)).requestId}`
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
