import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link, useLocation, useSearchParams } from 'react-router-dom'
import { EmptyState } from '@shared/components/EmptyState'
import { PageSkeleton } from '@shared/components/Skeleton'
import { formatDate } from '@shared/lib/formatDate'
import { mondaysOfMonth, toIsoDate } from '@shared/lib/monday'
import { fetchArchive, type PoemSummary } from '../api/publicApi'

const monthFormatter = new Intl.DateTimeFormat('fr-FR', { month: 'long' })
const monthLabel = (month: number) => monthFormatter.format(new Date(2000, month - 1, 1))
// The list view's month headings carry this id ("2026-09"), so a month can be linked to directly.
const monthAnchor = (year: number, month: number) => `${year}-${String(month).padStart(2, '0')}`

const WEEK_COLUMNS = 5

type WeekState = 'published' | 'skipped' | 'future'

interface Week {
  date: string
  day: number
  state: WeekState
  poem: PoemSummary | null
}

interface MonthRow {
  month: number
  weeks: Week[]
}

// There is no backend concept of a "deliberately skipped" week - a past Monday with no matching
// entry and a future Monday both just read as "no poem", so the distinction is inferred here from
// today's date alone.
function buildYearGrid(year: number, entries: PoemSummary[]): MonthRow[] {
  const today = toIsoDate(new Date())
  const byDate = new Map(entries.map((poem) => [poem.publicationDate, poem]))

  return Array.from({ length: 12 }, (_, index) => {
    const month = index + 1
    const weeks = mondaysOfMonth(year, month).map((monday): Week => {
      const date = toIsoDate(monday)
      const poem = byDate.get(date) ?? null
      const state: WeekState = poem ? 'published' : date > today ? 'future' : 'skipped'
      return { date, day: monday.getDate(), state, poem }
    })
    return { month, weeks }
  })
}

interface MonthGroup {
  month: number
  poems: PoemSummary[]
}

// One group per month that has poems, newest month first and newest poem first within each.
function groupByMonth(entries: PoemSummary[]): MonthGroup[] {
  const sorted = [...entries].sort((a, b) => b.publicationDate.localeCompare(a.publicationDate))
  const groups = new Map<number, PoemSummary[]>()
  for (const poem of sorted) {
    const month = Number(poem.publicationDate.slice(5, 7))
    groups.set(month, [...(groups.get(month) ?? []), poem])
  }
  return [...groups].map(([month, poems]) => ({ month, poems }))
}

export function ArchivePage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [selected, setSelected] = useState<string | null>(null)
  const { hash } = useLocation()

  const explicitView = searchParams.get('view')
  const monthParam = searchParams.get('month')
  // A bookmarked "?year=&month=" link (the old default view) still opens the list, even without
  // an explicit "view=list".
  const view = explicitView === 'list' ? 'list' : explicitView === 'calendar' || !monthParam ? 'calendar' : 'list'

  // Both views show one year at a time and share the selected year.
  const yearParam = searchParams.get('year')
  const currentYear = new Date().getFullYear()
  const year = yearParam ? Number(yearParam) : currentYear

  const query = useQuery({
    queryKey: ['archive', year],
    queryFn: () => fetchArchive({ year }),
  })

  // A "?month=" link (the list view used to drill down month by month) or a "#2026-09" anchor
  // lands on that month's section once the year has loaded.
  const anchor = view === 'list' ? (monthParam ? monthAnchor(year, Number(monthParam)) : hash.slice(1)) : ''
  useEffect(() => {
    if (anchor && query.data) {
      document.getElementById(anchor)?.scrollIntoView()
    }
  }, [anchor, query.data])

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

  const years = [...new Set([...archive.groups.map((group) => group.year), currentYear])].sort((a, b) => b - a)
  const yearGrid = view === 'calendar' ? buildYearGrid(year, archive.entries) : []
  const monthGroups = view === 'list' ? groupByMonth(archive.entries) : []
  const selectedWeek = yearGrid.flatMap((row) => row.weeks).find((week) => week.date === selected) ?? null

  const setView = (next: 'calendar' | 'list') => {
    const params = new URLSearchParams(searchParams)
    if (next === 'calendar') {
      params.delete('view')
      params.delete('month')
    } else {
      params.set('view', 'list')
    }
    setSearchParams(params)
  }

  const setYear = (nextYear: number) => {
    const params = new URLSearchParams(searchParams)
    params.set('year', String(nextYear))
    params.delete('month')
    setSearchParams(params)
    setSelected(null)
  }

  return (
    <section>
      <h2>Archive</h2>

      <div className="archive-toolbar">
        <div className="segmented" role="tablist" aria-label="Année">
          {years.map((y) => (
            <button key={y} type="button" aria-current={y === year} onClick={() => setYear(y)}>
              {y}
            </button>
          ))}
        </div>
        <div className="segmented" role="tablist" aria-label="Vue">
          <button type="button" aria-pressed={view === 'calendar'} onClick={() => setView('calendar')}>
            Calendrier
          </button>
          <button type="button" aria-pressed={view === 'list'} onClick={() => setView('list')}>
            Liste
          </button>
        </div>
      </div>

      {view === 'calendar' ? (
        <div className="calendar">
          {yearGrid.map((row) => (
            <div className="month-row" key={row.month}>
              <span className="month-row-label">{monthLabel(row.month)}</span>
              {row.weeks.map((week) => (
                <button
                  key={week.date}
                  type="button"
                  className={`week-cell week-cell--${week.state}`}
                  disabled={week.state !== 'published'}
                  aria-pressed={selected === week.date}
                  aria-label={formatDate(week.date)}
                  onClick={() => setSelected(week.date)}
                >
                  {week.day}
                </button>
              ))}
              {Array.from({ length: WEEK_COLUMNS - row.weeks.length }, (_, index) => (
                <span key={`spacer-${index}`} className="week-cell spacer" />
              ))}
            </div>
          ))}

          <div className="calendar-legend">
            <span>
              <i className="swatch swatch--published" />
              Poème publié
            </span>
            <span>
              <i className="swatch swatch--skipped" />
              Semaine sans poème
            </span>
            <span>
              <i className="swatch swatch--future" />À venir
            </span>
          </div>

          {selectedWeek?.poem && (
            <div className="calendar-detail">
              <time dateTime={selectedWeek.date}>{formatDate(selectedWeek.date)}</time>
              <h3>
                <Link to={`/poems/${selectedWeek.poem.slug}`}>{selectedWeek.poem.title}</Link>
              </h3>
            </div>
          )}
        </div>
      ) : (
        <div className="archive-list">
          {monthGroups.length === 0 ? (
            <EmptyState>Aucun poème publié en {year}.</EmptyState>
          ) : (
            monthGroups.map((group) => (
              <section key={group.month} aria-labelledby={monthAnchor(year, group.month)}>
                <h3 id={monthAnchor(year, group.month)}>
                  {monthLabel(group.month)} {year}
                </h3>
                <ul className="poem-list">
                  {group.poems.map((poem) => (
                    <li key={poem.id}>
                      <Link to={`/poems/${poem.slug}`}>{poem.title}</Link>
                      <time dateTime={poem.publicationDate}>{formatDate(poem.publicationDate)}</time>
                    </li>
                  ))}
                </ul>
              </section>
            ))
          )}
        </div>
      )}
    </section>
  )
}
