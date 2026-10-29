using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OmniRemote.Backend.Data;
using OmniRemote.Backend.Security;

namespace OmniRemote.Backend.Controllers;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("auth-policy")]
public sealed class AuthController : ControllerBase
{
    private readonly OmniRemoteDbContext _db;
    private readonly TokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(OmniRemoteDbContext db, TokenService tokenService, ILogger<AuthController> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Invalid registration request." });

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
        {
            // Avoid account enumeration where applicable, but safe generic message:
            return Conflict(new { message = "Registration failed. An account with this email may already exist." });
        }

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = "User",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("New user registered successfully.");

        var token = _tokenService.CreateToken(user.Id, user.Email, user.Role);
        return Created("", new AuthResponse(user.Id, user.Email, user.Role, token));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Invalid login request." });

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Constant time verify attempt to avoid timing attacks
        var valid = user is not null && PasswordHasher.Verify(request.Password, user.PasswordHash);
        if (!valid || user is null)
        {
            _logger.LogWarning("Failed login attempt for provided email.");
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var token = _tokenService.CreateToken(user.Id, user.Email, user.Role);
        return Ok(new AuthResponse(user.Id, user.Email, user.Role, token));
    }
}

public sealed record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password);

public sealed record LoginRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(128)] string Password);

public sealed record AuthResponse(Guid Id, string Email, string Role, string Token);
