import { useEffect, useState } from 'react'
import { CustomerAppLayout } from '../customer/CustomerAppLayout'
import { ProviderAppLayout } from '../providers/ProviderAppLayout'
import { CommunicationTabs } from './ReviewConversationPages'
import './communication.css'
type Audience='customer'|'provider'
async function count(path:string){const response=await fetch(path,{credentials:'include'});if(!response.ok)return 0;return ((await response.json()) as{count:number}).count}
function Hub({audience}:{audience:Audience}){const[chat,setChat]=useState(0),[reviews,setReviews]=useState(0),root=audience==='customer'?'/customer':'/provider',api=audience==='customer'?'/api/v1/customers/me':'/api/v1/providers/me';useEffect(()=>{void Promise.all([count('/api/v1/chat/unread-count'),count(`${api}/review-conversations/unread-count`)]).then(([a,b])=>{setChat(a);setReviews(b)})},[api]);return <div className="communicationPage"><CommunicationTabs audience={audience} active="hub"/><section className="communicationHero"><p>수달 소통</p><h1>소통</h1><span>업무 정보는 비공개 채팅으로, 서비스 후기는 공개 리뷰 대화로 구분해 주세요.</span></section><section className="communicationCards"><a href={`${root}/messages`}><span>비공개 업무 소통</span><strong>업무 채팅</strong><p>주소·연락처·방문 일정·결제·A/S 세부사항을 대화합니다.</p>{chat>0&&<b>{chat>99?'99+':chat}개 읽지 않음</b>}</a><a href={`${root}/reviews`}><span>공개 후기 소통</span><strong>{audience==='customer'?'내 리뷰·댓글':'고객 리뷰·답글'}</strong><p>서비스 평가와 설명, 개선 결과를 공개 댓글로 이어갑니다.</p>{reviews>0&&<b>{reviews>99?'99+':reviews}개 읽지 않음</b>}</a></section></div>}
export function CustomerCommunicationHubPage(){return <CustomerAppLayout><Hub audience="customer"/></CustomerAppLayout>}
export function ProviderCommunicationHubPage(){return <ProviderAppLayout><Hub audience="provider"/></ProviderAppLayout>}
