// src/SprintBoard.Client/Services/BoardHubClient.cs
using Microsoft.AspNetCore.SignalR.Client;
using SprintBoard.Client.State;
using SprintBoard.Contracts;
using SprintBoard.Domain.Models;

namespace SprintBoard.Client.Services;

public sealed class BoardHubClient(Store store, TokenStore tokens, ApiSettings settings) : IAsyncDisposable
{
    private HubConnection? _connection;

    public async Task ConnectAsync(Guid boardId)
    {
        await DisposeAsync();

        var connection = new HubConnectionBuilder()
            .WithUrl($"{settings.BaseUrl.TrimEnd('/')}/hubs/board",
                options => options.AccessTokenProvider = async () => await tokens.GetAsync())
            .WithAutomaticReconnect()
            .Build();

        // Server events become store actions; the reducer is idempotent, so duplicates are harmless.
        connection.On<WorkItem>(BoardEvents.ItemUpserted, item => store.Dispatch(new ItemUpserted(item)));
        connection.On<Guid>(BoardEvents.ItemDeleted, id => store.Dispatch(new ItemRemoved(id)));

        // After a dropped connection the server forgets our group membership: join again.
        connection.Reconnected += _ => connection.InvokeAsync("JoinBoard", boardId);

        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId);
        _connection = connection;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is null) return;
        await _connection.DisposeAsync();
        _connection = null;
    }
}
