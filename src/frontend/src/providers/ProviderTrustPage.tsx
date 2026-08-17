import { useEffect, useState } from 'react'
import { ProviderAppLayout } from './ProviderAppLayout'
import { getProviderTrust, type ProviderTrustDashboard } from './trustApi'

const date=(value:string|null)=>value?new Date(value).toLocaleString('ko-KR'):'아직 산정 전'
const score=(value:number|null)=>value===null?'평가 중':`${new Intl.NumberFormat('ko-KR',{maximumFractionDigits:1}).format(value)}점`
const delta=(value:number|null)=>value===null?'변동 없음':`${value>0?'+':''}${new Intl.NumberFormat('ko-KR',{maximumFractionDigits:1}).format(value)}점`

export function ProviderTrustPage(){
 const[data,setData]=useState<ProviderTrustDashboard|null>(null),[error,setError]=useState('')
 useEffect(()=>{getProviderTrust().then(setData).catch(reason=>setError(reason instanceof Error?reason.message:'신뢰도 정보를 불러오지 못했습니다.'))},[])
 return <ProviderAppLayout><section className="providerTrustHero"><div><p>PROVIDER TRUST</p><h1>나의 신뢰도</h1><span>거래 이행과 고객 평가 등 본사 정책에 따라 산정된 신뢰도와 변경 이력을 확인하세요.</span></div><div><small>현재 신뢰도</small><strong>{score(data?.score??null)}</strong><b>{data?.gradeLabel??'확인 중'}</b></div></section>
 {error&&<div className="providerAlert error">{error}</div>}{!data&&!error?<p className="emptyState">신뢰도 정보를 불러오는 중입니다.</p>:data&&<>
 <section className="providerTrustStatus"><div><span>현재 등급</span><strong>{data.gradeLabel}</strong></div><div><span>적용 정책</span><strong>{data.policyVersion??'확인 중'}</strong></div><div><span>최근 산정</span><strong>{date(data.calculatedAt)}</strong></div><p>{data.statusNotice}</p></section>
 <section className="providerPanel"><div className="sectionHeading"><div><p className="eyebrow">GRADE GUIDE</p><h2>신뢰도 등급 안내</h2></div><span>100점 기준</span></div><p className="providerTrustIntro">신규 공급자는 평가에 필요한 거래와 검증 리뷰가 쌓이는 동안 ‘신규·평가중’으로 표시됩니다.</p><div className="providerTrustGrades">{data.grades.map(item=><article className={data.gradeCode===item.code?'active':''} key={item.code}><span>{item.minimumScore}점 이상</span><strong>{item.label}</strong><p>{item.description}</p></article>)}</div></section>
 <section className="providerPanel"><div className="sectionHeading"><div><p className="eyebrow">SCORING POLICY</p><h2>점수는 이렇게 산정돼요</h2></div><span>{data.policyVersion??'정책 확인 중'}</span></div><p className="providerTrustIntro">평가 항목과 반영 비중은 본사에서 활성화한 신뢰도 정책을 그대로 사용하며 화면에서 임의로 정하지 않습니다.</p><div className="providerTrustComponents">{data.policyComponents.map(item=><article key={item.code}><div><strong>{item.name}</strong><b>{item.weight}%</b></div><p>{item.description}</p></article>)}{!data.policyComponents.length&&<p className="emptyState">적용할 신뢰도 정책을 확인하고 있습니다.</p>}</div></section>
 <section className="providerPanel"><div className="sectionHeading"><div><p className="eyebrow">SCORE HISTORY</p><h2>신뢰도 점수 획득·변경 이력</h2></div><span>최근 100건</span></div><div className="providerTrustEvents">{data.events.map(item=><article key={item.id}><div><div><strong>{item.eventLabel}</strong><span>{date(item.occurredAt)}{item.policyVersion?` · ${item.policyVersion}`:''}</span></div><b className={(item.scoreDelta??0)>=0?'up':'down'}>{delta(item.scoreDelta)}</b></div><p>{item.reason}</p><small>{item.scoreBefore===null?'최초 산정':`${score(item.scoreBefore)} → ${score(item.scoreAfter)}`}{item.gradeAfterLabel?` · ${item.gradeAfterLabel}`:''}</small></article>)}{!data.events.length&&<p className="emptyState">아직 기록된 신뢰도 점수 이력이 없습니다.</p>}</div></section>
 </>}</ProviderAppLayout>
}
