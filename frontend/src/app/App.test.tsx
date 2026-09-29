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

  it('renders the reading layout at the root', () => {
    renderAt('/')

    expect(screen.getByRole('heading', { name: 'La poésie du lundi' })).toBeInTheDocument()
  })

  it('links the site icon and title to the home page under the site name', () => {
    renderAt('/')

    const home = screen.getByRole('link', { name: 'La poésie du lundi' })
    expect(home).toHaveAttribute('href', '/')
    expect(home.querySelector('img')).toHaveAttribute('src', '/favicon.svg')
    expect(home.querySelector('img')).toHaveAttribute('alt', '')
  })

  it('renders the reading layout for a poem, archive, and series route', () => {
    renderAt('/poems/au-pied-de-mon-arbre')
    expect(screen.getByRole('heading', { name: 'La poésie du lundi' })).toBeInTheDocument()
    cleanup()

    renderAt('/archive')
    expect(screen.getByRole('heading', { name: 'La poésie du lundi' })).toBeInTheDocument()
    cleanup()

    renderAt('/series/une-saison')
    expect(screen.getByRole('heading', { name: 'La poésie du lundi' })).toBeInTheDocument()
  })

  it('renders the authoring route tree at /admin', () => {
    renderAt('/admin')

    expect(screen.getByRole('heading', { name: 'Poèmes' })).toBeInTheDocument()
  })

  it('renders the new-poem editor route', () => {
    renderAt('/admin/poems/new')

    expect(screen.getByRole('heading', { name: 'Nouveau poème' })).toBeInTheDocument()
  })

  it('renders a not-found page for an unknown path', () => {
    renderAt('/nonsense')

    expect(screen.getByRole('heading', { name: 'Page not found' })).toBeInTheDocument()
  })
})
