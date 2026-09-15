// Mirrors the backend's Slug.FromText (api/src/PoesieDuLundi/SharedKernel/Slug.cs) — a
// client-side preview for the editor's auto-derived slug field. The server's Slug value object
// stays the source of truth; this only has to agree with it closely enough for the preview to
// match what a save will actually produce.
export function slugify(text: string): string {
  const withoutDiacritics = text.normalize('NFD').replace(/[̀-ͯ]/g, '')
  const hyphenated = withoutDiacritics.toLowerCase().replace(/[^a-z0-9]+/g, '-')
  return hyphenated.replace(/-{2,}/g, '-').replace(/^-|-$/g, '')
}
