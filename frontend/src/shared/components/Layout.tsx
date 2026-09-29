import type { ReactNode } from 'react'
import { Link, Outlet } from 'react-router-dom'

// The public site's chrome — a static header, unlike the pages it wraps, so it renders
// immediately regardless of what a page's own data fetch is doing (App.test.tsx relies on this).
// `aside` is the side pane shown beside every page (issue #96): side by side on wide screens,
// stacked below the page on narrow ones (index.css "Site chrome").
export function Layout({ aside }: { aside?: ReactNode }) {
  return (
    <>
      <header className="site-header">
        <h1>
          <Link to="/">La poésie du lundi</Link>
        </h1>
        <nav className="site-nav">
          <Link to="/archive">Archive</Link>
        </nav>
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
