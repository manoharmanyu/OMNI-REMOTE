using OmniRemote.Core.Abstractions;
using OmniRemote.Domain.Devices;

namespace OmniRemote.Core.Devices;

/// <summary>Bounded, persisted, local activity history.</summary>
public sealed class ActivityLog : IDisposable
{
    public const int MaxEntries = 500;

    private readonly IActivityRepository _repository;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private List<ActivityEntry> _entries = [];
    private bool _loaded;

    public ActivityLog(IActivityRepository repository) => _repository = repository;

    public void Dispose() => _gate.Dispose();

    public event EventHandler<ActivityEntry>? EntryAdded;

    public IReadOnlyList<ActivityEntry> Entries => _entries;

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded) return;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_loaded) return;
            _entries = [.. (await _repository.LoadAsync(cancellationToken)).OrderByDescending(e => e.Timestamp).Take(MaxEntries)];
            _loaded = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public IReadOnlyList<ActivityEntry> ForDevice(Guid deviceId) => _entries.Where(e => e.DeviceId == deviceId).ToList();

    public async Task AddAsync(ActivityEntry entry, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var next = new List<ActivityEntry>(_entries.Count + 1) { entry };
            next.AddRange(_entries.Take(MaxEntries - 1));
            _entries = next;
            await _repository.SaveAsync(next, CancellationToken.None);
        }
        finally
        {
            _gate.Release();
        }
        EntryAdded?.Invoke(this, entry);
    }

    public async Task ClearAsync(Guid? deviceId = null, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _entries = deviceId is null ? [] : _entries.Where(e => e.DeviceId != deviceId).ToList();
            await _repository.SaveAsync(_entries, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }
}
