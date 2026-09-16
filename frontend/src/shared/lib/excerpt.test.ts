import { describe, expect, it } from 'vitest'
import { excerpt } from './excerpt'

describe('excerpt', () => {
  it('returns short text unchanged, Markdown syntax stripped', () => {
    expect(excerpt('# Un *poème* court')).toBe('Un poème court')
  })

  it('replaces links with their label text', () => {
    expect(excerpt('Voir [ce poème](https://example.com/poeme)')).toBe('Voir ce poème')
  })

  it('collapses newlines and repeated whitespace into single spaces', () => {
    expect(excerpt('Une ligne.\n\nUne autre  ligne.')).toBe('Une ligne. Une autre ligne.')
  })

  it('truncates long text at a word boundary and appends an ellipsis', () => {
    const long = 'mot '.repeat(60).trim()

    const result = excerpt(long, 20)

    expect(result.length).toBeLessThanOrEqual(21)
    expect(result.endsWith('…')).toBe(true)
  })
})
