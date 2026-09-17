import { useEffect, useMemo, useState } from "react";
import type { FormEvent } from "react";
import { useAuthentication } from "../auth/AuthenticationContext";
import { createLoginPath, navigate } from "../auth/routing";
import { serviceCompany } from "../config/serviceCompany";
import { CustomerAppLayout } from "./CustomerAppLayout";
import { publicCatalogApi } from "./api";
import type {
  PublicCategory,
  PublicContent,
  PublicPromotion,
  PublicServiceDetail,
  PublicServiceSummary,
} from "./types";
import { LiveActivitySection } from "./LiveActivitySection";
import {
  LivingHomeAfterActivity,
  LivingHomeLead,
} from "./LivingHomeSections";
import { useLivingHome } from "./useLivingHome";
import { proposalApi, type ProposalCampaign } from "../proposals/proposalApi";
import { customerAccountApi } from "./accountApi";
import { soodalAlert } from '../components/soodalDialog'
import { CategoryIcon, ServiceBanner, ServiceThumbnail } from '../serviceVisuals/ServiceVisual'

type CustomerHomePromotion = PublicPromotion & { targetCategoryId?: string; targetAreaId?: string }

function money(value: number, currency = "KRW") {
  return currency === "KRW"
    ? `${value.toLocaleString("ko-KR")}원`
    : `${value.toLocaleString("ko-KR")} ${currency}`;
}

function Price({ value }: { value: PublicServiceSummary["price"] }) {
  if (!value)
    return <span className="servicePrice isPending">가격 안내 준비 중</span>;
  const range =
    value.recommendedMinAmount !== null && value.recommendedMaxAmount !== null
      ? `${money(value.recommendedMinAmount, value.currency)} ~ ${money(value.recommendedMaxAmount, value.currency)}`
      : value.referenceAmount !== null
        ? money(value.referenceAmount, value.currency)
        : null;
  return (
    <span className="servicePrice">
      <b>{value.priceDisplayLabel}</b>
      {range && <> · {range}</>}
    </span>
  );
}

function ServiceCard({ service }: { service: PublicServiceSummary }) {
  const href = `/services/${service.slug ?? service.id}`;
  return (
    <a
      className="serviceCard"
      href={href}
      onClick={(event) => {
        event.preventDefault();
        navigate(href);
      }}
    >
      <ServiceThumbnail code={service.code} name={service.name} />
      <span>
        {service.majorName} · {service.middleName}
      </span>
      <strong>{service.name}</strong>
      <Price value={service.price} />
      <small>
        {service.description ??
          "서비스 상세에서 이용 조건과 요청 항목을 확인해 주세요."}
      </small>
    </a>
  );
}

function remember(key: string, value: string) {
  const values = JSON.parse(localStorage.getItem(key) ?? "[]") as string[];
  localStorage.setItem(
    key,
    JSON.stringify([value, ...values.filter((x) => x !== value)].slice(0, 100)),
  );
}
function legacySavedServices() {
  try { return JSON.parse(localStorage.getItem("soodal-saved-services") ?? "[]") as string[]; }
  catch { return []; }
}
function removeLegacySavedService(value: string) {
  const remaining = legacySavedServices().filter((item) => item !== value);
  if (remaining.length) localStorage.setItem("soodal-saved-services", JSON.stringify(remaining));
  else localStorage.removeItem("soodal-saved-services");
}
async function share(title: string, url = window.location.href) {
  if (navigator.share) {
    await navigator.share({ title, url });
    return;
  }
  await navigator.clipboard.writeText(url);
  void soodalAlert("공유 주소를 복사했습니다.");
}

function SearchBox({ initial = "" }: { initial?: string }) {
  const [query, setQuery] = useState(initial);
  const submit = (event: FormEvent) => {
    event.preventDefault();
    if (query.trim())
      navigate(`/services/search?q=${encodeURIComponent(query.trim())}`);
  };
  return (
    <form className="customerSearchBox" role="search" onSubmit={submit}>
      <label className="srOnly" htmlFor="service-search">
        필요한 서비스 검색
      </label>
      <input
        id="service-search"
        value={query}
        onChange={(event) => setQuery(event.target.value)}
        placeholder="어떤 도움이 필요하세요? 예: 수도꼭지 교체"
      />
      <button type="submit">서비스 찾기</button>
    </form>
  );
}

