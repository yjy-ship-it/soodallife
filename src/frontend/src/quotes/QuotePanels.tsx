import { useCallback, useEffect, useRef, useState } from "react";
import * as quoteApi from "./api";
import type {
  CustomerProviderProfile,
  PublicProviderReviewList,
  CustomerQuoteComparison,
  CustomerQuoteDetail,
  QuoteDetail,
  QuoteItemInput,
  ProviderQuoteTemplate,
  QuoteSubmissionReadiness,
  SaveQuoteRevisionInput,
} from "./types";
import * as requestApi from "../requests/api";
import { EmergencyResponsesPanel } from "../emergency/EmergencyPages";
import { CustomerSiteVisitPanel } from "../siteVisits/SiteVisitPanels";
import { customerAccountApi } from "../customer/accountApi";
import { providerBlockReasons } from "../relationshipBlocks/reasons";
import { loadDefaultAddress } from "../customer/defaultAddress";
import { apiUrl } from "../config/apiEndpoint";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { navigate } from "../auth/routing";
import { soodalConfirm, soodalPrompt } from "../components/soodalDialog";
import { quoteSummaryText, quoteTermsText } from "./displayText";
import "../providers/providerEnhancements.css";

const emptyItem = (): QuoteItemInput => ({
  itemName: "",
  description: null,
  quantity: 1,
  unitText: "식",
  unitPriceAmount: 0,
  itemCategoryCode: "UNCLASSIFIED",
});
const unitOptions=['식','개','회','시간','일','개월','㎡','평','m','세트'] as const;
const defaultDurationUnit=(categoryPath:string)=>/정기|관리|구독|돌봄/.test(categoryPath)?'개월':/인테리어|공사|도배|장판|철거|시공/.test(categoryPath)?'일':'시간';
const parseDuration=(value:string|null, fallback:string)=>{const match=value?.match(/^\s*(\d+(?:\.\d+)?)\s*(시간|일|개월)\s*$/);return {amount:match?.[1]??'',unit:match?.[2]??fallback}}
const digitsOnly=(value:string)=>Number(value.replace(/[^0-9]/g,''));

