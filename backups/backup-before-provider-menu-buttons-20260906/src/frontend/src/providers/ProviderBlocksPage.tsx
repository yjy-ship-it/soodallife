import { useEffect, useState } from 'react'
import { providerBlockReasonLabel } from '../relationshipBlocks/reasons'
import { ProviderAppLayout } from './ProviderAppLayout'
import { navigate } from '../auth/routing'

type ReceivedBlock = { id:string; direction:string; status:'ACTIVE'|'RELEASED'; reasonCode:string|null; createdAt:string; releasedAt:string|null }
const date = (value:string|null) => value ? new Intl.DateTimeFormat('ko-KR',{dateStyle:'medium',timeStyle:'short'}).format(new Date(value)) : '-'

export function ProviderBlocksPage() {
  const [items,setItems]=useState<ReceivedBlock[]>([]); const [error,setError]=useState('')
  useEffect(()=>{fetch('/api/v1/providers/me/customer-blocks',{credentials:'include'}).then(async response=>{if(!response.ok)throw new Error((await response.json() as {message?:string}).message??'차단 현황을 불러오지 못했습니다.');return response.json() as Promise<ReceivedBlock[]>}).then(setItems).catch(reason=>setError(reason instanceof Error?reason.message:'차단 현황을 불러오지 못했습니다.'))},[])
  const active=items.filter(item=>item.status==='ACTIVE')
  return <ProviderAppLayout><section className="providerHero"><div><p>MATCHING RESTRICTIONS</p><h1>매칭 제한 현황</h1><span>고객이 설정한 차단으로 제한된 신규 매칭만 확인합니다. 고객의 신원과 비공개 메모는 보호됩니다.</span></div><span className="providerStatusPill">제한 중 {active.length}건</span></section>{error&&<div className="providerAlert error">{error}</div>}<section className="providerPanel"><h2>현재 및 과거 제한 내역</h2><div className="providerBlockHistory">{items.map(item=><article key={item.id}><div><strong>{item.status==='ACTIVE'?'신규 매칭 제한':'제한 해제'}</strong><span>{providerBlockReasonLabel(item.reasonCode)}</span></div><small>설정 {date(item.createdAt)}{item.releasedAt?` · 해제 ${date(item.releasedAt)}`:''}</small></article>)}{items.length===0&&<p>확인된 매칭 제한 내역이 없습니다.</p>}</div></section><section className="providerPanel providerBlockPolicy"><h2>차단 적용 범위</h2><ul><li>차단 중에는 해당 고객의 새로운 요청 매칭, 견적 제출과 고객의 전문가 선택이 제한됩니다.</li><li>이미 시작된 거래, 채팅, 리뷰, A/S와 분쟁 기록은 유지되며 필요한 후속 처리를 계속할 수 있습니다.</li><li>고객이 설정한 차단은 전문가가 임의로 해제할 수 없습니다.</li><li>차단 사유에 이의가 있거나 안전 문제가 있다면 고객 신원을 추정하거나 직접 연락하지 말고 고객센터·분쟁 절차를 이용해 주세요.</li></ul><div className="proposalActions"><button type="button" onClick={()=>navigate('/provider/disputes')}>분쟁·이의 확인</button><button type="button" onClick={()=>navigate('/provider/support')}>고객센터 안내</button></div></section></ProviderAppLayout>
}
