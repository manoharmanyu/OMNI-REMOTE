using OmniRemote.Core.Abstractions;
using OmniRemote.Domain.Devices;
using OmniRemote.Domain.Validation;

namespace OmniRemote.Core.Devices;

/// <summary>Local registry of configured remotes, backed by an <see cref="IDeviceRepository"/>.</summary>
public sealed class DeviceRegistry : IDisposable
{
    public const int MaxDevices = 200;

    private readonly IDeviceRepository _repository;
    private readonly ISecretStore _secrets;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private List<RemoteDevice> _devices = [];
    private bool _loaded;

    public DeviceRegistry(IDeviceRepository repository, ISecretStore secrets)
    {
        _repository = repository;
        _secrets = secrets;
    }

    public void Dispose() => _gate.Dispose();

    public event EventHandler? Changed;

    public IReadOnlyList<RemoteDevice> Devices => _devices;

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded) return;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_loaded) return;
            var loaded = await _repository.LoadAsync(cancellationToken);
            // Drop anything that no longer passes validation (e.g. tampered local file).
            _devices = [.. loaded.Where(d => d.Validate().Count == 0).Take(MaxDevices)];
            _loaded = true;
        }
        finally
        {
            _gate.Release();
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public RemoteDevice? Find(Guid id) => _devices.FirstOrDefault(d => d.Id == id);

    public async Task<IReadOnlyList<string>> AddAsync(RemoteDevice device, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(device);
        var errors = normalized.Validate();
        if (errors.Count > 0) return errors;

        await EnsureLoadedAsync(cancellationToken);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_devices.Count >= MaxDevices) return ["Device limit reached."];
            if (_devices.Any(d => d.Id == normalized.Id)) return ["Device already exists."];
            var next = new List<RemoteDevice>(_devices) { normalized };
            await _repository.SaveAsync(next, cancellationToken);
            _devices = next;
        }
        finally
        {
            _gate.Release();
        }
        Changed?.Invoke(this, EventArgs.Empty);
        return [];
    }

    public async Task<IReadOnlyList<string>> UpdateAsync(RemoteDevice device, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(device);
        var errors = normalized.Validate();
        if (errors.Count > 0) return errors;

        await EnsureLoadedAsync(cancellationToken);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var index = _devices.FindIndex(d => d.Id == normalized.Id);
            if (index < 0) return ["Device not found."];
            var existing = _devices[index];
            // Identity-defining fields cannot be changed by an update.
            normalized = normalized with { Type = existing.Type, CreatedAt = existing.CreatedAt };
            var next = new List<RemoteDevice>(_devices) { [index] = normalized };
            await _repository.SaveAsync(next, cancellationToken);
            _devices = next;
        }
        finally
        {
            _gate.Release();
        }
        Changed?.Invoke(this, EventArgs.Empty);
        return [];
    }

    public async Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        RemoteDevice? removed;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            removed = _devices.FirstOrDefault(d => d.Id == id);
            if (removed is null) return false;
            var next = _devices.Where(d => d.Id != id).ToList();
            await _repository.SaveAsync(next, cancellationToken);
            _devices = next;
        }
        finally
        {
            _gate.Release();
        }
        if (removed.Connection.SecretRef is { Length: > 0 } secretRef)
            await _secrets.RemoveAsync(secretRef);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private static RemoteDevice Normalize(RemoteDevice d)
    {
        var name = InputRules.ValidateDisplayName(d.Name, InputRules.MaxDeviceNameLength);
        var room = d.Room.Length == 0 ? new NameValidation(true, "", null) : InputRules.ValidateDisplayName(d.Room, InputRules.MaxRoomNameLength);
        return d with
        {
            Name = name.IsValid ? name.Normalized : d.Name,
            Room = room.IsValid ? room.Normalized : d.Room,
            Model = string.IsNullOrWhiteSpace(d.Model) ? null : d.Model.Trim(),
        };
    }
}
