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
builder.Services.AddScoped<CouriersApiService>();

var apiBase = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5220";

builder.Services.AddHttpClient("UnauthorizedClient", client =>
{
    client.BaseAddress = new Uri(apiBase);
});

builder.Services.AddHttpClient("AuthorizedClient", client =>
{
    client.BaseAddress = new Uri(apiBase);
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
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseCors("AllowAll");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
