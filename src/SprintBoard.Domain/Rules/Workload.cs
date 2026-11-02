// src/SprintBoard.Domain/Rules/Workload.cs  (innovation feature: used by the heatmap on the client)
using SprintBoard.Domain.Models;

namespace SprintBoard.Domain.Rules;

public sealed record MemberLoad(Guid? UserId, int OpenItems, int OpenHours);

public static class Workload
{
    public static IReadOnlyList<MemberLoad> Compute(IEnumerable<WorkItem> items) =>
        items.Where(i => i.Status != WorkItemStatus.Done)
             .GroupBy(i => i.AssigneeId)
             .Select(g => new MemberLoad(g.Key, g.Count(), g.Sum(i => i.EstimateHours)))
             .OrderByDescending(l => l.OpenHours)
             .ToList();

    public static string Heat(int openHours) => openHours switch
    {
        <= 8 => "low",
        <= 20 => "medium",
        _ => "high"
    };
}