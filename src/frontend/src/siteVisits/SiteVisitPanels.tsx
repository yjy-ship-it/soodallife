import { useCallback, useEffect, useMemo, useState } from 'react'
import * as api from './api'
import type { SaveSiteVisitProposal, SiteVisitProposal } from './types'
import { soodalPrompt } from '../components/soodalDialog'
import * as quoteApi from '../quotes/api'

const money=(value:number)=>value===0?'무료':`${value.toLocaleString('ko-KR')}원`
const date=(value:string)=>new Intl.DateTimeFormat('ko-KR',{dateStyle:'medium',timeStyle:'short'}).format(new Date(value))
const localValue=(value:Date)=>new Date(value.getTime()-value.getTimezoneOffset()*60000).toISOString().slice(0,16)
const automaticExpiry=(scheduledValue:string,requestExpiresAt:string)=>{
  const now=Date.now(),scheduled=new Date(scheduledValue).getTime(),requestEnd=new Date(requestExpiresAt).getTime()
  const latest=Math.min(scheduled-60000,requestEnd-60000)
  const preferred=Math.min(scheduled-24*60*60*1000,latest)
  return localValue(new Date(preferred>now+5*60*1000?preferred:Math.min(now+12*60*60*1000,latest)))
}
const statusLabel:Record<string,string>={PROPOSED:'고객 확인 대기',ACCEPTED:'방문 확정',DEPARTED:'출발',ARRIVED:'도착',COMPLETED:'방문 완료',REJECTED:'고객 거절',CANCELLED:'취소',EXPIRED:'기한 만료',NO_SHOW:'노쇼 기록',DISPUTED:'노쇼 이의 진행'}
const paymentLabel:Record<string,string>={NO_FEE:'무료',ON_SITE:'현장 결제',TRANSFER_REPORTED:'이체 사실 통보 후 출발',TRANSFER_CONFIRMED:'전문가 입금 확인 후 출발'}
const digits=(value:string)=>Number(value.replace(/[^0-9]/g,''))

function VisitCard({item,children}:{item:SiteVisitProposal;children?:React.ReactNode}){
  return <article className="siteVisitCard">
    <div className="siteVisitCardHead"><strong>{item.providerName}</strong><span>{statusLabel[item.status]??item.status}</span></div>
    <dl className="siteVisitSummary"><div><dt>방문 예정</dt><dd>{date(item.scheduledAt)}</dd></div><div><dt>예상 소요</dt><dd>{item.estimatedDurationMinutes}분</dd></div><div><dt>방문비</dt><dd>{money(item.visitFeeAmount)}</dd></div><div><dt>결제</dt><dd>{paymentLabel[item.paymentMode]??item.paymentMode}</dd></div></dl>
    {item.deductFromWorkAmount&&<p className="siteVisitBenefit">작업 진행 시 방문비를 작업대금에서 차감합니다.</p>}{item.terms&&<p>{item.terms}</p>}
    {item.paymentInstruction&&<p className="privacyNote">전문가 안내: {item.paymentInstruction}<br/>플랫폼은 계좌이체 사실을 직접 확인하거나 보증하지 않습니다.</p>}
    {item.detailAddress&&<p><strong>방문 주소</strong> {item.detailAddress}</p>}
    <div className="siteVisitTimeline">{item.events.map(event=><span key={event.id}>{statusLabel[event.eventType]??event.eventType} · {date(event.occurredAt)}</span>)}</div>{children}
  </article>
}

