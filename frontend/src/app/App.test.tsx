import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from './App'

function renderAt(path: string) {
  return render(
    <QueryClientProvider client={new QueryClient()}>
      <MemoryRouter initialEntries={[path]}>
        <App />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('App routing', () => {
  afterEach(() => {
    cleanup()
    vi.useRealTimers()
  })

  it('renders the reading layout at the root', () => {
    renderAt('/')

    expect(screen.getByRole('heading', { name: 'La poésie du lundi' })).toBeInTheDocument()
  })

  it('links the site icon and title to the home page under the site name', () => {
    renderAt('/')

    const home = screen.getByRole('link', { name: 'La poésie du lundi' })
    expect(home).toHaveAttribute('href', '/')
    expect(home.querySelector('img')).toHaveAttribute('src', '/favicon.svg')
    expect(home.querySelector('img')).toHaveAttribute('alt', '')
  })

  it('renders the reading layout for a poem, archive, and series route', () => {
    renderAt('/poems/au-pied-de-mon-arbre')
    expect(screen.getByRole('heading', { name: 'La poésie du lundi' })).toBeInTheDocument()
    cleanup()

    renderAt('/archive')
    expect(screen.getByRole('heading', { name: 'La poésie du lundi' })).toBeInTheDocument()
    cleanup()

    renderAt('/series/une-saison')
    expect(screen.getByRole('heading', { name: 'La poésie du lundi' })).toBeInTheDocument()
  })

  it('signs every public page with a footer carrying the current year', () => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date('2027-03-01T12:00:00'))

    for (const path of ['/', '/poems/au-pied-de-mon-arbre', '/archive', '/series/une-saison']) {
      renderAt(path)
      expect(screen.getByRole('contentinfo')).toHaveTextContent(
        '© 2027 La poésie du lundi · Fait avec ♥ par monti-it.io',
      )
      cleanup()
    }
  })

  it('links monti-it.io in the footer to the portfolio', () => {
    renderAt('/')

    expect(screen.getByRole('link', { name: 'monti-it.io' })).toHaveAttribute(
      'href',
      'https://monti-it.io',
    )
  })

  it('puts the footer after the page and the side pane', () => {
    const { container } = renderAt('/')

    const footer = screen.getByRole('contentinfo')
    for (const selector of ['.site-main', '.side-pane']) {
      const region = container.querySelector(selector)
      expect(region).not.toBeNull()
      expect(region!.compareDocumentPosition(footer) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    }
  })

  it('renders no footer on the admin and not-found pages', () => {
    for (const path of ['/admin', '/admin/poems/new', '/nonsense']) {
      renderAt(path)
      expect(screen.queryByRole('contentinfo')).not.toBeInTheDocument()
      cleanup()
    }
  })

  it('renders the authoring route tree at /admin', () => {
    renderAt('/admin')

    expect(screen.getByRole('heading', { name: 'Poèmes' })).toBeInTheDocument()
  })

  it('renders the new-poem editor route', () => {
    renderAt('/admin/poems/new')

    expect(screen.getByRole('heading', { name: 'Nouveau poème' })).toBeInTheDocument()
  })

  it('renders a not-found page for an unknown path', () => {
    renderAt('/nonsense')

    expect(screen.getByRole('heading', { name: 'Page not found' })).toBeInTheDocument()
  })
})
