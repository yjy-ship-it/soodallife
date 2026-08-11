import {
  businessInformationVerificationUrl,
  serviceCompany,
  servicePolicyLinks,
} from '../config/serviceCompany'
import './ServiceFooter.css'

type ServiceFooterProps = {
  variant: 'customer' | 'provider'
}

function ContactLink({ href, children }: { href: string; children: string }) {
  return <a href={href}>{children}</a>
}

export function ServiceFooter({ variant }: ServiceFooterProps) {
  const audienceLabel = variant === 'customer' ? '고객 서비스' : '공급자 서비스'

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
              {serviceCompany.isPlaceholder && <strong className="placeholderBadge">개발용 임시정보</strong>}
            </div>
            <p>{serviceCompany.companyName}</p>
            <dl className="serviceFooterDetails">
              <div><dt>대표</dt><dd>{serviceCompany.ceoName}</dd></div>
              <div><dt>사업자등록번호</dt><dd>{serviceCompany.businessRegistrationNumber}</dd></div>
              <div><dt>통신판매업 신고번호</dt><dd>{serviceCompany.ecommerceRegistrationNumber}</dd></div>
              <div className="serviceFooterAddress"><dt>주소</dt><dd>{serviceCompany.address}</dd></div>
            </dl>
            {businessInformationVerificationUrl ? (
              <a className="businessVerification" href={businessInformationVerificationUrl} target="_blank" rel="noreferrer">
                사업자정보 확인
              </a>
            ) : (
              <span className="businessVerification isPending" aria-disabled="true">
                사업자정보 확인 <small>준비 중</small>
              </span>
            )}
          </section>

          <section className="serviceFooterContact" aria-labelledby="service-footer-support">
            <h3 id="service-footer-support">고객센터</h3>
            <ContactLink href={`tel:${serviceCompany.representativePhone.replaceAll('-', '')}`}>
              {serviceCompany.representativePhone}
            </ContactLink>
            <div className="serviceFooterHours">
              {serviceCompany.customerServiceHours.map((hours) => <span key={hours}>{hours}</span>)}
            </div>
            <ContactLink href={`mailto:${serviceCompany.customerServiceEmail}`}>
              {serviceCompany.customerServiceEmail}
            </ContactLink>
          </section>

          <section className="serviceFooterContact" aria-labelledby="service-footer-partnership">
            <h3 id="service-footer-partnership">사업제휴</h3>
            <ContactLink href={`mailto:${serviceCompany.partnershipEmail}`}>
              {serviceCompany.partnershipEmail}
            </ContactLink>
            <h3 className="serviceFooterSubheading">개인정보보호 문의</h3>
            <ContactLink href={`mailto:${serviceCompany.privacyEmail}`}>
              {serviceCompany.privacyEmail}
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
