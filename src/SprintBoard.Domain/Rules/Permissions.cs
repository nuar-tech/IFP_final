// src/SprintBoard.Domain/Rules/Permissions.cs
using SprintBoard.Domain.Models;

namespace SprintBoard.Domain.Rules;

public static class Permissions
{
    public static bool CanEdit(Role? role) => role is Role.Member or Role.Owner;
    public static bool CanManageMembers(Role? role) => role is Role.Owner;
}