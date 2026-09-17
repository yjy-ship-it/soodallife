import type { ReactNode } from 'react'

export type MenuIconName = 'home' | 'services' | 'care' | 'interior' | 'emergency' | 'communication' | 'work' | 'community' | 'account' | 'request' | 'proposal' | 'progress' | 'subscription' | 'history' | 'aftercare' | 'dispute' | 'report' | 'support' | 'favorite' | 'profile' | 'address' | 'security' | 'notification' | 'consent' | 'block' | 'schedule' | 'quote' | 'area' | 'document' | 'approval' | 'advertising' | 'trust' | 'wallet' | 'logout' | 'exit'

const paths: Record<MenuIconName, ReactNode> = {
  home: <><path d="M3.5 11 12 4l8.5 7"/><path d="M5.5 10v10h13V10M9.5 20v-6h5v6"/></>,
  services: <><rect x="4" y="4" width="6" height="6" rx="1"/><rect x="14" y="4" width="6" height="6" rx="1"/><rect x="4" y="14" width="6" height="6" rx="1"/><rect x="14" y="14" width="6" height="6" rx="1"/></>,
  care: <><path d="M12 20s-7-4.4-7-10a4 4 0 0 1 7-2.6A4 4 0 0 1 19 10c0 5.6-7 10-7 10Z"/><path d="M9 12h6M12 9v6"/></>,
  interior: <><path d="M4 18 15.5 6.5l2 2L6 20H4z"/><path d="m14 5 2-2 5 5-2 2M8 16l2 2"/></>,
  emergency: <><path d="M8 16V9a4 4 0 0 1 8 0v7M5 20h14M6 16h12"/><path d="M4 8H2M22 8h-2M5 3l1.5 1.5M19 3l-1.5 1.5"/></>,
  communication: <><path d="M4 5h16v11H9l-5 4V5Z"/><path d="M8 9h8M8 12.5h6"/></>,
  work: <><rect x="3.5" y="7" width="17" height="12" rx="2"/><path d="M9 7V4h6v3M3.5 12h17M10 12v2h4v-2"/></>,
  community: <><circle cx="8" cy="9" r="3"/><circle cx="17" cy="9" r="2.5"/><path d="M2.5 20c.4-4 2.2-6 5.5-6s5.1 2 5.5 6M14 15c3.7-.7 6 1 6.5 5"/></>,
  account: <><circle cx="12" cy="8" r="3.5"/><path d="M4.5 21c.6-4.7 3-7 7.5-7s6.9 2.3 7.5 7"/></>,
  request: <><path d="M7 3h7l4 4v14H7z"/><path d="M14 3v5h5M10 12h5M10 16h5"/></>,
  proposal: <><path d="M5 4h14v16H5z"/><path d="m8 14 2.5 2.5L16 10M8 8h5"/></>,
  progress: <><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3.5 2"/></>,
  subscription: <><path d="M4 7h16v12H4zM4 10h16"/><path d="M8 15h3"/></>,
  history: <><path d="M4 5h16v15H4zM8 3v4M16 3v4M4 9h16"/><path d="M8 13h3M8 16h6"/></>,
  aftercare: <path d="m14 6 4-2-2 4 3 3 2-2c.5 3.8-3.5 6.7-6.8 4.8L8 20l-4-4 6.2-6.2C8.3 6.5 11.2 2.5 15 3z"/>,
  dispute: <><path d="M12 3v18M6 6h12M6 6l-3 6h6L6 6ZM18 6l-3 6h6l-3-6Z"/><path d="M8 21h8"/></>,
  report: <><path d="M6 3h12v18H6z"/><path d="M9 8h6M9 12h6M9 16h3"/></>,
  support: <><circle cx="12" cy="12" r="9"/><path d="M8 15v-5M16 15v-5M8 10a4 4 0 0 1 8 0M6 13h2M16 13h2M16 15c0 2-1.4 3-4 3"/></>,
  favorite: <path d="m12 4 2.4 4.9 5.4.8-3.9 3.8.9 5.4-4.8-2.5-4.8 2.5.9-5.4-3.9-3.8 5.4-.8z"/>,
  profile: <><circle cx="12" cy="7.5" r="3"/><path d="M6 20v-2c0-3.3 2-5 6-5s6 1.7 6 5v2"/></>,
  address: <><path d="M12 21s7-6.3 7-12a7 7 0 1 0-14 0c0 5.7 7 12 7 12Z"/><circle cx="12" cy="9" r="2.3"/></>,
  security: <><rect x="5" y="10" width="14" height="11" rx="2"/><path d="M8 10V7a4 4 0 0 1 8 0v3M12 14v3"/></>,
  notification: <><path d="M6 16h12l-1.5-2.5V10a4.5 4.5 0 0 0-9 0v3.5zM10 19h4"/></>,
  consent: <><path d="M6 3h12v18H6zM9 7h6M9 11h6"/><path d="m9 16 2 2 4-4"/></>,
  block: <><circle cx="12" cy="12" r="9"/><path d="m6 6 12 12"/></>,
  schedule: <><rect x="4" y="5" width="16" height="15" rx="2"/><path d="M8 3v4M16 3v4M4 9h16M8 13h3M13 13h3M8 16h3"/></>,
  quote: <><path d="M5 3h14v18H5zM8 7h8M8 11h8M8 15h4"/><circle cx="16" cy="16" r="2.5"/></>,
  area: <><path d="m4 6 5-2 6 2 5-2v15l-5 2-6-2-5 2V6Z"/><path d="M9 4v15M15 6v15"/></>,
  document: <><path d="M7 3h7l4 4v14H7zM14 3v5h5M10 12h5M10 16h5"/></>,
  approval: <><circle cx="12" cy="12" r="9"/><path d="m8 12 2.5 2.5L16 9"/></>,
  advertising: <><path d="m4 11 12-5v12L4 13zM16 9h3M16 15h3M8 14l1 5H6l-1-6"/></>,
  trust: <><path d="M12 3 5 6v5c0 4.7 2.7 8 7 10 4.3-2 7-5.3 7-10V6z"/><path d="m8.5 12 2.2 2.2 4.8-5"/></>,
  wallet: <><rect x="3" y="6" width="18" height="14" rx="2"/><path d="M3 9h18M15 13h6v4h-6a2 2 0 0 1 0-4Z"/></>,
  logout: <><path d="M10 4H5v16h5M14 8l4 4-4 4M8 12h10"/></>,
  exit: <><path d="M10 4H5v16h5M14 8l4 4-4 4M8 12h10"/></>,
}

