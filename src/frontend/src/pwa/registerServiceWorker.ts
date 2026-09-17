export function registerServiceWorker(): void {
  if (!import.meta.env.PROD || !('serviceWorker' in navigator)) return
  window.addEventListener('load', () => {
    let hadController = Boolean(navigator.serviceWorker.controller)
    let refreshing = false
    navigator.serviceWorker.addEventListener('controllerchange', () => {
      if (!hadController) { hadController = true; return }
      if (refreshing) return
      refreshing = true
      window.location.reload()
    })
    void navigator.serviceWorker.register('/sw.js', { updateViaCache: 'none' }).then(registration => {
      void registration.update()
      const checkForUpdate = () => { if (document.visibilityState === 'visible') void registration.update() }
      window.addEventListener('focus', checkForUpdate)
      document.addEventListener('visibilitychange', checkForUpdate)
    })
  })
}
