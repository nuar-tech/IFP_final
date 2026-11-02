// src/SprintBoard.Application/WorkItemService.cs
using SprintBoard.Domain.Common;
using SprintBoard.Domain.Models;
using SprintBoard.Domain.Rules;

namespace SprintBoard.Application;

public sealed class WorkItemService(IWorkItemRepository items, IBoardRepository boards, IClock clock)
{
    public async Task<Result<IReadOnlyList<WorkItem>>> ListAsync(Guid boardId, Guid userId, CancellationToken ct)
    {
        var role = await boards.GetRoleAsync(boardId, userId, ct);
        if (role is null) return Result.Fail<IReadOnlyList<WorkItem>>(Errors.Forbidden);
        return Result.Ok(await items.GetByBoardAsync(boardId, ct));
    }

    public async Task<Result<WorkItem>> CreateAsync(CreateWorkItemCommand cmd, Guid userId, CancellationToken ct)
    {
        var role = await boards.GetRoleAsync(cmd.BoardId, userId, ct);
        if (!Permissions.CanEdit(role)) return Result.Fail<WorkItem>(Errors.Forbidden);

        var existing = await items.GetByBoardAsync(cmd.BoardId, ct);
        var position = existing.Count(i => i.Status == WorkItemStatus.Todo);

        var created = WorkItemRules.Create(
            Guid.NewGuid(), cmd.BoardId, cmd.Title, cmd.Description, cmd.Priority,
            cmd.DueDateUtc, cmd.EstimateHours, position, clock.UtcNow);

        if (created is Result<WorkItem>.Ok ok) await items.AddAsync(ok.Value, ct);
        return created;
    }

    public async Task<Result<WorkItem>> MoveAsync(Guid itemId, WorkItemStatus target, Guid userId, CancellationToken ct)
    {
        var loaded = await LoadEditableAsync(itemId, userId, ct);
        return await PersistAsync(loaded.Bind(item => WorkItemRules.Move(item, target)), ct);
    }

    public async Task<Result<WorkItem>> AssignAsync(Guid itemId, Guid? assigneeId, Guid userId, CancellationToken ct)
    {
        var loaded = await LoadEditableAsync(itemId, userId, ct);

        if (loaded is Result<WorkItem>.Ok { Value: var current } &&
            assigneeId is { } assignee &&
            await boards.GetRoleAsync(current.BoardId, assignee, ct) is null)
        {
            return Result.Fail<WorkItem>(Errors.Validation("Assignee must be a member of the board."));
        }

        return await PersistAsync(loaded.Map(i => i with { AssigneeId = assigneeId }), ct);
    }

    public async Task<Result<WorkItem>> DeleteAsync(Guid itemId, Guid userId, CancellationToken ct)
    {
        var loaded = await LoadEditableAsync(itemId, userId, ct);
        if (loaded is Result<WorkItem>.Ok ok) await items.DeleteAsync(ok.Value.Id, ct);
        return loaded;
    }

    private async Task<Result<WorkItem>> LoadEditableAsync(Guid itemId, Guid userId, CancellationToken ct)
    {
        var item = await items.GetAsync(itemId, ct);
        if (item is null) return Result.Fail<WorkItem>(Errors.NotFound);

        var role = await boards.GetRoleAsync(item.BoardId, userId, ct);
        return Permissions.CanEdit(role) ? Result.Ok(item) : Result.Fail<WorkItem>(Errors.Forbidden);
    }

    private async Task<Result<WorkItem>> PersistAsync(Result<WorkItem> result, CancellationToken ct)
    {
        if (result is Result<WorkItem>.Ok ok) await items.UpdateAsync(ok.Value, ct);
        return result;
    }
}