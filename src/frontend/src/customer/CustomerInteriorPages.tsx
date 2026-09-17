import {
  useCallback,
  useEffect,
  useState,
  type FormEvent,
  type PropsWithChildren,
} from "react";
import { useAuthentication } from "../auth/AuthenticationContext";
import { createLoginPath, navigate } from "../auth/routing";
import { CustomerAppLayout } from "./CustomerAppLayout";
import { loadDefaultAddress } from "./defaultAddress";
import { interiorApi } from "./interiorApi";
import type {
  InteriorHome,
  InteriorProjectDetail,
  InteriorProjectList,
  InteriorQuote,
  InteriorRequestCandidate,
  InteriorService,
} from "./interiorTypes";
import "./customerInterior.css";
import "./customerInteriorVisuals.css";
import { soodalConfirm, soodalPrompt } from '../components/soodalDialog'
import { quoteSummaryText,quoteTermsText } from '../quotes/displayText'
import { ServiceThumbnail, useServiceVisualSettings } from '../serviceVisuals/ServiceVisual'

const money = (value: number, currency = "KRW") =>
  `${new Intl.NumberFormat("ko-KR").format(value)} ${currency === "KRW" ? "원" : currency}`;
const date = (value: string | null) =>
  value
    ? new Intl.DateTimeFormat("ko-KR", {
        dateStyle: "medium",
        timeStyle: value.includes("T") ? "short" : undefined,
      }).format(new Date(value))
    : "—";
const message = (reason: unknown) =>
  reason instanceof Error ? reason.message : "요청을 처리하지 못했습니다.";
const statusLabels: Record<string, string> = {
  DRAFT: "작성 중", OPEN: "접수", RECEIVED: "접수", SUBMITTED: "제출 완료",
  PUBLISHED: "공개", MATCHING: "전문가 찾는 중", QUOTING: "견적 접수 중",
  SELECTED: "전문가 선택", ACTIVE: "진행 중", IN_PROGRESS: "진행 중",
  COMPLETED: "완료", CANCELLED: "취소", NOT_SCHEDULED: "실측 일정 미정",
  PROPOSED: "일정 제안", SCHEDULED: "예정", CONFIRMED: "확정",
  NOT_CREATED: "계약 전", CREATED: "작성 완료", CORRECTION_REQUIRED: "보완 필요",
  PENDING: "대기", PAID: "지급 완료", NONE: "없음", PASSED: "통과",
  FAILED: "미통과", REQUESTED: "요청", APPROVED: "승인", ACCEPTED: "승인",
  REJECTED: "반려", UNDER_REVIEW: "검토 중", RESOLVED: "해결 완료", CLOSED: "종료",
};
const statusLabel = (value: string | null | undefined) => value ? statusLabels[value] ?? "상태 확인 중" : "없음";
const online = () => {
  if (!navigator.onLine)
    throw new Error("변경 작업은 온라인에서만 처리할 수 있습니다.");
};

function InteriorLayout({
  title,
  description,
  children,
}: PropsWithChildren<{ title: string; description: string }>) {
  const path = window.location.pathname;
  const visualSettings = useServiceVisualSettings();
  const nav = [
    ["/interior", "인테리어 홈"],
    ["/customer/interior/projects", "내 프로젝트"],
    ["/customer/interior/projects/new", "상담 연결"],
  ];
  return (
    <CustomerAppLayout>
      <section className={`interiorHero${visualSettings.bannersEnabled ? " hasFeatureBanner" : ""}`}>
        {visualSettings.bannersEnabled && <img src="/service-visuals/feature-banners/interior.webp" alt="" aria-hidden="true" />}
        <p>수달 인테리어</p>
        <h1>{title}</h1>
        <span>{description}</span>
      </section>
      <div className="interiorShell">
        <nav aria-label="수달 인테리어 메뉴">
          {nav.map(([href, label]) => (
            <button
              className={path === href ? "isActive" : ""}
              onClick={() => navigate(href)}
              key={href}
            >
              {label}
            </button>
          ))}
        </nav>
        <section className="interiorContent">{children}</section>
      </div>
    </CustomerAppLayout>
  );
}

