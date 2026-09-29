import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { isNotFound } from '@shared/api/errors'
import { EmptyState } from '@shared/components/EmptyState'
import { PageSkeleton } from '@shared/components/Skeleton'
import { Prose } from '@shared/components/Prose'
import { formatDate } from '@shared/lib/formatDate'
import { fetchThisMondayPoem } from '../api/publicApi'

export function HomePage() {
  const thisMonday = useQuery({ queryKey: ['poems', 'this-monday'], queryFn: fetchThisMondayPoem })

  if (thisMonday.isPending) {
    return <PageSkeleton />
  }

  if (thisMonday.isError && !isNotFound(thisMonday.error)) {
    return <EmptyState>Impossible de charger le poème du moment pour l'instant.</EmptyState>
  }

  return (
    <section>
      {thisMonday.data ? (
        <article className="poem">
          <h2>
            <Link to={`/poems/${thisMonday.data.slug}`}>{thisMonday.data.title}</Link>
          </h2>
          <p className="poem-meta">
            <time dateTime={thisMonday.data.publicationDate}>{formatDate(thisMonday.data.publicationDate)}</time>
            {thisMonday.data.series && (
              <span>
                Série : <Link to={`/series/${thisMonday.data.series.slug}`}>{thisMonday.data.series.title}</Link>
              </span>
            )}
          </p>
          <Prose>{thisMonday.data.body}</Prose>
        </article>
      ) : (
        <EmptyState>Aucun poème n'a encore été publié.</EmptyState>
      )}
    </section>
  )
}