export function CustomerHomePage() {
  const { status, user } = useAuthentication();
  const customer = user?.roles.includes("CUSTOMER") ?? false;
  const livingHome = useLivingHome(
    status === "loading"
      ? null
      : customer
        ? `customer:${user!.publicId}`
        : "public",
  );
  const [majors, setMajors] = useState<PublicCategory[]>([]);
  const [services, setServices] = useState<PublicServiceSummary[]>([]);
  const [notices, setNotices] = useState<PublicContent[]>([]);
  const [promotions, setPromotions] = useState<CustomerHomePromotion[]>([]);
  const [proposals, setProposals] = useState<ProposalCampaign[]>([]);
  const [proposalAreaName, setProposalAreaName] = useState<string | null>(null);
  const [error, setError] = useState("");
  useEffect(() => {
    const proposalRequest = customer
      ? customerAccountApi.addresses().then((addresses) => {
          const address = addresses.find((item) => item.isDefault) ?? addresses[0];
          setProposalAreaName(address?.administrativeAreaName ?? null);
          return proposalApi.publicList(4, address?.administrativeAreaId ?? undefined);
        })
      : proposalApi.publicList(4);
    Promise.all([
      publicCatalogApi.majors(),
      publicCatalogApi.representativeServices(8),
      publicCatalogApi.contents("NOTICE"),
      publicCatalogApi.promotions(),
      proposalRequest,
    ])
      .then(
        ([
          majorItems,
          serviceItems,
          noticeItems,
          promotionItems,
          proposalItems,
        ]) => {
          const visible = promotionItems.slice(0, 2);
          setMajors(majorItems);
          setServices(serviceItems);
          setNotices(noticeItems.slice(0, 3));
          setPromotions(visible);
          setProposals(
            proposalItems.items.filter(
              (item) =>
                !customer ||
                !["APPLIED", "CONFIRMED", "DECLINED"].includes(
                  item.myApplicationStatus ?? "",
                ),
            ),
          );
          visible.forEach(
            (item) =>
              void publicCatalogApi
                .advertisingEvent(item.creativeId, "IMPRESSION")
                .catch(() => undefined),
          );
        },
      )
      .catch(() =>
        setError("서비스 정보를 잠시 불러오지 못했습니다. 다시 시도해 주세요."),
      );
  }, [customer]);
  useEffect(() => {
    if (!customer || services.length === 0) return;
    let active = true;
    void customerAccountApi.addresses().then(async addresses => {
      const address = addresses.find(item => item.isDefault) ?? addresses[0];
      const areaId = address?.administrativeAreaId;
      if (!areaId) return [];
      const groups = await Promise.all(services.slice(0, 8).map(service =>
        publicCatalogApi.promotions(service.id, areaId).then(rows =>
          rows.map(item => ({ ...item, targetCategoryId: service.id, targetAreaId: areaId })))));
      const providers = new Set<string>();
      return groups.flat().filter(item => {
        const owner = item.providerId ?? item.campaignId;
        if (providers.has(owner)) return false;
        providers.add(owner);
        return true;
      }).slice(0, 2);
    }).then(items => {
      if (!active || items.length === 0) return;
      setPromotions(items);
      items.forEach(item => void publicCatalogApi.advertisingEvent(item.creativeId, "IMPRESSION", item.targetCategoryId, item.targetAreaId).catch(() => undefined));
    }).catch(() => undefined);
    return () => { active = false; };
  }, [customer, services]);
  const protectedPath = (path: string) =>
    navigate(customer ? path : createLoginPath(path));
  return (
    <CustomerAppLayout>
      <section className="customerHero">
        <div>
          <p>생활서비스를 더 쉽고 안심되게</p>
          <h1>
            생활의 불편을
            <br />
            수달 라이프가 해결해 드려요
          </h1>
          <span>필요한 서비스를 찾고, 여러 견적과 조건을 확인해 보세요.</span>
          <SearchBox />
        </div>
        <aside aria-label="서비스 이용 순서">
          <strong>간단한 이용 흐름</strong>
          <ol>
            <li>서비스 찾기</li>
            <li>요청 작성</li>
            <li>견적 비교</li>
            <li>전문가 선택</li>
          </ol>
        </aside>
      </section>
      {customer && (
        <section className="signedInWelcome">
          <div>
            <p>고객으로 로그인했습니다</p>
            <h2>필요한 업무를 이어서 확인하세요.</h2>
            <span>
              내 요청과 진행 중인 거래를 아래에서 바로 확인할 수 있습니다.
            </span>
          </div>
          <div>
            <button onClick={() => navigate("/customer/requests")}>
              내 요청
            </button>
            <button onClick={() => navigate("/customer/transactions")}>
              진행 거래
            </button>
          </div>
        </section>
      )}
      {error && (
        <p className="customerPageError" role="alert">
          {error}
        </p>
      )}
      <LivingHomeLead {...livingHome} />
      <LiveActivitySection signedIn={customer} />
      <LivingHomeAfterActivity signedIn={customer} value={livingHome.value} />
      <section className="customerSection">
        <header>
          <div>
            <p>서비스 카테고리</p>
            <h2>어떤 도움이 필요하세요?</h2>
          </div>
          <button
            className="customerSectionLink"
            onClick={() => navigate("/services")}
          >
            전체 서비스 보기 <span aria-hidden="true">›</span>
          </button>
        </header>
        <div className="majorGrid">
          {majors.map((major) => (
            <button
              key={major.id}
              onClick={() => navigate(major.name.replace(/\s/g, "") === "정기구독" ? "/care" : `/services?major=${major.id}`)}
            >
              <CategoryIcon name={major.name} />
              <strong>{major.name}</strong>
              <span>{major.childCount}개 분야</span>
            </button>
          ))}
        </div>
      </section>
      <section className="customerSection sectionTint">
        <header>
          <div>
            <p>대표 서비스</p>
            <h2>생활 속 필요한 서비스를 찾아보세요</h2>
            <span>
              대분류와 중분류가 다양하게 보이도록 카테고리별 대표 서비스를
              표시합니다.
            </span>
          </div>
        </header>
        <div className="serviceGrid">
          {services.map((service) => (
            <ServiceCard service={service} key={service.id} />
          ))}
        </div>
      </section>
      <section className="customerSection homeProposalSection">
        <header>
          <div>
            <p>전문가 제안·공동모집</p>
            <h2>우리 동네에서 함께하면 더 좋은 서비스</h2>
            <span>
              {customer
                ? proposalAreaName
                  ? `마이수달 기본주소의 ${proposalAreaName}와 전국 모집을 보여드립니다.`
                  : "마이수달에 기본주소를 등록하면 해당 지역 모집을 우선 확인할 수 있습니다."
                : "진행 중인 모집을 최신 등록순으로 최대 4건 보여드립니다."}
            </span>
          </div>
          <div className="homeProposalHeaderActions">
            <button
              className="primary"
              onClick={() => navigate("/proposals")}
            >
              모집 전체 보기 <span aria-hidden="true">›</span>
            </button>
          </div>
        </header>
        <div className="homeProposalGrid">
          {proposals.map((item) => (
            <button
              key={item.id}
              onClick={() => navigate(`/proposals/${item.id}`)}
            >
              <strong>{item.title}</strong>
              <small>
                {item.serviceName} · {item.confirmedParticipants}/
                {item.maximumParticipants}명
              </small>
              <b>{money(item.offerPriceAmount)}</b>
            </button>
          ))}
          {!proposals.length && (
            <div className="homeProposalEmpty">
              <strong>첫 공동모집을 준비하고 있습니다.</strong>
              <span>
                새로운 모집이 등록되면 이곳에서 바로 확인할 수 있습니다.
              </span>
              <button onClick={() => navigate("/proposals")}>
                모집 전체 보기
              </button>
            </div>
          )}
        </div>
      </section>
      <section className="customerSection entryGrid">
        <article className="careEntry">
          <p>수달 케어</p>
          <h2>정기적으로 이용하는 생활서비스</h2>
          <span>청소·점검·세탁·생활관리처럼 반복해서 필요한 서비스를 선택하고 방문 주기, 희망 일정과 예산을 등록하세요. 여러 전문가의 제안을 비교해 선택한 뒤 계약, 방문 일정, 작업 완료 확인과 이용 내역까지 한곳에서 관리할 수 있습니다.</span>
          <button onClick={() => navigate("/care")}>
            수달 케어 서비스 보기
          </button>
        </article>
        <article className="interiorEntry">
          <p>수달 인테리어</p>
          <h2>실측부터 공사 완료까지</h2>
          <span>
            현장 실측을 요청하고 전문가 견적을 비교한 뒤 계약서와 공사 일정을 확정하세요. 공정별 진행 사진과 변경사항, 비용 내역, 완료 확인은 물론 공사 후 하자 접수와 A/S 처리까지 하나의 프로젝트에서 관리할 수 있습니다.
          </span>
          <button onClick={() => navigate("/interior")}>
            수달 인테리어 시작
          </button>
        </article>
      </section>
      {(promotions.length > 0 || notices.length > 0) && (
        <section className="customerSection newsGrid">
          {promotions.length > 0 && (
            <div>
              <p className="sectionEyebrow">프로모션</p>
              {promotions.map((item) => (
                <article
                  className="noticeCard promotionCard"
                  key={item.creativeId}
                >
                  <strong>{item.title}</strong>
                  <span>{item.subtitle ?? item.bodyText}</span>
                  {item.destinationTypeCode !== "NONE" &&
                    item.destinationValue && (
                      <button
                        type="button"
                        onClick={() => {
                          void publicCatalogApi
                            .advertisingEvent(item.creativeId, "CLICK", item.targetCategoryId, item.targetAreaId)
                            .catch(() => undefined);
                          if (item.destinationTypeCode === "EXTERNAL_URL")
                            window.open(
                              item.destinationValue!,
                              "_blank",
                              "noopener,noreferrer",
                            );
                          else navigate(item.destinationValue!);
                        }}
                      >
                        {item.buttonText?.trim() || "자세히 보기"}
                      </button>
                    )}
                </article>
              ))}
            </div>
          )}
          <div>
            <p className="sectionEyebrow">공지·안전 안내</p>
            {notices.length ? (
              notices.map((item) => (
                <button
                  className="noticeCard"
                  key={item.id}
                  onClick={() => navigate("/notices")}
                >
                  <strong>{item.title}</strong>
                  <span>{item.bodyText}</span>
                </button>
              ))
            ) : (
              <p className="customerEmptyText">현재 게시된 공지가 없습니다.</p>
            )}
          </div>
        </section>
      )}
      <section className="customerCta">
        <div>
          <p>원하는 서비스를 찾으셨나요?</p>
          <h2>
            {customer
              ? "원하는 서비스를 선택하고 견적 요청을 시작해 보세요."
              : "로그인하고 견적 요청을 시작해 보세요."}
          </h2>
        </div>
        <button onClick={() => protectedPath("/customer/requests/new")}>
          견적 요청하기
        </button>
      </section>
    </CustomerAppLayout>
  );
}

