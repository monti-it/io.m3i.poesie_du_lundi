import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { isNotFound } from '@shared/api/errors'
import { EmptyState } from '@shared/components/EmptyState'
import { PageSkeleton } from '@shared/components/Skeleton'
import { Prose } from '@shared/components/Prose'
import { formatDate } from '@shared/lib/formatDate'
import { fetchPoems, fetchThisMondayPoem } from '../api/publicApi'

const RECENT_POEMS_PAGE_SIZE = 6

export function HomePage() {
  const thisMonday = useQuery({ queryKey: ['poems', 'this-monday'], queryFn: fetchThisMondayPoem })
  const recent = useQuery({
    queryKey: ['poems', 'recent'],
    queryFn: () => fetchPoems({ page: 1, pageSize: RECENT_POEMS_PAGE_SIZE }),
  })

  if (thisMonday.isPending || recent.isPending) {
    return <PageSkeleton />
  }

  if (thisMonday.isError && !isNotFound(thisMonday.error)) {
    return <EmptyState>Impossible de charger le poème du moment pour l'instant.</EmptyState>
  }

  const recentPoems = (recent.data?.items ?? []).filter((poem) => poem.slug !== thisMonday.data?.slug)

  return (
    <>
      <section>
        {thisMonday.data ? (
          <article>
            <h2>
              <Link to={`/poems/${thisMonday.data.slug}`}>{thisMonday.data.title}</Link>
            </h2>
            <time dateTime={thisMonday.data.publicationDate}>{formatDate(thisMonday.data.publicationDate)}</time>
            {thisMonday.data.series && (
              <p>
                Série : <Link to={`/series/${thisMonday.data.series.slug}`}>{thisMonday.data.series.title}</Link>
              </p>
            )}
            <Prose>{thisMonday.data.body}</Prose>
          </article>
        ) : (
          <EmptyState>Aucun poème n'a encore été publié.</EmptyState>
        )}
      </section>

      {recentPoems.length > 0 && (
        <section>
          <h2>Poèmes récents</h2>
          <ul>
            {recentPoems.map((poem) => (
              <li key={poem.id}>
                <Link to={`/poems/${poem.slug}`}>{poem.title}</Link>{' '}
                <time dateTime={poem.publicationDate}>{formatDate(poem.publicationDate)}</time>
              </li>
            ))}
          </ul>
        </section>
      )}
    </>
  )
}
