// src/SprintBoard.Client/Services/ApiClient.cs
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SprintBoard.Contracts;
using SprintBoard.Domain.Common;
using SprintBoard.Domain.Models;

namespace SprintBoard.Client.Services;

public sealed class ApiClient(HttpClient http)
{
    // ---- generic helpers -------------------------------------------------
    public Task<Result<T>> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url, null);
    public Task<Result<T>> PostAsync<T>(string url, object body) => SendAsync<T>(HttpMethod.Post, url, body);
    public Task<Result<T>> PutAsync<T>(string url, object body) => SendAsync<T>(HttpMethod.Put, url, body);
    public Task<Result<T>> DeleteAsync<T>(string url) => SendAsync<T>(HttpMethod.Delete, url, null);

    // ---- typed endpoints used by the pages -------------------------------
    public Task<Result<IReadOnlyList<Board>>> GetBoardsAsync() =>
        GetAsync<IReadOnlyList<Board>>("api/boards");

    public Task<Result<Board>> CreateBoardAsync(string name) =>
        PostAsync<Board>("api/boards", new CreateBoardRequest(name));

    public Task<Result<IReadOnlyList<MemberInfo>>> GetMembersAsync(Guid boardId) =>
        GetAsync<IReadOnlyList<MemberInfo>>($"api/boards/{boardId}/members");

    public Task<Result<IReadOnlyList<WorkItem>>> GetItemsAsync(Guid boardId) =>
        GetAsync<IReadOnlyList<WorkItem>>($"api/boards/{boardId}/items");

    public Task<Result<WorkItem>> CreateItemAsync(Guid boardId, CreateWorkItemRequest request) =>
        PostAsync<WorkItem>($"api/boards/{boardId}/items", request);

    public Task<Result<WorkItem>> MoveAsync(Guid itemId, WorkItemStatus status) =>
        PutAsync<WorkItem>($"api/items/{itemId}/move", new MoveWorkItemRequest(status));

    public Task<Result<WorkItem>> AssignAsync(Guid itemId, Guid? assigneeId) =>
        PutAsync<WorkItem>($"api/items/{itemId}/assign", new AssignRequest(assigneeId));

    public Task<Result<WorkItem>> DeleteItemAsync(Guid itemId) =>
        DeleteAsync<WorkItem>($"api/items/{itemId}");

    // ---- plumbing: expected failures become values, never exceptions -----
    private async Task<Result<T>> SendAsync<T>(HttpMethod method, string url, object? body)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            if (body is not null) request.Content = JsonContent.Create(body, body.GetType());

            using var response = await http.SendAsync(request);
            return await ReadAsync<T>(response);
        }
        catch (HttpRequestException)
        {
            return Result.Fail<T>("network", "Cannot reach the server. Check your connection and try again.");
        }
    }

    private static async Task<Result<T>> ReadAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            return Result.Fail<T>(await ReadErrorAsync(response));

        var value = await response.Content.ReadFromJsonAsync<T>();
        return value is null
            ? Result.Fail<T>("empty", "The server sent an empty response.")
            : Result.Ok<T>(value);
    }

    private static async Task<Error> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<Error>();
            if (body is { Message.Length: > 0 }) return body;
        }
        catch (Exception e) when (e is JsonException or NotSupportedException)
        {
            // body was empty or not JSON: fall through to the generic messages below
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new Error("unauthorized", "Please sign in again."),
            HttpStatusCode.Forbidden => new Error(ErrorCodes.Forbidden, "You do not have permission for this action."),
            HttpStatusCode.NotFound => new Error(ErrorCodes.NotFound, "That item no longer exists."),
            _ => new Error("http_error", "Something went wrong. Please try again.")
        };
    }
}
