// Session-expiry handling for the shared Authelia SSO gate in front of /admin and
// /api/admin/* — a trimmed port of io.m3i.ledgy's src/shared/api/sessionExpired.ts (#206, #260).
// Only adminClient.ts uses this; the public site is anonymous and never hits the gate.
//
// When the SSO session lapses, Traefik answers the next request with a cross-origin 302 to the
// login portal. A `fetch` can't follow it (the portal sends no CORS header, so the browser turns
// the redirect chase into an opaque `TypeError`) — adminClient sends `redirect: 'manual'` instead,
// which hands back an opaque-redirect response (`type: 'opaqueredirect'`, `status: 0`) that this
// module treats as "session expired".
//
// Recovery is a top-level, cache-busting navigation the gate can 302 from. A `sessionStorage`
// stamp is a cross-reload circuit breaker: a second opaque redirect within the recovery window
// means the gate is flapping, so auto-navigation stops rather than reloading forever.

export class SessionExpiredError extends Error {
  constructor() {
    super('Your session has expired — signing you back in…')
    this.name = 'SessionExpiredError'
  }
}

let signalled = false

const RECOVERY_STAMP_KEY = 'poesie.session-recovery-at'
const RECOVERY_WINDOW_MS = 10_000

function readRecoveryStamp(): number | null {
  try {
    const raw = window.sessionStorage.getItem(RECOVERY_STAMP_KEY)
    if (raw === null) return null
    const at = Number(raw)
    return Number.isFinite(at) ? at : null
  } catch {
    return null
  }
}

function writeRecoveryStamp(): void {
  try {
    window.sessionStorage.setItem(RECOVERY_STAMP_KEY, String(Date.now()))
  } catch {
    // sessionStorage unavailable (private mode, disabled) — proceed without a circuit breaker.
  }
}

/** Clears the circuit-breaker stamp. Called on the first real admin response after a reload, so a
 *  later, unrelated session lapse gets a fresh recovery attempt rather than tripping the breaker. */
export function noteAuthenticatedResponse(): void {
  try {
    window.sessionStorage.removeItem(RECOVERY_STAMP_KEY)
  } catch {
    // ignore — nothing to clear if storage is unavailable.
  }
}

function navigateToGate(): void {
  try {
    const url = new URL(window.location.href)
    url.searchParams.set('_sso', Date.now().toString(36))
    window.location.replace(url.toString())
  } catch {
    window.location.replace(window.location.href)
  }
}

/**
 * Called when an admin fetch hits the gate's cross-origin login redirect. Stamps the recovery
 * time and navigates so the gate can send the author to the portal — unless a navigation moments
 * ago already tried that and got redirected right back, in which case the circuit breaker trips
 * and this stops auto-navigating (a future authoring UI can offer a manual retry instead).
 */
export function triggerSessionExpiredRedirect(): void {
  if (signalled) return
  signalled = true

  const lastRecovery = readRecoveryStamp()
  const inReloadLoop = lastRecovery !== null && Date.now() - lastRecovery < RECOVERY_WINDOW_MS

  if (inReloadLoop) {
    return
  }

  writeRecoveryStamp()
  setTimeout(navigateToGate, 600)
}

/** React Query's `queries.retry` predicate: a session-expired redirect is already in flight, so
 *  never retry it; cap everything else at two retries so a genuinely-down API fails fast. */
export const shouldRetryQuery = (failureCount: number, error: unknown): boolean =>
  !(error instanceof SessionExpiredError) && failureCount < 2

/** Test-only: forget the "already redirecting" flag and the circuit-breaker stamp. */
export function resetSessionExpiredForTests(): void {
  signalled = false
  try {
    window.sessionStorage.removeItem(RECOVERY_STAMP_KEY)
  } catch {
    // ignore
  }
}