export function ServiceCatalogPage() {
  const params = new URLSearchParams(window.location.search);
  const [majors, setMajors] = useState<PublicCategory[]>([]);
  const [middles, setMiddles] = useState<PublicCategory[]>([]);
  const [services, setServices] = useState<PublicServiceSummary[]>([]);
  const [majorId, setMajorId] = useState(params.get("major") ?? "");
  const [middleId, setMiddleId] = useState("");
  const [error, setError] = useState("");
  useEffect(() => {
    publicCatalogApi
      .majors()
      .then((items) => {
        const generalMajors = items.filter((item) => item.name !== "정기구독");
        setMajors(generalMajors);
        setMajorId((current) =>
          generalMajors.some((item) => item.id === current)
            ? current
            : generalMajors[0]?.id || "",
        );
      })
      .catch(() => setError("카테고리를 불러오지 못했습니다."));
  }, []);
  useEffect(() => {
    if (!majorId) return;
    publicCatalogApi
      .children(majorId)
      .then((items) => {
        const generalMiddles = items.filter((item) => item.name !== "긴급출동");
        setMiddles(generalMiddles);
        setMiddleId(generalMiddles[0]?.id ?? "");
      })
      .catch(() => setError("중분류를 불러오지 못했습니다."));
  }, [majorId]);
  useEffect(() => {
    if (!middleId) {
      setServices([]);
      return;
    }
    publicCatalogApi
      .servicesByMiddle(middleId)
      .then(setServices)
      .catch(() => setError("서비스를 불러오지 못했습니다."));
  }, [middleId]);
  return (
    <CustomerAppLayout>
      <section className="pageHeading">
        <p>전체 서비스</p>
        <h1>필요한 생활서비스를 찾아보세요</h1>
        <SearchBox />
      </section>
      {error && <p className="customerPageError">{error}</p>}
      <ServiceBanner major={majors.find(item => item.id === majorId)?.name} />
      <div className="catalogExplorer">
        <section aria-label="대분류">
          <h2>대분류</h2>
          {majors.map((item) => (
            <button
              className={majorId === item.id ? "isActive" : ""}
              onClick={() => setMajorId(item.id)}
              key={item.id}
            >
              {item.name}
              <span>{item.childCount}</span>
            </button>
          ))}
        </section>
        <section aria-label="중분류">
          <h2>중분류</h2>
          {middles.map((item) => (
            <button
              className={middleId === item.id ? "isActive" : ""}
              onClick={() => setMiddleId(item.id)}
              key={item.id}
            >
              {item.name}
              <span>{item.childCount}</span>
            </button>
          ))}
        </section>
        <section className="catalogServices" aria-label="하위 서비스">
          <h2>서비스</h2>
          {services.length ? (
            services.map((service) => (
              <ServiceCard service={service} key={service.id} />
            ))
          ) : (
            <p className="customerEmptyText">표시할 서비스가 없습니다.</p>
          )}
        </section>
      </div>
    </CustomerAppLayout>
  );
}

