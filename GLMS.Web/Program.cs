using GLMS.Web.Components;
using GLMS.Web.Data;
using GLMS.Web.Services.Currency;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<AppDbContext>(opts =>
    opts.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMemoryCache();

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