export function ProviderQuotePanel({
  requestId,
  requestExpiresAt,
  categoryPath,
}: {
  requestId: string;
  requestExpiresAt: string;
  categoryPath: string;
}) {
  const [quote, setQuote] = useState<QuoteDetail | null>(null);
  const [readiness, setReadiness] = useState<QuoteSubmissionReadiness | null>(null);
  const [templates, setTemplates] = useState<ProviderQuoteTemplate[]>([]);
  const [selectedTemplateId, setSelectedTemplateId] = useState("");
  const [summary, setSummary] = useState(""),
    [terms, setTerms] = useState(""),
    [vatMode, setVatMode] = useState<'INCLUDED'|'EXCLUDED'>('EXCLUDED');
  const [durationAmount, setDurationAmount] = useState(""),
    [durationUnit, setDurationUnit] = useState(() => defaultDurationUnit(categoryPath)),
    [availableStartAt, setAvailableStartAt] = useState("");
  const [validUntil, setValidUntil] = useState(() =>
    toLocalInput(requestExpiresAt),
  );
  const [revisionReason, setRevisionReason] = useState(""),
    [items, setItems] = useState<QuoteItemInput[]>([emptyItem()]);
  const [error, setError] = useState(""),
    [message, setMessage] = useState(""),
    [templateMessage, setTemplateMessage] = useState(""),
    [saving, setSaving] = useState(false),
    [quoteClosed, setQuoteClosed] = useState(false);
  const idempotencyKey = useRef(crypto.randomUUID());

  useEffect(() => {
    Promise.all([quoteApi.getProviderQuote(requestId), quoteApi.getQuoteSubmissionReadiness(requestId), quoteApi.getQuoteTemplates()])
      .then(([current, currentReadiness, currentTemplates]) => {
        setQuote(current);
        setReadiness(currentReadiness);
        setTemplates(currentTemplates);
        if (current) loadRevision(current);
      })
      .catch((reason: Error) => setError(reason.message));
  }, [requestId]);

  useEffect(() => {
    const realtime = new HubConnectionBuilder()
      .withUrl(apiUrl("/hubs/provider-work-inbox"), { withCredentials: true })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(LogLevel.Warning)
      .build();
    realtime.on("InboxChanged", (event: { reason?: string; requestId?: string }) => {
      if (event?.reason !== "REQUEST_QUOTE_LIMIT_REACHED" || event.requestId?.toLowerCase() !== requestId.toLowerCase()) return;
      setQuoteClosed(true);
      setError("견적 접수가 마감되었습니다");
    });
    void realtime.start().catch(() => undefined);
    return () => { void realtime.stop().catch(() => undefined); };
  }, [requestId]);

  const loadRevision = (current: QuoteDetail) => {
    setSummary(quoteSummaryText(current.revision.summary,current.revision.items));
    setTerms(quoteTermsText(current.revision.terms));
    const lineAmount=current.revision.items.reduce((sum,item)=>sum+item.lineTotalAmount,0);
    setVatMode(Math.abs(current.revision.totalAmount-lineAmount)<0.01?'INCLUDED':'EXCLUDED');
    const parsedDuration=parseDuration(current.revision.estimatedDurationText,defaultDurationUnit(categoryPath));
    setDurationAmount(parsedDuration.amount);setDurationUnit(parsedDuration.unit);
    setAvailableStartAt(toLocalInput(current.revision.availableStartAt));
    setValidUntil(toLocalInput(current.revision.validUntil));
    setRevisionReason("");
    setItems(
      current.revision.items.map((item) => ({
        itemName: item.itemName,
        description: item.description,
        quantity: item.quantity,
        unitText: item.unitText,
        unitPriceAmount: item.unitPriceAmount,
        itemCategoryCode: item.itemCategoryCode ?? 'UNCLASSIFIED',
      })),
    );
  };

  const updateItem = (index: number, patch: Partial<QuoteItemInput>) =>
    setItems((current) =>
      current.map((item, itemIndex) =>
        itemIndex === index ? { ...item, ...patch } : item,
      ),
    );

  const loadTemplate = async () => {
    const template = templates.find((item) => item.id === selectedTemplateId);
    if (!template) return;
    if ((summary.trim() || items.some((item) => item.itemName.trim())) &&
        !await soodalConfirm(`현재 작성 중인 내용을 '${template.name}' 기본폼으로 바꿀까요?`)) return;
    setSummary(template.summary);
    setTerms(template.terms ?? "");
    setVatMode(template.vatMode);
    const duration = parseDuration(template.estimatedDurationText, defaultDurationUnit(categoryPath));
    setDurationAmount(duration.amount);
    setDurationUnit(duration.unit);
    setItems(template.items.length ? template.items.map((item) => ({ ...item })) : [emptyItem()]);
    setError("");
    setMessage(`'${template.name}' 기본폼을 불러왔습니다. 일정과 유효기간을 확인해 주세요.`);
  };

  const saveTemplate = async () => {
    if (!summary.trim() || items.some((item) => !item.itemName.trim())) {
      setError("기본폼으로 저장하려면 견적 요약과 모든 품목명을 입력해 주세요.");
      return;
    }
    const selected = templates.find((item) => item.id === selectedTemplateId);
    const name = (await soodalPrompt("기본폼 이름을 입력해 주세요. 같은 이름을 입력하면 기존 기본폼을 갱신합니다.", selected?.name ?? "", { title: "견적 기본폼 저장", multiline: false }))?.trim();
    if (!name) return;
    setSaving(true); setError(""); setMessage(""); setTemplateMessage("");
    try {
      const saved = await quoteApi.saveQuoteTemplate({
        name, summary: summary.trim(), terms: terms.trim() || null,
        estimatedDurationText: durationAmount ? `${durationAmount} ${durationUnit}` : null,
        vatMode,
        items: items.map((item) => ({ ...item, quantity: Number(item.quantity), unitPriceAmount: Number(item.unitPriceAmount), description: item.description || null, unitText: item.unitText || null })),
      });
      const next = await quoteApi.getQuoteTemplates();
      setTemplates(next); setSelectedTemplateId(saved.id);
      setTemplateMessage(`${saved.name} 기본폼 이름으로 저장했습니다.`);
    } catch (reason) { setError(reason instanceof Error ? reason.message : "기본폼을 저장하지 못했습니다."); }
    finally { setSaving(false); }
  };

  const deleteTemplate = async () => {
    const template = templates.find((item) => item.id === selectedTemplateId);
    if (!template || !await soodalConfirm(`'${template.name}' 기본폼을 삭제할까요?`)) return;
    setSaving(true); setError(""); setMessage("");
    try {
      await quoteApi.deleteQuoteTemplate(template.id);
      setTemplates((current) => current.filter((item) => item.id !== template.id));
      setSelectedTemplateId(""); setMessage("기본폼을 삭제했습니다.");
    } catch (reason) { setError(reason instanceof Error ? reason.message : "기본폼을 삭제하지 못했습니다."); }
    finally { setSaving(false); }
  };
  const itemAmount = items.reduce(
    (sum, item) =>
      sum + Number(item.quantity || 0) * Number(item.unitPriceAmount || 0),
    0,
  );
  const vatAmount = Math.round(vatMode==='INCLUDED' ? itemAmount/11 : itemAmount*0.1);
  const subtotal = vatMode==='INCLUDED' ? itemAmount-vatAmount : itemAmount;
  const totalAmount = vatMode==='INCLUDED' ? itemAmount : subtotal+vatAmount;
  const editable = !quoteClosed && (quote?.canEdit ?? true);

  const save = async (): Promise<QuoteDetail | null> => {
    if (items.some((item) => !Number.isInteger(Number(item.quantity)) || Number(item.quantity) < 1)) {
      setError("수량은 1 이상의 정수로 입력해 주세요.");
      return null;
    }
    setSaving(true);
    setError("");
    setMessage("");
    try {
      const payload: SaveQuoteRevisionInput = {
        summary,
        terms: terms || null,
        vatAmount: Number(vatAmount),
        estimatedDurationText: durationAmount ? `${durationAmount} ${durationUnit}` : null,
        availableStartAt: availableStartAt
          ? new Date(availableStartAt).toISOString()
          : null,
        validUntil: new Date(validUntil).toISOString(),
        revisionReason: revisionReason || null,
        idempotencyKey: idempotencyKey.current,
        items: items.map((item) => ({
          ...item,
          quantity: Number(item.quantity),
          unitPriceAmount: Number(item.unitPriceAmount),
          description: item.description || null,
          unitText: item.unitText || null,
        })),
        vatMode,
      };
      const saved = quote
        ? await quoteApi.addQuoteRevision(quote.id, payload)
        : await quoteApi.createProviderQuote(requestId, payload);
      setQuote(saved);
      setReadiness(await quoteApi.getQuoteSubmissionReadiness(requestId));
      loadRevision(saved);
      idempotencyKey.current = crypto.randomUUID();
      setMessage(
        saved.status === "SUBMITTED"
          ? "수정 견적서가 제출되었습니다."
          : "견적이 임시저장되었습니다.",
      );
      return saved;
    } catch (reason) {
      setError(
        reason instanceof Error
          ? reason.message
          : "견적을 저장하지 못했습니다.",
      );
      return null;
    } finally {
      setSaving(false);
    }
  };

  const saveAndPrepareCharge = async () => {
    const saved = await save();
    if (!saved) return;
    try {
      const latestReadiness = await quoteApi.getQuoteSubmissionReadiness(requestId);
      setReadiness(latestReadiness);
      const returnUrl = `${window.location.pathname}${window.location.search}`;
      const shortfall = Math.max(0, latestReadiness.expectedAcceptanceFee - latestReadiness.availableWalletBalance);
      navigate(`/provider/wallet?returnUrl=${encodeURIComponent(returnUrl)}&requestId=${encodeURIComponent(requestId)}&expectedFee=${latestReadiness.expectedAcceptanceFee}&shortfall=${shortfall}`);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "최신 이용료 잔액 상태를 확인하지 못했습니다.");
    }
  };

  const submit = async () => {
    if (!quote) return;
    setSaving(true);
    setError("");
    setMessage("");
    try {
      const submitted = await quoteApi.submitQuote(quote.id);
      setQuote(submitted);
      loadRevision(submitted);
      setMessage("견적이 고객에게 제출되었습니다.");
    } catch (reason) {
      setError(
        reason instanceof Error
          ? reason.message
          : "견적을 제출하지 못했습니다.",
      );
    } finally {
      setSaving(false);
    }
  };

  return (
    <section className="detailCard quotePanel" id="provider-quote-form" tabIndex={-1}>
      <div className="sectionHeading">
        <div>
          <p className="eyebrow">QUOTE</p>
          <h2>견적 작성</h2>
        </div>
        {quote && (
          <span className="statusBadge">
            {quoteStatusLabel(quote.status)} · {quote.revision.revisionNo}차
          </span>
        )}
      </div>
      {error && <div className="errorBanner">{error}</div>}
      {message && quote?.status !== "SUBMITTED" && <div className="successBanner">{message}</div>}
      {editable && templates.length > 0 && <section className="quoteTemplateBox" aria-labelledby="quote-template-title">
        <div><strong id="quote-template-title">견적 기본폼</strong><small>반복 사용하는 요약·조건·작업기간·품목·단가를 저장하고 불러옵니다. 작업 가능일과 유효기간은 현재 요청에 맞게 별도로 입력합니다.</small></div>
        <div className="quoteTemplateActions">
          <select aria-label="저장한 견적 기본폼" value={selectedTemplateId} disabled={saving} onChange={(event) => setSelectedTemplateId(event.target.value)}>
            <option value="">저장한 기본폼 선택</option>
            {templates.map((template) => <option key={template.id} value={template.id}>{template.name}</option>)}
          </select>
          <button className="secondaryButton" type="button" disabled={saving || !selectedTemplateId} onClick={() => void loadTemplate()}>불러오기</button>
          <button className="quoteTemplateDelete" type="button" disabled={saving || !selectedTemplateId} onClick={() => void deleteTemplate()}>삭제</button>
        </div>
      </section>}
      <div className="formGrid">
        <div className="formField fullWidth">
          <label>견적 요약 *</label>
          <textarea
            rows={3}
            maxLength={1000}
            placeholder="예: LED 거실등 2개 교체와 기존 등기구 철거 비용을 포함한 견적입니다."
            value={summary}
            disabled={!editable}
            onChange={(event) => setSummary(event.target.value)}
          />
        </div>
        <div className="formField fullWidth">
          <label>조건·설명</label>
          <textarea
            rows={4}
            placeholder="예: 자재·철거·폐기 비용 포함, 주차비 별도, 현장 상태에 따라 추가 작업은 고객 동의 후 진행합니다."
            value={terms}
            disabled={!editable}
            onChange={(event) => setTerms(event.target.value)}
          />
        </div>
        <div className="formField">
          <label>예상 작업기간</label>
          <div className="durationInput"><input type="number" min="0.5" step="0.5" value={durationAmount} disabled={!editable} placeholder="숫자" onChange={(event) => setDurationAmount(event.target.value)} /><select value={durationUnit} disabled={!editable} onChange={(event)=>setDurationUnit(event.target.value)}><option value="시간">시간</option><option value="일">일수</option><option value="개월">개월</option></select></div>
        </div>
        <div className="formField">
          <label>작업 가능 시작일</label>
          <input
            type="datetime-local"
            value={availableStartAt}
            disabled={!editable}
            onChange={(event) => setAvailableStartAt(event.target.value)}
          />
        </div>
        <div className="formField">
          <label>견적 유효기간 *</label>
          <input
            type="datetime-local"
            required
            value={validUntil}
            disabled={!editable}
            max={toLocalInput(requestExpiresAt)}
            onChange={(event) => setValidUntil(event.target.value)}
          />
        </div>
        <div className="formField">
          <label>부가세</label>
          <select value={vatMode} disabled={!editable} onChange={(event)=>setVatMode(event.target.value as 'INCLUDED'|'EXCLUDED')}><option value="INCLUDED">부가세 포함(입력 단가에 포함)</option><option value="EXCLUDED">부가세 별도(소계의 10%)</option></select><small>자동 계산 부가세 {formatMoney(vatAmount)}</small>
        </div>
        {quote && (
          <div className="formField fullWidth">
            <label>견적 수정 사유</label>
            <input
              maxLength={1000}
              value={revisionReason}
              disabled={!editable}
              onChange={(event) => setRevisionReason(event.target.value)}
            />
          </div>
        )}
      </div>
      <div className="quoteItems">
        <div className="sectionHeading">
          <h3>견적 항목</h3>
          {editable && (
            <button
              className="secondaryButton inlineButton"
              type="button"
              onClick={() => setItems((current) => [...current, emptyItem()])}
            >
              항목 추가
            </button>
          )}
        </div>
        {items.map((item, index) => (
          <div className="quoteItemEditor" key={index}>
            <div className="formField">
              <label>품목 *</label>
              <input
                maxLength={200}
                value={item.itemName}
                disabled={!editable}
                onChange={(event) =>
                  updateItem(index, { itemName: event.target.value })
                }
              />
            </div>
            <div className="formField">
              <label>설명</label>
              <textarea
                maxLength={1000}
                rows={3}
                value={item.description ?? ""}
                disabled={!editable}
                onChange={(event) =>
                  updateItem(index, { description: event.target.value })
                }
              />
            </div>
            <div className="formField">
              <label>수량 *</label>
              <input
                type="number"
                min="1"
                step="1"
                value={item.quantity || ""}
                disabled={!editable}
                onChange={(event) =>
                  updateItem(index, { quantity: event.target.value === "" ? 0 : Math.max(1, Math.trunc(Number(event.target.value))) })
                }
              />
            </div>
            <div className="formField">
              <label>단위</label>
              <select value={item.unitText??'식'} disabled={!editable} onChange={(event)=>updateItem(index,{unitText:event.target.value})}>{unitOptions.map(unit=><option key={unit} value={unit}>{unit}</option>)}</select>
            </div>
            <div className="formField">
              <label>단가 *</label>
              <input
                type="text"
                inputMode="numeric"
                value={item.unitPriceAmount ? item.unitPriceAmount.toLocaleString('ko-KR') : ""}
                disabled={!editable}
                onChange={(event) =>
                  updateItem(index, {
                    unitPriceAmount: digitsOnly(event.target.value),
                  })
                }
              />
            </div>
            <div className="lineTotal">
              <span>항목 금액</span>
              <strong>
                {formatMoney(item.quantity * item.unitPriceAmount)}
              </strong>
              {editable && items.length > 1 && (
                <button
                  type="button"
                  onClick={() =>
                    setItems((current) =>
                      current.filter((_, itemIndex) => itemIndex !== index),
                    )
                  }
                >
                  삭제
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
      <div className="quoteTotals">
        <span>공급가액 {formatMoney(subtotal)}</span>
        <span>부가세 {formatMoney(vatAmount)}</span>
        <strong>총액 {formatMoney(totalAmount)}</strong>
      </div>
      {editable && <section className="quoteTemplateSavePrompt"><div><strong>이 견적을 다음에도 재사용하시겠어요?</strong><small>현재 내용을 견적 기본폼으로 저장하면 다음 견적 작성 시 불러올 수 있습니다.</small></div><div className="quoteTemplateSaveAction"><button className="secondaryButton" type="button" disabled={saving || !summary.trim() || items.some(item=>!item.itemName.trim())} onClick={()=>void saveTemplate()}>기본폼으로 저장</button>{templateMessage&&<span role="status">{templateMessage}</span>}</div></section>}
      {editable && (
        <>
        {readiness&&<section className={`quoteFundingStatus${readiness.canSubmit?' ready':' insufficient'}`} aria-live="polite">
          <div><span>예상 수수료 예약</span><strong>{formatMoney(readiness.expectedAcceptanceFee)}</strong></div>
          <div><span>사용 가능 이용료</span><strong>{formatMoney(readiness.availableWalletBalance)}</strong></div>
          <div><span>제출 후 사용 가능 잔액</span><strong>{formatMoney(Math.max(0,readiness.availableWalletBalance-readiness.expectedAcceptanceFee))}</strong></div>
          {readiness.canSubmit
            ? <p>현재 서비스의 수수료 정책에 설정된 정액 {formatMoney(readiness.expectedAcceptanceFee)}입니다. 견적 총액 {formatMoney(totalAmount)}의 비율 계산값이 아니며, 미채택·만료 시 자동으로 반환됩니다.</p>
            : readiness.unavailableReason==='WALLET_INSUFFICIENT_BALANCE'
              ? <p>이용료 잔액이 {formatMoney(Math.max(0,readiness.expectedAcceptanceFee-readiness.availableWalletBalance))} 부족합니다. 작성 내용을 임시저장한 후 결제를 준비할 수 있습니다.</p>
              : <p>현재 이용료 잔액 상태로는 견적을 제출할 수 없습니다. 이용료 잔액 상태를 확인해 주세요.</p>}
        </section>}
        <div className="formActions">
          {message && quote?.status === "SUBMITTED" && <span className="quoteActionMessage" role="status">{message}</span>}
          <button
            className="secondaryButton"
            type="button"
            disabled={
              saving ||
              !summary.trim() ||
              items.some((item) => !item.itemName.trim())
            }
            onClick={() => void save()}
          >
            {quote?.status === "SUBMITTED" ? "수정 견적서 제출" : "임시저장"}
          </button>
          {quote?.status === "DRAFT" && (
            <button
              className="primaryButton"
              type="button"
              disabled={saving || !readiness?.canSubmit}
              title={!readiness?.canSubmit ? "예상 수수료 보다 잔액이 부족합니다." : undefined}
              onClick={() => void submit()}
            >
              견적 제출
            </button>
          )}
          {(!quote||quote.status==='DRAFT')&&readiness?.unavailableReason==='WALLET_INSUFFICIENT_BALANCE'&&(
            <button className="primaryButton" type="button" disabled={saving||!summary.trim()||items.some(item=>!item.itemName.trim())} onClick={()=>void saveAndPrepareCharge()}>
              임시저장 후 이용료 결제
            </button>
          )}
        </div>
        </>
      )}
      {quote?.transactionId && (
        <p className="successBanner">
          선택된 견적입니다. 거래번호: {quote.transactionId}
        </p>
      )}
    </section>
  );
}

export function CustomerQuotesPanel({ requestId, onSiteVisitCountChange }: { requestId: string; onSiteVisitCountChange?: (count: number) => void }) {
  const [isEmergency, setIsEmergency] = useState(false);
  const [quotes, setQuotes] = useState<CustomerQuoteComparison[]>([]),
    [selected, setSelected] = useState<CustomerQuoteDetail | null>(null);
  const [profile, setProfile] = useState<CustomerProviderProfile | null>(null),
    [accepting, setAccepting] = useState(false);
  const [publicReviews,setPublicReviews]=useState<PublicProviderReviewList|null>(null),[reviewProviderId,setReviewProviderId]=useState(''),[reviewsLoading,setReviewsLoading]=useState(false);
  const [error, setError] = useState(""),
    [message, setMessage] = useState(""),
    [loading, setLoading] = useState(true);
  const [defaultDetailAddress, setDefaultDetailAddress] = useState("");
  const [acceptDetailAddress, setAcceptDetailAddress] = useState("");
  const [showBlockForm, setShowBlockForm] = useState(false);
  const [blockReason, setBlockReason] = useState("");
  const [blockMemo, setBlockMemo] = useState("");
  const [blocking, setBlocking] = useState(false);
  const focusSection = (id: string) => window.requestAnimationFrame(() => window.requestAnimationFrame(() => { const target = document.getElementById(id); target?.scrollIntoView({ behavior: "smooth", block: "start" }); target?.focus({ preventScroll: true }); }));
  const load = useCallback(
    () =>
      quoteApi
        .getCustomerQuotes(requestId)
        .then(setQuotes)
        .catch((reason: Error) => setError(reason.message))
        .finally(() => setLoading(false)),
    [requestId],
  );
  useEffect(() => {
    void load();
  }, [load]);
  useEffect(() => {
    requestApi
      .getMyRequest(requestId)
      .then((value) => { const address=value.detailAddress ?? ""; setIsEmergency(value.isUrgent); setDefaultDetailAddress(address); setAcceptDetailAddress(current=>current||address) })
      .catch(() => setIsEmergency(false));
  }, [requestId]);
  useEffect(() => {
    void loadDefaultAddress()
      .then((value) => { const address=value?.fullAddress||""; setDefaultDetailAddress((current) => current || address); setAcceptDetailAddress(current=>current||address) })
      .catch(() => undefined);
  }, []);
  const show = async (quoteId: string) => {
    setError("");
    setProfile(null);
    setPublicReviews(null);
    setReviewProviderId("");
    try {
      setSelected(await quoteApi.getCustomerQuote(quoteId));
      focusSection("customer-quote-detail");
    } catch (reason) {
      setError(
        reason instanceof Error
          ? reason.message
          : "견적을 불러오지 못했습니다.",
      );
    }
  };
  const showReviews=async()=>{
    if(!selected)return;
    setError("");setReviewsLoading(true);
    try{setPublicReviews(await quoteApi.getPublicProviderReviews(selected.providerId));setReviewProviderId(selected.providerId);focusSection("customer-provider-reviews")}catch(reason){setError(reason instanceof Error?reason.message:"고객 후기를 불러오지 못했습니다.")}finally{setReviewsLoading(false)}
  };
  const showProfile = async () => {
    if (!selected) return;
    setError("");
    try {
      setProfile(
        await quoteApi.getCustomerProviderProfile(
          selected.providerId,
          requestId,
        ),
      );
      focusSection("customer-provider-profile");
    } catch (reason) {
      setError(
        reason instanceof Error
          ? reason.message
          : "전문가 정보를 불러오지 못했습니다.",
      );
    }
  };
  const blockProvider = async () => {
    if (!profile) return;
    if (!blockReason) { setError("차단 사유를 선택해 주세요."); return; }
    if (!await soodalConfirm(`${profile.businessName} 전문가를 차단할까요? 기존 거래와 기록은 유지되며 새로운 매칭만 제한됩니다.`, { title: "전문가 차단" })) return;
    try {
      setBlocking(true);
      await customerAccountApi.blockProvider(profile.id, blockReason, blockMemo.trim() || null);
      setMessage("전문가를 차단했습니다. 마이수달에서 해제할 수 있습니다.");
      setProfile(null);
      setShowBlockForm(false); setBlockReason(""); setBlockMemo("");
      await load();
    } catch (reason) {
      setError(
        reason instanceof Error
          ? reason.message
          : "전문가를 차단하지 못했습니다.",
      );
    } finally { setBlocking(false); }
  };
  const accept = async () => {
    if (!selected) return;
    const detailAddress = acceptDetailAddress.trim();
    if (!detailAddress) {
      setError("견적을 선택하려면 상세주소를 입력해 주세요.");
      return;
    }
    if (detailAddress.length > 500) {
      setError("상세주소는 500자 이내로 입력해 주세요.");
      return;
    }
    if (
      !await soodalConfirm(
        `${selected.providerName}의 견적 ${formatMoney(selected.revision.totalAmount)}을 선택할까요? 선택 후에는 다른 견적을 선택할 수 없습니다.`,
        { title: "견적 선택", confirmText: "이 견적 선택" },
      )
    )
      return;
    setError("");
    setAccepting(true);
    try {
      const result = await quoteApi.acceptQuote(selected.id, detailAddress);
      setMessage(`견적을 선택했습니다. 거래번호: ${result.transactionId}`);
      setSelected(current => current ? { ...current, status: "ACCEPTED" } : current);
      const refreshed = await quoteApi.getCustomerQuote(selected.id).catch(() => null);
      if (refreshed) setSelected(refreshed);
      await load();
    } catch (reason) {
      setError(
        reason instanceof Error
          ? reason.message
          : "견적을 선택하지 못했습니다.",
      );
    } finally {
      setAccepting(false);
    }
  };
  if (isEmergency) return <EmergencyResponsesPanel requestId={requestId} />;
  return (
    <><CustomerSiteVisitPanel requestId={requestId} defaultDetailAddress={defaultDetailAddress} onCountChange={onSiteVisitCountChange}/><section className="detailCard quotePanel">
      <div className="sectionHeading">
        <div>
          <p className="eyebrow">RECEIVED QUOTES</p>
          <h2>받은 견적</h2>
        </div>
        <span>{quotes.length}건</span>
      </div>
      {error && <div className="errorBanner">{error}</div>}
      {message && <div className="successBanner">{message}</div>}
      {loading ? (
        <p className="emptyState">견적을 불러오는 중입니다.</p>
      ) : quotes.length === 0 ? (
        <p className="emptyState">
          아직 제출된 견적이 없습니다. 조건에 맞는 전문가에게 요청을 전달하고
          있습니다.
        </p>
      ) : (
        <>
          <p className="privacyNote">
            신뢰도 현재값이 높은 순으로 표시합니다. 플랫폼이 특정 전문가를
            추천하는 순위는 아닙니다.
          </p>
          <div className="quoteCompareGrid">
            {quotes.map((item) => (
              <button
                key={item.id}
                className={
                  selected?.id === item.id
                    ? "quoteCompareCard selectedQuote"
                    : "quoteCompareCard"
                }
                type="button"
                onClick={() => void show(item.id)}
              >
                <span className="statusBadge">
                  {item.isSelected ? "선택됨" : quoteStatusLabel(item.status)}
                </span>
                <h3>{item.providerName}</h3>
                <strong>{formatMoney(item.totalAmount)}</strong>
                <p>
                  {item.trustDisplay} · 공개 리뷰 {item.publicReviewCount}건
                </p>
                <small>
                  예상 작업기간 {item.estimatedDurationText || "협의"} · A/S 기준{" "}
                  {item.defaultWarrantyDays}일
                </small>
                <span className="quoteCardClickHint">클릭하여 상세 견적 보기 →</span>
              </button>
            ))}
          </div>
        </>
      )}
      {selected && (
        <div id="customer-quote-detail" tabIndex={-1} className="quoteDetail">
          <div className="sectionHeading">
            <div>
              <h3>{selected.providerName}</h3>
              <p>{quoteSummaryText(selected.revision.summary,selected.revision.items)}</p>
            </div>
            <strong>{formatMoney(selected.revision.totalAmount)}</strong>
          </div>
          <div className="quoteProviderActions">
            <button className="secondaryButton inlineButton" type="button" onClick={() => void showProfile()}>전문가 상세정보</button>
            <button className="secondaryButton inlineButton" type="button" disabled={reviewsLoading} onClick={() => void showReviews()}>{reviewsLoading?"후기 불러오는 중…":`고객 후기 ${selected.comparison.publicReviewCount}건 보기`}</button>
          </div>
          <dl>
            <dt>신뢰도</dt>
            <dd>{selected.comparison.trustDisplay}</dd>
            <dt>승인 상태</dt>
            <dd>
              전문가 {approvalStatusLabel(selected.comparison.providerApprovalStatus)} · 서비스{" "}
              {approvalStatusLabel(selected.comparison.serviceApprovalStatus)}
            </dd>
            <dt>필수 자격</dt>
            <dd>
              {selected.comparison.requiredEvidenceSatisfied
                ? "확인됨"
                : "확인 중"}
            </dd>
            <dt>제출일</dt>
            <dd>
              {selected.submittedAt ? formatDate(selected.submittedAt) : "-"}
            </dd>
            <dt>유효기간</dt>
            <dd>{formatDate(selected.revision.validUntil)}</dd>
            <dt>작업 가능일</dt>
            <dd>
              {selected.revision.availableStartAt
                ? formatDate(selected.revision.availableStartAt)
                : "협의"}
            </dd>
            <dt>예상 작업기간</dt>
            <dd>{selected.revision.estimatedDurationText || "협의"}</dd>
            <dt>A/S 기준</dt>
            <dd>{selected.comparison.defaultWarrantyDays}일</dd>
            <dt>조건·메모</dt>
            <dd>{quoteTermsText(selected.revision.terms)}</dd>
          </dl>
          <div className="ratingBars">
            {selected.comparison.ratingItemAverages.map((item) => (
              <span key={item.itemId}>
                {item.itemName} {item.averageValue.toFixed(1)} / {item.maxValue}{" "}
                ({item.ratingCount}건)
              </span>
            ))}
          </div>
          <div className="quoteItemTable">
            {selected.revision.items.map((item) => (
              <div key={item.lineNo}>
                <span>{item.itemName}</span>
                <span>
                  {item.quantity} {item.unitText ?? ""}
                </span>
                <span>{formatMoney(item.unitPriceAmount)}</span>
                <strong>{formatMoney(item.lineTotalAmount)}</strong>
              </div>
            ))}
          </div>
          <div className="quoteTotals">
            <span>소계 {formatMoney(selected.revision.subtotalAmount)}</span>
            <span>부가세 {formatMoney(selected.revision.vatAmount)}</span>
            <strong>총액 {formatMoney(selected.revision.totalAmount)}</strong>
          </div>
          {selected.status === "SUBMITTED" && (
            <div className="quoteAcceptanceBox">
              <div className="quoteAcceptanceHeading"><span aria-hidden="true">⌂</span><div><small>선택한 전문가의 방문 정보</small><h3>방문 상세주소</h3></div></div>
              <label className="quoteAddressField" htmlFor="quote-accept-detail-address"><span>동·호수 등 상세주소</span><input id="quote-accept-detail-address" type="text" maxLength={500} value={acceptDetailAddress} onChange={event=>setAcceptDetailAddress(event.target.value)} placeholder="예: 우리집 101동 1203호" /></label>
              <p className="quoteAddressPrivacy"><b aria-hidden="true">✓</b> 입력한 상세주소는 이 견적을 선택한 전문가에게만 안전하게 공개됩니다.</p>
              <div className="formActions"><button className="primaryButton" disabled={accepting||!acceptDetailAddress.trim()} type="button" onClick={() => void accept()}>{accepting ? "선택 처리 중…" : "이 견적 선택"}</button></div>
            </div>
          )}
          {selected.status === "ACCEPTED" && (
            <div className="successBanner"><p>선택된 견적입니다. 상세주소와 업무 연락처는 이 전문가에게만 공개됩니다.</p><button className="secondaryButton" type="button" onClick={()=>navigate('/customer/progress')}>진행 업무 확인</button></div>
          )}
        </div>
      )}
      {selected&&publicReviews&&reviewProviderId===selected.providerId&&(
        <section id="customer-provider-reviews" tabIndex={-1} className="providerPublicReviews" aria-label={`${selected.providerName} 고객 후기`}>
          <div className="sectionHeading"><div><p className="eyebrow">VERIFIED CUSTOMER REVIEWS</p><h3>검증된 고객 후기</h3><p>수달 라이프에서 실제로 완료된 거래의 공개 후기만 보여드립니다.</p></div><div><strong>{publicReviews.totalCount}건</strong><button type="button" onClick={()=>{setPublicReviews(null);setReviewProviderId('')}}>닫기</button></div></div>
          {publicReviews.items.length===0?<p className="emptyState">아직 공개된 고객 후기가 없습니다.</p>:<div className="providerPublicReviewList">{publicReviews.items.map(review=><article key={review.id}>
            <header><div><strong>{review.customerDisplayName}</strong><span className="verifiedReviewBadge">✓ 실제 거래 확인</span></div><time>{formatDate(review.submittedAt)}</time></header>
            {review.overallRating!=null&&<div className="reviewOverallRating" aria-label={`종합 평점 ${review.overallRating}`}>★ {review.overallRating.toFixed(1)}</div>}
            <p>{review.bodyText}</p>
            {review.ratings.length>0&&<div className="publicReviewRatings">{review.ratings.map(rating=><span key={rating.itemId}>{rating.itemName} <b>{rating.ratingValue.toFixed(1)}</b>/{rating.maxValue}</span>)}</div>}
            {review.files.some(file=>file.contentType.startsWith('image/')&&file.downloadUrl)&&<div className="publicReviewImages">{review.files.filter(file=>file.contentType.startsWith('image/')&&file.downloadUrl).map(file=><a key={file.fileId} href={apiUrl(file.downloadUrl!)} target="_blank" rel="noreferrer"><img src={apiUrl(file.downloadUrl!)} alt="고객 후기 첨부 이미지"/></a>)}</div>}
            {review.providerReply&&<div className="publicReviewReply"><strong>{review.providerReply.providerName} 답글</strong><p>{review.providerReply.bodyText}</p><time>{formatDate(review.providerReply.submittedAt)}</time></div>}
          </article>)}</div>}
          {publicReviews.totalCount>publicReviews.items.length&&<p className="privacyNote">최근 후기 {publicReviews.items.length}건을 표시하고 있습니다.</p>}
          <p className="reviewPrivacyNote">고객 이름은 개인정보 보호를 위해 일부 가려서 표시하며, 숨김 처리된 후기와 안전 확인이 끝나지 않은 첨부파일은 공개하지 않습니다.</p>
        </section>
      )}
      {profile && (
        <aside id="customer-provider-profile" tabIndex={-1} className="providerProfile">
          <div className="sectionHeading">
            <div className="providerProfileHero">
              <img
                className="providerProfileLogo"
                src={profile.publicLogoUrl ? apiUrl(profile.publicLogoUrl) : '/brand/soodal-life-mark.png'}
                alt={profile.publicLogoUrl ? `${profile.businessName} 로고` : '수달 라이프 기본 전문가 이미지'}
                onError={event=>{event.currentTarget.onerror=null;event.currentTarget.src='/brand/soodal-life-mark.png'}}
              />
              <div>
                <h3>{profile.businessName}</h3>
                <p>
                  {profile.trustDisplay} · 완료 서비스{" "}
                  {profile.completedServiceCount}건 · 공개 리뷰{" "}
                  {profile.publicReviewCount}건
                </p>
              </div>
            </div>
            <button type="button" onClick={() => setProfile(null)}>
              닫기
            </button>
          </div>
          {profile.introductionHtml && (
            <div
              className="providerPublicIntroduction"
              dangerouslySetInnerHTML={{
                __html: sanitizeProviderHtml(profile.introductionHtml),
              }}
            />
          )}
          <p>승인 서비스: {profile.activeServices.join(", ") || "없음"}</p>
          <p>
            필수 증빙 {profile.approvedEvidenceCount}/
            {profile.requiredEvidenceCount} 확인
          </p>
          {profile.publicPhone && (
            <p>
              <strong>전화</strong> {profile.publicPhone}
            </p>
          )}
          {profile.publicEmail && (
            <p>
              <strong>이메일</strong>{" "}
              <a href={`mailto:${profile.publicEmail}`}>
                {profile.publicEmail}
              </a>
            </p>
          )}
          {profile.publicAddress && (
            <p>
              <strong>주소</strong> {profile.publicAddress}{" "}
              <a
                href={`https://map.naver.com/p/search/${encodeURIComponent(profile.publicAddress)}`}
                target="_blank"
                rel="noreferrer"
              >
                지도 보기
              </a>
            </p>
          )}
          <div className="providerPublicLinks">
            {profile.publicBlogUrl && (
              <a href={profile.publicBlogUrl} target="_blank" rel="noreferrer">
                블로그
              </a>
            )}
            {profile.publicWebsiteUrl && (
              <a
                href={profile.publicWebsiteUrl}
                target="_blank"
                rel="noreferrer"
              >
                홈페이지
              </a>
            )}
          </div>
          {profile.publicPhotoUrls.length > 0 && (
            <div className="providerPublicGallery">
              {profile.publicPhotoUrls.map((url) => (
                <img
                  key={url}
                  src={apiUrl(url)}
                  alt={`${profile.businessName} 홍보 사진`}
                />
              ))}
            </div>
          )}
          <section className="providerReviewPreview">
            <div><h4>고객 후기</h4><span>실제 거래 확인 · {profile.publicReviewCount}건</span></div>
            {profile.recentReviews.map((review) => <blockquote key={review.id}>{review.bodyText}<small>{formatDate(review.submittedAt)}</small></blockquote>)}
            {profile.recentReviews.length===0&&<p>아직 공개된 고객 후기가 없습니다.</p>}
            {profile.publicReviewCount>0&&<button className="secondaryButton" type="button" onClick={()=>void showReviews()}>전체 고객 후기 보기</button>}
          </section>
          {!showBlockForm ? <button className="dangerButton" type="button" onClick={() => setShowBlockForm(true)}>이 전문가 차단</button> :
            <div className="providerBlockForm">
              <label>차단 사유 <b>*</b><select required value={blockReason} onChange={event => setBlockReason(event.target.value)}><option value="">선택해 주세요</option>{providerBlockReasons.map(([value,label])=><option key={value} value={value}>{label}</option>)}</select></label>
              <label>메모(선택)<textarea maxLength={1000} rows={4} value={blockMemo} onChange={event => setBlockMemo(event.target.value)} placeholder="본인 확인용 메모를 입력할 수 있습니다." /></label>
              <small>선택한 표준 사유는 전문가가 확인할 수 있습니다. 메모와 고객 신원은 전문가에게 공개되지 않습니다.</small>
              <div><button type="button" onClick={()=>{setShowBlockForm(false);setBlockReason('');setBlockMemo('')}}>취소</button><button className="dangerButton" type="button" disabled={blocking || !blockReason} onClick={() => void blockProvider()}>{blocking?'처리 중…':'차단하기'}</button></div>
            </div>}
          {!showBlockForm && <small>차단은 새로운 매칭·견적 선택만 제한하며 신고와 별도로 처리됩니다.</small>}
        </aside>
      )}
    </section></>
  );
}

const formatMoney = (value: number) =>
  new Intl.NumberFormat("ko-KR", {
    style: "currency",
    currency: "KRW",
    maximumFractionDigits: 4,
  }).format(value);
const quoteStatusLabel = (value: string) => (({ DRAFT: "작성 중", SUBMITTED: "제출됨", ACCEPTED: "선택됨", NOT_SELECTED: "미선택", WITHDRAWN: "철회됨", EXPIRED: "기간 만료", INVALIDATED: "무효" } as Record<string,string>)[value] ?? value);
const approvalStatusLabel = (value: string) => (({ APPROVED: "승인 완료", PENDING: "승인 심사 중", IN_REVIEW: "승인 심사 중", REJECTED: "승인 반려", SUSPENDED: "승인 정지", INACTIVE: "비활성" } as Record<string,string>)[value] ?? value);
const formatDate = (value: string) =>
  new Intl.DateTimeFormat("ko-KR", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
function toLocalInput(value: string | null) {
  if (!value) return "";
  const date = new Date(value);
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 16);
}

function sanitizeProviderHtml(value: string) {
  const documentValue = new DOMParser().parseFromString(value, "text/html");
  documentValue
    .querySelectorAll(
      "script,iframe,object,embed,form,input,button,style,link,meta",
    )
    .forEach((node) => node.remove());
  documentValue.body.querySelectorAll("*").forEach((element) => {
    for (const attribute of Array.from(element.attributes)) {
      const name = attribute.name.toLowerCase();
      if (name === "style") {
        const safe = attribute.value
          .split(";")
          .filter((rule) =>
            /^(color|font-weight|font-style|text-decoration|text-align)\s*:/i.test(
              rule.trim(),
            ),
          )
          .join(";");
        if (safe) element.setAttribute("style", safe);
        else element.removeAttribute("style");
      } else if (
        name === "href" &&
        element.tagName === "A" &&
        /^https:\/\//i.test(attribute.value)
      ) {
        element.setAttribute("target", "_blank");
        element.setAttribute("rel", "noreferrer");
      } else element.removeAttribute(attribute.name);
    }
  });
  return documentValue.body.innerHTML;
}
