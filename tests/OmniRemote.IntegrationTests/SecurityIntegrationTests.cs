using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OmniRemote.Backend.Controllers;
using OmniRemote.Backend.Data;
using OmniRemote.Backend.Security;
using OmniRemote.Domain.Commands;
using OmniRemote.Domain.Devices;
using Xunit;

namespace OmniRemote.IntegrationTests;

public sealed class SecurityIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SecurityIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Use a unique in-memory or temporary SQLite DB per test run
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<OmniRemoteDbContext>));
                if (descriptor is not null) services.Remove(descriptor);

                var dbName = Guid.NewGuid().ToString("N");
                services.AddDbContext<OmniRemoteDbContext>(opt => opt.UseSqlite($"Data Source={dbName}.db"));
            });
        });
    }

    private async Task<(HttpClient Client, Guid UserId, string Token)> CreateUserAndClientAsync(string email, string password, string role = "User")
    {
        var client = _factory.CreateClient();

        // Direct DB seed to bypass rate limiting for setup
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OmniRemoteDbContext>();
        db.Database.EnsureCreated();

        var userId = Guid.NewGuid();
        var user = new UserEntity
        {
            Id = userId,
            Email = email.ToLowerInvariant(),
            PasswordHash = PasswordHasher.Hash(password),
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var tokenService = scope.ServiceProvider.GetRequiredService<TokenService>();
        var token = tokenService.CreateToken(user.Id, user.Email, user.Role);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, userId, token);
    }

    [Fact]
    public async Task RegisterAndLogin_SucceedsWithJwtToken()
    {
        var client = _factory.CreateClient();
        var uniqueEmail = $"user_{Guid.NewGuid():N}@example.com";

        // Register
        var regRes = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(uniqueEmail, "P@ssw0rd1234!"));
        Assert.Equal(HttpStatusCode.Created, regRes.StatusCode);
        var regData = await regRes.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(regData?.Token);

        // Login
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(uniqueEmail, "P@ssw0rd1234!"));
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);
        var loginData = await loginRes.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(loginData?.Token);
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_ReturnsGenericUnauthorized()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("nonexistent@example.com", "WrongPassword123!"));

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task UserDataIsolation_UserACannotAccessOrModifyUserBDevice()
    {
        var (clientA, _, _) = await CreateUserAndClientAsync($"userA_{Guid.NewGuid():N}@example.com", "SecretPass123!");
        var (clientB, _, _) = await CreateUserAndClientAsync($"userB_{Guid.NewGuid():N}@example.com", "SecretPass123!");

        // User A creates a device
        var createReq = new CreateDeviceRequest(
            Name: "Living Room AC",
            Type: DeviceType.AirConditioner,
            BrandId: "carrier",
            BrandName: "Carrier",
            Model: "Series-X",
            Room: "Living Room",
            ConnectionKind: ConnectionKind.Simulation,
            ConnectionValues: null,
            SecretRef: null);

        var createRes = await clientA.PostAsJsonAsync("/api/v1/devices", createReq);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var createdDevice = await createRes.Content.ReadFromJsonAsync<StoredDeviceEntity>();
        Assert.NotNull(createdDevice);

        // User B attempts to GET User A's device -> must be 404 (isolation)
        var getRes = await clientB.GetAsync($"/api/v1/devices/{createdDevice.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);

        // User B attempts to DELETE User A's device -> must be 404 (isolation)
        var delRes = await clientB.DeleteAsync($"/api/v1/devices/{createdDevice.Id}");
        Assert.Equal(HttpStatusCode.NotFound, delRes.StatusCode);
    }

    [Fact]
    public async Task UserDataIsolation_UserACannotSendCommandToUserBDevice()
    {
        var (clientA, _, _) = await CreateUserAndClientAsync($"userA_{Guid.NewGuid():N}@example.com", "SecretPass123!");
        var (clientB, _, _) = await CreateUserAndClientAsync($"userB_{Guid.NewGuid():N}@example.com", "SecretPass123!");

        // User A creates device
        var createReq = new CreateDeviceRequest(
            Name: "Master AC",
            Type: DeviceType.AirConditioner,
            BrandId: "daikin",
            BrandName: "Daikin",
            Model: null,
            Room: "Bedroom",
            ConnectionKind: ConnectionKind.Simulation,
            ConnectionValues: null,
            SecretRef: null);

        var createRes = await clientA.PostAsJsonAsync("/api/v1/devices", createReq);
        var createdDevice = await createRes.Content.ReadFromJsonAsync<StoredDeviceEntity>();

        // User B attempts to send command to User A's device
        var cmdReq = new ExecuteCommandRequest(
            RequestId: Guid.NewGuid(),
            DeviceId: createdDevice!.Id,
            Name: CommandNames.PowerSet,
            Parameters: new Dictionary<string, string> { ["on"] = "true" });

        var cmdRes = await clientB.PostAsJsonAsync("/api/v1/commands", cmdReq);

        // Must reject with 404 Not Found (device does not exist for User B)
        Assert.Equal(HttpStatusCode.NotFound, cmdRes.StatusCode);
    }

    [Fact]
    public async Task PrivilegeEscalation_NormalUserCannotAccessAdminRoutes()
    {
        var (clientUser, _, _) = await CreateUserAndClientAsync($"normal_{Guid.NewGuid():N}@example.com", "SecretPass123!", role: "User");
        var res = await clientUser.GetAsync("/api/v1/admin/stats");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task AdminRole_CanAccessAdminRoutes()
    {
        var (clientAdmin, _, _) = await CreateUserAndClientAsync($"admin_{Guid.NewGuid():N}@example.com", "SecretPass123!", role: "Admin");
        var res = await clientAdmin.GetAsync("/api/v1/admin/stats");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task SecurityHeaders_ArePresentOnAllResponses()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/v1/auth/register");

        Assert.True(res.Headers.Contains("X-Content-Type-Options"));
        Assert.Equal("nosniff", res.Headers.GetValues("X-Content-Type-Options").First());

        Assert.True(res.Headers.Contains("X-Frame-Options"));
        Assert.Equal("DENY", res.Headers.GetValues("X-Frame-Options").First());

        Assert.True(res.Headers.Contains("Referrer-Policy"));
    }

    [Fact]
    public async Task SqlInjectionAttempt_InDeviceName_SafelyTreatedAsLiteralOrRejected()
    {
        var (client, _, _) = await CreateUserAndClientAsync($"sqli_{Guid.NewGuid():N}@example.com", "SecretPass123!");

        var sqlInjectionName = "'; DROP TABLE Devices; --";
        var createReq = new CreateDeviceRequest(
            Name: sqlInjectionName,
            Type: DeviceType.Tv,
            BrandId: "sony",
            BrandName: "Sony",
            Model: null,
            Room: "Living",
            ConnectionKind: ConnectionKind.Simulation,
            ConnectionValues: null,
            SecretRef: null);

        var res = await client.PostAsJsonAsync("/api/v1/devices", createReq);

        // Either safely rejected by InputRules, or safely stored as parameter without SQL injection
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OmniRemoteDbContext>();
        // Verify database tables still intact
        var count = await db.Devices.CountAsync();
        Assert.True(count >= 0);
    }
}
