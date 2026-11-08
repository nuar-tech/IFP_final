// src/SprintBoard.Contracts/Contracts.cs
using SprintBoard.Domain.Models;

namespace SprintBoard.Contracts;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(string Token, Guid UserId, string DisplayName);

public sealed record CreateBoardRequest(string Name);
public sealed record AddMemberRequest(string Email, Role Role);

public sealed record CreateWorkItemRequest(
    string Title, string? Description, Priority Priority, DateTime? DueDateUtc, int EstimateHours);
public sealed record EditWorkItemRequest(string Title, Priority Priority, int EstimateHours);
public sealed record MoveWorkItemRequest(WorkItemStatus Status);
public sealed record AssignRequest(Guid? AssigneeId);

public static class BoardEvents
{
    public const string ItemUpserted = "ItemUpserted";
    public const string ItemDeleted = "ItemDeleted";
}