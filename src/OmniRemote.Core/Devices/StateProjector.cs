using System.Globalization;
using OmniRemote.Domain.Commands;
using OmniRemote.Domain.Devices;

namespace OmniRemote.Core.Devices;

/// <summary>
/// Pure function that projects a validated command onto a state dictionary.
/// Used by the simulation engine (as the "physical" effect) and by the router to build Assumed state.
/// </summary>
public static class StateProjector
{
    public static Dictionary<string, string> Apply(IReadOnlyDictionary<string, string> current, ValidatedCommand cmd, DateTimeOffset now)
    {
        var s = new Dictionary<string, string>(current, StringComparer.Ordinal);
        switch (cmd.Name)
        {
            case CommandNames.PowerSet:
                s[StateKeys.Power] = cmd.GetBool("on") ? "on" : "off";
                break;
            case CommandNames.VolumeStep:
                s[StateKeys.Volume] = Fmt(Math.Clamp(GetInt(s, StateKeys.Volume, 20) + cmd.GetInt("delta"), 0, 100));
                s[StateKeys.Muted] = "false";
                break;
            case CommandNames.MuteToggle:
                s[StateKeys.Muted] = s.GetValueOrDefault(StateKeys.Muted) == "true" ? "false" : "true";
                break;
            case CommandNames.ChannelStep:
                var ch = GetInt(s, StateKeys.Channel, 1) + cmd.GetInt("delta");
                s[StateKeys.Channel] = Fmt(ch < 1 ? 9999 : ch > 9999 ? 1 : ch);
                break;
            case CommandNames.ChannelSet:
                s[StateKeys.Channel] = Fmt(cmd.GetInt("number"));
                break;
            case CommandNames.NavPress:
                s[StateKeys.LastKey] = cmd.GetString("key");
                break;
            case CommandNames.TemperatureSet:
                s[StateKeys.Temperature] = Fmt(cmd.GetInt("celsius"));
                break;
            case CommandNames.ModeSet:
                s[StateKeys.Mode] = cmd.GetString("mode");
                break;
            case CommandNames.FanSpeedSet:
                s[StateKeys.FanSpeed] = cmd.GetString("speed");
                break;
            case CommandNames.SwingSet:
                s[StateKeys.Swing] = cmd.GetBool("on") ? "true" : "false";
                break;
            case CommandNames.OscillationSet:
                s[StateKeys.Oscillation] = cmd.GetBool("on") ? "true" : "false";
                break;
            case CommandNames.BrightnessSet:
                s[StateKeys.Brightness] = Fmt(cmd.GetInt("percent"));
                break;
            case CommandNames.ColorSet:
                s[StateKeys.Color] = cmd.GetString("hex");
                break;
            case CommandNames.InputSet:
                s[StateKeys.Input] = cmd.GetString("source");
                break;
            case CommandNames.PlaybackPress:
                var action = cmd.GetString("action");
                switch (action)
                {
                    case "play": s[StateKeys.Playback] = "playing"; break;
                    case "pause": s[StateKeys.Playback] = "paused"; break;
                    case "stop": s[StateKeys.Playback] = "stopped"; s[StateKeys.Position] = "0"; break;
                    case "previous" or "next": s[StateKeys.Position] = "0"; break;
                    case "rewind": s[StateKeys.Position] = Fmt(Math.Max(0, GetInt(s, StateKeys.Position, 0) - 10)); break;
                    case "fastforward": s[StateKeys.Position] = Fmt(GetInt(s, StateKeys.Position, 0) + 10); break;
                }
                break;
            case CommandNames.SeekStep:
                s[StateKeys.Position] = Fmt(Math.Max(0, GetInt(s, StateKeys.Position, 0) + cmd.GetInt("seconds")));
                break;
            case CommandNames.PrivacySet:
                s[StateKeys.Privacy] = cmd.GetBool("on") ? "true" : "false";
                if (cmd.GetBool("on")) s[StateKeys.Recording] = "false";
                break;
            case CommandNames.RecordingSet:
                s[StateKeys.Recording] = cmd.GetBool("on") ? "true" : "false";
                break;
            case CommandNames.SnapshotTake:
                s[StateKeys.LastSnapshot] = now.ToString("O", CultureInfo.InvariantCulture);
                break;
            case CommandNames.MotionDetectionSet:
                s[StateKeys.MotionDetection] = cmd.GetBool("on") ? "true" : "false";
                break;
        }
        return s;
    }

    public static int GetInt(IReadOnlyDictionary<string, string> s, string key, int fallback) =>
        s.TryGetValue(key, out var v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : fallback;

    private static string Fmt(int i) => i.ToString(CultureInfo.InvariantCulture);
}
