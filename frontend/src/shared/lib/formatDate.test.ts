import { afterEach, describe, expect, it, vi } from 'vitest'
import { formatDate } from './formatDate'

describe('formatDate', () => {
  afterEach(() => {
    vi.unstubAllEnvs()
    vi.resetModules()
  })

  it('formats a DateOnly as a long French date', () => {
    expect(formatDate('2026-09-21')).toBe('21 septembre 2026')
  })

  it.each(['UTC', 'America/Los_Angeles', 'Pacific/Honolulu', 'Pacific/Kiritimati'])(
    'keeps the first of a month on that day in the %s timezone',
    async (timeZone) => {
      vi.stubEnv('TZ', timeZone)
      vi.resetModules()
      const { formatDate: formatDateInZone } = await import('./formatDate')
      expect(formatDateInZone('2026-03-01')).toBe('1 mars 2026')
    },
  )
})
