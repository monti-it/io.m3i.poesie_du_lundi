import { describe, expect, it } from 'vitest'
import { isMonday, nextMonday } from './monday'

describe('isMonday', () => {
  it('accepts a Monday', () => {
    expect(isMonday('2026-09-21')).toBe(true)
  })

  it('rejects any other day of the week', () => {
    expect(isMonday('2026-09-22')).toBe(false)
    expect(isMonday('2026-09-20')).toBe(false)
  })
})

describe('nextMonday', () => {
  it('returns the following Monday when today is a Monday', () => {
    expect(nextMonday(new Date(2026, 8, 21))).toBe('2026-09-28')
  })

  it('returns the upcoming Monday from a mid-week day', () => {
    expect(nextMonday(new Date(2026, 8, 15))).toBe('2026-09-21')
  })

  it('returns the upcoming Monday from a Sunday', () => {
    expect(nextMonday(new Date(2026, 8, 20))).toBe('2026-09-21')
  })
})
