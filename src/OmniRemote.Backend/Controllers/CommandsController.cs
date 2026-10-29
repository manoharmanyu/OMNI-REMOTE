using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OmniRemote.Backend.Data;
using OmniRemote.Core.Devices;
using OmniRemote.Domain.Commands;
using OmniRemote.Domain.Devices;

namespace OmniRemote.Backend.Controllers;

[ApiController]
[Route("api/v1/commands")]
[Authorize]
[EnableRateLimiting("command-policy")]
public sealed class CommandsController : ControllerBase
{
    private readonly OmniRemoteDbContext _db;
    private readonly CommandRouter _router;
    private readonly DeviceRegistry _registry;
    private readonly ILogger<CommandsController> _logger;

    public CommandsController(OmniRemoteDbContext db, CommandRouter router, DeviceRegistry registry, ILogger<CommandsController> logger)
    {
        _db = db;
        _router = router;
        _registry = registry;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(sub, out var userId)) return userId;
        throw new InvalidOperationException("Authenticated user has no valid identifier claim.");
    }

    [HttpPost]
    public async Task<IActionResult> ExecuteCommand([FromBody] ExecuteCommandRequest req, CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        // 1. Look up device and strictly enforce user ownership
        var storedDevice = await _db.Devices.FirstOrDefaultAsync(d => d.Id == req.DeviceId && d.UserId == userId, ct);
        if (storedDevice is null)
        {
            _logger.LogWarning("Command target device not found or access denied for user.");
            return NotFound(new { message = "Device not found or access denied." });
        }

        // Deserialize profile
        var profile = JsonSerializer.Deserialize<DeviceProfile>(storedDevice.ProfileJson)
                      ?? DeviceProfile.DefaultFor(storedDevice.Type);

        var domainDevice = new RemoteDevice
        {
            Id = storedDevice.Id,
            Name = storedDevice.Name,
            Type = storedDevice.Type,
            BrandId = storedDevice.BrandId,
            BrandName = storedDevice.BrandName,
            Model = storedDevice.Model,
            Room = storedDevice.Room,
            Connection = new ConnectionSettings
            {
                Kind = storedDevice.ConnectionKind,
                Values = storedDevice.ConnectionValuesJson is not null
                    ? JsonSerializer.Deserialize<Dictionary<string, string>>(storedDevice.ConnectionValuesJson) ?? []
                    : [],
                SecretRef = storedDevice.SecretRef,
            },
            Profile = profile,
            CreatedAt = storedDevice.CreatedAt,
        };

        var command = new DeviceCommand
        {
            RequestId = req.RequestId != Guid.Empty ? req.RequestId : Guid.NewGuid(),
            DeviceId = storedDevice.Id,
            Name = req.Name,
            Parameters = req.Parameters ?? new Dictionary<string, string>(),
            RequestedAt = DateTimeOffset.UtcNow,
        };

        // 2. Validate Command Server-Side
        var validation = CommandValidator.Validate(command, domainDevice);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Command validation failed: {Error}", validation.Error);
            return BadRequest(new
            {
                status = CommandStatus.Failed.ToString(),
                errorCode = validation.ErrorCode.ToString(),
                message = validation.Error ?? "Invalid command parameters.",
            });
        }

        // 3. Ensure device is registered in memory and route through CommandRouter
        if (_registry.Find(domainDevice.Id) is null)
        {
            await _registry.AddAsync(domainDevice, ct);
        }

        var outcome = await _router.SendAsync(command, ct);

        // 4. Update Database Log
        var activity = new StoredActivityEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RequestId = command.RequestId,
            DeviceId = domainDevice.Id,
            DeviceName = domainDevice.Name,
            CommandName = command.Name,
            Parameters = command.ParameterSummary,
            Status = outcome.Result.Status,
            ErrorCode = outcome.Result.ErrorCode,
            Timestamp = DateTimeOffset.UtcNow,
            Simulated = outcome.Result.Simulated,
            Message = outcome.Result.Message,
        };

        _db.Activities.Add(activity);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            requestId = command.RequestId,
            deviceId = domainDevice.Id,
            status = outcome.Result.Status.ToString(),
            errorCode = outcome.Result.ErrorCode.ToString(),
            message = outcome.Result.Message,
            simulated = outcome.Result.Simulated,
            state = outcome.State.Values,
        });
    }

    [HttpGet("history/{deviceId:guid}")]
    public async Task<IActionResult> GetHistory(Guid deviceId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        // Verify device belongs to user
        if (!await _db.Devices.AnyAsync(d => d.Id == deviceId && d.UserId == userId, ct))
            return NotFound(new { message = "Device not found." });

        var activities = await _db.Activities
            .Where(a => a.DeviceId == deviceId && a.UserId == userId)
            .ToListAsync(ct);

        return Ok(activities.OrderByDescending(a => a.Timestamp).Take(50));
    }
}

public sealed record ExecuteCommandRequest(
    Guid RequestId,
    Guid DeviceId,
    string Name,
    Dictionary<string, string>? Parameters);
