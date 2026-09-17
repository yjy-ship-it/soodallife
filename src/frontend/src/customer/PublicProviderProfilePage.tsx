import { useEffect,useState,type SyntheticEvent } from 'react'
import { useAuthentication } from '../auth/AuthenticationContext'
import { createLoginPath,navigate } from '../auth/routing'
import { chatApi } from '../chat/chatApi'
import { apiUrl } from '../config/apiEndpoint'
import { CustomerAppLayout } from './CustomerAppLayout'
import './publicProviderProfile.css'

type Rating={name:string;average:number;count:number;minValue:number;maxValue:number}
type Performance={serviceName:string;completedCount:number}
type Review={id:string;bodyText:string;submittedAt:string;averageRating:number|null;providerReply:string|null}
type Campaign={id:string;title:string;subtitle:string|null;bodyText:string|null;startAt:string;endAt:string|null}
type Profile={id:string;businessName:string;introductionHtml:string|null;logoUrl:string|null;photoUrls:string[];blogUrl:string|null;websiteUrl:string|null;trustScore:number|null;trustGrade:string|null;trustDisplay:string;completedServiceCount:number;publicReviewCount:number;ratingItems:Rating[];activeServices:string[];performance:Performance[];recentReviews:Review[];campaign:Campaign|null}

async function getProfile(providerId:string,campaignId:string|null){const query=campaignId?`?campaignId=${encodeURIComponent(campaignId)}`:'';const response=await fetch(`/api/v1/public/providers/${providerId}${query}`,{credentials:'include'});if(!response.ok){const body=await response.json().catch(()=>null) as {message?:string}|null;throw new Error(body?.message??'전문가 소개를 불러오지 못했습니다.')}return response.json() as Promise<Profile>}

