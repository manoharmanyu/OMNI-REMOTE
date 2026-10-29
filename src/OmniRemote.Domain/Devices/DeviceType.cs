namespace OmniRemote.Domain.Devices;

/// <summary>Device categories. The numeric order of the first twelve matches the "Select type" grid.</summary>
public enum DeviceType
{
    AirConditioner = 1,
    SetTopBox = 2,
    Tv = 3,
    TvBox = 4,
    Projector = 5,
    Fan = 6,
    LightBulb = 7,
    AirPurifier = 8,
    PowerAmplifier = 9,
    DvdPlayer = 10,
    WaterHeater = 11,
    Camera = 12,
    TemperatureHumiditySensor = 13,
    MotionSensor = 14,
}

/// <summary>A discrete, independently validated operation family a device may support.</summary>
public enum Capability
{
    Power,
    Volume,
    Mute,
    Channel,
    Navigation,
    Temperature,
    Mode,
    FanSpeed,
    Swing,
    Oscillation,
    Brightness,
    Color,
    Input,
    Playback,
    Seek,
    CameraPrivacy,
    CameraRecording,
    CameraSnapshot,
    MotionDetection,
    TemperatureSensor,
    HumiditySensor,
    MotionSensor,
}

/// <summary>How OmniRemote reaches a device.</summary>
public enum ConnectionKind
{
    /// <summary>Virtual device inside the in-app simulation engine. No hardware, no network, no permissions.</summary>
    Simulation = 0,
    BluetoothLe = 1,
    Wifi = 2,
    InfraredGateway = 3,
    SmartHomeHub = 4,
}

public static class DeviceTypeInfo
{
    /// <summary>Order shown on the Select type screen.</summary>
    public static IReadOnlyList<DeviceType> SelectionOrder { get; } =
    [
        DeviceType.AirConditioner, DeviceType.SetTopBox, DeviceType.Tv,
        DeviceType.TvBox, DeviceType.Projector, DeviceType.Fan,
        DeviceType.LightBulb, DeviceType.AirPurifier, DeviceType.PowerAmplifier,
        DeviceType.DvdPlayer, DeviceType.WaterHeater, DeviceType.Camera,
        DeviceType.TemperatureHumiditySensor, DeviceType.MotionSensor,
    ];

    public static string DisplayName(DeviceType type) => type switch
    {
        DeviceType.AirConditioner => "Air conditioner",
        DeviceType.SetTopBox => "Set-top box",
        DeviceType.Tv => "TV",
        DeviceType.TvBox => "TV box",
        DeviceType.Projector => "Projector",
        DeviceType.Fan => "Fan",
        DeviceType.LightBulb => "Light bulb",
        DeviceType.AirPurifier => "Air purifier",
        DeviceType.PowerAmplifier => "Power amplifier",
        DeviceType.DvdPlayer => "DVD player",
        DeviceType.WaterHeater => "Water heater",
        DeviceType.Camera => "Camera",
        DeviceType.TemperatureHumiditySensor => "Temp & humidity sensor",
        DeviceType.MotionSensor => "Motion sensor",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static bool IsSensor(DeviceType type) =>
        type is DeviceType.TemperatureHumiditySensor or DeviceType.MotionSensor;

    public static string DisplayName(ConnectionKind kind) => kind switch
    {
        ConnectionKind.Simulation => "Simulation",
        ConnectionKind.BluetoothLe => "Bluetooth LE",
        ConnectionKind.Wifi => "Wi-Fi (local API)",
        ConnectionKind.InfraredGateway => "Infrared gateway",
        ConnectionKind.SmartHomeHub => "Home Assistant hub",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
