using System.Text.Json;
using System.Text.Json.Serialization;
using OmniRemote.Core.Abstractions;
using OmniRemote.Core.Simulation;
using OmniRemote.Domain.Commands;
using OmniRemote.Domain.Devices;

namespace OmniRemote.Infrastructure.Persistence;

/// <summary>
/// Stores NON-SENSITIVE data (remotes, history, settings) as JSON files in the app's private data directory.
/// Writes are atomic (temp file + move) so a crash cannot leave a half-written file.
/// Secrets never go here — they go to <see cref="ISecretStore"/>.
/// </summary>
public sealed class JsonFileStore : IDeviceRepository, IActivityRepository, ISettingsStore, IDisposable
{
    private const int MaxFileBytes = 4 * 1024 * 1024;
    private readonly string _directory;
    private readonly SemaphoreSlim _io = new(1, 1);

    public JsonFileStore(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(directory);
    }

    public void Dispose() => _io.Dispose();

    public async Task<IReadOnlyList<RemoteDevice>> LoadAsync(CancellationToken cancellationToken)
    {
        var file = await ReadAsync("devices.json", PersistenceJsonContext.Default.DeviceFile, cancellationToken);
        return file?.Devices.Select(DeviceRecord.ToDomain).OfType<RemoteDevice>().ToList() ?? [];
    }

    public Task SaveAsync(IReadOnlyList<RemoteDevice> devices, CancellationToken cancellationToken) =>
        WriteAsync("devices.json", new DeviceFile { Version = 1, Devices = devices.Select(DeviceRecord.FromDomain).ToList() },
            PersistenceJsonContext.Default.DeviceFile, cancellationToken);

    async Task<IReadOnlyList<ActivityEntry>> IActivityRepository.LoadAsync(CancellationToken cancellationToken)
    {
        var file = await ReadAsync("activity.json", PersistenceJsonContext.Default.ActivityFile, cancellationToken);
        return file?.Entries ?? [];
    }

    public Task SaveAsync(IReadOnlyList<ActivityEntry> entries, CancellationToken cancellationToken) =>
        WriteAsync("activity.json", new ActivityFile { Version = 1, Entries = [.. entries] }, PersistenceJsonContext.Default.ActivityFile, cancellationToken);

    public async Task<T?> LoadAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        var info = (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>)PersistenceJsonContext.Default.GetTypeInfo(typeof(T))!;
        return await ReadAsync(SettingsFile(key), info, cancellationToken);
    }

    public Task SaveAsync<T>(string key, T value, CancellationToken cancellationToken) where T : class
    {
        var info = (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>)PersistenceJsonContext.Default.GetTypeInfo(typeof(T))!;
        return WriteAsync(SettingsFile(key), value, info, cancellationToken);
    }

    private static string SettingsFile(string key)
    {
        if (!Domain.Validation.InputRules.IsSafeIdentifier(key, 40)) throw new ArgumentException("Invalid settings key.", nameof(key));
        return $"settings.{key}.json";
    }

    private async Task<T?> ReadAsync<T>(string name, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> info, CancellationToken ct) where T : class
    {
        var path = Path.Combine(_directory, name);
        await _io.WaitAsync(ct);
        try
        {
            if (!File.Exists(path)) return null;
            if (new FileInfo(path).Length > MaxFileBytes) return null;
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync(stream, info, ct);
        }
        catch (JsonException)
        {
            // Corrupt or tampered file: start fresh rather than crash. The bad file is preserved for diagnosis.
            File.Move(path, path + ".corrupt", overwrite: true);
            return null;
        }
        finally
        {
            _io.Release();
        }
    }

    private async Task WriteAsync<T>(string name, T value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> info, CancellationToken ct)
    {
        var path = Path.Combine(_directory, name);
        var temp = path + ".tmp";
        await _io.WaitAsync(ct);
        try
        {
            await using (var stream = File.Create(temp))
            {
                await JsonSerializer.SerializeAsync(stream, value, info, ct);
            }
            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            _io.Release();
        }
    }
}

public sealed class DeviceFile
{
    public int Version { get; set; }
    public List<DeviceRecord> Devices { get; set; } = [];
}

public sealed class ActivityFile
{
    public int Version { get; set; }
    public List<ActivityEntry> Entries { get; set; } = [];
}

/// <summary>Versionable persistence shape, decoupled from the domain record.</summary>
public sealed class DeviceRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public DeviceType Type { get; set; }
    public string BrandId { get; set; } = "";
    public string BrandName { get; set; } = "";
    public string? Model { get; set; }
    public string Room { get; set; } = "";
    public ConnectionKind ConnectionKind { get; set; }
    public Dictionary<string, string> ConnectionValues { get; set; } = [];
    public string? SecretRef { get; set; }
    public List<Capability> Capabilities { get; set; } = [];
    public int MinTemperatureC { get; set; }
    public int MaxTemperatureC { get; set; }
    public List<string> Modes { get; set; } = [];
    public List<string> FanSpeeds { get; set; } = [];
    public List<string> Inputs { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }

    public static DeviceRecord FromDomain(RemoteDevice d) => new()
    {
        Id = d.Id, Name = d.Name, Type = d.Type, BrandId = d.BrandId, BrandName = d.BrandName, Model = d.Model, Room = d.Room,
        ConnectionKind = d.Connection.Kind, ConnectionValues = new(d.Connection.Values), SecretRef = d.Connection.SecretRef,
        Capabilities = [.. d.Profile.Capabilities], MinTemperatureC = d.Profile.MinTemperatureC, MaxTemperatureC = d.Profile.MaxTemperatureC,
        Modes = [.. d.Profile.Modes], FanSpeeds = [.. d.Profile.FanSpeeds], Inputs = [.. d.Profile.Inputs], CreatedAt = d.CreatedAt,
    };

    public static RemoteDevice? ToDomain(DeviceRecord r)
    {
        if (!Enum.IsDefined(r.Type) || !Enum.IsDefined(r.ConnectionKind)) return null;
        // A stored profile can only narrow, never widen, the category's known capabilities.
        var allowed = DeviceProfile.DefaultFor(r.Type).Capabilities;
        return new RemoteDevice
        {
            Id = r.Id, Name = r.Name, Type = r.Type, BrandId = r.BrandId, BrandName = r.BrandName, Model = r.Model, Room = r.Room ?? "",
            Connection = new ConnectionSettings { Kind = r.ConnectionKind, Values = r.ConnectionValues ?? [], SecretRef = r.SecretRef },
            Profile = new DeviceProfile
            {
                Capabilities = r.Capabilities.Where(allowed.Contains).ToHashSet(),
                MinTemperatureC = r.MinTemperatureC, MaxTemperatureC = r.MaxTemperatureC,
                Modes = r.Modes ?? [], FanSpeeds = r.FanSpeeds ?? [], Inputs = r.Inputs ?? [],
            },
            CreatedAt = r.CreatedAt,
        };
    }
}

/// <summary>App preferences that are not secret.</summary>
public sealed class AppPreferences
{
    public string? BackendBaseUrl { get; set; }
    public string? AccountEmail { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = false, UseStringEnumConverter = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(DeviceFile))]
[JsonSerializable(typeof(ActivityFile))]
[JsonSerializable(typeof(SimulationSettings))]
[JsonSerializable(typeof(AppPreferences))]
[JsonSerializable(typeof(CommandStatus))]
public sealed partial class PersistenceJsonContext : JsonSerializerContext;
