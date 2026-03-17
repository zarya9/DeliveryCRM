using Blazored.LocalStorage;
using Blazored.Toast;
using Microsoft.AspNetCore.Components.Authorization;
using WebBlazorDeliveryCRM.Components;
using WebBlazorDeliveryCRM.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredToast();

builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthenticationStateProvider>());
builder.Services.AddScoped<AuthApiService>();
builder.Services.AddScoped<ChatHubClientService>();
builder.Services.AddScoped<OrdersApiService>();
builder.Services.AddScoped<ClientsApiService>();
builder.Services.AddScoped<ClientsDetailsApiService>();
builder.Services.AddScoped<CouriersApiService>();
builder.Services.AddScoped<AppNotificationService>();
builder.Services.AddScoped<ThemeApiService>();
builder.Services.AddScoped<RolesApiService>();
builder.Services.AddScoped<EmployeesApiService>();
builder.Services.AddScoped<LeadsApiService>();

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
app.UseAntiforgery();

app.UseCors("AllowAll");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
