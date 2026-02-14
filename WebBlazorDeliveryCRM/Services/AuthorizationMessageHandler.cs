using System.Net.Http.Headers;
using Blazored.LocalStorage;

namespace WebBlazorDeliveryCRM.Services;

public class AuthorizationMessageHandler : DelegatingHandler
{
    private const string TokenKey = "auth_token";
    private readonly ILocalStorageService _localStorage;

    public AuthorizationMessageHandler(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _localStorage.GetItemAsync<string>(TokenKey, cancellationToken);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