export function ServiceSearchPage() {
  const query =
    new URLSearchParams(window.location.search).get("q")?.trim() ?? "";
  const [items, setItems] = useState<PublicServiceSummary[]>([]);
  const [loading, setLoading] = useState(Boolean(query));
  const [error, setError] = useState("");
  useEffect(() => {
    if (!query) {
      setItems([]);
      setLoading(false);
      return;
    }
    setLoading(true);
    publicCatalogApi
      .search(query)
      .then(setItems)
      .catch(() => setError("검색 결과를 불러오지 못했습니다."))
      .finally(() => setLoading(false));
  }, [query]);
  return (
    <CustomerAppLayout>
      <section className="pageHeading soodalHeroHeading">
        <p>서비스 검색</p>
        <h1>{query ? `“${query}” 검색 결과` : "어떤 서비스가 필요하세요?"}</h1>
        <SearchBox initial={query} />
      </section>
      <section className="customerSection searchResults">
        {error && <p className="customerPageError">{error}</p>}
        {loading ? (
          <p className="customerEmptyText">검색 중입니다…</p>
        ) : items.length ? (
          <>
            <p>{items.length}개의 서비스를 찾았습니다.</p>
            <div className="serviceGrid">
              {items.map((item) => (
                <ServiceCard service={item} key={item.id} />
              ))}
            </div>
          </>
        ) : (
          <p className="customerEmptyText">
            {query
              ? "일치하는 서비스가 없습니다. 다른 표현으로 검색해 주세요."
              : "서비스명이나 카테고리명을 입력해 주세요."}
          </p>
        )}
      </section>
    </CustomerAppLayout>
  );
}

