// Transport for the SSO-gated /api/admin/* authoring surface. Detects the forward-auth gate's
// session-expiry redirect (sessionExpired.ts) instead of letting it surface as an opaque
// NetworkError — see the public counterpart in publicClient.ts, which has no such gate to detect.
import { ApiError, type ProblemDetails } from './errors'
import {
  noteAuthenticatedResponse,
  SessionExpiredError,
  triggerSessionExpiredRedirect,
} from './sessionExpired'

export async function adminFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    // The SSO gate answers an expired session with a cross-origin 302 to the login portal.
    // 'manual' stops the browser chasing it into a CORS TypeError — we get an opaque redirect
    // (type 'opaqueredirect', status 0) to detect instead.
    redirect: 'manual',
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  })

  if (response.type === 'opaqueredirect' || response.status === 0) {
    triggerSessionExpiredRedirect()
    throw new SessionExpiredError()
  }

  // A real response (any status) means the gate let this request through.
  noteAuthenticatedResponse()

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as ProblemDetails | null
    const message =
      problem?.detail ?? problem?.title ?? `Request to ${path} failed with status ${response.status}`
    throw new ApiError(message, response.status, problem ?? undefined)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export const adminPost = <T>(path: string, body: unknown) =>
  adminFetch<T>(path, { method: 'POST', body: JSON.stringify(body) })
export const adminPatch = <T>(path: string, body: unknown) =>
  adminFetch<T>(path, { method: 'PATCH', body: JSON.stringify(body) })
export const adminPut = <T>(path: string, body: unknown) =>
  adminFetch<T>(path, { method: 'PUT', body: JSON.stringify(body) })
export const adminDelete = (path: string) => adminFetch<void>(path, { method: 'DELETE' })
