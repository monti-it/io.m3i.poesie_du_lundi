using PoesieDuLundi.Domain;

namespace PoesieDuLundi.Tests.Domain;

public class AuthorTests
{
    private static Author CreateAuthor(
        string subject = "auth0|abc123",
        string displayName = "Christophe",
        string? bio = null,
        string? avatarUrl = null) =>
        new(subject, displayName, bio, avatarUrl);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_or_whitespace_subject_is_rejected(string subject)
    {
        Assert.Throws<ArgumentException>(() => new Author(subject, "Christophe"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_or_whitespace_display_name_is_rejected(string displayName)
    {
        Assert.Throws<ArgumentException>(() => new Author("auth0|abc123", displayName));
    }

    [Fact]
    public void Subject_and_display_name_are_trimmed()
    {
        var author = new Author("  auth0|abc123  ", "  Christophe  ");

        Assert.Equal("auth0|abc123", author.Subject);
        Assert.Equal("Christophe", author.DisplayName);
    }

    [Fact]
    public void Bio_and_avatar_url_default_to_null_when_not_given()
    {
        var author = CreateAuthor();

        Assert.Null(author.Bio);
        Assert.Null(author.AvatarUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_bio_is_normalized_to_null(string bio)
    {
        var author = CreateAuthor(bio: bio);

        Assert.Null(author.Bio);
    }

    [Fact]
    public void Bio_and_avatar_url_are_kept_when_given()
    {
        var author = CreateAuthor(bio: "Poète du lundi.", avatarUrl: "https://example.com/avatar.png");

        Assert.Equal("Poète du lundi.", author.Bio);
        Assert.Equal("https://example.com/avatar.png", author.AvatarUrl);
    }

    [Fact]
    public void ProvisionFor_creates_a_minimal_author_when_the_subject_is_unknown()
    {
        var author = Author.ProvisionFor("auth0|abc123", existingAuthor: null);

        Assert.Equal("auth0|abc123", author.Subject);
        Assert.Equal("auth0|abc123", author.DisplayName);
        Assert.Null(author.Bio);
        Assert.Null(author.AvatarUrl);
    }

    [Fact]
    public void ProvisionFor_returns_the_existing_author_unchanged_when_the_subject_is_already_known()
    {
        var existingAuthor = CreateAuthor(displayName: "Christophe", bio: "Poète du lundi.");

        var author = Author.ProvisionFor(existingAuthor.Subject, existingAuthor);

        Assert.Same(existingAuthor, author);
        Assert.Equal("Christophe", author.DisplayName);
        Assert.Equal("Poète du lundi.", author.Bio);
    }

    [Fact]
    public void UpdateProfile_replaces_display_name_bio_and_avatar_url()
    {
        var author = CreateAuthor(displayName: "auth0|abc123");

        author.UpdateProfile("Christophe", "Poète du lundi.", "https://example.com/avatar.png");

        Assert.Equal("Christophe", author.DisplayName);
        Assert.Equal("Poète du lundi.", author.Bio);
        Assert.Equal("https://example.com/avatar.png", author.AvatarUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateProfile_rejects_an_empty_or_whitespace_display_name(string displayName)
    {
        var author = CreateAuthor();

        Assert.Throws<ArgumentException>(() => author.UpdateProfile(displayName, null, null));
    }
}
