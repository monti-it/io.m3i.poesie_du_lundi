import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  resetSessionExpiredForTests,
  SessionExpiredError,
  shouldRetryQuery,
  triggerSessionExpiredRedirect,
} from './sessionExpired'

describe('sessionExpired', () => {
  const originalLocation = window.location
  let replace: ReturnType<typeof vi.fn>

  beforeEach(() => {
    vi.useFakeTimers()
    resetSessionExpiredForTests()

    // jsdom's window.location.replace isn't directly spy-able (non-configurable) — swap the
    // whole object for one whose replace is a mock.
    replace = vi.fn()
    Object.defineProperty(window, 'location', {
      configurable: true,
      value: { ...originalLocation, replace },
    })
  })

  afterEach(() => {
    vi.useRealTimers()
    Object.defineProperty(window, 'location', { configurable: true, value: originalLocation })
  })

  it('navigates once, after a short delay, on the first expiry', () => {
    triggerSessionExpiredRedirect()
    expect(replace).not.toHaveBeenCalled()

    vi.advanceTimersByTime(600)
    expect(replace).toHaveBeenCalledTimes(1)
  })

  it('does not navigate again for a second expiry in the same page life', () => {
    triggerSessionExpiredRedirect()
    vi.advanceTimersByTime(600)
    triggerSessionExpiredRedirect()
    vi.advanceTimersByTime(600)

    expect(replace).toHaveBeenCalledTimes(1)
  })

  it('trips the circuit breaker and skips navigation when the gate just bounced back', () => {
    window.sessionStorage.setItem('poesie.session-recovery-at', String(Date.now()))

    triggerSessionExpiredRedirect()
    vi.advanceTimersByTime(600)

    expect(replace).not.toHaveBeenCalled()
  })

  it('retries a normal error but never a session-expired one', () => {
    expect(shouldRetryQuery(0, new Error('boom'))).toBe(true)
    expect(shouldRetryQuery(1, new Error('boom'))).toBe(true)
    expect(shouldRetryQuery(2, new Error('boom'))).toBe(false)
    expect(shouldRetryQuery(0, new SessionExpiredError())).toBe(false)
  })
})
