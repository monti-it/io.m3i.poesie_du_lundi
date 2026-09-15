import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { NotFoundPage } from '@app/NotFoundPage'
import { isNotFound } from '@shared/api/errors'
import { EmptyState } from '@shared/components/EmptyState'
import { PageSkeleton } from '@shared/components/Skeleton'
import { formatDate } from '@shared/lib/formatDate'
import { fetchSeriesBySlug } from '../api/publicApi'

export function SeriesPage() {
  const { slug } = useParams<{ slug: string }>()
  const query = useQuery({
    queryKey: ['series', slug],
    queryFn: () => fetchSeriesBySlug(slug!),
    enabled: Boolean(slug),
  })

  if (query.isPending) {
    return <PageSkeleton />
  }

  if (query.isError) {
    return isNotFound(query.error) ? (
      <NotFoundPage />
    ) : (
      <EmptyState>Impossible de charger cette série pour l'instant.</EmptyState>
    )
  }

  const series = query.data

  return (
    <section>
      <h2>{series.title}</h2>
      {series.description && <p>{series.description}</p>}
      {series.poems.length === 0 ? (
        <EmptyState>Aucun poème publié dans cette série pour l'instant.</EmptyState>
      ) : (
        <ul className="poem-list">
          {series.poems.map((poem) => (
            <li key={poem.id}>
              <Link to={`/poems/${poem.slug}`}>{poem.title}</Link>
              <time dateTime={poem.publicationDate}>{formatDate(poem.publicationDate)}</time>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
