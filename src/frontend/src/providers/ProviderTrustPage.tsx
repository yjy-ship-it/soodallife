import { useEffect, useState } from 'react'
import { ProviderAppLayout } from './ProviderAppLayout'
import { getProviderTrust, type ProviderTrustDashboard } from './trustApi'
import { TrustScoreMethodGuide } from '../trust/TrustScoreMethodGuide'

const date=(value:string|null)=>value?new Date(value).toLocaleString('ko-KR'):'아직 산정 전'
const score=(value:number|null)=>value===null?'평가 중':`${new Intl.NumberFormat('ko-KR',{maximumFractionDigits:1}).format(value)}점`
const delta=(value:number|null)=>value===null?'변동 없음':`${value>0?'+':''}${new Intl.NumberFormat('ko-KR',{maximumFractionDigits:1}).format(value)}점`
const defaultPolicyComponents=[
 {code:'EVIDENCE',name:'인증·증빙',weight:15,description:'전문가 승인 25% + 서비스 승인률 25% + 필수 증빙 검증률 40% + 증빙 유효률 10%'},
 {code:'TRANSACTION',name:'거래 이행',weight:30,description:'거래 완료율 80% + 완료 사진·증빙 등록률 20%'},
 {code:'REVIEW',name:'고객 평가',weight:30,description:'완료 거래에 연결된 검증·공개 리뷰의 항목별 평점을 100점으로 환산한 평균'},
 {code:'AFTER_SERVICE',name:'사후관리 처리',weight:10,description:'종결된 사후관리의 해결·미해결·재발·분쟁 전환 결과. 종결 이력이 없으면 기준점수 100점'},
 {code:'DISPUTE',name:'분쟁 판정',weight:10,description:'분쟁 접수 자체가 아닌 본사가 확정한 귀책유형별 정책 점수. 확정 분쟁이 없으면 기준점수 100점'},
 {code:'SANCTION',name:'운영정책 준수',weight:5,description:'현재 유효한 확정 제재의 유형별 점수. 활성 제재가 없으면 기준점수 100점'},
] as const

