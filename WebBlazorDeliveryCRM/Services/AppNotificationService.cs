using Blazored.Toast.Services;
using Microsoft.JSInterop;

namespace WebBlazorDeliveryCRM.Services;

/// <summary>
/// Всплывающие уведомления (toast) при действиях в приложении;
/// push-уведомления в браузере, когда вкладка скрыта.
/// </summary>
public class AppNotificationService
{
    private readonly IToastService _toast;
    private readonly IJSRuntime _js;

    public AppNotificationService(IToastService toast, IJSRuntime js)
    {
        _toast = toast;
        _js = js;
    }

    /// <summary>Показать успех (toast; если вкладка скрыта — push).</summary>
    public void ShowSuccess(string message, bool pushWhenHidden = false)
    {
        _toast.ShowSuccess(message);
        if (pushWhenHidden) _ = TryShowPushAsync("Успешно", message);
    }

    /// <summary>Показать ошибку.</summary>
    public void ShowError(string message, bool pushWhenHidden = false)
    {
        _toast.ShowError(message);
        if (pushWhenHidden) _ = TryShowPushAsync("Ошибка", message);
    }

    /// <summary>Показать предупреждение.</summary>
    public void ShowWarning(string message)
    {
        _toast.ShowWarning(message);
    }

    /// <summary>Показать информацию.</summary>
    public void ShowInfo(string message, bool pushWhenHidden = false)
    {
        _toast.ShowInfo(message);
        if (pushWhenHidden) _ = TryShowPushAsync("Delivery CRM", message);
    }

    /// <summary>Показать push только если вкладка скрыта (например, из SignalR).</summary>
    public async Task ShowPushWhenHiddenAsync(string title, string body)
    {
        try
        {
            var visible = await _js.InvokeAsync<bool>("deliveryCrmNotifications.isTabVisible");
            if (visible) return;
            await _js.InvokeVoidAsync("deliveryCrmNotifications.showPush", title, body);
        }
        catch { /* ignore */ }
    }

    /// <summary>Запросить разрешение на push-уведомления (вызывать после входа).</summary>
    public async Task<string> RequestPushPermissionAsync()
    {
        try
        {
            return await _js.InvokeAsync<string>("deliveryCrmNotifications.requestPermission");
        }
        catch
        {
            return "unsupported";
        }
    }

    private async Task TryShowPushAsync(string title, string body)
    {
        try
        {
            var visible = await _js.InvokeAsync<bool>("deliveryCrmNotifications.isTabVisible");
            if (!visible)
                await _js.InvokeVoidAsync("deliveryCrmNotifications.showPush", title, body);
        }
        catch { /* ignore */ }
    }
}
