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
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${monday.getFullYear()}-${pad(monday.getMonth() + 1)}-${pad(monday.getDate())}`
}