export function CustomerInteriorHomePage() {
  const { user } = useAuthentication();
  const customer = user?.roles.includes("CUSTOMER") ?? false;
  const [services, setServices] = useState<InteriorService[]>([]);
  const [serviceQuery, setServiceQuery] = useState("");
  const [home, setHome] = useState<InteriorHome | null>(null);
  const [requests,setRequests]=useState<InteriorRequestCandidate[]>([]);
  const [error, setError] = useState("");
  const normalizedServiceQuery = serviceQuery.trim().toLocaleLowerCase("ko-KR");
  const visibleServices = normalizedServiceQuery
    ? services.filter((item) => `${item.code} ${item.categoryPath}`.toLocaleLowerCase("ko-KR").includes(normalizedServiceQuery))
    : services;
  useEffect(() => {
    interiorApi
      .services()
      .then(setServices)
      .catch((reason) => setError(message(reason)));
    if (customer)
      Promise.all([interiorApi.home(),interiorApi.candidates()])
        .then(([dashboard,candidates])=>{setHome(dashboard);setRequests(candidates)})
        .catch((reason) => setError(message(reason)));
  }, [customer]);
  const start = (id?: string) => {
    const target = id
      ? `/customer/requests/new?service=${id}&domain=INTERIOR`
      : "/customer/interior/projects/new";
    navigate(customer ? target : createLoginPath(target));
  };
  return (
    <InteriorLayout
      title="상담부터 완공 후 하자관리까지"
      description="실측·상세견적·계약·공정·검사 기록을 한 프로젝트에서 확인하세요."
    >
      {error && <p className="interiorError">{error}</p>}
      <section className="interiorIntro">
        <div>
          <h2>공간과 요구사항을 알려주세요</h2>
          <p>
            요청을 공개해 실측과 견적을 준비합니다. 상세주소와 연락처는 실측
            전문가 확정 전까지 공개되지 않습니다.
          </p>
          <button onClick={() => start()}>상담 시작</button>
        </div>
        <ol>
          <li>상담·요청</li>
          <li>현장 실측</li>
          <li>견적·설계 비교</li>
          <li>계약·공사</li>
          <li>검사·완료</li>
          <li>하자·A/S</li>
        </ol>
      </section>
      {customer && home && (
        <section className="interiorDashboard">
          <h2>내 인테리어</h2>
          <div>
            {[
              ["전체 프로젝트", home.projectCount],
              ["진행 중", home.activeProjectCount],
              ["최근 완료", home.completedProjectCount],
              ["하자/A/S", home.defectCount],
              ["분쟁", home.disputeCount],
            ].map(([label, count]) => (
              <article key={String(label)}>
                <span>{label}</span>
                <strong>{count}</strong>
              </article>
            ))}
          </div>
          <button onClick={() => navigate("/customer/interior/projects")}>
            내 프로젝트 보기
          </button>
        </section>
      )}
      {customer&&<section className="interiorSection interiorRequestHistory"><header><div><h2>내 인테리어 요청</h2><span>상담 요청을 등록한 직후부터 이곳에서 확인할 수 있습니다.</span></div><button onClick={()=>navigate('/customer/interior/projects/new')}>전체 요청 보기</button></header><div className="interiorCandidateList">{requests.slice(0,4).map(item=><article key={item.id}><span>수달 인테리어 · {item.areaName}</span><h3>{item.title}</h3><small>{date(item.createdAt)} · {item.projectCreated?'프로젝트 연결됨':'전문가 제안 대기'}</small><button onClick={()=>navigate(item.projectCreated?'/customer/interior/projects':`/customer/requests/${item.id}`)}>요청 내용 보기</button></article>)}{requests.length===0&&<p className="interiorEmpty">등록한 수달 인테리어 요청이 없습니다.</p>}</div></section>}
      <section className="interiorSection interiorTargetServices">
        <header>
          <div>
            <h2>대상 서비스</h2>
            <span>운영 중인 INT 계열 카테고리만 표시합니다.</span>
          </div>
          <strong>{normalizedServiceQuery ? `검색 결과 ${visibleServices.length}개` : `전체 ${services.length}개`}</strong>
        </header>
        <label className="interiorServiceSearch">
          <span>대상 서비스 검색</span>
          <input
            type="search"
            value={serviceQuery}
            onChange={(event) => setServiceQuery(event.target.value)}
            placeholder="서비스명·코드·카테고리 검색"
            autoComplete="off"
          />
          {serviceQuery && <button type="button" onClick={() => setServiceQuery("")} aria-label="검색어 지우기">지우기</button>}
        </label>
        <div className="interiorServices">
          {visibleServices.map((item) => (
            <button onClick={() => start(item.id)} key={item.id}>
              <ServiceThumbnail code={item.code} name={item.categoryPath} className="interiorServiceThumbnail" />
              <span className="interiorServiceCopy">
                <small>{item.code}</small>
                <strong>{item.categoryPath.split(" > ").at(-1)}</strong>
                <span>{item.categoryPath}</span>
              </span>
            </button>
          ))}
          {services.length > 0 && visibleServices.length === 0 && <p className="interiorServiceSearchEmpty">검색어와 일치하는 대상 서비스가 없습니다.</p>}
        </div>
      </section>
      <section className="interiorPolicy">
        <h2>대금 지급 안내</h2>
        <p>
          플랫폼은 지급계획과 확인 이력만 관리합니다. 실제 PG, 에스크로, 플랫폼
          수납 또는 자동송금은 연동되어 있지 않습니다.
        </p>
      </section>
    </InteriorLayout>
  );
}

function ProjectCard({ item }: { item: InteriorProjectList }) {
  return (
    <button
      className="interiorProjectCard"
      onClick={() => navigate(`/customer/interior/projects/${item.id}`)}
    >
      <div className="interiorProjectLead">
        <ServiceThumbnail name={item.serviceName} />
        <div><span>{item.projectNumber} · {item.areaName}</span><strong>{item.serviceName}</strong></div>
      </div>
      <b>{item.statusDisplay}</b>
      <dl>
        <div>
          <dt>실측</dt>
          <dd>{statusLabel(item.siteVisitStatus)}</dd>
        </div>
        <div>
          <dt>계약</dt>
          <dd>{statusLabel(item.contractStatus)}</dd>
        </div>
        <div>
          <dt>변경</dt>
          <dd>{statusLabel(item.changeStatus)}</dd>
        </div>
        <div>
          <dt>검사</dt>
          <dd>{statusLabel(item.inspectionStatus)}</dd>
        </div>
      </dl>
      <div className="stageMini">
        {item.stageProgress.map((stage) => (
          <span key={stage.id}>
            {stage.name} {stage.progressPercent}%
          </span>
        ))}
      </div>
      <small>
        하자/A/S {item.defectCount} · 분쟁 {item.disputeCount}
      </small>
    </button>
  );
}

export function CustomerInteriorProjectsPage() {
  const [items, setItems] = useState<InteriorProjectList[]>([]);
  const [error, setError] = useState("");
  useEffect(() => {
    interiorApi
      .projects()
      .then(setItems)
      .catch((reason) => setError(message(reason)));
  }, []);
  return (
    <InteriorLayout
      title="내 인테리어 프로젝트"
      description="각 프로젝트의 실제 저장 상태와 단계별 진행률을 확인하세요."
    >
      <div className="interiorToolbar">
        <button onClick={() => navigate("/customer/interior/projects/new")}>
          상담 프로젝트 연결
        </button>
      </div>
      {error && <p className="interiorError">{error}</p>}
      <div className="interiorProjectList">
        {items.map((item) => (
          <ProjectCard item={item} key={item.id} />
        ))}
        {items.length === 0 && (
          <p className="interiorEmpty">
            아직 연결된 인테리어 프로젝트가 없습니다.
          </p>
        )}
      </div>
    </InteriorLayout>
  );
}

