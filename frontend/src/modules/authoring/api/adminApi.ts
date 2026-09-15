// Typed wrappers over the SSO-gated /api/admin/* authoring endpoints
// (api/src/PoesieDuLundi/Api/Admin), mirrored field-for-field from their DTOs — no server-driven
// schema/codegen in this app yet (see the public counterpart, modules/reading/api/publicApi.ts).
import { adminDelete, adminFetch, adminPatch, adminPost } from '@shared/api/adminClient'

export type PoemStatus = 'Draft' | 'Scheduled' | 'Published'

export interface PoemSummary {
  id: string
  title: string
  slug: string
  status: PoemStatus
  publicationDate: string | null
  seriesId: string | null
  tags: string[]
}

export interface Poem {
  id: string
  title: string
  body: string
  slug: string
  status: PoemStatus
  publicationDate: string | null
  seriesId: string | null
  tags: string[]
}

export interface SeriesSummary {
  id: string
  title: string
  slug: string
}

export interface PoemContentInput {
  title: string
  body: string
  seriesId: string | null
}

export interface PoemUpdateInput extends PoemContentInput {
  slug: string | null
  tags: string[]
}

export const fetchAdminPoems = (status?: PoemStatus) =>
  adminFetch<PoemSummary[]>(`/api/admin/poems${status ? `?status=${status}` : ''}`)

export const fetchAdminPoem = (id: string) => adminFetch<Poem>(`/api/admin/poems/${id}`)

export const createPoem = (input: PoemContentInput) =>
  adminPost<{ id: string }>('/api/admin/poems', input)

export const updatePoem = (id: string, input: PoemUpdateInput) =>
  adminPatch<void>(`/api/admin/poems/${id}`, input)

export const deletePoem = (id: string) => adminDelete(`/api/admin/poems/${id}`)

export const schedulePoem = (id: string, date: string) =>
  adminPost<void>(`/api/admin/poems/${id}/schedule`, { date })

export const publishPoem = (id: string) => adminPost<void>(`/api/admin/poems/${id}/publish`, {})

export const unpublishPoem = (id: string) => adminPost<void>(`/api/admin/poems/${id}/unpublish`, {})

export const fetchAdminSeries = () => adminFetch<SeriesSummary[]>('/api/admin/series')
