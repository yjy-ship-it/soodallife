import { navigate } from '../auth/routing'
import { ProviderAppLayout } from './ProviderAppLayout'

export function ProviderSupportPage() {
  return <ProviderAppLayout><section className="providerHero"><div><p>PARTNER SUPPORT</p><h1>전문가 고객센터</h1><span>견적·거래·정산·계정 이용 중 필요한 도움을 한곳에서 확인하세요.</span></div></section>
    <section className="providerPanel"><h2>빠른 문의</h2><div className="providerDashboardGrid"><article><small>전국 대표번호</small><h3><a href="tel:16705073">1670-5073</a></h3><p>평일 09:00~18:00 · 주말·공휴일 제외</p></article><article><small>이메일 문의</small><h3><a href="mailto:soodallife@dh9.kr">soodallife@dh9.kr</a></h3><p>접수 내용과 회신받을 연락처를 함께 보내 주세요.</p></article></div></section>
    <section className="providerPanel"><h2>문의 전 준비하면 빠른 정보</h2><ul><li>요청·견적·거래·정산 화면에 표시된 번호</li><li>문제가 발생한 날짜와 시각, 이용한 기기·브라우저</li><li>오류 문구와 개인정보를 가린 화면 캡처</li></ul><p className="providerAlert">비밀번호, 카드 전체번호, 보안코드와 계좌 비밀번호는 고객센터에도 보내지 마세요.</p><div className="proposalActions"><button type="button" onClick={()=>navigate('/suggestions')}>서비스 제안·오류 접수</button><button type="button" onClick={()=>navigate('/provider/disputes')}>분쟁·이의 확인</button></div></section>
  </ProviderAppLayout>
}
