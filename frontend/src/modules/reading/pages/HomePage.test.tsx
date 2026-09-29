import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { Poem } from '../api/publicApi'
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

  it('shows this Monday\'s poem', async () => {
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
    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input)
        if (url === '/api/poems/this-monday') return jsonResponse(200, thisMonday)
        throw new Error(`Unexpected fetch to ${url}`)
      }),
    )

    renderHomePage()

    expect(await screen.findByRole('heading', { name: 'Poème de cette semaine' })).toBeInTheDocument()
    expect(screen.getByText('Une saison')).toBeInTheDocument()
    // "Poèmes récents" lives in the shared side pane now (SidePane.test.tsx), not the page body.
    expect(screen.queryByRole('heading', { name: 'Poèmes récents' })).not.toBeInTheDocument()
  })

  it('shows an empty state when nothing has been published yet', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input)
        if (url === '/api/poems/this-monday') return jsonResponse(404, { error: 'No poem has been published yet.' })
        throw new Error(`Unexpected fetch to ${url}`)
      }),
    )

    renderHomePage()

    expect(await screen.findByText(/Aucun poème/)).toBeInTheDocument()
  })
})
