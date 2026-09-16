import { useQuery } from '@tanstack/react-query'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { NotFoundPage } from '@app/NotFoundPage'
import { isNotFound } from '@shared/api/errors'
import { EmptyState } from '@shared/components/EmptyState'
import { PageSkeleton } from '@shared/components/Skeleton'
import { Prose } from '@shared/components/Prose'
import { excerpt } from '@shared/lib/excerpt'
import { formatDate } from '@shared/lib/formatDate'
import type { Poem } from '../api/publicApi'
import { fetchPoemBySlug } from '../api/publicApi'

const SiteName = 'La poésie du lundi'

// React 19 hoists <title>/<meta>/<link> rendered anywhere in the tree into <head> — no
// react-helmet needed. This still only runs client-side (see docs/ARCHITECTURE.md "Feeds &
// SEO" for what that does and doesn't cover for crawlers).
function PoemHead({ poem, isPreview }: { poem: Poem; isPreview: boolean }) {
  const canonicalUrl = `${window.location.origin}/poems/${poem.slug}`
  const description = excerpt(poem.body)
  const jsonLd = {
    '@context': 'https://schema.org',
    '@type': 'CreativeWork',
    name: poem.title,
    url: canonicalUrl,
    datePublished: poem.publicationDate,
    description,
    isPartOf: { '@type': 'WebSite', name: SiteName, url: window.location.origin },
  }

  return (
    <>
      <title>{`${poem.title} — ${SiteName}`}</title>
      {isPreview && <meta name="robots" content="noindex, nofollow" />}
      <meta name="description" content={description} />
      <link rel="canonical" href={canonicalUrl} />
      <meta property="og:type" content="article" />
      <meta property="og:site_name" content={SiteName} />
      <meta property="og:title" content={poem.title} />
      <meta property="og:description" content={description} />
      <meta property="og:url" content={canonicalUrl} />
      <meta name="twitter:card" content="summary" />
      <meta name="twitter:title" content={poem.title} />
      <meta name="twitter:description" content={description} />
      <script type="application/ld+json">{JSON.stringify(jsonLd)}</script>
    </>
  )
}

export function PoemPage() {
  const { slug } = useParams<{ slug: string }>()
  const [searchParams] = useSearchParams()
  const previewToken = searchParams.get('preview')
  const query = useQuery({
    queryKey: ['poem', slug, previewToken],
    queryFn: () => fetchPoemBySlug(slug!, previewToken),
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
      <PoemHead poem={poem} isPreview={Boolean(previewToken)} />
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
