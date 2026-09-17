import type { ChargeRequestResult, WalletDetail, WalletList } from './walletManagementTypes'
interface ApiError { message?:string }
async function request<T>(path:string, init?:RequestInit):Promise<T>{ const response=await fetch(path,{credentials:'include',...init,headers:{'Content-Type':'application/json',...(init?.headers??{})}}); if(!response.ok){let message='이용료 정보를 처리하지 못했습니다.';try{const error=await response.json() as ApiError;if(error.message)message=error.message}catch{/* 공통 안내 */}throw new Error(message)} return response.json() as Promise<T> }
const body=(value:unknown):RequestInit=>({method:'POST',body:JSON.stringify(value)})
export function searchWallets(filters:Record<string,string|number|boolean>){const query=new URLSearchParams();Object.entries(filters).forEach(([key,value])=>{if(value!=='')query.set(key,String(value))});return request<WalletList>(`/api/v1/admin/wallets?${query}`)}
export const getWallet=(providerId:string)=>request<WalletDetail>(`/api/v1/admin/wallets/${providerId}`)
export const createDevelopmentCharge=(providerId:string,input:{amount:number;reason:string;idempotencyKey:string})=>request<ChargeRequestResult>(`/api/v1/admin/wallets/${providerId}/development-charges`,body(input))
export const confirmDevelopmentCharge=(providerId:string,chargeId:string,rowVersion:string)=>request(`/api/v1/admin/wallets/${providerId}/development-charges/${chargeId}/confirm`,body({rowVersion}))
export const adjustWallet=(providerId:string,input:{directionCode:string;amount:number;reason:string;idempotencyKey:string;rowVersion:string})=>request(`/api/v1/admin/wallets/${providerId}/adjustments`,body(input))
export const restoreFee=(providerId:string,feeChargeId:string,input:{reasonCode:string;reason:string;idempotencyKey:string;rowVersion:string})=>request(`/api/v1/admin/wallets/${providerId}/fee-charges/${feeChargeId}/restore`,body(input))
export const requestRefund=(providerId:string,input:{amount:number;reason:string;idempotencyKey:string})=>request(`/api/v1/admin/wallets/${providerId}/refunds`,body(input))
export const approveRefund=(providerId:string,refundId:string,reason:string,rowVersion:string)=>request(`/api/v1/admin/wallets/${providerId}/refunds/${refundId}/approve`,body({reason,rowVersion}))
export const completeDevelopmentRefund=(providerId:string,refundId:string,reason:string,rowVersion:string)=>request(`/api/v1/admin/wallets/${providerId}/refunds/${refundId}/complete-development`,body({reason,rowVersion}))
export const cancelRefund=(providerId:string,refundId:string,reason:string,rowVersion:string)=>request(`/api/v1/admin/wallets/${providerId}/refunds/${refundId}/cancel`,body({reason,rowVersion}))
