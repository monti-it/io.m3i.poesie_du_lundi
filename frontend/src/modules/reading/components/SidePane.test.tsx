import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { Layout } from '@shared/components/Layout'
import type { PagedPoems, Poem, PoemSummary } from '../api/publicApi'
import { SidePane } from './SidePane'

function jsonResponse(status: number, body: unknown) {
  return { ok: status >= 200 && status < 300, status, json: async () => body }
}

const thisMonday: Poem = {
  id: '1',
  title: 'Poème de cette semaine',
  body: 'Un corps.',
  slug: 'poeme-de-cette-semaine',
  publicationDate: '2026-09-21',
  series: null,
  previous: null,
  next: null,
}

const recent: PagedPoems = {
  items: [
    { id: '1', title: 'Poème de cette semaine', slug: 'poeme-de-cette-semaine', publicationDate: '2026-09-21' },
    { id: '2', title: 'Poème lu', slug: 'poeme-lu', publicationDate: '2026-09-14' },
    { id: '3', title: 'Un autre poème', slug: 'un-autre-poeme', publicationDate: '2026-09-07' },
  ],
  page: 1,
  pageSize: 6,
  totalCount: 3,
}

const picks: PoemSummary[] = [
  { id: '10', title: 'Pris au hasard', slug: 'pris-au-hasard', publicationDate: '2024-01-01' },
  { id: '11', title: 'Encore un', slug: 'encore-un', publicationDate: '2023-01-02' },
]

// Stubs the three endpoints the pane reads; returns the /random URLs requested, in order.
function stubApi({ random = 'picks', recentPoems = recent }: { random?: 'picks' | 'none'; recentPoems?: PagedPoems } = {}) {
  const randomUrls: string[] = []
  vi.stubGlobal(
    'fetch',
    vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input)
      if (url === '/api/poems/this-monday') return jsonResponse(200, thisMonday)
      if (url.startsWith('/api/poems?')) return jsonResponse(200, recentPoems)
      if (url.startsWith('/api/poems/random')) {
        randomUrls.push(url)
        return random === 'none'
          ? jsonResponse(404, { error: 'No poem has been published yet.' })
          : jsonResponse(200, picks[(randomUrls.length - 1) % picks.length])
      }
      throw new Error(`Unexpected fetch to ${url}`)
    }),
  )
  return randomUrls
}

const excludedSlugs = (url: string) => new URL(url, 'http://localhost').searchParams.getAll('exclude')

function renderAt(path: string) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route element={<Layout aside={<SidePane />} />}>
            <Route path="/" element={<p>Accueil</p>} />
            <Route path="/poems/:slug" element={<p>Page du poème</p>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('SidePane', () => {
  afterEach(() => {
    cleanup()
    vi.unstubAllGlobals()
  })

  it('shows recent poems without this Monday\'s, and a random pick excluding everything shown', async () => {
    const randomUrls = stubApi()

    renderAt('/')

    const aside = await screen.findByRole('complementary')
    expect(await within(aside).findByRole('link', { name: 'Pris au hasard' })).toHaveAttribute(
      'href',
      '/poems/pris-au-hasard',
    )
    expect(within(aside).getByRole('heading', { name: 'Poèmes récents' })).toBeInTheDocument()
    expect(within(aside).getByText('Un autre poème')).toBeInTheDocument()
    expect(within(aside).queryByText('Poème de cette semaine')).not.toBeInTheDocument()
    expect(excludedSlugs(randomUrls[0])).toEqual(['poeme-de-cette-semaine', 'poeme-lu', 'un-autre-poeme'])
  })

  it('also leaves out the poem being viewed', async () => {
    const randomUrls = stubApi()

    renderAt('/poems/poeme-lu')

    const aside = await screen.findByRole('complementary')
    await within(aside).findByRole('link', { name: 'Pris au hasard' })
    expect(within(aside).queryByText('Poème lu')).not.toBeInTheDocument()
    expect(within(aside).getByText('Un autre poème')).toBeInTheDocument()
    expect(excludedSlugs(randomUrls[0])).toContain('poeme-lu')
  })

  it('re-rolls the random pick without reloading, excluding the one it replaces', async () => {
    const randomUrls = stubApi()
    renderAt('/')
    const aside = await screen.findByRole('complementary')
    await within(aside).findByRole('link', { name: 'Pris au hasard' })

    await userEvent.click(within(aside).getByRole('button', { name: 'Un autre' }))

    expect(await within(aside).findByRole('link', { name: 'Encore un' })).toBeInTheDocument()
    expect(excludedSlugs(randomUrls[1])).toContain('pris-au-hasard')
  })

  it('hides the random section when nothing can be picked', async () => {
    const randomUrls = stubApi({ random: 'none' })

    renderAt('/')

    const aside = await screen.findByRole('complementary')
    await within(aside).findByText('Un autre poème')
    await vi.waitFor(() => expect(randomUrls).toHaveLength(1))
    expect(within(aside).queryByRole('heading', { name: 'Au hasard' })).not.toBeInTheDocument()
  })

  it('links to the archive from the side pane, not the header', async () => {
    stubApi()

    renderAt('/')

    const aside = await screen.findByRole('complementary')
    expect(await within(aside).findByRole('link', { name: "Toute l'archive →" })).toHaveAttribute('href', '/archive')
    expect(within(screen.getByRole('banner')).queryByRole('link', { name: /archive/i })).not.toBeInTheDocument()
  })

  it('keeps the archive link when there is no recent poem or random pick to show', async () => {
    const randomUrls = stubApi({ random: 'none', recentPoems: { ...recent, items: [recent.items[0]], totalCount: 1 } })

    renderAt('/')

    await vi.waitFor(() => expect(randomUrls).toHaveLength(1))
    const aside = screen.getByRole('complementary')
    expect(within(aside).queryByRole('heading', { name: 'Poèmes récents' })).not.toBeInTheDocument()
    expect(within(aside).getByRole('link', { name: "Toute l'archive →" })).toHaveAttribute('href', '/archive')
  })
})
