import { useCallback, useEffect, useMemo, useState } from "react";
import { AdminLayout } from "./AdminLayout";
import { getInteriorProject, getInteriorProjects } from "./interiorApi";
import type {
  InteriorProjectDetail,
  InteriorProjectItem,
} from "./interiorTypes";

const statuses: Record<string, string> = {
  CONSULTATION: "상담",
  SITE_VISIT_SELECTION: "실측 공급자 선택",
  SITE_VISIT_SCHEDULED: "실측 예정",
  SITE_VISIT_COMPLETED: "실측 완료",
  ESTIMATE_IN_PROGRESS: "설계·견적 작성",
  ESTIMATE_READY: "견적 준비 완료",
  CONTRACT_PENDING: "계약 동의 대기",
  CONTRACTED: "계약 완료",
  CONSTRUCTION: "공사 진행",
  INSPECTION: "검수",
  COMPLETED: "완료",
  DEFECT_MANAGEMENT: "하자 관리",
  CANCELLED: "취소",
  PROPOSED: "일정 제안",
  CONFIRMED: "일정 확정",
  PENDING_AGREEMENT: "동의 대기",
  EFFECTIVE: "계약 효력 발생",
  PLANNED: "예정",
  IN_PROGRESS: "진행 중",
  REQUESTED: "승인 대기",
  APPROVED: "승인",
  REJECTED: "반려",
};
const tabs = [
  "요청",
  "실측",
  "견적·설계",
  "계약",
  "지급계획",
  "공정",
  "변경",
  "검수·완료",
  "하자·A/S",
  "분쟁",
  "관리이력",
] as const;
const date = (value: string | null) =>
  value
    ? new Intl.DateTimeFormat("ko-KR", {
        dateStyle: "medium",
        timeStyle: "short",
      }).format(new Date(value))
    : "-";
const money = (value: number | null, currency = "KRW") =>
  value === null
    ? "-"
    : new Intl.NumberFormat("ko-KR", {
        style: "currency",
        currency,
        maximumFractionDigits: 0,
      }).format(value);

