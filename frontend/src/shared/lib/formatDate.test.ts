import { afterEach, describe, expect, it, vi } from 'vitest'
import { formatDate } from './formatDate'

// The formatter is built when the module loads, so each timezone gets a fresh import.
async function formatDateIn(timeZone: string, iso: string): Promise<string> {
  vi.stubEnv('TZ', timeZone)
  vi.resetModules()
  const { formatDate: formatDateInZone } = await import('./formatDate')
  return formatDateInZone(iso)
}

describe('formatDate', () => {
  afterEach(() => {
    vi.unstubAllEnvs()
  })

  it('formats a date in long French form', () => {
    expect(formatDate('2026-09-21')).toBe('21 septembre 2026')
  })

  it.each(['UTC', 'America/Los_Angeles', 'Pacific/Pago_Pago', 'Pacific/Kiritimati'])(
    'keeps the first of a month on that day in %s',
    async (timeZone) => {
      expect(await formatDateIn(timeZone, '2026-03-01')).toBe('1 mars 2026')
    },
  )
})
