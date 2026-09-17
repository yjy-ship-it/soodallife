import type { SettlementOperationsDashboard, UnifiedLedger } from './settlementOperationsTypes'

async function read<T>(url:string):Promise<T>{
  const response=await fetch(url,{credentials:'include'})
  if(!response.ok){
    const body=await response.json().catch(()=>null) as {message?:string}|null
    throw new Error(body?.message??'결제·정산 정보를 불러오지 못했습니다.')
  }
  return response.json() as Promise<T>
}

export const getSettlementOperationsDashboard=()=>read<SettlementOperationsDashboard>('/api/v1/admin/settlement-operations/dashboard')

export function getUnifiedSettlementLedger(input:{source?:string;search?:string;from?:string;to?:string;page:number;pageSize?:number}){
  const query=new URLSearchParams({page:String(input.page),pageSize:String(input.pageSize??30)})
  if(input.source)query.set('source',input.source)
  if(input.search)query.set('search',input.search)
  if(input.from)query.set('from',`${input.from}T00:00:00+09:00`)
  if(input.to)query.set('to',`${input.to}T23:59:59+09:00`)
  return read<UnifiedLedger>(`/api/v1/admin/settlement-operations/ledger?${query}`)
}
