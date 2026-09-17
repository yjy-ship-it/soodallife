import {
  useEffect,
  useMemo,
  useRef,
  useState,
  type FormEvent,
  type PropsWithChildren,
} from "react";
import { useAuthentication } from "../auth/AuthenticationContext";
import { createLoginPath, navigate } from "../auth/routing";
import { CustomerAppLayout } from "./CustomerAppLayout";
import { loadDefaultAddress } from "./defaultAddress";
import { customerAccountApi } from "./accountApi";
import type { AdministrativeArea } from "./accountTypes";
import { careApi } from "./careApi";
import { openBusinessChat } from "../chat/chatApi";
import { interiorApi } from "./interiorApi";
import type {
  BillingRegistration,
  CareApplication,
  CareContract,
  CareHome,
  CareRequest,
  CareVisit,
  CareVisitDetail,
  PaymentHistory,
  PaymentMethod,
  SubscriptionService,
} from "./careTypes";
import "./customerCare.css";
import "./customerCareVisuals.css";
import { soodalConfirm, soodalPrompt } from '../components/soodalDialog'
import { ServiceThumbnail, useServiceVisualSettings } from '../serviceVisuals/ServiceVisual'
type TossPaymentClient={requestBillingAuth:(input:{method:'CARD';successUrl:string;failUrl:string})=>Promise<void>}
type TossPaymentsFactory=(clientKey:string)=>{payment:(input:{customerKey:string})=>TossPaymentClient}
declare global{interface Window{TossPayments?:TossPaymentsFactory}}
const loadTossPayments=async()=>{if(window.TossPayments)return window.TossPayments;await new Promise<void>((resolve,reject)=>{const existing=document.querySelector<HTMLScriptElement>('script[data-soodal-toss]');if(existing){existing.addEventListener('load',()=>resolve(),{once:true});existing.addEventListener('error',()=>reject(new Error('결제창을 불러오지 못했습니다.')),{once:true});return}const script=document.createElement('script');script.src='https://js.tosspayments.com/v2/standard';script.async=true;script.dataset.soodalToss='true';script.onload=()=>resolve();script.onerror=()=>reject(new Error('결제창을 불러오지 못했습니다.'));document.head.appendChild(script)});if(!window.TossPayments)throw new Error('결제창을 초기화하지 못했습니다.');return window.TossPayments}
const money = (value: number | null, currency = "KRW") =>
  value == null
    ? "정책/제안 확인"
    : `${new Intl.NumberFormat("ko-KR").format(value)} ${currency === "KRW" ? "원" : currency}`;
const date = (value: string | null) =>
  value
    ? new Intl.DateTimeFormat("ko-KR", {
        dateStyle: "medium",
        timeStyle: value.includes("T") ? "short" : undefined,
      }).format(new Date(value))
    : "—";
const message = (reason: unknown) =>
  reason instanceof Error ? reason.message : "요청을 처리하지 못했습니다.";
const sortSubscriptionServices = (items: SubscriptionService[]) =>
  [...items].sort((left, right) =>
    left.majorName.localeCompare(right.majorName, "ko-KR") ||
    left.middleName.localeCompare(right.middleName, "ko-KR") ||
    left.serviceName.localeCompare(right.serviceName, "ko-KR"),
  );
const online = () => {
  if (!navigator.onLine)
    throw new Error(
      "변경 작업은 인터넷에 연결된 상태에서만 실행할 수 있습니다.",
    );
};
const frequency = (value: string) =>
  ({
    WEEKLY: "주간",
    BIWEEKLY: "격주",
    MONTHLY: "월간",
    QUARTERLY: "분기",
    HALF_YEARLY: "반기",
  })[value] ?? "반복주기 확인";
const weekdayNames = ["일", "월", "화", "수", "목", "금", "토"];
const amountDigits = (value: string) => value.replace(/\D/g, "").replace(/^0+(?=\d)/, "");
const recurrenceSummary = (rule: { frequencyTypeCode: string; intervalValue: number; weekdays?: number[] | null }) => {
  const days = (rule.weekdays ?? []).map((day) => weekdayNames[day]).filter(Boolean);
  if (rule.frequencyTypeCode === "WEEKLY") return `${rule.intervalValue === 1 ? "매주" : `${rule.intervalValue}주마다`} · ${days.join("·") || "요일 미선택"} · 주 ${days.length}회`;
  if (rule.frequencyTypeCode === "BIWEEKLY") return `${rule.intervalValue === 1 ? "격주" : `${rule.intervalValue * 2}주마다`} · ${days.join("·") || "요일 미선택"} · 해당 주 ${days.length}회`;
  return `${frequency(rule.frequencyTypeCode)} · ${rule.intervalValue === 1 ? "매 주기" : `${rule.intervalValue}주기마다`}`;
};
const calendarDate = (value: string | null | undefined) => {
  if (!value) return "—";
  const [year, month, day] = value.slice(0, 10).split("-");
  return year && month && day ? `${year}년 ${Number(month)}월 ${Number(day)}일` : value;
};
const clockTime = (value: unknown) => typeof value === "string" && value
  ? value.slice(0, 5)
  : null;
const careStatusLabel = (code: string) => ({
  OPEN: "전문가 모집 중", DRAFT: "작성 중", CONTRACTED: "계약 완료", CANCELLED: "취소",
  EXPIRED: "모집 종료", CLOSED: "모집 종료", SUBMITTED: "제안 제출", SELECTED: "선택 완료",
  NOT_SELECTED: "미선택", WITHDRAWN: "철회", PAYMENT_PENDING: "첫 결제 대기", ACTIVE: "이용 중",
  PAUSED: "일시정지", TERMINATION_REQUESTED: "해지 처리 중", TERMINATED: "해지 완료",
  SCHEDULED: "방문 예정", RESCHEDULED: "일정 변경 완료", IN_PROGRESS: "작업 중",
  PROVIDER_COMPLETED: "완료 확인 대기", COMPLETED: "완료", SKIPPED: "건너뜀",
  DISPUTED: "분쟁 확인 중", REQUESTED: "요청", APPROVED: "승인", REJECTED: "반려",
} as Record<string, string>)[code] ?? "상태 확인 중";
const requestStatus = (code: string, selected: boolean) => selected ? "전문가 선택·계약 완료" : careStatusLabel(code);
const parseSnapshot = (raw: string): unknown => {
  let value: unknown = raw;
  for (let attempt = 0; attempt < 4 && typeof value === "string"; attempt += 1) {
    const text = value.trim();
    if (!text || (!text.startsWith("{") && !text.startsWith("["))) break;
    try { value = JSON.parse(text); } catch { break; }
  }
  return value;
};
const snapshotObject = (value: unknown): Record<string, unknown> | null =>
  value != null && typeof value === "object" && !Array.isArray(value)
    ? value as Record<string, unknown>
    : null;
