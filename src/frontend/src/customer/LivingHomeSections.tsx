import { createLoginPath,navigate } from '../auth/routing'
import type { LivingHome } from './types'
import './livingHome.css'

export function LivingHomeLead({value,error,reload}:{value:LivingHome|null;error:string;reload:()=>void}){
  const checks=value?.dailyChecks??instantDailyChecks()
  return <>
    <section className="customerSection livingToday"><header><div><p>오늘의 수달</p><h2>오늘의 생활점검</h2><span>문제가 생기기 전에 3분만 확인해 보세요.</span></div><b>{checks[0]?.seasonLabel}</b></header><div className="livingCheckGrid">{checks.map((item,index)=><article key={item.id}><i>{index+1}</i><div><strong>{item.title}</strong><p>{item.body}</p><button type="button" onClick={()=>navigate(item.servicePath)}>{item.actionLabel} ›</button></div></article>)}</div>{error&&<p className="livingLoad"><span>{error}</span><button type="button" onClick={reload}>다시 확인</button></p>}</section>
    {value&&<LocalInsights value={value}/>}
  </>
}

export function LivingHomeAfterActivity({signedIn,value}:{signedIn:boolean;value:LivingHome|null}){
  if(!value)return null
  const request=()=>navigate(signedIn?'/customer/requests/new':createLoginPath('/customer/requests/new'))
  return <>
    <section className="customerSection livingCalendar"><header><div><p>우리 집 관리</p><h2>우리 집 관리 예정 항목</h2><span>{value.isPersonalized?'내 실제 서비스 이력과 등록 설비를 기준으로 계산했습니다.':'로그인하면 내 서비스 이력에 맞춰 관리 시기를 알려드립니다.'}</span></div>{!signedIn&&<button type="button" onClick={()=>navigate(createLoginPath('/'))}>로그인</button>}</header><div className="livingCalendarList">{value.maintenanceCalendar.map(item=><button type="button" key={item.id} onClick={()=>navigate(item.servicePath)}><time>{date(item.dueDate)}</time><span><em className={item.statusCode.toLowerCase()}>{status(item.statusCode)}</em><strong>{item.title}</strong><small>{item.detail} · {item.sourceLabel}</small></span><b>확인 ›</b></button>)}</div></section>
    {value.workStories.length>0&&<section className="customerSection livingStories"><header><div><p>실제 서비스 사례</p><h2>실제 작업·후기 사례</h2><span>완료된 거래에 등록된 공개 후기만 보여드립니다.</span></div></header><div>{value.workStories.slice(0,4).map(item=><article key={item.id}><span><b>작업 완료</b><time>{date(item.completedAt)}</time></span><h3>{item.serviceName}</h3><p>“{item.reviewText}”</p><footer><small>{item.regionName} · {item.providerName}{item.rating?` · 평점 ${item.rating}`:''}</small><button type="button" onClick={()=>navigate(item.servicePath)}>비슷한 서비스 보기</button></footer></article>)}</div></section>}
    {value.expertAnswers.length>0&&<section className="customerSection livingAnswers"><header><div><p>전문가 1분 답변</p><h2>전문가 1분 답변</h2><span>공개된 고객 질문에 실제 전문가가 남긴 답변입니다.</span></div></header><div>{value.expertAnswers.slice(0,4).map(item=><article key={item.id}><small>{item.serviceName} · {item.providerName}</small><h3>질문. {item.question}</h3><p>답변. {item.answer}</p><button type="button" onClick={()=>navigate(item.servicePath)}>관련 서비스 보기 ›</button></article>)}</div></section>}
    <section className="livingRequestCta"><div><p>우리 집과 비슷한 문제가 있나요?</p><h2>사진과 증상을 등록하면 내 조건에 맞는 전문가의 견적을 받을 수 있습니다.</h2><span>{value.privacyNotice}</span></div><button type="button" onClick={request}>비슷한 서비스 요청하기</button></section>
  </>
}

