using System.Text;
using GLMS.Api.Data;
using GLMS.Api.Services;
using GLMS.Api.Services.Clients;
using GLMS.Api.Services.Contracts;
using GLMS.Api.Services.Currency;
using GLMS.Api.Services.ServiceRequests;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// databases

// "Data Source=..." is SQLite syntax. anything else (like a SQL Server connection string) goes
// to SQL Server. In development, appsettings.json sets DefaultConnection to "Data Source=glms.db"

// in docker, the environment variable overrides it with the SQL Server connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var useSqlite = string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase);

if (useSqlite)
{
    // fallback to glms.db in the working directory if no connection string is configured at all
    var sqliteConn = string.IsNullOrWhiteSpace(connectionString) ? "Data Source=glms.db" : connectionString;
    builder.Services.AddDbContextFactory<AppDbContext>(opts =>
        opts.UseSqlite(sqliteConn));
}
else
{
    builder.Services.AddDbContextFactory<AppDbContext>(opts =>
        opts.UseSqlServer(connectionString));
}

// business services
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IServiceRequestService, ServiceRequestService>();
builder.Services.AddScoped<DatabaseInitializationService>();

// currency services - proxy pattern with caching and external API integration
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<ExchangeRateApiService>(client =>
{
    var baseUrl = builder.Configuration["Currency:BaseUrl"] ?? "https://v6.exchangerate-api.com/v6/";
    var apiKey = builder.Configuration["Currency:ApiKey"] ?? string.Empty;
    client.BaseAddress = new Uri($"{baseUrl.TrimEnd('/')}/{apiKey}/");
});
builder.Services.AddScoped<ICurrencyService>(sp =>
    new CurrencyServiceProxy(
        sp.GetRequiredService<ExchangeRateApiService>(),
        sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
        sp.GetRequiredService<IConfiguration>()));

// JWT auth
var jwtKey = builder.Configuration["JwtSettings:SecretKey"]
    ?? throw new InvalidOperationException("JwtSettings:SecretKey must be configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "glms-api",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "glms-web",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// controllers + swagger
// reference:
// https://duendesoftware.com/blog/20251126-securing-openapi-and-swagger-ui-with-oauth-in-dotnet-10
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "GLMS API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer token. Enter: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            []
        }
    });
});

var app = builder.Build();

// init database on startup
using (var scope = app.Services.CreateScope())
{
    var dbInit = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
    await dbInit.InitializeAsync();
}

// middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
