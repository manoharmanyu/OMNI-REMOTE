using System.ComponentModel.DataAnnotations;
using OmniRemote.Domain.Commands;
using OmniRemote.Domain.Devices;

namespace OmniRemote.Backend.Data;

public sealed class UserEntity
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(256)]
    public required string Email { get; set; }

    [MaxLength(256)]
    public required string PasswordHash { get; set; }

    [MaxLength(32)]
    public required string Role { get; set; } = "User";

    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class StoredDeviceEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [MaxLength(40)]
    public required string Name { get; set; }

    public DeviceType Type { get; set; }

    [MaxLength(64)]
    public required string BrandId { get; set; }

    [MaxLength(64)]
    public required string BrandName { get; set; }

    [MaxLength(60)]
    public string? Model { get; set; }

    [MaxLength(30)]
    public string Room { get; set; } = "";

    public ConnectionKind ConnectionKind { get; set; }

    public string? ConnectionValuesJson { get; set; }

    public string? SecretRef { get; set; }

    public required string ProfileJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class StoredActivityEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid RequestId { get; set; }

    public Guid DeviceId { get; set; }

    [MaxLength(40)]
    public required string DeviceName { get; set; }

    [MaxLength(40)]
    public required string CommandName { get; set; }

    [MaxLength(120)]
    public string Parameters { get; set; } = "";

    public CommandStatus Status { get; set; }

    public CommandErrorCode ErrorCode { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public bool Simulated { get; set; }

    [MaxLength(256)]
    public string? Message { get; set; }
}