export function ServiceDetailPage({
  id,
  slug,
}: {
  id?: string;
  slug?: string;
}) {
  const { status, user } = useAuthentication();
  const [value, setValue] = useState<PublicServiceDetail | null>(null);
  const [error, setError] = useState("");
  const [saved, setSaved] = useState(false);
  const [savingInterest, setSavingInterest] = useState(false);
  const [targetedAds, setTargetedAds] = useState<CustomerHomePromotion[]>([]);
  useEffect(() => {
    const load = slug
      ? publicCatalogApi.detailBySlug(slug)
      : publicCatalogApi.detail(id!);
    load
      .then((item) => {
        setValue(item);
        remember("soodal-recent-services", item.id);
        if (user?.roles.includes("CUSTOMER"))
          void proposalApi.recordSignal(item.id).catch(() => undefined);
        if (status === "authenticated" && user?.roles.includes("CUSTOMER")) {
          const pending = sessionStorage.getItem("soodal-pending-interested-service") === item.id;
          const legacy = legacySavedServices().includes(item.id);
          if (pending || legacy)
            void customerAccountApi.saveInterestedService(item.id, true).then((state) => {
              setSaved(state.interested);
              sessionStorage.removeItem("soodal-pending-interested-service");
              removeLegacySavedService(item.id);
            }).catch(() => setSaved(false));
          else void customerAccountApi.interestedServiceState(item.id)
            .then((state) => setSaved(state.interested))
            .catch(() => setSaved(false));
        }
        else setSaved(false);
        document.title = item.seoTitle;
        document
          .querySelector('meta[name="description"]')
          ?.setAttribute("content", item.seoDescription);
        let canonical = document.querySelector<HTMLLinkElement>(
          'link[rel="canonical"]',
        );
        if (!canonical) {
          canonical = document.createElement("link");
          canonical.rel = "canonical";
          document.head.appendChild(canonical);
        }
        canonical.href = `${window.location.origin}/services/${item.slug ?? item.id}`;
        const areaRequest = status === "authenticated" && user?.roles.includes("CUSTOMER")
          ? customerAccountApi.addresses().then((addresses) =>
              (addresses.find((address) => address.isDefault) ?? addresses[0])?.administrativeAreaId ?? undefined)
          : Promise.resolve(undefined);
        void areaRequest
          .catch(() => undefined)
          .then((areaId) => publicCatalogApi.promotions(item.id, areaId).then((rows) => ({ rows, areaId })))
          .then(({ rows, areaId }) => {
            const visible = rows.slice(0, 2).map((ad) => ({
              ...ad,
              targetCategoryId: item.id,
              targetAreaId: areaId,
            }));
            setTargetedAds(visible);
            visible.forEach(
              (ad) =>
                void publicCatalogApi
                  .advertisingEvent(ad.creativeId, "IMPRESSION", ad.targetCategoryId, ad.targetAreaId)
                  .catch(() => undefined),
            );
          });
      })
      .catch(() => setError("서비스 상세를 찾을 수 없습니다."));
    return () => {
      document.title = "수달 라이프";
    };
  }, [id, slug, status, user]);
  const toggleInterest = async () => {
    if (!value) return;
    if (status !== "authenticated" || !user?.roles.includes("CUSTOMER")) {
      sessionStorage.setItem("soodal-pending-interested-service", value.id);
      navigate(createLoginPath(`${window.location.pathname}${window.location.search}`));
      return;
    }
    setSavingInterest(true); setError("");
    try {
      const state = await customerAccountApi.saveInterestedService(value.id, !saved);
      setSaved(state.interested);
    } catch {
      setError("관심 서비스를 저장하지 못했습니다. 잠시 후 다시 시도해 주세요.");
    } finally { setSavingInterest(false); }
  };
  const request = () => {
    if (!value) return;
    const subscription =
      value.majorName === "정기구독" && value.subscriptionAvailable;
    const path = subscription
      ? `/customer/care/request/new?serviceId=${value.id}`
      : `/customer/requests/new?service=${value.id}`;
    navigate(user?.roles.includes("CUSTOMER") ? path : createLoginPath(path));
  };
  if (!value)
    return (
      <CustomerAppLayout>
        <section className="pageHeading">
          <h1>{error || "서비스 정보를 불러오는 중입니다…"}</h1>
        </section>
      </CustomerAppLayout>
    );
  return (
    <CustomerAppLayout>
      <article className="serviceDetail">
        <nav aria-label="현재 위치">
          <button onClick={() => navigate(`/services?major=${value.majorId}`)}>
            {value.majorName}
          </button>
          <span>›</span>
          <button onClick={() => navigate(`/services?major=${value.majorId}`)}>
            {value.middleName}
          </button>
        </nav>
        <header>
          <div>
            <p>{value.code ?? "생활서비스"}</p>
            <h1>{value.name}</h1>
            <span>
              {value.description ??
                "서비스 범위와 현장 조건을 확인한 뒤 견적을 안내합니다."}
            </span>
            <div className="serviceShareActions">
              <button
                type="button"
                disabled={savingInterest}
                onClick={() => void toggleInterest()}
              >
                {savingInterest ? "저장 중…" : saved ? "관심 서비스 해제" : "관심 서비스로 저장"}
              </button>
              <button type="button" onClick={() => void share(value.name)}>
                공유하기
              </button>
            </div>
            {error && <p className="customerPageError" role="alert">{error}</p>}
          </div>
          <ServiceThumbnail code={value.code} name={value.name} className="serviceDetailVisual" />
          <button className="detailRequestButton" onClick={request}>
            견적 요청하기
          </button>
        </header>
        <div className="detailColumns">
          <div>
            <section>
              <h2>가격 안내</h2>
              {value.price ? (
                <>
                  <div className="pricePrimary">
                    <span>{value.price.priceDisplayLabel}</span>
                    <strong>
                      {value.price.referenceAmount !== null
                        ? money(
                            value.price.referenceAmount,
                            value.price.currency,
                          )
                        : "상담 후 안내"}
                    </strong>
                  </div>
                  <p className="standardPriceNotice">
                    표시된 금액은 기본 작업을 기준으로 한 참고 금액이며 확정
                    가격이 아닙니다. 자재, 수량, 현장 상태, 작업 범위와 출장
                    조건에 따라 달라질 수 있으므로 전문가별 작업 내용과 포함
                    비용을 비교한 뒤 결정해 주세요.
                  </p>
                  {value.price.workUnit && (
                    <p>작업 단위: {value.price.workUnit}</p>
                  )}
                  <p className={`priceVat vat-${value.price.vatPolicyCode.toLowerCase()}`}><b>{value.price.vatDisplayText}</b>{value.price.vatPolicyCode === 'EXCLUDED' && value.price.vatAmount !== null && value.price.totalAmount !== null && <span> · 예상 VAT {money(value.price.vatAmount, value.price.currency)} · 예상 합계 {money(value.price.totalAmount, value.price.currency)}</span>}</p>
                  {value.price.vatPolicyCode === 'UNDETERMINED' && <p className="standardPriceNotice">이 참고가는 VAT 포함 여부가 확정되지 않았습니다. 실제 견적서의 공급가액·VAT·최종 결제금액을 확인해 주세요.</p>}
                  <small>{value.price.guidanceText}</small>
                </>
              ) : (
                <p>가격 안내를 준비하고 있습니다.</p>
              )}
            </section>
            <section>
              <h2>이용 안내</h2>
              <dl className="serviceFacts">
                <div>
                  <dt>현장방문</dt>
                  <dd>{value.onsiteRequirement}</dd>
                </div>
                <div>
                  <dt>긴급 요청</dt>
                  <dd>
                    {value.emergencyRequestAllowed ? "가능" : "지원하지 않음"}
                  </dd>
                </div>
                <div>
                  <dt>정기구독</dt>
                  <dd>
                    {value.subscriptionAvailable
                      ? "이용 가능"
                      : "일회성 서비스"}
                  </dd>
                </div>
                <div>
                  <dt>기본 A/S</dt>
                  <dd>
                    {value.defaultWarrantyDays > 0
                      ? `${value.defaultWarrantyDays}일`
                      : "견적 조건에서 확인"}
                  </dd>
                </div>
              </dl>
            </section>
          </div>
          <aside>
            <h2>요청 전 확인할 항목</h2>
            <p>{value.requestGuide}</p>
            <ul>
              {value.requestFields.map((field) => (
                <li key={field.id}>
                  <strong>{field.label}</strong>
                  {field.required && <span>필수</span>}
                  {field.unit && <small>{field.unit}</small>}
                </li>
              ))}
            </ul>
            {value.providerRequirementGuide && (
              <div className="providerGuide">
                <strong>전문가 확인 안내</strong>
                <p>{value.providerRequirementGuide}</p>
              </div>
            )}
          </aside>
        </div>
        {targetedAds.length > 0 && (
          <section
            className="serviceTargetedAds"
            aria-label={`${value.name} 관련 광고`}
          >
            <h2>이 서비스의 추천 전문가</h2>
            {targetedAds.map((ad) => (
              <article key={ad.creativeId}>
                <strong>{ad.title}</strong>
                <span>{ad.subtitle ?? ad.bodyText}</span>
                {ad.destinationValue && (
                  <button
                    type="button"
                    onClick={() => {
                      void publicCatalogApi
                        .advertisingEvent(ad.creativeId, "CLICK", ad.targetCategoryId, ad.targetAreaId)
                        .catch(() => undefined);
                      if (ad.destinationTypeCode === "EXTERNAL_URL") {
                        window.open(
                          ad.destinationValue!,
                          "_blank",
                          "noopener,noreferrer",
                        );
                      } else {
                        navigate(ad.destinationValue!);
                      }
                    }}
                  >
                    {ad.buttonText?.trim() || "자세히 보기"}
                  </button>
                )}
              </article>
            ))}
          </section>
        )}
      </article>
    </CustomerAppLayout>
  );
}

