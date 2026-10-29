using System.Globalization;
using System.Windows.Input;
using OmniRemote.Core.Devices;
using OmniRemote.Domain.Commands;
using OmniRemote.Domain.Devices;
using DeviceType = OmniRemote.Domain.Devices.DeviceType;

namespace OmniRemote.Mobile.ViewModels;

[QueryProperty(nameof(DeviceIdQuery), "deviceId")]
public sealed class RemoteControlViewModel : BaseViewModel
{
    private readonly DeviceRegistry _registry;
    private readonly CommandRouter _router;
    private readonly DeviceStateStore _stateStore;
    private readonly TimeProvider _clock;

    private Guid _deviceId;
    private RemoteDevice? _device;

    private string _deviceName = string.Empty;
    private string _subtitle = string.Empty;
    private bool _isSimulated = true;
    private bool _isPoweredOn;
    private string _statusPill = "SIMULATION";
    private string _lastFeedback = string.Empty;

    // AC State
    private int _temperatureC = 24;
    private string _acMode = "cool";
    private string _acFanSpeed = "auto";
    private bool _isSwingOn;

    // TV State
    private int _volume = 20;
    private bool _isMuted;
    private int _channel = 1;
    private string _lastNavKey = string.Empty;

    // Fan State
    private string _fanSpeed = "1";
    private bool _isOscillationOn;

    // Light State
    private int _brightness = 80;
    private string _colorHex = "#FFFFFF";

    // Playback State
    private string _playbackState = "stopped";

    // Camera State
    private bool _isPrivacyOn;
    private bool _isRecording;

    // Sensor State
    private string _sensorTemp = "24.0°C";
    private string _sensorHumidity = "50%";
    private string _sensorMotion = "No motion";

    public string DeviceIdQuery
    {
        set
        {
            if (Guid.TryParse(value, out var id))
            {
                _deviceId = id;
                LoadDevice();
            }
        }
    }

    public RemoteDevice? Device => _device;
    public DeviceType DeviceType => _device?.Type ?? DeviceType.AirConditioner;

    public string DeviceName
    {
        get => _deviceName;
        set => SetProperty(ref _deviceName, value);
    }

    public string Subtitle
    {
        get => _subtitle;
        set => SetProperty(ref _subtitle, value);
    }

    public bool IsSimulated
    {
        get => _isSimulated;
        set => SetProperty(ref _isSimulated, value);
    }

    public bool IsPoweredOn
    {
        get => _isPoweredOn;
        set => SetProperty(ref _isPoweredOn, value);
    }

    public string StatusPill
    {
        get => _statusPill;
        set => SetProperty(ref _statusPill, value);
    }

    public string LastFeedback
    {
        get => _lastFeedback;
        set => SetProperty(ref _lastFeedback, value);
    }

    // AC
    public int TemperatureC
    {
        get => _temperatureC;
        set => SetProperty(ref _temperatureC, value);
    }

    public string AcMode
    {
        get => _acMode;
        set => SetProperty(ref _acMode, value);
    }

    public string AcFanSpeed
    {
        get => _acFanSpeed;
        set => SetProperty(ref _acFanSpeed, value);
    }

    public bool IsSwingOn
    {
        get => _isSwingOn;
        set => SetProperty(ref _isSwingOn, value);
    }

    // TV
    public int Volume
    {
        get => _volume;
        set => SetProperty(ref _volume, value);
    }

    public bool IsMuted
    {
        get => _isMuted;
        set => SetProperty(ref _isMuted, value);
    }

    public int Channel
    {
        get => _channel;
        set => SetProperty(ref _channel, value);
    }

    public string LastNavKey
    {
        get => _lastNavKey;
        set => SetProperty(ref _lastNavKey, value);
    }

    // Fan
    public string FanSpeed
    {
        get => _fanSpeed;
        set => SetProperty(ref _fanSpeed, value);
    }

    public bool IsOscillationOn
    {
        get => _isOscillationOn;
        set => SetProperty(ref _isOscillationOn, value);
    }

    // Light
    public int Brightness
    {
        get => _brightness;
        set => SetProperty(ref _brightness, value);
    }

    public string ColorHex
    {
        get => _colorHex;
        set => SetProperty(ref _colorHex, value);
    }

    // Playback
    public string PlaybackState
    {
        get => _playbackState;
        set => SetProperty(ref _playbackState, value);
    }

