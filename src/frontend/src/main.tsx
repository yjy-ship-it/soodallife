import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './app/App.tsx'
import { registerServiceWorker } from './pwa/registerServiceWorker'
import { installApiEndpoint } from './config/apiEndpoint'
import { installImageUploadPolicy } from './utils/imageUploadPolicy'

installApiEndpoint()
installImageUploadPolicy()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)

registerServiceWorker()