export function AdminInteriorPage({
  pathname,
  projectId,
}: {
  pathname: string;
  projectId?: string;
}) {
  const [items, setItems] = useState<InteriorProjectItem[]>([]);
  const [detail, setDetail] = useState<InteriorProjectDetail | null>(null);
  const [tab, setTab] = useState<(typeof tabs)[number]>("요청");
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [error, setError] = useState<string | null>(null);
  const load = useCallback(() => {
    const query = new URLSearchParams();
    if (search.trim()) query.set("search", search.trim());
    if (status) query.set("status", status);
    getInteriorProjects(query)
      .then(setItems)
      .catch((e) =>
        setError(
          e instanceof Error ? e.message : "목록을 불러오지 못했습니다.",
        ),
      );
  }, [search, status]);
  useEffect(() => {
    load();
  }, [load]);
  useEffect(() => {
    if (projectId)
      getInteriorProject(projectId)
        .then(setDetail)
        .catch((e) =>
          setError(
            e instanceof Error ? e.message : "상세정보를 불러오지 못했습니다.",
          ),
        );
    else setDetail(null);
  }, [projectId]);
  const metrics = useMemo(
    () => [
      { label: "전체 프로젝트", value: items.length },
      {
        label: "실측 예정",
        value: items.filter((v) => v.statusCode === "SITE_VISIT_SCHEDULED")
          .length,
      },
      {
        label: "공사 진행",
        value: items.filter((v) => v.statusCode === "CONSTRUCTION").length,
      },
      {
        label: "A/S·분쟁",
        value: items.filter((v) => v.hasAfterService || v.hasDispute).length,
      },
    ],
    [items],
  );
  return (
    <AdminLayout pathname={pathname}>
      <section className="adminPageHeading">
        <div>
          <p>수달 인테리어 운영</p>
          <h1>인테리어 프로젝트 관리</h1>
        </div>
        <span>
          상담부터 실측·계약·공정·변경·검수·하자까지 하나의 프로젝트 이력으로
          관리합니다.
        </span>
      </section>
      {error && <div className="adminError">{error}</div>}
      {!projectId ? (
        <>
          <section className="adminMetricGrid">
            {metrics.map((v) => (
              <article className="adminMetricCard" key={v.label}>
                <span>{v.label}</span>
                <strong className="adminMetricValue">
                  {v.value.toLocaleString("ko-KR")}
                  <small>건</small>
                </strong>
              </article>
            ))}
          </section>
          <section className="customerListCard">
            <header>
              <div>
                <h2>프로젝트 목록</h2>
                <span>
                  개인정보는 상세 업무권한이 있는 관리자에게만 표시됩니다.
                </span>
              </div>
            </header>
            <form
              className="customerFilters"
              onSubmit={(e) => {
                e.preventDefault();
                load();
              }}
            >
              <label className="customerSearch">
                프로젝트 찾기
                <input
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  placeholder="프로젝트번호, 고객, 서비스"
                />
              </label>
              <label>
                현재 단계
                <select
                  value={status}
                  onChange={(e) => setStatus(e.target.value)}
                >
                  <option value="">전체</option>
                  {Object.entries(statuses)
                    .slice(0, 13)
                    .map(([key, label]) => (
                      <option value={key} key={key}>
                        {label}
                      </option>
                    ))}
                </select>
              </label>
              <button>검색</button>
            </form>
            <ProjectTable items={items} />
          </section>
        </>
      ) : (
        detail && <Detail value={detail} tab={tab} setTab={setTab} />
      )}
    </AdminLayout>
  );
}
function ProjectTable({ items }: { items: InteriorProjectItem[] }) {
  return items.length ? (
    <div className="customerTableWrap">
      <table className="customerTable">
        <thead>
          <tr>
            <th>프로젝트</th>
            <th>고객·서비스</th>
            <th>지역</th>
            <th>현재 단계</th>
            <th>담당 공급자</th>
            <th>계약금액</th>
            <th>공사기간</th>
            <th>진행률</th>
            <th>이슈</th>
          </tr>
        </thead>
        <tbody>
          {items.map((v) => (
            <tr
              key={v.id}
              onClick={() => location.assign(`/admin/interior/${v.id}`)}
            >
              <td>
                <strong>{v.projectNumber}</strong>
              </td>
              <td>
                {v.customerName}
                <br />
                <small>{v.serviceName}</small>
              </td>
              <td>{v.areaName}</td>
              <td>
                <span className="customerStatus">
                  {statuses[v.statusCode] ?? v.statusCode}
                </span>
              </td>
              <td>
                실측 {v.siteVisitProvider ?? "-"}
                <br />
                시공 {v.contractor ?? "-"}
              </td>
              <td>{money(v.contractAmount)}</td>
              <td>
                {v.startDate ?? "-"} ~ {v.completionDate ?? "-"}
              </td>
              <td>{v.progressPercent}%</td>
              <td>
                {(v.hasAfterService ? "A/S " : "") +
                  (v.hasDispute ? "분쟁" : "") || "-"}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  ) : (
    <div className="customerEmpty">
      <strong>등록된 인테리어 프로젝트가 없습니다.</strong>
      <p>인테리어 요청을 프로젝트로 전환하면 이곳에 표시됩니다.</p>
    </div>
  );
}
function Detail({
  value,
  tab,
  setTab,
}: {
  value: InteriorProjectDetail;
  tab: (typeof tabs)[number];
  setTab: (v: (typeof tabs)[number]) => void;
}) {
  const contract = value.contracts.at(-1);
  const progress = value.workStages.length
    ? Math.round(
        value.workStages.reduce((a, v) => a + v.progressPercent, 0) /
          value.workStages.length,
      )
    : 0;
  return (
    <>
      <button
        className="adminBackButton"
        onClick={() => location.assign("/admin/interior")}
      >
        ← 프로젝트 목록
      </button>
      <section className="customerDetailHero">
        <div>
          <span className="customerStatus">
            {statuses[value.statusCode] ?? value.statusCode}
          </span>
          <h2>{value.projectNumber}</h2>
          <p>
            {value.customerName} · {value.serviceName} · {value.areaName}
          </p>
        </div>
        <dl>
          <div>
            <dt>실측 공급자</dt>
            <dd>{value.siteVisitProvider ?? "미확정"}</dd>
          </div>
          <div>
            <dt>최종 선택 공급자</dt>
            <dd>{value.contractor ?? "미확정"}</dd>
          </div>
          <div>
            <dt>선택 견적·시각</dt>
            <dd>
              {value.selectedQuoteRevisionNo
                ? `Version ${value.selectedQuoteRevisionNo} · ${date(value.contractorSelectedAt)}`
                : "미선택"}
            </dd>
          </div>
          <div>
            <dt>계약금액</dt>
            <dd>{money(contract?.amount ?? null, contract?.currencyCode)}</dd>
          </div>
          <div>
            <dt>공사기간</dt>
            <dd>
              {contract
                ? `${contract.plannedStartDate} ~ ${contract.plannedCompletionDate}`
                : "미확정"}
            </dd>
          </div>
          <div>
            <dt>진행률</dt>
            <dd>{progress}%</dd>
          </div>
          <div>
            <dt>다음 작업</dt>
            <dd>{nextAction(value.statusCode)}</dd>
          </div>
        </dl>
      </section>
      <nav className="customerDetailTabs">
        {tabs.map((v) => (
          <button
            className={tab === v ? "active" : ""}
            onClick={() => setTab(v)}
            key={v}
          >
            {v}
          </button>
        ))}
      </nav>
      <section className="customerDetailPanel">
        {tab === "요청" && (
          <Info
            title="요청·고객 정보"
            rows={[
              ["고객", value.customerName],
              ["전화번호", value.customerPhone ?? "-"],
              ["서비스 주소", `${value.areaName} ${value.detailAddress ?? ""}`],
              ["서비스", value.serviceName],
              [
                "수수료",
                value.feeAssessmentStatusCode === "POLICY_PENDING"
                  ? "인테리어 수수료 정책 확인 필요"
                  : value.feeAssessmentStatusCode,
              ],
            ]}
          />
        )}{" "}
        {tab === "실측" && (
          <Cards
            empty="등록된 실측 일정이 없습니다."
            values={value.siteVisits.map((v) => ({
              title: `${v.providerName} · ${statuses[v.statusCode] ?? v.statusCode}`,
              body: `${date(v.scheduledStartAt)} ~ ${date(v.scheduledEndAt)}\n${v.measurementSummary ?? "실측 결과 미등록"}`,
            }))}
          />
        )}{" "}
        {tab === "견적·설계" && (
          <Cards
            empty="등록된 설계 차수가 없습니다."
            values={value.designs.map((v) => ({
              title: `설계 ${v.versionNo}차 · ${v.title}`,
              body: `${statuses[v.statusCode] ?? v.statusCode}\n${v.description ?? ""}`,
            }))}
          />
        )}{" "}
        {tab === "계약" && (
          <Cards
            empty="등록된 계약이 없습니다."
            values={value.contracts.map((v) => ({
              title: `계약 ${v.version}차 · ${statuses[v.statusCode] ?? v.statusCode}`,
              body: `${v.providerName} · ${money(v.amount, v.currencyCode)}\n고객동의 ${date(v.customerAgreedAt)} · 공급자동의 ${date(v.providerAgreedAt)}`,
            }))}
          />
        )}{" "}
        {tab === "지급계획" && (
          <Cards
            empty="등록된 지급계획이 없습니다."
            values={value.paymentPlans.map((v) => ({
              title: `${v.sequenceNo}. ${v.name}`,
              body: `${money(v.amount)} · 예정일 ${v.dueDate ?? "조건 충족 시"}\n확인 ${v.confirmationCount}건 — 실제 자금이동 없음`,
            }))}
          />
        )}{" "}
        {tab === "공정" && (
          <Cards
            empty="등록된 공정이 없습니다."
            values={value.workStages.map((v) => ({
              title: `${v.sequenceNo}. ${v.name} · ${v.progressPercent}%`,
              body: `${v.plannedStartDate} ~ ${v.plannedEndDate}\n진행기록 ${v.updateCount}건 · 검수 ${v.inspectionCount}건`,
            }))}
          />
        )}{" "}
        {tab === "변경" && (
          <Cards
            empty="등록된 변경·추가공사가 없습니다."
            values={value.changes.map((v) => ({
              title: `변경 ${v.changeNo}차 · ${statuses[v.statusCode] ?? v.statusCode}`,
              body: `${v.reason}\n${v.scopeChange}\n금액 증감 ${money(v.amountDelta)} · 일정 영향 ${v.scheduleImpactDays ?? 0}일`,
            }))}
          />
        )}{" "}
        {tab === "검수·완료" && (
          <Info
            title="검수·완료 현황"
            rows={[
              ["현재 단계", statuses[value.statusCode] ?? value.statusCode],
              ["완료일", value.actualCompletionDate ?? "-"],
              ["공정 진행률", `${progress}%`],
              ["완료 후 이력", "ServiceHistory 연결"],
            ]}
          />
        )}{" "}
        {tab === "하자·A/S" && (
          <section className="customerNotice">
            <h3>A/S 운영 화면 연결</h3>
            <p>
              인테리어 하자는 위치·공정·계약 차수를 보조정보로 보존하며,
              접수 이후의 담당자 배정·일정·조치·증빙·완료 처리는 공통 A/S
              운영 화면에서 관리합니다.
            </p>
            <button
              type="button"
              className="adminBackButton"
              onClick={() => location.assign("/admin/disputes")}
            >
              A/S 운영 화면 열기
            </button>
          </section>
        )}{" "}
        {tab === "분쟁" && (
          <section className="customerNotice">
            <h3>분쟁 운영 화면 연결</h3>
            <p>
              프로젝트·계약 변경·공정 근거를 공통 분쟁 및 귀책판정 엔진과
              연결합니다. 프로젝트 화면에서는 사실관계를 조회하고, 판단과
              상태 변경은 권한이 분리된 공통 운영 화면에서 처리합니다.
            </p>
            <button
              type="button"
              className="adminBackButton"
              onClick={() => location.assign("/admin/disputes")}
            >
              A/S·분쟁 운영 화면 열기
            </button>
          </section>
        )}{" "}
        {tab === "관리이력" && (
          <Cards
            empty="관리이력이 없습니다."
            values={value.events.map((v) => ({
              title: v.eventTypeCode,
              body: `${date(v.occurredAt)}\n${v.data ?? ""}`,
            }))}
          />
        )}
      </section>
    </>
  );
}
function nextAction(status: string) {
  return (
    (
      {
        CONSULTATION: "실측 후보 등록",
        SITE_VISIT_SELECTION: "실측 공급자 확정",
        SITE_VISIT_SCHEDULED: "실측 수행",
        SITE_VISIT_COMPLETED: "설계·견적 등록",
        ESTIMATE_IN_PROGRESS: "견적 확정",
        CONTRACT_PENDING: "양 당사자 계약 동의",
        CONTRACTED: "공정 시작",
        CONSTRUCTION: "공정기록·검수",
        INSPECTION: "최종 완료 확인",
      } as Record<string, string>
    )[status] ?? "이력 확인"
  );
}
function Info({ title, rows }: { title: string; rows: string[][] }) {
  return (
    <section>
      <h3>{title}</h3>
      <dl className="customerInfoGrid">
        {rows.map((v) => (
          <div key={v[0]}>
            <dt>{v[0]}</dt>
            <dd>{v[1]}</dd>
          </div>
        ))}
      </dl>
    </section>
  );
}
function Cards({
  values,
  empty,
}: {
  values: { title: string; body: string }[];
  empty: string;
}) {
  return values.length ? (
    <div className="caseCardList">
      {values.map((v, i) => (
        <article className="caseRow" key={i}>
          <strong>{v.title}</strong>
          <p style={{ whiteSpace: "pre-line" }}>{v.body}</p>
        </article>
      ))}
    </div>
  ) : (
    <div className="customerEmpty">{empty}</div>
  );
}
