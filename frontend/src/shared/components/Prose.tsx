import Markdown from 'react-markdown'
import remarkBreaks from 'remark-breaks'

// A poem's body, rendered from Markdown. A blank line starts a new stanza (its own paragraph);
// remark-breaks keeps a single line break inside a stanza as a line break too, since a poem's
// line breaks are part of the poem and must survive rendering.
export function Prose({ children }: { children: string }) {
  return (
    <div className="prose">
      <Markdown remarkPlugins={[remarkBreaks]}>{children}</Markdown>
    </div>
  )
}
