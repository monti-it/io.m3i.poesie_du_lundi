import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { NotFoundPage } from '@app/NotFoundPage'
import { isNotFound } from '@shared/api/errors'
import { EmptyState } from '@shared/components/EmptyState'
import { PageSkeleton } from '@shared/components/Skeleton'
import { Prose } from '@shared/components/Prose'
import { isMonday, nextMonday } from '@shared/lib/monday'
import { slugify } from '@shared/lib/slug'
import {
  createPoem,
  deletePoem,
  fetchAdminPoem,
  fetchAdminSeries,
  publishPoem,
  schedulePoem,
  unpublishPoem,
  updatePoem,
  type Poem,
  type SeriesSummary,
} from '../api/adminApi'

function SeriesPicker({
  value,
  onChange,
  series,
}: {
  value: string
  onChange: (seriesId: string) => void
  series: SeriesSummary[] | undefined
}) {
  return (
    <label>
      Série{' '}
      <select value={value} onChange={(event) => onChange(event.target.value)}>
        <option value="">Aucune</option>
        {series?.map((s) => (
          <option key={s.id} value={s.id}>
            {s.title}
          </option>
        ))}
      </select>
    </label>
  )
}

function NewPoemForm() {
  const navigate = useNavigate()
  const seriesQuery = useQuery({ queryKey: ['admin', 'series'], queryFn: fetchAdminSeries })
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')
  const [seriesId, setSeriesId] = useState('')

  const mutation = useMutation({
    mutationFn: () => createPoem({ title, body, seriesId: seriesId || null }),
    onSuccess: (created) => navigate(`/admin/poems/${created.id}`),
  })

  const slugPreview = slugify(title)
  const canSubmit = title.trim().length > 0 && body.trim().length > 0 && slugPreview.length > 0

  return (
    <section>
      <h1>Nouveau poème</h1>
      <form
        onSubmit={(event) => {
          event.preventDefault()
          mutation.mutate()
        }}
      >
        <p>
          <label>
            Titre <input value={title} onChange={(event) => setTitle(event.target.value)} required />
          </label>
        </p>
        <p>Slug : {slugPreview || '—'}</p>
        <p>
          <SeriesPicker value={seriesId} onChange={setSeriesId} series={seriesQuery.data} />
        </p>
        <p>
          <label>
            Corps
            <br />
            <textarea
              value={body}
              onChange={(event) => setBody(event.target.value)}
              rows={12}
              required
            />
          </label>
        </p>
        <Prose>{body}</Prose>
        {mutation.error && <EmptyState>{mutation.error.message}</EmptyState>}
        <button type="submit" disabled={!canSubmit || mutation.isPending}>
          Créer le brouillon
        </button>
      </form>
    </section>
  )
}

