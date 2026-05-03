using Blazored.Toast;
using ApexCharts;
using Fluxor;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using WebBlazorDeliveryCRM.Components;
using WebBlazorDeliveryCRM.Models;
using WebBlazorDeliveryCRM.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });
builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddBlazoredToast();
builder.Services.AddMudServices();
builder.Services.AddApexCharts();
builder.Services.AddFluxor(options => options.ScanAssemblies(typeof(Program).Assembly));

builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthenticationStateProvider>());
builder.Services.AddScoped<AuthApiService>();
builder.Services.AddScoped<ChatHubClientService>();
builder.Services.AddScoped<ChatApiService>();
builder.Services.AddScoped<ChatUnreadStateService>();
builder.Services.AddScoped<OrdersApiService>();
builder.Services.AddScoped<LogisticsHubsApiService>();
builder.Services.AddScoped<ClientsApiService>();
builder.Services.AddScoped<ClientsDetailsApiService>();
builder.Services.AddScoped<CouriersApiService>();
builder.Services.AddScoped<VehiclesApiService>();
builder.Services.AddScoped<AuditApiService>();
builder.Services.AddScoped<AppNotificationService>();
builder.Services.AddScoped<RolesApiService>();
builder.Services.AddScoped<EmployeesApiService>();
builder.Services.AddScoped<LeadsApiService>();
builder.Services.AddScoped<GeoAnalyticsApiService>();
builder.Services.AddScoped<MonitoringApiService>();
builder.Services.AddScoped<ReportsApiService>();
builder.Services.AddScoped<CompanySettingsApiService>();
builder.Services.AddScoped<AddressSuggestApiService>();
builder.Services.AddScoped<UserPresenceApiService>();
builder.Services.AddScoped<NotificationsApiService>();
builder.Services.AddScoped<SupportTicketsApiService>();
builder.Services.AddScoped<ServiceAreaZonesApiService>();
builder.Services.AddScoped<BillingApiService>();
builder.Services.AddScoped<CommunicationTemplatesApiService>();
builder.Services.AddScoped<ScheduledReportsApiService>();

// Связь Blazor с API: один базовый адрес, два HttpClient — без токена (логин) и с JWT (остальные запросы)
var apiBase = (builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5220").TrimEnd('/');

builder.Services.AddHttpClient("UnauthorizedClient", client =>
{
    client.BaseAddress = new Uri(apiBase);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient("AuthorizedClient", client =>
{
    client.BaseAddress = new Uri(apiBase);
    client.Timeout = TimeSpan.FromSeconds(5);
}).AddHttpMessageHandler<AuthorizationMessageHandler>();

builder.Services.AddTransient<AuthorizationMessageHandler>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseStatusCodePagesWithReExecute("/404");

// HttpOnly-cookie с JWT: выставляется ответом на same-origin POST из браузера (после логина к API).
app.MapPost("/api/auth/session", (HttpContext http, SessionRequest req) =>
{
    var token = req.Token?.Trim();
    if (string.IsNullOrEmpty(token))
        return Results.BadRequest();
    var parts = token.Split('.');
    if (parts.Length < 3)
        return Results.BadRequest();

    var cookieOptions = new CookieOptions
    {
        HttpOnly = true,
        Secure = !app.Environment.IsDevelopment(),
        SameSite = SameSiteMode.Lax,
        Path = "/",
        MaxAge = TimeSpan.FromDays(7),
        IsEssential = true,
    };
    http.Response.Cookies.Append(AuthCookieConstants.CookieName, token, cookieOptions);
    return Results.Ok();
}).AllowAnonymous().DisableAntiforgery();

app.MapPost("/api/auth/logout", (HttpContext http) =>
{
    http.Response.Cookies.Delete(AuthCookieConstants.CookieName, new CookieOptions
    {
        Path = "/",
        HttpOnly = true,
        Secure = !app.Environment.IsDevelopment(),
        SameSite = SameSiteMode.Lax,
    });
    return Results.Ok();
}).AllowAnonymous().DisableAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
