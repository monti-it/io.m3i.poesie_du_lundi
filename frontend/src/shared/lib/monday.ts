// Client-side mirror of the "poems can only be scheduled for a Monday" rule
// (api/src/PoesieDuLundi/Application/SchedulePoemForMonday.cs) — lets the editor's schedule
// picker reject a non-Monday date before it ever reaches the server.

/** True when `date` (an ISO "yyyy-MM-dd" string, as produced by an `<input type="date">`) falls
 *  on a Monday. Parsed as a calendar date, never through `new Date(iso)` directly — see
 *  shared/lib/formatDate.ts for why that shifts under a negative-offset timezone. */
export function isMonday(date: string): boolean {
  const [year, month, day] = date.split('-').map(Number)
  return new Date(year, month - 1, day).getDay() === 1
}

/** The next Monday on or after today, as a sensible default for the schedule picker. */
export function nextMonday(today = new Date()): string {
  const daysUntilMonday = (8 - today.getDay()) % 7 || 7
  const monday = new Date(today.getFullYear(), today.getMonth(), today.getDate() + daysUntilMonday)
  return toIsoDate(monday)
}

/** `date` as an ISO "yyyy-MM-dd" string, built from local date parts (never `toISOString`, which
 *  reads through UTC and can shift the day — see shared/lib/formatDate.ts). */
export function toIsoDate(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}

/** Every Monday that falls within the given calendar month, in order. Used to lay out a
 *  publication calendar (one poem per Monday) without rendering a full, mostly-empty day grid. */
export function mondaysOfMonth(year: number, month: number): Date[] {
  const firstOfMonth = new Date(year, month - 1, 1)
  const offsetToMonday = (1 - firstOfMonth.getDay() + 7) % 7
  const mondays: Date[] = []
  const cursor = new Date(year, month - 1, 1 + offsetToMonday)
  while (cursor.getMonth() === month - 1) {
    mondays.push(new Date(cursor))
    cursor.setDate(cursor.getDate() + 7)
  }
  return mondays
}
