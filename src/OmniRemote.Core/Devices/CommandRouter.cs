using Microsoft.Extensions.Logging;
using OmniRemote.Core.Abstractions;
using OmniRemote.Domain.Commands;
using OmniRemote.Domain.Devices;
using OmniRemote.Domain.Validation;

namespace OmniRemote.Core.Devices;

public sealed class CommandRouterOptions
{
    /// <summary>Per-attempt timeout.</summary>
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Automatic retries for idempotent commands only. Non-idempotent commands are never retried.</summary>
    public int MaxIdempotentRetries { get; set; } = 1;

    /// <summary>How many recent request ids are remembered to reject replays.</summary>
    public int ReplayWindow { get; set; } = 256;
}

public sealed record CommandOutcome(DeviceCommand Command, CommandResult Result, DeviceState State, int Attempts);

/// <summary>
/// Validates commands, routes them to the adapter for the device's connection kind, enforces timeouts,
/// applies the retry policy, reconciles state and records history.
/// </summary>
public sealed class CommandRouter
{
    private readonly DeviceRegistry _registry;
    private readonly Dictionary<ConnectionKind, IDeviceAdapter> _adapters;
    private readonly DeviceStateStore _stateStore;
    private readonly ActivityLog _activity;
    private readonly TimeProvider _clock;
    private readonly CommandRouterOptions _options;
    private readonly ILogger<CommandRouter> _logger;
    private readonly LinkedList<Guid> _recentOrder = new();
    private readonly HashSet<Guid> _recent = [];
    private readonly Lock _replayLock = new();

    public CommandRouter(
        DeviceRegistry registry,
        IEnumerable<IDeviceAdapter> adapters,
        DeviceStateStore stateStore,
        ActivityLog activity,
        TimeProvider clock,
        CommandRouterOptions options,
        ILogger<CommandRouter> logger)
    {
        _registry = registry;
        _adapters = adapters.ToDictionary(a => a.Kind);
        _stateStore = stateStore;
        _activity = activity;
        _clock = clock;
        _options = options;
        _logger = logger;
    }

    public IDeviceAdapter? AdapterFor(ConnectionKind kind) => _adapters.GetValueOrDefault(kind);

    public async Task<CommandOutcome> SendAsync(DeviceCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var device = _registry.Find(command.DeviceId);
        if (device is null)
            return Finish(command, null, CommandResult.Fail(CommandErrorCode.DeviceNotFound, "Device not found."), 0);

        if (!TryRegisterRequest(command.RequestId))
            return await RecordAsync(device, command, null, CommandResult.Fail(CommandErrorCode.ValidationFailed, "Duplicate request rejected."), 0);

        var validation = CommandValidator.Validate(command, device);
        if (!validation.IsValid)
            return await RecordAsync(device, command, null, CommandResult.Fail(validation.ErrorCode, validation.Error ?? "Invalid command.", device.IsSimulated), 0);

        if (!_adapters.TryGetValue(device.Connection.Kind, out var adapter) || !adapter.IsAvailable)
            return await RecordAsync(device, command, validation.Command, CommandResult.Fail(CommandErrorCode.AdapterUnavailable, "This connection type is not available on this device."), 0);

        var validated = validation.Command!;
        var maxAttempts = validated.Spec.Idempotent ? 1 + Math.Max(0, _options.MaxIdempotentRetries) : 1;
        CommandResult result = CommandResult.Fail(CommandErrorCode.TransportError, "Not sent.");
        var attempts = 0;

        while (attempts < maxAttempts)
        {
            attempts++;
            result = await ExecuteOnceAsync(adapter, device, validated, cancellationToken);
            var retryable = result.Status == CommandStatus.TimedOut || result.ErrorCode == CommandErrorCode.TransportError;
            if (!retryable || cancellationToken.IsCancellationRequested) break;
        }

        return await RecordAsync(device, command, validated, result, attempts);
    }

    private async Task<CommandResult> ExecuteOnceAsync(IDeviceAdapter adapter, RemoteDevice device, ValidatedCommand cmd, CancellationToken callerToken)
    {
        using var timeout = new CancellationTokenSource(_options.CommandTimeout, _clock);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(callerToken, timeout.Token);
        try
        {
            return await adapter.ExecuteAsync(device, cmd, linked.Token);
        }
        catch (OperationCanceledException) when (callerToken.IsCancellationRequested)
        {
            return new CommandResult { Status = CommandStatus.Unknown, ErrorCode = CommandErrorCode.Cancelled, Message = "Cancelled.", Simulated = device.IsSimulated };
        }
        catch (OperationCanceledException)
        {
            return CommandResult.TimedOut(device.IsSimulated);
        }
#pragma warning disable CA1031 // Adapters are a trust boundary: never let transport exceptions crash the UI.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogWarning("Adapter {Kind} failed for command {Command}: {Error}", adapter.Kind, cmd.Name, ex.GetType().Name);
            return CommandResult.Fail(CommandErrorCode.TransportError, "Could not reach the device.", device.IsSimulated);
        }
    }

    private async Task<CommandOutcome> RecordAsync(RemoteDevice device, DeviceCommand command, ValidatedCommand? validated, CommandResult result, int attempts)
    {
        var now = _clock.GetUtcNow();
        var state = validated is null ? _stateStore.Get(device.Id) : _stateStore.Reconcile(device.Id, validated, result, now);
        var outcome = Finish(command, state, result, attempts);

        await _activity.AddAsync(new ActivityEntry
        {
            Id = Guid.NewGuid(),
            RequestId = command.RequestId,
            DeviceId = device.Id,
            DeviceName = device.Name,
            CommandName = InputRules.ForLog(command.Name, 40),
            Parameters = InputRules.ForLog(command.ParameterSummary, 120),
            Status = result.Status,
            ErrorCode = result.ErrorCode,
            Timestamp = now,
            Simulated = result.Simulated || device.IsSimulated,
            Message = result.Message,
        });

        _logger.LogInformation("Command {Command} -> {Status} ({Error}) after {Attempts} attempt(s)",
            InputRules.ForLog(command.Name, 40), result.Status, result.ErrorCode, attempts);
        return outcome;
    }

    private static CommandOutcome Finish(DeviceCommand command, DeviceState? state, CommandResult result, int attempts) =>
        new(command with { Status = result.Status, ErrorCode = result.ErrorCode }, result, state ?? DeviceState.Empty, attempts);

    private bool TryRegisterRequest(Guid requestId)
    {
        lock (_replayLock)
        {
            if (!_recent.Add(requestId)) return false;
            _recentOrder.AddLast(requestId);
            while (_recentOrder.Count > _options.ReplayWindow)
            {
                _recent.Remove(_recentOrder.First!.Value);
                _recentOrder.RemoveFirst();
            }
            return true;
        }
    }
}
