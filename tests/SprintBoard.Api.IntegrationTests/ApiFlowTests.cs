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
        var (client, _) = await SignUpWithAuthAsync(email);
        return client;
    }

    private async Task<(HttpClient Client, AuthResponse Auth)> SignUpWithAuthAsync(string email)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Passw0rd!x", "Test user"));
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth!.Token);
        return (client, auth);
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

    [Fact]
    public async Task Owner_can_add_a_member_and_assign_a_task_to_them()
    {
        var owner = await SignUpAsync("owner-members@test.dev");
        var (member, memberAuth) = await SignUpWithAuthAsync("member@test.dev");
        var (otherMember, _) = await SignUpWithAuthAsync("other-member@test.dev");

        var boardResponse = await owner.PostAsJsonAsync("/api/boards", new CreateBoardRequest("Sprint 2"));
        var board = (await boardResponse.Content.ReadFromJsonAsync<Board>())!;

        var permissions = await owner.GetFromJsonAsync<BoardAccess>(
            $"/api/boards/{board.Id}/permissions");
        Assert.Equal(Role.Owner, permissions!.Role);

        var addResponse = await owner.PostAsJsonAsync(
            $"/api/boards/{board.Id}/members", new AddMemberRequest("member@test.dev", Role.Member));
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        var memberPermissions = await member.GetFromJsonAsync<BoardAccess>(
            $"/api/boards/{board.Id}/permissions");
        Assert.Equal(Role.Member, memberPermissions!.Role);

        var forbidden = await member.PostAsJsonAsync(
            $"/api/boards/{board.Id}/members", new AddMemberRequest("another@test.dev", Role.Member));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var members = await owner.GetFromJsonAsync<IReadOnlyList<MemberInfo>>(
            $"/api/boards/{board.Id}/members");
        Assert.Contains(members!, m => m.UserId == memberAuth.UserId && m.Role == Role.Member);

        var itemResponse = await owner.PostAsJsonAsync(
            $"/api/boards/{board.Id}/items", new CreateWorkItemRequest("Review work", null, Priority.Medium, null, 2));
        var item = (await itemResponse.Content.ReadFromJsonAsync<WorkItem>())!;

        var memberCreate = await member.PostAsJsonAsync(
            $"/api/boards/{board.Id}/items", new CreateWorkItemRequest("Not allowed", null, Priority.Low, null, 1));
        Assert.Equal(HttpStatusCode.Forbidden, memberCreate.StatusCode);

        var assignResponse = await owner.PutAsJsonAsync(
            $"/api/items/{item.Id}/assign", new AssignRequest(memberAuth.UserId));
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);
        Assert.Equal(memberAuth.UserId, (await assignResponse.Content.ReadFromJsonAsync<WorkItem>())!.AssigneeId);

        var unassignedMove = await otherMember.PutAsJsonAsync(
            $"/api/items/{item.Id}/move", new MoveWorkItemRequest(WorkItemStatus.InProgress));
        Assert.Equal(HttpStatusCode.Forbidden, unassignedMove.StatusCode);

        var memberEdit = await member.PutAsJsonAsync(
            $"/api/items/{item.Id}/edit", new EditWorkItemRequest("Changed", Priority.High, 5));
        Assert.Equal(HttpStatusCode.Forbidden, memberEdit.StatusCode);

        var memberAssign = await member.PutAsJsonAsync(
            $"/api/items/{item.Id}/assign", new AssignRequest(null));
        Assert.Equal(HttpStatusCode.Forbidden, memberAssign.StatusCode);

        var memberMove = await member.PutAsJsonAsync(
            $"/api/items/{item.Id}/move", new MoveWorkItemRequest(WorkItemStatus.InProgress));
        Assert.Equal(HttpStatusCode.OK, memberMove.StatusCode);

        var memberDelete = await member.DeleteAsync($"/api/items/{item.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, memberDelete.StatusCode);

        var ownerEdit = await owner.PutAsJsonAsync(
            $"/api/items/{item.Id}/edit", new EditWorkItemRequest("Review the release", Priority.High, 6));
        Assert.Equal(HttpStatusCode.OK, ownerEdit.StatusCode);
        var updated = (await ownerEdit.Content.ReadFromJsonAsync<WorkItem>())!;
        Assert.Equal("Review the release", updated.Title);
        Assert.Equal(Priority.High, updated.Priority);
        Assert.Equal(6, updated.EstimateHours);
        Assert.Equal(memberAuth.UserId, updated.AssigneeId);
        Assert.Equal(WorkItemStatus.InProgress, updated.Status);

        var ownerDelete = await owner.DeleteAsync($"/api/items/{item.Id}");
        Assert.Equal(HttpStatusCode.OK, ownerDelete.StatusCode);
    }

    [Fact]
    public async Task Email_addresses_are_case_sensitive_for_registration_and_member_lookup()
    {
        var owner = await SignUpAsync("owner-email-case@test.dev");
        var (_, lowerAuth) = await SignUpWithAuthAsync("student01@mail.ru");
        var duplicate = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest("student01@mail.ru", "Passw0rd!x", "Duplicate"));
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);

        var (_, upperAuth) = await SignUpWithAuthAsync("Student01@mail.ru");
        Assert.NotEqual(lowerAuth.UserId, upperAuth.UserId);

        var boardResponse = await owner.PostAsJsonAsync("/api/boards", new CreateBoardRequest("Email case"));
        var board = (await boardResponse.Content.ReadFromJsonAsync<Board>())!;

        var addResponse = await owner.PostAsJsonAsync(
            $"/api/boards/{board.Id}/members", new AddMemberRequest("Student01@mail.ru", Role.Member));
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        var members = await owner.GetFromJsonAsync<IReadOnlyList<MemberInfo>>(
            $"/api/boards/{board.Id}/members");
        Assert.Contains(members!, m => m.UserId == upperAuth.UserId);
        Assert.DoesNotContain(members!, m => m.UserId == lowerAuth.UserId);

        var login = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("Student01@mail.ru", "Passw0rd!x"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loggedIn = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(upperAuth.UserId, loggedIn!.UserId);
    }
}