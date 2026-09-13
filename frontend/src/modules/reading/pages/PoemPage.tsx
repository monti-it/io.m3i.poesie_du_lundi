import { useParams } from 'react-router-dom'

// Placeholder — the real poem permalink (fetched by slug, OpenGraph/SEO tags) lands in #21/#24.
export function PoemPage() {
  const { slug } = useParams<{ slug: string }>()

  return (
    <section>
      <p>Poem: {slug}</p>
    </section>
  )
}