export function ProviderSiteVisitPanel({requestId,requestExpiresAt,allowNewProposal=true}:{requestId:string;requestExpiresAt:string;allowNewProposal?:boolean}){
  const [items,setItems]=useState<SiteVisitProposal[]>([]),[error,setError]=useState(''),[message,setMessage]=useState(''),[saving,setSaving]=useState(false)
  const [quoteStatus,setQuoteStatus]=useState<string|null|undefined>(undefined)
  const tomorrow=useMemo(()=>{const value=new Date();value.setDate(value.getDate()+1);value.setHours(10,0,0,0);return localValue(value)},[])
  const expiry=useMemo(()=>automaticExpiry(tomorrow,requestExpiresAt),[requestExpiresAt,tomorrow])
  const [form,setForm]=useState<SaveSiteVisitProposal>({scheduledAt:tomorrow,estimatedDurationMinutes:30,visitFeeAmount:0,paymentMode:'NO_FEE',terms:null,deductFromWorkAmount:true,paymentInstruction:null,noShowWaitMinutes:10,expiresAt:expiry,idempotencyKey:crypto.randomUUID(),rowVersion:null})
  const load=useCallback(()=>api.providerList(requestId).then(setItems).catch((e:Error)=>setError(e.message)),[requestId]);useEffect(()=>{void load()},[load])
  useEffect(()=>{quoteApi.getProviderQuote(requestId).then(value=>setQuoteStatus(value?.status??null)).catch(()=>setQuoteStatus(null))},[requestId])
  useEffect(()=>{const current=items[0];if(current?.canEdit)setForm({scheduledAt:localValue(new Date(current.scheduledAt)),estimatedDurationMinutes:current.estimatedDurationMinutes,visitFeeAmount:current.visitFeeAmount,paymentMode:current.paymentMode,terms:current.terms,deductFromWorkAmount:current.deductFromWorkAmount,paymentInstruction:current.paymentInstruction,noShowWaitMinutes:current.noShowWaitMinutes,expiresAt:localValue(new Date(current.expiresAt)),idempotencyKey:crypto.randomUUID(),rowVersion:current.rowVersion})},[items])
  const submit=async()=>{setError('');setMessage('');setSaving(true);try{await api.save(requestId,{...form,scheduledAt:new Date(form.scheduledAt).toISOString(),expiresAt:new Date(form.expiresAt).toISOString(),paymentMode:form.visitFeeAmount===0?'NO_FEE':form.paymentMode});setMessage('방문견적 제안 제출이 완료되었습니다.');await load()}catch(e){setError(e instanceof Error?e.message:'저장하지 못했습니다.')}finally{setSaving(false)}}
  const visitActionsBlocked=quoteStatus==='NOT_SELECTED'
  const act=async(item:SiteVisitProposal,action:string)=>{if(visitActionsBlocked)return;setError('');try{if(action==='PAYMENT_CONFIRM')await api.decidePayment(item.id,item.rowVersion,'CONFIRM',null);else if(action==='NO_SHOW'){const note=await soodalPrompt('연락 시도와 대기 상황을 적어 주세요.');if(note===null)return;await api.noShow(item.id,item.rowVersion,'CUSTOMER',note.trim()||null)}else if(action==='CANCEL'){const reason=await soodalPrompt('취소 사유를 입력해 주세요.');if(reason===null)return;await api.cancel(item.id,reason.trim()||null,item.rowVersion)}else await api.progress(item.id,action,item.rowVersion);await load()}catch(e){setError(e instanceof Error?e.message:'처리하지 못했습니다.')}}
  const current=items[0]
  if(!current&&!allowNewProposal)return null
  return <section id="provider-site-visit-form" tabIndex={-1} className="detailCard siteVisitPanel"><div className="sectionHeading"><div><p className="eyebrow">방문견적</p><h2>방문견적</h2><p>현장 확인이 필요할 때만 제안하세요. 최종 견적을 제출할 때만 기존 플랫폼 수수료 예약이 적용됩니다.</p></div></div>
    {error&&<p className="errorBanner">{error}</p>}{message&&<p className="successBanner">{message}</p>}
    {current?<VisitCard item={current}>{quoteStatus===undefined?<p className="privacyNote">견적 상태를 확인하고 있습니다.</p>:visitActionsBlocked?<p className="infoBanner">이 견적은 미채택되어 방문 업무를 진행하거나 취소할 필요가 없습니다.</p>:<div className="formActions">{current.status==='PROPOSED'&&<button className="primaryButton" type="button" disabled>방문견적 제안 제출 완료</button>}{current.status==='ACCEPTED'&&<button className="primaryButton" onClick={()=>void act(current,'DEPARTED')}>출발</button>}{current.status==='DEPARTED'&&<button className="primaryButton" onClick={()=>void act(current,'ARRIVED')}>도착</button>}{current.status==='ARRIVED'&&<><button className="primaryButton" onClick={()=>void act(current,'COMPLETED')}>방문 완료</button><button className="secondaryButton siteVisitNoShowButton" onClick={()=>void act(current,'NO_SHOW')}>고객 노쇼 기록</button></>}{current.paymentStatus==='REPORTED'&&<button className="secondaryButton" onClick={()=>void act(current,'PAYMENT_CONFIRM')}>입금 확인</button>}{!['COMPLETED','CANCELLED','REJECTED','EXPIRED'].includes(current.status)&&current.status!=='PROPOSED'&&<button className="secondaryButton" onClick={()=>void act(current,'CANCEL')}>방문 취소</button>}</div>}</VisitCard>:allowNewProposal?<div className="siteVisitForm">
      <label>방문 예정일시<input type="datetime-local" value={form.scheduledAt} onChange={e=>{const scheduledAt=e.target.value;setForm({...form,scheduledAt,expiresAt:automaticExpiry(scheduledAt,requestExpiresAt)})}}/></label><label>예상 소요시간(분)<input type="number" min={10} max={480} value={form.estimatedDurationMinutes} onChange={e=>setForm({...form,estimatedDurationMinutes:Number(e.target.value)})}/></label><label>방문비<input type="text" inputMode="numeric" value={form.visitFeeAmount.toLocaleString('ko-KR')} onChange={e=>setForm({...form,visitFeeAmount:digits(e.target.value)})}/></label><label>결제 방식<select value={form.visitFeeAmount===0?'NO_FEE':form.paymentMode} disabled={form.visitFeeAmount===0} onChange={e=>setForm({...form,paymentMode:e.target.value})}><option value="NO_FEE">무료</option><option value="ON_SITE">현장 결제</option><option value="TRANSFER_REPORTED">고객 이체 통보 후 출발</option><option value="TRANSFER_CONFIRMED">전문가 입금 확인 후 출발</option></select></label><label>고객 수락 기한<input type="datetime-local" value={form.expiresAt} readOnly aria-readonly="true"/><small>방문 예정 24시간 전을 우선으로 요청 마감 안에서 자동 계산됩니다.</small></label><label>노쇼 대기시간(분)<input type="number" min={5} max={60} value={form.noShowWaitMinutes} onChange={e=>setForm({...form,noShowWaitMinutes:Number(e.target.value)})}/></label><label className="siteVisitWide">방문 조건<textarea value={form.terms??''} onChange={e=>setForm({...form,terms:e.target.value||null})} placeholder="무료견적 범위, 추가 비용 가능성 등을 적어 주세요."/></label>{form.visitFeeAmount>0&&form.paymentMode.startsWith('TRANSFER')&&<label className="siteVisitWide">이체 안내<textarea value={form.paymentInstruction??''} onChange={e=>setForm({...form,paymentInstruction:e.target.value||null})} placeholder="은행·계좌·예금주 안내. 고객 수락 후에만 공개됩니다."/></label>}<label className="siteVisitDeduction siteVisitWide"><input type="checkbox" checked={form.deductFromWorkAmount} onChange={e=>setForm({...form,deductFromWorkAmount:e.target.checked})}/><span>작업 진행 시 방문비를 작업대금에서 차감</span></label><div className="formActions siteVisitWide"><button className="primaryButton" disabled={saving} onClick={()=>void submit()}>{saving?'저장 중...':'방문견적 제안'}</button></div>
    </div>:null}
  </section>
}

