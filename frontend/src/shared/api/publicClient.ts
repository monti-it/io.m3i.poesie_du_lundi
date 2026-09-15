// Transport for the anonymous public site (home, poem pages, feeds — docs/ARCHITECTURE.md
// "Request flow"). No SSO gate sits in front of these routes, so a failed fetch is a plain
// network/HTTP error, never a session redirect — see adminClient.ts for the gated counterpart.
import { ApiError, type ProblemDetails } from './errors'

export async function publicFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  })

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as ProblemDetails | null
    const message =
      problem?.error ??
      problem?.detail ??
      problem?.title ??
      `Request to ${path} failed with status ${response.status}`
    throw new ApiError(message, response.status, problem ?? undefined)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}
