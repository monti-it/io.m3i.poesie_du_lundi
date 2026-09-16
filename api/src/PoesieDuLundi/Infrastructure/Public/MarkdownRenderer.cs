using Markdig;

namespace PoesieDuLundi.Infrastructure.Public;

/// <summary>The only server-side Markdown → HTML rendering — every other surface (the public poem
/// list/detail JSON, the admin editor) ships raw Markdown and lets <c>frontend/src/shared/
/// components/Prose.tsx</c> (react-markdown + remark-breaks) render it; a feed has no client to do
/// that, so it renders here instead. The pipeline mirrors remark-breaks: a soft line break within a
/// stanza becomes <c>&lt;br&gt;</c>, keeping line breaks intact the way commit 47ce6f9 established.</summary>
internal static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseSoftlineBreakAsHardlineBreak()
        .Build();

    public static string ToHtml(string markdown) => Markdown.ToHtml(markdown, Pipeline);
}
