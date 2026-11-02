// src/SprintBoard.Application/Abstractions.cs
using SprintBoard.Domain.Models;

namespace SprintBoard.Application;

public interface IClock { DateTime UtcNow { get; } }

public interface IUserLookup
{
    Task<Guid?> FindIdByEmailAsync(string email, CancellationToken ct);
}

public interface IWorkItemRepository
{
    Task<IReadOnlyList<WorkItem>> GetByBoardAsync(Guid boardId, CancellationToken ct);
    Task<WorkItem?> GetAsync(Guid id, CancellationToken ct);
    Task AddAsync(WorkItem item, CancellationToken ct);
    Task UpdateAsync(WorkItem item, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

public interface IBoardRepository
{
    Task<IReadOnlyList<Board>> GetForUserAsync(Guid userId, CancellationToken ct);
    Task<Role?> GetRoleAsync(Guid boardId, Guid userId, CancellationToken ct);
    Task AddAsync(Board board, BoardMember owner, CancellationToken ct);
    Task<IReadOnlyList<MemberInfo>> GetMembersAsync(Guid boardId, CancellationToken ct);
    Task AddMemberAsync(BoardMember member, CancellationToken ct);
}

public sealed record CreateWorkItemCommand(
    Guid BoardId, string? Title, string? Description, Priority Priority, DateTime? DueDateUtc, int EstimateHours);