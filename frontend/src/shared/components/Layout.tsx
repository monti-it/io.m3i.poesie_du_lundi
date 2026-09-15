import { Link, Outlet } from 'react-router-dom'

// The public site's chrome — a static header, unlike the pages it wraps, so it renders
// immediately regardless of what a page's own data fetch is doing (App.test.tsx relies on this).
export function Layout() {
  return (
    <>
      <header>
        <h1>
          <Link to="/">La poésie du lundi</Link>
        </h1>
        <nav>
          <Link to="/archive">Archive</Link>
        </nav>
      </header>
      <main>
        <Outlet />
      </main>
    </>
  )
}
