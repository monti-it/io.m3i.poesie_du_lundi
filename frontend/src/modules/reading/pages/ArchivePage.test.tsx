import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, onTestFinished, vi } from 'vitest'
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

  describe('list view', () => {
    const archive: Archive = {
      groups: [
        { year: 2026, month: 9, count: 2 },
        { year: 2026, month: 3, count: 1 },
        { year: 2025, month: 12, count: 1 },
      ],
      entries: [
        { id: '1', title: 'Poème de mars', slug: 'poeme-de-mars', publicationDate: '2026-03-02' },
        { id: '2', title: 'Fin septembre', slug: 'fin-septembre', publicationDate: '2026-09-28' },
        { id: '3', title: 'Début septembre', slug: 'debut-septembre', publicationDate: '2026-09-07' },
      ],
    }

    // Answers /api/archive for whichever year is asked; returns the requested URLs.
    function stubArchive() {
      const urls: string[] = []
      vi.stubGlobal(
        'fetch',
        vi.fn(async (input: RequestInfo | URL) => {
          const url = String(input)
          urls.push(url)
          const year = new URL(url, 'http://localhost').searchParams.get('year')
          return jsonResponse(200, year === '2026' ? archive : { groups: archive.groups, entries: [] })
        }),
      )
      return urls
    }

    it("shows every poem of the year under month headings, newest first", async () => {
      const urls = stubArchive()

      renderArchivePage('/archive?view=list&year=2026')

      const headings = await screen.findAllByRole('heading', { level: 3 })
      expect(headings.map((heading) => heading.textContent)).toEqual(['septembre 2026', 'mars 2026'])
      expect(headings[0]).toHaveAttribute('id', '2026-09')
      expect(screen.getAllByRole('link').map((link) => link.textContent)).toEqual([
        'Fin septembre',
        'Début septembre',
        'Poème de mars',
      ])
      expect(screen.getByText('28 septembre 2026')).toHaveAttribute('datetime', '2026-09-28')
      expect(urls).toEqual(['/api/archive?year=2026'])
    })

    it('keeps the selected year when switching years and views', async () => {
      const urls = stubArchive()

      renderArchivePage('/archive?view=list&year=2026')
      await screen.findByRole('heading', { name: 'septembre 2026' })

      await userEvent.click(screen.getByRole('button', { name: '2025' }))
      expect(await screen.findByText('Aucun poème publié en 2025.')).toBeInTheDocument()
      expect(urls.at(-1)).toBe('/api/archive?year=2025')

      await userEvent.click(screen.getByRole('button', { name: 'Calendrier' }))
      expect(await screen.findByRole('button', { name: '1 décembre 2025' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: '2025' })).toHaveAttribute('aria-current', 'true')
    })

    it.each(['/archive?view=list&year=2026&month=3', '/archive?year=2026&month=3'])(
      'lands an old month link (%s) on that month of the year',
      async (path) => {
        stubArchive()
        // jsdom has no layout, hence no scrollIntoView to spy on.
        const scrollIntoView = vi.fn()
        Element.prototype.scrollIntoView = scrollIntoView
        onTestFinished(() => {
          delete (Element.prototype as Partial<Element>).scrollIntoView
        })

        renderArchivePage(path)

        const march = await screen.findByRole('heading', { name: 'mars 2026' })
        expect(screen.getByRole('link', { name: 'Fin septembre' })).toBeInTheDocument()
        await vi.waitFor(() => expect(scrollIntoView).toHaveBeenCalled())
        expect(scrollIntoView.mock.contexts[0]).toBe(march)
      },
    )
  })
})