function EditPoemForm({ id }: { id: string }) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const poemQuery = useQuery({ queryKey: ['admin', 'poem', id], queryFn: () => fetchAdminPoem(id) })
  const seriesQuery = useQuery({ queryKey: ['admin', 'series'], queryFn: fetchAdminSeries })

  type FormState = { title: string; body: string; slug: string; seriesId: string; tagsText: string }

  const [form, setForm] = useState<FormState | null>(null)
  // Seeds the form once the poem loads — adjusting state during render (React's documented
  // pattern for deriving state from a prop/query result) rather than an effect, since the poem
  // never changes under this component (the route remounts it on a different :id).
  const [seededFrom, setSeededFrom] = useState<Poem | null>(null)
  if (poemQuery.data && poemQuery.data !== seededFrom) {
    setSeededFrom(poemQuery.data)
    setForm({
      title: poemQuery.data.title,
      body: poemQuery.data.body,
      slug: poemQuery.data.slug,
      seriesId: poemQuery.data.seriesId ?? '',
      tagsText: poemQuery.data.tags.join(', '),
    })
  }

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['admin', 'poem', id] })
    queryClient.invalidateQueries({ queryKey: ['admin', 'poems'] })
  }

  const saveMutation = useMutation({
    mutationFn: () => {
      const tags = form!.tagsText
        .split(',')
        .map((tag) => slugify(tag.trim()))
        .filter((tag) => tag.length > 0)
      return updatePoem(id, {
        title: form!.title,
        body: form!.body,
        slug: form!.slug || null,
        seriesId: form!.seriesId || null,
        tags,
      })
    },
    onSuccess: invalidate,
  })

  const [scheduleDate, setScheduleDate] = useState(nextMonday())
  const scheduleMutation = useMutation({
    mutationFn: () => schedulePoem(id, scheduleDate),
    onSuccess: invalidate,
  })

  const publishMutation = useMutation({ mutationFn: () => publishPoem(id), onSuccess: invalidate })

  const unpublishMutation = useMutation({
    mutationFn: () => unpublishPoem(id),
    onSuccess: invalidate,
  })

  const deleteMutation = useMutation({
    mutationFn: () => deletePoem(id),
    onSuccess: () => navigate('/admin'),
  })

  if (poemQuery.isPending) {
    return <PageSkeleton />
  }

  if (poemQuery.isError) {
    return isNotFound(poemQuery.error) ? (
      <NotFoundPage />
    ) : (
      <EmptyState>Impossible de charger ce poème pour l'instant.</EmptyState>
    )
  }

  // Unreachable once the seeding above has run (React restarts this render synchronously after a
  // render-phase setState) — kept as a type-narrowing guard for `form` below.
  if (form === null) {
    return <PageSkeleton />
  }

  const poem: Poem = poemQuery.data
  const canSave = form.title.trim().length > 0 && form.body.trim().length > 0

  return (
    <section>
      <h1>{poem.title}</h1>
      <p>Statut : {poem.status}</p>

      <form
        onSubmit={(event) => {
          event.preventDefault()
          saveMutation.mutate()
        }}
      >
        <p>
          <label>
            Titre{' '}
            <input
              value={form.title}
              onChange={(event) => setForm({ ...form, title: event.target.value })}
              required
            />
          </label>
        </p>
        <p>
          <label>
            Slug{' '}
            <input value={form.slug} onChange={(event) => setForm({ ...form, slug: event.target.value })} />
          </label>{' '}
          <button type="button" onClick={() => setForm({ ...form, slug: slugify(form.title) })}>
            Régénérer depuis le titre
          </button>
        </p>
        <p>
          <SeriesPicker
            value={form.seriesId}
            onChange={(seriesId) => setForm({ ...form, seriesId })}
            series={seriesQuery.data}
          />
        </p>
        <p>
          <label>
            Étiquettes (séparées par des virgules){' '}
            <input
              value={form.tagsText}
              onChange={(event) => setForm({ ...form, tagsText: event.target.value })}
            />
          </label>
        </p>
        <p>
          <label>
            Corps
            <br />
            <textarea
              value={form.body}
              onChange={(event) => setForm({ ...form, body: event.target.value })}
              rows={12}
              required
            />
          </label>
        </p>
        <Prose>{form.body}</Prose>
        {saveMutation.error && <EmptyState>{saveMutation.error.message}</EmptyState>}
        <button type="submit" disabled={!canSave || saveMutation.isPending}>
          Enregistrer
        </button>
      </form>

      {poem.status === 'Draft' && (
        <section>
          <h2>Programmer</h2>
          <label>
            Date (un lundi){' '}
            <input
              type="date"
              value={scheduleDate}
              onChange={(event) => setScheduleDate(event.target.value)}
            />
          </label>{' '}
          <button
            type="button"
            disabled={!isMonday(scheduleDate) || scheduleMutation.isPending}
            onClick={() => scheduleMutation.mutate()}
          >
            Programmer
          </button>
          {!isMonday(scheduleDate) && <p role="alert">La date doit être un lundi.</p>}
          {scheduleMutation.error && <EmptyState>{scheduleMutation.error.message}</EmptyState>}
        </section>
      )}

      {poem.status === 'Scheduled' && (
        <section>
          <button type="button" disabled={publishMutation.isPending} onClick={() => publishMutation.mutate()}>
            Publier maintenant
          </button>
          {publishMutation.error && <EmptyState>{publishMutation.error.message}</EmptyState>}
        </section>
      )}

      {poem.status === 'Published' && (
        <section>
          <button
            type="button"
            disabled={unpublishMutation.isPending}
            onClick={() => {
              if (window.confirm('Dépublier ce poème ?')) {
                unpublishMutation.mutate()
              }
            }}
          >
            Dépublier
          </button>
          {unpublishMutation.error && <EmptyState>{unpublishMutation.error.message}</EmptyState>}
        </section>
      )}

      <section>
        <button
          type="button"
          disabled={deleteMutation.isPending}
          onClick={() => {
            if (window.confirm('Supprimer définitivement ce poème ?')) {
              deleteMutation.mutate()
            }
          }}
        >
          Supprimer
        </button>
        {deleteMutation.error && <EmptyState>{deleteMutation.error.message}</EmptyState>}
      </section>
    </section>
  )
}

export function PoemEditorPage() {
  const { id } = useParams<{ id: string }>()
  return id ? <EditPoemForm id={id} /> : <NewPoemForm />
}
