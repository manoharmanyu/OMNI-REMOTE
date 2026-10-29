using Microsoft.Extensions.Logging.Abstractions;
using OmniRemote.Core.Abstractions;
using OmniRemote.Core.Devices;
using OmniRemote.Domain.Commands;
using OmniRemote.Domain.Devices;
using Xunit;

namespace OmniRemote.UnitTests.Devices;

public sealed class CommandRouterTests
{
    private readonly InMemoryDeviceRepository _repo = new();
    private readonly InMemorySecretStore _secrets = new();
    private readonly DeviceStateStore _stateStore = new();
    private readonly ActivityLog _activity;
    private readonly FlakyAdapter _adapter = new();
    private readonly DeviceRegistry _registry;

    public CommandRouterTests()
    {
        _registry = new DeviceRegistry(_repo, _secrets);
        _activity = new ActivityLog(new InMemoryActivityRepository());
    }

    private CommandRouter CreateRouter(int retries = 1)
    {
        var options = new CommandRouterOptions
        {
            CommandTimeout = TimeSpan.FromSeconds(2),
            MaxIdempotentRetries = retries,
        };
        return new CommandRouter(
            _registry,
            [_adapter],
            _stateStore,
            _activity,
            TimeProvider.System,
            options,
            NullLogger<CommandRouter>.Instance);
    }

    private async Task<RemoteDevice> RegisterDeviceAsync()
    {
        var dev = new RemoteDevice
        {
            Id = Guid.NewGuid(),
            Name = "Router Unit",
            Type = DeviceType.Tv,
            BrandId = "sony",
            BrandName = "Sony",
            Room = "Living",
            Connection = ConnectionSettings.Simulation,
            Profile = DeviceProfile.DefaultFor(DeviceType.Tv),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await _registry.AddAsync(dev);
        return dev;
    }

    [Fact]
    public async Task ReplayAttack_SameRequestIdRejected()
    {
        var router = CreateRouter();
        var dev = await RegisterDeviceAsync();

        var requestId = Guid.NewGuid();
        var cmd1 = new DeviceCommand
        {
            RequestId = requestId,
            DeviceId = dev.Id,
            Name = CommandNames.PowerSet,
            Parameters = new Dictionary<string, string> { ["on"] = "true" },
            RequestedAt = DateTimeOffset.UtcNow,
        };

        var outcome1 = await router.SendAsync(cmd1);
        Assert.Equal(CommandStatus.Confirmed, outcome1.Result.Status);

        // Attempting to send identical RequestId again (replay)
        var cmd2 = new DeviceCommand
        {
            RequestId = requestId,
            DeviceId = dev.Id,
            Name = CommandNames.PowerSet,
            Parameters = new Dictionary<string, string> { ["on"] = "false" },
            RequestedAt = DateTimeOffset.UtcNow,
        };

        var outcome2 = await router.SendAsync(cmd2);
        Assert.Equal(CommandStatus.Failed, outcome2.Result.Status);
        Assert.Equal(CommandErrorCode.ValidationFailed, outcome2.Result.ErrorCode);
    }

    [Fact]
    public async Task NonIdempotentCommand_NeverRetried()
    {
        var router = CreateRouter(retries: 3);
        var dev = await RegisterDeviceAsync();

        _adapter.SimulateFailures = 2; // will fail twice then succeed

        // volume.step is NOT idempotent
        var cmd = DeviceCommand.Create(dev.Id, CommandNames.VolumeStep, TimeProvider.System, ("delta", "1"));
        var outcome = await router.SendAsync(cmd);

        // Should attempt only once and fail immediately
        Assert.Equal(1, outcome.Attempts);
        Assert.Equal(CommandStatus.Failed, outcome.Result.Status);
    }

    [Fact]
    public async Task IdempotentCommand_RetriesAndSucceeds()
    {
        var router = CreateRouter(retries: 2);
        var dev = await RegisterDeviceAsync();

        _adapter.SimulateFailures = 1; // fails once then succeeds

        // power.set IS idempotent
        var cmd = DeviceCommand.Create(dev.Id, CommandNames.PowerSet, TimeProvider.System, ("on", "true"));
        var outcome = await router.SendAsync(cmd);

        Assert.Equal(2, outcome.Attempts);
        Assert.Equal(CommandStatus.Confirmed, outcome.Result.Status);
    }

    private sealed class FlakyAdapter : IDeviceAdapter
    {
        public ConnectionKind Kind => ConnectionKind.Simulation;
        public bool IsAvailable => true;
        public string VerificationNote => "Flaky test adapter";
        public int SimulateFailures { get; set; }

        public ConnectionState GetConnectionState(Guid deviceId) => ConnectionState.Connected;
        public Task<ConnectionState> ConnectAsync(RemoteDevice device, CancellationToken cancellationToken) => Task.FromResult(ConnectionState.Connected);
        public Task DisconnectAsync(RemoteDevice device, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<CommandResult> ExecuteAsync(RemoteDevice device, ValidatedCommand command, CancellationToken cancellationToken)
        {
            if (SimulateFailures > 0)
            {
                SimulateFailures--;
                return Task.FromResult(CommandResult.Fail(CommandErrorCode.TransportError, "Simulated transient failure."));
            }

            return Task.FromResult(new CommandResult
            {
                Status = CommandStatus.Confirmed,
                Simulated = true,
                Message = "Success",
                ReportedState = new Dictionary<string, string> { [StateKeys.Power] = "on" },
            });
        }

        public Task<DeviceState?> ReadStateAsync(RemoteDevice device, CancellationToken cancellationToken) => Task.FromResult<DeviceState?>(null);
    }

    private sealed class InMemoryDeviceRepository : IDeviceRepository
    {
        private IReadOnlyList<RemoteDevice> _list = [];
        public Task<IReadOnlyList<RemoteDevice>> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(_list);
        public Task SaveAsync(IReadOnlyList<RemoteDevice> devices, CancellationToken cancellationToken) { _list = devices; return Task.CompletedTask; }
    }

    private sealed class InMemoryActivityRepository : IActivityRepository
    {
        private IReadOnlyList<ActivityEntry> _list = [];
        public Task<IReadOnlyList<ActivityEntry>> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(_list);
        public Task SaveAsync(IReadOnlyList<ActivityEntry> entries, CancellationToken cancellationToken) { _list = entries; return Task.CompletedTask; }
    }

    private sealed class InMemorySecretStore : ISecretStore
    {
        private readonly Dictionary<string, string> _dict = new();
        public Task<string?> GetAsync(string key) => Task.FromResult(_dict.GetValueOrDefault(key));
        public Task SetAsync(string key, string value) { _dict[key] = value; return Task.CompletedTask; }
        public Task RemoveAsync(string key) { _dict.Remove(key); return Task.CompletedTask; }
    }
}
