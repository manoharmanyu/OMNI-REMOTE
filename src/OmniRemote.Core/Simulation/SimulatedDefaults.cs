using System.Globalization;
using OmniRemote.Domain.Commands;
using OmniRemote.Domain.Devices;

namespace OmniRemote.Core.Simulation;

/// <summary>Initial state of a freshly created virtual device.</summary>
public static class SimulatedDefaults
{
    public static Dictionary<string, string> InitialState(RemoteDevice device)
    {
        var p = device.Profile;
        var s = new Dictionary<string, string>(StringComparer.Ordinal);
        if (p.Supports(Capability.Power)) s[StateKeys.Power] = "off";
        if (p.Supports(Capability.Volume)) s[StateKeys.Volume] = "20";
        if (p.Supports(Capability.Mute)) s[StateKeys.Muted] = "false";
        if (p.Supports(Capability.Channel)) s[StateKeys.Channel] = "1";
        if (p.Supports(Capability.Temperature))
            s[StateKeys.Temperature] = Math.Clamp(device.Type == DeviceType.WaterHeater ? 50 : 24, p.MinTemperatureC, p.MaxTemperatureC).ToString(CultureInfo.InvariantCulture);
        if (p.Supports(Capability.Mode) && p.Modes.Count > 0) s[StateKeys.Mode] = p.Modes[0];
        if (p.Supports(Capability.FanSpeed) && p.FanSpeeds.Count > 0) s[StateKeys.FanSpeed] = p.FanSpeeds[0];
        if (p.Supports(Capability.Swing)) s[StateKeys.Swing] = "false";
        if (p.Supports(Capability.Oscillation)) s[StateKeys.Oscillation] = "false";
        if (p.Supports(Capability.Brightness)) s[StateKeys.Brightness] = "80";
        if (p.Supports(Capability.Color)) s[StateKeys.Color] = "#FFFFFF";
        if (p.Supports(Capability.Input) && p.Inputs.Count > 0) s[StateKeys.Input] = p.Inputs[0];
        if (p.Supports(Capability.Playback)) s[StateKeys.Playback] = "stopped";
        if (p.Supports(Capability.Seek) || p.Supports(Capability.Playback)) s[StateKeys.Position] = "0";
        if (p.Supports(Capability.CameraPrivacy)) s[StateKeys.Privacy] = "false";
        if (p.Supports(Capability.CameraRecording)) s[StateKeys.Recording] = "false";
        if (p.Supports(Capability.MotionDetection)) s[StateKeys.MotionDetection] = "true";
        return s;
    }
}
