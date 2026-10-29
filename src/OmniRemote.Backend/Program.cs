using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OmniRemote.Backend.Data;
using OmniRemote.Backend.Middleware;
using OmniRemote.Backend.Security;
using OmniRemote.Core.Abstractions;
using OmniRemote.Core.Devices;
using OmniRemote.Core.Simulation;
using OmniRemote.Domain.Devices;
using OmniRemote.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// --- Mandatory Security: Secret / Configuration Validation ---
var jwtKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    // In Development environment, permit a deterministic local dev key if not provided, but log loud warning
    if (builder.Environment.IsDevelopment())
    {
        jwtKey = "LOCAL_DEV_INSECURE_SIGNING_KEY_32_CHARS_LONG_FOR_TESTS_ONLY!";
        builder.Configuration["Jwt:SigningKey"] = jwtKey;
    }
    else
    {
        throw new InvalidOperationException("FATAL: 'Jwt:SigningKey' must be set via environment variable or secret store with at least 32 characters in Production.");
    }
}

// Database configuration: SQLite
var connectionString = builder.Configuration.GetConnectionString("OmniRemote") ?? "Data Source=omniremote.db";
builder.Services.AddDbContext<OmniRemoteDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

// Authentication & JWT
builder.Services.AddSingleton<TokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "omniremote-backend",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "omniremote-mobile",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorization();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("auth-policy", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("command-policy", opt =>
    {
        opt.PermitLimit = 60;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

// Core Domain Services
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ISimulationRandom, SystemSimulationRandom>();
builder.Services.AddSingleton<SimulationSettings>();
builder.Services.AddSingleton<MockDeviceAdapter>();
builder.Services.AddSingleton<IDeviceAdapter>(sp => sp.GetRequiredService<MockDeviceAdapter>());

// Local In-Memory Secret Store for backend adapter routing
builder.Services.AddSingleton<ISecretStore, InMemorySecretStore>();

// Dummy repository to initialize backend-side registry and router
var backendStoreDir = Path.Combine(AppContext.BaseDirectory, "backend_data");
Directory.CreateDirectory(backendStoreDir);
var fileStore = new JsonFileStore(backendStoreDir);
builder.Services.AddSingleton<IDeviceRepository>(fileStore);
builder.Services.AddSingleton<IActivityRepository>(fileStore);
builder.Services.AddSingleton<ISettingsStore>(fileStore);

builder.Services.AddSingleton<DeviceRegistry>();
builder.Services.AddSingleton<DeviceStateStore>();
builder.Services.AddSingleton<ActivityLog>();
builder.Services.AddSingleton(new CommandRouterOptions());
builder.Services.AddSingleton<CommandRouter>();

builder.Services.AddControllers();

var app = builder.Build();

// Ensure DB is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OmniRemoteDbContext>();
    db.Database.EnsureCreated();
}

// Security Middleware Pipeline
app.UseMiddleware<SafeErrorHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

internal sealed class InMemorySecretStore : ISecretStore
{
    private readonly Dictionary<string, string> _secrets = new(StringComparer.Ordinal);
    public Task<string?> GetAsync(string key) => Task.FromResult(_secrets.GetValueOrDefault(key));
    public Task SetAsync(string key, string value) { _secrets[key] = value; return Task.CompletedTask; }
    public Task RemoveAsync(string key) { _secrets.Remove(key); return Task.CompletedTask; }
}

public partial class Program { }
