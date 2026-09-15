import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { NotFoundPage } from '@app/NotFoundPage'
import { isNotFound } from '@shared/api/errors'
import { EmptyState } from '@shared/components/EmptyState'
import { PageSkeleton } from '@shared/components/Skeleton'
import { Prose } from '@shared/components/Prose'
import { formatDate } from '@shared/lib/formatDate'
import { fetchPoemBySlug } from '../api/publicApi'

export function PoemPage() {
  const { slug } = useParams<{ slug: string }>()
  const query = useQuery({
    queryKey: ['poem', slug],
    queryFn: () => fetchPoemBySlug(slug!),
    enabled: Boolean(slug),
  })

  if (query.isPending) {
    return <PageSkeleton />
  }

  if (query.isError) {
    return isNotFound(query.error) ? (
      <NotFoundPage />
    ) : (
      <EmptyState>Impossible de charger ce poème pour l'instant.</EmptyState>
    )
  }

  const poem = query.data

  return (
    <article className="poem">
      <h2>{poem.title}</h2>
      <p className="poem-meta">
        <time dateTime={poem.publicationDate}>{formatDate(poem.publicationDate)}</time>
        {poem.series && (
          <span>
            Série : <Link to={`/series/${poem.series.slug}`}>{poem.series.title}</Link>
          </span>
        )}
      </p>
      <Prose>{poem.body}</Prose>
      <nav className="poem-nav">
        {poem.previous && <Link to={`/poems/${poem.previous.slug}`}>← {poem.previous.title}</Link>}
        {poem.next && <Link to={`/poems/${poem.next.slug}`}>{poem.next.title} →</Link>}
      </nav>
    </article>
  )
}
