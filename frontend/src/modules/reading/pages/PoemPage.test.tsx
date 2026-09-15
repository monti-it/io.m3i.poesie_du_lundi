import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { Poem } from '../api/publicApi'
import { PoemPage } from './PoemPage'

function jsonResponse(status: number, body: unknown) {
  return { ok: status >= 200 && status < 300, status, json: async () => body }
}

function renderPoemPage(slug: string) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/poems/${slug}`]}>
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
})
