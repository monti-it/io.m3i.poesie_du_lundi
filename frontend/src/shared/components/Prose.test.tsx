import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { Prose } from './Prose'

describe('Prose', () => {
  afterEach(() => {
    cleanup()
  })

  it('renders a blank-line-separated stanza as its own paragraph', () => {
    const { container } = render(<Prose>{'Première strophe.\n\nSeconde strophe.'}</Prose>)

    const paragraphs = container.querySelectorAll('p')
    expect(paragraphs).toHaveLength(2)
    expect(paragraphs[0]).toHaveTextContent('Première strophe.')
    expect(paragraphs[1]).toHaveTextContent('Seconde strophe.')
  })

  it('preserves a single line break within a stanza', () => {
    const { container } = render(<Prose>{'Vers un.\nVers deux.'}</Prose>)

    const paragraph = container.querySelector('p')
    expect(paragraph?.querySelector('br')).toBeInTheDocument()
    expect(paragraph?.textContent).toBe('Vers un.\nVers deux.')
  })

  it('renders basic Markdown emphasis', () => {
    render(<Prose>{'Un mot en *italique*.'}</Prose>)

    expect(screen.getByText('italique').tagName).toBe('EM')
  })

  it('does not render raw HTML as markup', () => {
    render(<Prose>{'<strong>injecté</strong>'}</Prose>)

    expect(screen.queryByText('injecté')?.tagName).not.toBe('STRONG')
  })
})
