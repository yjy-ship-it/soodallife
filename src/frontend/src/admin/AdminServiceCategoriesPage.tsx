import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { AdminLayout } from './AdminLayout'
import {
  getCategorySummary,
  getMajorCategories,
  getMiddleCategories,
  getServiceCategory,
  searchServiceCategories,
  updateServiceCategory,
} from './serviceCategoryApi'
import type {
  AdminCategoryOption,
  AdminCategorySummary,
  AdminServiceCategoryDetail,
  AdminServiceCategoryListItem,
  ServiceCategoryStatus,
} from './serviceCategoryTypes'

const statusLabels: Record<ServiceCategoryStatus, string> = {
  ACTIVE: '운영중',
  PAUSED: '일시중지',
  REVIEW: '검토중',
}

const detailTabs = ['기본정보', '고객 요청항목', '가격정책', '수수료', '공급자 요건', '운영정책'] as const

export function AdminServiceCategoriesPage({ pathname }: { pathname: string }) {
  const [summary, setSummary] = useState<AdminCategorySummary | null>(null)
  const [majors, setMajors] = useState<AdminCategoryOption[]>([])
  const [middles, setMiddles] = useState<AdminCategoryOption[]>([])
  const [services, setServices] = useState<AdminServiceCategoryListItem[]>([])
  const [resultCount, setResultCount] = useState(0)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [majorId, setMajorId] = useState('')
  const [middleId, setMiddleId] = useState('')
  const [status, setStatus] = useState('')
  const [selected, setSelected] = useState<AdminServiceCategoryDetail | null>(null)
  const [name, setName] = useState('')
  const [editStatus, setEditStatus] = useState<ServiceCategoryStatus>('ACTIVE')
  const [sortOrder, setSortOrder] = useState(0)
  const [activeTab, setActiveTab] = useState<(typeof detailTabs)[number]>('기본정보')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  useEffect(() => {
    Promise.all([getCategorySummary(), getMajorCategories()])
      .then(([summaryResult, majorResult]) => {
        setSummary(summaryResult)
        setMajors(majorResult)
      })
      .catch((requestError: unknown) => setError(requestError instanceof Error ? requestError.message : '서비스 분류를 불러오지 못했습니다.'))
  }, [])

  useEffect(() => {
    if (!majorId) {
      setMiddles([])
      setMiddleId('')
      return
    }
    getMiddleCategories(majorId)
      .then(setMiddles)
      .catch((requestError: unknown) => setError(requestError instanceof Error ? requestError.message : '중분류를 불러오지 못했습니다.'))
  }, [majorId])

  useEffect(() => {
    setLoading(true)
    setError(null)
    searchServiceCategories({ search, majorId, middleId, status })
      .then((result) => {
        setServices(result.items)
        setResultCount(result.totalCount)
      })
      .catch((requestError: unknown) => setError(requestError instanceof Error ? requestError.message : '서비스 목록을 불러오지 못했습니다.'))
      .finally(() => setLoading(false))
  }, [search, majorId, middleId, status])

  const selectedMajor = useMemo(() => majors.find((major) => major.id === majorId), [majorId, majors])
  const selectedMiddle = useMemo(() => middles.find((middle) => middle.id === middleId), [middleId, middles])

  const selectService = async (serviceId: string) => {
    setError(null)
    setNotice(null)
    try {
      const detail = await getServiceCategory(serviceId)
      setSelected(detail)
      setName(detail.name)
      setEditStatus(detail.statusCode)
      setSortOrder(detail.sortOrder)
      setActiveTab('기본정보')
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : '서비스 상세정보를 불러오지 못했습니다.')
    }
  }

  const applySearch = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setSearch(searchInput.trim())
  }

  const resetFilters = () => {
    setSearchInput('')
    setSearch('')
    setMajorId('')
    setMiddleId('')
    setStatus('')
  }

  const saveService = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!selected) return
    if (selected.statusCode === 'ACTIVE' && editStatus === 'PAUSED') {
      const confirmed = window.confirm('운영상태만 일시중지로 변경합니다. 기존 요청·거래 데이터는 삭제하지 않습니다. 계속할까요?')
      if (!confirmed) return
    }

    setSaving(true)
    setError(null)
    setNotice(null)
    try {
      const updated = await updateServiceCategory(selected.id, { name: name.trim(), statusCode: editStatus, sortOrder })
      setSelected(updated)
      setName(updated.name)
      setEditStatus(updated.statusCode)
      setSortOrder(updated.sortOrder)
      setServices((current) => current
        .map((item) => item.id === updated.id ? { ...item, ...updated } : item)
        .sort((left, right) => left.sortOrder - right.sortOrder || left.name.localeCompare(right.name, 'ko-KR')))
      setSummary(await getCategorySummary())
      setNotice('서비스 기본정보를 저장했습니다.')
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : '서비스 정보를 저장하지 못했습니다.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <AdminLayout pathname={pathname}>
      <section className="adminPageHeading serviceAdminHeading">
        <div><p>서비스 분류 운영</p><h1>서비스 관리</h1></div>
        <span>고객 요청과 공급자 매칭에 사용되는 서비스 분류와 운영상태를 관리합니다.</span>
      </section>

      <section className="categorySummaryGrid" aria-label="서비스 분류 요약">
        {[
          ['대분류', summary?.majorCount, '개'],
          ['중분류', summary?.middleCount, '개'],
          ['하위 서비스', summary?.serviceCount, '개'],
          ['운영중 서비스', summary?.activeServiceCount, '개'],
        ].map(([label, value, unit]) => (
          <article key={label as string}><span>{label}</span><strong>{value ?? '—'}<small>{value === undefined ? '' : unit}</small></strong></article>
        ))}
      </section>

      <form className="categoryFilters" onSubmit={applySearch}>
        <div className="categorySearchField">
          <label htmlFor="category-search">서비스 검색</label>
          <div><input id="category-search" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} placeholder="카테고리명, 서비스명 또는 코드" /><button type="submit">검색</button></div>
        </div>
        <label>대분류<select value={majorId} onChange={(event) => { setMajorId(event.target.value); setMiddleId('') }}><option value="">전체 대분류</option>{majors.map((major) => <option key={major.id} value={major.id}>{major.name}</option>)}</select></label>
        <label>중분류<select value={middleId} disabled={!majorId} onChange={(event) => setMiddleId(event.target.value)}><option value="">{majorId ? '전체 중분류' : '대분류를 먼저 선택'}</option>{middles.map((middle) => <option key={middle.id} value={middle.id}>{middle.name}</option>)}</select></label>
        <label>운영상태<select value={status} onChange={(event) => setStatus(event.target.value)}><option value="">전체 상태</option><option value="ACTIVE">운영중</option><option value="PAUSED">일시중지</option><option value="REVIEW">검토중</option></select></label>
        <button className="categoryReset" type="button" onClick={resetFilters}>검색조건 초기화</button>
      </form>

      {(error || notice) && <div className={error ? 'adminError' : 'categorySuccess'} role="status">{error ?? notice}</div>}

      <section className="categoryWorkspace">
        <div className="categoryExplorer">
          <header className="categoryExplorerHeader">
            <div><strong>{resultCount.toLocaleString('ko-KR')}건</strong><span>의 하위 서비스</span></div>
            <p>{selectedMajor?.name ?? '전체 대분류'} <b>›</b> {selectedMiddle?.name ?? '전체 중분류'}</p>
          </header>
          <div className="categoryColumns">
            <section aria-label="대분류 목록">
              <h2>대분류 <small>{majors.length}</small></h2>
              <div className="categoryColumnList">
                <button className={!majorId ? 'selected' : ''} type="button" onClick={() => { setMajorId(''); setMiddleId('') }}>전체 대분류</button>
                {majors.map((major) => <button className={majorId === major.id ? 'selected' : ''} key={major.id} type="button" onClick={() => { setMajorId(major.id); setMiddleId('') }}><span>{major.name}</span><small>{statusLabels[major.statusCode]}</small></button>)}
              </div>
            </section>
            <section aria-label="중분류 목록">
              <h2>중분류 <small>{middles.length}</small></h2>
              <div className="categoryColumnList">
                {!majorId ? <p className="categoryColumnEmpty">대분류를 선택해 주세요.</p> : <>
                  <button className={!middleId ? 'selected' : ''} type="button" onClick={() => setMiddleId('')}>전체 중분류</button>
                  {middles.map((middle) => <button className={middleId === middle.id ? 'selected' : ''} key={middle.id} type="button" onClick={() => setMiddleId(middle.id)}><span>{middle.name}</span><small>{statusLabels[middle.statusCode]}</small></button>)}
                </>}
              </div>
            </section>
            <section className="serviceResultColumn" aria-label="하위 서비스 목록">
              <h2>하위 서비스 <small>{resultCount}</small></h2>
              <div className="categoryColumnList serviceResultList">
                {loading ? <p className="categoryColumnEmpty">서비스를 불러오는 중입니다…</p> : services.length === 0 ? <p className="categoryColumnEmpty">조건에 맞는 서비스가 없습니다.</p> : services.map((service) => (
                  <button className={selected?.id === service.id ? 'selected serviceSelected' : ''} key={service.id} type="button" onClick={() => selectService(service.id)}>
                    <span><strong>{service.name}</strong><small>{service.majorName} › {service.middleName}</small></span>
                    <em className={`categoryStatus ${service.statusCode.toLowerCase()}`}>{statusLabels[service.statusCode]}</em>
                  </button>
                ))}
              </div>
            </section>
          </div>
        </div>

        <aside className="categoryDetail" aria-label="서비스 상세정보">
          {!selected ? <div className="categoryDetailEmpty"><span>서비스 선택</span><h2>관리할 서비스를 선택해 주세요.</h2><p>왼쪽 계층 목록에서 하위 서비스를 선택하면 기본정보가 표시됩니다.</p></div> : <>
            <header><div><span>{selected.majorName} › {selected.middleName}</span><h2>{selected.name}</h2></div><em className={`categoryStatus ${selected.statusCode.toLowerCase()}`}>{statusLabels[selected.statusCode]}</em></header>
            <div className="categoryTabs" role="tablist" aria-label="서비스 관리 항목">
              {detailTabs.map((tab) => <button className={activeTab === tab ? 'active' : ''} key={tab} type="button" role="tab" aria-selected={activeTab === tab} onClick={() => setActiveTab(tab)}>{tab}</button>)}
            </div>
            {activeTab !== '기본정보' ? <div className="categoryFutureTab"><strong>{activeTab}</strong><p>다음 개발 단계에서 제공됩니다.</p></div> : (
              <form className="categoryEditForm" onSubmit={saveService}>
                <div className="categoryReadOnlyRow"><span>대분류</span><strong>{selected.majorName}</strong></div>
                <div className="categoryReadOnlyRow"><span>중분류</span><strong>{selected.middleName}</strong></div>
                <label>서비스명<input required maxLength={200} value={name} onChange={(event) => setName(event.target.value)} /></label>
                <div className="categoryReadOnlyRow"><span>서비스 코드</span><strong>{selected.externalCode ?? '등록된 코드 없음'}</strong></div>
                <label>운영상태<select value={editStatus} onChange={(event) => setEditStatus(event.target.value as ServiceCategoryStatus)}><option value="ACTIVE">운영중</option><option value="PAUSED">일시중지</option><option value="REVIEW">검토중</option></select><small>일시중지로 변경해도 기존 요청과 거래 데이터는 삭제되지 않습니다.</small></label>
                <label>노출순서<input type="number" min="0" value={sortOrder} onChange={(event) => setSortOrder(Number(event.target.value))} /></label>
                <div className="categoryFormNotice">원본 추적정보와 상위 분류 관계는 이 화면에서 변경할 수 없습니다.</div>
                <button className="categorySaveButton" type="submit" disabled={saving || !name.trim()}>{saving ? '저장 중…' : '변경사항 저장'}</button>
              </form>
            )}
          </>}
        </aside>
      </section>
    </AdminLayout>
  )
}
