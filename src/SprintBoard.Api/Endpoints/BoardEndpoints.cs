// src/SprintBoard.Api/Endpoints/BoardEndpoints.cs
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using SprintBoard.Api.Hubs;
using SprintBoard.Application;
using SprintBoard.Contracts;
using SprintBoard.Domain.Common;
using SprintBoard.Domain.Models;

namespace SprintBoard.Api.Endpoints;

public static class BoardEndpoints
{
    public static IEndpointRouteBuilder MapBoardEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").RequireAuthorization();

        api.MapGet("/boards", async (ClaimsPrincipal user, BoardService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(user.GetUserId(), ct)));

        api.MapPost("/boards", async (CreateBoardRequest req, ClaimsPrincipal user, BoardService svc, CancellationToken ct) =>
            (await svc.CreateAsync(req.Name, user.GetUserId(), ct))
                .ToHttp(b => Results.Created($"/api/boards/{b.Id}", b)));

        api.MapGet("/boards/{boardId:guid}/members", async (Guid boardId, ClaimsPrincipal user, BoardService svc, CancellationToken ct) =>
            (await svc.MembersAsync(boardId, user.GetUserId(), ct)).ToHttp());

        api.MapGet("/boards/{boardId:guid}/permissions", async (Guid boardId, ClaimsPrincipal user, BoardService svc, CancellationToken ct) =>
            (await svc.AccessAsync(boardId, user.GetUserId(), ct)).ToHttp());

        api.MapPost("/boards/{boardId:guid}/members", async (Guid boardId, AddMemberRequest req, ClaimsPrincipal user, BoardService svc, CancellationToken ct) =>
            (await svc.AddMemberAsync(boardId, req.Email, req.Role, user.GetUserId(), ct)).ToHttp());

        api.MapGet("/boards/{boardId:guid}/items", async (Guid boardId, ClaimsPrincipal user, WorkItemService svc, CancellationToken ct) =>
            (await svc.ListAsync(boardId, user.GetUserId(), ct)).ToHttp());

        api.MapPost("/boards/{boardId:guid}/items", async (Guid boardId, CreateWorkItemRequest req, ClaimsPrincipal user,
            WorkItemService svc, IHubContext<BoardHub> hub, CancellationToken ct) =>
        {
            var cmd = new CreateWorkItemCommand(boardId, req.Title, req.Description, req.Priority, req.DueDateUtc, req.EstimateHours);
            var result = await svc.CreateAsync(cmd, user.GetUserId(), ct);
            return await BroadcastAsync(result, hub, ct, item => Results.Created($"/api/items/{item.Id}", item));
        });

        api.MapPut("/items/{itemId:guid}/move", async (Guid itemId, MoveWorkItemRequest req, ClaimsPrincipal user,
            WorkItemService svc, IHubContext<BoardHub> hub, CancellationToken ct) =>
            await BroadcastAsync(await svc.MoveAsync(itemId, req.Status, user.GetUserId(), ct), hub, ct));

        api.MapPut("/items/{itemId:guid}/edit", async (Guid itemId, EditWorkItemRequest req, ClaimsPrincipal user,
            WorkItemService svc, IHubContext<BoardHub> hub, CancellationToken ct) =>
            await BroadcastAsync(
                await svc.EditAsync(itemId, req.Title, req.Priority, req.EstimateHours, user.GetUserId(), ct),
                hub, ct));

        api.MapPut("/items/{itemId:guid}/assign", async (Guid itemId, AssignRequest req, ClaimsPrincipal user,
            WorkItemService svc, IHubContext<BoardHub> hub, CancellationToken ct) =>
            await BroadcastAsync(await svc.AssignAsync(itemId, req.AssigneeId, user.GetUserId(), ct), hub, ct));

        api.MapDelete("/items/{itemId:guid}", async (Guid itemId, ClaimsPrincipal user,
            WorkItemService svc, IHubContext<BoardHub> hub, CancellationToken ct) =>
        {
            var result = await svc.DeleteAsync(itemId, user.GetUserId(), ct);
            if (result is Result<WorkItem>.Ok ok)
                await hub.Clients.Group(ok.Value.BoardId.ToString()).SendAsync(BoardEvents.ItemDeleted, ok.Value.Id, ct);
            return result.ToHttp();
        });

        return app;
    }

    /// <summary>On success, push the new version of the item to everyone viewing the board.</summary>
    private static async Task<IResult> BroadcastAsync(
        Result<WorkItem> result, IHubContext<BoardHub> hub, CancellationToken ct, Func<WorkItem, IResult>? onOk = null)
    {
        if (result is Result<WorkItem>.Ok ok)
            await hub.Clients.Group(ok.Value.BoardId.ToString()).SendAsync(BoardEvents.ItemUpserted, ok.Value, ct);

        return result.ToHttp(onOk);
    }
}