export function NewCustomerInteriorProjectPage() {
  const [items, setItems] = useState<InteriorRequestCandidate[]>([]);
  const [busy, setBusy] = useState("");
  const [error, setError] = useState("");
  useEffect(() => {
    interiorApi
      .candidates()
      .then(setItems)
      .catch((reason) => setError(message(reason)));
  }, []);
  const create = async (item: InteriorRequestCandidate) => {
    try {
      online();
      setBusy(item.id);
      const project = await interiorApi.create(item.id);
      navigate(`/customer/interior/projects/${project.id}`);
    } catch (reason) {
      setError(message(reason));
      setBusy("");
    }
  };
  return (
    <InteriorLayout
      title="상담 요청을 프로젝트로 연결"
      description="기존 인테리어 요청을 재사용하므로 같은 정보를 다시 입력하지 않습니다."
    >
      {error && <p className="interiorError">{error}</p>}
      <section className="interiorStartGuide">
        <h2>새 요청이 필요한가요?</h2>
        <p>
          공간 유형, 시·도/시·군·구, 예산, 희망 일정, 요구사항과 사진은 기존
          동적 요청서에서 입력합니다. 상세주소는 선택된 전문가에게 공개할
          단계에서 별도로 확인합니다.
        </p>
        <button onClick={() => navigate("/services/search?q=인테리어")}>
          인테리어 서비스 선택
        </button>
      </section>
      <div className="interiorCandidateList">
        {items.map((item) => (
          <article key={item.id}>
            <span>
              {item.serviceName} · {item.areaName}
            </span>
            <h3>{item.title}</h3>
            <small>
              {statusLabel(item.statusCode)} · {date(item.createdAt)}
            </small>
            {item.projectCreated ? (
              <>
                <p className="interiorCandidateHelp">이미 프로젝트로 연결되었습니다. 내 프로젝트에서 진행 상황과 다음 업무를 확인하세요.</p>
                <button onClick={() => navigate("/customer/interior/projects")}>내 프로젝트 보기</button>
              </>
            ) : item.statusCode === "DRAFT" ? (
              <>
                <p className="interiorCandidateHelp">작성 중인 요청입니다. 이어서 작성한 뒤 마지막 단계에서 요청을 공개하면 프로젝트를 시작할 수 있습니다.</p>
                <button onClick={() => navigate(`/customer/requests/new?draft=${item.id}&domain=INTERIOR`)}>이어서 작성</button>
              </>
            ) : item.statusCode === "CANCELLED" ? (
              <>
                <p className="interiorCandidateHelp">취소된 요청은 프로젝트로 시작할 수 없습니다.</p>
                <button disabled>취소된 요청</button>
              </>
            ) : (
              <button
                disabled={busy === item.id}
                onClick={() => void create(item)}
              >
                {busy === item.id ? "연결 중…" : "프로젝트 시작"}
              </button>
            )}
          </article>
        ))}
        {items.length === 0 && (
          <p className="interiorEmpty">
            인테리어 요청이 없습니다. 서비스를 선택해 상담 요청을 먼저 작성해
            주세요.
          </p>
        )}
      </div>
    </InteriorLayout>
  );
}

function Files({
  items,
}: {
  items: {
    id: string;
    fileName: string;
    purpose: string;
    downloadUrl: string | null;
    publicationMessage: string | null;
  }[];
}) {
  return items.length ? (
    <ul className="interiorFiles">
      {items.map((file) => (
        <li key={file.id}>
          {file.downloadUrl ? (
            <a href={file.downloadUrl}>{file.fileName}</a>
          ) : (
            <span>{file.fileName}</span>
          )}
          <small>
            {file.purpose} ·{" "}
            {file.downloadUrl
              ? "공개 가능"
              : (file.publicationMessage ?? "안전 확인 필요")}
          </small>
        </li>
      ))}
    </ul>
  ) : (
    <p className="interiorEmpty">등록된 파일이 없습니다.</p>
  );
}

type PendingInteriorFile = {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  previewUrl: string | null;
};

function PendingFiles({
  items,
  onRemove,
}: {
  items: PendingInteriorFile[];
  onRemove: (file: PendingInteriorFile) => void;
}) {
  if (!items.length)
    return <p className="interiorEmpty">선택한 파일이 없습니다.</p>;
  return (
    <div className="interiorPendingFiles">
      {items.map((file) => (
        <article key={file.id}>
          {file.previewUrl && file.contentType.startsWith("image/") ? (
            <img src={file.previewUrl} alt={`${file.fileName} 미리보기`} />
          ) : file.previewUrl ? (
            <a href={file.previewUrl} target="_blank" rel="noreferrer">
              PDF 미리보기
            </a>
          ) : (
            <span>미리보기 없음</span>
          )}
          <strong>{file.fileName}</strong>
          <small>
            {(file.sizeBytes / 1024 / 1024).toFixed(1)}MB · 비공개 · 파일 안전 검사 준비 중
          </small>
          <button type="button" className="secondary" onClick={() => onRemove(file)}>
            첨부에서 제외
          </button>
        </article>
      ))}
    </div>
  );
}
function Section({
  id,
  title,
  children,
}: {
  id: string;
  title: string;
  children: React.ReactNode;
}) {
  return (
    <section id={id} className="interiorDetailSection">
      <h2>{title}</h2>
      {children}
    </section>
  );
}

