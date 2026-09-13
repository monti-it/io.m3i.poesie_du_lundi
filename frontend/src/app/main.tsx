import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { shouldRetryQuery } from '@shared/api/sessionExpired'
import '../index.css'
import App from './App'

// Never retry a lapsed-SSO-session redirect on an admin route; cap the rest at two retries.
const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: shouldRetryQuery } },
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
)
