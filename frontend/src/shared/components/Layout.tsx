import type { ReactNode } from 'react'
import { Link, Outlet } from 'react-router-dom'

// The public site's chrome — a static header, unlike the pages it wraps, so it renders
// immediately regardless of what a page's own data fetch is doing (App.test.tsx relies on this).
// `aside` is the side pane shown beside every page (issue #96): side by side on wide screens,
// stacked below the page on narrow ones (index.css "Site chrome"). It also carries the link to the
// archive (issue #102). The site icon beside the title is decorative (`alt=""`), so the home link's
// accessible name stays the site name (issue #103).
export function Layout({ aside }: { aside?: ReactNode }) {
  return (
    <>
      <header className="site-header">
        <h1>
          <Link to="/" className="site-title">
            <img src="/favicon.svg" alt="" className="site-icon" />
            La poésie du lundi
          </Link>
        </h1>
      </header>
      <div className="site-body">
        <main className="site-main">
          <Outlet />
        </main>
        {aside && <aside className="side-pane">{aside}</aside>}
      </div>
    </>
  )
}
