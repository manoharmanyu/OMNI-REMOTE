using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniRemote.Backend.Data;
using OmniRemote.Domain.Devices;
using OmniRemote.Domain.Validation;

namespace OmniRemote.Backend.Controllers;

[ApiController]
[Route("api/v1/devices")]
[Authorize]
public sealed class DevicesController : ControllerBase
{
    private readonly OmniRemoteDbContext _db;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(OmniRemoteDbContext db, ILogger<DevicesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(sub, out var userId)) return userId;
        throw new InvalidOperationException("Authenticated user has no valid identifier claim.");
    }

    [HttpGet]
    public async Task<IActionResult> ListDevices(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var devices = await _db.Devices
            .Where(d => d.UserId == userId)
            .ToListAsync(ct);

        return Ok(devices.OrderByDescending(d => d.CreatedAt));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDevice(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var device = await _db.Devices.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, ct);
        if (device is null)
            return NotFound(new { message = "Device not found." });

        return Ok(device);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDevice([FromBody] CreateDeviceRequest req, CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        // Server-side input validation
        var nameValidation = InputRules.ValidateDisplayName(req.Name, InputRules.MaxDeviceNameLength);
        if (!nameValidation.IsValid)
            return BadRequest(new { message = nameValidation.Error });

        if (!InputRules.IsSafeIdentifier(req.BrandId, 64))
            return BadRequest(new { message = "Invalid brand identifier." });

        if (req.Room.Length > 0 && !InputRules.ValidateDisplayName(req.Room, InputRules.MaxRoomNameLength).IsValid)
            return BadRequest(new { message = "Invalid room name." });

        if (!Enum.IsDefined(req.Type))
            return BadRequest(new { message = "Unknown device type." });

        if (!Enum.IsDefined(req.ConnectionKind))
            return BadRequest(new { message = "Unknown connection type." });

        var defaultProfile = DeviceProfile.DefaultFor(req.Type);
        var profileJson = JsonSerializer.Serialize(defaultProfile);

        var entity = new StoredDeviceEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId, // Server-derived identity
            Name = nameValidation.Normalized,
            Type = req.Type,
            BrandId = req.BrandId,
            BrandName = req.BrandName,
            Model = req.Model?.Trim(),
            Room = req.Room.Trim(),
            ConnectionKind = req.ConnectionKind,
            ConnectionValuesJson = req.ConnectionValues is not null ? JsonSerializer.Serialize(req.ConnectionValues) : null,
            SecretRef = req.SecretRef,
            ProfileJson = profileJson,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.Devices.Add(entity);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Device created successfully for user.");
        return CreatedAtAction(nameof(GetDevice), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDevice(Guid id, [FromBody] UpdateDeviceRequest req, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var entity = await _db.Devices.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, ct);
        if (entity is null)
            return NotFound(new { message = "Device not found." });

        var nameValidation = InputRules.ValidateDisplayName(req.Name, InputRules.MaxDeviceNameLength);
        if (!nameValidation.IsValid)
            return BadRequest(new { message = nameValidation.Error });

        if (req.Room.Length > 0 && !InputRules.ValidateDisplayName(req.Room, InputRules.MaxRoomNameLength).IsValid)
            return BadRequest(new { message = "Invalid room name." });

        entity.Name = nameValidation.Normalized;
        entity.Room = req.Room.Trim();
        entity.Model = req.Model?.Trim();
        if (req.ConnectionValues is not null)
            entity.ConnectionValuesJson = JsonSerializer.Serialize(req.ConnectionValues);

        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDevice(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var entity = await _db.Devices.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, ct);
        if (entity is null)
            return NotFound(new { message = "Device not found." });

        _db.Devices.Remove(entity);
        // Also cascade delete user's activities for this device
        var activities = await _db.Activities.Where(a => a.DeviceId == id && a.UserId == userId).ToListAsync(ct);
        _db.Activities.RemoveRange(activities);

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public sealed record CreateDeviceRequest(
    string Name,
    DeviceType Type,
    string BrandId,
    string BrandName,
    string? Model,
    string Room,
    ConnectionKind ConnectionKind,
    Dictionary<string, string>? ConnectionValues,
    string? SecretRef);

public sealed record UpdateDeviceRequest(
    string Name,
    string Room,
    string? Model,
    Dictionary<string, string>? ConnectionValues);
