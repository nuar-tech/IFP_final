// tests/SprintBoard.Api.IntegrationTests/ApiFlowTests.cs
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SprintBoard.Contracts;
using SprintBoard.Domain.Models;

namespace SprintBoard.Api.IntegrationTests;

public class ApiFlowTests(WebApplicationFactory<Program> baseFactory) : IClassFixture<WebApplicationFactory<Program>>
{
    // A fresh SQLite file per test, so tests never affect each other.
    private readonly WebApplicationFactory<Program> _factory = baseFactory.WithWebHostBuilder(b =>
        b.UseSetting("ConnectionStrings:Default",
            $"Data Source={Path.Combine(Path.GetTempPath(), $"sb-{Guid.NewGuid():N}.db")}"));

    private async Task<HttpClient> SignUpAsync(string email)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Passw0rd!x", "Test user"));
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth!.Token);
        return client;
    }

    [Fact]
    public async Task Requests_without_a_token_are_rejected()
    {
        var response = await _factory.CreateClient().GetAsync("/api/boards");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Owner_can_work_on_a_board_and_a_stranger_is_forbidden()
    {
        var owner = await SignUpAsync("owner@test.dev");
        var stranger = await SignUpAsync("stranger@test.dev");

        var boardResponse = await owner.PostAsJsonAsync("/api/boards", new CreateBoardRequest("Sprint 1"));
        Assert.Equal(HttpStatusCode.Created, boardResponse.StatusCode);
        var board = (await boardResponse.Content.ReadFromJsonAsync<Board>())!;

        var created = await owner.PostAsJsonAsync($"/api/boards/{board.Id}/items",
            new CreateWorkItemRequest("Write tests", null, Priority.High, null, 2));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var item = (await created.Content.ReadFromJsonAsync<WorkItem>())!;

        var moved = await owner.PutAsJsonAsync($"/api/items/{item.Id}/move", new MoveWorkItemRequest(WorkItemStatus.InProgress));
        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);

        var invalid = await owner.PostAsJsonAsync($"/api/boards/{board.Id}/items",
            new CreateWorkItemRequest("  ", null, Priority.Low, null, 1));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var forbidden = await stranger.GetAsync($"/api/boards/{board.Id}/items");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }
}