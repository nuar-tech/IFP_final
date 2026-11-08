// src/SprintBoard.Domain/Rules/WorkItemRules.cs
using SprintBoard.Domain.Common;
using SprintBoard.Domain.Models;

namespace SprintBoard.Domain.Rules;

/// <summary>Pure business rules: no I/O, no clock, no random ids. Same input always gives the same output.</summary>
public static class WorkItemRules
{
    public const int MaxTitleLength = 120;

    public static Result<WorkItem> Create(
        Guid id, Guid boardId, string? title, string? description, Priority priority,
        DateTime? dueDateUtc, int estimateHours, int position, DateTime nowUtc)
    {
        var trimmed = title?.Trim() ?? "";

        if (trimmed.Length == 0)
            return Result.Fail<WorkItem>(Errors.Validation("Title is required."));
        if (trimmed.Length > MaxTitleLength)
            return Result.Fail<WorkItem>(Errors.Validation($"Title must be {MaxTitleLength} characters or fewer."));
        if (!Enum.IsDefined(typeof(Priority), priority))
            return Result.Fail<WorkItem>(Errors.Validation("Task priority is invalid."));
        if (estimateHours is < 1 or > 80)
            return Result.Fail<WorkItem>(Errors.Validation("Estimate must be between 1 and 80 hours."));
        if (dueDateUtc is { } due && due < nowUtc.Date)
            return Result.Fail<WorkItem>(Errors.Validation("Due date cannot be in the past."));

        return Result.Ok(new WorkItem(
            id, boardId, trimmed, description?.Trim() ?? "", WorkItemStatus.Todo,
            priority, null, dueDateUtc, estimateHours, position));
    }

    /// <summary>Done tasks must be reopened via InProgress, never straight back to Todo.</summary>
    public static Result<WorkItem> Move(WorkItem item, WorkItemStatus target) =>
        !Enum.IsDefined(typeof(WorkItemStatus), target)
            ? Result.Fail<WorkItem>(Errors.Validation("Task status is invalid."))
            : (item.Status, target) switch
        {
            var (a, b) when a == b => Result.Ok(item),
            (WorkItemStatus.Done, WorkItemStatus.Todo) =>
                Result.Fail<WorkItem>(Errors.InvalidMove("Reopen a finished task by moving it to In progress first.")),
            _ => Result.Ok(item with { Status = target })
        };

    public static Result<WorkItem> Edit(WorkItem item, string? title, Priority priority, int estimateHours)
    {
        var trimmed = title?.Trim() ?? "";

        if (trimmed.Length == 0)
            return Result.Fail<WorkItem>(Errors.Validation("Title is required."));
        if (trimmed.Length > MaxTitleLength)
            return Result.Fail<WorkItem>(Errors.Validation($"Title must be {MaxTitleLength} characters or fewer."));
        if (!Enum.IsDefined(typeof(Priority), priority))
            return Result.Fail<WorkItem>(Errors.Validation("Task priority is invalid."));
        if (estimateHours is < 1 or > 80)
            return Result.Fail<WorkItem>(Errors.Validation("Estimate must be between 1 and 80 hours."));

        return Result.Ok(item with { Title = trimmed, Priority = priority, EstimateHours = estimateHours });
    }

    /// <summary>An open task is at risk when its deadline is near compared with the work still to do.</summary>
    public static bool IsAtRisk(WorkItem item, DateTime nowUtc) =>
        item.Status != WorkItemStatus.Done &&
        item.DueDateUtc is { } due &&
        (due - nowUtc).TotalHours <= Math.Max(48, item.EstimateHours * 2);
}