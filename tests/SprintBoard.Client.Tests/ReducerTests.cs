// tests/SprintBoard.Client.Tests/ReducerTests.cs
using SprintBoard.Client.State;
using SprintBoard.Domain.Models;

namespace SprintBoard.Client.Tests;

public class ReducerTests
{
    private static readonly Guid BoardId = Guid.NewGuid();

    private static WorkItem Item(string title = "Task", WorkItemStatus status = WorkItemStatus.Todo, Guid? boardId = null) =>
        new(Guid.NewGuid(), boardId ?? BoardId, title, "", status, Priority.Medium, null, null, 1, 0);

    private static AppState OnBoard() => AppState.Initial with { BoardId = BoardId };

    [Fact]
    public void Upsert_adds_a_new_item_then_replaces_it_by_id()
    {
        var item = Item();
        var added = Reducer.Reduce(OnBoard(), new ItemUpserted(item));
        var replaced = Reducer.Reduce(added, new ItemUpserted(item with { Status = WorkItemStatus.Done }));

        Assert.Single(replaced.Items);
        Assert.Equal(WorkItemStatus.Done, replaced.Items[0].Status);
    }

    [Fact]
    public void Upsert_is_idempotent_so_a_server_echo_of_our_own_change_is_harmless()
    {
        var item = Item();
        var once = Reducer.Reduce(OnBoard(), new ItemUpserted(item));
        var twice = Reducer.Reduce(once, new ItemUpserted(item));

        Assert.Single(twice.Items);
    }

    [Fact]
    public void Reduce_never_mutates_the_previous_state()
    {
        var before = OnBoard();
        _ = Reducer.Reduce(before, new ItemUpserted(Item()));

        Assert.Empty(before.Items);
    }

    [Fact]
    public void An_item_from_another_board_is_ignored()
    {
        var state = Reducer.Reduce(OnBoard(), new ItemUpserted(Item(boardId: Guid.NewGuid())));
        Assert.Empty(state.Items);
    }

    [Fact]
    public void Removing_an_item_deletes_only_that_item()
    {
        var a = Item("A");
        var b = Item("B");
        var state = Reducer.Reduce(Reducer.Reduce(OnBoard(), new ItemUpserted(a)), new ItemUpserted(b));

        state = Reducer.Reduce(state, new ItemRemoved(a.Id));

        Assert.Equal("B", Assert.Single(state.Items).Title);
    }

    [Fact]
    public void Failed_sets_the_error_and_dismissing_clears_it()
    {
        var failed = Reducer.Reduce(OnBoard(), new Failed("Boom"));
        Assert.Equal("Boom", failed.Error);

        Assert.Null(Reducer.Reduce(failed, new ErrorDismissed()).Error);
    }

    [Fact]
    public void ByStatus_returns_only_the_requested_column_in_position_order()
    {
        var state = OnBoard();
        state = Reducer.Reduce(state, new ItemUpserted(Item("Second") with { Position = 2 }));
        state = Reducer.Reduce(state, new ItemUpserted(Item("First") with { Position = 1 }));
        state = Reducer.Reduce(state, new ItemUpserted(Item("Finished", WorkItemStatus.Done)));

        var todo = Selectors.ByStatus(state, WorkItemStatus.Todo);

        Assert.Equal(["First", "Second"], todo.Select(i => i.Title));
    }
}

