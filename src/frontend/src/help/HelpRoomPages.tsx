import { useEffect, useMemo, useState, type FormEvent, type ReactNode } from "react";
import { useAuthentication } from "../auth/AuthenticationContext";
import { useCallback } from "react";
import { navigate } from "../auth/routing";
import { CustomerAppLayout } from "../customer/CustomerAppLayout";
import { ProviderAppLayout } from "../providers/ProviderAppLayout";
import { publicCatalogApi } from "../customer/api";
import { customerAccountApi } from "../customer/accountApi";
import type { PublicArea, PublicCategory, PublicServiceSummary } from "../customer/types";
import { helpRequest as request, type Suggestion } from "./helpApi";
import { AuthenticatedFilePreview } from "../components/AuthenticatedFilePreview";
import "./helpRoom.css";
import { soodalConfirm } from '../components/soodalDialog'

type ProviderAction = "ANSWERABLE" | "FOLLOW_UP_AVAILABLE" | "WAITING_CUSTOMER";
type HelpList = {
  id: string;
  title: string;
  categoryId: string;
  categoryName: string;
  regionName: string | null;
  status: string;
  intent: string;
  createdAt: string;
  answerCount: number;
  authorAlias: string;
  providerAction: ProviderAction | null;
};
type HelpEntry = {
  id: string;
  authorRole: string;
  authorDisplay: string;
  body: string;
  cause: string | null;
  check: string | null;
  diySteps: string | null;
  risk: string | null;
  nextStep: string | null;
  requiresProfessional: boolean;
  safety: string;
  createdAt: string;
};
type HelpFile = {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  downloadUrl: string;
  publiclySafe: boolean;
};
type HelpDetail = {
  id: string;
  title: string;
  body: string;
  categoryId: string;
  categoryName: string;
  regionName: string | null;
  regionDisclosure: string;
  purpose: string;
  status: string;
  intent: string;
  intentReason: string | null;
  converted: boolean;
  isOwner: boolean;
  authorAlias: string;
  createdAt: string;
  entries: HelpEntry[];
  files: HelpFile[];
  resolution: { code: string; summary: string; createdAt: string } | null;
  providerAction: ProviderAction | null;
};
const fmt = (v: string) =>
  new Intl.DateTimeFormat("ko-KR", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(v));
const isProviderPortal = (roles?: readonly string[]) =>
  window.location.hostname.toLowerCase().startsWith("partner.") ||
  Boolean(roles?.includes("PROVIDER") && !roles.includes("CUSTOMER"));
function Layout({ children }: { children: ReactNode }) {
  const { user } = useAuthentication();
  return isProviderPortal(user?.roles) ? (
    <ProviderAppLayout>{children}</ProviderAppLayout>
  ) : (
    <CustomerAppLayout>{children}</CustomerAppLayout>
  );
}

