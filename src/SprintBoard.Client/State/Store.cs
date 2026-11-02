// src/SprintBoard.Client/State/Store.cs
using Microsoft.AspNetCore.Components;

namespace SprintBoard.Client.State;

public sealed class Store
{
    public AppState State { get; private set; } = AppState.Initial;
    public event Action? Changed;

    public void Dispatch(StoreAction action)
    {
        State = Reducer.Reduce(State, action);
        Changed?.Invoke();
    }
}

/// <summary>Base class: re-renders the component whenever the store changes, and unsubscribes on dispose.</summary>
public abstract class StoreComponent : ComponentBase, IDisposable
{
    [Inject] protected Store Store { get; set; } = default!;

    protected AppState State => Store.State;

    protected override void OnInitialized() => Store.Changed += OnStoreChanged;

    private void OnStoreChanged() => InvokeAsync(StateHasChanged);

    public virtual void Dispose() => Store.Changed -= OnStoreChanged;
}

/// <summary>Light/dark theme. Shared with all components through a CascadingValue in MainLayout.</summary>
public sealed class ThemeState
{
    public bool Dark { get; private set; }
    public event Action? Changed;

    public void Toggle()
    {
        Dark = !Dark;
        Changed?.Invoke();
    }
}
