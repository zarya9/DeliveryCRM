using System.Text.Json;
using Fluxor;
using Microsoft.JSInterop;

namespace WebBlazorDeliveryCRM.Store.PlayerPosition;

public class PlayerPositionEffects
{
    private const string StorageKey = "player-position";
    private readonly IJSRuntime _jsRuntime;

    public PlayerPositionEffects(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    [EffectMethod]
    public async Task HandleLoadPlayerPositionAction(LoadPlayerPositionAction action, IDispatcher dispatcher)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (string.IsNullOrWhiteSpace(json))
                return;

            var model = JsonSerializer.Deserialize<PlayerPositionStorageModel>(json);
            if (model is null)
                return;

            dispatcher.Dispatch(new PlayerPositionLoadedAction(model.X, model.Y));
        }
        catch
        {
            // localStorage может быть недоступен в некоторых окружениях рендера.
        }
    }

    [EffectMethod]
    public async Task HandleSetPlayerPositionAction(SetPlayerPositionAction action, IDispatcher dispatcher)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new PlayerPositionStorageModel(action.X, action.Y));
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, payload);
        }
        catch
        {
            // Ошибки localStorage не должны ломать UX.
        }
    }

    private sealed record PlayerPositionStorageModel(int X, int Y);
}
