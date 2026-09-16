import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { Poem } from '../api/publicApi'
import { PoemPage } from './PoemPage'

function jsonResponse(status: number, body: unknown) {
  return { ok: status >= 200 && status < 300, status, json: async () => body }
}

function renderPoemPage(slug: string, search = '') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/poems/${slug}${search}`]}>
        <Routes>
          <Route path="/poems/:slug" element={<PoemPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('PoemPage', () => {
  afterEach(() => {
    cleanup()
    vi.unstubAllGlobals()
  })

  it('shows the poem, its series link, and prev/next navigation', async () => {
    const poem: Poem = {
      id: '1',
      title: 'Un poème',
      body: 'Un corps.',
      slug: 'un-poeme',
      publicationDate: '2026-09-14',
      series: { id: 's1', title: 'Une saison', slug: 'une-saison' },
      previous: { slug: 'poeme-precedent', title: 'Le précédent' },
      next: { slug: 'poeme-suivant', title: 'Le suivant' },
    }
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(200, poem)))

    renderPoemPage('un-poeme')

    expect(await screen.findByRole('heading', { name: 'Un poème' })).toBeInTheDocument()
    expect(screen.getByText('Une saison')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Le précédent/ })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Le suivant/ })).toBeInTheDocument()
  })

  it('renders the not-found page for an unknown slug', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(404, { error: 'Poem not found.' })))

    renderPoemPage('inconnu')

    expect(await screen.findByRole('heading', { name: 'Page not found' })).toBeInTheDocument()
  })

  it('sets the document title, description, canonical link, OpenGraph/Twitter tags, and JSON-LD', async () => {
    const poem: Poem = {
      id: '1',
      title: 'Un poème',
      body: 'Un corps.',
      slug: 'un-poeme',
      publicationDate: '2026-09-14',
      series: null,
      previous: null,
      next: null,
    }
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(200, poem)))

    renderPoemPage('un-poeme')
    await screen.findByRole('heading', { name: 'Un poème' })

    expect(document.title).toBe('Un poème — La poésie du lundi')
    expect(document.querySelector('meta[name="description"]')).toHaveAttribute('content', 'Un corps.')
    expect(document.querySelector('link[rel="canonical"]')).toHaveAttribute(
      'href',
      `${window.location.origin}/poems/un-poeme`,
    )
    expect(document.querySelector('meta[property="og:title"]')).toHaveAttribute('content', 'Un poème')
    expect(document.querySelector('meta[name="twitter:card"]')).toHaveAttribute('content', 'summary')
    const jsonLd = document.querySelector('script[type="application/ld+json"]')
    expect(jsonLd).not.toBeNull()
    expect(JSON.parse(jsonLd!.textContent!)).toMatchObject({
      '@type': 'CreativeWork',
      name: 'Un poème',
      datePublished: '2026-09-14',
    })
  })

  it('adds a noindex meta tag when viewing via a preview link', async () => {
    const poem: Poem = {
      id: '1',
      title: 'Un poème en révision',
      body: 'Un corps.',
      slug: 'un-poeme-en-revision',
      publicationDate: '2026-09-14',
      series: null,
      previous: null,
      next: null,
    }
    const fetchMock = vi.fn(async () => jsonResponse(200, poem))
    vi.stubGlobal('fetch', fetchMock)

    renderPoemPage('un-poeme-en-revision', '?preview=a-signed-token')
    await screen.findByRole('heading', { name: 'Un poème en révision' })

    expect(document.querySelector('meta[name="robots"]')).toHaveAttribute('content', 'noindex, nofollow')
    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('preview=a-signed-token'),
      expect.anything(),
    )
  })

  it('has no noindex meta tag outside preview mode', async () => {
    const poem: Poem = {
      id: '1',
      title: 'Un poème',
      body: 'Un corps.',
      slug: 'un-poeme',
      publicationDate: '2026-09-14',
      series: null,
      previous: null,
      next: null,
    }
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(200, poem)))

    renderPoemPage('un-poeme')
    await screen.findByRole('heading', { name: 'Un poème' })

    expect(document.querySelector('meta[name="robots"]')).toBeNull()
  })
})
