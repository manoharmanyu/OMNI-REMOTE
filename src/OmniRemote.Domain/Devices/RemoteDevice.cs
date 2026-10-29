using System.Globalization;
using OmniRemote.Domain.Validation;

namespace OmniRemote.Domain.Devices;

/// <summary>Non-secret transport settings. Secrets are stored separately (platform secure storage) and referenced by key.</summary>
public sealed record ConnectionSettings
{
    public ConnectionKind Kind { get; init; } = ConnectionKind.Simulation;

    /// <summary>Non-sensitive settings such as host, entity id or BLE identifier.</summary>
    public IReadOnlyDictionary<string, string> Values { get; init; } = new Dictionary<string, string>();

    /// <summary>Key of a secret held in secure storage (e.g. a Home Assistant token). Never the secret itself.</summary>
    public string? SecretRef { get; init; }

    public static ConnectionSettings Simulation { get; } = new();

    public string? Get(string key) => Values.TryGetValue(key, out var v) ? v : null;
}

/// <summary>A configured remote control.</summary>
public sealed record RemoteDevice
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DeviceType Type { get; init; }
    public required string BrandId { get; init; }
    public required string BrandName { get; init; }
    public string? Model { get; init; }
    public string Room { get; init; } = "";
    public required ConnectionSettings Connection { get; init; }
    public required DeviceProfile Profile { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    public bool IsSimulated => Connection.Kind == ConnectionKind.Simulation;

    public string Subtitle => string.IsNullOrEmpty(Room)
        ? string.Create(CultureInfo.InvariantCulture, $"{BrandName} · {DeviceTypeInfo.DisplayName(Type)}")
        : string.Create(CultureInfo.InvariantCulture, $"{Room} · {BrandName}");

    /// <summary>Validates user-controlled fields. Returns a list of safe, user-presentable errors.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (Id == Guid.Empty) errors.Add("Device id is required.");
        if (!Enum.IsDefined(Type)) errors.Add("Unknown device type.");
        if (!Enum.IsDefined(Connection.Kind)) errors.Add("Unknown connection type.");
        if (!InputRules.ValidateDisplayName(Name, InputRules.MaxDeviceNameLength).IsValid) errors.Add("Name is invalid.");
        if (Room.Length > 0 && !InputRules.ValidateDisplayName(Room, InputRules.MaxRoomNameLength).IsValid) errors.Add("Room is invalid.");
        if (!InputRules.IsSafeIdentifier(BrandId, 64)) errors.Add("Brand is invalid.");
        if (Model is not null && Model.Length > 0 && !InputRules.ValidateDisplayName(Model, 60).IsValid) errors.Add("Model is invalid.");
        foreach (var (k, v) in Connection.Values)
        {
            if (!InputRules.IsSafeIdentifier(k, 32) || v.Length > 256 || InputRules.ContainsUnsafeControlChars(v))
            {
                errors.Add("Connection settings are invalid.");
                break;
            }
        }
        return errors;
    }
}
