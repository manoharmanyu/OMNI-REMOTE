using OmniRemote.Domain.Devices;
using Xunit;

namespace OmniRemote.UnitTests.Devices;

public sealed class DeviceProfileTests
{
    [Theory]
    [InlineData(DeviceType.AirConditioner, Capability.Temperature)]
    [InlineData(DeviceType.AirConditioner, Capability.Power)]
    [InlineData(DeviceType.AirConditioner, Capability.Mode)]
    [InlineData(DeviceType.AirConditioner, Capability.FanSpeed)]
    [InlineData(DeviceType.AirConditioner, Capability.Swing)]
    [InlineData(DeviceType.Tv, Capability.Volume)]
    [InlineData(DeviceType.Tv, Capability.Channel)]
    [InlineData(DeviceType.Tv, Capability.Navigation)]
    [InlineData(DeviceType.Fan, Capability.FanSpeed)]
    [InlineData(DeviceType.Fan, Capability.Oscillation)]
    [InlineData(DeviceType.LightBulb, Capability.Brightness)]
    [InlineData(DeviceType.LightBulb, Capability.Color)]
    [InlineData(DeviceType.AirPurifier, Capability.Mode)]
    [InlineData(DeviceType.PowerAmplifier, Capability.Input)]
    [InlineData(DeviceType.DvdPlayer, Capability.Playback)]
    [InlineData(DeviceType.WaterHeater, Capability.Temperature)]
    [InlineData(DeviceType.Camera, Capability.CameraPrivacy)]
    [InlineData(DeviceType.TemperatureHumiditySensor, Capability.TemperatureSensor)]
    [InlineData(DeviceType.MotionSensor, Capability.MotionSensor)]
    public void DefaultProfile_SupportsExpectedCapabilities(DeviceType type, Capability expected)
    {
        var profile = DeviceProfile.DefaultFor(type);
        Assert.True(profile.Supports(expected));
    }

    [Fact]
    public void SelectionOrder_Contains14CategoriesInExactSequence()
    {
        var order = DeviceTypeInfo.SelectionOrder;
        Assert.Equal(14, order.Count);
        Assert.Equal(DeviceType.AirConditioner, order[0]);
        Assert.Equal(DeviceType.SetTopBox, order[1]);
        Assert.Equal(DeviceType.Tv, order[2]);
        Assert.Equal(DeviceType.TvBox, order[3]);
        Assert.Equal(DeviceType.Projector, order[4]);
        Assert.Equal(DeviceType.Fan, order[5]);
        Assert.Equal(DeviceType.LightBulb, order[6]);
        Assert.Equal(DeviceType.AirPurifier, order[7]);
        Assert.Equal(DeviceType.PowerAmplifier, order[8]);
        Assert.Equal(DeviceType.DvdPlayer, order[9]);
        Assert.Equal(DeviceType.WaterHeater, order[10]);
        Assert.Equal(DeviceType.Camera, order[11]);
    }
}