export function PublicContentPage({
  type,
  id,
}: {
  type: "NOTICE" | "FAQ";
  id?: string;
}) {
  const [items, setItems] = useState<PublicContent[]>([]);
  const [query, setQuery] = useState("");
  const [error, setError] = useState("");
  useEffect(() => {
    publicCatalogApi
      .contents(type)
      .then(setItems)
      .catch(() => setError("게시물을 불러오지 못했습니다."));
  }, [type]);
  const title = type === "NOTICE" ? "공지사항" : "자주 묻는 질문";
  const selected = id ? items.find((item) => item.id === id) : null;
  const filtered = items.filter((item) =>
    `${item.title} ${item.questionText ?? ""} ${item.bodyText ?? ""} ${item.answerText ?? ""}`
      .toLowerCase()
      .includes(query.trim().toLowerCase()),
  );
  if (id)
    return (
      <CustomerAppLayout>
        <section className="pageHeading">
          <p>고객지원 · {title}</p>
          <h1>
            {selected?.questionText ??
              selected?.title ??
              "게시물을 찾을 수 없습니다."}
          </h1>
        </section>
        <article className="publicContentDetail">
          {selected ? (
            <>
              {type === "NOTICE" && <div className="publicContentPurpose"><strong>서비스 운영 기준 안내</strong><span>이 문서는 공지·안전 안내에 보관되는 이용 기준입니다. 서비스 신청이나 진행 업무를 처리하는 화면은 아닙니다.</span></div>}
              <span>
                게시일{" "}
                {new Intl.DateTimeFormat("ko-KR", { dateStyle: "long" }).format(
                  new Date(selected.publishedAt),
                )}{" "}
                · 대상{" "}
                {selected.audienceTypeCode === "ALL" ? "전체 사용자" : "고객"} ·
                버전 {selected.versionNo}
              </span>
              <p>{selected.answerText ?? selected.bodyText}</p>
              <button
                onClick={() =>
                  navigate(type === "NOTICE" ? "/notices" : "/faq")
                }
              >
                목록으로
              </button>
            </>
          ) : (
            <p className="customerEmptyText">현재 공개된 게시물이 아닙니다.</p>
          )}
        </article>
      </CustomerAppLayout>
    );
  return (
    <CustomerAppLayout>
      <section className="pageHeading soodalHeroHeading">
        <p>고객지원</p>
        <h1>{title}</h1>
        <label className="contentSearch">
          게시물 검색
          <input
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="제목과 내용에서 검색"
          />
        </label>
      </section>
      <section className="contentListPage">
        {error && <p className="customerPageError">{error}</p>}
        {filtered.length ? (
          filtered.map((item) =>
            type === "FAQ" ? (
              <details key={item.id}>
                <summary>{item.questionText ?? item.title}</summary>
                <p>{item.answerText ?? item.bodyText}</p>
                <small>
                  게시일{" "}
                  {new Intl.DateTimeFormat("ko-KR").format(
                    new Date(item.publishedAt),
                  )}
                </small>
              </details>
            ) : (
              <button
                className="publicNoticeRow"
                key={item.id}
                onClick={() => navigate(`/notices/${item.id}`)}
              >
                <span>
                  {new Intl.DateTimeFormat("ko-KR").format(
                    new Date(item.publishedAt),
                  )}
                </span>
                <strong>{item.title}</strong>
                <small>상세 보기</small>
              </button>
            ),
          )
        ) : (
          <p className="customerEmptyText">현재 게시된 {title}이 없습니다.</p>
        )}
      </section>
    </CustomerAppLayout>
  );
}

