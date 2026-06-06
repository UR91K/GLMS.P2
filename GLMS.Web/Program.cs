using GLMS.Web.Auth;
using GLMS.Web.Components;
using GLMS.Web.Services.Clients;
using GLMS.Web.Services.Contracts;
using GLMS.Web.Services.Currency;
using GLMS.Web.Services.ServiceRequests;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient("GlmsApi", client =>
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5001"));

builder.Services.AddScoped<GlmsAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<GlmsAuthStateProvider>());
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddMemoryCache();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IServiceRequestService, ServiceRequestService>();

builder.Services.AddHttpClient<ExchangeRateApiService>(client =>
{
    var baseUrl = builder.Configuration["Currency:ApiBaseUrl"] ?? "https://v6.exchangerate-api.com/v6";
    var apiKey = builder.Configuration["Currency:ApiKey"];

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException("Currency:ApiKey is not configured.");
    }

    client.BaseAddress = new Uri($"{baseUrl.TrimEnd('/')}/{apiKey.Trim()}/");
});

builder.Services.AddScoped<ICurrencyService, CurrencyServiceProxy>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