    // Camera
    public bool IsPrivacyOn
    {
        get => _isPrivacyOn;
        set => SetProperty(ref _isPrivacyOn, value);
    }

    public bool IsRecording
    {
        get => _isRecording;
        set => SetProperty(ref _isRecording, value);
    }

    // Sensor
    public string SensorTemp
    {
        get => _sensorTemp;
        set => SetProperty(ref _sensorTemp, value);
    }

    public string SensorHumidity
    {
        get => _sensorHumidity;
        set => SetProperty(ref _sensorHumidity, value);
    }

    public string SensorMotion
    {
        get => _sensorMotion;
        set => SetProperty(ref _sensorMotion, value);
    }

    // Commands
    public ICommand TogglePowerCommand { get; }
    public ICommand AdjustTempCommand { get; }
    public ICommand SetModeCommand { get; }
    public ICommand SetFanSpeedCommand { get; }
    public ICommand ToggleSwingCommand { get; }
    public ICommand AdjustVolumeCommand { get; }
    public ICommand ToggleMuteCommand { get; }
    public ICommand AdjustChannelCommand { get; }
    public ICommand SendNavCommand { get; }
    public ICommand PlaybackActionCommand { get; }
    public ICommand ToggleOscillationCommand { get; }
    public ICommand SetBrightnessCommand { get; }
    public ICommand SetColorCommand { get; }
    public ICommand TogglePrivacyCommand { get; }
    public ICommand ToggleRecordingCommand { get; }
    public ICommand TakeSnapshotCommand { get; }
    public ICommand RefreshSensorCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand OpenHistoryCommand { get; }

    public RemoteControlViewModel(DeviceRegistry registry, CommandRouter router, DeviceStateStore stateStore, TimeProvider clock)
    {
        _registry = registry;
        _router = router;
        _stateStore = stateStore;
        _clock = clock;

        _stateStore.StateChanged += (_, id) =>
        {
            if (id == _deviceId) UpdateUiFromState();
        };

        TogglePowerCommand = new Command(async () =>
        {
            await SendAsync(CommandNames.PowerSet, ("on", IsPoweredOn ? "false" : "true"));
        });

        AdjustTempCommand = new Command<int>(async delta =>
        {
            var next = TemperatureC + delta;
            if (_device is not null)
            {
                next = Math.Clamp(next, _device.Profile.MinTemperatureC, _device.Profile.MaxTemperatureC);
            }
            await SendAsync(CommandNames.TemperatureSet, ("celsius", next.ToString(CultureInfo.InvariantCulture)));
        });

        SetModeCommand = new Command<string>(async mode =>
        {
            if (!string.IsNullOrEmpty(mode))
                await SendAsync(CommandNames.ModeSet, ("mode", mode));
        });

        SetFanSpeedCommand = new Command<string>(async speed =>
        {
            if (!string.IsNullOrEmpty(speed))
                await SendAsync(CommandNames.FanSpeedSet, ("speed", speed));
        });

        ToggleSwingCommand = new Command(async () =>
        {
            await SendAsync(CommandNames.SwingSet, ("on", IsSwingOn ? "false" : "true"));
        });

        AdjustVolumeCommand = new Command<int>(async delta =>
        {
            await SendAsync(CommandNames.VolumeStep, ("delta", delta.ToString(CultureInfo.InvariantCulture)));
        });

        ToggleMuteCommand = new Command(async () =>
        {
            await SendAsync(CommandNames.MuteToggle);
        });

        AdjustChannelCommand = new Command<int>(async delta =>
        {
            await SendAsync(CommandNames.ChannelStep, ("delta", delta.ToString(CultureInfo.InvariantCulture)));
        });

        SendNavCommand = new Command<string>(async key =>
        {
            if (!string.IsNullOrEmpty(key))
                await SendAsync(CommandNames.NavPress, ("key", key));
        });

        PlaybackActionCommand = new Command<string>(async action =>
        {
            if (!string.IsNullOrEmpty(action))
                await SendAsync(CommandNames.PlaybackPress, ("action", action));
        });

        ToggleOscillationCommand = new Command(async () =>
        {
            await SendAsync(CommandNames.OscillationSet, ("on", IsOscillationOn ? "false" : "true"));
        });

        SetBrightnessCommand = new Command<int>(async percent =>
        {
            await SendAsync(CommandNames.BrightnessSet, ("percent", percent.ToString(CultureInfo.InvariantCulture)));
        });

        SetColorCommand = new Command<string>(async hex =>
        {
            if (!string.IsNullOrEmpty(hex))
                await SendAsync(CommandNames.ColorSet, ("hex", hex));
        });

        TogglePrivacyCommand = new Command(async () =>
        {
            await SendAsync(CommandNames.PrivacySet, ("on", IsPrivacyOn ? "false" : "true"));
        });

        ToggleRecordingCommand = new Command(async () =>
        {
            await SendAsync(CommandNames.RecordingSet, ("on", IsRecording ? "false" : "true"));
        });

        TakeSnapshotCommand = new Command(async () =>
        {
            await SendAsync(CommandNames.SnapshotTake);
        });

        RefreshSensorCommand = new Command(async () =>
        {
            await SendAsync(CommandNames.SensorRefresh);
        });

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        OpenHistoryCommand = new Command(async () => await Shell.Current.GoToAsync($"activity-history?deviceId={_deviceId}"));
    }

