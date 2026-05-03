using Fluxor;

namespace WebBlazorDeliveryCRM.Store.PlayerPosition;

public static class PlayerPositionReducers
{
    [ReducerMethod]
    public static PlayerPositionState ReduceSetPlayerPosition(PlayerPositionState state, SetPlayerPositionAction action) =>
        state with
        {
            X = action.X,
            Y = action.Y,
            IsLoaded = true,
            UpdatedAtUtc = DateTime.UtcNow
        };

    [ReducerMethod]
    public static PlayerPositionState ReducePlayerPositionLoaded(PlayerPositionState state, PlayerPositionLoadedAction action) =>
        state with
        {
            X = action.X,
            Y = action.Y,
            IsLoaded = true,
            UpdatedAtUtc = DateTime.UtcNow
        };
}