export function HelpRoomPage({ id }: { id?: string }) {
  const { user } = useAuthentication();
  const providerPortal = isProviderPortal(user?.roles),
    provider = providerPortal && Boolean(user?.roles.includes("PROVIDER"));
  const [items, setItems] = useState<HelpList[]>([]),
    [detail, setDetail] = useState<HelpDetail | null>(null),
    [error, setError] = useState("");
  const load = useCallback(() =>
    id
      ? request<HelpDetail>(`/api/v1/help-room/${id}`).then(setDetail)
      : request<HelpList[]>(
          providerPortal
            ? "/api/v1/help-room?mode=provider"
            : "/api/v1/help-room",
        ).then(setItems), [id, providerPortal]);
  useEffect(() => {
    void load().catch((e) => setError(e.message));
  }, [load]);
  const action = (value: ProviderAction | null) =>
    value === "ANSWERABLE"
      ? "첫 조언 작성"
      : value === "FOLLOW_UP_AVAILABLE"
        ? "추가 질문 답변"
        : "고객 추가 질문 대기";
  return (
    <Layout>
      <section
        className={`helpHero soodalHeroHeading ${providerPortal ? "providerHelpHero" : ""}`}
      >
        <p>{providerPortal ? "전문가 도움방" : "수달 도움방"}</p>
        <h1>{providerPortal ? "전문가 도움방 답변" : "수달 도움방"}</h1>
        <span>
          {providerPortal
            ? "승인받은 서비스 분야의 고객 질문을 확인하고 단계별 조언을 등록해 주세요."
            : "견적요청이 아닌 생활서비스 질문을 올리고, 승인 전문가에게 단계별 조언을 받아보세요."}
        </span>
        {!providerPortal && !id && user?.roles.includes("CUSTOMER") && (
          <button onClick={() => navigate("/help-room/new")}>
            도움 요청 글쓰기
          </button>
        )}
      </section>
      {error && <div className="helpError">{error}</div>}
      {id && detail ? (
        <HelpDetailView value={detail} provider={provider} reload={load} />
      ) : (
        <section
          className={`helpGrid ${providerPortal ? "providerHelpGrid" : ""}`}
        >
          {items.map((x) => (
            <button
              className={x.providerAction?.toLowerCase() ?? ""}
              key={x.id}
              onClick={() => navigate(`/help-room/${x.id}`)}
            >
              <span>
                {x.categoryName}
                {x.regionName ? ` · ${x.regionName}` : ""}
              </span>
              <h2>{x.title}</h2>
              <p>
                {x.authorAlias} · 전문가 답변 {x.answerCount}개
              </p>
              {providerPortal && x.providerAction && (
                <b className="helpProviderAction">{action(x.providerAction)}</b>
              )}
              <small>
                {fmt(x.createdAt)} ·{" "}
                {providerPortal
                  ? "승인 서비스 분야"
                  : x.status === "RESOLVED"
                    ? "해결 완료"
                    : x.status === "CONVERTED"
                      ? "견적요청 전환"
                      : x.intent === "DANGEROUS"
                        ? "안전 주의"
                        : x.intent === "QUOTE_RECOMMENDED"
                          ? "견적 전환 안내"
                          : "조언 진행"}
              </small>
            </button>
          ))}
        </section>
      )}
      {!id && !items.length && !error && (
        <p className="helpEmpty">
          {providerPortal
            ? "현재 답변 가능한 고객 질문이 없습니다. 승인받은 서비스 분야의 새 질문이 등록되면 여기에 표시됩니다."
            : "아직 등록된 도움방 글이 없습니다."}
        </p>
      )}
    </Layout>
  );
}

