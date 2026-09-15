// A poem's body, line breaks preserved. Not a Markdown renderer — poem bodies in this app are
// plain text (the admin Markdown editor and a real renderer are future work), and a poem's line
// breaks are part of the poem, so they must survive regardless.
export function Prose({ children }: { children: string }) {
  return <p style={{ whiteSpace: 'pre-wrap' }}>{children}</p>
}
