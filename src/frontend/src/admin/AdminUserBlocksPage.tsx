import { useEffect, useState } from 'react'
import { AdminLayout } from './AdminLayout'

type Block = { id: string; customerId: string; customerName: string; providerId: string; providerName: string; direction: string; status: string; reasonCode: string | null; createdAt: string; releasedAt: string | null }
const date = (value: string | null) => value ? new Intl.DateTimeFormat('ko-KR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : '-'

export function AdminUserBlocksPage({ pathname }: { pathname: string }) {
  const [items, setItems] = useState<Block[]>([]); const [status, setStatus] = useState('ACTIVE'); const [error, setError] = useState('')
  useEffect(() => { fetch(`/api/v1/admin/user-blocks${status ? `?status=${status}` : ''}`, { credentials: 'include' }).then(async response => { if (!response.ok) throw new Error((await response.json() as { message?: string }).message ?? '차단 내역을 불러오지 못했습니다.'); return response.json() as Promise<Block[]> }).then(setItems).catch(reason => setError(reason instanceof Error ? reason.message : '차단 내역을 불러오지 못했습니다.')) }, [status])
  return <AdminLayout pathname={pathname}><section className="adminPageHeader"><div><p>USER RELATIONSHIP BLOCKS</p><h1>사용자 차단 조회</h1><span>고객→공급자 차단 상태를 읽기 전용으로 확인합니다. 관리자 강제해제는 정책 미확정입니다.</span></div></section><div className="adminToolbar"><label>상태<select value={status} onChange={event => setStatus(event.target.value)}><option value="">전체</option><option value="ACTIVE">차단 중</option><option value="RELEASED">해제됨</option></select></label></div>{error && <div className="adminError">{error}</div>}<div className="adminTableWrap"><table><thead><tr><th>고객</th><th>공급자</th><th>방향</th><th>상태</th><th>사유코드</th><th>생성</th><th>해제</th></tr></thead><tbody>{items.map(item => <tr key={item.id}><td>{item.customerName}</td><td>{item.providerName}</td><td>고객 → 공급자</td><td>{item.status}</td><td>{item.reasonCode ?? '미입력'}</td><td>{date(item.createdAt)}</td><td>{date(item.releasedAt)}</td></tr>)}</tbody></table>{items.length === 0 && <p className="adminEmpty">조회된 차단 내역이 없습니다.</p>}</div></AdminLayout>
}
