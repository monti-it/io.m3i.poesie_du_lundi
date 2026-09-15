import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { Archive } from '../api/publicApi'
import { ArchivePage } from './ArchivePage'

function jsonResponse(status: number, body: unknown) {
  return { ok: status >= 200 && status < 300, status, json: async () => body }
}

function renderArchivePage(path: string) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[path]}>
        <ArchivePage />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('ArchivePage', () => {
  afterEach(() => {
    cleanup()
    vi.unstubAllGlobals()
  })

  it('shows the year/month groups as a browsable tree', async () => {
    const archive: Archive = {
      groups: [{ year: 2026, month: 3, count: 2 }],
      entries: [],
    }
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(200, archive)))

    renderArchivePage('/archive')

    expect(await screen.findByRole('link', { name: /mars 2026 \(2\)/ })).toBeInTheDocument()
  })

  it('shows the entries for a chosen year and month', async () => {
    const archive: Archive = {
      groups: [{ year: 2026, month: 3, count: 1 }],
      entries: [{ id: '1', title: 'Poème de mars', slug: 'poeme-de-mars', publicationDate: '2026-03-02' }],
    }
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(200, archive)))

    renderArchivePage('/archive?year=2026&month=3')

    expect(await screen.findByText('Poème de mars')).toBeInTheDocument()
  })

  it('shows an empty state when nothing has been published yet', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(200, { groups: [], entries: [] } satisfies Archive)))

    renderArchivePage('/archive')

    expect(await screen.findByText(/Aucun poème/)).toBeInTheDocument()
  })
})
