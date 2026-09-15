import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { Series } from '../api/publicApi'
import { SeriesPage } from './SeriesPage'

function jsonResponse(status: number, body: unknown) {
  return { ok: status >= 200 && status < 300, status, json: async () => body }
}

function renderSeriesPage(slug: string) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/series/${slug}`]}>
        <Routes>
          <Route path="/series/:slug" element={<SeriesPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('SeriesPage', () => {
  afterEach(() => {
    cleanup()
    vi.unstubAllGlobals()
  })

  it('shows the series description and its ordered poems', async () => {
    const series: Series = {
      id: 's1',
      title: 'Une saison',
      slug: 'une-saison',
      description: 'Une description.',
      poems: [
        { id: '1', title: 'Premier', slug: 'premier', publicationDate: '2026-09-07' },
        { id: '2', title: 'Deuxième', slug: 'deuxieme', publicationDate: '2026-09-14' },
      ],
    }
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(200, series)))

    renderSeriesPage('une-saison')

    expect(await screen.findByRole('heading', { name: 'Une saison' })).toBeInTheDocument()
    expect(screen.getByText('Une description.')).toBeInTheDocument()
    expect(screen.getByText('Premier')).toBeInTheDocument()
    expect(screen.getByText('Deuxième')).toBeInTheDocument()
  })

  it('shows an empty state when the series has no published poems yet', async () => {
    const series: Series = { id: 's1', title: 'Une saison vide', slug: 'une-saison-vide', description: null, poems: [] }
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(200, series)))

    renderSeriesPage('une-saison-vide')

    expect(await screen.findByText(/Aucun poème publié/)).toBeInTheDocument()
  })

  it('renders the not-found page for an unknown series slug', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(404, { error: 'Series not found.' })))

    renderSeriesPage('inconnue')

    expect(await screen.findByRole('heading', { name: 'Page not found' })).toBeInTheDocument()
  })
})
