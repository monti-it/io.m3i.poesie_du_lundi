// A plain-text preview of a poem's Markdown body — <meta name="description"> and JSON-LD
// need prose, not Markdown syntax, and neither wants the whole poem.
const MAX_LENGTH = 160

export function excerpt(markdown: string, maxLength = MAX_LENGTH): string {
  const plainText = markdown
    .replace(/[#>*_`~]/g, '')
    .replace(/\[(.*?)\]\(.*?\)/g, '$1')
    .replace(/\s+/g, ' ')
    .trim()

  if (plainText.length <= maxLength) {
    return plainText
  }

  const truncated = plainText.slice(0, maxLength)
  const lastSpace = truncated.lastIndexOf(' ')
  return `${truncated.slice(0, lastSpace > 0 ? lastSpace : maxLength)}…`
}
