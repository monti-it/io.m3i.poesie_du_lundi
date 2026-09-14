using PoesieDuLundi.Domain;
using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Tests.Domain;

public class PoemTests
{
    private static Poem CreatePoem(string title = "Au pied de mon arbre", string body = "Des mots simples.") =>
        new(title, body);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_or_whitespace_title_is_rejected(string title)
    {
        Assert.Throws<ArgumentException>(() => new Poem(title, "Un corps."));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_or_whitespace_body_is_rejected(string body)
    {
        Assert.Throws<ArgumentException>(() => new Poem("Un titre", body));
    }

    [Fact]
    public void Title_is_trimmed()
    {
        var poem = new Poem("  Au pied de mon arbre  ", "Des mots simples.");

        Assert.Equal("Au pied de mon arbre", poem.Title);
    }

    [Fact]
    public void Slug_is_derived_from_the_title()
    {
        var poem = new Poem("Au pied de mon arbre", "Des mots simples.");

        Assert.Equal("au-pied-de-mon-arbre", poem.Slug.Value);
    }

    [Fact]
    public void ChangeSlug_replaces_the_derived_slug()
    {
        var poem = CreatePoem();
        var slug = new Slug("un-slug-choisi-a-la-main");

        poem.ChangeSlug(slug);

        Assert.Equal(slug, poem.Slug);
    }

    [Fact]
    public void New_poems_start_as_drafts_with_no_publication_date()
    {
        var poem = CreatePoem();

        Assert.Equal(PoemStatus.Draft, poem.Status);
        Assert.Null(poem.PublicationDate);
    }

    [Fact]
    public void Series_and_author_default_to_unset()
    {
        var poem = CreatePoem();

        Assert.Null(poem.SeriesId);
        Assert.Null(poem.AuthorId);
    }

    [Fact]
    public void Series_and_author_can_be_set_at_construction()
    {
        var seriesId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        var poem = new Poem("Un titre", "Un corps.", seriesId, authorId);

        Assert.Equal(seriesId, poem.SeriesId);
        Assert.Equal(authorId, poem.AuthorId);
    }

    [Fact]
    public void Schedule_moves_a_draft_to_scheduled_with_the_given_date()
    {
        var poem = CreatePoem();
        var publicationDate = new DateOnly(2026, 9, 21);

        var result = poem.Schedule(publicationDate);

        Assert.True(result.IsSuccess);
        Assert.Equal(PoemStatus.Scheduled, poem.Status);
        Assert.Equal(publicationDate, poem.PublicationDate);
    }

    [Fact]
    public void Schedule_fails_when_the_poem_is_not_a_draft()
    {
        var poem = CreatePoem();
        poem.Schedule(new DateOnly(2026, 9, 21));

        var result = poem.Schedule(new DateOnly(2026, 9, 28));

        Assert.True(result.IsFailure);
        Assert.Equal(PoemStatus.Scheduled, poem.Status);
    }

    [Fact]
    public void Publish_moves_a_scheduled_poem_to_published_and_raises_PoemPublished()
    {
        var poem = CreatePoem();
        var publicationDate = new DateOnly(2026, 9, 21);
        poem.Schedule(publicationDate);

        var result = poem.Publish();

        Assert.True(result.IsSuccess);
        Assert.Equal(PoemStatus.Published, poem.Status);
        var raised = Assert.Single(poem.DomainEvents);
        var published = Assert.IsType<PoemPublished>(raised);
        Assert.Equal(poem.Id, published.PoemId);
        Assert.Equal(publicationDate, published.PublicationDate);
    }

    [Fact]
    public void Publish_fails_when_the_poem_is_not_scheduled()
    {
        var poem = CreatePoem();

        var result = poem.Publish();

        Assert.True(result.IsFailure);
        Assert.Equal(PoemStatus.Draft, poem.Status);
        Assert.Empty(poem.DomainEvents);
    }

    [Fact]
    public void Unpublish_moves_a_published_poem_back_to_draft_clears_the_date_and_raises_PoemUnpublished()
    {
        var poem = CreatePoem();
        poem.Schedule(new DateOnly(2026, 9, 21));
        poem.Publish();
        poem.ClearDomainEvents();

        var result = poem.Unpublish();

        Assert.True(result.IsSuccess);
        Assert.Equal(PoemStatus.Draft, poem.Status);
        Assert.Null(poem.PublicationDate);
        var raised = Assert.Single(poem.DomainEvents);
        var unpublished = Assert.IsType<PoemUnpublished>(raised);
        Assert.Equal(poem.Id, unpublished.PoemId);
    }

    [Fact]
    public void Unpublish_fails_when_the_poem_is_not_published()
    {
        var poem = CreatePoem();

        var result = poem.Unpublish();

        Assert.True(result.IsFailure);
        Assert.Equal(PoemStatus.Draft, poem.Status);
    }
}
