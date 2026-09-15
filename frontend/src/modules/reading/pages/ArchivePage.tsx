import { useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router-dom'
import { EmptyState } from '@shared/components/EmptyState'
import { PageSkeleton } from '@shared/components/Skeleton'
import { formatDate } from '@shared/lib/formatDate'
import { fetchArchive } from '../api/publicApi'

const monthFormatter = new Intl.DateTimeFormat('fr-FR', { month: 'long' })
const monthLabel = (month: number) => monthFormatter.format(new Date(2000, month - 1, 1))

export function ArchivePage() {
  const [searchParams] = useSearchParams()
  const yearParam = searchParams.get('year')
  const monthParam = searchParams.get('month')
  const year = yearParam ? Number(yearParam) : undefined
  const month = monthParam ? Number(monthParam) : undefined

  const query = useQuery({
    queryKey: ['archive', year, month],
    queryFn: () => fetchArchive({ year, month }),
  })

  if (query.isPending) {
    return <PageSkeleton />
  }

  if (query.isError) {
    return <EmptyState>Impossible de charger l'archive pour l'instant.</EmptyState>
  }

  const archive = query.data

  if (archive.groups.length === 0) {
    return <EmptyState>Aucun poème n'a encore été publié.</EmptyState>
  }

  return (
    <>
      <section>
        <h2>Archive</h2>
        <ul>
          {archive.groups.map((group) => (
            <li key={`${group.year}-${group.month}`}>
              <Link to={`/archive?year=${group.year}&month=${group.month}`}>
                {monthLabel(group.month)} {group.year} ({group.count})
              </Link>
            </li>
          ))}
        </ul>
      </section>

      {year && (
        <section>
          <h3>{month ? `${monthLabel(month)} ${year}` : year}</h3>
          {archive.entries.length === 0 ? (
            <EmptyState>Aucun poème pour cette période.</EmptyState>
          ) : (
            <ul>
              {archive.entries.map((poem) => (
                <li key={poem.id}>
                  <Link to={`/poems/${poem.slug}`}>{poem.title}</Link>{' '}
                  <time dateTime={poem.publicationDate}>{formatDate(poem.publicationDate)}</time>
                </li>
              ))}
            </ul>
          )}
        </section>
      )}
    </>
  )
}
