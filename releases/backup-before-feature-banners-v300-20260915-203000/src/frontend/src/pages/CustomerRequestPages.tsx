import { useCallback, useEffect, useRef, useState } from "react";
import type { PropsWithChildren } from "react";
import { navigate } from "../auth/routing";
import { CustomerAppLayout } from "../customer/CustomerAppLayout";
import { publicCatalogApi } from "../customer/api";
import type { PublicServiceDetail } from "../customer/types";
import { AuthenticatedFilePreview } from "../components/AuthenticatedFilePreview";
import * as requestApi from "../requests/api";
import { DynamicFieldInput } from "../requests/DynamicFieldInput";
import type { DynamicValue } from "../requests/DynamicFieldInput";
import type {
  AdministrativeArea,
  Category,
  RequestField,
  RequestFile,
  ServiceRequestDetail,
  ServiceRequestListItem,
} from "../requests/types";
import { interiorApi } from "../customer/interiorApi";
import {
  formatRequestAnswer,
  formatRequestAnswerLabel,
  isZeroRequestAnswer,
} from "../requests/formatRequestAnswer";
import { CustomerQuotesPanel } from "../quotes/QuotePanels";
import { loadDefaultAddress } from "../customer/defaultAddress";
import { soodalConfirm } from "../components/soodalDialog";
import "./customerRequestFlow.css";
import { ServiceThumbnail, serviceNameFromPath } from "../serviceVisuals/ServiceVisual";

const steps = ["서비스", "요청 정보·지역", "일정", "사진·파일", "확인", "공개"];

