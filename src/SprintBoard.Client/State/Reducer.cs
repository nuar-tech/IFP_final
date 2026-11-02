// src/SprintBoard.Client/State/Reducer.cs
using System.Collections.Immutable;
using SprintBoard.Domain.Models;

namespace SprintBoard.Client.State;

public static class Reducer
{
    public static AppState Reduce(AppState state, StoreAction action) => action switch
    {
        BoardLoading a => state with { BoardId = a.BoardId, Items = ImmutableList<WorkItem>.Empty, Loading = true, Error = null },
        ItemsLoaded a => state with { Items = a.Items.ToImmutableList(), Loading = false },
        ItemUpserted a when a.Item.BoardId != state.BoardId => state,   // ignore events from other boards
        ItemUpserted a => state with { Items = Upsert(state.Items, a.Item) },
        ItemRemoved a => state with { Items = state.Items.RemoveAll(i => i.Id == a.ItemId) },
        Failed a => state with { Loading = false, Error = a.Message },
        ErrorDismissed => state with { Error = null },
        _ => state
    };

    private static ImmutableList<WorkItem> Upsert(ImmutableList<WorkItem> items, WorkItem item)
    {
        var index = items.FindIndex(i => i.Id == item.Id);
        return index < 0 ? items.Add(item) : items.SetItem(index, item);
    }
}

public static class Selectors
{
    public static IReadOnlyList<WorkItem> ByStatus(AppState state, WorkItemStatus status) =>
        state.Items.Where(i => i.Status == status).OrderBy(i => i.Position).ThenBy(i => i.Title).ToList();
}