function HelpDetailView({
  value,
  provider,
  reload,
}: {
  value: HelpDetail;
  provider: boolean;
  reload: () => Promise<void>;
}) {
  const [follow, setFollow] = useState(""),
    [advice, setAdvice] = useState({
      body: "",
      cause: "",
      check: "",
      diySteps: "",
      risk: "",
      nextStep: "",
      requiresProfessional: false,
    }),
    [summary, setSummary] = useState(""),
    [error, setError] = useState(""),
    [busy, setBusy] = useState(false);
  const run = async (action: () => Promise<unknown>) => {
    setBusy(true);
    setError("");
    try {
      await action();
      await reload();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  };
  return (
    <article className="helpDetail">
      <button className="helpBack" onClick={() => navigate("/help-room")}>
        ← 도움방 목록
      </button>
      <header>
        <span>
          {value.categoryName}
          {value.regionName ? ` · ${value.regionName}` : " · 지역 비공개"}
        </span>
        <h1>{value.title}</h1>
        <p>
          {value.authorAlias} · {fmt(value.createdAt)}
        </p>
      </header>
      {value.intentReason && (
        <div className={`helpIntent ${value.intent.toLowerCase()}`}>
          <strong>
            {value.intent === "DANGEROUS"
              ? "위험 작업 안내"
              : "견적요청 전환 안내"}
          </strong>
          <p>{value.intentReason}</p>
          {value.isOwner &&
            value.status === "PUBLISHED" &&
            value.intent === "QUOTE_RECOMMENDED" && (
              <button
                onClick={() =>
                  void run(async () => {
                    if (!await soodalConfirm("이 글을 견적요청 임시저장으로 전환할까요?"))
                      return;
                    const x = await request<{ continuePath: string }>(
                      `/api/v1/help-room/${value.id}/convert-to-request`,
                      { method: "POST" },
                    );
                    navigate(x.continuePath);
                  })
                }
              >
                확인 후 견적요청으로 전환
              </button>
            )}
        </div>
      )}
      <section className="helpBody">
        <p>{value.body}</p>
        {value.files.length > 0 && (
          <div className="helpPhotos helpPhotoGrid">
            {value.files.map((file) =>
              file.publiclySafe || value.isOwner ? (
                <div key={file.id} className="helpPhotoItem">
                  <AuthenticatedFilePreview file={file} statusText="위치정보 제거 및 안전처리 완료" />
                  {!value.isOwner && (
                    <button
                      type="button"
                      onClick={() =>
                        void run(() =>
                          request(
                            `/api/v1/help-room/${value.id}/files/${file.id}/report`,
                            {
                              method: "POST",
                              body: JSON.stringify({
                                reason: "부적절하거나 개인정보가 포함된 사진",
                              }),
                            },
                          ),
                        )
                      }
                    >
                      사진 신고
                    </button>
                  )}
                </div>
              ) : (
                <span key={file.id}>
                  사진 안전 확인 중 · 다른 회원에게는 아직 비공개
                </span>
              ),
            )}
          </div>
        )}
      </section>
      <section className="helpEntries">
        <h2>단계별 질문과 조언</h2>
        {value.entries.map((entry) => (
          <article
            key={entry.id}
            className={
              entry.authorRole === "PROVIDER"
                ? "providerAdvice"
                : "customerFollow"
            }
          >
            <header>
              <strong>{entry.authorDisplay}</strong>
              <span>{fmt(entry.createdAt)}</span>
            </header>
            <p>{entry.body}</p>
            {entry.cause && (
              <dl>
                <dt>가능한 원인</dt>
                <dd>{entry.cause}</dd>
              </dl>
            )}
            {entry.check && (
              <dl>
                <dt>먼저 확인</dt>
                <dd>{entry.check}</dd>
              </dl>
            )}
            {entry.diySteps && (
              <dl>
                <dt>안전한 자가조치</dt>
                <dd>{entry.diySteps}</dd>
              </dl>
            )}
            {entry.risk && (
              <dl>
                <dt>주의사항</dt>
                <dd>{entry.risk}</dd>
              </dl>
            )}
            {entry.nextStep && (
              <dl>
                <dt>다음 단계</dt>
                <dd>{entry.nextStep}</dd>
              </dl>
            )}
            {entry.requiresProfessional && (
              <b className="professionalNeeded">전문가 점검 권장</b>
            )}
          </article>
        ))}
        {value.resolution && (
          <article className="helpResolution">
            <strong>도움방 해결 후기</strong>
            <p>{value.resolution.summary}</p>
            <small>거래 후기·전문가 평점에는 반영되지 않습니다.</small>
          </article>
        )}
      </section>
      {error && <div className="helpError">{error}</div>}
      {value.status === "PUBLISHED" && value.isOwner && (
        <>
          <form
            className="helpForm"
            onSubmit={(e) => {
              e.preventDefault();
              void run(async () => {
                await request(`/api/v1/help-room/${value.id}/follow-ups`, {
                  method: "POST",
                  body: JSON.stringify({ body: follow }),
                });
                setFollow("");
              });
            }}
          >
            <h2>추가 질문</h2>
            <textarea
              required
              minLength={2}
              maxLength={5000}
              value={follow}
              onChange={(e) => setFollow(e.target.value)}
            />
            <button disabled={busy}>질문 추가</button>
          </form>
          <form
            className="helpForm"
            onSubmit={(e) => {
              e.preventDefault();
              void run(() =>
                request(`/api/v1/help-room/${value.id}/resolve`, {
                  method: "POST",
                  body: JSON.stringify({
                    resolutionCode: "SELF_RESOLVED",
                    summary,
                    helpfulEntryId: null,
                  }),
                }),
              );
            }}
          >
            <h2>해결 후기 남기기</h2>
            <textarea
              required
              minLength={2}
              maxLength={3000}
              value={summary}
              onChange={(e) => setSummary(e.target.value)}
              placeholder="어떤 방법으로 해결했는지 알려주세요."
            />
            <button disabled={busy}>해결 완료</button>
          </form>
        </>
      )}
      {value.status === "PUBLISHED" && provider && !value.isOwner && (
        <form
          className="helpForm"
          onSubmit={(e) => {
            e.preventDefault();
            void run(async () => {
              await request(`/api/v1/help-room/${value.id}/advice`, {
                method: "POST",
                body: JSON.stringify(advice),
              });
              setAdvice({
                body: "",
                cause: "",
                check: "",
                diySteps: "",
                risk: "",
                nextStep: "",
                requiresProfessional: false,
              });
            });
          }}
        >
          <h2>승인 전문가 조언</h2>
          <textarea
            required
            value={advice.body}
            onChange={(e) => setAdvice({ ...advice, body: e.target.value })}
            placeholder="핵심 답변"
          />
          <input
            value={advice.cause}
            onChange={(e) => setAdvice({ ...advice, cause: e.target.value })}
            placeholder="가능한 원인"
          />
          <input
            value={advice.check}
            onChange={(e) => setAdvice({ ...advice, check: e.target.value })}
            placeholder="먼저 확인할 내용"
          />
          {value.intent !== "DANGEROUS" && (
            <textarea
              value={advice.diySteps}
              onChange={(e) =>
                setAdvice({ ...advice, diySteps: e.target.value })
              }
              placeholder="안전한 범위의 자가조치"
            />
          )}
          <input
            value={advice.risk}
            onChange={(e) => setAdvice({ ...advice, risk: e.target.value })}
            placeholder="주의사항"
          />
          <input
            value={advice.nextStep}
            onChange={(e) => setAdvice({ ...advice, nextStep: e.target.value })}
            placeholder="다음 단계"
          />
          <label>
            <input
              type="checkbox"
              checked={advice.requiresProfessional}
              onChange={(e) =>
                setAdvice({ ...advice, requiresProfessional: e.target.checked })
              }
            />{" "}
            전문가 점검이 필요합니다
          </label>
          <button disabled={busy}>조언 등록</button>
        </form>
      )}
    </article>
  );
}

export function NewHelpPostPage() {
  const [majors, setMajors] = useState<PublicCategory[]>([]),
    [middles, setMiddles] = useState<PublicCategory[]>([]),
    [services, setServices] = useState<PublicServiceSummary[]>([]),
    [majorId, setMajorId] = useState(""),
    [middleId, setMiddleId] = useState(""),
    [sidos, setSidos] = useState<PublicArea[]>([]),
    [areas, setAreas] = useState<PublicArea[]>([]),
    [sido, setSido] = useState(""),
    [files, setFiles] = useState<File[]>([]),
    [form, setForm] = useState({
      categoryId: "",
      administrativeAreaId: "",
      revealRegion: false,
      purposeCode: "DIAGNOSIS",
      title: "",
      body: "",
    }),
    [error, setError] = useState(""),
    [busy, setBusy] = useState(false);
  const chooseFiles = (selected: File[]) => {
    const supported = selected.filter((file) => ["image/jpeg", "image/png"].includes(file.type) && file.size <= 5 * 1024 * 1024).slice(0, 8);
    setFiles(supported);
    if (supported.length !== selected.slice(0, 8).length) setError("JPG·PNG 사진만 장당 5MB 이하로 등록할 수 있습니다.");
    else setError("");
  };
  useEffect(() => {
    publicCatalogApi
      .majors()
      .then(setMajors)
      .catch((e) => setError(e.message));
    customerAccountApi
      .sidos()
      .then(setSidos)
      .catch(() => {});
  }, []);
  useEffect(() => {
    setMiddleId("");
    setMiddles([]);
    setServices([]);
    setForm((current) => ({ ...current, categoryId: "" }));
    if (!majorId) return;
    publicCatalogApi.children(majorId).then(setMiddles).catch((e) => setError(e.message));
  }, [majorId]);
  useEffect(() => {
    setServices([]);
    setForm((current) => ({ ...current, categoryId: "" }));
    if (!middleId) return;
    publicCatalogApi.servicesByMiddle(middleId).then(setServices).catch((e) => setError(e.message));
  }, [middleId]);
  useEffect(() => {
    if (!sido) {
      setAreas([]);
      return;
    }
    customerAccountApi
      .sigungu(sido)
      .then(setAreas)
      .catch(() => setAreas([]));
  }, [sido]);
  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError("");
    try {
      const post = await request<HelpDetail>("/api/v1/help-room", {
        method: "POST",
        body: JSON.stringify({
          ...form,
          administrativeAreaId: form.revealRegion
            ? form.administrativeAreaId || null
            : null,
        }),
      });
      for (const file of files) {
        const body = new FormData();
        body.append("file", file);
        await request(`/api/v1/help-room/${post.id}/files`, {
          method: "POST",
          body,
        });
      }
      navigate(`/help-room/${post.id}`);
    } catch (reason) {
      setError((reason as Error).message);
      setBusy(false);
    }
  };
  return (
    <CustomerAppLayout>
      <section className="helpHero">
        <p>상담 요청</p>
        <h1>수달 도움방 글쓰기</h1>
        <span>
          견적 요청이 아니라, 무엇을 확인하고 어떻게 진행할지 조언을 받는
          공간입니다.
        </span>
      </section>
      <form className="helpForm helpNew" onSubmit={(e) => void submit(e)}>
        <div className="helpCategoryGrid">
        <label>
          대분류
          <select
            required
            value={majorId}
            onChange={(e) => setMajorId(e.target.value)}
          >
            <option value="">선택</option>
            {majors.map((x) => (
              <option value={x.id} key={x.id}>
                {x.name}
              </option>
            ))}
          </select>
        </label>
        <label>
          중분류
          <select required disabled={!majorId} value={middleId} onChange={(e) => setMiddleId(e.target.value)}>
            <option value="">선택</option>
            {middles.map((x) => <option value={x.id} key={x.id}>{x.name}</option>)}
          </select>
        </label>
        <label>
          하위 서비스
          <select required disabled={!middleId} value={form.categoryId} onChange={(e) => setForm({ ...form, categoryId: e.target.value })}>
            <option value="">선택</option>
            {services.map((x) => <option value={x.id} key={x.id}>{x.name}</option>)}
          </select>
        </label>
        </div>
        <label>
          도움 유형
          <select
            value={form.purposeCode}
            onChange={(e) => setForm({ ...form, purposeCode: e.target.value })}
          >
            <option value="DIAGNOSIS">원인 진단</option>
            <option value="DIY">자가수리 방법</option>
            <option value="MATERIALS">자재·준비물</option>
            <option value="PROFESSIONAL_DECISION">전문가 필요 여부</option>
            <option value="INTERIOR_IDEA">인테리어 아이디어</option>
          </select>
        </label>
        <label>
          제목
          <input
            required
            minLength={5}
            maxLength={200}
            value={form.title}
            onChange={(e) => setForm({ ...form, title: e.target.value })}
          />
        </label>
        <label>
          상황 설명
          <textarea
            required
            minLength={10}
            maxLength={5000}
            value={form.body}
            onChange={(e) => setForm({ ...form, body: e.target.value })}
            placeholder="현재 상태, 이미 확인한 내용, 궁금한 점을 적어주세요."
          />
        </label>
        <label className="helpCheck">
          <input
            type="checkbox"
            checked={form.revealRegion}
            onChange={(e) =>
              setForm({
                ...form,
                revealRegion: e.target.checked,
                administrativeAreaId: "",
              })
            }
          />{" "}
          시·군·구를 다른 회원에게 공개
        </label>
        {form.revealRegion && (
          <div className="helpRegion">
            <select
              required
              value={sido}
              onChange={(e) => {
                setSido(e.target.value);
                setForm({ ...form, administrativeAreaId: "" });
              }}
            >
              <option value="">시·도 선택</option>
              {sidos.map((x) => (
                <option value={x.id} key={x.id}>
                  {x.name}
                </option>
              ))}
            </select>
            <select
              required
              value={form.administrativeAreaId}
              onChange={(e) =>
                setForm({ ...form, administrativeAreaId: e.target.value })
              }
            >
              <option value="">시·군·구 선택</option>
              {areas.map((x) => (
                <option value={x.id} key={x.id}>
                  {x.name}
                </option>
              ))}
            </select>
          </div>
        )}
        <label>
          사진(선택, 최대 8장·장당 5MB)
          <input
            type="file"
            accept="image/jpeg,image/png"
            multiple
            onChange={(e) => chooseFiles(Array.from(e.target.files ?? []))}
          />
        </label>
        {files.length > 0 && <SelectedHelpPhotos files={files} remove={(index) => setFiles(current => current.filter((_, candidate) => candidate !== index))} />}
        <p className="helpPrivacy">
          상세주소는 공개하지 않습니다. 사진은 업로드 즉시 위치정보와
          메타데이터를 제거하며, 원본 파일명 대신 임의 이름을 사용해 안전 처리된
          사본만 비공개 저장합니다.
        </p>
        {error && <div className="helpError">{error}</div>}
        <button disabled={busy}>{busy ? "등록 중…" : "도움 요청 등록"}</button>
      </form>
    </CustomerAppLayout>
  );
}

