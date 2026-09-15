import { describe, expect, it } from 'vitest'
import { slugify } from './slug'

describe('slugify', () => {
  it('lowercases and hyphenates words', () => {
    expect(slugify('Un Poème du Lundi')).toBe('un-poeme-du-lundi')
  })

  it('strips accents', () => {
    expect(slugify('Été précoce')).toBe('ete-precoce')
  })

  it('collapses repeated separators into a single hyphen', () => {
    expect(slugify('Trop   d\'espaces --- et de ponctuation !!!')).toBe('trop-d-espaces-et-de-ponctuation')
  })

  it('trims leading and trailing hyphens', () => {
    expect(slugify('  — Entre tirets —  ')).toBe('entre-tirets')
  })

  it('returns an empty string when nothing slug-able remains', () => {
    expect(slugify('!!!')).toBe('')
  })
})
