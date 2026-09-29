// Typed wrappers over the anonymous /api/* read endpoints (api/src/PoesieDuLundi/Api/Public),
// mirrored field-for-field from their DTOs — no server-driven schema/codegen in this app yet.
import { publicFetch } from '@shared/api/publicClient'

export interface SeriesLink {
  id: string
  title: string
  slug: string
}

export interface PoemNeighbor {
  slug: string
  title: string
}

export interface PoemSummary {
  id: string
  title: string
  slug: string
  publicationDate: string
}

export interface Poem {
  id: string
  title: string
  body: string
  slug: string
  publicationDate: string
  series: SeriesLink | null
  previous: PoemNeighbor | null
  next: PoemNeighbor | null
}

export interface PagedPoems {
  items: PoemSummary[]
  page: number
  pageSize: number
  totalCount: number
}

export interface ArchiveGroup {
  year: number
  month: number
  count: number
}

export interface Archive {
  groups: ArchiveGroup[]
  entries: PoemSummary[]
}

export interface Series {
  id: string
  title: string
  slug: string
  description: string | null
  poems: PoemSummary[]
}

export const fetchThisMondayPoem = () => publicFetch<Poem>('/api/poems/this-monday')

// `exclude` lists slugs already on screen; the server ignores it when it would leave nothing.
export const fetchRandomPoem = (exclude: readonly string[] = []) => {
  const query = new URLSearchParams(exclude.map((slug) => ['exclude', slug])).toString()
  return publicFetch<PoemSummary>(`/api/poems/random${query ? `?${query}` : ''}`)
}

export const fetchPoemBySlug = (slug: string, previewToken?: string | null) => {
  const query = previewToken ? `?preview=${encodeURIComponent(previewToken)}` : ''
  return publicFetch<Poem>(`/api/poems/${encodeURIComponent(slug)}${query}`)
}

export const fetchPoems = (params: { page?: number; pageSize?: number } = {}) => {
  const query = new URLSearchParams()
  if (params.page) query.set('page', String(params.page))
  if (params.pageSize) query.set('pageSize', String(params.pageSize))
  const queryString = query.toString()
  return publicFetch<PagedPoems>(`/api/poems${queryString ? `?${queryString}` : ''}`)
}

export const fetchArchive = (params: { year?: number; month?: number } = {}) => {
  const query = new URLSearchParams()
  if (params.year) query.set('year', String(params.year))
  if (params.month) query.set('month', String(params.month))
  const queryString = query.toString()
  return publicFetch<Archive>(`/api/archive${queryString ? `?${queryString}` : ''}`)
}

export const fetchSeriesBySlug = (slug: string) =>
  publicFetch<Series>(`/api/series/${encodeURIComponent(slug)}`)