function ProviderSelectionAction({
  quote,
  busy,
  onSelect,
}: {
  quote: InteriorQuote;
  busy: boolean;
  onSelect: () => void;
}) {
  const [open, setOpen] = useState(false);
  if (quote.selected)
    return (
      <p className="providerSelectionMark">최종 선택한 전문가 · 계약 준비 중</p>
    );
  if (!quote.canSelect) return null;
  return (
    <>
      <button
        className="providerSelectionButton"
        disabled={busy}
        onClick={() => setOpen(true)}
      >
        이 전문가로 최종 선택
      </button>
      {open && (
        <div
          className="providerSelectionBackdrop"
          onClick={() => setOpen(false)}
        >
          <section
            role="dialog"
            aria-modal="true"
            aria-labelledby="provider-selection-title"
            onClick={(event) => event.stopPropagation()}
          >
            <span>최종 시공 전문가 선택</span>
            <h3 id="provider-selection-title">
              {quote.providerName}을 선택할까요?
            </h3>
            <dl>
              <div>
                <dt>견적</dt>
                <dd>
                  {money(quote.totalAmount, quote.currencyCode)} · {quote.revisionNo}차 견적
                </dd>
              </div>
              <div>
                <dt>기간</dt>
                <dd>{quote.duration ?? "기간 미정"}</dd>
              </div>
              <div>
                <dt>시작 가능일</dt>
                <dd>{date(quote.availableStartAt)}</dd>
              </div>
              <div>
                <dt>신뢰도·후기</dt>
                <dd>
                  {quote.trustDisplay} · 공개 리뷰 {quote.publicReviewCount}
                </dd>
              </div>
              <div>
                <dt>설계</dt>
                <dd>
                  {quote.designVersion
                    ? `${quote.designVersion}차 · ${statusLabel(quote.designStatus)}`
                    : "연결 설계 없음"}
                </dd>
              </div>
            </dl>
            <p>{quote.summary}</p>
            <small>
              전문가 선택은 계약 체결이 아닙니다. 선택 후 확정 견적을 기준으로
              계약 내용을 별도로 확인하고 동의합니다. 선택하면 다른 후보로
              변경할 수 없습니다.
            </small>
            <div>
              <button className="secondary" onClick={() => setOpen(false)}>
                다시 비교
              </button>
              <button
                disabled={busy}
                onClick={() => {
                  setOpen(false);
                  onSelect();
                }}
              >
                확인하고 선택
              </button>
            </div>
          </section>
        </div>
      )}
    </>
  );
}