function LocalInsights({value}:{value:LivingHome}){if(!value.localInsights.length)return null;return <section className="customerSection livingPrices"><header><div><p>지역 실제 가격</p><h2>{value.regionName} 실제 거래·가격정보</h2><span>개별 거래금액이 아닌 충분한 표본의 가격 범위만 제공합니다.</span></div></header><div>{value.localInsights.map(item=><button type="button" key={`${item.regionName}-${item.serviceName}`} onClick={()=>navigate(item.servicePath)}><span>{item.regionName} · 최근 {item.samplePeriodDays}일</span><strong>{item.serviceName}</strong><b>{price(item)}</b><small>완료 거래 {item.completedCount}건 · {item.evidenceLabel}</small></button>)}</div></section>}
const date=(value:string)=>new Intl.DateTimeFormat('ko-KR',{month:'short',day:'numeric'}).format(new Date(value.length===10?`${value}T00:00:00`:value))
const status=(value:string)=>value==='OVERDUE'?'점검 시기 지남':value==='DUE_SOON'?'곧 점검':'예정'
const price=(item:LivingHome['localInsights'][number])=>item.typicalMinAmount==null||item.typicalMaxAmount==null?'가격 표본 준비 중':item.currencyCode==='KRW'?`${item.typicalMinAmount.toLocaleString()}원 ~ ${item.typicalMaxAmount.toLocaleString()}원`:`${item.typicalMinAmount.toLocaleString()} ~ ${item.typicalMaxAmount.toLocaleString()} ${item.currencyCode}`
function instantDailyChecks():LivingHome['dailyChecks']{
  const month=new Date().getMonth()+1
  const season=month===12||month<=2?'겨울 점검':month<=5?'봄 점검':month<=8?'여름 점검':'가을 점검'
  const values=month===12||month<=2?[['freeze','수도·보일러 동파 징후가 없나요?','외부 수도와 보일러 배관 보온 상태를 3분만 확인해 보세요.','동파'],['lock','현관문 잠금장치가 뻑뻑하지 않나요?','추운 날에는 배터리 성능과 잠금쇠 정렬 상태를 함께 확인하세요.','도어락'],['electric','전열기 사용 전 콘센트를 확인하세요','변색·탄 냄새·헐거움이 있으면 사용을 멈추고 점검이 필요합니다.','전기 점검']]:month<=5?[['air','에어컨을 켜기 전 필터를 확인하세요','필터 먼지와 실외기 주변 장애물을 미리 정리하면 고장을 줄일 수 있습니다.','에어컨 청소'],['screen','방충망과 창호 틈새를 확인하세요','찢어진 망과 창틀 틈을 미리 보수하면 벌레와 빗물 유입을 줄일 수 있습니다.','방충망'],['bath','욕실 실리콘에 검은 점이 생겼나요?','곰팡이가 반복되거나 들뜬 부분은 재시공 시기를 확인해 보세요.','욕실 실리콘']]:month<=8?[['drain','장마 전 창틀 배수구를 확인하세요','먼지로 막힌 배수구는 실내 누수의 원인이 될 수 있습니다.','창호'],['air','에어컨 냄새와 물 떨어짐을 확인하세요','필터 청소 후에도 냄새나 누수가 계속되면 전문가 점검이 필요합니다.','에어컨'],['leak','천장·벽지에 번진 자국이 없나요?','젖은 자국이 커지면 사진을 남기고 누수 위치를 조기에 확인하세요.','누수']]:[['boiler','난방 전 보일러를 시험 가동하세요','소음·누수·에러코드를 미리 확인하면 갑작스러운 고장을 줄일 수 있습니다.','보일러'],['seal','창문 틈바람을 확인하세요','창호 고무와 실리콘 들뜸을 확인하면 난방 손실을 줄일 수 있습니다.','창호'],['hood','주방 후드 기름때를 확인하세요','흡입력이 약해졌다면 필터 청소 또는 교체 시기일 수 있습니다.','후드 청소']]
  return values.map(([id,title,body,query])=>({id,seasonLabel:season,title,body,actionLabel:'점검 방법·서비스 보기',servicePath:`/services/search?q=${encodeURIComponent(query)}`}))
}
