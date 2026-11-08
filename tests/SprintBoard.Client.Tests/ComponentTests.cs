// tests/SprintBoard.Client.Tests/ComponentTests.cs
using Bunit;
using Microsoft.AspNetCore.Components;
using SprintBoard.Client.Components;
using SprintBoard.Domain.Models;

namespace SprintBoard.Client.Tests;

public class TaskCardTests : BunitContext
{
    private static WorkItem Item(WorkItemStatus status = WorkItemStatus.Todo, DateTime? due = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Write report", "", status, Priority.High, null, due, 3, 0);

    [Fact]
    public void Shows_the_title_and_priority()
    {
        var cut = Render<TaskCard>(p => p.Add(c => c.Item, Item()));

        Assert.Contains("Write report", cut.Markup);
        Assert.Contains("High", cut.Markup);
    }

    [Fact]
    public void The_next_button_raises_OnMove_with_the_following_status()
    {
        WorkItemStatus? moved = null;

        var cut = Render<TaskCard>(p => p
            .Add(c => c.Item, Item())
            .Add(c => c.CanMove, true)
            .Add(c => c.OnMove, EventCallback.Factory.Create<WorkItemStatus>(this, s => { moved = s; })));

        cut.Find("button[aria-label='Move to next column']").Click();

        Assert.Equal(WorkItemStatus.InProgress, moved);
    }

    [Fact]
    public void A_task_in_the_first_column_has_no_previous_button()
    {
        var cut = Render<TaskCard>(p => p.Add(c => c.Item, Item()));

        Assert.Empty(cut.FindAll("button[aria-label='Move to previous column']"));
    }

    [Fact]
    public void An_overdue_task_is_marked_at_risk()
    {
        var cut = Render<TaskCard>(p => p.Add(c => c.Item, Item(due: DateTime.UtcNow.AddHours(-2))));

        Assert.Contains("at-risk", cut.Find("article").ClassName);
    }

    [Fact]
    public void Only_task_managers_see_edit_and_delete_actions()
    {
        var memberCard = Render<TaskCard>(p => p.Add(c => c.Item, Item()));

        Assert.Empty(memberCard.FindAll("button[aria-label='Edit task']"));
        Assert.Empty(memberCard.FindAll("button[aria-label='Delete task']"));

        var ownerCard = Render<TaskCard>(p => p
            .Add(c => c.Item, Item())
            .Add(c => c.CanManageTasks, true));

        ownerCard.Find("button[aria-label='Edit task']").Click();
        Assert.Single(ownerCard.FindAll("input[maxlength='120']"));
        Assert.Single(ownerCard.FindAll("input[type='number']"));
    }

    [Fact]
    public void An_unassigned_member_cannot_move_a_task()
    {
        var cut = Render<TaskCard>(p => p.Add(c => c.Item, Item()));

        Assert.Empty(cut.FindAll("button[aria-label='Move to next column']"));
        Assert.Empty(cut.FindAll("button[aria-label='Move to previous column']"));
    }
}

public class WorkloadHeatmapTests : BunitContext
{
    [Fact]
    public void Shows_member_names_and_marks_a_heavy_load_as_high()
    {
        var anna = Guid.NewGuid();
        IReadOnlyList<WorkItem> items =
            [new(Guid.NewGuid(), Guid.NewGuid(), "Big task", "", WorkItemStatus.Todo, Priority.High, anna, null, 30, 0)];
        IReadOnlyList<MemberInfo> members = [new(anna, "Anna", Role.Member)];

        var cut = Render<WorkloadHeatmap>(p => p.Add(c => c.Items, items).Add(c => c.Members, members));

        Assert.Contains("Anna", cut.Markup);
        Assert.NotEmpty(cut.FindAll(".heat-high"));
    }
}
