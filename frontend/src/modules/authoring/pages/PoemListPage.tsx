import { useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router-dom'
import { EmptyState } from '@shared/components/EmptyState'
import { Skeleton } from '@shared/components/Skeleton'
import { formatDate } from '@shared/lib/formatDate'
import { fetchAdminPoems, type PoemStatus, type PoemSummary } from '../api/adminApi'

const statusFilters: { value: PoemStatus | ''; label: string }[] = [
  { value: '', label: 'Tous' },
  { value: 'Draft', label: 'Brouillon' },
  { value: 'Scheduled', label: 'Programmé' },
  { value: 'Published', label: 'Publié' },
]

function statusLabel(poem: PoemSummary): string {
  switch (poem.status) {
    case 'Draft':
      return 'Brouillon'
    case 'Scheduled':
      return `Programmé pour le ${formatDate(poem.publicationDate!)}`
    case 'Published':
      return `Publié le ${formatDate(poem.publicationDate!)}`
  }
}

export function PoemListPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const statusParam = searchParams.get('status') as PoemStatus | null
  const status = statusParam ?? undefined

  const query = useQuery({
    queryKey: ['admin', 'poems', status ?? null],
    queryFn: () => fetchAdminPoems(status),
  })

  return (
    <section>
      <header>
        <h1>Poèmes</h1>
        <Link to="/admin/poems/new">+ Nouveau poème</Link>
      </header>

      <label>
        Statut{' '}
        <select
          value={statusParam ?? ''}
          onChange={(event) => {
            const value = event.target.value
            setSearchParams(value ? { status: value } : {})
          }}
        >
          {statusFilters.map((filter) => (
            <option key={filter.value} value={filter.value}>
              {filter.label}
            </option>
          ))}
        </select>
      </label>

      {query.isPending && <Skeleton lines={4} />}

      {query.isError && <EmptyState>Impossible de charger les poèmes pour l'instant.</EmptyState>}

      {query.isSuccess &&
        (query.data.length === 0 ? (
          <EmptyState>Aucun poème pour l'instant.</EmptyState>
        ) : (
          <ul>
            {query.data.map((poem) => (
              <li key={poem.id}>
                <Link to={`/admin/poems/${poem.id}`}>{poem.title}</Link> <span>{statusLabel(poem)}</span>
              </li>
            ))}
          </ul>
        ))}
    </section>
  )
}