    private void LoadDevice()
    {
        _device = _registry.Find(_deviceId);
        if (_device is null) return;

        DeviceName = _device.Name;
        Subtitle = _device.IsSimulated
            ? $"{_device.Room} · {_device.BrandName} (Simulation)"
            : $"{_device.Room} · {_device.BrandName} ({DeviceTypeInfo.DisplayName(_device.Connection.Kind)})";
        IsSimulated = _device.IsSimulated;
        StatusPill = _device.IsSimulated ? "SIMULATION MODE" : "CONNECTED";

        if (_device.Profile.Supports(Capability.Temperature))
            TemperatureC = Math.Clamp(24, _device.Profile.MinTemperatureC, _device.Profile.MaxTemperatureC);

        UpdateUiFromState();
    }

    private void UpdateUiFromState()
    {
        if (_device is null) return;
        var state = _stateStore.Get(_deviceId);

        IsPoweredOn = state.Get(StateKeys.Power) == "on";
        if (int.TryParse(state.Get(StateKeys.Temperature), NumberStyles.Integer, CultureInfo.InvariantCulture, out var t))
            TemperatureC = t;
        if (state.Get(StateKeys.Mode) is { } m) AcMode = m;
        if (state.Get(StateKeys.FanSpeed) is { } fs) { AcFanSpeed = fs; FanSpeed = fs; }
        IsSwingOn = state.Get(StateKeys.Swing) == "true";

        if (int.TryParse(state.Get(StateKeys.Volume), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            Volume = v;
        IsMuted = state.Get(StateKeys.Muted) == "true";
        if (int.TryParse(state.Get(StateKeys.Channel), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ch))
            Channel = ch;
        if (state.Get(StateKeys.LastKey) is { } k) LastNavKey = k;

        IsOscillationOn = state.Get(StateKeys.Oscillation) == "true";
        if (int.TryParse(state.Get(StateKeys.Brightness), NumberStyles.Integer, CultureInfo.InvariantCulture, out var b))
            Brightness = b;
        if (state.Get(StateKeys.Color) is { } hex) ColorHex = hex;

        if (state.Get(StateKeys.Playback) is { } pb) PlaybackState = pb;
        IsPrivacyOn = state.Get(StateKeys.Privacy) == "true";
        IsRecording = state.Get(StateKeys.Recording) == "true";

        if (state.Get(StateKeys.SensorTemperature) is { } st)
            SensorTemp = $"{st}°C";
        if (state.Get(StateKeys.SensorHumidity) is { } sh)
            SensorHumidity = $"{sh}%";
        if (state.Get(StateKeys.SensorMotion) is { } sm)
            SensorMotion = sm == "true" ? "Motion Detected!" : "Idle (No motion)";
    }

    private async Task SendAsync(string name, params (string, string)[] parameters)
    {
        if (_device is null) return;
        var cmd = DeviceCommand.Create(_device.Id, name, _clock, parameters);
        var outcome = await _router.SendAsync(cmd);

        if (outcome.Result.Status.IsSuccess())
        {
            LastFeedback = outcome.Result.Status == CommandStatus.Confirmed && outcome.Result.Simulated
                ? $"✓ Confirmed (simulated)"
                : $"✓ {outcome.Result.Status.Describe(outcome.Result.Simulated)}";
        }
        else
        {
            LastFeedback = $"✕ {outcome.Result.Message ?? "Failed"}";
        }

        UpdateUiFromState();
    }
}
