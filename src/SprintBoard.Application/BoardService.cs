// src/SprintBoard.Application/BoardService.cs
using SprintBoard.Domain.Common;
using SprintBoard.Domain.Models;
using SprintBoard.Domain.Rules;

namespace SprintBoard.Application;

public sealed class BoardService(IBoardRepository boards, IUserLookup users, IClock clock)
{
    public Task<IReadOnlyList<Board>> ListAsync(Guid userId, CancellationToken ct) =>
        boards.GetForUserAsync(userId, ct);

    public async Task<Result<Board>> CreateAsync(string? name, Guid userId, CancellationToken ct)
    {
        var trimmed = name?.Trim() ?? "";
        if (trimmed.Length is 0 or > 60)
            return Result.Fail<Board>(Errors.Validation("Board name must be 1 to 60 characters."));

        var board = new Board(Guid.NewGuid(), trimmed, userId, clock.UtcNow);
        await boards.AddAsync(board, new BoardMember(board.Id, userId, Role.Owner), ct);
        return Result.Ok(board);
    }

    public async Task<Result<IReadOnlyList<MemberInfo>>> MembersAsync(Guid boardId, Guid userId, CancellationToken ct)
    {
        var role = await boards.GetRoleAsync(boardId, userId, ct);
        if (role is null) return Result.Fail<IReadOnlyList<MemberInfo>>(Errors.Forbidden);
        return Result.Ok(await boards.GetMembersAsync(boardId, ct));
    }

    public async Task<Result<BoardMember>> AddMemberAsync(
        Guid boardId, string email, Role role, Guid userId, CancellationToken ct)
    {
        if (!Permissions.CanManageMembers(await boards.GetRoleAsync(boardId, userId, ct)))
            return Result.Fail<BoardMember>(Errors.Forbidden);
        if (role == Role.Owner)
            return Result.Fail<BoardMember>(Errors.Validation("A board has exactly one owner."));

        var targetId = await users.FindIdByEmailAsync(email, ct);
        if (targetId is null) return Result.Fail<BoardMember>(Errors.NotFound);
        if (await boards.GetRoleAsync(boardId, targetId.Value, ct) is not null)
            return Result.Fail<BoardMember>(Errors.Validation("This user is already a member."));

        var member = new BoardMember(boardId, targetId.Value, role);
        await boards.AddMemberAsync(member, ct);
        return Result.Ok(member);
    }
}