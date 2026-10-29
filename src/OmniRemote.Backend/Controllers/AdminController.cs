using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniRemote.Backend.Data;

namespace OmniRemote.Backend.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminController : ControllerBase
{
    private readonly OmniRemoteDbContext _db;

    public AdminController(OmniRemoteDbContext db)
    {
        _db = db;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetSystemStats(CancellationToken ct)
    {
        var totalUsers = await _db.Users.CountAsync(ct);
        var totalDevices = await _db.Devices.CountAsync(ct);
        var totalCommands = await _db.Activities.CountAsync(ct);

        return Ok(new
        {
            totalUsers,
            totalDevices,
            totalCommands,
            serverTimeUtc = DateTimeOffset.UtcNow,
        });
    }
}
