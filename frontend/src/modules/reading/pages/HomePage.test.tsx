import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { Poem, PagedPoems } from '../api/publicApi'
import { HomePage } from './HomePage'

function jsonResponse(status: number, body: unknown) {
  return { ok: status >= 200 && status < 300, status, json: async () => body }
}

function renderHomePage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/']}>
        <HomePage />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('HomePage', () => {
  afterEach(() => {
    cleanup()
    vi.unstubAllGlobals()
  })

  it('shows this Monday\'s poem and the recent list, without repeating it', async () => {
    const thisMonday: Poem = {
      id: '1',
      title: 'Poème de cette semaine',
      body: 'Un corps.\nSur deux lignes.',
      slug: 'poeme-de-cette-semaine',
      publicationDate: '2026-09-21',
      series: { id: 's1', title: 'Une saison', slug: 'une-saison' },
      previous: null,
      next: null,
    }
    const recent: PagedPoems = {
      items: [
        { id: '1', title: 'Poème de cette semaine', slug: 'poeme-de-cette-semaine', publicationDate: '2026-09-21' },
        { id: '2', title: 'Un autre poème', slug: 'un-autre-poeme', publicationDate: '2026-09-14' },
      ],
      page: 1,
      pageSize: 6,
      totalCount: 2,
    }
    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input)
        if (url === '/api/poems/this-monday') return jsonResponse(200, thisMonday)
        if (url.startsWith('/api/poems?')) return jsonResponse(200, recent)
        throw new Error(`Unexpected fetch to ${url}`)
      }),
    )

    renderHomePage()

    expect(await screen.findByRole('heading', { name: 'Poème de cette semaine' })).toBeInTheDocument()
    expect(screen.getByText('Une saison')).toBeInTheDocument()
    expect(screen.getByText('Un autre poème')).toBeInTheDocument()
    // The recent list must not repeat the poem already shown in full above.
    expect(screen.getAllByText('Poème de cette semaine')).toHaveLength(1)
  })

  it('shows an empty state when nothing has been published yet', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input)
        if (url === '/api/poems/this-monday') return jsonResponse(404, { error: 'No poem has been published yet.' })
        if (url.startsWith('/api/poems?')) {
          return jsonResponse(200, { items: [], page: 1, pageSize: 6, totalCount: 0 } satisfies PagedPoems)
        }
        throw new Error(`Unexpected fetch to ${url}`)
      }),
    )

    renderHomePage()

    expect(await screen.findByText(/Aucun poème/)).toBeInTheDocument()
  })
})
