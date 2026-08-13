import { useEffect, useState } from 'react'

interface DirectPayment {
  id:string;statusCode:string;amount:number;currencyCode:string;paymentMethodCode:string;paidAt:string;
  registeredByRoleCode:string;registeredAt:string;decidedAt:string|null;rejectionReason:string|null
}
interface TransactionDetail { directPayment:DirectPayment|null }

export function AdminDirectPaymentStatus() {
  const match = window.location.pathname.match(/^\/admin\/transactions\/([0-9a-f-]{36})$/i)
  const id = match?.[1]
  const [payment, setPayment] = useState<DirectPayment | null | undefined>(undefined)
  useEffect(() => {
    if (!id) return
    fetch(`/api/v1/admin/transactions/${id}`, { credentials:'include' })
      .then(response => response.ok ? response.json() as Promise<TransactionDetail> : Promise.reject())
      .then(value => setPayment(value.directPayment)).catch(() => setPayment(null))
  }, [id])
  if (!id) return null
  return <section className="adminSection">
    <h2>직접지급 확인</h2>
    {payment === undefined && <p>지급 확인 기록을 불러오는 중입니다.</p>}
    {payment === null && <p>등록된 직접지급 확인 기록이 없습니다.</p>}
    {payment && <div className="customerInfoGrid">
      <div><dt>상태</dt><dd>{payment.statusCode}</dd></div><div><dt>금액</dt><dd>{money(payment.amount, payment.currencyCode)}</dd></div>
      <div><dt>지급수단</dt><dd>{payment.paymentMethodCode}</dd></div><div><dt>실제 지급일시</dt><dd>{date(payment.paidAt)}</dd></div>
      <div><dt>등록자 역할</dt><dd>{payment.registeredByRoleCode}</dd></div><div><dt>상대방 결정</dt><dd>{payment.decidedAt ? date(payment.decidedAt) : '대기'}</dd></div>
      {payment.rejectionReason && <div><dt>거절 사유</dt><dd>{payment.rejectionReason}</dd></div>}
    </div>}
    <p className="customerNotice">본사는 지급 사실과 상태만 조회합니다. 지급 실행·환불·정산 조정 기능은 제공하지 않습니다.</p>
  </section>
}
const money=(value:number,currency:string)=>new Intl.NumberFormat('ko-KR',{style:'currency',currency,maximumFractionDigits:0}).format(value)
const date=(value:string)=>new Intl.DateTimeFormat('ko-KR',{dateStyle:'medium',timeStyle:'short'}).format(new Date(value))
