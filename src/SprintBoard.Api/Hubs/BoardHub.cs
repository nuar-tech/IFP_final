// src/SprintBoard.Api/Hubs/BoardHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SprintBoard.Application;

namespace SprintBoard.Api.Hubs;

[Authorize]
public sealed class BoardHub(IBoardRepository boards) : Hub
{
    public async Task JoinBoard(Guid boardId)
    {
        var userId = Context.User!.GetUserId();
        if (await boards.GetRoleAsync(boardId, userId, Context.ConnectionAborted) is null)
            throw new HubException("You are not a member of this board.");

        await Groups.AddToGroupAsync(Context.ConnectionId, boardId.ToString());
    }

    public Task LeaveBoard(Guid boardId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, boardId.ToString());
}