export function CustomerSupportPage() {
  const [counts, setCounts] = useState<{ notices: number; faqs: number } | null>(null);
  const hours = useMemo(
    () =>
      serviceCompany.customerServiceHours?.join(" · ") ?? "운영시간 확정 전",
    [],
  );
  useEffect(() => {
    let active = true;
    Promise.all([publicCatalogApi.contents("NOTICE"), publicCatalogApi.contents("FAQ")])
      .then(([notices, faqs]) => { if (active) setCounts({ notices: notices.length, faqs: faqs.length }); })
      .catch(() => { if (active) setCounts({ notices: 0, faqs: 0 }); });
    return () => { active = false; };
  }, []);
  return (
    <CustomerAppLayout>
      <section className="pageHeading soodalHeroHeading">
        <p>고객지원</p>
        <h1>궁금한 점을 확인해 보세요</h1>
      </section>
      <section className="supportGrid">
        <button onClick={() => navigate("/notices")}>
          <span className="supportCardTitle"><i aria-hidden="true">●</i><strong>공지사항</strong><b>{counts ? `${counts.notices}건` : '확인 중'} 〉</b></span>
          <span>서비스 운영 소식을 확인합니다.</span>
        </button>
        <button onClick={() => navigate("/faq")}>
          <span className="supportCardTitle"><i aria-hidden="true">?</i><strong>자주 묻는 질문</strong><b>{counts ? `${counts.faqs}건` : '확인 중'} 〉</b></span>
          <span>자주 묻는 질문을 확인합니다.</span>
        </button>
        <article>
          <strong>전국 대표번호</strong>
          {serviceCompany.representativePhone ? (
            <a
              href={`tel:${serviceCompany.representativePhone.replaceAll("-", "")}`}
            >
              {serviceCompany.representativePhone}
            </a>
          ) : (
            <b>확정 전</b>
          )}
          <span>{hours}</span>
          <span>서비스 이용, 회원정보, 요청·견적 및 거래 진행 문의를 도와드립니다.</span>
          <small>문의 시 요청번호나 거래번호를 준비해 주세요. 비밀번호, 주민등록번호, 카드 전체번호는 상담원이 요청하지 않습니다.</small>
        </article>
        <article>
          <strong>이메일 문의</strong>
          {serviceCompany.customerServiceEmail ? (
            <a href={`mailto:${serviceCompany.customerServiceEmail}`}>
              {serviceCompany.customerServiceEmail}
            </a>
          ) : (
            <b>확정 전</b>
          )}
          <span>서비스 이용 문의를 이메일로 접수합니다.</span>
          <small>통화량이 많거나 운영시간 외에는 문의 내용을 남겨 주세요.</small>
        </article>
      </section>
    </CustomerAppLayout>
  );
}

