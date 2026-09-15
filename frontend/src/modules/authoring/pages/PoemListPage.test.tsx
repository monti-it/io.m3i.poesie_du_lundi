import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { PoemSummary } from '../api/adminApi'
import { PoemListPage } from './PoemListPage'

function jsonResponse(status: number, body: unknown) {
  return { ok: status >= 200 && status < 300, status, json: async () => body }
}

function renderPoemListPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/admin']}>
        <Routes>
          <Route path="/admin" element={<PoemListPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('PoemListPage', () => {
  afterEach(() => {
    cleanup()
    vi.unstubAllGlobals()
  })

  it('lists poems with a status label for each', async () => {
    const poems: PoemSummary[] = [
      { id: '1', title: 'Un brouillon', slug: 'un-brouillon', status: 'Draft', publicationDate: null, seriesId: null, tags: [] },
      {
        id: '2',
        title: 'Un poème programmé',
        slug: 'un-poeme-programme',
        status: 'Scheduled',
        publicationDate: '2026-09-21',
        seriesId: null,
        tags: [],
      },
    ]
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(200, poems)))

    renderPoemListPage()

    expect(await screen.findByRole('link', { name: 'Un brouillon' })).toBeInTheDocument()
    const list = within(screen.getByRole('list'))
    expect(list.getByText('Brouillon')).toBeInTheDocument()
    expect(list.getByText(/Programmé pour le/)).toBeInTheDocument()
  })

  it('shows an empty state when there are no poems', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(200, [])))

    renderPoemListPage()

    expect(await screen.findByText("Aucun poème pour l'instant.")).toBeInTheDocument()
  })

  it('refetches with a status filter when one is selected', async () => {
    const fetchMock = vi.fn(async () => jsonResponse(200, []))
    vi.stubGlobal('fetch', fetchMock)
    const user = userEvent.setup()

    renderPoemListPage()
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/admin/poems', expect.anything()))

    await user.selectOptions(screen.getByLabelText('Statut'), 'Draft')

    await waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith('/api/admin/poems?status=Draft', expect.anything()),
    )
  })
})
