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
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var useSqlite = string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase);

if (useSqlite)
{
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

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