export function CustomerSiteVisitPanel({requestId,defaultDetailAddress,onCountChange}:{requestId:string;defaultDetailAddress:string;onCountChange?:(count:number)=>void}){
  const [items,setItems]=useState<SiteVisitProposal[]>([]),[error,setError]=useState(''),[message,setMessage]=useState('')
  const load=useCallback(()=>api.customerList(requestId).then(values=>{setItems(values);onCountChange?.(values.length)}).catch((e:Error)=>setError(e.message)),[requestId,onCountChange])
  const pollingInterval=items.some(item=>['DEPARTED','ARRIVED'].includes(item.status))?10000:30000
  const pollingComplete=items.length>0&&items.every(item=>['COMPLETED','CANCELLED','REJECTED','EXPIRED'].includes(item.status))
  useEffect(()=>{void load();if(pollingComplete)return;const timer=window.setInterval(()=>{if(document.visibilityState==='visible')void load()},pollingInterval);return()=>window.clearInterval(timer)},[load,pollingComplete,pollingInterval])
  useEffect(()=>{if(items.length>0&&window.location.hash==='#customer-site-visit-proposals')window.requestAnimationFrame(()=>{const target=document.getElementById('customer-site-visit-proposals');target?.scrollIntoView({behavior:'smooth',block:'start'});target?.focus({preventScroll:true})})},[items.length])
  const act=async(item:SiteVisitProposal,action:string)=>{setError('');setMessage('');try{if(action==='ACCEPT'){const address=(await soodalPrompt('방문할 상세주소를 확인해 주세요.',defaultDetailAddress,{title:'방문 주소 확인'}))?.trim();if(!address)return;await api.accept(item.id,address,item.rowVersion);setMessage('방문견적 조건에 동의했습니다. 전문가에게 상세주소가 공개됩니다.')}else if(action==='REJECT'){const reason=await soodalPrompt('거절 사유를 입력해 주세요. 입력하지 않아도 거절할 수 있습니다.','',{title:'방문견적 거절'});if(reason===null)return;await api.reject(item.id,reason.trim()||null,item.rowVersion)}else if(action==='PAYMENT'){const memo=await soodalPrompt('이체 메모를 입력해 주세요. 입력하지 않아도 됩니다.','',{title:'이체 사실 알림'});if(memo===null)return;await api.reportPayment(item.id,item.rowVersion,memo.trim()||null)}else if(action==='NO_SHOW'){const note=await soodalPrompt('연락 시도 내용을 입력해 주세요.','',{title:'전문가 노쇼 기록'});if(note===null)return;await api.noShow(item.id,item.rowVersion,'PROVIDER',note.trim()||null)}else if(action==='DISPUTE'){const reason=(await soodalPrompt('노쇼 이의 사유를 입력해 주세요.','',{title:'노쇼 이의 제기'}))?.trim();if(reason)await api.dispute(item.id,item.rowVersion,reason)}await load()}catch(e){setError(e instanceof Error?e.message:'처리하지 못했습니다.')}}
  if(items.length===0)return null
  const activeStatus=items.find(item=>['DEPARTED','ARRIVED'].includes(item.status))?.status
  return <section id="customer-site-visit-proposals" tabIndex={-1} className="detailCard siteVisitPanel"><div className="sectionHeading"><div><p className="eyebrow">방문견적</p><h2>방문견적 제안</h2><p>방문비와 결제 조건을 확인한 뒤 필요한 제안만 선택해 주세요. 방문견적 수락은 최종 작업 견적 채택이 아닙니다.</p><p className="siteVisitLiveGuide">전문가가 출발·도착 버튼을 누르면 상태와 시각이 이 화면에 자동 반영됩니다. 위치정보를 추적하거나 표시하지는 않습니다.</p></div></div>
    {activeStatus&&<p className="siteVisitProgressAlert" role="status" aria-live="polite">{activeStatus==='DEPARTED'?'전문가가 출발했습니다.':'전문가가 도착했습니다.'}</p>}{error&&<p className="errorBanner">{error}</p>}{message&&<p className="successBanner">{message}</p>}
    <div className="siteVisitList">{items.map(item=><VisitCard key={item.id} item={item}><div className="formActions">{item.canAccept&&<><button className="primaryButton" onClick={()=>void act(item,'ACCEPT')}>조건 동의·방문 확정</button><button className="secondaryButton" onClick={()=>void act(item,'REJECT')}>거절</button></>}{item.status==='ACCEPTED'&&item.paymentMode.startsWith('TRANSFER')&&item.paymentStatus!=='CONFIRMED'&&<button className="secondaryButton" onClick={()=>void act(item,'PAYMENT')}>이체 사실 알리기</button>}{['ACCEPTED','DEPARTED'].includes(item.status)&&new Date()>new Date(new Date(item.scheduledAt).getTime()+15*60000)&&<button className="dangerButton" onClick={()=>void act(item,'NO_SHOW')}>전문가 노쇼 기록</button>}{item.status==='NO_SHOW'&&<button className="secondaryButton" onClick={()=>void act(item,'DISPUTE')}>노쇼 이의 제기</button>}</div></VisitCard>)}</div>
  </section>
}
