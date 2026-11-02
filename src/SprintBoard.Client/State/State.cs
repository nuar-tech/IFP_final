// src/SprintBoard.Client/State/State.cs
using System.Collections.Immutable;
using SprintBoard.Domain.Models;

namespace SprintBoard.Client.State;

public sealed record AppState(Guid? BoardId, ImmutableList<WorkItem> Items, bool Loading, string? Error)
{
    public static AppState Initial { get; } = new(null, ImmutableList<WorkItem>.Empty, false, null);
}

public abstract record StoreAction;
public sealed record BoardLoading(Guid BoardId) : StoreAction;
public sealed record ItemsLoaded(IReadOnlyList<WorkItem> Items) : StoreAction;
public sealed record ItemUpserted(WorkItem Item) : StoreAction;
public sealed record ItemRemoved(Guid ItemId) : StoreAction;
public sealed record Failed(string Message) : StoreAction;
public sealed record ErrorDismissed() : StoreAction;