export function NewCustomerRequestPage() {
  const [step, setStep] = useState(0),
    [draftId, setDraftId] = useState<string | null>(null);
  const [majors, setMajors] = useState<Category[]>([]),
    [middles, setMiddles] = useState<Category[]>([]),
    [services, setServices] = useState<Category[]>([]);
  const [sidos, setSidos] = useState<AdministrativeArea[]>([]),
    [areas, setAreas] = useState<AdministrativeArea[]>([]),
    [fields, setFields] = useState<RequestField[]>([]);
  const [majorId, setMajorId] = useState(""),
    [middleId, setMiddleId] = useState(""),
    [serviceId, setServiceId] = useState(""),
    [sidoId, setSidoId] = useState(""),
    [areaId, setAreaId] = useState("");
  const [serviceDetail, setServiceDetail] =
    useState<PublicServiceDetail | null>(null);
  const [title, setTitle] = useState(""),
    [description, setDescription] = useState("");
  const [detailAddress, setDetailAddress] = useState(""),
    [detailAddressDisclosureCode, setDetailAddressDisclosureCode] = useState<
      "AFTER_SELECTION" | "BEFORE_QUOTE"
    >("AFTER_SELECTION");
  const emergencyMode =
    new URLSearchParams(window.location.search).get("emergency") === "1";
  const interiorMode =
    new URLSearchParams(window.location.search).get("domain") === "INTERIOR";
  const [isUrgent, setIsUrgent] = useState(emergencyMode),
    [answers, setAnswers] = useState<Record<string, DynamicValue>>({}),
    [files, setFiles] = useState<RequestFile[]>([]);
  const [error, setError] = useState(""),
    [notice, setNotice] = useState(""),
    [busy, setBusy] = useState(false),
    [canRetrySave, setCanRetrySave] = useState(false);
  const idempotencyKey = useRef(crypto.randomUUID());
  useEffect(() => {
    if (emergencyMode && !isUrgent) setIsUrgent(true);
  }, [emergencyMode, isUrgent]);
  useEffect(() => {
    if (new URLSearchParams(window.location.search).has("draft")) return;
    void loadDefaultAddress()
      .then((value) => {
        if (!value) return;
        if (value.area) {
          setSidoId((current) => current || value.sidoId);
          setAreaId((current) => current || value.area!.id);
        }
        setDetailAddress((current) => current || value.fullAddress);
      })
      .catch(() => undefined);
  }, []);

  useEffect(() => {
    let cancelled = false;
    const selectedId = new URLSearchParams(window.location.search).get(
      "service",
    );
    const load = async () => {
      setError("");
      const [majorResult, sidoResult, areaResult] = await Promise.allSettled([
        requestApi.getMajorCategories(emergencyMode),
        requestApi.getSidoAreas(),
        requestApi.getAdministrativeAreas(),
      ]);
      if (cancelled) return;
      if (majorResult.status === "fulfilled") setMajors(majorResult.value);
      if (sidoResult.status === "fulfilled") setSidos(sidoResult.value);
      if (areaResult.status === "fulfilled") setAreas(areaResult.value);

      if (!selectedId) {
        const failed = [majorResult, sidoResult, areaResult].find(
          (result) => result.status === "rejected",
        );
        if (failed?.status === "rejected")
          setError(
            requestErrorMessage(
              failed.reason,
              "요청에 필요한 기본 정보를 불러오지 못했습니다.",
            ),
          );
        return;
      }

      try {
        const selected = await publicCatalogApi.detail(selectedId);
        if (cancelled) return;
        if (emergencyMode && !selected.emergencyRequestAllowed) {
          setError(
            "선택한 서비스는 긴급출동을 지원하지 않습니다. 긴급출동 가능한 서비스를 선택해 주세요.",
          );
          return;
        }
        setMajorId(selected.majorId);
        setMiddleId(selected.middleId);
        setServiceId(selected.id);
        setServiceDetail(selected);

        const [middleItems, serviceItems, fieldItems] = await Promise.all([
          requestApi.getMiddleCategories(selected.majorId, emergencyMode),
          requestApi.getServiceCategories(selected.middleId, emergencyMode),
          requestApi.getRequestFields(selected.id),
        ]);
        if (cancelled) return;
        setMiddles(middleItems);
        setServices(
          serviceItems.some((item) => item.id === selected.id)
            ? serviceItems
            : [
                ...serviceItems,
                {
                  id: selected.id,
                  name: selected.name,
                  level: "SERVICE",
                  externalCode: selected.code,
                  sortOrder: Number.MAX_SAFE_INTEGER,
                },
              ],
        );
        setFields(fieldItems);
        applyServiceDefaults(selected, fieldItems, setTitle, setAnswers);
        setStep(1);
        if (
          sidoResult.status === "rejected" ||
          areaResult.status === "rejected"
        ) {
          setError(
            "서비스 선택은 유지했지만 지역 목록을 불러오지 못했습니다. 새로고침 후 다시 선택해 주세요.",
          );
        }
      } catch (reason) {
        if (!cancelled)
          setError(
            requestErrorMessage(
              reason,
              "선택한 서비스의 요청 항목을 불러오지 못했습니다. 잠시 후 다시 시도해 주세요.",
            ),
          );
      }
    };
    void load();
    return () => {
      cancelled = true;
    };
  }, [emergencyMode]);
  useEffect(() => {
    const resumeId = new URLSearchParams(window.location.search).get("draft");
    if (!resumeId) return;
    requestApi
      .getMyRequest(resumeId)
      .then(async (draft) => {
        if (!draft.canEdit) throw new Error("수정할 수 없는 요청입니다.");
        const [middleItems, serviceItems, fieldItems, detail] =
          await Promise.all([
            requestApi.getMiddleCategories(
              draft.majorCategoryId,
              emergencyMode,
            ),
            requestApi.getServiceCategories(
              draft.middleCategoryId,
              emergencyMode,
            ),
            requestApi.getRequestFields(draft.serviceCategoryId),
            publicCatalogApi.detail(draft.serviceCategoryId),
          ]);
        if (emergencyMode && !detail.emergencyRequestAllowed)
          throw new Error(
            "이 임시 요청의 서비스는 현재 긴급출동을 지원하지 않습니다.",
          );
        setDraftId(draft.id);
        setMajorId(draft.majorCategoryId);
        setMiddleId(draft.middleCategoryId);
        setServiceId(draft.serviceCategoryId);
        setServiceDetail(detail);
        setMiddles(middleItems);
        setServices(serviceItems);
        setFields(fieldItems);
        setAreaId(draft.administrativeAreaId ?? "");
        setSidoId(
          areas.find((item) => item.id === draft.administrativeAreaId)
            ?.parentId ?? "",
        );
        setTitle(draft.title === "작성 중인 요청" ? "" : draft.title);
        setDescription(draft.description ?? "");
        setDetailAddress(draft.detailAddress ?? "");
        setDetailAddressDisclosureCode(
          draft.detailAddressDisclosureCode === "BEFORE_QUOTE"
            ? "BEFORE_QUOTE"
            : "AFTER_SELECTION",
        );
        setIsUrgent(draft.isUrgent);
        const restoredAnswers: Record<string, DynamicValue> = {};
        const expiredScheduleLabels: string[] = [];
        for (const answer of draft.answers) {
          const field = fieldItems.find(
            (candidate) => candidate.id === answer.fieldId,
          );
          if (field?.inputType === "DATETIME") {
            const parsed = new Date(String(answer.value));
            if (
              field.validationRule.includes("현재 이후") &&
              !Number.isNaN(parsed.getTime()) &&
              parsed.getTime() <= Date.now()
            ) {
              expiredScheduleLabels.push(field.label);
              continue;
            }
            restoredAnswers[answer.fieldId] = toLocalDateTimeValue(
              answer.value,
            );
            continue;
          }
          restoredAnswers[answer.fieldId] = answer.value as DynamicValue;
        }
        setAnswers(restoredAnswers);
        setFiles(draft.files);
        setNotice(
          expiredScheduleLabels.length > 0
            ? `지난 희망일시는 비웠습니다. 일정 단계에서 ${expiredScheduleLabels.join(", ")}을(를) 다시 선택해 주세요.`
            : "임시저장한 요청을 이어서 작성합니다.",
        );
      })
      .catch((reason: unknown) =>
        setError(requestErrorMessage(reason, "임시 요청을 불러오지 못했습니다.")),
      );
  }, [areas, emergencyMode]);
  const chooseMajor = async (id: string) => {
    setMajorId(id);
    setMiddleId("");
    setServiceId("");
    setMiddles(
      id ? await requestApi.getMiddleCategories(id, emergencyMode) : [],
    );
    setServices([]);
    setFields([]);
  };
  const chooseMiddle = async (id: string) => {
    setMiddleId(id);
    setServiceId("");
    setServices(
      id ? await requestApi.getServiceCategories(id, emergencyMode) : [],
    );
    setFields([]);
  };
  const chooseService = async (id: string) => {
    setServiceId(id);
    setAnswers({});
    setDescription("");
    setError("");
    if (!id) {
      setFields([]);
      setServiceDetail(null);
      return;
    }
    const [fieldItems, detail] = await Promise.all([
      requestApi.getRequestFields(id),
      publicCatalogApi.detail(id),
    ]);
    setFields(fieldItems);
    setServiceDetail(detail);
    applyServiceDefaults(detail, fieldItems, setTitle, setAnswers);
    if (emergencyMode && !detail.emergencyRequestAllowed)
      setError(
        "이 서비스는 긴급출동을 지원하지 않습니다. 다른 하위 서비스를 선택해 주세요.",
      );
  };
  const valueAnswers = () =>
    fields
      .filter(
        (field) =>
          field.inputType !== "FILE" && !isRetiredStructuralDuplicate(field) &&
          !isBudgetDuplicate(field, fields),
      )
      .flatMap((field) => {
        const rawValue = answers[field.id];
        if (isEmptyValue(rawValue)) return [];
        const value =
          field.inputType === "NUMBER" || field.inputType === "MONEY"
            ? Number(rawValue)
            : field.inputType === "DATETIME"
              ? new Date(String(rawValue)).toISOString()
              : rawValue;
        return [{ fieldId: field.id, value }];
      });
  const draftPayload = () => ({
    administrativeAreaId: isRemoteService ? null : areaId || null,
    title: title.trim() || null,
    description: description.trim() || null,
    detailAddress: isRemoteService ? null : detailAddress.trim() || null,
    detailAddressDisclosureCode: isRemoteService ? "AFTER_SELECTION" : detailAddressDisclosureCode,
    isUrgent,
    answers: valueAnswers(),
  });
  const saveDraft = async () => {
    if (!serviceId) throw new Error("서비스를 먼저 선택해 주세요.");
    if (!draftId) {
      const created = await requestApi.createServiceRequest({
        categoryId: serviceId,
        idempotencyKey: idempotencyKey.current,
        ...draftPayload(),
      });
      setDraftId(created.id);
      setNotice("현재 단계까지 안전하게 임시저장했습니다.");
      return created.id;
    }
    await requestApi.updateServiceRequest(draftId, draftPayload());
    setNotice("현재 단계까지 안전하게 임시저장했습니다.");
    return draftId;
  };
  const validateCurrentStep = () => {
    if (step === 0 && !serviceId) return "하위 서비스를 선택해 주세요.";
    if (step === 1) {
      if (!title.trim()) return "요청 제목을 입력해 주세요.";
      const missing = detailFields.find(
        (field) => field.required && isEmptyValue(answers[field.id]),
      );
      if (missing) return `${missing.label} 항목을 입력하거나 선택해 주세요.`;
      if (!isRemoteService && !sidoId) return "서비스 받을 시·도를 선택해 주세요.";
      if (!isRemoteService && !areaId) return "서비스 받을 시·군·구를 선택해 주세요.";
      if (!isRemoteService && !detailAddress.trim()) return "서비스 상세주소를 입력해 주세요.";
      if (!isRemoteService && detailAddress.trim().length > 500)
        return "서비스 상세주소는 500자 이내로 입력해 주세요.";
    }
    if (step === 2) {
      const missing = scheduleFields.find(
        (field) =>
          isEffectivelyRequiredScheduleField(field, dateTimeFields) &&
          isEmptyValue(answers[field.id]),
      );
      if (missing) return `${missing.label} 항목을 입력해 주세요.`;
      const pastDateTime = scheduleFields.find(
        (field) =>
          field.inputType === "DATETIME" &&
          !isEmptyValue(answers[field.id]) &&
          field.validationRule.includes("현재 이후") &&
          !isFutureDateTime(answers[field.id]),
      );
      if (pastDateTime)
        return `${pastDateTime.label}은(는) 현재 이후의 날짜와 시간을 선택해 주세요.`;
    }
    if (step === 3) {
      const missing = fileFields.find(
        (field) =>
          field.required &&
          !files.some((file) => file.requestFieldId === field.id),
      );
      if (missing) return `${missing.label} 파일을 첨부해 주세요.`;
    }
    return null;
  };
  const next = async () => {
    setBusy(true);
    setError("");
    setNotice("");
    setCanRetrySave(false);
    try {
      const validationMessage = validateCurrentStep();
      if (validationMessage) {
        setError(validationMessage);
        return;
      }
      if (
        step === 0 &&
        isUrgent &&
        serviceDetail &&
        !serviceDetail.emergencyRequestAllowed
      )
        throw new Error("긴급출동이 허용된 서비스를 선택해 주세요.");
      await saveDraft();
      setStep((value) => Math.min(value + 1, steps.length - 1));
    } catch (reason) {
      setError(requestErrorMessage(reason, "임시저장하지 못했습니다."));
      setCanRetrySave(isNetworkRequestError(reason));
    } finally {
      setBusy(false);
    }
  };
  const upload = async (selected: FileList | null, requestFieldId?: string) => {
    if (!selected) return;
    const selectedFiles = Array.from(selected);
    if (selectedFiles.some((file) => file.size > 5 * 1024 * 1024)) {
      setError("사진과 PDF 파일은 각각 5MB 이하만 업로드할 수 있습니다.");
      return;
    }
    setBusy(true);
    setError("");
    try {
      const id = await saveDraft();
      for (const file of selectedFiles) {
        const uploaded = await requestApi.uploadRequestFile(
          id,
          file,
          requestFieldId,
        );
        setFiles((current) => [...current, uploaded]);
      }
      setNotice(
        "파일은 비공개 저장되었습니다. 악성코드 검사와 사진 개인정보 보호 처리는 아직 연동되지 않아 전문가 공개가 제한됩니다.",
      );
    } catch (reason) {
      setError(requestErrorMessage(reason, "파일을 업로드하지 못했습니다."));
    } finally {
      setBusy(false);
    }
  };
  const removeFile = async (file: RequestFile) => {
    if (!draftId) return;
    await requestApi.deleteRequestFile(draftId, file.id);
    setFiles((current) => current.filter((item) => item.id !== file.id));
  };
  const publish = async () => {
    if (!draftId) return;
    setBusy(true);
    setError("");
    try {
      if (isUrgent && serviceDetail && !serviceDetail.emergencyRequestAllowed)
        throw new Error("긴급출동이 허용된 서비스를 선택해 주세요.");
      await saveDraft();
      const result = await requestApi.publishServiceRequest(draftId);
      setNotice(result.customerMessage);
      if(interiorMode){
        const project=await interiorApi.create(draftId);
        setNotice('수달 인테리어 상담 요청이 공개되고 내 프로젝트에 연결되었습니다.');
        setTimeout(()=>navigate(`/customer/interior/projects/${project.id}`,true),500);
      }else setTimeout(() => navigate(`/customer/requests/${draftId}`, true), 500);
    } catch (reason) {
      setError(requestErrorMessage(reason, "요청을 공개하지 못했습니다."));
    } finally {
      setBusy(false);
    }
  };
  const isInteriorService = serviceDetail?.majorName === "인테리어";
  const isRemoteService = serviceDetail?.requiresServiceAddress === false;
  const visibleFields = fields.filter(
    (field) =>
      !isRetiredStructuralDuplicate(field) &&
      !isBudgetDuplicate(field, fields),
  );
  const scheduleFields = visibleFields.filter((field) =>
    ["DATETIME", "PERIOD", "RECURRENCE"].includes(field.inputType),
  );
  const dateTimeFields = scheduleFields.filter(
    (field) => field.inputType === "DATETIME",
  );
  const detailFields = visibleFields.filter(
    (field) =>
      !["DATETIME", "PERIOD", "RECURRENCE", "FILE"].includes(field.inputType),
  );
  const fileFields = visibleFields.filter(
    (field) => field.inputType === "FILE",
  );
  const selectedService = services.find((item) => item.id === serviceId)?.name;

  return (
    <CustomerAppLayout>
      <div
        className={`requestJourney${emergencyMode ? " emergencyRequestJourney" : ""}`}
      >
        <header className="journeyHeader soodalHeroHeading">
          <p className="eyebrow">
            {emergencyMode ? "수달 긴급출동" : "서비스 요청"}
          </p>
          <h1>{emergencyMode ? "🚨 긴급출동 요청하기" : "서비스 요청하기"}</h1>
          <p>
            {emergencyMode
              ? "본사 정책에서 긴급출동이 허용된 서비스를 등록하면 현재 출동 가능한 전문가에게 요청을 전달합니다."
              : "필요한 내용만 단계별로 묻고, 매 단계 서버에 임시저장합니다."}
          </p>
          {emergencyMode && (
            <div className="emergencySafetyInline">
              <strong>
                즉시 위험하면 119 등 공공 긴급대응을 먼저 이용하세요.
              </strong>
              <span>
                현재 가능한 전문가가 없더라도 요청은 정상 등록되며, 출동 가능
                상태가 확인되는 전문가에게 전달됩니다.
              </span>
            </div>
          )}
        </header>
        <ol className="journeySteps">
          {steps.map((label, index) => (
            <li
              key={label}
              className={index === step ? "active" : index < step ? "done" : ""}
            >
              <span>{index + 1}</span>
              <small>{isRemoteService && label === "요청 정보·지역" ? "요청 정보" : label}</small>
            </li>
          ))}
        </ol>
        {error && (
          <div className="errorBanner" role="alert">
            <span>{error}</span>
            {canRetrySave && (
              <button type="button" disabled={busy} onClick={() => void next()}>
                다시 시도
              </button>
            )}
          </div>
        )}
        {notice && (
          <div className="successBanner" role="status">
            {notice}
          </div>
        )}
        <section className="journeyCard">
          {step === 0 && (
            <>
              <h2>어떤 서비스가 필요하세요?</h2>
              {emergencyMode && (
                <p className="privacyNote">
                  현재 정책에서 긴급출동이 허용된 서비스만 표시합니다.
                </p>
              )}
              <div className="formGrid threeColumns">
                <SelectField
                  label="대분류"
                  value={majorId}
                  items={majors}
                  onChange={chooseMajor}
                />
                <SelectField
                  label="중분류"
                  value={middleId}
                  items={middles}
                  onChange={chooseMiddle}
                  disabled={!majorId}
                />
                <SelectField
                  label="하위 서비스"
                  value={serviceId}
                  items={services}
                  onChange={chooseService}
                  disabled={!middleId}
                />
              </div>
              {emergencyMode && majors.length === 0 && (
                <p className="emptyState">
                  현재 긴급출동 요청이 가능한 서비스가 없습니다.
                </p>
              )}
            </>
          )}
          {step === 1 && (
            <>
              <h2>{selectedService} 요청을 알려주세요</h2>
              <div className="formGrid">
                <div className="formField fullWidth">
                  <label>요청 제목 *</label>
                  <input
                    maxLength={200}
                    value={title}
                    onChange={(event) => setTitle(event.target.value)}
                  />
                  <small>
                    선택한 하위 서비스에 맞춰 자연스러운 제목을 자동으로
                    채웠습니다. 필요하면 수정해 주세요.
                  </small>
                </div>
                {detailFields.map((field) => (
                  <GuidedRequestField
                    key={field.id}
                    field={field}
                    value={answers[field.id]}
                    service={serviceDetail}
                    interior={isInteriorService}
                    onChange={(value) =>
                      setAnswers((current) => ({
                        ...current,
                        [field.id]: value,
                      }))
                    }
                  />
                ))}
                {isRemoteService ? (
                  <div className="remoteServiceNotice fullWidth">
                    <strong>전국 온라인 서비스</strong>
                    <span>방문 주소와 상세주소 공개 시점은 입력하지 않습니다. 필요한 자료·계정 전달 방법은 선택한 전문가와 비공개 업무 채팅에서 협의하세요.</span>
                  </div>
                ) : (<>
                <h3 className="regionFieldHeading fullWidth">서비스 받을 주소</h3>
                <div className="formField">
                  <label>시·도 *</label>
                  <select
                    value={sidoId}
                    onChange={(event) => {
                      setSidoId(event.target.value);
                      setAreaId("");
                    }}
                  >
                    <option value="">선택하세요</option>
                    {sidos.map((area) => (
                      <option key={area.id} value={area.id}>
                        {area.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="formField">
                  <label>시·군·구 *</label>
                  <select
                    value={areaId}
                    disabled={!sidoId}
                    onChange={(event) => setAreaId(event.target.value)}
                  >
                    <option value="">선택하세요</option>
                    {areas
                      .filter((area) => area.parentId === sidoId)
                      .map((area) => (
                        <option key={area.id} value={area.id}>
                          {area.name}
                        </option>
                      ))}
                  </select>
                </div>
                <div className="formField fullWidth">
                  <label>상세주소 *</label>
                  <input
                    maxLength={500}
                    autoComplete="street-address"
                    value={detailAddress}
                    onChange={(event) => setDetailAddress(event.target.value)}
                    placeholder="도로명·건물명과 동·호수 등 방문할 주소"
                  />
                  <small>
                    주소는 암호화하여 저장하며 아래에서 선택한 시점에만
                    전문가에게 공개합니다.
                  </small>
                </div>
                <div className="formField fullWidth">
                  <label>상세주소 공개 시점 *</label>
                  <select
                    value={detailAddressDisclosureCode}
                    onChange={(event) =>
                      setDetailAddressDisclosureCode(
                        event.target.value as
                          "AFTER_SELECTION" | "BEFORE_QUOTE",
                      )
                    }
                  >
                    <option value="AFTER_SELECTION">
                      견적 채택 후 — 선택한 전문가 1명에게만 공개
                    </option>
                    <option value="BEFORE_QUOTE">
                      견적 제출 전 — 요청을 받은 승인 전문가에게 공개
                    </option>
                  </select>
                  <small>
                    {detailAddressDisclosureCode === "BEFORE_QUOTE"
                      ? "전문가가 실제 이동거리를 확인한 뒤 견적 제출 여부를 결정할 수 있습니다."
                      : "개인정보 보호가 우선이며, 전문가는 시·군·구를 기준으로 이동 가능 여부를 판단합니다."}
                  </small>
                </div>
                </>)}
                <div className="formField fullWidth">
                  <label>추가 설명</label>
                  <textarea
                    rows={5}
                    maxLength={20000}
                    value={description}
                    onChange={(event) => setDescription(event.target.value)}
                    placeholder="전문가가 알아야 할 내용이나 요청사항을 자유롭게 입력해 주세요."
                  />
                  <small>선택 항목에 없는 내용만 자유롭게 적어 주세요.</small>
                </div>
                {!isInteriorService && (
                  <label className="emergencyChoice fullWidth">
                    <input
                      type="checkbox"
                      checked={isUrgent}
                      onChange={(event) => setIsUrgent(event.target.checked)}
                    />
                    <span aria-hidden="true">🚨</span>
                    <strong>긴급요청</strong>
                    <small>
                      즉시 위험한 상황은 119 등 공공 긴급대응을 먼저 이용하세요.
                    </small>
                  </label>
                )}
              </div>
            </>
          )}
          {step === 2 && (
            <>
              <h2>희망 일정을 알려주세요</h2>
              {scheduleFields.length ? (
                <div className="formGrid">
                  {scheduleFields.map((field) => {
                    const dateIndex = dateTimeFields.findIndex(
                      (item) => item.id === field.id,
                    );
                    const alternative =
                      field.inputType === "DATETIME" && dateIndex > 0;
                    const heading =
                      field.inputType === "DATETIME" &&
                      dateTimeFields.length > 1
                        ? `희망일시 ${dateIndex + 1}순위${alternative ? " (선택)" : ""}`
                        : field.label;
                    return (
                      <div key={field.id} className="scheduleChoice">
                        <strong>{heading}</strong>
                        <DynamicFieldInput
                          field={{
                            ...field,
                            required: alternative ? false : field.required,
                            label: "",
                            helperText:
                              field.inputType === "DATETIME"
                                ? dateIndex === 0
                                  ? "현재 이후 · 원하는 분 단위로 선택 · 전문가와 조율하는 참고 일정"
                                  : "선택사항 · 앞선 희망일시와 다른 날짜·시간을 분 단위로 선택해 주세요."
                                : field.helperText,
                          }}
                          value={answers[field.id]}
                          onChange={(value) =>
                            setAnswers((current) => ({
                              ...current,
                              [field.id]: value,
                            }))
                          }
                        />
                      </div>
                    );
                  })}
                </div>
              ) : (
                <p className="emptyState">
                  이 서비스에는 별도 일정 질문이 없습니다. 전문가와 견적
                  단계에서 조율할 수 있습니다.
                </p>
              )}
            </>
          )}
          {step === 3 && (
            <>
              <h2>현장 사진이나 참고 파일이 있나요?</h2>
              <p>
                JPG, JPEG, PNG 이미지는 각각 5MB 이하만 올릴 수 있습니다.
                외부 검사가 연동되기 전에는 전문가 공개가 차단됩니다.
              </p>
              {fileFields.map((field) => (
                <label className="fileDrop" key={field.id}>
                  <input
                    type="file"
                    required={field.required}
                    accept=".jpg,.jpeg,.png,image/jpeg,image/png"
                    multiple
                    onChange={(event) =>
                      void upload(event.target.files, field.id)
                    }
                  />
                  <span>
                    {field.label}
                    {field.required ? " *" : ""}
                  </span>
                </label>
              ))}
              <label className="fileDrop">
                <input
                  type="file"
                  accept=".jpg,.jpeg,.png,image/jpeg,image/png"
                  multiple
                  onChange={(event) => void upload(event.target.files)}
                />
                <span>추가 사진·PDF 선택</span>
              </label>
              <div className="attachmentPreviewGrid">
                {files.map((file) => (
                  <FilePreview
                    key={file.id}
                    file={file}
                    onRemove={() => void removeFile(file)}
                  />
                ))}
              </div>
            </>
          )}
          {step === 4 && (
            <>
              <h2>요청 내용을 모두 확인하세요</h2>
              <ReviewSection title="서비스" onEdit={() => setStep(0)}>
                <p>
                  {majors.find((item) => item.id === majorId)?.name} ›{" "}
                  {middles.find((item) => item.id === middleId)?.name} ›{" "}
                  {selectedService}
                </p>
              </ReviewSection>
              <ReviewSection title={isRemoteService ? "요청 정보" : "요청 정보·지역"} onEdit={() => setStep(1)}>
                <dl className="reviewList">
                  <dt>제목</dt>
                  <dd>{title || "미입력"}</dd>
                  {isRemoteService ? <><dt>진행 방식</dt><dd>전국 온라인 진행 · 주소 입력 없음</dd></> : <>
                    <dt>지역</dt>
                    <dd>{sidos.find((item) => item.id === sidoId)?.name}{" "}{areas.find((item) => item.id === areaId)?.name}</dd>
                    <dt>상세주소</dt><dd>{detailAddress || "미입력"}</dd>
                    <dt>공개 시점</dt><dd>{detailAddressDisclosureCode === "BEFORE_QUOTE" ? "견적 제출 전 요청받은 전문가" : "견적 채택 후 선택 전문가"}</dd>
                  </>}
                  {!isInteriorService && (
                    <>
                      <dt>긴급요청</dt>
                      <dd>{isUrgent ? "예" : "아니오"}</dd>
                    </>
                  )}
                  {detailFields.map((field) => (
                    <span className="reviewPair" key={field.id}>
                      <dt>{field.label}</dt>
                      <dd>{formatFieldAnswer(field, answers[field.id])}</dd>
                    </span>
                  ))}
                  <dt>추가 설명</dt>
                  <dd>{description || "입력 없음"}</dd>
                </dl>
              </ReviewSection>
              <ReviewSection title="일정" onEdit={() => setStep(2)}>
                <dl className="reviewList">
                  {scheduleFields.map((field) => {
                    const dateIndex = dateTimeFields.findIndex(
                      (item) => item.id === field.id,
                    );
                    const label =
                      field.inputType === "DATETIME" &&
                      dateTimeFields.length > 1
                        ? `${dateIndex + 1}순위`
                        : field.label;
                    return (
                      <span className="reviewPair" key={field.id}>
                        <dt>{label}</dt>
                        <dd>{formatAnswer(answers[field.id])}</dd>
                      </span>
                    );
                  })}
                </dl>
              </ReviewSection>
              <ReviewSection
                title={`사진·문서 ${files.length}개`}
                onEdit={() => setStep(3)}
              >
                <div className="attachmentPreviewGrid">
                  {files.map((file) => (
                    <FilePreview key={file.id} file={file} />
                  ))}
                </div>
              </ReviewSection>
              <p className="privacyNote">
                공개 후 조건이 맞는 승인 전문가에게만 요청이 전달됩니다.
                {isRemoteService ? " 온라인 서비스에는 주소를 수집하지 않습니다." : " 상세주소는 고객이 선택한 공개 시점에만 제공됩니다."}
              </p>
              <button
                className="secondaryButton"
                type="button"
                disabled={busy}
                onClick={() => void saveDraft()}
              >
                임시저장
              </button>
            </>
          )}
          {step === 5 && (
            <>
              <h2>전문가에게 요청을 공개할까요?</h2>
              <p>
                공개하면 승인 상태, 서비스 분야, 활동지역, 필수 자격요건을 모두
                충족한 전문가에게 순차적으로 전달됩니다.
              </p>
              <div className="privacyNote">
                <strong>건전한 견적 이용 안내</strong>
                <br />
                동일 서비스는 24시간 2건, 전체 요청은 24시간 5건·7일 15건까지
                공개할 수 있습니다. 동시에 모집 중인 요청은 3건, 긴급출동은
                1건까지이며 같은 내용의 반복 요청은 제한됩니다. 반복 취소하거나
                도착한 견적을 계속 확인하지 않으면 일정 시간 새 요청 공개가
                제한될 수 있습니다.
              </div>
              <button
                className="primaryButton publishButton"
                type="button"
                disabled={busy}
                onClick={() => void publish()}
              >
                {busy ? "공개 중…" : "요청 공개하기"}
              </button>
            </>
          )}
        </section>
        <div className="journeyActions">
          <button
            className="secondaryButton"
            type="button"
            disabled={busy}
            onClick={() =>
              step === 0 ? navigate("/services") : setStep((value) => value - 1)
            }
          >
            이전
          </button>
          {step < 5 && (
            <button
              className="primaryButton"
              type="button"
              disabled={busy || (step === 0 && !serviceId)}
              onClick={() => void next()}
            >
              {busy ? "저장 중…" : "저장하고 다음"}
            </button>
          )}
        </div>
      </div>
    </CustomerAppLayout>
  );
}

export function CustomerRequestListPage() {
  const pageSize=20;
  const [items, setItems] = useState<ServiceRequestListItem[]>([]),
    [error, setError] = useState(""),
    [loading, setLoading] = useState(true),[page,setPage]=useState(1);
  useEffect(() => {
    requestApi
      .getMyRequests()
      .then(setItems)
      .catch((reason: unknown) =>
        setError(requestErrorMessage(reason, "요청 목록을 불러오지 못했습니다.")),
      )
      .finally(() => setLoading(false));
  }, []);
  const ordered=[...items].sort((a,b)=>Date.parse(b.createdAt)-Date.parse(a.createdAt));
  const pageCount=Math.max(1,Math.ceil(ordered.length/pageSize)),currentPage=Math.min(page,pageCount),visible=ordered.slice((currentPage-1)*pageSize,currentPage*pageSize);
  return (
    <CustomerAppLayout>
      <div className="requestJourney">
        <header className="journeyHeader soodalHeroHeading">
          <p className="eyebrow">내 요청</p>
          <h1>요청·견적</h1>
          <button
            className="primaryButton inlineButton"
            onClick={() => navigate("/customer/requests/new")}
          >
            새 요청
          </button>
        </header>
        {error && <div className="errorBanner">{error}</div>}
        {loading ? (
          <p className="emptyState">불러오는 중…</p>
        ) : items.length === 0 ? (
          <p className="emptyState">등록한 요청이 없습니다.</p>
        ) : (
          <section className="requestList">
            {visible.map((item) => (
              <button
                key={item.id}
                className="requestListItem"
                onClick={() => navigate(`/customer/requests/${item.id}`)}
              >
                <ServiceThumbnail name={serviceNameFromPath(item.categoryPath)} />
                <div>
                  <span className={`requestDomainBadge domain-${item.domain.toLowerCase()}`}>{customerRequestDomainLabel(item.domain)}</span>
                  <span className="statusBadge">{item.displayStatus}</span>
                  <h2>{item.title}</h2>
                  <p>{item.categoryPath}</p>
                </div>
                <div className="requestMeta">
                  <strong>견적 {item.quoteCount}건</strong>
                  {item.siteVisitProposalCount > 0 && <span className="siteVisitProposalNotice" role="link" tabIndex={0} onClick={event => { event.stopPropagation(); navigate(`/customer/requests/${item.id}#customer-site-visit-proposals`) }} onKeyDown={event => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); event.stopPropagation(); navigate(`/customer/requests/${item.id}#customer-site-visit-proposals`) } }}>전문가가 방문견적을 제안하였습니다. →</span>}
                  <span>{formatDate(item.createdAt)}</span>
                </div>
              </button>
            ))}
          </section>
        )}
        {items.length>pageSize&&<nav className="quotePagination" aria-label="내 요청 페이지"><button disabled={currentPage<=1} onClick={()=>setPage(value=>Math.max(1,value-1))}>이전</button><span>{currentPage} / {pageCount} · {items.length}건</span><button disabled={currentPage>=pageCount} onClick={()=>setPage(value=>Math.min(pageCount,value+1))}>다음</button></nav>}
      </div>
    </CustomerAppLayout>
  );
}

const customerRequestDomainLabel=(value:ServiceRequestListItem['domain'])=>({GENERAL:'일반 서비스',INTERIOR:'수달 인테리어',EMERGENCY:'긴급출동'}[value]);

export function CustomerRequestDetailPage({
  requestId,
}: {
  requestId: string;
}) {
  const [item, setItem] = useState<ServiceRequestDetail | null>(null),
    [error, setError] = useState(""),
    [siteVisitCount, setSiteVisitCount] = useState(0);
  const load = useCallback(
    () =>
      requestApi
        .getMyRequest(requestId)
        .then(setItem)
        .catch((reason: unknown) =>
          setError(requestErrorMessage(reason, "요청 내용을 불러오지 못했습니다.")),
        ),
    [requestId],
  );
  useEffect(() => {
    void load();
  }, [load]);
  const cancel = async () => {
    if (!item || !await soodalConfirm("이 요청을 취소할까요?", { title: "요청 취소" })) return;
    await requestApi.cancelServiceRequest(item.id, "고객 요청 취소");
    await load();
  };
  return (
    <CustomerAppLayout>
      <div className="requestJourney">
        {error ? (
          <div className="errorBanner">{error}</div>
        ) : !item ? (
          <p className="emptyState">요청을 불러오는 중…</p>
        ) : (
          <>
            <header className="journeyHeader">
              <span className="statusBadge">{item.displayStatus}</span>
              <h1>{item.title}</h1>
              <p>
                {item.categoryPath} · 견적 {item.quoteCount}건 · 방문견적 {siteVisitCount}건
              </p>
            </header>
            <section className="detailCard">
              <dl>
                <dt>등록일</dt>
                <dd>{formatDate(item.createdAt)}</dd>
                <dt>지역</dt>
                <dd>{item.administrativeAreaName ?? "미입력"}</dd>
                <dt>상세주소</dt>
                <dd>
                  {item.detailAddress || "미입력"}
                  <small className="privateTag">
                    {" "}
                    {item.detailAddressDisclosureCode === "BEFORE_QUOTE"
                      ? "견적 전 승인 전문가 공개"
                      : "채택 후 선택 전문가 공개"}
                  </small>
                </dd>
                <dt>선택 전문가</dt>
                <dd>{item.selectedProviderName ?? "아직 선택하지 않음"}</dd>
                <dt>추가 설명</dt>
                <dd>{item.description || "입력 없음"}</dd>
              </dl>
            </section>
            {item.answers.length > 0 && (
              <section className="detailCard">
                <h2>요청 현황</h2>
                <dl>
                  {orderRequestAnswers(item.answers).map((answer) => (
                    <div key={answer.fieldId} className="answerRow">
                      <dt>{formatRequestAnswerLabel(item.answers, answer)}</dt>
                      <dd>
                        {formatCustomerRequestAnswer(answer)}
                      </dd>
                    </div>
                  ))}
                </dl>
              </section>
            )}
            {item.files.length > 0 && (
              <section className="detailCard">
                <h2>첨부파일</h2>
                <div className="authenticatedFilePreviewGrid">
                  {item.files.map((file) => (
                    <FilePreview key={file.id} file={file} />
                  ))}
                </div>
              </section>
            )}
            <CustomerQuotesPanel requestId={requestId} onSiteVisitCountChange={setSiteVisitCount} />
            <div className="formActions">
              <button
                className="secondaryButton"
                onClick={() => navigate("/customer/requests")}
              >
                목록
              </button>
              {item.canEdit && (
                <button
                  className="primaryButton"
                  onClick={() =>
                    navigate(`/customer/requests/new?draft=${item.id}`)
                  }
                >
                  이어서 작성
                </button>
              )}
              {item.canCancel && (
                <button className="dangerButton" onClick={() => void cancel()}>
                  요청 취소
                </button>
              )}
            </div>
          </>
        )}
      </div>
    </CustomerAppLayout>
  );
}

function orderRequestAnswers(answers: ServiceRequestDetail["answers"]) {
  const dateTimes = answers.filter(answer => answer.inputType.toUpperCase() === "DATETIME");
  if (dateTimes.length < 2) return answers;
  const firstDateIndex = answers.findIndex(answer => answer.inputType.toUpperCase() === "DATETIME");
  const ordered = answers.filter(answer => answer.inputType.toUpperCase() !== "DATETIME");
  ordered.splice(firstDateIndex, 0, ...dateTimes);
  return ordered;
}

function formatCustomerRequestAnswer(answer: ServiceRequestDetail["answers"][number]) {
  if (/희망\s*(비용|예산)/.test(answer.label) && isZeroRequestAnswer(answer.value)) return "견적상담 후 결정";
  return formatRequestAnswer(answer.value, answer.inputType);
}

function SelectField({
  label,
  value,
  items,
  onChange,
  disabled = false,
}: {
  label: string;
  value: string;
  items: Category[];
  onChange: (id: string) => void;
  disabled?: boolean;
}) {
  return (
    <div className="formField">
      <label>{label} *</label>
      <select
        disabled={disabled}
        value={value}
        onChange={(event) => void onChange(event.target.value)}
      >
        <option value="">선택하세요</option>
        {items.map((item) => (
          <option key={item.id} value={item.id}>
            {item.name}
          </option>
        ))}
      </select>
    </div>
  );
}
function naturalRequestTitle(name: string) {
  const serviceName = name.trim() || "서비스";
  if (name.includes("세면대") && name.includes("막힘"))
    return "세면대가 막혔어요";
  if (name.includes("변기") && name.includes("막힘")) return "변기가 막혔어요";
  if (name.includes("변기")) return "변기 점검이 필요해요";
  if (name.includes("누수")) return "누수가 발생했어요";
  if (name.includes("에어컨") && name.includes("청소"))
    return "에어컨 청소가 필요해요";
  if (name.includes("보일러")) return "보일러 점검이 필요해요";
  return `${serviceName}${hasKoreanFinalConsonant(serviceName) ? "이" : "가"} 필요해요`;
}
function hasKoreanFinalConsonant(value: string) {
  const lastHangul = [...value.normalize("NFC")]
    .reverse()
    .find((character) => character >= "가" && character <= "힣");
  if (!lastHangul) return false;
  return (lastHangul.charCodeAt(0) - 0xac00) % 28 !== 0;
}
function isRetiredStructuralDuplicate(field: RequestField) {
  const label = field.label.replace(/[^\p{L}\p{N}]/gu, "");
  return (
    ["service_address", "request_address"].includes(
      field.fieldKey.toLowerCase(),
    ) || label === "서비스주소"
  );
}
function normalizedFieldLabel(field: RequestField) {
  return field.label.replace(/[^\p{L}\p{N}]/gu, "");
}
function isBudgetDuplicate(field: RequestField, fields: RequestField[]) {
  return normalizedFieldLabel(field) === "희망예산" &&
    fields.some(candidate => normalizedFieldLabel(candidate) === "희망비용");
}
type GuidedFieldKind =
  | "CONTENT"
  | "BUDGET"
  | "SYMPTOM"
  | "QUANTITY"
  | "SPACE"
  | "CONDITION"
  | "OTHER";
function guidedFieldKind(field: RequestField): GuidedFieldKind {
  const key = field.fieldKey.toLowerCase();
  const label = field.label.replace(/[^\p{L}\p{N}]/gu, "");
  if (
    [
      "request_detail",
      "request_content",
      "task_detail",
      "support_detail",
      "pet_service_detail",
      "vehicle_request_detail",
      "event_request_detail",
      "care_request_detail",
      "project_requirements",
    ].includes(key) ||
    ["요청내용", "필요한심부름내용", "필요한도움내용"].includes(label)
  )
    return "CONTENT";
  if (["desired_cost", "desired_price"].includes(key) || label === "희망비용")
    return "BUDGET";
  if (
    ["issue", "symptom_detail", "request_symptom_detail"].includes(key) ||
    label.includes("증상")
  )
    return "SYMPTOM";
  if (
    [
      "quantity",
      "quantity_scale",
      "target_quantity",
      "item_or_task_count",
      "participant_count",
      "guest_or_item_count",
    ].includes(key) ||
    /(수량|개수|대수|인원|건수|마릿수)/u.test(label)
  )
    return "QUANTITY";
  if (key === "space_type" || label.includes("공간유형")) return "SPACE";
  if (
    ["site_condition", "site_state", "contamination", "vehicle_condition"].includes(
      key,
    ) ||
    /(현장조건|현장상태|오염관리상태|차량상태)/u.test(label)
  )
    return "CONDITION";
  return "OTHER";
}
function serviceRequestContent(name: string) {
  if (name.includes("세면대") && name.includes("막힘"))
    return "세면대 물이 원활하게 내려가지 않습니다. 막힘 원인을 확인하고 배수 상태를 정상화해 주세요.";
  if (name.includes("변기") && name.includes("막힘"))
    return "변기 물이 원활하게 내려가지 않습니다. 막힘 원인을 확인하고 정상적으로 사용할 수 있게 수리해 주세요.";
  if (name.includes("누수"))
    return "물이 새는 위치와 원인을 확인하고 필요한 보수 범위와 예상 비용을 안내해 주세요.";
  if (name.includes("에어컨") && name.includes("청소"))
    return "에어컨 내부 오염 상태를 확인하고 필터와 열교환기 등 필요한 부분을 청소해 주세요.";
  return `${name} 서비스가 필요합니다. 현재 상태를 확인하고 필요한 작업과 예상 비용을 안내해 주세요.`;
}
function serviceSymptoms(name: string) {
  if (name.includes("세면대") && name.includes("막힘"))
    return [
      "물이 천천히 내려가요",
      "물이 전혀 내려가지 않아요",
      "물이 역류해요",
      "악취가 나요",
      "누수도 함께 있어요",
    ];
  if (name.includes("변기"))
    return [
      "물이 천천히 내려가요",
      "물이 전혀 내려가지 않아요",
      "물이 역류해요",
      "물이 계속 흘러요",
      "악취가 나요",
    ];
  if (name.includes("누수"))
    return [
      "물이 조금씩 새요",
      "물이 계속 흘러요",
      "벽·천장이 젖었어요",
      "바닥에 물이 고여요",
      "누수 위치를 모르겠어요",
    ];
  return [
    `${name} 기능이 작동하지 않아요`,
    `${name} 상태가 평소와 달라요`,
    "소음이나 냄새가 발생해요",
    "정확한 증상을 모르겠어요",
  ];
}
function quantityUnit(field: RequestField, name: string) {
  const label = normalizedFieldLabel(field);
  if (/(인원|참석자)/u.test(label)) return "명";
  if (/(반려동물|마릿수)/u.test(label)) return "마리";
  if (/(업무|건수|품목)/u.test(label)) return "건";
  if (
    /(에어컨|세탁기|냉장고|건조기|보일러|차량|자동차|실외기)/u.test(
      `${name}${label}`,
    )
  )
    return "대";
  if (/(방|공간)/u.test(`${name}${label}`)) return "곳";
  return "개";
}
function quantityOptions(field: RequestField, name: string, numeric = false) {
  if (numeric) return ["1", "2", "3", "4"];
  const unit = quantityUnit(field, name);
  return [
    `1${unit}`,
    `2${unit}`,
    `3${unit}`,
    `4${unit} 이상`,
    "수량을 잘 모르겠음",
  ];
}
const conditionOptions = [
  "좋음 - 작업 공간과 접근이 충분함",
  "보통 - 일반적인 작업 환경",
  "나쁨 - 공간이 좁거나 접근이 어려움",
  "아주 나쁨 - 심한 오염·장애물 등 추가 작업 예상",
  "잘 모르겠음",
];
const contaminationOptions = [
  "보통 - 일반적인 오염 상태",
  "심함 - 찌든 때·곰팡이 등 집중 작업 필요",
  "특수 오염 있음 - 별도 상담 필요",
  "잘 모르겠음",
];
const vehicleConditionOptions = [
  "시동과 주행 모두 가능",
  "시동은 가능하지만 주행 어려움",
  "시동 불가",
  "사고·파손 상태",
  "잘 모르겠음",
];
const generalSpaceOptions = [
  "아파트·공동주택",
  "단독·다가구주택",
  "상가·매장",
  "사무실",
  "공장·창고",
  "공용공간",
  "야외",
  "기타",
  "잘 모름",
];
function fieldOptions(
  field: RequestField,
  service: PublicServiceDetail | null,
  kind: GuidedFieldKind,
) {
  if (field.options.length > 0) return field.options;
  const serviceName = service?.name ?? "";
  if (kind === "SYMPTOM") return serviceSymptoms(serviceName);
  if (kind === "QUANTITY")
    return quantityOptions(
      field,
      serviceName,
      ["NUMBER", "MONEY"].includes(field.inputType),
    );
  if (kind === "SPACE")
    return service?.majorName === "인테리어"
      ? interiorSpaceOptions
      : generalSpaceOptions;
  if (kind === "CONDITION") {
    const key = field.fieldKey.toLowerCase();
    if (key === "contamination") return contaminationOptions;
    if (key === "vehicle_condition") return vehicleConditionOptions;
    if (service?.majorName === "인테리어" && key === "site_state")
      return interiorSiteOptions;
    return conditionOptions;
  }
  return [];
}
function standardAmount(service: PublicServiceDetail | null) {
  return (
    service?.price?.referenceAmount ??
    service?.price?.recommendedMinAmount ??
    service?.price?.recommendedMaxAmount ??
    null
  );
}
function referencePriceLabel(service: PublicServiceDetail | null) {
  const minimum = service?.price?.recommendedMinAmount;
  const maximum = service?.price?.recommendedMaxAmount;
  if (minimum !== null && minimum !== undefined && maximum !== null && maximum !== undefined)
    return `참고 가격 범위 · ${minimum.toLocaleString("ko-KR")}~${maximum.toLocaleString("ko-KR")}원`;
  const amount = standardAmount(service);
  return amount === null ? null : `기본 작업 참고가 · ${amount.toLocaleString("ko-KR")}원`;
}
function applyServiceDefaults(
  service: PublicServiceDetail,
  fields: RequestField[],
  setTitle: (value: string) => void,
  setAnswers: (value: Record<string, DynamicValue>) => void,
) {
  const defaults: Record<string, DynamicValue> = {};
  for (const field of fields) {
    const kind = guidedFieldKind(field);
    if (kind === "CONTENT")
      defaults[field.id] = serviceRequestContent(service.name);
   if (kind === "BUDGET") {
      defaults[field.id] = 0;
    }
    if (
      kind === "SYMPTOM" ||
      kind === "QUANTITY" ||
      kind === "SPACE" ||
      kind === "CONDITION"
    ) {
      const options = fieldOptions(field, service, kind);
      if (options.length > 0) defaults[field.id] = options[0];
    }
  }
  setTitle(naturalRequestTitle(service.name));
  setAnswers(defaults);
}
const interiorSpaceOptions = [
  "아파트·공동주택",
  "단독·다가구주택",
  "상가·매장",
  "사무실",
  "공장·창고",
  "공용공간",
  "기타",
  "잘 모름",
];
const interiorSiteOptions = [
  "공실·입주 전",
  "거주 중",
  "영업 중",
  "공사·철거 중",
  "신축 현장",
  "상태가 좋음",
  "보수가 필요한 상태",
  "상태가 매우 나쁨",
  "잘 모름",
];
function GuidedRequestField({
  field,
  value,
  service,
  interior,
  onChange,
}: {
  field: RequestField;
  value: DynamicValue | undefined;
  service: PublicServiceDetail | null;
  interior?: boolean;
  onChange: (value: DynamicValue) => void;
}) {
  const normalizedLabel = normalizedFieldLabel(field);
  if (interior && normalizedLabel === "공간유형")
    return (
      <GuidedSelectField
        field={field}
        value={value}
        options={interiorSpaceOptions}
        onChange={onChange}
        help="서비스를 받을 공간과 가장 가까운 유형을 선택해 주세요."
      />
    );
  if (interior && normalizedLabel === "현장상태")
    return (
      <GuidedSelectField
        field={field}
        value={value}
        options={interiorSiteOptions}
        onChange={onChange}
        help="현재 이용 상태와 현장 상태에 가장 가까운 항목을 선택해 주세요."
      />
    );
  if (interior && normalizedLabel === "공사면적")
    return (
      <InteriorAreaField field={field} value={value} onChange={onChange} />
    );
  const kind = guidedFieldKind(field);
  if (kind === "OTHER")
    return (
      <DynamicFieldInput field={field} value={value} onChange={onChange} />
    );
  if (kind === "CONTENT")
    return (
      <div className="formField dynamicField fullWidth">
        <label>
          {field.label}
          {field.required && <span className="requiredMark"> *</span>}
        </label>
        <textarea
          rows={5}
          required={field.required}
          value={String(value ?? "")}
          onChange={(event) => onChange(event.target.value)}
        />
        <small>
          하위 서비스 기준 추천 문구입니다. 현장 상황에 맞게 수정해 주세요.
        </small>
      </div>
    );
  if (kind === "BUDGET") {
    const priceGuide = referencePriceLabel(service);
    const consultation =
      value !== undefined &&
      value !== null &&
      value !== "" &&
      Number(value) === 0;
    return (
      <div className="formField dynamicField budgetField">
        <div className="budgetFieldHeading">
          <label>
            {field.label}
            {field.required && <span className="requiredMark"> *</span>}
          </label>
          <label className="budgetConsultation">
            <input
              type="checkbox"
              checked={consultation}
              onChange={(event) =>
                event.target.checked ? onChange(0) : onChange("")
              }
            />
            견적상담 후 결정
          </label>
        </div>
        <input
          type="text"
          inputMode="numeric"
          required={field.required && !consultation}
          disabled={consultation}
          value={consultation || value === undefined || value === "" ? "" : Number(String(value).replace(/[^0-9]/g, "")).toLocaleString("ko-KR")}
          onChange={(event) => onChange(event.target.value.replace(/[^0-9]/g, ""))}
        />
        {priceGuide !== null ? (
          <>
            <div className="budgetPresets" aria-label="기본 작업 참고 가격">
              <span>{priceGuide}</span>
            </div>
            <small>
              {consultation
                ? "전문가별 작업 범위와 포함 비용을 비교한 뒤 결정합니다."
                : "직접 입력한 희망비용은 확정 가격이 아닙니다. 자재·수량·현장 상태에 따라 실제 견적이 달라질 수 있습니다."}
            </small>
          </>
        ) : (
          <small>
              {consultation
                ? "전문가별 작업 범위와 포함 비용을 비교한 뒤 결정합니다."
                : "참고 가격 자료가 부족한 서비스입니다. 희망비용을 입력하거나 견적상담 후 결정해 주세요."}
          </small>
        )}
      </div>
    );
  }
  const options = fieldOptions(field, service, kind);
  return (
    <div className="formField dynamicField">
      <label>
        {field.label}
        {field.required && <span className="requiredMark"> *</span>}
      </label>
      <select
        required={field.required}
        value={String(value ?? "")}
        onChange={(event) => onChange(event.target.value)}
      >
        <option value="">선택하세요</option>
        {options.map((option) => (
          <option key={option} value={option}>
            {kind === "QUANTITY" &&
            ["NUMBER", "MONEY"].includes(field.inputType)
              ? `${option}${quantityUnit(field, service?.name ?? "")}`
              : option}
          </option>
        ))}
      </select>
      <small>하위 서비스에 맞는 항목을 선택해 주세요.</small>
    </div>
  );
}
function GuidedSelectField({
  field,
  value,
  options,
  help,
  onChange,
}: {
  field: RequestField;
  value: DynamicValue | undefined;
  options: string[];
  help: string;
  onChange: (value: DynamicValue) => void;
}) {
  return (
    <div className="formField dynamicField">
      <label>
        {field.label}
        {field.required && <span className="requiredMark"> *</span>}
      </label>
      <select
        required={field.required}
        value={String(value ?? "")}
        onChange={(event) => onChange(event.target.value)}
      >
        <option value="">선택하세요</option>
        {options.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </select>
      <small>{help}</small>
    </div>
  );
}
function InteriorAreaField({
  field,
  value,
  onChange,
}: {
  field: RequestField;
  value: DynamicValue | undefined;
  onChange: (value: DynamicValue) => void;
}) {
  const [width, setWidth] = useState("");
  const [height, setHeight] = useState("");
  const unknown =
    value !== undefined &&
    value !== null &&
    value !== "" &&
    Number(value) === 0;
  const updateArea = (nextWidth: string, nextHeight: string) => {
    const widthValue = Number(nextWidth);
    const heightValue = Number(nextHeight);
    onChange(
      widthValue > 0 && heightValue > 0
        ? Math.round(widthValue * heightValue * 100) / 100
        : "",
    );
  };
  return (
    <div className="formField dynamicField interiorAreaField">
      <label>
        {field.label}
        {field.required && <span className="requiredMark"> *</span>}
      </label>
      <div className="interiorAreaInputs">
        <label>
          <span>가로</span>
          <input
            type="number"
            min="0.1"
            step="0.1"
            disabled={unknown}
            value={width}
            onChange={(event) => {
              setWidth(event.target.value);
              updateArea(event.target.value, height);
            }}
          />
          <small>미터(m)</small>
        </label>
        <span aria-hidden="true">×</span>
        <label>
          <span>세로</span>
          <input
            type="number"
            min="0.1"
            step="0.1"
            disabled={unknown}
            value={height}
            onChange={(event) => {
              setHeight(event.target.value);
              updateArea(width, event.target.value);
            }}
          />
          <small>미터(m)</small>
        </label>
      </div>
      <label className="interiorAreaUnknown">
        <input
          type="checkbox"
          checked={unknown}
          onChange={(event) => {
            if (event.target.checked) {
              setWidth("");
              setHeight("");
              onChange(0);
            } else onChange("");
          }}
        />{" "}
        면적을 잘 모름
      </label>
      <small>
        {unknown
          ? "면적을 잘 모름으로 저장합니다."
          : value
            ? `계산된 공사 면적: ${Number(value).toLocaleString()}㎡`
            : "가로와 세로 길이를 미터 단위로 입력하면 면적을 자동 계산합니다."}
      </small>
    </div>
  );
}
function FilePreview({
  file,
  onRemove,
}: {
  file: RequestFile;
  onRemove?: () => void;
}) {
  return (
    <AuthenticatedFilePreview
      file={file}
      statusText={`악성코드 ${file.malwareScanStatus} · 개인정보 ${file.privacyInspectionStatus} · 전문가 공개 ${file.providerVisibilityStatus}`}
      onRemove={onRemove}
    />
  );
}
function ReviewSection({
  title,
  onEdit,
  children,
}: PropsWithChildren<{ title: string; onEdit: () => void }>) {
  return (
    <section className="reviewSection">
      <header>
        <h3>{title}</h3>
        <button type="button" onClick={onEdit}>
          수정
        </button>
      </header>
      {children}
    </section>
  );
}
function isEmptyValue(value: DynamicValue | undefined) {
  return (
    value === undefined ||
    value === "" ||
    (Array.isArray(value) && value.length === 0) ||
    (typeof value === "object" &&
      !Array.isArray(value) &&
      "value" in value &&
      !String(value.value ?? "").trim())
  );
}
function isFutureDateTime(value: DynamicValue | undefined) {
  if (isEmptyValue(value)) return true;
  const parsed = new Date(String(value));
  return !Number.isNaN(parsed.getTime()) && parsed.getTime() > Date.now();
}
function isEffectivelyRequiredScheduleField(
  field: RequestField,
  dateTimeFields: RequestField[],
) {
  return (
    field.required &&
    (field.inputType !== "DATETIME" ||
      dateTimeFields.findIndex((item) => item.id === field.id) <= 0)
  );
}
function toLocalDateTimeValue(value: unknown) {
  const parsed = new Date(String(value));
  if (Number.isNaN(parsed.getTime())) return String(value ?? "");
  const local = new Date(
    parsed.getTime() - parsed.getTimezoneOffset() * 60_000,
  );
  return local.toISOString().slice(0, 16);
}
function requestErrorMessage(reason: unknown, fallback: string) {
  if (reason instanceof requestApi.RequestApiError) {
    const details = Object.values(reason.fieldErrors ?? {})
      .flat()
      .filter(Boolean);
    const message = details.length > 0
      ? `${reason.message} ${details.join(" ")}`
      : reason.message;
    return reason.traceId
      ? `${message} 오류 확인번호: ${reason.traceId}`
      : message;
  }
  if (reason instanceof requestApi.RequestNetworkError) {
    return navigator.onLine
      ? `서버 연결이 차단되었습니다. 입력 내용은 화면에 유지되어 있습니다. 진단: 페이지 ${window.location.origin}, 요청 ${reason.path}, 브라우저 ${reason.causeMessage || "TypeError"}`
      : "인터넷 연결을 확인해 주세요. 입력 내용은 화면에 유지되어 있습니다.";
  }
  if (isNetworkRequestError(reason)) {
    return navigator.onLine
      ? "서버와 연결하지 못했습니다. 입력 내용은 화면에 유지되어 있습니다. 잠시 후 다시 시도해 주세요."
      : "인터넷 연결을 확인해 주세요. 입력 내용은 화면에 유지되어 있습니다.";
  }
  return reason instanceof Error ? reason.message : fallback;
}
function isNetworkRequestError(reason: unknown) {
  return (
    reason instanceof TypeError ||
    (reason instanceof Error && /failed to fetch|networkerror|load failed/i.test(reason.message))
  );
}
function formatDate(value: string) {
  return new Intl.DateTimeFormat("ko-KR", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}
function formatAnswer(value: unknown) {
  return formatRequestAnswer(value);
}
function formatFieldAnswer(field: RequestField, value: unknown) {
  if (guidedFieldKind(field) === "BUDGET" && Number(value) === 0)
    return "견적상담 후 결정";
  return normalizedFieldLabel(field) === "공사면적"
    ? Number(value) === 0
      ? "잘 모름"
      : `${formatAnswer(value)}㎡`
    : formatAnswer(value);
}
