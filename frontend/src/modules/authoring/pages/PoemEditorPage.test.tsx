import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { Poem } from '../api/adminApi'
import { PoemEditorPage } from './PoemEditorPage'

function jsonResponse(status: number, body: unknown) {
  return { ok: status >= 200 && status < 300, status, json: async () => body }
}

function renderEditor(initialPath: string) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialPath]}>
        <Routes>
          <Route path="/admin/poems/new" element={<PoemEditorPage />} />
          <Route path="/admin/poems/:id" element={<PoemEditorPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

const poem: Poem = {
  id: 'poem-1',
  title: 'Un poème',
  body: 'Un corps.',
  slug: 'un-poeme',
  status: 'Draft',
  publicationDate: null,
  seriesId: null,
  tags: ['hiver'],
}

describe('PoemEditorPage', () => {
  afterEach(() => {
    cleanup()
    vi.unstubAllGlobals()
  })

  it('creates a draft and navigates to its editor', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn(async (url: string, init?: RequestInit) => {
      if (url === '/api/admin/series') return jsonResponse(200, [])
      if (url === '/api/admin/poems' && init?.method === 'POST') {
        return jsonResponse(201, { id: 'poem-1' })
      }
      if (url === '/api/admin/poems/poem-1') return jsonResponse(200, poem)
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    renderEditor('/admin/poems/new')

    await user.type(screen.getByLabelText('Titre'), 'Un poème')
    await user.type(screen.getByLabelText(/Corps/), 'Un corps.')
    await user.click(screen.getByRole('button', { name: 'Créer le brouillon' }))

    await waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        '/api/admin/poems',
        expect.objectContaining({ method: 'POST' }),
      ),
    )
    expect(await screen.findByRole('heading', { name: 'Un poème' })).toBeInTheDocument()
  })

  it('loads an existing poem into the form and disables scheduling on a non-Monday', async () => {
    const fetchMock = vi.fn(async (url: string) => {
      if (url === '/api/admin/series') return jsonResponse(200, [])
      if (url === '/api/admin/poems/poem-1') return jsonResponse(200, poem)
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)
    const user = userEvent.setup()

    renderEditor('/admin/poems/poem-1')

    expect(await screen.findByDisplayValue('Un poème')).toBeInTheDocument()
    expect(screen.getByDisplayValue('un-poeme')).toBeInTheDocument()

    const dateInput = screen.getByLabelText(/Date/)
    await user.clear(dateInput)
    await user.type(dateInput, '2026-09-22')

    expect(screen.getByRole('button', { name: 'Programmer' })).toBeDisabled()
    expect(screen.getByRole('alert')).toHaveTextContent('La date doit être un lundi.')
  })

  it('renders the not-found page for an unknown poem id', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(404, { error: 'Poem not found.' })))

    renderEditor('/admin/poems/inconnu')

    expect(await screen.findByRole('heading', { name: 'Page not found' })).toBeInTheDocument()
  })
})
