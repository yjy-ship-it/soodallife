import { useState } from 'react'
import { openBusinessChat, openSubscriptionVisitChat, type ChatResourceType } from './chatApi'

type Shortcut = { audience:'customer'|'provider'; resourceType:ChatResourceType; resourceId:string; subscriptionVisit:boolean }

function currentShortcut():Shortcut|null{
  const match=window.location.pathname.match(/^\/(customer|provider)\/(?:care\/(?:contracts|visits)|interior\/projects|after-services)\/([0-9a-f-]{36})(?:\/|$)/i)
  if(!match)return null
  const resourceType:ChatResourceType=window.location.pathname.includes('/care/contracts/')?'SUBSCRIPTION':window.location.pathname.includes('/interior/projects/')?'INTERIOR':'AFTER_SERVICE'
  return {audience:match[1] as Shortcut['audience'],resourceType,resourceId:match[2],subscriptionVisit:window.location.pathname.includes('/care/visits/')}
}

export function BusinessChatShortcut(){
  const shortcut=currentShortcut(),[error,setError]=useState(''),[busy,setBusy]=useState(false)
  if(!shortcut)return null
  const open=async()=>{setBusy(true);setError('');try{
    if(shortcut.subscriptionVisit)await openSubscriptionVisitChat(shortcut.audience,shortcut.resourceId)
    else if(shortcut.resourceType==='INTERIOR'){
      try{await openBusinessChat(shortcut.audience,'INTERIOR',shortcut.resourceId,'PRIMARY_CONTRACTOR')}
      catch{await openBusinessChat(shortcut.audience,'INTERIOR',shortcut.resourceId,'SITE_SURVEY')}
    }else await openBusinessChat(shortcut.audience,shortcut.resourceType,shortcut.resourceId)
  }catch(reason){setError(reason instanceof Error?reason.message:'현재 담당자와 채팅을 시작할 수 없습니다.')}finally{setBusy(false)}}
  return <aside className="businessChatShortcut"><button type="button" disabled={busy} onClick={()=>void open()}>{busy?'채팅 확인 중…':'담당자와 채팅'}</button>{error&&<span role="alert">{error}</span>}</aside>
}
