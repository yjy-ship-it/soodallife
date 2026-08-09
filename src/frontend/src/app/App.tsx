import { useEffect, useState } from 'react'
import { AuthenticationProvider } from '../auth/AuthContext'
import { useAuthentication } from '../auth/AuthenticationContext'
import { getInitialAuthenticatedPath, navigate } from '../auth/routing'
import type { RoleCode } from '../auth/types'
import { LoginPage } from '../pages/LoginPage'
import { AccessDeniedPage, RoleHomePage, RoleSelectionPage } from '../pages/RolePages'
import './App.css'

const protectedRoutes: Record<string, RoleCode> = {
  '/customer': 'CUSTOMER',
  '/provider': 'PROVIDER',
  '/admin': 'ADMIN',
}

function usePathname() {
  const [pathname, setPathname] = useState(window.location.pathname)
  useEffect(() => {
    const updatePath = () => setPathname(window.location.pathname)
    window.addEventListener('popstate', updatePath)
    return () => window.removeEventListener('popstate', updatePath)
  }, [])
  return pathname
}

function ApplicationRoutes() {
  const pathname = usePathname()
  const { status, user } = useAuthentication()

  useEffect(() => {
    if (status === 'anonymous' && pathname !== '/login') {
      navigate('/login', true)
    } else if (status === 'authenticated' && user && (pathname === '/' || pathname === '/login')) {
      navigate(getInitialAuthenticatedPath(user), true)
    }
  }, [pathname, status, user])

  if (status === 'loading') {
    return <main className="loadingScreen" aria-live="polite">인증 상태를 확인하고 있습니다…</main>
  }

  if (status === 'anonymous') {
    return <LoginPage />
  }

  if (!user) {
    return null
  }

  if (pathname === '/roles') {
    return <RoleSelectionPage />
  }

  const requiredRole = protectedRoutes[pathname]
  if (requiredRole) {
    return user.roles.includes(requiredRole) ? <RoleHomePage role={requiredRole} /> : <AccessDeniedPage />
  }

  return <RoleSelectionPage />
}

function App() {
  return (
    <AuthenticationProvider>
      <ApplicationRoutes />
    </AuthenticationProvider>
  )
}

export default App
