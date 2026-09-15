// Poems always carry a `DateOnly` from the API ("2026-09-21") — parsed as a calendar date, never
// through `new Date(iso)` directly, which reads a bare date string as UTC midnight and can shift a
// day under a negative-offset timezone.
const formatter = new Intl.DateTimeFormat('fr-FR', { dateStyle: 'long' })

export function formatDate(iso: string): string {
  const [year, month, day] = iso.split('-').map(Number)
  return formatter.format(new Date(year, month - 1, day))
}
