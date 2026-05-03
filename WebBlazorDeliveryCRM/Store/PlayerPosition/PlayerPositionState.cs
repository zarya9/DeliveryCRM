using Fluxor;

namespace WebBlazorDeliveryCRM.Store.PlayerPosition;

[FeatureState]
public record PlayerPositionState
{
    public bool IsLoaded { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
}