export function CompanyInfoPage() {
  return (
    <CustomerAppLayout>
      <section className="pageHeading companyHeading">
        <p>회사 소개</p>
        <h1>기업의 내일을 기술로 연결합니다</h1>
        <span>
          AI와 데이터, 현장을 이해하는 기술로 고객의 디지털 전환을 설계하고
          실행합니다.
        </span>
      </section>
      <section className="companyIntroduction">
        <article className="companyLead">
          <p>
            주식회사 디에이치는 기업용 소프트웨어와 AI 솔루션, 스마트팩토리, IT
            컨설팅을 기획부터 구축·운영까지 연결하는 기술 기업입니다.
          </p>
          <a
            href="https://www.dh9.kr"
            target="_blank"
            rel="noopener noreferrer"
          >
            디에이치 공식 홈페이지
          </a>
        </article>
        <div className="companyValues">
          <article>
            <span>01</span>
            <h2>고객 중심</h2>
            <p>
              현장의 문제와 목표를 먼저 이해하고 실제 업무에 도움이 되는 결과를
              만듭니다.
            </p>
          </article>
          <article>
            <span>02</span>
            <h2>실용적 혁신</h2>
            <p>
              기술 자체보다 사용성과 운영 효과를 기준으로 지속 가능한 해법을
              설계합니다.
            </p>
          </article>
          <article>
            <span>03</span>
            <h2>신뢰의 파트너십</h2>
            <p>
              구축 이후의 안정적인 운영과 개선까지 함께하는 장기 파트너를
              지향합니다.
            </p>
          </article>
        </div>
        <section className="companyBusiness">
          <header>
            <p>주요 사업</p>
            <h2>사업 영역</h2>
          </header>
          <div>
            <article>
              <h3>AI 솔루션</h3>
              <p>
                AI 챗봇, RAG 기반 지식검색과 데이터 분석으로 업무 활용도를
                높입니다.
              </p>
            </article>
            <article>
              <h3>스마트팩토리</h3>
              <p>
                MES·POP·IoT와 설비 연계, 실시간 모니터링으로 제조 현장을
                연결합니다.
              </p>
            </article>
            <article>
              <h3>기업용 소프트웨어</h3>
              <p>
                B2B SaaS, 업무 시스템과 클라우드 전환을 통해 디지털 업무 기반을
                구축합니다.
              </p>
            </article>
            <article>
              <h3>IT 컨설팅</h3>
              <p>
                디지털 전환 전략, 프로세스 혁신과 운영 지원으로 실행 가능한
                변화를 만듭니다.
              </p>
            </article>
          </div>
        </section>
        <section className="companyBrand">
          <div>
            <p>제공 서비스</p>
            <h2>수달 라이프</h2>
            <span>
              고객과 검증된 생활서비스 전문가를 안전하게 연결하고
              요청·견적·선택·진행·사후관리 기록을 한 흐름으로 관리하는
              생활서비스 플랫폼입니다.
            </span>
          </div>
          <dl>
            <div>
              <dt>법인명</dt>
              <dd>{serviceCompany.companyName}</dd>
            </div>
            <div>
              <dt>대표자</dt>
              <dd>{serviceCompany.ceoName}</dd>
            </div>
            <div>
              <dt>사업자등록번호</dt>
              <dd>{serviceCompany.businessRegistrationNumber}</dd>
            </div>
            <div>
              <dt>주소</dt>
              <dd>{serviceCompany.address}</dd>
            </div>
          </dl>
        </section>
      </section>
    </CustomerAppLayout>
  );
}

export function CustomerNotFoundPage() {
  return (
    <CustomerAppLayout>
      <section className="pageHeading">
        <p>404</p>
        <h1>요청하신 화면을 찾을 수 없습니다.</h1>
        <button className="detailRequestButton" onClick={() => navigate("/")}>
          고객 홈으로
        </button>
      </section>
    </CustomerAppLayout>
  );
}
