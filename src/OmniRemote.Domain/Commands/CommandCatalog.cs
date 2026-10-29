using System.Globalization;
using OmniRemote.Domain.Devices;

namespace OmniRemote.Domain.Commands;

public enum ParameterKind { Toggle, Number, Choice, ColorHex }

/// <summary>Where an Int range or Enum value list comes from when it depends on the configured device.</summary>
public enum ProfileBinding { None, TemperatureRange, Modes, FanSpeeds, Inputs }

public sealed record ParameterSpec(
    string Name,
    ParameterKind Kind,
    int Min = 0,
    int Max = 0,
    bool DisallowZero = false,
    IReadOnlyList<string>? AllowedValues = null,
    ProfileBinding Binding = ProfileBinding.None);

/// <summary>
/// Declarative command definition. <see cref="Idempotent"/> drives the retry policy:
/// non-idempotent commands (relative steps, key presses) are never retried automatically.
/// </summary>
public sealed record CommandSpec(string Name, Capability Capability, bool Idempotent, IReadOnlyList<ParameterSpec> Parameters);

public static class CommandNames
{
    public const string PowerSet = "power.set";
    public const string VolumeStep = "volume.step";
    public const string MuteToggle = "mute.toggle";
    public const string ChannelStep = "channel.step";
    public const string ChannelSet = "channel.set";
    public const string NavPress = "nav.press";
    public const string TemperatureSet = "temperature.set";
    public const string ModeSet = "mode.set";
    public const string FanSpeedSet = "fanspeed.set";
    public const string SwingSet = "swing.set";
    public const string OscillationSet = "oscillation.set";
    public const string BrightnessSet = "brightness.set";
    public const string ColorSet = "color.set";
    public const string InputSet = "input.set";
    public const string PlaybackPress = "playback.press";
    public const string SeekStep = "seek.step";
    public const string PrivacySet = "privacy.set";
    public const string RecordingSet = "recording.set";
    public const string SnapshotTake = "snapshot.take";
    public const string MotionDetectionSet = "motiondetection.set";
    public const string SensorRefresh = "sensor.refresh";
}

public static class NavKeys
{
    public static readonly IReadOnlyList<string> All = ["up", "down", "left", "right", "ok", "back", "home", "menu", "info"];
}

public static class PlaybackActions
{
    public static readonly IReadOnlyList<string> All = ["play", "pause", "stop", "previous", "next", "rewind", "fastforward"];
}

/// <summary>State keys shared by adapters and UI.</summary>
public static class StateKeys
{
    public const string Power = "power";
    public const string Volume = "volume";
    public const string Muted = "muted";
    public const string Channel = "channel";
    public const string Temperature = "temperature";
    public const string Mode = "mode";
    public const string FanSpeed = "fanSpeed";
    public const string Swing = "swing";
    public const string Oscillation = "oscillation";
    public const string Brightness = "brightness";
    public const string Color = "color";
    public const string Input = "input";
    public const string Playback = "playback";
    public const string Position = "position";
    public const string Privacy = "privacy";
    public const string Recording = "recording";
    public const string MotionDetection = "motionDetection";
    public const string LastSnapshot = "lastSnapshot";
    public const string LastKey = "lastKey";
    public const string SensorTemperature = "sensor.temperature";
    public const string SensorHumidity = "sensor.humidity";
    public const string SensorMotion = "sensor.motion";
    public const string SensorLastMotion = "sensor.lastMotion";
}

public static class CommandCatalog
{
    private static readonly ParameterSpec OnParam = new("on", ParameterKind.Toggle);