function SelectedHelpPhotos({ files, remove }: { files: File[]; remove: (index: number) => void }) {
  const [urls, setUrls] = useState<string[]>([]);
  useEffect(() => {
    const next = files.map(file => URL.createObjectURL(file));
    setUrls(next);
    return () => next.forEach(url => URL.revokeObjectURL(url));
  }, [files]);
  return <section className="helpSelectedPhotos" aria-label="선택한 사진 미리보기">
    <header><strong>선택한 사진 {files.length}장</strong><span>등록 전 사진을 확인하고 필요 없는 사진은 삭제하세요.</span></header>
    <div>{files.map((file, index) => <article key={`${file.name}-${file.lastModified}-${index}`}>
      {urls[index] ? <img src={urls[index]} alt={`${index + 1}번째 선택 사진 미리보기`} /> : <span>미리보기 준비 중…</span>}
      <small>{file.name} · {(file.size / 1024 / 1024).toFixed(1)}MB</small>
      <button type="button" onClick={() => remove(index)}>삭제</button>
    </article>)}</div>
  </section>;
}

export function SuggestionBoxPage() {
  const initialQuery = useMemo(() => new URLSearchParams(window.location.search), []);
  const initialType = initialQuery.get("type") ?? "BUG";
  const [items, setItems] = useState<Suggestion[]>([]),
    [form, setForm] = useState({ typeCode: ["BUG", "MISSING", "UX", "FEATURE", "SECURITY", "OTHER"].includes(initialType) ? initialType : "BUG", title: initialQuery.get("title") ?? "", body: initialQuery.get("body") ?? "" }),
    [error, setError] = useState(""),
    [notice, setNotice] = useState("");
  const load = () =>
    request<Suggestion[]>("/api/v1/suggestions").then(setItems);
  useEffect(() => {
    void load().catch((e) => setError(e.message));
  }, []);
  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setError("");
    try {
      await request("/api/v1/suggestions", {
        method: "POST",
        body: JSON.stringify({
          ...form,
          pageUrl: window.location.href,
          deviceInfo: navigator.userAgent,
          appVersion: null,
          idempotencyKey: crypto.randomUUID(),
        }),
      });
      setForm({ typeCode: "BUG", title: "", body: "" });
      setNotice("제안을 비공개로 접수했습니다.");
      await load();
    } catch (reason) {
      setError((reason as Error).message);
    }
  };
  return (
    <Layout>
      <section className="helpHero soodalHeroHeading">
        <p>수달 제안</p>
        <h1>수달 제안함</h1>
        <span>오류·누락 기능·개선 의견을 본사에 비공개로 알려주세요.</span>
      </section>
      <form className="helpForm" onSubmit={(e) => void submit(e)}>
        <p className="helpPrivacy">
          작성 내용은 작성자와 본사 관리자만 볼 수 있습니다. 계정당 하루 5건,
          1분 간격이며 같은 내용은 30일 안에 다시 등록할 수 없습니다.
        </p>
        <label>
          유형
          <select
            value={form.typeCode}
            onChange={(e) => setForm({ ...form, typeCode: e.target.value })}
          >
            <option value="BUG">오류</option>
            <option value="MISSING">누락 기능</option>
            <option value="UX">사용 불편</option>
            <option value="FEATURE">개선 제안</option>
            <option value="SECURITY">보안·개인정보</option>
            <option value="OTHER">기타</option>
          </select>
        </label>
        <label>
          제목
          <input
            required
            minLength={5}
            maxLength={200}
            value={form.title}
            onChange={(e) => setForm({ ...form, title: e.target.value })}
          />
        </label>
        <label>
          내용
          <textarea
            required
            minLength={10}
            maxLength={5000}
            value={form.body}
            onChange={(e) => setForm({ ...form, body: e.target.value })}
          />
        </label>
        {error && <div className="helpError">{error}</div>}
        {notice && <div className="helpNotice">{notice}</div>}
        <button>비공개 접수</button>
      </form>
      <section className="suggestionList">
        <h2>내 제안</h2>
        {items.map((x) => (
          <article key={x.id}>
            <header>
              <span>{x.typeCode}</span>
              <b>{x.statusCode}</b>
            </header>
            <h3>{x.title}</h3>
            <p>{x.body}</p>
            {x.adminReply && (
              <blockquote>
                <strong>본사 답변</strong>
                <p>{x.adminReply}</p>
                {x.releaseVersion && (
                  <small>반영 버전 {x.releaseVersion}</small>
                )}
              </blockquote>
            )}
          </article>
        ))}
      </section>
    </Layout>
  );
}
