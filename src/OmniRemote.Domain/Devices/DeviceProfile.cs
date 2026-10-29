namespace OmniRemote.Domain.Devices;

/// <summary>
/// The capabilities and parameter ranges a specific configured device actually supports.
/// Commands are validated against this profile, never against the device category alone.
/// </summary>
public sealed record DeviceProfile
{
    public required IReadOnlySet<Capability> Capabilities { get; init; }
    public int MinTemperatureC { get; init; }
    public int MaxTemperatureC { get; init; }
    public IReadOnlyList<string> Modes { get; init; } = [];
    public IReadOnlyList<string> FanSpeeds { get; init; } = [];
    public IReadOnlyList<string> Inputs { get; init; } = [];

    public bool Supports(Capability capability) => Capabilities.Contains(capability);

    /// <summary>Default capability profile for a category. Real integrations may narrow this after discovery.</summary>
    public static DeviceProfile DefaultFor(DeviceType type) => type switch
    {
        DeviceType.AirConditioner => new()
        {
            Capabilities = Set(Capability.Power, Capability.Temperature, Capability.Mode, Capability.FanSpeed, Capability.Swing),
            MinTemperatureC = 16, MaxTemperatureC = 30,
            Modes = ["cool", "heat", "fan", "dry", "auto"],
            FanSpeeds = ["auto", "low", "medium", "high"],
        },
        DeviceType.Tv => new()
        {
            Capabilities = Set(Capability.Power, Capability.Volume, Capability.Mute, Capability.Channel, Capability.Navigation, Capability.Input),
            Inputs = ["tv", "hdmi1", "hdmi2", "hdmi3", "av"],
        },
        DeviceType.SetTopBox => new()
        {
            Capabilities = Set(Capability.Power, Capability.Volume, Capability.Mute, Capability.Channel, Capability.Navigation),
        },
        DeviceType.TvBox => new()
        {
            Capabilities = Set(Capability.Power, Capability.Volume, Capability.Mute, Capability.Navigation, Capability.Playback),
        },
        DeviceType.Projector => new()
        {
            Capabilities = Set(Capability.Power, Capability.Input, Capability.Navigation),
            Inputs = ["hdmi1", "hdmi2", "usb", "vga"],
        },
        DeviceType.Fan => new()
        {
            Capabilities = Set(Capability.Power, Capability.FanSpeed, Capability.Oscillation),
            FanSpeeds = ["1", "2", "3"],
        },
        DeviceType.LightBulb => new()
        {
            Capabilities = Set(Capability.Power, Capability.Brightness, Capability.Color),
        },
        DeviceType.AirPurifier => new()
        {
            Capabilities = Set(Capability.Power, Capability.FanSpeed, Capability.Mode),
            Modes = ["auto", "sleep", "manual"],
            FanSpeeds = ["low", "medium", "high", "turbo"],
        },
        DeviceType.PowerAmplifier => new()
        {
            Capabilities = Set(Capability.Power, Capability.Volume, Capability.Mute, Capability.Input),
            Inputs = ["aux", "optical", "coaxial", "bluetooth", "hdmi-arc"],
        },
        DeviceType.DvdPlayer => new()
        {
            Capabilities = Set(Capability.Power, Capability.Playback, Capability.Seek, Capability.Navigation),
        },
        DeviceType.WaterHeater => new()
        {
            Capabilities = Set(Capability.Power, Capability.Temperature),
            MinTemperatureC = 30, MaxTemperatureC = 75,
        },
        DeviceType.Camera => new()
        {
            Capabilities = Set(Capability.CameraPrivacy, Capability.CameraRecording, Capability.CameraSnapshot, Capability.MotionDetection),
        },
        DeviceType.TemperatureHumiditySensor => new()
        {
            Capabilities = Set(Capability.TemperatureSensor, Capability.HumiditySensor),
        },
        DeviceType.MotionSensor => new()
        {
            Capabilities = Set(Capability.MotionSensor),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    private static HashSet<Capability> Set(params Capability[] caps) => [.. caps];
}
