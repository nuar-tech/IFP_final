// tests/SprintBoard.Domain.Tests/WorkItemRulesTests.cs
using SprintBoard.Domain.Common;
using SprintBoard.Domain.Models;
using SprintBoard.Domain.Rules;

namespace SprintBoard.Domain.Tests;

public class WorkItemRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);

    private static WorkItem Sample(WorkItemStatus status = WorkItemStatus.Todo, DateTime? due = null,
                                   int hours = 2, Guid? assignee = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Task", "", status, Priority.Medium, assignee, due, hours, 0);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_rejects_a_blank_title(string? title)
    {
        var result = WorkItemRules.Create(Guid.NewGuid(), Guid.NewGuid(), title, null, Priority.Low, null, 1, 0, Now);
        Assert.IsType<Result<WorkItem>.Fail>(result);
    }

    [Fact]
    public void Create_trims_the_title_and_starts_in_todo()
    {
        var result = WorkItemRules.Create(Guid.NewGuid(), Guid.NewGuid(), "  Write docs ", null, Priority.High, null, 3, 0, Now);
        var ok = Assert.IsType<Result<WorkItem>.Ok>(result);
        Assert.Equal("Write docs", ok.Value.Title);
        Assert.Equal(WorkItemStatus.Todo, ok.Value.Status);
    }

    [Fact]
    public void Create_rejects_a_due_date_in_the_past()
    {
        var result = WorkItemRules.Create(Guid.NewGuid(), Guid.NewGuid(), "Task", null, Priority.Low, Now.AddDays(-2), 1, 0, Now);
        Assert.IsType<Result<WorkItem>.Fail>(result);
    }

    [Fact]
    public void Create_rejects_an_invalid_priority()
    {
        var result = WorkItemRules.Create(Guid.NewGuid(), Guid.NewGuid(), "Task", null, (Priority)99, null, 1, 0, Now);
        Assert.IsType<Result<WorkItem>.Fail>(result);
    }

    [Fact]
    public void Edit_updates_title_priority_and_estimate_without_changing_assignment_or_status()
    {
        var assignee = Guid.NewGuid();
        var original = Sample(WorkItemStatus.InProgress, assignee: assignee);

        var edited = Assert.IsType<Result<WorkItem>.Ok>(
            WorkItemRules.Edit(original, "  Updated task  ", Priority.High, 8));

        Assert.Equal("Updated task", edited.Value.Title);
        Assert.Equal(Priority.High, edited.Value.Priority);
        Assert.Equal(8, edited.Value.EstimateHours);
        Assert.Equal(assignee, edited.Value.AssigneeId);
        Assert.Equal(WorkItemStatus.InProgress, edited.Value.Status);
    }

    [Fact]
    public void Edit_rejects_invalid_estimates()
    {
        var result = WorkItemRules.Edit(Sample(), "Updated task", Priority.High, 0);
        Assert.IsType<Result<WorkItem>.Fail>(result);
    }

    [Fact]
    public void Move_from_done_straight_to_todo_is_rejected()
    {
        var result = WorkItemRules.Move(Sample(WorkItemStatus.Done), WorkItemStatus.Todo);
        var fail = Assert.IsType<Result<WorkItem>.Fail>(result);
        Assert.Equal(ErrorCodes.InvalidMove, fail.Error.Code);
    }

    [Fact]
    public void Move_returns_a_new_item_and_never_mutates_the_original()
    {
        var original = Sample();
        var moved = Assert.IsType<Result<WorkItem>.Ok>(WorkItemRules.Move(original, WorkItemStatus.InProgress));

        Assert.Equal(WorkItemStatus.Todo, original.Status);
        Assert.Equal(WorkItemStatus.InProgress, moved.Value.Status);
    }

    [Fact]
    public void An_overdue_open_item_is_at_risk() =>
        Assert.True(WorkItemRules.IsAtRisk(Sample(due: Now.AddHours(-1)), Now));

    [Fact]
    public void A_finished_item_is_never_at_risk() =>
        Assert.False(WorkItemRules.IsAtRisk(Sample(WorkItemStatus.Done, due: Now.AddHours(-1)), Now));

    [Fact]
    public void Workload_sums_open_hours_per_assignee_and_ignores_done_items()
    {
        var anna = Guid.NewGuid();
        var items = new[]
        {
            Sample(hours: 5, assignee: anna),
            Sample(hours: 4, assignee: anna),
            Sample(WorkItemStatus.Done, hours: 10, assignee: anna),
            Sample(hours: 1)
        };

        var loads = Workload.Compute(items);

        Assert.Equal(9, loads.Single(l => l.UserId == anna).OpenHours);
        Assert.Equal(1, loads.Single(l => l.UserId is null).OpenHours);
    }
}