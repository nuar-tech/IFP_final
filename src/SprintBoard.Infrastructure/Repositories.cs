// src/SprintBoard.Infrastructure/Repositories.cs
using Microsoft.EntityFrameworkCore;
using SprintBoard.Application;
using SprintBoard.Domain.Models;

namespace SprintBoard.Infrastructure;

internal sealed class WorkItemRepository(AppDbContext db) : IWorkItemRepository
{
    public async Task<IReadOnlyList<WorkItem>> GetByBoardAsync(Guid boardId, CancellationToken ct) =>
        await db.WorkItems.AsNoTracking()
            .Where(i => i.BoardId == boardId)
            .OrderBy(i => i.Position)
            .ToListAsync(ct);

    public Task<WorkItem?> GetAsync(Guid id, CancellationToken ct) =>
        db.WorkItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task AddAsync(WorkItem item, CancellationToken ct)
    {
        db.WorkItems.Add(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(WorkItem item, CancellationToken ct)
    {
        db.WorkItems.Update(item);
        await db.SaveChangesAsync(ct);
    }

    public Task DeleteAsync(Guid id, CancellationToken ct) =>
        db.WorkItems.Where(i => i.Id == id).ExecuteDeleteAsync(ct);
}

internal sealed class BoardRepository(AppDbContext db) : IBoardRepository
{
    public async Task<IReadOnlyList<Board>> GetForUserAsync(Guid userId, CancellationToken ct)
    {
        var boardIds = db.BoardMembers.Where(m => m.UserId == userId).Select(m => m.BoardId);
        return await db.Boards.AsNoTracking()
            .Where(b => boardIds.Contains(b.Id))
            .OrderBy(b => b.Name)
            .ToListAsync(ct);
    }

    public Task<Role?> GetRoleAsync(Guid boardId, Guid userId, CancellationToken ct) =>
        db.BoardMembers.AsNoTracking()
            .Where(m => m.BoardId == boardId && m.UserId == userId)
            .Select(m => (Role?)m.Role)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(Board board, BoardMember owner, CancellationToken ct)
    {
        db.Boards.Add(board);
        db.BoardMembers.Add(owner);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<MemberInfo>> GetMembersAsync(Guid boardId, CancellationToken ct) =>
        await (from m in db.BoardMembers.AsNoTracking()
               where m.BoardId == boardId
               join u in db.Users on m.UserId equals u.Id
               select new MemberInfo(u.Id, u.DisplayName, m.Role)).ToListAsync(ct);

    public async Task AddMemberAsync(BoardMember member, CancellationToken ct)
    {
        db.BoardMembers.Add(member);
        await db.SaveChangesAsync(ct);
    }
}

internal sealed class UserLookup(AppDbContext db) : IUserLookup
{
    public async Task<Guid?> FindIdByEmailAsync(string email, CancellationToken ct)
    {
        var normalized = email.Trim().ToUpperInvariant();
        return await db.Users
            .Where(u => u.NormalizedEmail == normalized)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct);
    }
}

internal sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}