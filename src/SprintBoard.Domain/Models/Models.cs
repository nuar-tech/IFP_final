// src/SprintBoard.Domain/Models/Models.cs
namespace SprintBoard.Domain.Models;

public enum Role { Viewer = 0, Member = 1, Owner = 2 }
public enum Priority { Low = 0, Medium = 1, High = 2 }
public enum WorkItemStatus { Todo = 0, InProgress = 1, Done = 2 }

public sealed record Board(Guid Id, string Name, Guid OwnerId, DateTime CreatedUtc);

public sealed record BoardMember(Guid BoardId, Guid UserId, Role Role);

public sealed record MemberInfo(Guid UserId, string DisplayName, Role Role);

public sealed record WorkItem(
    Guid Id,
    Guid BoardId,
    string Title,
    string Description,
    WorkItemStatus Status,
    Priority Priority,
    Guid? AssigneeId,
    DateTime? DueDateUtc,
    int EstimateHours,
    int Position);