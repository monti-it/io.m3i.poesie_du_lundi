import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it } from 'vitest'
import App from './App'

function renderAt(path: string) {
  return render(
    <QueryClientProvider client={new QueryClient()}>
      <MemoryRouter initialEntries={[path]}>
        <App />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('App routing', () => {
  afterEach(() => {
    cleanup()
  })

  it('renders the reading route tree at the root', () => {
    renderAt('/')

    expect(screen.getByRole('heading', { name: 'La poésie du lundi' })).toBeInTheDocument()
  })

  it('renders a poem placeholder by slug', () => {
    renderAt('/poems/au-pied-de-mon-arbre')

    expect(screen.getByText('Poem: au-pied-de-mon-arbre')).toBeInTheDocument()
  })

  it('renders the authoring route tree at /admin', () => {
    renderAt('/admin')

    expect(screen.getByRole('heading', { name: 'Admin' })).toBeInTheDocument()
  })

  it('renders a not-found page for an unknown path', () => {
    renderAt('/nonsense')

    expect(screen.getByRole('heading', { name: 'Page not found' })).toBeInTheDocument()
  })
})