export function ProviderTrustPage(){
 const[data,setData]=useState<ProviderTrustDashboard|null>(null),[error,setError]=useState('')
 useEffect(()=>{getProviderTrust().then(setData).catch(reason=>setError(reason instanceof Error?reason.message:'신뢰도 정보를 불러오지 못했습니다.'))},[])
 const policyComponents=data?.policyComponents.length?data.policyComponents:defaultPolicyComponents
 return <ProviderAppLayout><section className="providerTrustHero"><div><p>전문가 신뢰도</p><h1>나의 신뢰도</h1><span>거래 이행과 고객 평가 등 본사 정책에 따라 산정된 신뢰도와 변경 이력을 확인하세요.</span></div><div><small>현재 신뢰도</small><strong>{score(data?.score??null)}</strong><b>{data?.gradeLabel??'확인 중'}</b></div></section>
 <TrustScoreMethodGuide/>{error&&<div className="providerAlert error">{error}</div>}{!data&&!error?<p className="emptyState">신뢰도 정보를 불러오는 중입니다.</p>:data&&<>
 <section className="providerTrustStatus"><div><span>현재 등급</span><strong>{data.gradeLabel}</strong></div><div><span>적용 정책</span><strong>{data.policyVersion??'확인 중'}</strong></div><div><span>최근 산정</span><strong>{date(data.calculatedAt)}</strong></div><p>{data.statusNotice}</p></section>
 <section className="providerPanel providerTrustPerformance"><div className="sectionHeading"><div><p className="eyebrow">거래 이행 현황</p><h2>실제로 반영된 완료 실적</h2></div><span>{data.transactionPerformance.earnedScore===null?'산정 준비 중':`${score(data.transactionPerformance.earnedScore)} / ${data.transactionPerformance.maximumScore}점`}</span></div><p className="providerTrustIntro">{data.transactionPerformance.notice}</p><div className="providerTrustPerformanceGrid"><article><span>전체 완료 실적</span><strong>{data.transactionPerformance.completedCount}건</strong></article><article><span>완료 증빙 충족</span><strong>{data.transactionPerformance.evidenceCount}/{data.transactionPerformance.completedCount}건</strong></article><article><span>전문가 사정 취소</span><strong>{data.transactionPerformance.providerFaultCancellationCount}건</strong></article><article><span>감점 제외 취소</span><strong>{data.transactionPerformance.neutralCancellationCount}건</strong></article></div><div className="providerTrustBreakdown"><span>일반 서비스 {data.transactionPerformance.generalServiceCount}건</span><span>긴급출동 {data.transactionPerformance.emergencyCount}건</span><span>수달 케어 {data.transactionPerformance.careVisitCount}회</span><span>수달 인테리어 {data.transactionPerformance.interiorCount}건</span></div></section>
 <section className="providerPanel"><div className="sectionHeading"><div><p className="eyebrow">등급 안내</p><h2>신뢰도 등급 안내</h2></div><span>100점 기준</span></div><p className="providerTrustIntro">신규 전문가는 평가에 필요한 거래와 검증 리뷰가 쌓이는 동안 ‘신규·평가중’으로 표시됩니다.</p><div className="providerTrustGrades">{data.grades.map(item=><article className={data.gradeCode===item.code?'active':''} key={item.code}><span>{item.minimumScore}점 이상</span><strong>{item.label}</strong><p>{item.description}</p></article>)}</div></section>
 <section className="providerPanel providerTrustPolicy"><div className="sectionHeading"><div><p className="eyebrow">신뢰도 정책</p><h2>전문가 신뢰도 운영정책</h2></div><span>공정한 근거 중심 평가</span></div><ul><li>신뢰도는 100점 만점이며 완료 거래 3건과 검증된 공개 리뷰 3건부터 산정합니다.</li><li>인증·증빙 15%, 거래 이행 30%, 고객 평가 30%, 사후관리 처리 10%, 분쟁 판정 10%, 운영정책 준수 5%를 반영합니다.</li><li>신고·사후관리·분쟁이 접수됐다는 이유만으로 감점하지 않으며 확인된 처리 결과와 최종 판정만 반영합니다.</li><li>관리자 조정은 반드시 근거와 사유를 남기며, 모든 변경 이력은 수정하거나 삭제하지 않고 보관합니다.</li><li>본사가 정책을 변경하면 새 버전으로 승인·활성화하고 적용 정책 버전을 점수와 함께 표시합니다.</li></ul></section>
 <section className="providerPanel"><div className="sectionHeading"><div><p className="eyebrow">점수 산정 기준</p><h2>점수는 이렇게 산정돼요</h2></div><span>{data.policyVersion??'기본 운영정책 안내'}</span></div><p className="providerTrustIntro">평가 항목과 반영 비중은 본사에서 활성화한 신뢰도 정책을 그대로 사용합니다. 활성 정책 조회 전에도 기본 산정 기준을 안내합니다.</p><div className="providerTrustComponents">{policyComponents.map(item=><article key={item.code}><div><strong>{item.name}</strong><b>{item.weight}%</b></div><p>{item.description}</p></article>)}</div></section>
 <section className="providerPanel"><div className="sectionHeading"><div><p className="eyebrow">점수 변경 이력</p><h2>신뢰도 점수 획득·변경 이력</h2></div><span>최근 100건</span></div><div className="providerTrustEvents">{data.events.map(item=><article key={item.id}><div><div><strong>{item.eventLabel}</strong><span>{date(item.occurredAt)}{item.policyVersion?` · ${item.policyVersion}`:''}</span></div><b className={(item.scoreDelta??0)>=0?'up':'down'}>{delta(item.scoreDelta)}</b></div><p>{item.reason}</p><small>{item.scoreBefore===null?'최초 산정':`${score(item.scoreBefore)} → ${score(item.scoreAfter)}`}{item.gradeAfterLabel?` · ${item.gradeAfterLabel}`:''}</small></article>)}{!data.events.length&&<p className="emptyState">아직 기록된 신뢰도 점수 이력이 없습니다.</p>}</div></section>
 </>}</ProviderAppLayout>
}
