import {
  familySites,
  serviceCompany,
  servicePolicyLinks,
} from '../config/serviceCompany'
import './ServiceFooter.css'

type ServiceFooterProps = {
  variant: 'customer' | 'provider' | 'admin'
}

function PendingValue() {
  return <span className="serviceFooterPending">확정 전</span>
}

function ContactLink({ href, children }: { href: string | null; children: string | null }) {
  return href && children ? <a href={href}>{children}</a> : <PendingValue />
}

export function ServiceFooter({ variant }: ServiceFooterProps) {
  const audienceLabel = {
    customer: '고객 서비스',
    provider: '공급자 서비스',
    admin: '본사 관리자 서비스',
  }[variant]
  return (
    <footer className="serviceFooter" data-variant={variant} aria-label={`${audienceLabel} 공통 푸터`}>
      <div className="serviceFooterInner">
        <nav className="serviceFooterPolicies" aria-label="서비스 정책 및 고객지원">
          {servicePolicyLinks.map((link) =>
            link.href ? (
              <a key={link.label} className={link.emphasized ? 'isEmphasized' : undefined} href={link.href}>
                {link.label}
              </a>
            ) : (
              <span
                key={link.label}
                className={link.emphasized ? 'isEmphasized isPending' : 'isPending'}
                aria-disabled="true"
                title="페이지 준비 중"
              >
                {link.label}<small>준비 중</small>
              </span>
            ),
          )}
        </nav>

        <div className="serviceFooterDivider" />

        <div className="serviceFooterGrid">
          <section className="serviceFooterCompany" aria-labelledby="service-footer-company">
            <div className="serviceFooterTitleRow">
              <h2 id="service-footer-company">
                {serviceCompany.serviceName} <span aria-hidden="true">|</span> {serviceCompany.serviceNameEnglish}
              </h2>
              <strong className="verifiedBadge">사업자등록증명 확인</strong>
            </div>
            <p>{serviceCompany.companyName}</p>
            <dl className="serviceFooterDetails">
              <div><dt>대표</dt><dd>{serviceCompany.ceoName}</dd></div>
              <div><dt>사업자등록번호</dt><dd>{serviceCompany.businessRegistrationNumber}</dd></div>
              <div><dt>통신판매업 신고번호</dt><dd>{serviceCompany.ecommerceRegistrationNumber ?? <PendingValue />}</dd></div>
              <div className="serviceFooterAddress"><dt>주소</dt><dd>{serviceCompany.address}</dd></div>
            </dl>
          </section>

          <section className="serviceFooterContact" aria-labelledby="service-footer-support">
            <h3 id="service-footer-support">고객센터</h3>
            <ContactLink href={serviceCompany.representativePhone ? `tel:${serviceCompany.representativePhone.replaceAll('-', '')}` : null}>
              {serviceCompany.representativePhone}
            </ContactLink>
            <div className="serviceFooterHours">
              {serviceCompany.customerServiceHours?.map((hours) => <span key={hours}>{hours}</span>) ?? <span>운영시간 확정 전</span>}
            </div>
            <ContactLink href={serviceCompany.customerServiceEmail ? `mailto:${serviceCompany.customerServiceEmail}` : null}>
              {serviceCompany.customerServiceEmail}
            </ContactLink>
          </section>

          <section className="serviceFooterContact" aria-labelledby="service-footer-partnership">
            <h3 id="service-footer-partnership">패밀리 사이트</h3>
            {familySites.map(site => <a href={site.href} target="_blank" rel="noopener noreferrer" key={site.href}>{site.label}</a>)}
            <h3 className="serviceFooterSubheading">개인정보보호 문의</h3>
            <ContactLink href={`tel:${serviceCompany.privacyContact.replaceAll('-', '')}`}>
              {serviceCompany.privacyContact}
            </ContactLink>
          </section>
        </div>

        <aside className="serviceFooterNotice" aria-label="플랫폼 안내">
          {serviceCompany.platformNotice}
        </aside>
        <p className="serviceFooterCopyright">{serviceCompany.copyright}</p>
      </div>
    </footer>
  )
}
