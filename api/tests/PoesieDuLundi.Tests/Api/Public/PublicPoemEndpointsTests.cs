using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PoesieDuLundi.Api.Public;
using PoesieDuLundi.Application;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;
using PoesieDuLundi.SharedKernel;
using PoesieDuLundi.Tests.Api;

namespace PoesieDuLundi.Tests.Api.Public;

public sealed class PublicPoemEndpointsTests : IClassFixture<PoesieDuLundiApiFactory>
{
    private readonly PoesieDuLundiApiFactory _factory;
    private readonly HttpClient _client;

    public PublicPoemEndpointsTests(PoesieDuLundiApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Poem> SeedPublishedPoemAsync(string title, DateOnly publicationDate)
    {
        var poem = new Poem(title, "Un corps.");
        poem.Schedule(publicationDate);
        poem.Publish();
        await AddAsync(poem);
        return poem;
    }

    private async Task<Poem> SeedDraftPoemAsync(string title)
    {
        var poem = new Poem(title, "Un corps.");
        await AddAsync(poem);
        return poem;
    }

    private async Task AddAsync(Poem poem)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PoesieDuLundiDbContext>();
        await dbContext.Poems.AddAsync(poem);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task List_returns_only_published_poems()
    {
        var published = await SeedPublishedPoemAsync("Publié un", new DateOnly(2020, 1, 6));
        await SeedDraftPoemAsync("Brouillon un");

        var response = await _client.GetAsync("/api/poems");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedPoemsDto>();
        Assert.Contains(body!.Items, poem => poem.Id == published.Id);
        Assert.DoesNotContain(body.Items, poem => poem.Title == "Brouillon un");
    }

    [Fact]
    public async Task List_filters_by_tag()
    {
        var tagged = await SeedPublishedPoemAsync("Publié étiqueté", new DateOnly(2020, 1, 6));
        tagged.ChangeTags([new Slug("amour")]);
        await SaveAsync(tagged);
        var untagged = await SeedPublishedPoemAsync("Publié sans étiquette", new DateOnly(2020, 1, 13));

        var response = await _client.GetAsync("/api/poems?tag=amour");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedPoemsDto>();
        Assert.Contains(body!.Items, poem => poem.Id == tagged.Id);
        Assert.DoesNotContain(body.Items, poem => poem.Id == untagged.Id);
    }

    private async Task SaveAsync(Poem poem)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PoesieDuLundiDbContext>();
        dbContext.Poems.Update(poem);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Get_by_slug_returns_the_poem()
    {
        var poem = await SeedPublishedPoemAsync("Poème détail public", new DateOnly(2020, 2, 3));

        var response = await _client.GetAsync($"/api/poems/{poem.Slug.Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PublicPoemDto>();
        Assert.Equal(poem.Id, body!.Id);
        Assert.Equal(poem.Body, body.Body);
    }

    [Fact]
    public async Task Get_by_slug_of_a_draft_returns_404()
    {
        var draft = await SeedDraftPoemAsync("Brouillon détail public");

        var response = await _client.GetAsync($"/api/poems/{draft.Slug.Value}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_by_an_unknown_slug_returns_404()
    {
        var response = await _client.GetAsync("/api/poems/un-slug-inconnu");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Poem> SeedScheduledPoemAsync(string title, DateOnly publicationDate)
    {
        var poem = new Poem(title, "Un corps.");
        poem.Schedule(publicationDate);
        await AddAsync(poem);
        return poem;
    }

    private string IssuePreviewToken(Guid poemId)
    {
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IPreviewTokenService>();
        return tokenService.Issue(poemId).Token;
    }

    [Fact]
    public async Task Get_by_slug_with_a_valid_preview_token_returns_a_poem_not_yet_live()
    {
        var poem = await SeedScheduledPoemAsync("Poème en révision", new DateOnly(2030, 1, 7));
        var token = IssuePreviewToken(poem.Id);

        var response = await _client.GetAsync($"/api/poems/{poem.Slug.Value}?preview={token}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PublicPoemDto>();
        Assert.Equal(poem.Id, body!.Id);
    }

    [Fact]
    public async Task Get_by_slug_with_an_invalid_preview_token_returns_404()
    {
        var poem = await SeedScheduledPoemAsync("Poème mal prévisualisé", new DateOnly(2030, 1, 14));

        var response = await _client.GetAsync($"/api/poems/{poem.Slug.Value}?preview=not-a-real-token");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_by_slug_with_another_poems_preview_token_returns_404()
    {
        var poem = await SeedScheduledPoemAsync("Poème ciblé", new DateOnly(2030, 1, 21));
        var otherPoem = await SeedScheduledPoemAsync("Un autre poème", new DateOnly(2030, 1, 28));
        var tokenForOtherPoem = IssuePreviewToken(otherPoem.Id);

        var response = await _client.GetAsync($"/api/poems/{poem.Slug.Value}?preview={tokenForOtherPoem}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task This_monday_returns_the_current_weeks_poem_when_one_exists()
    {
        var poem = await SeedPublishedPoemAsync("Poème de cette semaine", CurrentWeeksMonday());

        var response = await _client.GetAsync("/api/poems/this-monday");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PublicPoemDto>();
        Assert.Equal(poem.Id, body!.Id);
    }

    private static DateOnly CurrentWeeksMonday()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var offset = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return today.AddDays(-offset);
    }

    [Fact]
    public async Task Unfurl_returns_html_with_the_poems_og_and_twitter_tags()
    {
        var poem = await SeedPublishedPoemAsync("Poème dévoilé", new DateOnly(2020, 3, 2));

        var response = await _client.GetAsync($"/api/poems/{poem.Slug.Value}/unfurl");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("<title>Poème dévoilé — La poésie du lundi</title>", body);
        Assert.Contains("""<meta property="og:title" content="Poème dévoilé">""", body);
        Assert.Contains("""<meta property="og:type" content="article">""", body);
        Assert.Matches($"""<link rel="canonical" href="https?://[^"]+/poems/{poem.Slug.Value}">""", body);
        Assert.Contains("""<meta name="twitter:card" content="summary">""", body);
        Assert.Contains("\"@type\":\"CreativeWork\"", body);
        Assert.DoesNotContain("""<meta name="robots" content="noindex, nofollow">""", body);
    }

    [Fact]
    public async Task Unfurl_description_is_the_poems_body_with_markdown_syntax_stripped()
    {
        var poem = new Poem("Poème markdown", "# Un *poème* court");
        poem.Schedule(new DateOnly(2020, 4, 6));
        poem.Publish();
        await AddAsync(poem);

        var response = await _client.GetAsync($"/api/poems/{poem.Slug.Value}/unfurl");

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("""<meta name="description" content="Un poème court">""", body);
    }

    [Fact]
    public async Task Unfurl_of_a_draft_returns_404()
    {
        var draft = await SeedDraftPoemAsync("Brouillon dévoilé");

        var response = await _client.GetAsync($"/api/poems/{draft.Slug.Value}/unfurl");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unfurl_of_an_unknown_slug_returns_404()
    {
        var response = await _client.GetAsync("/api/poems/un-slug-inconnu/unfurl");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unfurl_with_a_valid_preview_token_marks_the_page_noindex()
    {
        var poem = await SeedScheduledPoemAsync("Poème dévoilé en révision", new DateOnly(2030, 2, 4));
        var token = IssuePreviewToken(poem.Id);

        var response = await _client.GetAsync($"/api/poems/{poem.Slug.Value}/unfurl?preview={token}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("""<meta name="robots" content="noindex, nofollow">""", body);
    }
}
