namespace WebBlazorDeliveryCRM.Store.PlayerPosition;

public record LoadPlayerPositionAction;

public record SetPlayerPositionAction(int X, int Y);

public record PlayerPositionLoadedAction(int X, int Y);
