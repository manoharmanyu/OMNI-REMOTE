using OmniRemote.Domain.Commands;
using OmniRemote.Domain.Devices;
using Xunit;

namespace OmniRemote.UnitTests.Commands;

public sealed class CommandValidatorTests
{
    private static RemoteDevice CreateDevice(DeviceType type) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Unit",
        Type = type,
        BrandId = "test-brand",
        BrandName = "Test Brand",
        Room = "Lab",
        Connection = ConnectionSettings.Simulation,
        Profile = DeviceProfile.DefaultFor(type),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public void ValidTemperatureCommand_Succeeds()
    {
        var dev = CreateDevice(DeviceType.AirConditioner);
        var cmd = DeviceCommand.Create(dev.Id, CommandNames.TemperatureSet, TimeProvider.System, ("celsius", "22"));

        var result = CommandValidator.Validate(cmd, dev);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Command);
        Assert.Equal(22, result.Command.GetInt("celsius"));
    }

    [Fact]
    public void TemperatureOutOfRange_FailsValidation()
    {
        var dev = CreateDevice(DeviceType.AirConditioner);
        var cmd = DeviceCommand.Create(dev.Id, CommandNames.TemperatureSet, TimeProvider.System, ("celsius", "45"));

        var result = CommandValidator.Validate(cmd, dev);

        Assert.False(result.IsValid);
        Assert.Equal(CommandErrorCode.ValidationFailed, result.ErrorCode);
    }

    [Fact]
    public void UnsupportedCapability_FailsValidation()
    {
        // Fan does not support TemperatureSet
        var dev = CreateDevice(DeviceType.Fan);
        var cmd = DeviceCommand.Create(dev.Id, CommandNames.TemperatureSet, TimeProvider.System, ("celsius", "22"));

        var result = CommandValidator.Validate(cmd, dev);

        Assert.False(result.IsValid);
        Assert.Equal(CommandErrorCode.UnsupportedCapability, result.ErrorCode);
    }

    [Fact]
    public void TargetMismatch_FailsValidation()
    {
        var dev = CreateDevice(DeviceType.Tv);
        var cmd = DeviceCommand.Create(Guid.NewGuid(), CommandNames.PowerSet, TimeProvider.System, ("on", "true"));

        var result = CommandValidator.Validate(cmd, dev);

        Assert.False(result.IsValid);
        Assert.Equal(CommandErrorCode.DeviceMismatch, result.ErrorCode);
    }

    [Fact]
    public void ValidColorHex_SucceedsAndNormalizes()
    {
        var dev = CreateDevice(DeviceType.LightBulb);
        var cmd = DeviceCommand.Create(dev.Id, CommandNames.ColorSet, TimeProvider.System, ("hex", "#ff007f"));

        var result = CommandValidator.Validate(cmd, dev);

        Assert.True(result.IsValid);
        Assert.Equal("#FF007F", result.Command!.GetString("hex"));
    }

    [Theory]
    [InlineData("not-a-hex")]
    [InlineData("#FFF")]
    [InlineData("#GG0000")]
    [InlineData("<script>")]
    public void InvalidColorHex_FailsValidation(string invalidHex)
    {
        var dev = CreateDevice(DeviceType.LightBulb);
        var cmd = DeviceCommand.Create(dev.Id, CommandNames.ColorSet, TimeProvider.System, ("hex", invalidHex));

        var result = CommandValidator.Validate(cmd, dev);

        Assert.False(result.IsValid);
    }
}
