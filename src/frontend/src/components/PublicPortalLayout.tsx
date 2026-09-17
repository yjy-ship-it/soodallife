export function isSharedPortalPublicPath(pathname: string) {
  return /^(\/company|\/support|\/notices(?:\/|$)|\/faq(?:\/|$)|\/policies(?:\/|$))/.test(pathname)
}
