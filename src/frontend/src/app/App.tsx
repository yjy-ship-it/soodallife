import './App.css'

const roleAreas = [
  { name: '고객', path: 'features/customer' },
  { name: 'Provider', path: 'features/provider' },
  { name: '관리자', path: 'features/admin' },
]

function App() {
  return (
    <main className="shell">
      <section className="card" aria-labelledby="page-title">
        <p className="eyebrow">SOODAL LIFE</p>
        <h1 id="page-title">수달 라이프 개발 환경</h1>
        <p className="summary">
          React 개발 서버가 정상 실행 중입니다. 실제 고객·Provider·관리자 업무
          화면은 다음 개발 단계부터 구현합니다.
        </p>

        <div className="status" role="status">
          <span className="statusDot" aria-hidden="true" />
          Frontend ready
        </div>

        <ul className="roleList" aria-label="향후 화면 영역">
          {roleAreas.map((role) => (
            <li key={role.path}>
              <strong>{role.name}</strong>
              <code>{role.path}</code>
            </li>
          ))}
        </ul>
      </section>
    </main>
  )
}

export default App