export function PublicProviderProfilePage({providerId}:{providerId:string}){
 const{status,user}=useAuthentication(),[profile,setProfile]=useState<Profile|null>(null),[error,setError]=useState(''),[busy,setBusy]=useState(false)
 const campaignId=new URLSearchParams(window.location.search).get('campaign')
 useEffect(()=>{getProfile(providerId,campaignId).then(setProfile).catch((reason:Error)=>setError(reason.message))},[providerId,campaignId])
 const consult=async()=>{if(status!=='authenticated'||!user){navigate(createLoginPath(`${window.location.pathname}${window.location.search}`));return}if(!user.roles.includes('CUSTOMER')){setError('채팅 상담은 고객 계정으로 이용해 주세요.');return}setBusy(true);setError('');try{const room=await chatApi.providerConsultationRoom(providerId);navigate(`/customer/messages/${room.id}`)}catch(reason){setError((reason as Error).message)}finally{setBusy(false)}}
 if(!profile)return <CustomerAppLayout><section className="publicProviderLoading"><h1>{error||'전문가 소개를 불러오는 중입니다…'}</h1></section></CustomerAppLayout>
 return <CustomerAppLayout><article className="publicProviderPage">
  <header className="publicProviderHero"><div className="publicProviderIdentity"><img src={publicImage(profile.logoUrl)} alt={`${profile.businessName} 로고`} onError={fallbackImage}/><div><p>수달 라이프 전문가</p><h1>{profile.businessName}</h1><strong>{profile.trustDisplay}</strong></div></div><button type="button" disabled={busy} onClick={()=>void consult()}>{busy?'채팅 준비 중…':'채팅 상담'}</button></header>
  {error&&<p className="publicProviderError">{error}</p>}
  {profile.campaign&&<section className="publicProviderCampaign"><p>광고 상세 설명</p><h2>{profile.campaign.title}</h2>{profile.campaign.subtitle&&<strong>{profile.campaign.subtitle}</strong>}<div>{profile.campaign.bodyText||'등록된 상세 설명이 없습니다.'}</div><small>게시 기간 {date(profile.campaign.startAt)} ~ {profile.campaign.endAt?date(profile.campaign.endAt):'종료일 없음'}</small></section>}
  <div className="publicProviderGrid"><main>
   <section><h2>전문가 소개</h2>{profile.introductionHtml?<div className="publicProviderIntroduction" dangerouslySetInnerHTML={{__html:sanitize(profile.introductionHtml)}}/>:<p>등록된 소개 내용이 없습니다.</p>}{(profile.websiteUrl||profile.blogUrl)&&<div className="publicProviderLinks">{profile.websiteUrl&&<a href={profile.websiteUrl} target="_blank" rel="noreferrer">공식 웹사이트</a>}{profile.blogUrl&&<a href={profile.blogUrl} target="_blank" rel="noreferrer">블로그</a>}</div>}</section>
   {profile.photoUrls.length>0&&<section><h2>홍보 사진</h2><div className="publicProviderPhotos">{profile.photoUrls.map((url,index)=><img src={publicImage(url)} alt={`${profile.businessName} 홍보 사진 ${index+1}`} onError={fallbackImage} key={url}/>)}</div></section>}
   <section><h2>고객 리뷰</h2><p className="publicProviderReviewSummary">검증된 공개 리뷰 <b>{profile.publicReviewCount}건</b></p>{profile.ratingItems.length>0&&<div className="publicProviderRatings">{profile.ratingItems.map(item=><div key={item.name}><span>{item.name}</span><b>{item.average.toFixed(1)} / {item.maxValue}</b><small>{item.count}건</small></div>)}</div>}<div className="publicProviderReviews">{profile.recentReviews.map(review=><blockquote key={review.id}><div>{review.averageRating&&<strong>★ {review.averageRating.toFixed(1)}</strong>}<time>{date(review.submittedAt)}</time></div><p>{review.bodyText}</p>{review.providerReply&&<footer><b>전문가 답글</b>{review.providerReply}</footer>}</blockquote>)}{!profile.recentReviews.length&&<p>아직 공개된 고객 리뷰가 없습니다.</p>}</div></section>
  </main><aside>
   <section><h2>활동 현황</h2><dl><div><dt>완료 서비스</dt><dd>{profile.completedServiceCount}건</dd></div><div><dt>공개 리뷰</dt><dd>{profile.publicReviewCount}건</dd></div><div><dt>신뢰도</dt><dd>{profile.trustDisplay}</dd></div></dl></section>
   <section><h2>서비스 분야</h2><div className="publicProviderTags">{profile.activeServices.map(name=><span key={name}>{name}</span>)}{!profile.activeServices.length&&<p>공개 가능한 서비스가 없습니다.</p>}</div></section>
   <section><h2>수주·완료 실적</h2>{profile.performance.map(item=><div className="publicProviderPerformance" key={item.serviceName}><span>{item.serviceName}</span><b>{item.completedCount}건</b></div>)}{!profile.performance.length&&<p>완료 실적을 집계 중입니다.</p>}</section>
   <button className="publicProviderConsult" type="button" disabled={busy} onClick={()=>void consult()}>{busy?'채팅 준비 중…':'채팅 상담 신청'}</button><small className="publicProviderNotice">상담을 신청하면 전문가에게 웹 알림이 생성됩니다. 알림톡 채널은 운영 설정이 활성화된 경우 함께 전송됩니다.</small>
  </aside></div>
 </article></CustomerAppLayout>
}

const date=(value:string)=>new Intl.DateTimeFormat('ko-KR',{dateStyle:'medium'}).format(new Date(value))
const fallback='/brand/soodal-life-mark.png'
const publicImage=(value:string|null)=>value?apiUrl(value):fallback
const fallbackImage=(event:SyntheticEvent<HTMLImageElement>)=>{const image=event.currentTarget;if(!image.src.endsWith(fallback)){image.src=fallback;image.classList.add('fallback')}}
function sanitize(value:string){const documentValue=new DOMParser().parseFromString(value,'text/html');documentValue.querySelectorAll('script,iframe,object,embed,form,input,button,style,link,meta').forEach(node=>node.remove());documentValue.body.querySelectorAll('*').forEach(element=>{for(const attribute of Array.from(element.attributes)){const name=attribute.name.toLowerCase();if(name==='style'){const safe=attribute.value.split(';').filter(rule=>/^(color|font-weight|font-style|text-decoration|text-align)\s*:/i.test(rule.trim())).join(';');if(safe)element.setAttribute('style',safe);else element.removeAttribute('style')}else if(name==='href'&&element.tagName==='A'&&/^https:\/\//i.test(attribute.value)){element.setAttribute('target','_blank');element.setAttribute('rel','noreferrer')}else element.removeAttribute(attribute.name)}});return documentValue.body.innerHTML}
