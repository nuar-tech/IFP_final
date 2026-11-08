// src/SprintBoard.Domain/Rules/Permissions.cs
using SprintBoard.Domain.Models;

namespace SprintBoard.Domain.Rules;

public static class Permissions
{
    public static bool CanManageTasks(Role? role) => role is Role.Owner;
    public static bool CanMoveTask(Role? role, Guid userId, WorkItem item) =>
        role is Role.Owner || role is Role.Member && item.AssigneeId == userId;
    public static bool CanManageMembers(Role? role) => role is Role.Owner;
}