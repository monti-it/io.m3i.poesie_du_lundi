import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useMatch } from 'react-router-dom'
import { formatDate } from '@shared/lib/formatDate'
import type { PoemSummary } from '../api/publicApi'
import { fetchPoems, fetchRandomPoem, fetchThisMondayPoem } from '../api/publicApi'

const RECENT_POEMS_PAGE_SIZE = 6

// The public site's secondary sections (issue #96) — "Poèmes récents" and "Au hasard" — shown
// beside every public page by Layout. Shares the this-Monday query key with HomePage, so the home
// page doesn't fetch it twice.
export function SidePane() {
  const queryClient = useQueryClient()
  const viewedSlug = useMatch('/poems/:slug')?.params.slug

  const thisMonday = useQuery({ queryKey: ['poems', 'this-monday'], queryFn: fetchThisMondayPoem })
  const recent = useQuery({
    queryKey: ['poems', 'recent'],
    queryFn: () => fetchPoems({ page: 1, pageSize: RECENT_POEMS_PAGE_SIZE }),
  })

  const shownSlugs = [thisMonday.data?.slug, viewedSlug].filter((slug): slug is string => !!slug)
  const recentPoems = (recent.data?.items ?? []).filter((poem) => !shownSlugs.includes(poem.slug))
  const exclude = [...shownSlugs, ...recentPoems.map((poem) => poem.slug)]

  // One pick per set of excluded slugs, kept until re-rolled — never refetched behind the
  // reader's back on focus/remount. Re-rolling also excludes the pick being replaced.
  const randomKey = ['poems', 'random', exclude]
  const random = useQuery({
    queryKey: randomKey,
    queryFn: () => {
      const previous = queryClient.getQueryData<PoemSummary>(randomKey)
      return fetchRandomPoem(previous ? [...exclude, previous.slug] : exclude)
    },
    enabled: !thisMonday.isPending && !recent.isPending,
    staleTime: Infinity,
    refetchOnWindowFocus: false,
    retry: false,
  })

  // Always rendered, so every public page links to the archive (issue #102) — at the foot of
  // "Poèmes récents" when there is one, on its own otherwise.
  const archiveLink = (
    <p className="archive-link">
      <Link to="/archive">Toute l'archive →</Link>
    </p>
  )

  return (
    <>
      {recentPoems.length > 0 ? (
        <section>
          <h2>Poèmes récents</h2>
          <ul className="poem-list">
            {recentPoems.map((poem) => (
              <li key={poem.id}>
                <Link to={`/poems/${poem.slug}`}>{poem.title}</Link>
                <time dateTime={poem.publicationDate}>{formatDate(poem.publicationDate)}</time>
              </li>
            ))}
          </ul>
          {archiveLink}
        </section>
      ) : (
        archiveLink
      )}

      {random.data && (
        <section>
          <h2>Au hasard</h2>
          <p className="random-poem">
            <Link to={`/poems/${random.data.slug}`}>{random.data.title}</Link>
          </p>
          <button
            type="button"
            className="reroll"
            onClick={() => void random.refetch()}
            disabled={random.isFetching}
          >
            Un autre
          </button>
        </section>
      )}
    </>
  )
}
