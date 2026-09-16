import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
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

  it('shows an empty state when nothing has been published yet', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(200, { groups: [], entries: [] } satisfies Archive)))

    renderArchivePage('/archive')

    expect(await screen.findByText(/Aucun poème/)).toBeInTheDocument()
  })

  describe('calendar view (default)', () => {
    it('renders a Monday cell per published week and a dashed cell for a skipped one', async () => {
      // 2020 is entirely in the past, so no week can land in the "future" state.
      const archive: Archive = {
        groups: [{ year: 2020, month: 3, count: 1 }],
        entries: [{ id: '1', title: 'Poème de mars', slug: 'poeme-de-mars', publicationDate: '2020-03-02' }],
      }
      vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(200, archive)))

      renderArchivePage('/archive?year=2020')

      const published = await screen.findByRole('button', { name: '2 mars 2020' })
      expect(published).toHaveClass('week-cell--published')

      const skipped = screen.getByRole('button', { name: '9 mars 2020' })
      expect(skipped).toHaveClass('week-cell--skipped')
      expect(skipped).toBeDisabled()
    })

    it('marks every Monday of a distant future year as "future"', async () => {
      const archive: Archive = { groups: [{ year: 2020, month: 3, count: 1 }], entries: [] }
      vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(200, archive)))

      renderArchivePage('/archive?year=2099')

      const futureCell = await screen.findByRole('button', { name: '2 mars 2099' })
      expect(futureCell).toHaveClass('week-cell--future')
      expect(futureCell).toBeDisabled()
    })

    it('reveals the poem for a published week on click', async () => {
      const archive: Archive = {
        groups: [{ year: 2020, month: 3, count: 1 }],
        entries: [{ id: '1', title: 'Poème de mars', slug: 'poeme-de-mars', publicationDate: '2020-03-02' }],
      }
      vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(200, archive)))

      renderArchivePage('/archive?year=2020')

      const published = await screen.findByRole('button', { name: '2 mars 2020' })
      await userEvent.click(published)

      expect(await screen.findByRole('link', { name: 'Poème de mars' })).toBeInTheDocument()
    })
  })

  describe('list view (fallback)', () => {
    it('shows the year/month groups as a browsable tree', async () => {
      const archive: Archive = {
        groups: [{ year: 2026, month: 3, count: 2 }],
        entries: [],
      }
      vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(200, archive)))

      renderArchivePage('/archive?view=list')

      expect(await screen.findByRole('link', { name: /mars 2026 \(2\)/ })).toBeInTheDocument()
    })

    it('shows the entries for a chosen year and month', async () => {
      const archive: Archive = {
        groups: [{ year: 2026, month: 3, count: 1 }],
        entries: [{ id: '1', title: 'Poème de mars', slug: 'poeme-de-mars', publicationDate: '2026-03-02' }],
      }
      vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(200, archive)))

      renderArchivePage('/archive?view=list&year=2026&month=3')

      expect(await screen.findByText('Poème de mars')).toBeInTheDocument()
    })

    it('is still reached from a bookmarked year/month link with no explicit view param', async () => {
      const archive: Archive = {
        groups: [{ year: 2026, month: 3, count: 1 }],
        entries: [{ id: '1', title: 'Poème de mars', slug: 'poeme-de-mars', publicationDate: '2026-03-02' }],
      }
      vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(200, archive)))

      renderArchivePage('/archive?year=2026&month=3')

      expect(await screen.findByText('Poème de mars')).toBeInTheDocument()
    })
  })
})