export function CustomerInteriorProjectPage({ id }: { id: string }) {
  const [item, setItem] = useState<InteriorProjectDetail | null>(null);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState("");
  const [detailAddress, setDetailAddress] = useState("");
  const [defect, setDefect] = useState({
    subject: "",
    location: "",
    description: "",
    workStageId: "",
    contractVersion: "",
  });
  const [dispute, setDispute] = useState({
    subject: "",
    description: "",
    workStageId: "",
    contractChangeId: "",
  });
  const [defectFiles, setDefectFiles] = useState<PendingInteriorFile[]>([]);
  const [disputeFiles, setDisputeFiles] = useState<PendingInteriorFile[]>([]);
  const load = useCallback(
    () =>
      interiorApi
        .project(id)
        .then((value) => {
          setItem(value);
          setDetailAddress((current) => current || value.detailAddress || "");
        })
        .catch((reason) => setError(message(reason))),
    [id],
  );
  useEffect(() => {
    void load();
  }, [load]);
  useEffect(() => {
    void loadDefaultAddress()
      .then((value) => setDetailAddress((current) => current || value?.fullAddress || ""))
      .catch(() => undefined);
  }, []);
  const act = async (
    key: string,
    action: () => Promise<unknown>,
    onSuccess?: () => void,
  ) => {
    try {
      online();
      setBusy(key);
      setError("");
      await action();
      onSuccess?.();
      await load();
    } catch (reason) {
      setError(message(reason));
    } finally {
      setBusy("");
    }
  };
  const upload = async (
    target: "defect" | "dispute",
    selected: FileList | null,
  ) => {
    if (!selected) return;
    try {
      online();
      for (const file of Array.from(selected)) {
        const value = await interiorApi.upload(id, file);
        const pending: PendingInteriorFile = {
          ...value,
          previewUrl: ["image/jpeg", "image/png", "application/pdf"].includes(
            file.type,
          )
            ? URL.createObjectURL(file)
            : null,
        };
        if (target === "defect")
          setDefectFiles((current) => [...current, pending]);
        else setDisputeFiles((current) => [...current, pending]);
      }
    } catch (reason) {
      setError(message(reason));
    }
  };
  const removePending = (
    target: "defect" | "dispute",
    file: PendingInteriorFile,
  ) => {
    if (file.previewUrl) URL.revokeObjectURL(file.previewUrl);
    if (target === "defect")
      setDefectFiles((current) => current.filter((value) => value.id !== file.id));
    else
      setDisputeFiles((current) => current.filter((value) => value.id !== file.id));
  };
  const clearPending = (target: "defect" | "dispute") => {
    const current = target === "defect" ? defectFiles : disputeFiles;
    current.forEach((file) => {
      if (file.previewUrl) URL.revokeObjectURL(file.previewUrl);
    });
    if (target === "defect") setDefectFiles([]);
    else setDisputeFiles([]);
  };
  const submitDefect = (event: FormEvent) => {
    event.preventDefault();
    void act("defect", () =>
      interiorApi.defect(id, {
        ...defect,
        workStageId: defect.workStageId || null,
        contractVersion: defect.contractVersion
          ? Number(defect.contractVersion)
          : null,
        fileIds: defectFiles.map((file) => file.id),
        idempotencyKey: `customer-interior-defect-${crypto.randomUUID()}`,
      }),
      () => {
        setDefect({
          subject: "",
          location: "",
          description: "",
          workStageId: "",
          contractVersion: "",
        });
        clearPending("defect");
      },
    );
  };
  const submitDispute = (event: FormEvent) => {
    event.preventDefault();
    void act("dispute", () =>
      interiorApi.dispute(id, {
        ...dispute,
        workStageId: dispute.workStageId || null,
        contractChangeId: dispute.contractChangeId || null,
        fileIds: disputeFiles.map((file) => file.id),
        idempotencyKey: `customer-interior-dispute-${crypto.randomUUID()}`,
      }),
      () => {
        setDispute({
          subject: "",
          description: "",
          workStageId: "",
          contractChangeId: "",
        });
        clearPending("dispute");
      },
    );
  };
  if (!item)
    return (
      <InteriorLayout
        title="프로젝트 확인"
        description="프로젝트 정보를 불러오고 있습니다."
      >
        {error && <p className="interiorError">{error}</p>}
      </InteriorLayout>
    );
  const tabs = [
    ["summary", "요약"],
    ["visits", "실측"],
    ["quotes", "견적·설계"],
    ["contract", "계약·지급"],
    ["stages", "공정·검사"],
    ["changes", "변경"],
    ["completion", "완료"],
    ["aftercare", "하자/A/S"],
    ["disputes", "분쟁"],
    ["events", "이력"],
  ];
  return (
    <InteriorLayout
      title={item.serviceName}
      description={`${item.projectNumber} · ${item.statusDisplay}`}
    >
      <nav className="interiorTabs">
        {tabs.map(([anchor, label]) => (
          <a href={`#${anchor}`} key={anchor}>
            {label}
          </a>
        ))}
      </nav>
      {error && <p className="interiorError">{error}</p>}
      {item.providerSelection && (
        <article className="providerSelectionSummary">
          <span>최종 시공 전문가 선택 완료</span>
          <h2>{item.providerSelection.providerName}</h2>
          <strong>
            {money(
              item.providerSelection.totalAmount,
              item.providerSelection.currencyCode,
            )}{" "}
            · 견적 {item.providerSelection.revisionNo}차
          </strong>
          <p>{item.providerSelection.summary}</p>
          <small>
            선택 {date(item.providerSelection.selectedAt)} · 계약은 아직
            체결되지 않았으며 별도 계약 확인·동의가 필요합니다.
          </small>
          {item.contractDeadline && (
            <p className="contractDeadlineNotice">
              {item.contractDeadline.isPaused
                ? "보완 요청 또는 분쟁 처리 중이어서 자동 만료가 일시 중지되었습니다."
                : `${item.contractDeadline.phaseCode === "PROVIDER_SUBMISSION" ? "전문가 계약자료 제출" : "고객 계약 확인"} 기한: ${date(item.contractDeadline.dueAt)}`}
            </p>
          )}
          {!item.contract?.effectiveAt && (
            <div className="providerSelectionActions">
              <button disabled={busy!==""} onClick={async ()=>{if(await soodalConfirm("전문가 선택을 해제하고 예약 수수료를 반환하시겠습니까?"))void act("release-provider",()=>interiorApi.releaseProvider(id,false))}}>{busy==="release-provider"?"해제 중…":"전문가 선택 해제"}</button>
              <button className="danger" disabled={busy!==""} onClick={async ()=>{if(await soodalConfirm("프로젝트를 취소하고 예약 수수료를 반환하시겠습니까?"))void act("cancel-project",()=>interiorApi.releaseProvider(id,true))}}>{busy==="cancel-project"?"취소 중…":"프로젝트 취소"}</button>
            </div>
          )}
        </article>
      )}
      <Section id="summary" title="프로젝트 요약">
        <div className="interiorSummary">
          <article>
            <span>현재 단계</span>
            <strong>{item.statusDisplay}</strong>
          </article>
          <article>
            <span>시·도/시·군·구</span>
            <strong>{item.areaName}</strong>
          </article>
          <article>
            <span>실측 당시 신뢰도</span>
            <strong>{item.siteVisitTrustSnapshot ?? "신규·평가중"}</strong>
          </article>
          <article>
            <span>계약 당시 신뢰도</span>
            <strong>{item.contractorTrustSnapshot ?? "신규·평가중"}</strong>
          </article>
        </div>
        <div className="interiorSummaryService"><ServiceThumbnail name={item.serviceName} /><div><span>하위 서비스</span><strong>{item.serviceName}</strong><h3>{item.requestTitle}</h3></div></div>
        <p>{item.requestDescription ?? "추가 설명 없음"}</p>
        <small>
          상세주소는 본인 프로젝트에서만 보입니다:{" "}
          {item.detailAddress ?? "미입력"}
        </small>
      </Section>
      <Section id="visits" title="현장 실측">
        {item.siteVisits.map((visit) => (
          <article className="interiorRecord" key={visit.id}>
            <header>
              <div>
                <span>{visit.statusDisplay}</span>
                <h3>{visit.providerName}</h3>
              </div>
              <b>
                {visit.trustDisplay} · 공개 리뷰 {visit.publicReviewCount}
              </b>
            </header>
            <p>
              {date(visit.scheduledStartAt)} ~ {date(visit.scheduledEndAt)}
            </p>
            <dl>
              <div>
                <dt>실측 비용</dt>
                <dd>{visit.proposedVisitFee ? money(visit.proposedVisitFee, "KRW") : "무료"}</dd>
              </div>
              <div>
                <dt>제안 조건</dt>
                <dd>{visit.proposalTerms ?? "별도 조건 없음"}</dd>
              </div>
            </dl>
            {visit.canSelect && (
              <div className="siteVisitSelection">
                <label>
                  실측할 상세주소 <b>*</b>
                  <input
                    value={detailAddress}
                    maxLength={500}
                    autoComplete="street-address"
                    placeholder="예: 효동로 72-1, 3층 301호"
                    onChange={(event) => setDetailAddress(event.target.value)}
                  />
                </label>
                <small>
                  일정 선택 후 이 주소와 연락처는 선택된 실측 전문가에게만
                  공개됩니다. 다른 후보에게는 공개되지 않습니다.
                </small>
                <button
                  disabled={busy === "visit" || detailAddress.trim().length < 2}
                  onClick={() =>
                    void act("visit", () =>
                      interiorApi.selectVisit(id, visit.id, detailAddress.trim()),
                    )
                  }
                >
                  상세주소 확인 후 이 실측 일정 선택
                </button>
              </div>
            )}
            {visit.completedAt && (
              <>
                <dl>
                  <div>
                    <dt>실측 요약</dt>
                    <dd>{visit.measurementSummary ?? "—"}</dd>
                  </div>
                  <div>
                    <dt>제약사항</dt>
                    <dd>{visit.constraint ?? "없음"}</dd>
                  </div>
                  <div>
                    <dt>위험요소</dt>
                    <dd>{visit.riskNote ?? "없음"}</dd>
                  </div>
                </dl>
                <table>
                  <thead>
                    <tr>
                      <th>항목</th>
                      <th>위치</th>
                      <th>값</th>
                      <th>메모</th>
                    </tr>
                  </thead>
                  <tbody>
                    {visit.measurements.map((m, index) => (
                      <tr key={`${m.key}-${index}`}>
                        <td>{m.key}</td>
                        <td>{m.location ?? "—"}</td>
                        <td>
                          {m.value ?? m.text ?? "—"} {m.unit}
                        </td>
                        <td>{m.note ?? "—"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                <Files items={visit.files} />
              </>
            )}
          </article>
        ))}
        {item.siteVisits.length === 0 && (
          <p className="interiorEmpty">현재 제안된 실측 일정이 없습니다.</p>
        )}
      </Section>
      <Section id="quotes" title="견적·설계 비교">
        <p className="interiorHint">
          가격이나 신뢰도만으로 자동 추천하지 않습니다. 공종·자재·기간·조건을
          함께 확인하세요.
        </p>
        <div className="quoteCompare">
          {item.quotes.map((quote) => (
            <article key={quote.revisionId}>
              <span>
                {quote.purposeDisplay} · Revision {quote.revisionNo}
              </span>
              <h3>{quote.providerName}</h3>
              <strong>{money(quote.totalAmount, quote.currencyCode)}</strong>
              <small>
                VAT {money(quote.vatAmount, quote.currencyCode)} ·{" "}
                {quote.duration ?? "기간 미정"} · {quote.trustDisplay} · 리뷰{" "}
                {quote.publicReviewCount}
              </small>
              <p>{quoteSummaryText(quote.summary,quote.items.map(line=>({itemName:line.name})))}</p>
              <table>
                <thead>
                  <tr>
                    <th>공종/공간</th>
                    <th>항목·자재</th>
                    <th>금액</th>
                  </tr>
                </thead>
                <tbody>
                  {quote.items.map((line) => (
                    <tr key={line.lineNo}>
                      <td>
                        {line.trade ?? "—"} / {line.space ?? "—"}
                      </td>
                      <td>
                        {line.name}
                        <small>{line.materialSpec ?? line.description}</small>
                      </td>
                      <td>{money(line.lineTotal, quote.currencyCode)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              <p>{quoteTermsText(quote.terms)}</p>
              <small>
                제안 {date(quote.submittedAt)} · 시작 가능{" "}
                {date(quote.availableStartAt)} · 유효 {date(quote.validUntil)} ·
                설계{" "}
                {quote.designVersion
                  ? `${quote.designVersion}차 (${statusLabel(quote.designStatus)})`
                  : "연결 없음"}
              </small>
              <ProviderSelectionAction
                quote={quote}
                busy={busy === "provider-selection"}
                onSelect={() =>
                  void act("provider-selection", () =>
                    interiorApi.selectProvider(
                      id,
                      quote.revisionId,
                      item.rowVersion,
                    ),
                  )
                }
              />
            </article>
          ))}
        </div>
        <h3>설계 차수</h3>
        {item.designs.map((design) => (
          <article className="interiorRecord" key={design.id}>
            <b>
              {design.versionNo}차 · {statusLabel(design.statusCode)}
            </b>
            <h3>{design.title}</h3>
            <p>{design.description ?? "설명 없음"}</p>
            <Files items={design.files} />
          </article>
        ))}
      </Section>
      <Section id="contract" title="계약·지급계획">
        {item.contract ? (
          <>
            <article className="interiorContract">
              <header>
                <div>
                  <span>계약 {item.contract.version}차</span>
                  <h3>{item.contract.providerName}</h3>
                </div>
                <strong>
                  {money(item.contract.amount, item.contract.currencyCode)}
                </strong>
              </header>
              <dl>
                <div><dt>계약 체결일</dt><dd>{item.contract.contractSignedDate}</dd></div>
                <div>
                  <dt>공사기간</dt>
                  <dd>
                    {item.contract.plannedStartDate} ~{" "}
                    {item.contract.plannedCompletionDate}
                  </dd>
                </div>
                <div>
                  <dt>고객 동의</dt>
                  <dd>{date(item.contract.customerAgreedAt)}</dd>
                </div>
                <div>
                  <dt>전문가 동의</dt>
                  <dd>{date(item.contract.providerAgreedAt)}</dd>
                </div>
                <div>
                  <dt>효력 발생</dt>
                  <dd>{date(item.contract.effectiveAt)}</dd>
                </div>
              </dl>
              <p className="interiorHint">수달 라이프는 계약 당사자가 아닙니다. 고객과 전문가가 직접 체결한 계약서와 아래 관리정보가 일치하는지 확인해 주세요.</p>
              <Files items={item.contract.documents} />
              {item.contract.customerMismatchReason && <p className="interiorError">재등록 요청: {item.contract.customerMismatchReason}</p>}
              <details>
                <summary>계약 범위·일정·보증 확인</summary>
                <pre>{item.contract.scope}</pre>
                <pre>{item.contract.schedule}</pre>
                <pre>{item.contract.warranty ?? "보증 조건 미입력"}</pre>
              </details>
              {!item.contract.customerAgreedAt && item.contract.statusCode !== 'CORRECTION_REQUIRED' && <div className="interiorActions"><button disabled={busy === "agree"} onClick={() => void act("agree", () => interiorApi.reviewContract(id,item.contract!.id,true,null))}>내가 체결한 계약서와 일치합니다</button><button className="danger" disabled={busy === "agree"} onClick={async () => {const reason=await soodalPrompt('다른 계약서이거나 실제 계약과 다른 내용을 입력해 주세요.');if(reason?.trim())void act("agree",()=>interiorApi.reviewContract(id,item.contract!.id,false,reason.trim()))}}>계약자료 재등록 요청</button></div>}
            </article>
            <div className="contractVersions">
              {item.contract.versions.map((version) => (
                <details key={version.versionNo}>
                  <summary>
                    계약 {version.versionNo}차 ·{" "}
                    {money(version.amount, version.currencyCode)}
                  </summary>
                  <pre>{version.scope}</pre>
                  <pre>{version.schedule}</pre>
                  <small>생성 {date(version.createdAt)}</small>
                </details>
              ))}
            </div>
            <h3>지급계획</h3>
            <p className="interiorHint">
              실제 대금은 고객과 전문가 사이에서 직접 이동하며 아래는 확인
              이력입니다.
            </p>
            {item.contract.paymentPlans.map((plan) => (
              <article className="paymentPlan" key={plan.id}>
                <div>
                  <b>
                    {plan.sequenceNo}. {plan.name}
                  </b>
                  <strong>
                    {money(plan.amount, item.contract!.currencyCode)}
                  </strong>
                  <small>
                    예정일 {plan.dueDate ?? "미정"} · {statusLabel(plan.statusCode)}
                  </small>
                </div>
                {plan.confirmations.map((value) => (
                  <div key={value.id}>
                    <p>
                      확인 {money(value.amount, item.contract!.currencyCode)} ·{" "}
                      {date(value.confirmedAt)} · {value.note}
                    </p>
                    {value.evidence && <Files items={[value.evidence]} />}
                  </div>
                ))}
                <button
                  disabled={busy === plan.id}
                  onClick={async () => {
                    const note =
                      await soodalPrompt("직접 지급 확인 메모를 입력하세요.") ?? "";
                    if (
                      note ||
                      await soodalConfirm("메모 없이 직접 지급 사실을 확인할까요?")
                    )
                      void act(plan.id, () =>
                        interiorApi.confirmPayment(
                          id,
                          plan.id,
                          plan.amount,
                          note,
                        ),
                      );
                  }}
                >
                  직접 지급 사실 확인
                </button>
              </article>
            ))}
          </>
        ) : (
          <p className="interiorEmpty">
            계약이 아직 준비되지 않았습니다. 최종 시공 전문가 선택과 계약 작성은
            현재 운영 절차에서 처리됩니다.
          </p>
        )}
      </Section>
      <Section id="stages" title="공정·단계검사">
        {item.workStages.map((stage) => (
          <article className="interiorStage" key={stage.id}>
            <header>
              <div>
                <span>
                  {stage.sequenceNo}단계 · {statusLabel(stage.statusCode)}
                </span>
                <h3>{stage.name}</h3>
              </div>
              <strong>{stage.progressPercent}%</strong>
            </header>
            <progress max="100" value={stage.progressPercent} />
            <small>
              예정 {stage.plannedStartDate} ~ {stage.plannedEndDate} · 실제{" "}
              {date(stage.actualStartAt)} ~ {date(stage.actualEndAt)}
            </small>
            {stage.updates.map((update) => (
              <div className="workUpdate" key={update.id}>
                <b>
                  {update.progressPercent}% · {date(update.createdAt)}
                </b>
                <p>{update.text}</p>
                {update.issue && <p className="issue">이슈: {update.issue}</p>}
                <Files items={update.files} />
              </div>
            ))}
            {stage.inspections.map((check) => (
              <div className="inspection" key={check.id}>
                <b>
                  검사 {statusLabel(check.statusCode)} · {date(check.inspectedAt)}
                </b>
                <p>{check.result}</p>
                {check.correction && <p>보완: {check.correction}</p>}
                <pre>{check.checklist}</pre>
                <Files items={check.files} />
              </div>
            ))}
          </article>
        ))}
      </Section>
      <Section id="changes" title="계약 변경·추가공사">
        {item.changes.map((change) => (
          <article className="interiorRecord" key={change.id}>
            <header>
              <div>
                <span>
                  변경 {change.changeNo} · {statusLabel(change.statusCode)}
                </span>
                <h3>{change.reason}</h3>
              </div>
              <strong>
                {change.amountDelta >= 0 ? "+" : ""}
                {money(change.amountDelta, item.contract?.currencyCode)}
              </strong>
            </header>
            <p>{change.scopeChange}</p>
            <small>일정 영향 {change.scheduleImpactDays ?? 0}일</small>
            <Files items={change.files} />
            {change.canDecide && (
              <div>
                <button
                  disabled={busy === change.id}
                  onClick={() =>
                    void act(change.id, () =>
                      interiorApi.decideChange(id, change.id, true),
                    )
                  }
                >
                  변경 승인
                </button>
                <button
                  className="danger"
                  disabled={busy === change.id}
                  onClick={() =>
                    void act(change.id, () =>
                      interiorApi.decideChange(id, change.id, false),
                    )
                  }
                >
                  거절
                </button>
              </div>
            )}
          </article>
        ))}
      </Section>
      <Section id="completion" title="완료·서비스 이력">
        {item.completion ? (
          <article className="interiorRecord">
            <b>최종 완료일 {item.completion.completedDate}</b>
            <h3>{item.completion.historyTitle ?? "인테리어 공사 완료"}</h3>
            <p>
              {item.completion.historySummary ??
                "완료 Snapshot이 보존되었습니다."}
            </p>
            <small>
              최종 계약 {item.contract?.version ?? "—"}차 · 단계 검사
              결과는 공정 섹션에서 확인
            </small>
            {item.reviewAvailable && item.reviewTransactionId ? (
              <button
                onClick={() =>
                  navigate(`/customer/transactions/${item.reviewTransactionId}`)
                }
              >
                연결 거래 리뷰 확인
              </button>
            ) : (
              <p>
                인테리어 다전문가 리뷰 정책은 미확정이므로 별도 리뷰를 생성하지
                않습니다.
              </p>
            )}
          </article>
        ) : (
          <p className="interiorEmpty">프로젝트가 아직 완료되지 않았습니다.</p>
        )}
      </Section>
      <Section id="aftercare" title="하자/A/S">
        {item.defects.map((value) => (
          <article className="interiorRecord" key={value.id}>
            <b>
              {statusLabel(value.statusCode)} · {date(value.receivedAt)}
            </b>
            <h3>{value.subject}</h3>
            <p>
              {value.location} · {value.description}
            </p>
            <button
              onClick={() =>
                navigate(`/customer/after-services/${value.afterServiceCaseId}`)
              }
            >
              A/S 진행상태 보기
            </button>
          </article>
        ))}
        {item.completion && item.contract && (
          <form className="interiorForm" onSubmit={submitDefect}>
            <h3>하자 접수</h3>
            <input
              required
              placeholder="제목"
              value={defect.subject}
              onChange={(event) =>
                setDefect({ ...defect, subject: event.target.value })
              }
            />
            <input
              required
              placeholder="하자 위치"
              value={defect.location}
              onChange={(event) =>
                setDefect({ ...defect, location: event.target.value })
              }
            />
            <select
              value={defect.workStageId}
              onChange={(event) =>
                setDefect({ ...defect, workStageId: event.target.value })
              }
            >
              <option value="">관련 공정 선택 안 함</option>
              {item.workStages.map((stage) => (
                <option value={stage.id} key={stage.id}>
                  {stage.name}
                </option>
              ))}
            </select>
            <input
              type="number"
              min="1"
              placeholder="관련 계약 차수"
              value={defect.contractVersion}
              onChange={(event) =>
                setDefect({ ...defect, contractVersion: event.target.value })
              }
            />
            <textarea
              required
              placeholder="하자 내용을 입력하세요"
              value={defect.description}
              onChange={(event) =>
                setDefect({ ...defect, description: event.target.value })
              }
            />
            <label className="interiorFilePicker">
              사진/PDF
              <input
                type="file"
                accept=".jpg,.jpeg,.png,.pdf,image/jpeg,image/png,application/pdf"
                multiple
                onChange={(event) =>
                  void upload("defect", event.target.files)
                }
              />
            </label>
            <PendingFiles
              items={defectFiles}
              onRemove={(file) => removePending("defect", file)}
            />
            <small>
              첨부 {defectFiles.length}개 · 외부 검사가 연동되기 전에는 상대방
              공개가 차단됩니다.
            </small>
            <button disabled={busy === "defect"}>하자/A/S 접수</button>
          </form>
        )}
      </Section>
      <Section id="disputes" title="분쟁">
        {item.disputes.map((value) => (
          <article className="interiorRecord" key={value.id}>
            <b>
              {statusLabel(value.statusCode)} · {date(value.receivedAt)}
            </b>
            <h3>{value.subject}</h3>
            <p>{value.description}</p>
            <button onClick={() => navigate(`/customer/disputes/${value.id}`)}>
              분쟁 진행상태 보기
            </button>
          </article>
        ))}
        {item.contract && (
          <form className="interiorForm" onSubmit={submitDispute}>
            <h3>분쟁 접수</h3>
            <p>접수만으로 전문가 귀책 또는 신뢰도 변경으로 처리되지 않습니다.</p>
            <input
              required
              placeholder="제목"
              value={dispute.subject}
              onChange={(event) =>
                setDispute({ ...dispute, subject: event.target.value })
              }
            />
            <select
              value={dispute.workStageId}
              onChange={(event) =>
                setDispute({ ...dispute, workStageId: event.target.value })
              }
            >
              <option value="">관련 공정 선택 안 함</option>
              {item.workStages.map((stage) => (
                <option value={stage.id} key={stage.id}>
                  {stage.name}
                </option>
              ))}
            </select>
            <select
              value={dispute.contractChangeId}
              onChange={(event) =>
                setDispute({ ...dispute, contractChangeId: event.target.value })
              }
            >
              <option value="">관련 변경 선택 안 함</option>
              {item.changes.map((change) => (
                <option value={change.id} key={change.id}>
                  변경 {change.changeNo}
                </option>
              ))}
            </select>
            <textarea
              required
              placeholder="분쟁 내용을 입력하세요"
              value={dispute.description}
              onChange={(event) =>
                setDispute({ ...dispute, description: event.target.value })
              }
            />
            <label className="interiorFilePicker">
              분쟁 사진/PDF
              <input
                type="file"
                accept=".jpg,.jpeg,.png,.pdf,image/jpeg,image/png,application/pdf"
                multiple
                onChange={(event) =>
                  void upload("dispute", event.target.files)
                }
              />
            </label>
            <PendingFiles
              items={disputeFiles}
              onRemove={(file) => removePending("dispute", file)}
            />
            <small>
              하자 첨부와 분쟁 첨부는 서로 섞이지 않으며, 외부 검사 전에는
              상대방에게 공개되지 않습니다.
            </small>
            <button disabled={busy === "dispute"}>분쟁 접수</button>
          </form>
        )}
      </Section>
      <Section id="events" title="프로젝트 이력">
        <ol className="interiorTimeline">
          {item.events.map((value) => (
            <li key={value.id}>
              <b>{value.typeDisplay}</b>
              <time>{date(value.occurredAt)}</time>
            </li>
          ))}
        </ol>
      </Section>
      <div className="interiorMobileCta">
        <button onClick={() => window.scrollTo({ top: 0, behavior: "smooth" })}>
          현재 상태 보기
        </button>
      </div>
    </InteriorLayout>
  );
}
