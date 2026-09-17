type QuoteDisplayItem={itemName:string}

const submitterPlaceholder=/^[A-Za-z0-9._-]+\s+(?:개인|사업자)(?:이|가)?\s*제출$/u

export function quoteSummaryText(value:string|null|undefined,items:QuoteDisplayItem[],serviceName?:string){
  const current=value?.trim()??''
  if(current&&!submitterPlaceholder.test(current))return current
  const main=items[0]?.itemName?.trim()||serviceName?.trim()||'서비스'
  return items.length>1?`${main} 외 ${items.length-1}개 항목 작업 견적`:`${main} 작업 견적`
}

export function quoteTermsText(value:string|null|undefined){
  const current=value?.trim()??''
  if(current&&!submitterPlaceholder.test(current))return current
  return '세부 작업 범위와 자재·일정은 견적 항목 및 현장 조건을 기준으로 고객과 협의합니다.'
}
