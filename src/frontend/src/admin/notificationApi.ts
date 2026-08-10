import type{NotificationDelivery,NotificationSummary,NotificationTemplate}from'./notificationTypes'
async function call<T>(path:string,init?:RequestInit):Promise<T>{const r=await fetch(path,{credentials:'include',headers:{'Content-Type':'application/json'},...init});if(!r.ok){const b=await r.json().catch(()=>null) as{message?:string}|null;throw new Error(b?.message??'알림 관리 정보를 처리하지 못했습니다.')}return r.json() as Promise<T>}
export const getNotificationSummary=()=>call<NotificationSummary>('/api/v1/admin/notifications/summary')
export const getNotificationTemplates=(search='')=>call<NotificationTemplate[]>(`/api/v1/admin/notifications/templates?search=${encodeURIComponent(search)}`)
export const createNotificationTemplate=(body:unknown)=>call<NotificationTemplate>('/api/v1/admin/notifications/templates',{method:'POST',body:JSON.stringify(body)})
export const setNotificationTemplateStatus=(id:string,isActive:boolean,rowVersion:string)=>call<NotificationTemplate>(`/api/v1/admin/notifications/templates/${id}/status`,{method:'PATCH',body:JSON.stringify({isActive,rowVersion})})
export const getNotificationDeliveries=(status='')=>call<NotificationDelivery[]>(`/api/v1/admin/notifications/deliveries?status=${encodeURIComponent(status)}`)
export const retryNotificationDelivery=(id:string)=>call<NotificationDelivery>(`/api/v1/admin/notifications/deliveries/${id}/retry`,{method:'POST'})
