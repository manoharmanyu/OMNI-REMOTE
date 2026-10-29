using OmniRemote.Core.Simulation;
using OmniRemote.Domain.Commands;
using OmniRemote.Domain.Devices;
using Xunit;

namespace OmniRemote.UnitTests.Simulation;

public sealed class MockDeviceAdapterTests
{
    private readonly SimulationSettings _settings = new() { Scenario = SimulationScenario.Normal, CustomLatencyMs = 0 };
    private readonly MockRandom _random = new();

    private MockDeviceAdapter CreateAdapter() => new(_settings, _random, TimeProvider.System);

    private static RemoteDevice CreateDevice(DeviceType type) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Virtual Unit",
        Type = type,
        BrandId = "virtual",
        BrandName = "Virtual Brand",
        Room = "Simulated Room",
        Connection = ConnectionSettings.Simulation,
        Profile = DeviceProfile.DefaultFor(type),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task PowerOnAndSetTemperature_SucceedsAndUpdatesState()
    {
        var adapter = CreateAdapter();
        var dev = CreateDevice(DeviceType.AirConditioner);

        // Turn On
        var powerCmd = DeviceCommand.Create(dev.Id, CommandNames.PowerSet, TimeProvider.System, ("on", "true"));
        var valPower = CommandValidator.Validate(powerCmd, dev).Command!;
        var resPower = await adapter.ExecuteAsync(dev, valPower, CancellationToken.None);

        Assert.Equal(CommandStatus.Confirmed, resPower.Status);
        Assert.True(resPower.Simulated);
        Assert.Equal("on", resPower.ReportedState![StateKeys.Power]);

        // Change Temperature
        var tempCmd = DeviceCommand.Create(dev.Id, CommandNames.TemperatureSet, TimeProvider.System, ("celsius", "21"));
        var valTemp = CommandValidator.Validate(tempCmd, dev).Command!;
        var resTemp = await adapter.ExecuteAsync(dev, valTemp, CancellationToken.None);

        Assert.Equal(CommandStatus.Confirmed, resTemp.Status);
        Assert.Equal("21", resTemp.ReportedState![StateKeys.Temperature]);
    }

    [Fact]
    public async Task CommandsRejected_WhenDevicePoweredOff()
    {
        var adapter = CreateAdapter();
        var dev = CreateDevice(DeviceType.AirConditioner);

        // AC starts powered off by default in simulated defaults
        var tempCmd = DeviceCommand.Create(dev.Id, CommandNames.TemperatureSet, TimeProvider.System, ("celsius", "22"));
        var valTemp = CommandValidator.Validate(tempCmd, dev).Command!;

        var res = await adapter.ExecuteAsync(dev, valTemp, CancellationToken.None);

        Assert.Equal(CommandStatus.Failed, res.Status);
        Assert.Equal(CommandErrorCode.RejectedByDevice, res.ErrorCode);
    }

    [Fact]
    public async Task ReadSensor_ReturnsSimulatedTelemetry()
    {
        var adapter = CreateAdapter();
        var dev = CreateDevice(DeviceType.TemperatureHumiditySensor);

        var state = await adapter.ReadStateAsync(dev, CancellationToken.None);

        Assert.NotNull(state);
        Assert.Equal(StateOrigin.Simulated, state.Origin);
        Assert.NotNull(state.Get(StateKeys.SensorTemperature));
        Assert.NotNull(state.Get(StateKeys.SensorHumidity));
    }

    [Fact]
    public async Task OfflineScenario_RejectsCommandsWithNotConnected()
    {
        _settings.Scenario = SimulationScenario.Offline;
        var adapter = CreateAdapter();
        var dev = CreateDevice(DeviceType.Tv);

        var powerCmd = DeviceCommand.Create(dev.Id, CommandNames.PowerSet, TimeProvider.System, ("on", "true"));
        var valPower = CommandValidator.Validate(powerCmd, dev).Command!;

        var res = await adapter.ExecuteAsync(dev, valPower, CancellationToken.None);

        Assert.Equal(CommandStatus.Failed, res.Status);
        Assert.Equal(CommandErrorCode.NotConnected, res.ErrorCode);
    }

    private sealed class MockRandom : ISimulationRandom
    {
        public int NextPercent() => 0; // 0% failure / timeout
        public double NextDouble() => 0.5;
    }
}