    public static IReadOnlyDictionary<string, CommandSpec> All { get; } = new Dictionary<string, CommandSpec>(StringComparer.Ordinal)
    {
        [CommandNames.PowerSet] = new(CommandNames.PowerSet, Capability.Power, true, [OnParam]),
        [CommandNames.VolumeStep] = new(CommandNames.VolumeStep, Capability.Volume, false, [new("delta", ParameterKind.Number, -5, 5, DisallowZero: true)]),
        [CommandNames.MuteToggle] = new(CommandNames.MuteToggle, Capability.Mute, false, []),
        [CommandNames.ChannelStep] = new(CommandNames.ChannelStep, Capability.Channel, false, [new("delta", ParameterKind.Number, -1, 1, DisallowZero: true)]),
        [CommandNames.ChannelSet] = new(CommandNames.ChannelSet, Capability.Channel, true, [new("number", ParameterKind.Number, 1, 9999)]),
        [CommandNames.NavPress] = new(CommandNames.NavPress, Capability.Navigation, false, [new("key", ParameterKind.Choice, AllowedValues: NavKeys.All)]),
        [CommandNames.TemperatureSet] = new(CommandNames.TemperatureSet, Capability.Temperature, true, [new("celsius", ParameterKind.Number, Binding: ProfileBinding.TemperatureRange)]),
        [CommandNames.ModeSet] = new(CommandNames.ModeSet, Capability.Mode, true, [new("mode", ParameterKind.Choice, Binding: ProfileBinding.Modes)]),
        [CommandNames.FanSpeedSet] = new(CommandNames.FanSpeedSet, Capability.FanSpeed, true, [new("speed", ParameterKind.Choice, Binding: ProfileBinding.FanSpeeds)]),
        [CommandNames.SwingSet] = new(CommandNames.SwingSet, Capability.Swing, true, [OnParam]),
        [CommandNames.OscillationSet] = new(CommandNames.OscillationSet, Capability.Oscillation, true, [OnParam]),
        [CommandNames.BrightnessSet] = new(CommandNames.BrightnessSet, Capability.Brightness, true, [new("percent", ParameterKind.Number, 1, 100)]),
        [CommandNames.ColorSet] = new(CommandNames.ColorSet, Capability.Color, true, [new("hex", ParameterKind.ColorHex)]),
        [CommandNames.InputSet] = new(CommandNames.InputSet, Capability.Input, true, [new("source", ParameterKind.Choice, Binding: ProfileBinding.Inputs)]),
        [CommandNames.PlaybackPress] = new(CommandNames.PlaybackPress, Capability.Playback, false, [new("action", ParameterKind.Choice, AllowedValues: PlaybackActions.All)]),
        [CommandNames.SeekStep] = new(CommandNames.SeekStep, Capability.Seek, false, [new("seconds", ParameterKind.Number, -600, 600, DisallowZero: true)]),
        [CommandNames.PrivacySet] = new(CommandNames.PrivacySet, Capability.CameraPrivacy, true, [OnParam]),
        [CommandNames.RecordingSet] = new(CommandNames.RecordingSet, Capability.CameraRecording, true, [OnParam]),
        [CommandNames.SnapshotTake] = new(CommandNames.SnapshotTake, Capability.CameraSnapshot, false, []),
        [CommandNames.MotionDetectionSet] = new(CommandNames.MotionDetectionSet, Capability.MotionDetection, true, [OnParam]),
        [CommandNames.SensorRefresh] = new(CommandNames.SensorRefresh, Capability.TemperatureSensor, true, []),
    };

    public static bool TryGet(string name, out CommandSpec spec) => All.TryGetValue(name, out spec!);

    /// <summary>sensor.refresh is valid for any sensor capability.</summary>
    public static bool IsCapabilitySatisfied(CommandSpec spec, DeviceProfile profile) =>
        spec.Name == CommandNames.SensorRefresh
            ? profile.Supports(Capability.TemperatureSensor) || profile.Supports(Capability.HumiditySensor) || profile.Supports(Capability.MotionSensor)
            : profile.Supports(spec.Capability);

    internal static string FormatBool(bool b) => b ? "true" : "false";
    internal static string FormatInt(int i) => i.ToString(CultureInfo.InvariantCulture);
}
