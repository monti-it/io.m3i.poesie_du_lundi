import { describe, expect, it } from 'vitest'
import { isMonday, mondaysOfMonth, nextMonday, toIsoDate } from './monday'

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

describe('toIsoDate', () => {
  it('formats local date parts as yyyy-MM-dd, zero-padded', () => {
    expect(toIsoDate(new Date(2026, 0, 5))).toBe('2026-01-05')
  })
})

describe('mondaysOfMonth', () => {
  it('lists every Monday of a month starting mid-week', () => {
    // March 2020: the 1st is a Sunday, so the first Monday is the 2nd.
    expect(mondaysOfMonth(2020, 3).map(toIsoDate)).toEqual([
      '2020-03-02',
      '2020-03-09',
      '2020-03-16',
      '2020-03-23',
      '2020-03-30',
    ])
  })

  it('lists every Monday of a month that starts on a Monday', () => {
    expect(mondaysOfMonth(2026, 6).map(toIsoDate)).toEqual([
      '2026-06-01',
      '2026-06-08',
      '2026-06-15',
      '2026-06-22',
      '2026-06-29',
    ])
  })
})