export function MenuIcon({ name, className = '' }: { name: MenuIconName; className?: string }) {
  return <svg className={`menuVectorIcon ${className}`.trim()} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" focusable="false">{paths[name]}</svg>
}

const routeIcons: ReadonlyArray<readonly [string, MenuIconName]> = [
  ['/customer/interested-services', 'favorite'], ['/customer/profile', 'profile'], ['/customer/addresses', 'address'], ['/customer/security', 'security'], ['/customer/notifications', 'notification'], ['/customer/notification-settings', 'notification'], ['/customer/consents', 'consent'], ['/customer/provider-blocks', 'block'], ['/customer/proposals', 'proposal'], ['/customer/requests', 'request'], ['/customer/transactions', 'progress'], ['/customer/progress', 'progress'], ['/customer/care/contracts', 'subscription'], ['/customer/interior/projects', 'interior'], ['/customer/service-history', 'history'], ['/customer/after-services', 'aftercare'], ['/customer/disputes', 'dispute'], ['/customer/reports', 'report'], ['/customer', 'account'],
  ['/provider/matched-requests', 'request'], ['/provider/quotes', 'quote'], ['/provider/progress', 'progress'], ['/provider/schedule', 'schedule'], ['/provider/communications', 'communication'], ['/provider/messages', 'communication'], ['/provider/reviews', 'communication'], ['/provider/care', 'care'], ['/provider/interior', 'interior'], ['/provider/emergency', 'emergency'], ['/provider/after-services', 'aftercare'], ['/provider/proposals', 'proposal'], ['/provider/advertising', 'advertising'], ['/provider/onboarding', 'profile'], ['/provider/services', 'services'], ['/provider/areas', 'area'], ['/provider/documents', 'document'], ['/provider/approval', 'approval'], ['/provider/blocks', 'block'], ['/provider/support', 'support'], ['/provider/exit', 'exit'], ['/provider', 'home'],
  ['/services', 'services'], ['/care', 'care'], ['/interior', 'interior'], ['/emergency', 'emergency'], ['/help-room', 'community'], ['/suggestions', 'proposal'], ['/support', 'support'],
]

export function menuIconForPath(path: string): MenuIconName {
  return routeIcons.find(([prefix]) => path === prefix || path.startsWith(`${prefix}/`))?.[1] ?? 'services'
}
