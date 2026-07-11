import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import { App } from '@/app/App'
import { i18n } from '@/i18n'
import '@/styles/tokens.css'
import '@/styles/global.css'
const container = document.getElementById('root')
if (container === null) throw new Error('Root container was not found.')
createRoot(container).render(<StrictMode><I18nextProvider i18n={i18n}><QueryClientProvider client={new QueryClient()}><App /></QueryClientProvider></I18nextProvider></StrictMode>)