const snapshotField = (value: Record<string, unknown> | null, ...names: string[]) => {
  if (!value) return undefined;
  const key = Object.keys(value).find((item) => names.some((name) => item.toLowerCase() === name.toLowerCase()));
  return key ? value[key] : undefined;
};
const scopeSnapshot = (raw: string) => {
  let root = snapshotObject(parseSnapshot(raw));
  const nested = parseSnapshot(String(snapshotField(root, "requested") ?? ""));
  const nestedObject = snapshotObject(nested);
  if (nestedObject && ["requested", "proposed", "product"].some((key) => snapshotField(nestedObject, key) !== undefined)) root = nestedObject;
  const text = (name: string) => {
    const value = snapshotField(root, name);
    return typeof value === "string" && value.trim() ? value.trim() : null;
  };
  return { requested: text("requested"), proposed: text("proposed"), product: text("product") };
};
const recurrenceSnapshot = (raw: string) => snapshotObject(parseSnapshot(raw));
function CareContractSummaryCard({ item }: { item: CareContract }) {
  const rule = recurrenceSnapshot(item.recurrenceSnapshotJson);
  const ruleValue = (...names: string[]) => snapshotField(rule, ...names);
  const weekdays = Array.isArray(ruleValue("Weekdays")) ? (ruleValue("Weekdays") as unknown[]).map(Number).filter((value) => Number.isInteger(value) && value >= 0 && value <= 6) : [];
  const frequencyCode = String(ruleValue("FrequencyTypeCode") ?? "");
  const interval = Number(ruleValue("IntervalValue") ?? 1);
  const duration = Number(ruleValue("ExpectedDurationMinutes") ?? 0);
  const timeFrom = clockTime(ruleValue("PreferredTimeFrom"));
  const timeTo = clockTime(ruleValue("PreferredTimeTo"));
  const endDate = typeof ruleValue("EndDate") === "string" ? String(ruleValue("EndDate")) : null;
  const requestedAmount = item.requestPriceNegotiable
    ? "금액 협의"
    : item.requestedMonthlyAmount != null
      ? `월 ${money(item.requestedMonthlyAmount)}`
      : item.requestedVisitAmount != null
        ? `회차 ${money(item.requestedVisitAmount)}`
        : "희망 금액 미입력";
  return (
    <button className="careContractSummaryCard" onClick={() => navigate(`/customer/care/contracts/${item.id}`)}>
      <header><span>{item.contractNumber}</span><b>{item.statusDisplay}</b></header>
      <strong>{item.serviceName}</strong>
      <p>{item.requestedScope || scopeSnapshot(item.serviceScopeSnapshotJson).requested || "요청 내용 없음"}</p>
      <dl>
        <div><dt>서비스 지역</dt><dd>{item.requestAreaName}</dd></div>
        <div><dt>고객 희망 비용</dt><dd>{requestedAmount}</dd></div>
        <div><dt>반복 주기</dt><dd>{recurrenceSummary({ frequencyTypeCode: frequencyCode, intervalValue: interval, weekdays })}</dd></div>
        <div><dt>희망 시간</dt><dd>{timeFrom ?? "시간 협의"}{timeTo ? `~${timeTo}` : ""}{duration > 0 ? ` · ${duration}분` : ""}</dd></div>
        <div><dt>이용 기간</dt><dd>{calendarDate(item.preferredStartDate)} ~ {calendarDate(endDate)}</dd></div>
        <div><dt>선택한 전문가</dt><dd>{item.providerName}</dd></div>
      </dl>
      <footer>다음 방문 {item.nextVisitAt ? date(item.nextVisitAt) : "예정 없음"}</footer>
    </button>
  );
}
const paymentStatusLabel = (value: string) => ({ REQUESTED: "결제 요청 준비", PROCESSING: "결제 처리 중", COMPLETED: "결제 완료", FAILED: "결제 실패", CANCELLED: "결제 취소", REFUNDED: "환불 완료", PARTIALLY_REFUNDED: "일부 환불" } as Record<string,string>)[value] ?? "상태 확인 중";
const paymentMethodLabel = (value: string) => ({ CARD: "카드", BANK_TRANSFER: "계좌이체" } as Record<string,string>)[value] ?? "결제수단";
const paymentProviderLabel = (value: string | null) => value === "TOSS" ? "토스페이먼츠" : value ? "결제기관 연결" : "결제기관 미연결";
const paymentMethodStatusLabel = (value: string) => ({ ACTIVE: "사용 가능", INACTIVE: "사용 중지", EXPIRED: "사용기간 만료" } as Record<string,string>)[value] ?? "상태 확인 중";
const refundStatusLabel = (value: string) => ({ REQUESTED: "환불 접수", UNDER_REVIEW: "환불 검토 중", APPROVED: "환불 승인", COMPLETED: "환불 완료", FAILED: "환불 실패", MANUAL_REQUIRED: "관리자 확인 필요", REJECTED: "환불 반려" } as Record<string,string>)[value] ?? "환불 상태 확인 중";
const refundTypeLabel = (value: string) => ({ FULL: "전액 환불", PARTIAL: "일부 환불", ADJUSTMENT: "금액 조정" } as Record<string,string>)[value] ?? "환불·조정";
function CareLayout({
  title,
  description,
  children,
}: PropsWithChildren<{ title: string; description: string }>) {
  const path = window.location.pathname;
  const visualSettings = useServiceVisualSettings();
  const nav = [
    ["/care", "수달 케어 홈"],
    ["/customer/care/requests", "맞춤 요청"],
    ["/customer/care/contracts", "내 케어"],
    ["/customer/care/visits", "회차"],
    ["/customer/care/payments", "결제"],
  ];
  return (
    <CustomerAppLayout>
      <section className={`careHero${visualSettings.bannersEnabled ? " hasFeatureBanner" : ""}`}>
        {visualSettings.bannersEnabled && <img src="/service-visuals/feature-banners/care.webp" alt="" aria-hidden="true" />}
        <p>수달 케어</p> <h1>{title}</h1> <span>{description}</span>
      </section>
      <div className="careShell">
        <nav aria-label="수달 케어 메뉴">
          {nav.map(([href, label]) => (
            <button
              className={
                path === href ||
                (href !== "/care" && path.startsWith(`${href}/`))
                  ? "isActive"
                  : ""
              }
              onClick={() => navigate(href)}
              key={href}
            >
              {label}
            </button>
          ))}
        </nav>
        <section className="careContent">{children}</section>
      </div>
    </CustomerAppLayout>
  );
}
export function CustomerCareHomePage() {
  const { user } = useAuthentication();
  const customer = user?.roles.includes("CUSTOMER") ?? false;
  const [services, setServices] = useState<SubscriptionService[]>([]);
  const [home, setHome] = useState<CareHome | null>(null);
  const [error, setError] = useState("");
  useEffect(() => {
    careApi.services()
      .then((items) => setServices(sortSubscriptionServices(items)))
      .catch((reason) => setError(message(reason)));
    if (customer)
      careApi
        .home()
        .then(setHome)
        .catch(() => undefined);
  }, [customer]);
  const start = (serviceId?: string) => {
    const target = `/customer/care/request/new?${new URLSearchParams({ ...(serviceId ? { serviceId } : {}) })}`;
    navigate(customer ? target : createLoginPath(target));
  };
  return (
    <CareLayout
      title="반복해서 필요한 생활서비스"
      description="필요한 서비스와 방문 조건을 직접 적으면 승인 전문가가 맞춤 조건을 제안합니다."
    >
      {error && <p className="careError">{error}</p>}
      <section className="careIntro">
        <div>
          <h2>내 생활에 맞춘 케어를 요청하세요</h2>
          <p>
            지원 전문가 비교부터 일정 변경, 완료확인과 사후관리까지 한 흐름으로
            이어집니다.
          </p>
          <button onClick={() => start()}>맞춤 케어 요청</button>
        </div>
        <dl>
          <div>
            <dt>대상 서비스</dt> <dd>{services.length}</dd>
          </div>
          <div><dt>신청 방식</dt><dd>맞춤 요청</dd></div>
          {customer && (
            <>
              <div className="careIntroMetricLink" role="link" tabIndex={0} onClick={() => navigate("/customer/care/contracts")} onKeyDown={(event)=>{if(event.key==='Enter')navigate("/customer/care/contracts")}}>
                <dt>내 케어</dt> <dd>{home?.activeContractCount ?? 0}</dd>
              </div>
              <div className="careIntroMetricLink" role="link" tabIndex={0} onClick={() => navigate("/customer/care/visits")} onKeyDown={(event)=>{if(event.key==='Enter')navigate("/customer/care/visits")}}>
                <dt>진행 회차</dt> <dd>{home?.upcomingVisitCount ?? 0}</dd>
              </div>
            </>
          )}
        </dl>
      </section>
      {customer && (
        <section className="careDashboard">
          <header>
            <h2>내 수달 케어</h2>
            <button onClick={() => navigate("/customer/care/contracts")}>
              전체 보기
            </button>
          </header>
          <div>
            {[
              [
                "맞춤 요청",
                home?.openRequestCount ?? 0,
                "/customer/care/requests",
              ],
              [
                "내 케어",
                home?.activeContractCount ?? 0,
                "/customer/care/contracts",
              ],
              [
                "예정 회차",
                home?.upcomingVisitCount ?? 0,
                "/customer/care/visits",
              ],
              [
                "케어 알림",
                home?.unreadNotificationCount ?? 0,
                "/customer/notifications",
              ],
            ].map(([label, count, href]) => (
              <button
                onClick={() => navigate(String(href))}
                key={String(label)}
              >
                <span>{label}</span> <strong>{count}</strong>
              </button>
            ))}
          </div>
        </section>
      )}
      <section className="careSection">
        <header>
          <div>
            <p>맞춤 케어</p> <h2>맞춤 케어 요청은 이렇게 진행됩니다</h2>
            <span>현재 맞춤 케어를 요청할 수 있는 서비스는 {services.length}개입니다.</span>
          </div>
        </header>
        <div className="careProductGrid">
          <article>
            <span>고객이 필요한 조건을 직접 등록</span> <h3>맞춤 케어 요청</h3>
            <p>
              대상 서비스 중 하나를 선택하고 방문주기·작업 범위·희망일정을
              입력하면, 전문가가 가능한 조건과 금액을 제안합니다.
            </p>
            <button onClick={() => start()}>맞춤 케어 요청하기</button>
          </article>
          <article>
            <span>승인 전문가가 조건을 확인</span> <h3>맞춤 제안 비교</h3>
            <p>
              전문가가 작업 범위·방문 횟수·가능 일정·월 또는 회차 금액을 제안하면
              고객이 비교하고 한 명을 선택합니다.
            </p>
          </article>
        </div>
      </section>
      <section className="careSection">
        <header>
          <div>
            <p>이용 가능 서비스</p> <h2>맞춤 케어 대상 서비스</h2>
          </div>
        </header>
        <div className="careServiceList">
          {services.map((item) => (
            <button onClick={() => start(item.id)} key={item.id}>
              <ServiceThumbnail name={item.serviceName} className="careServiceThumbnail" />
              <span className="careServiceCopy">
                <span>{item.majorName} · {item.middleName}</span>
                <strong>{item.serviceName}</strong>
                <small>{item.categoryPath}</small>
              </span>
            </button>
          ))}
        </div>
      </section>
    </CareLayout>
  );
}
function careScopeSuggestion(service?: SubscriptionService) {
  return service
    ? `${service.serviceName} 서비스를 정기적으로 받고 싶습니다. 현장 상태를 확인하고 필요한 작업 범위와 방문 일정을 안내해 주세요.`
    : "";
}
export function NewCustomerCareRequestPage() {
  const query = useMemo(() => new URLSearchParams(window.location.search), []);
  const requestKey = useRef(`customer-care-request-${crypto.randomUUID()}`);
  const initialService = query.get("serviceId") ?? "";
  const [services, setServices] = useState<SubscriptionService[]>([]);
  const [sidos, setSidos] = useState<AdministrativeArea[]>([]);
  const [sigungu, setSigungu] = useState<AdministrativeArea[]>([]);
  const [sidoId, setSidoId] = useState("");
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({
    serviceCategoryId: initialService,
    administrativeAreaId: "",
    detailAddress: "",
    requestedScopeText: "",
    priceConsultation: true,
    desiredAmountType: "MONTHLY",
    desiredAmount: "",
    preferredStartDate: "",
    frequencyTypeCode: "WEEKLY",
    intervalValue: 1,
    weekdays: [1] as number[],
    preferredTimeFrom: "10:00",
    preferredTimeTo: "11:00",
    expectedDurationMinutes: 60,
    endDate: "",
  });
  useEffect(() => {
    Promise.all([
      careApi.services(),
      customerAccountApi.sidos(),
    ])
      .then(([a, c]) => {
        const sortedServices = sortSubscriptionServices(a);
        setServices(sortedServices);
        setSidos(c);
        const selected = sortedServices.find((item) => item.id === initialService);
        if (selected)
          setForm((value) => ({
            ...value,
            requestedScopeText:
              value.requestedScopeText || careScopeSuggestion(selected),
          }));
      })
      .catch((reason) => setError(message(reason)));
  }, [initialService]);
  useEffect(() => {
    void loadDefaultAddress()
      .then((value) => {
        if (!value?.area) return;
        setSidoId((current) => current || value.sidoId);
        setForm((current) => ({
          ...current,
          administrativeAreaId: current.administrativeAreaId || value.area!.id,
          detailAddress: current.detailAddress || value.fullAddress,
        }));
      })
      .catch(() => undefined);
  }, []);
  useEffect(() => {
    if (!sidoId) {
      setSigungu([]);
      return;
    }
    customerAccountApi
      .sigungu(sidoId)
      .then(setSigungu)
      .catch((reason) => {
        setSigungu([]);
        setError(message(reason));
      });
  }, [sidoId]);
  const selectedService = services.find((item) => item.id === form.serviceCategoryId);
  const submit = async (event: FormEvent) => {
    event.preventDefault();
    try {
      online();
      if (!form.serviceCategoryId) throw new Error("요청할 서비스를 선택해 주세요.");
      if (!form.administrativeAreaId) throw new Error("서비스를 받을 시·군·구를 선택해 주세요.");
      if (!form.preferredStartDate) throw new Error("희망 시작일을 선택해 주세요.");
      if (form.requestedScopeText.trim().length < 20) throw new Error("요청 범위를 20자 이상 입력해 주세요.");
      if (!form.priceConsultation && Number(form.desiredAmount) < 1) throw new Error("희망 비용을 1원 이상 입력하거나 견적상담 후 결정을 선택해 주세요.");
      if (form.frequencyTypeCode.includes("WEEK") && form.weekdays.length === 0) throw new Error("방문할 요일을 한 개 이상 선택해 주세요.");
      if (form.preferredTimeTo && form.preferredTimeTo <= form.preferredTimeFrom) throw new Error("희망 종료시간은 시작시간보다 늦게 선택해 주세요.");
      if (form.endDate && form.endDate < form.preferredStartDate) throw new Error("종료일은 희망 시작일과 같거나 이후여야 합니다.");
      setSaving(true);
      setError("");
      const result = await careApi.createRequest({
        serviceCategoryId: form.serviceCategoryId,
        careProductId: null,
        administrativeAreaId: form.administrativeAreaId,
        requestTypeCode: "CUSTOM",
        requestedScopeText: form.requestedScopeText.trim(),
        priceNegotiable: form.priceConsultation,
        desiredMonthlyAmount: !form.priceConsultation && form.desiredAmountType === "MONTHLY" ? Number(form.desiredAmount) : null,
        desiredVisitAmount: !form.priceConsultation && form.desiredAmountType === "VISIT" ? Number(form.desiredAmount) : null,
        preferredStartDate: form.preferredStartDate,
        detailAddress: form.detailAddress,
        customerAddressId: null,
        idempotencyKey: requestKey.current,
        recurrence: {
          frequencyTypeCode: form.frequencyTypeCode,
          intervalValue: form.intervalValue,
          visitsPerPeriod: form.frequencyTypeCode.includes("WEEK") ? form.weekdays.length : null,
          weekdays: form.frequencyTypeCode.includes("WEEK")
            ? form.weekdays
            : [],
          preferredTimeFrom: form.preferredTimeFrom,
          preferredTimeTo: form.preferredTimeTo || null,
          expectedDurationMinutes: form.expectedDurationMinutes,
          startDate: form.preferredStartDate,
          endDate: form.endDate || null,
        },
      });
      navigate(`/customer/care/requests/${result.id}`);
    } catch (reason) {
      setError(message(reason));
      setSaving(false);
    }
  };
  return (
    <CareLayout
      title="맞춤 케어 요청"
      description="필요한 서비스 범위와 반복 일정, 희망 비용을 직접 등록하세요."
    >
      <form className="careForm" onSubmit={(event) => void submit(event)}>
        <fieldset>
          <legend>1. 서비스와 요청 범위</legend>
          <label>
            서비스
            <select
              required
              value={form.serviceCategoryId}
              onChange={(event) => {
                const previousSuggestion = careScopeSuggestion(
                  services.find((item) => item.id === form.serviceCategoryId),
                );
                const nextSuggestion = careScopeSuggestion(
                  services.find((item) => item.id === event.target.value),
                );
                setForm({
                  ...form,
                  serviceCategoryId: event.target.value,
                  desiredAmount: "",
                  requestedScopeText:
                    !form.requestedScopeText.trim() ||
                    form.requestedScopeText === previousSuggestion
                      ? nextSuggestion
                      : form.requestedScopeText,
                });
              }}
            >
              <option value="">선택</option>
              {services.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.categoryPath.replace(/^정기구독\s*>\s*/, "")}
                </option>
              ))}
            </select>
          </label>
          {selectedService && (
            <div className="careSelectedServiceVisual">
              <ServiceThumbnail name={selectedService.serviceName} />
              <div><small>선택한 하위 서비스</small><strong>{selectedService.serviceName}</strong><span>{selectedService.categoryPath.replace(/^정기구독\s*>\s*/, "")}</span></div>
            </div>
          )}
          <label>
            요청 범위
            <textarea
              required
              minLength={20}
              value={form.requestedScopeText}
              onChange={(event) =>
                setForm({ ...form, requestedScopeText: event.target.value })
              }
              placeholder="필요한 공간, 규모, 포함·제외 작업과 특이사항을 적어 주세요."
            />
            <small>
              서비스를 선택하면 기본 요청 문구가 자동으로 입력됩니다. 실제
              공간·작업 범위·특이사항에 맞게 수정해 주세요.
            </small>
          </label>
          <div className="careBudgetField">
            <div className="careBudgetHeading">
              <strong>희망 비용</strong>
              <label>
                <input
                  type="checkbox"
                  checked={form.priceConsultation}
                  onChange={(event) =>
                    setForm({
                      ...form,
                      priceConsultation: event.target.checked,
                      desiredAmount: event.target.checked
                        ? ""
                        : form.desiredAmount,
                    })
                  }
                />
                견적상담 후 결정
              </label>
            </div>
            {!form.priceConsultation && <label>희망 금액 기준
              <select value={form.desiredAmountType} onChange={(event)=>setForm({...form,desiredAmountType:event.target.value})}>
                <option value="MONTHLY">월 금액</option><option value="VISIT">회차 금액</option>
              </select>
            </label>}
            <input
              aria-label="희망 비용"
              type="text"
              inputMode="numeric"
              required={!form.priceConsultation}
              disabled={form.priceConsultation}
              value={form.desiredAmount ? Number(form.desiredAmount).toLocaleString("ko-KR") : ""}
              onChange={(event) =>
                setForm({ ...form, desiredAmount: amountDigits(event.target.value) })
              }
            />
            <small>
              {form.priceConsultation
                ? "전문가가 제안한 견적을 확인한 뒤 비용을 결정합니다."
                : "희망 금액은 전문가 제안을 비교하기 위한 기준이며 최종 금액은 채택 전에 확인합니다."}
            </small>
          </div>
        </fieldset>
        <fieldset>
          <legend>2. 서비스 받을 주소</legend>
          <label>
            시·도
            <select
              required
              value={sidoId}
              onChange={(event) => {
                setSidoId(event.target.value);
                setForm({ ...form, administrativeAreaId: "" });
              }}
            >
              <option value="">선택</option>
              {sidos.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
            </select>
          </label>
          <label>
            시·군·구
            <select
              required
              disabled={!sidoId}
              value={form.administrativeAreaId}
              onChange={(event) =>
                setForm({ ...form, administrativeAreaId: event.target.value })
              }
            >
              <option value="">선택</option>
              {sigungu.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
            </select>
          </label>
          <label>
            서비스 상세주소
            <input
              required
              value={form.detailAddress}
              onChange={(event) =>
                setForm({ ...form, detailAddress: event.target.value })
              }
            />
          </label>
          <small>
            지원 전문가 비교 단계에서는 전화번호와 상세주소가 공개되지 않습니다.
          </small>
        </fieldset>
        <fieldset>
          <legend>3. 반복일정</legend>
          <div className="careFormGrid">
            <label>
              희망 시작일
              <input
                type="date"
                required
                min={new Date().toISOString().slice(0, 10)}
                value={form.preferredStartDate}
                onChange={(event) =>
                  setForm({ ...form, preferredStartDate: event.target.value })
                }
              />
            </label>
            <label>
              반복주기
              <select
                value={form.frequencyTypeCode}
                onChange={(event) =>
                  setForm({ ...form, frequencyTypeCode: event.target.value, intervalValue: 1 })
                }
              >
                {[
                  ["WEEKLY", "매주"],
                  ["BIWEEKLY", "2주마다(격주)"],
                  ["MONTHLY", "매월"],
                  ["QUARTERLY", "3개월마다(분기)"],
                  ["HALF_YEARLY", "6개월마다(반기)"],
                ].map(([code, label]) => (
                  <option value={code} key={code}>
                    {label}
                  </option>
                ))}
              </select>
            </label>
            <div className="careRecurrenceGuide"><strong>선택한 반복</strong><span>{recurrenceSummary({...form, intervalValue:1})}</span><small>간격 숫자를 따로 입력하지 않습니다. 원하는 반복 주기를 위에서 바로 선택하세요.</small></div>
            <label>
              회당 예상시간(분)
              <input
                type="number"
                min="1"
                max="1440"
                value={form.expectedDurationMinutes}
                onChange={(event) =>
                  setForm({
                    ...form,
                    expectedDurationMinutes: Number(event.target.value),
                  })
                }
              />
            </label>
            <label>
              희망 시작시간
              <input
                type="time"
                value={form.preferredTimeFrom}
                onChange={(event) =>
                  setForm({ ...form, preferredTimeFrom: event.target.value })
                }
              />
            </label>
            <label>
              희망 종료시간
              <input
                type="time"
                value={form.preferredTimeTo}
                onChange={(event) =>
                  setForm({ ...form, preferredTimeTo: event.target.value })
                }
              />
            </label>
            <label>
              종료일(선택)
              <input
                type="date"
                min={form.preferredStartDate || new Date().toISOString().slice(0, 10)}
                value={form.endDate}
                onChange={(event) =>
                  setForm({ ...form, endDate: event.target.value })
                }
              />
            </label>
          </div>
          {form.frequencyTypeCode.includes("WEEK") && (
            <div className="weekdayChoice">
              {weekdayNames.map(
                (label, index) => (
                  <label key={label}>
                    <input
                      type="checkbox"
                      checked={form.weekdays.includes(index)}
                      onChange={(event) =>
                        setForm({
                          ...form,
                          weekdays: event.target.checked
                            ? [...form.weekdays, index]
                            : form.weekdays.filter((item) => item !== index),
                        })
                      }
                    />
                    {label}
                  </label>
                ),
              )}
            </div>
          )}
          <p className="careRecurrencePreview"><strong>저장될 일정</strong> {recurrenceSummary(form)}</p>
        </fieldset>
        {error && <p className="careError">{error}</p>}
        <div className="careStickyAction">
          <button disabled={saving}>
            {saving ? "신청 중…" : "맞춤 케어 요청 공개"}
          </button>
        </div>
      </form>
    </CareLayout>
  );
}
export function CustomerCareRequestsPage({ id }: { id?: string }) {
  const [items, setItems] = useState<CareRequest[]>([]);
  const [detail, setDetail] = useState<CareRequest | null>(null);
  const [applications, setApplications] = useState<CareApplication[]>([]);
  const [contractId, setContractId] = useState("");
  const [error, setError] = useState("");
  const [selecting, setSelecting] = useState("");
  useEffect(() => {
    const task = id
      ? (async () => {
          try {
            const [requestDetail, requestApplications] = await Promise.all([careApi.request(id), careApi.applications(id)]);
            setDetail(requestDetail);
            setApplications(requestApplications);
            if (requestDetail.providerSelected) {
              try {
                const contracts = await careApi.contracts();
                setContractId(contracts.find((contract) => contract.requestId === requestDetail.id)?.id ?? "");
              } catch { setContractId(""); }
            } else setContractId("");
          } catch (requestError) {
            try {
              await careApi.contract(id);
              navigate(`/customer/care/contracts/${id}`, true);
            } catch {
              throw requestError;
            }
          }
        })()
      : careApi.requests().then(setItems);
    task.catch((reason) => setError(message(reason)));
  }, [id]);
  const choose = async (application: CareApplication) => {
    if (
      !detail ||
      !await soodalConfirm(
        `${application.providerName} 전문가를 선택하시겠습니까? 선택 후 맞춤 케어 계약과 회차가 생성됩니다.`,
      )
    )
      return;
    try {
      online();
      setSelecting(application.id);
      const contract = await careApi.select(detail.id, application.id);
      navigate(`/customer/care/contracts/${contract.id}`);
    } catch (reason) {
      setError(message(reason));
      setSelecting("");
    }
  };
  const cancelRequest = async () => {
    if (!detail) return;
    const reason = await soodalPrompt("요청 취소 사유를 입력해 주세요.");
    if (!reason?.trim()) return;
    try {
      online();
      const next = await careApi.cancelRequest(detail, reason);
      setDetail(next);
      setApplications([]);
    } catch (reason) {
      setError(message(reason));
    }
  };
  if (id)
    return (
      <CareLayout
        title="맞춤 케어 요청 상세"
        description="지원 전문가의 신뢰정보와 실제 제안조건을 같은 기준으로 비교하세요."
      >
        {error && <p className="careError">{error}</p>}
        {detail && (
          <section className="careDetailCard">
            <span>
              {detail.requestNumber} · 맞춤 케어
            </span>
            <h2>{detail.serviceName}</h2> <p>{detail.requestedScope}</p>
            <dl>
              <div>
                <dt>지역</dt> <dd>{detail.areaName}</dd>
              </div>
              <div>
                <dt>반복</dt>
                <dd>
                  {recurrenceSummary(detail.recurrence)}
                </dd>
              </div>
              <div>
                <dt>시작</dt> <dd>{detail.preferredStartDate}</dd>
              </div>
              <div>
                <dt>상태</dt> <dd>{requestStatus(detail.statusCode, detail.providerSelected)}</dd>
              </div>
              <div><dt>희망 비용</dt><dd>{detail.priceNegotiable ? "전문가와 협의" : detail.desiredMonthlyAmount != null ? `월 ${money(detail.desiredMonthlyAmount)}` : `회차 ${money(detail.desiredVisitAmount)}`}</dd></div>
            </dl>
            {detail.statusCode === "OPEN" && !detail.providerSelected && (
              <button onClick={() => void cancelRequest()}>맞춤 요청 취소</button>
            )}
            {contractId && (
              <div className="careApplicationActions">
                <button onClick={() => navigate(`/customer/care/contracts/${contractId}`)}>계약 보기</button>
                <button onClick={() => void openBusinessChat("customer", "SUBSCRIPTION", contractId).catch((reason) => setError(message(reason)))}>선택한 전문가와 채팅</button>
              </div>
            )}
          </section>
        )}
        <section className="careApplications">
          <header>
            <h2>지원 전문가 비교</h2>
            <span>수달신뢰점수와 제안내용을 함께 확인하세요. 가격만으로 추천하지 않습니다.</span>
          </header>
          {applications.map((item) => (
            <article
              className={item.isSelected ? "isSelected" : ""}
              key={item.id}
            >
              <header>
                <div>
                  <span>{item.trustDisplay}</span>
                  <h3>{item.providerName}</h3>
                </div>
                <strong>{item.isSelected ? "선택 완료" : careStatusLabel(item.statusCode)}</strong>
              </header>
              <p>{item.proposedScope}</p>
              <div className="applicationPrices">
                <div>
                  <small>월 제안</small>
                  <b>{money(item.proposedMonthlyAmount)}</b>
                </div>
                <div>
                  <small>회차 제안</small>
                  <b>{money(item.proposedVisitAmount)}</b>
                </div>
              </div>
              <div className="careApplicationActions">
                <button className="careProfileButton" onClick={() => navigate(`/providers/${item.providerId}`)}>전문가 프로필 보기</button>
              {detail?.statusCode === "OPEN" &&
                item.statusCode === "SUBMITTED" && (
                  <button
                    disabled={selecting === item.id}
                    onClick={() => void choose(item)}
                  >
                    {selecting === item.id ? "선택 중…" : "이 전문가 선택"}
                  </button>
                )}
              </div>
            </article>
          ))}
          {applications.length === 0 && (
            <p className="careEmpty">아직 도착한 전문가 지원이 없습니다.</p>
          )}
        </section>
      </CareLayout>
    );
  return (
    <CareLayout
      title="내 맞춤 케어 요청"
      description="전문가 모집·제안 비교·선택 상태를 확인하세요."
    >
      <div className="careToolbar">
        <button onClick={() => navigate("/customer/care/request/new")}>
          새 맞춤 케어 요청
        </button>
      </div>
      {error && <p className="careError">{error}</p>}
      <div className="careList">
        {items.map((item) => (
          <button
            className="careRequestSummaryCard"
            onClick={() => navigate(`/customer/care/requests/${item.id}`)}
            key={item.id}
          >
            <div className="careRequestSummaryLead">
              <ServiceThumbnail name={item.serviceName} />
              <div><span>{item.requestNumber} · {item.areaName}</span><strong>{item.serviceName}</strong></div>
            </div>
            <p>{item.requestedScope}</p>
            <dl className="careRequestSummaryFacts">
              <div><dt>희망 비용</dt><dd>{item.priceNegotiable ? "전문가와 협의" : money(item.desiredMonthlyAmount)}</dd></div>
              <div><dt>반복 일정</dt><dd>{recurrenceSummary(item.recurrence)}</dd></div>
              <div><dt>방문 시간</dt><dd>{clockTime(item.recurrence.preferredTimeFrom) ?? "시간 협의"}{clockTime(item.recurrence.preferredTimeTo) ? `~${clockTime(item.recurrence.preferredTimeTo)}` : ""} · {item.recurrence.expectedDurationMinutes}분</dd></div>
              <div><dt>이용 기간</dt><dd>{calendarDate(item.preferredStartDate)} ~ {calendarDate(item.recurrence.endDate)}</dd></div>
              <div><dt>지원 현황</dt><dd>전문가 {item.applicationCount}명</dd></div>
            </dl>
            <b>{requestStatus(item.statusCode, item.providerSelected)}</b>
          </button>
        ))}
        {items.length === 0 && (
          <p className="careEmpty">맞춤 케어 요청이 없습니다.</p>
        )}
      </div>
    </CareLayout>
  );
}
export function CustomerCareContractsPage({ id }: { id?: string }) {
  const [items, setItems] = useState<CareContract[]>([]);
  const [detail, setDetail] = useState<CareContract | null>(null);
  const [contractVisits, setContractVisits] = useState<CareVisit[] | null>(null);
  const [error, setError] = useState("");
  const [reason, setReason] = useState("");
  useEffect(() => {
    const task = id
      ? careApi.contract(id).then(setDetail)
      : careApi.contracts().then(setItems);
    task.catch((reason) => setError(message(reason)));
  }, [id]);
  const action = async (value: "pause" | "resume" | "terminate") => {
    if (!detail) return;
    if ((value === "pause" || value === "terminate") && !reason.trim()) {
      setError(value === "pause" ? "일시정지 사유를 입력해 주세요." : "해지 사유를 입력해 주세요.");
      return;
    }
    if (
      value === "terminate" &&
      !await soodalConfirm(
        "구독 해지를 요청하시겠습니까? 미래 회차는 취소되고 결제 내역에 따라 남은 기간·미사용 회차 환불이 자동 계산됩니다.",
      )
    )
      return;
    try {
      online();
      const next = await careApi.contractAction(detail.id, value, {
        reason: reason.trim() || null,
        resumePlannedAt: null,
        rowVersion: detail.rowVersion,
        idempotencyKey: `customer-care-${value}-${detail.id}-${crypto.randomUUID()}`,
      });
      setDetail(next);
      setReason("");
      setError("");
    } catch (reason) {
      setError(message(reason));
    }
  };
  const toggleContractVisits = async () => {
    if (!detail) return;
    if (contractVisits) { setContractVisits(null); return; }
    try {
      setError("");
      setContractVisits(await careApi.visits(detail.id));
    } catch (reason) {
      setError(message(reason));
    }
  };
  const serviceScope = detail ? scopeSnapshot(detail.serviceScopeSnapshotJson) : null;
  const recurrenceRule = detail ? recurrenceSnapshot(detail.recurrenceSnapshotJson) : null;
  const recurrenceValue = (...names: string[]) => snapshotField(recurrenceRule, ...names);
  const recurrenceDays = Array.isArray(recurrenceValue("Weekdays"))
    ? (recurrenceValue("Weekdays") as unknown[]).map((value) => weekdayNames[Number(value)]).filter(Boolean)
    : [];
  const plannedEndDate = typeof recurrenceValue("EndDate") === "string" ? recurrenceValue("EndDate") as string : null;
  const ruleFrequency = String(recurrenceValue("FrequencyTypeCode") ?? "");
  const intervalValue = Number(recurrenceValue("IntervalValue") ?? 1);
  const visitsPerPeriod = Number(recurrenceValue("VisitsPerPeriod") ?? recurrenceDays.length);
  const durationMinutes = Number(recurrenceValue("ExpectedDurationMinutes") ?? 0);
  const timeFrom = clockTime(recurrenceValue("PreferredTimeFrom"));
  const timeTo = clockTime(recurrenceValue("PreferredTimeTo"));
  if (id)
    return (
      <CareLayout
        title="맞춤 케어 계약 확인"
        description="선택한 전문가의 맞춤 케어 조건과 반복 방문 계약을 확인합니다."
      >
        {error && <p className="careError">{error}</p>}
        {detail && (
          <>
            <section className="careContractHero">
              <div>
                <span>{detail.contractNumber}</span>
                <h2>{detail.serviceName}</h2>
                <p>
                  맞춤 케어 ·
                  {detail.providerName}
                </p>
              </div>
              <strong>{detail.statusDisplay}</strong>
            </section>
            <section className="careDetailGrid">
              <article>
                <h3>계약 조건</h3>
                <dl>
                  <div>
                    <dt>월 금액</dt>
                    <dd>{money(detail.monthlyAmount, detail.currencyCode)}</dd>
                  </div>
                  <div>
                    <dt>회차 금액</dt>
                    <dd>{money(detail.visitAmount, detail.currencyCode)}</dd>
                  </div>
                  <div>
                    <dt>계약 시작일</dt> <dd>{date(detail.startedAt)}</dd>
                  </div>
                  <div>
                    <dt>계약 종료일</dt> <dd>{calendarDate(plannedEndDate)}</dd>
                  </div>
                  <div>
                    <dt>다음 방문</dt>
                    <dd>{detail.nextVisitAt ? date(detail.nextVisitAt) : detail.statusCode === "PAYMENT_PENDING" ? "첫 결제 완료 후 일정 생성" : "예정된 방문 없음"}</dd>
                  </div>
                  <div>
                    <dt>선택 당시 신뢰정보</dt>
                    <dd>
                      {detail.providerTrustScoreSnapshot == null
                        ? "신규·평가중"
                        : `${detail.providerTrustScoreSnapshot}점`}
                    </dd>
                  </div>
                </dl>
              </article>
              <article className="careContractTerms">
                <h3>서비스 범위</h3>
                <dl className="careScopeDetails">
                  <div><dt>고객 요청</dt><dd>{serviceScope?.requested ?? "등록된 요청 내용 없음"}</dd></div>
                  <div><dt>전문가 제안</dt><dd>{serviceScope?.proposed ?? "등록된 제안 내용 없음"}</dd></div>
                  {serviceScope?.product && <div><dt>상품 기본 범위</dt><dd>{serviceScope.product}</dd></div>}
                </dl>
                <h3>반복 방문 규칙</h3>
                <dl className="careRecurrenceDetails">
                  <div><dt>방문 주기</dt><dd>{frequency(ruleFrequency)} · {intervalValue > 1 ? `${intervalValue}주기마다` : "매 주기"}</dd></div>
                  <div><dt>방문 횟수</dt><dd>주기당 {visitsPerPeriod}회</dd></div>
                  <div><dt>방문 요일</dt><dd>{recurrenceDays.length ? `${recurrenceDays.join("·")}요일` : "요일 협의"}</dd></div>
                  <div><dt>희망 시간</dt><dd>{timeFrom ?? "시간 협의"}{timeTo ? `~${timeTo}` : ""}{durationMinutes ? ` · 회차당 ${durationMinutes}분` : ""}</dd></div>
                  <div><dt>이용 기간</dt><dd>{calendarDate(typeof recurrenceValue("StartDate") === "string" ? recurrenceValue("StartDate") as string : null)} ~ {calendarDate(plannedEndDate)}</dd></div>
                </dl>
              </article>
            </section>
            <section className="careContractActions">
              <h3>맞춤 케어 관리</h3>
              <div className="careManagementGrid">
                <article>
                  <span>방문 내역</span><h4>회차 확인</h4>
                  <p>예정된 방문과 완료된 작업, 일정 변경 내역을 확인합니다.</p>
                  <button onClick={() => void toggleContractVisits()}>{contractVisits ? "회차 닫기" : "회차 보기"}</button>
                </article>
                <article>
                  <span>계약 상태</span><h4>일시정지·해지</h4>
                  <label>요청 사유<input value={reason} onChange={(event) => setReason(event.target.value)} placeholder="정지 또는 해지 사유를 입력하세요." /></label>
                  <div className="careManagementButtons">
                    {detail.statusCode === "ACTIVE" && <button onClick={() => void action("pause")}>일시정지</button>}
                    {detail.statusCode === "PAUSED" && <button onClick={() => void action("resume")}>구독 재개</button>}
                    {detail.statusCode !== "TERMINATED" && <button className="danger" onClick={() => void action("terminate")}>해지 요청</button>}
                  </div>
                </article>
                <article>
                  <span>전문가 지원</span><h4>전문가 변경 문의</h4>
                  <p>전문가 변경은 고객센터가 진행 상황과 대체 가능 전문가를 확인한 뒤 처리합니다.</p>
                  <button onClick={() => navigate(`/suggestions?type=OTHER&title=${encodeURIComponent(`전문가 변경 문의 · ${detail.contractNumber}`)}&body=${encodeURIComponent(`계약번호: ${detail.contractNumber}\n서비스: ${detail.serviceName}\n현재 전문가: ${detail.providerName}\n\n변경이 필요한 사유를 입력해 주세요.`)}`)}>전문가 변경 문의</button>
                </article>
              </div>
              {contractVisits && (
                <section className="careList" aria-label="이 계약의 회차 목록">
                  {contractVisits.map((visit) => (
                    <button key={visit.id} onClick={() => navigate(`/customer/care/visits/${visit.id}`)}>
                      <span>{visit.contractNumber} · {visit.visitNo}회차</span>
                      <strong>{date(visit.scheduledStartAt)}</strong>
                      <small>{visit.providerName}</small>
                      <b>{visit.statusDisplay}</b>
                    </button>
                  ))}
                  {contractVisits.length === 0 && <p className="careEmpty">첫 결제 완료 후 예정 회차가 생성됩니다.</p>}
                </section>
              )}
            </section>
          </>
        )}
      </CareLayout>
    );
  return (
    <CareLayout
      title="내 맞춤 케어"
      description="이용 중·일시정지·해지·종료 상태별 계약을 확인하세요."
    >
      <div className="careTabs">
        {["", "ACTIVE", "PAUSED", "TERMINATED"].map((value) => (
          <button
            onClick={() => careApi.contracts(value || undefined).then(setItems)}
            key={value}
          >
            {value === ""
              ? "전체"
              : value === "ACTIVE"
                ? "이용 중"
                : value === "PAUSED"
                  ? "일시정지"
                  : "해지"}
          </button>
        ))}
      </div>
      {error && <p className="careError">{error}</p>}
      <div className="careList">
        {items.map((item) => <CareContractSummaryCard item={item} key={item.id} />)}
        {items.length === 0 && (
          <p className="careEmpty">조건에 맞는 맞춤 케어가 없습니다.</p>
        )}
      </div>
    </CareLayout>
  );
}
export function CustomerCareVisitsPage({ id }: { id?: string }) {
  const query = useMemo(() => new URLSearchParams(window.location.search), []);
  const [items, setItems] = useState<CareVisit[]>([]);
  const [detail, setDetail] = useState<CareVisitDetail | null>(null);
  const [error, setError] = useState("");
  const [form, setForm] = useState({
    date: "",
    reason: "",
    subject: "",
    description: "",
    rating: 5,
  });
  useEffect(() => {
    const task = id
      ? careApi.visit(id).then(setDetail)
      : careApi.visits(query.get("contractId") ?? undefined).then(setItems);
    task.catch((reason) => setError(message(reason)));
  }, [id, query]);
  const refresh = async () => {
    if (id) setDetail(await careApi.visit(id));
  };
  const change = async (event: FormEvent) => {
    event.preventDefault();
    if (!detail) return;
    try {
      online();
      await careApi.scheduleChange(detail.visit.id, {
        scheduledStartAt: new Date(form.date).toISOString(),
        scheduledEndAt: null,
        reason: form.reason,
        idempotencyKey: `customer-care-change-${crypto.randomUUID()}`,
      });
      await refresh();
    } catch (reason) {
      setError(message(reason));
    }
  };
  const skip = async () => {
    if (
      !detail ||
      !await soodalConfirm(
        "방문 24시간 전까지만 건너뛸 수 있습니다. 이번 회차를 건너뛰시겠습니까? 금액과 환불은 자동 조정되지 않습니다.",
      )
    )
      return;
    try {
      online();
      await careApi.skip(detail.visit.id, "", form.reason);
      await refresh();
    } catch (reason) {
      setError(message(reason));
    }
  };
  const confirm = async () => {
    if (!detail) return;
    try {
      online();
      await careApi.confirm(detail.visit.id, "");
      await refresh();
    } catch (reason) {
      setError(message(reason));
    }
  };
  const followup = async (kind: "review" | "after-service" | "dispute") => {
    if (!detail) return;
    try {
      online();
      if (kind === "review")
        await careApi.review(detail.visit.id, form.description, form.rating);
      else if (kind === "after-service")
        await careApi.afterService(
          detail.visit.id,
          form.subject,
          form.description,
        );
      else
        await careApi.dispute(detail.visit.id, form.subject, form.description);
      await refresh();
    } catch (reason) {
      setError(message(reason));
    }
  };
  if (id)
    return (
      <CareLayout
        title={`맞춤 케어 회차 ${detail?.visit.visitNo ?? ""}`}
        description="방문·완료보고·고객확인과 리뷰/A·S/분쟁을 한 화면에서 이어갑니다."
      >
        {error && <p className="careError">{error}</p>}
        {detail && (
          <>
            <section className="visitTimeline">
              <div className={detail.visit.visitVerified ? "done" : ""}>
                <b>1</b> <span>방문 확인</span>
              </div>
              <div
                className={
                  detail.visit.providerCompletionSubmitted ? "done" : ""
                }
              >
                <b>2</b> <span>전문가 완료보고</span>
              </div>
              <div className={detail.visit.customerConfirmed ? "done" : ""}>
                <b>3</b> <span>고객 완료확인</span>
              </div>
            </section>
            <section className="careDetailCard">
              <span>
                {detail.visit.contractNumber} · 회차 {detail.visit.visitNo}
              </span>
              <h2>{detail.visit.serviceName}</h2>
              <dl>
                <div>
                  <dt>일정</dt>
                  <dd>{date(detail.visit.scheduledStartAt)}</dd>
                </div>
                <div>
                  <dt>전문가</dt> <dd>{detail.visit.providerName}</dd>
                </div>
                <div>
                  <dt>상태</dt> <dd>{detail.customerProgressDisplay}</dd>
                </div>
                <div>
                  <dt>방문확인</dt>
                  <dd>{detail.visitVerificationResult ?? "미확인"}</dd>
                </div>
              </dl>
              {detail.completionNote && (
                <div className="completionReport">
                  <h3>전문가 완료보고</h3> <p>{detail.completionNote}</p>
                  {detail.completionChecklistJson && (
                    <pre>{detail.completionChecklistJson}</pre>
                  )}
                  <div>
                    {detail.files.map((file) =>
                      file.downloadUrl ? (
                        <a href={file.downloadUrl} key={file.id}>
                          {file.fileName}
                        </a>
                      ) : (
                        <p className="carePrivacy" key={file.id}>
                          {file.fileName} ·
                          {file.publicationMessage ??
                            "안전 확인 전에는 공개되지 않습니다."}
                        </p>
                      ),
                    )}
                  </div>
                </div>
              )}
            </section>
            {new Date(detail.visit.scheduledStartAt) > new Date() &&
              !["CANCELLED", "SKIPPED", "COMPLETED"].includes(
                detail.visit.statusCode,
              ) && (
                <section className="careActionPanel">
                  <h3>미래 회차 관리</h3>
                  <form onSubmit={(event) => void change(event)}>
                    <label>
                      희망 일정
                      <input
                        type="datetime-local"
                        required
                        value={form.date}
                        onChange={(event) =>
                          setForm({ ...form, date: event.target.value })
                        }
                      />
                    </label>
                    <label>
                      사유
                      <input
                        required
                        value={form.reason}
                        onChange={(event) =>
                          setForm({ ...form, reason: event.target.value })
                        }
                      />
                    </label>
                    <button>일정변경 요청</button>
                  </form>
                  <button onClick={() => void skip()}>
                    이번 회차 건너뛰기
                  </button>
                  <small>
                    일정변경은 상대방이 48시간 안에 응답하며 미응답 시 자동 만료됩니다. 회차 건너뛰기는 방문 24시간 전까지만 가능하고 금액은 자동조정하지 않습니다.
                  </small>
                </section>
              )}
            {detail.scheduleChanges.length > 0 && (
              <section className="scheduleChanges">
                <h3>일정변경 이력</h3>
                {detail.scheduleChanges.map((item) => (
                  <article key={item.id}>
                    <span>
                      {date(item.oldStartAt)} → {date(item.newStartAt)}
                    </span>
                    <b>
                      {item.statusCode === "REQUESTED"
                        ? "요청"
                        : item.statusCode === "APPROVED"
                          ? "승인"
                          : item.statusCode === "REJECTED"
                            ? "반려"
                            : "취소"}
                    </b>
                    <p>{item.reason}</p>
                    {item.statusCode === "REQUESTED" && (
                      <button
                        onClick={() =>
                          careApi
                            .cancelScheduleChange(item.id, item.rowVersion)
                            .then(refresh)
                            .catch((reason) => setError(message(reason)))
                        }
                      >
                        요청 취소
                      </button>
                    )}
                  </article>
                ))}
              </section>
            )}
            {detail.visit.statusCode === "PROVIDER_COMPLETED" && (
              <div className="careStickyAction">
                <button onClick={() => void confirm()}>작업 완료 확인</button>
              </div>
            )}
            {detail.visit.customerConfirmed && (
              <section className="careFollowup">
                <h3>완료 후 이용</h3>
                <label>
                  제목
                  <input
                    value={form.subject}
                    onChange={(event) =>
                      setForm({ ...form, subject: event.target.value })
                    }
                    placeholder="A/S 또는 분쟁 제목"
                  />
                </label>
                <label>
                  내용
                  <textarea
                    value={form.description}
                    onChange={(event) =>
                      setForm({ ...form, description: event.target.value })
                    }
                  />
                </label>
                <label>
                  리뷰 평점
                  <select
                    value={form.rating}
                    onChange={(event) =>
                      setForm({ ...form, rating: Number(event.target.value) })
                    }
                  >
                    {[5, 4, 3, 2, 1].map((value) => (
                      <option value={value} key={value}>
                        {value}점
                      </option>
                    ))}
                  </select>
                </label>
                <div>
                  {!detail.reviewId && (
                    <button onClick={() => void followup("review")}>
                      리뷰 작성
                    </button>
                  )}
                  {!detail.afterServiceId && (
                    <button onClick={() => void followup("after-service")}>
                      A/S 접수
                    </button>
                  )}
                  {!detail.disputeId && (
                    <button onClick={() => void followup("dispute")}>
                      분쟁 도움 요청
                    </button>
                  )}
                </div>
              </section>
            )}
          </>
        )}
      </CareLayout>
    );
  return (
    <CareLayout
      title="맞춤 케어 회차"
      description="예정·변경·완료 회차와 전문가 완료보고를 확인하세요."
    >
      {error && <p className="careError">{error}</p>}
      <div className="careList">
        {items.map((item) => (
          <button
            onClick={() => navigate(`/customer/care/visits/${item.id}`)}
            key={item.id}
          >
            <span>
              {item.contractNumber} · 회차 {item.visitNo}
            </span>
            <strong>{item.serviceName}</strong>
            <small>
              {item.providerName} · {date(item.scheduledStartAt)}
            </small>
            <b>{item.statusDisplay}</b>
          </button>
        ))}
        {items.length === 0 && (
          <p className="careEmpty">맞춤 케어 회차가 없습니다.</p>
        )}
      </div>
    </CareLayout>
  );
}
export function CustomerCarePaymentsPage() {
  const [methods, setMethods] = useState<PaymentMethod[]>([]);
  const [payments, setPayments] = useState<PaymentHistory[]>([]);
  const [contracts, setContracts] = useState<CareContract[]>([]);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [registration,setRegistration]=useState<BillingRegistration|null>(null);
  const [showRegistration,setShowRegistration]=useState(false);
  const [registrationAcknowledged,setRegistrationAcknowledged]=useState(false);
  const callbackHandled=useRef(false);
  const load=()=>Promise.all([careApi.paymentMethods(), careApi.payments(), careApi.contracts(),careApi.billingRegistration()]).then(([a,b,c,d])=>{setMethods(a);setPayments(b);setContracts(c);setRegistration(d)});
  useEffect(() => {
    if(callbackHandled.current)return;callbackHandled.current=true;const query=new URLSearchParams(window.location.search);const result=query.get('billing');const authKey=query.get('authKey');const customerKey=query.get('customerKey');
    const finish=async()=>{if(result==='success'&&authKey&&customerKey){await careApi.completeBillingAuthorization(authKey,customerKey);setNotice('결제수단이 안전하게 등록되었습니다. 결제 대기 구독에서 정기결제에 동의해 주세요.')}else if(result==='fail'){setError(query.get('message')??'결제수단 인증이 취소되었거나 실패했습니다.')}window.history.replaceState({},'',window.location.pathname);await load()};
    finish().catch((reason)=>setError(message(reason)));
  }, []);
  const defaultMethod = methods.find((item) => item.isDefault && item.statusCode === 'ACTIVE') ?? methods.find((item) => item.statusCode === 'ACTIVE');
  const consent = async (contract:CareContract) => {
    if (!defaultMethod) { setError('먼저 사용할 결제수단을 등록해 주세요.'); return; }
    setError(''); setNotice('');
    try { await careApi.consentRecurringPayment(contract.id,defaultMethod.id,contract.rowVersion);await load();setNotice('정기결제 동의와 첫 결제를 처리했습니다. 결제내역에서 결과를 확인해 주세요.'); }
    catch(reason){setError(message(reason));}
  };
  const connectPaymentMethod=async()=>{setError('');setNotice('');try{const config=registration??await careApi.billingRegistration();if(!config.enabled)throw new Error('정기결제 서비스 운영 설정이 아직 완료되지 않았습니다.');const toss=await loadTossPayments();const payment=toss(config.clientKey).payment({customerKey:config.customerKey});setShowRegistration(false);await payment.requestBillingAuth({method:'CARD',successUrl:new URL(config.successUrl,window.location.origin).toString(),failUrl:new URL(config.failUrl,window.location.origin).toString()})}catch(reason){setShowRegistration(false);setError(message(reason))}};
  return (
    <CareLayout
      title="결제수단·결제내역"
      description="결제수단 등록부터 첫 결제, 월 자동결제와 환불 상태를 확인합니다."
    >
      <div className="paymentPending">
        <strong>첫 결제 완료 후 구독 시작</strong>
        <p>
          결제수단을 등록하고 정기결제에 동의하면 첫 결제가 실행됩니다. 성공하면 계약이 활성화되고 방문 일정이 자동 생성됩니다.
        </p>
        <p>월 1회 자동결제 · 1회 최대 500,000원 · 최대 12개월 이용 후 갱신 시 새 동의가 필요합니다.</p>
        <a href="/policies/subscription-refund" target="_blank" rel="noreferrer">정기구독 취소·해지·환불정책 확인</a>
      </div>
      {error && <p className="careError">{error}</p>}
      {notice && <p className="careSuccess">{notice}</p>}
      <section className="careSection">
        <h2>결제 대기 구독</h2>
        {contracts.filter((item)=>item.statusCode==='PAYMENT_PENDING').map((item)=><article className="paymentMethod" key={item.id}><strong>{item.serviceName} · {item.providerName}</strong><span>{item.statusDisplay} · {money(item.monthlyAmount??item.visitAmount??0,item.currencyCode)}</span><button disabled={item.billingStatusCode==='AUTO_PAY_CONSENTED'} onClick={()=>defaultMethod?void consent(item):setShowRegistration(true)}>{item.billingStatusCode==='AUTO_PAY_CONSENTED'?'정기결제 동의 완료':defaultMethod?'정기결제 동의·첫 결제':'결제수단 등록 후 첫 결제'}</button></article>)}
        {contracts.every((item)=>item.statusCode!=='PAYMENT_PENDING')&&<p className="careEmpty">첫 결제 대기 중인 구독이 없습니다.</p>}
      </section>
      <section className="careSection">
        <div className="sectionHeading"><h2>저장된 결제수단</h2><button onClick={()=>{setRegistrationAcknowledged(false);setShowRegistration(true)}}>결제수단 등록</button></div>
        {methods.map((item) => (
          <article className="paymentMethod" key={item.id}>
            <strong>
              {item.maskedDisplayText ?? paymentMethodLabel(item.paymentMethodTypeCode)}
            </strong>
            <span>
              {paymentProviderLabel(item.providerCode)} · {paymentMethodStatusLabel(item.statusCode)}
            </span>
            {item.isDefault && <b>기본</b>}
          </article>
        ))}
        {methods.length === 0 && (
            <p className="careEmpty">연결된 결제수단이 없습니다.</p>
        )}
      </section>
      <section className="careSection">
        <h2>결제 내역</h2>
        <div className="paymentHistory">
          {payments.map((item) => (
            <article key={item.id}>
              <header>
                <div>
                  <span>
                    {item.billingPeriodStart} ~ {item.billingPeriodEnd}
                  </span>
                  <h3>{item.serviceName}</h3>
                </div>
                <b>{paymentStatusLabel(item.statusCode)}</b>
              </header>
              <strong>{money(item.requestedAmount, item.currencyCode)}</strong>
              <small>
                요청 {date(item.requestedAt)} · 처리
                {date(item.processedAt)}
              </small>
              {item.failureReason && <p>{item.failureReason}</p>}
              {item.refunds.map((refund) => (
                <div className="refund" key={refund.id}>
                  <span>{refundTypeLabel(refund.typeCode)} · {refundStatusLabel(refund.statusCode)}</span>
                  <b>
                    {money(refund.approvedAmount ?? refund.requestedAmount)}
                  </b>
                </div>
              ))}
            </article>
          ))}
          {payments.length === 0 && (
            <p className="careEmpty">실제로 처리된 결제 내역이 없습니다.</p>
          )}
        </div>
      </section>
      {showRegistration && <div className="carePaymentRegistrationBackdrop" role="presentation" onMouseDown={(event)=>{if(event.target===event.currentTarget)setShowRegistration(false)}}>
        <section className="carePaymentRegistration" role="dialog" aria-modal="true" aria-labelledby="care-payment-registration-title">
          <header><div><span>안전한 카드 연결</span><h2 id="care-payment-registration-title">결제수단 등록</h2></div><button type="button" aria-label="닫기" onClick={()=>setShowRegistration(false)}>×</button></header>
          <p>카드 정보는 토스페이먼츠의 보안 등록 화면에서 입력합니다. 수달 라이프는 카드번호와 비밀번호를 직접 저장하지 않습니다.</p>
          <ul><li>등록만으로 결제되지 않습니다.</li><li>결제 대기 구독에서 정기결제에 동의해야 첫 결제가 진행됩니다.</li><li>결제 완료 후 계약이 시작되고 방문 회차가 생성됩니다.</li></ul>
          <label className="carePaymentRegistrationCheck"><input type="checkbox" checked={registrationAcknowledged} onChange={(event)=>setRegistrationAcknowledged(event.target.checked)}/><span>결제수단 등록 절차와 정기결제 안내를 확인했습니다.</span></label>
          <footer><button type="button" onClick={()=>setShowRegistration(false)}>취소</button><button type="button" disabled={!registrationAcknowledged} onClick={()=>void connectPaymentMethod()}>카드 등록 계속</button></footer>
        </section>
      </div>}
    </CareLayout>
  );
}
export function CustomerProgressPage() {
  const [home, setHome] = useState<CareHome | null>(null);
  useEffect(() => {
    careApi
      .home()
      .then(setHome)
      .catch(() => undefined);
  }, []);
  const [interior, setInterior] = useState<Awaited<
    ReturnType<typeof interiorApi.home>
  > | null>(null);
  useEffect(() => {
    interiorApi
      .home()
      .then(setInterior)
      .catch(() => undefined);
  }, []);
  return (
    <CustomerAppLayout>
      <section className="careHero">
        <p>진행 현황</p> <h1>진행 중인 서비스</h1>
        <span>일반 거래, 수달 케어, 수달 인테리어를 구분하여 확인하세요.</span>
      </section>
      <div className="progressChoices">
        <button onClick={() => navigate("/customer/transactions")}>
          <span>일반 서비스</span> <strong>요청·거래 진행</strong>
          <small>일정, 작업완료, 리뷰와 A/S 확인</small>
        </button>
        <button onClick={() => navigate("/customer/care/contracts")}>
          <span>수달 케어</span>
          <strong>내 구독 {home?.activeContractCount ?? 0}건</strong>
          <small>
            예정 회차 {home?.upcomingVisitCount ?? 0}건 · 반복일정 관리
          </small>
        </button>
        <button onClick={() => navigate("/customer/interior/projects")}>
          <span>수달 인테리어</span>
          <strong>진행 프로젝트 {interior?.activeProjectCount ?? 0}건</strong>
          <small>실측·계약·공정·검사·하자관리</small>
        </button>
      </div>
    </CustomerAppLayout>
  );
}
