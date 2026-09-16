using System.Net;
using System.Net.Http.Json;
using PoesieDuLundi.Api;
using PoesieDuLundi.Api.Admin;
using PoesieDuLundi.Tests.Api;

namespace PoesieDuLundi.Tests.Api.Admin;

public sealed class AdminPoemEndpointsTests : IClassFixture<PoesieDuLundiApiFactory>
{
    private const string Subject = "alice@example.com";
    private readonly HttpClient _client;

    public AdminPoemEndpointsTests(PoesieDuLundiApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static HttpRequestMessage AsAdmin(HttpMethod method, string url) =>
        new(method, url) { Headers = { { ForwardAuthIdentityProvider.UserHeader, Subject } } };

    private async Task<Guid> CreateDraftAsync(string title = "Un titre", string body = "Un corps.")
    {
        using var request = AsAdmin(HttpMethod.Post, "/api/admin/poems");
        request.Content = JsonContent.Create(new CreatePoemRequest(title, body, null));
        var response = await _client.SendAsync(request);
        var created = await response.Content.ReadFromJsonAsync<CreatedPoemDto>();
        return created!.Id;
    }

    [Fact]
    public async Task Create_without_a_Remote_User_header_is_unauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/admin/poems", new CreatePoemRequest("Un titre", "Un corps.", null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_returns_201_with_the_new_poem_location()
    {
        using var request = AsAdmin(HttpMethod.Post, "/api/admin/poems");
        request.Content = JsonContent.Create(new CreatePoemRequest("Un titre", "Un corps.", null));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreatedPoemDto>();
        Assert.Equal($"/api/admin/poems/{body!.Id}", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Create_with_an_empty_title_returns_400_with_an_error_body()
    {
        using var request = AsAdmin(HttpMethod.Post, "/api/admin/poems");
        request.Content = JsonContent.Create(new CreatePoemRequest("", "Un corps.", null));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Error));
    }

    [Fact]
    public async Task List_returns_every_created_poem()
    {
        await CreateDraftAsync("Poème listé un", "Un corps.");
        await CreateDraftAsync("Poème listé deux", "Un corps.");
        using var request = AsAdmin(HttpMethod.Get, "/api/admin/poems");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<PoemSummaryDto>>();
        Assert.True(body!.Count >= 2);
    }

    [Fact]
    public async Task List_filters_by_status()
    {
        var id = await CreateDraftAsync("Poème filtré", "Un corps.");
        using var scheduleRequest = AsAdmin(HttpMethod.Post, $"/api/admin/poems/{id}/schedule");
        scheduleRequest.Content = JsonContent.Create(new SchedulePoemRequest(NextMonday(1)));
        await _client.SendAsync(scheduleRequest);
        using var listRequest = AsAdmin(HttpMethod.Get, "/api/admin/poems?status=Scheduled");

        var response = await _client.SendAsync(listRequest);

        var body = await response.Content.ReadFromJsonAsync<List<PoemSummaryDto>>();
        Assert.Contains(body!, poem => poem.Id == id);
        Assert.All(body!, poem => Assert.Equal("Scheduled", poem.Status));
    }

    [Fact]
    public async Task Get_by_id_returns_the_poem()
    {
        var id = await CreateDraftAsync("Poème détail", "Un corps.");
        using var request = AsAdmin(HttpMethod.Get, $"/api/admin/poems/{id}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PoemDto>();
        Assert.Equal(id, body!.Id);
        Assert.Equal("Poème détail", body.Title);
    }

    [Fact]
    public async Task Get_by_an_unknown_id_returns_404()
    {
        using var request = AsAdmin(HttpMethod.Get, $"/api/admin/poems/{Guid.NewGuid()}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Error));
    }

    [Fact]
    public async Task Patch_updates_title_body_and_slug()
    {
        var id = await CreateDraftAsync("Titre original", "Corps original.");
        using var request = AsAdmin(HttpMethod.Patch, $"/api/admin/poems/{id}");
        request.Content = JsonContent.Create(
            new UpdatePoemRequest("Titre modifié", "Corps modifié.", "un-slug-choisi", null));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var getRequest = AsAdmin(HttpMethod.Get, $"/api/admin/poems/{id}");
        var getResponse = await _client.SendAsync(getRequest);
        var body = await getResponse.Content.ReadFromJsonAsync<PoemDto>();
        Assert.Equal("Titre modifié", body!.Title);
        Assert.Equal("un-slug-choisi", body.Slug);
    }

    [Fact]
    public async Task Patch_updates_tags()
    {
        var id = await CreateDraftAsync("Poème étiqueté", "Un corps.");
        using var request = AsAdmin(HttpMethod.Patch, $"/api/admin/poems/{id}");
        request.Content = JsonContent.Create(
            new UpdatePoemRequest("Poème étiqueté", "Un corps.", null, null, ["hiver", "amour"]));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var getRequest = AsAdmin(HttpMethod.Get, $"/api/admin/poems/{id}");
        var getResponse = await _client.SendAsync(getRequest);
        var body = await getResponse.Content.ReadFromJsonAsync<PoemDto>();
        Assert.Equal(["amour", "hiver"], body!.Tags);
    }

    [Fact]
    public async Task Patch_with_an_invalid_tag_returns_400()
    {
        var id = await CreateDraftAsync("Poème mal étiqueté", "Un corps.");
        using var request = AsAdmin(HttpMethod.Patch, $"/api/admin/poems/{id}");
        request.Content = JsonContent.Create(
            new UpdatePoemRequest("Poème mal étiqueté", "Un corps.", null, null, ["Not A Valid Tag!"]));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Patch_of_an_unknown_poem_returns_404()
    {
        using var request = AsAdmin(HttpMethod.Patch, $"/api/admin/poems/{Guid.NewGuid()}");
        request.Content = JsonContent.Create(new UpdatePoemRequest("Un titre", "Un corps.", null, null));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_removes_the_poem()
    {
        var id = await CreateDraftAsync("Poème à supprimer", "Un corps.");
        using var deleteRequest = AsAdmin(HttpMethod.Delete, $"/api/admin/poems/{id}");

        var response = await _client.SendAsync(deleteRequest);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var getRequest = AsAdmin(HttpMethod.Get, $"/api/admin/poems/{id}");
        var getResponse = await _client.SendAsync(getRequest);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_of_an_unknown_poem_returns_404()
    {
        using var request = AsAdmin(HttpMethod.Delete, $"/api/admin/poems/{Guid.NewGuid()}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Schedule_moves_a_draft_to_scheduled()
    {
        var id = await CreateDraftAsync("Poème à programmer", "Un corps.");
        using var request = AsAdmin(HttpMethod.Post, $"/api/admin/poems/{id}/schedule");
        request.Content = JsonContent.Create(new SchedulePoemRequest(NextMonday(5)));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Schedule_for_a_non_Monday_returns_400()
    {
        var id = await CreateDraftAsync("Poème non-lundi", "Un corps.");
        var nonMonday = NextMonday(2).AddDays(1);
        using var request = AsAdmin(HttpMethod.Post, $"/api/admin/poems/{id}/schedule");
        request.Content = JsonContent.Create(new SchedulePoemRequest(nonMonday));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Schedule_of_an_unknown_poem_returns_404()
    {
        using var request = AsAdmin(HttpMethod.Post, $"/api/admin/poems/{Guid.NewGuid()}/schedule");
        request.Content = JsonContent.Create(new SchedulePoemRequest(NextMonday(3)));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Schedule_a_second_poem_for_the_same_Monday_returns_409()
    {
        var monday = NextMonday(4);
        var first = await CreateDraftAsync("Premier poème du lundi", "Un corps.");
        using var firstRequest = AsAdmin(HttpMethod.Post, $"/api/admin/poems/{first}/schedule");
        firstRequest.Content = JsonContent.Create(new SchedulePoemRequest(monday));
        await _client.SendAsync(firstRequest);
        var second = await CreateDraftAsync("Second poème du lundi", "Un corps.");
        using var secondRequest = AsAdmin(HttpMethod.Post, $"/api/admin/poems/{second}/schedule");
        secondRequest.Content = JsonContent.Create(new SchedulePoemRequest(monday));

        var response = await _client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Publish_moves_a_scheduled_poem_to_published()
    {
        var id = await CreateDraftAsync("Poème à publier", "Un corps.");
        using var scheduleRequest = AsAdmin(HttpMethod.Post, $"/api/admin/poems/{id}/schedule");
        scheduleRequest.Content = JsonContent.Create(new SchedulePoemRequest(NextMonday(6)));
        await _client.SendAsync(scheduleRequest);
        using var publishRequest = AsAdmin(HttpMethod.Post, $"/api/admin/poems/{id}/publish");

        var response = await _client.SendAsync(publishRequest);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Publish_a_draft_poem_returns_409()
    {
        var id = await CreateDraftAsync("Poème brouillon", "Un corps.");
        using var request = AsAdmin(HttpMethod.Post, $"/api/admin/poems/{id}/publish");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Publish_of_an_unknown_poem_returns_404()
    {
        using var request = AsAdmin(HttpMethod.Post, $"/api/admin/poems/{Guid.NewGuid()}/publish");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unpublish_moves_a_published_poem_back_to_draft()
    {
        var id = await CreateDraftAsync("Poème à dépublier", "Un corps.");
        using var scheduleRequest = AsAdmin(HttpMethod.Post, $"/api/admin/poems/{id}/schedule");
        scheduleRequest.Content = JsonContent.Create(new SchedulePoemRequest(NextMonday(7)));
        await _client.SendAsync(scheduleRequest);
        using var publishRequest = AsAdmin(HttpMethod.Post, $"/api/admin/poems/{id}/publish");
        await _client.SendAsync(publishRequest);
        using var unpublishRequest = AsAdmin(HttpMethod.Post, $"/api/admin/poems/{id}/unpublish");

        var response = await _client.SendAsync(unpublishRequest);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Unpublish_a_draft_poem_returns_409()
    {
        var id = await CreateDraftAsync("Poème jamais publié", "Un corps.");
        using var request = AsAdmin(HttpMethod.Post, $"/api/admin/poems/{id}/unpublish");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Unpublish_of_an_unknown_poem_returns_404()
    {
        using var request = AsAdmin(HttpMethod.Post, $"/api/admin/poems/{Guid.NewGuid()}/unpublish");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Preview_link_returns_a_url_carrying_the_poems_slug_and_a_preview_token()
    {
        var id = await CreateDraftAsync("Poème à prévisualiser", "Un corps.");
        using var scheduleRequest = AsAdmin(HttpMethod.Post, $"/api/admin/poems/{id}/schedule");
        scheduleRequest.Content = JsonContent.Create(new SchedulePoemRequest(NextMonday(8)));
        await _client.SendAsync(scheduleRequest);
        using var getRequest = AsAdmin(HttpMethod.Get, $"/api/admin/poems/{id}");
        var poem = await (await _client.SendAsync(getRequest)).Content.ReadFromJsonAsync<PoemDto>();
        using var request = AsAdmin(HttpMethod.Post, $"/api/admin/poems/{id}/preview-link");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PreviewLinkDto>();
        Assert.Contains($"/poems/{poem!.Slug}?preview=", body!.Url);
        Assert.True(body.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Preview_link_for_a_poem_not_yet_scheduled_returns_409()
    {
        var id = await CreateDraftAsync("Poème brouillon sans date", "Un corps.");
        using var request = AsAdmin(HttpMethod.Post, $"/api/admin/poems/{id}/preview-link");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Preview_link_for_an_unknown_poem_returns_404()
    {
        using var request = AsAdmin(HttpMethod.Post, $"/api/admin/poems/{Guid.NewGuid()}/preview-link");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>A Monday <paramref name="weeksAhead"/> weeks out. Tests share one in-memory database
    /// via <see cref="IClassFixture{T}"/> and the one-poem-per-Monday rule is global, so every test
    /// that schedules a poem needs its own week to avoid tripping over another test's poem.</summary>
    private static DateOnly NextMonday(int weeksAhead = 1)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        daysUntilMonday = daysUntilMonday == 0 ? 7 : daysUntilMonday;
        return today.AddDays(daysUntilMonday + ((weeksAhead - 1) * 7));
    }
}
