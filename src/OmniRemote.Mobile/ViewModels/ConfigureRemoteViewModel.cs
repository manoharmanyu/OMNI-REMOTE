using System.Collections.ObjectModel;
using System.Windows.Input;
using OmniRemote.Core.Catalog;
using OmniRemote.Core.Devices;
using OmniRemote.Domain.Devices;
using OmniRemote.Domain.Validation;
using DeviceType = OmniRemote.Domain.Devices.DeviceType;

namespace OmniRemote.Mobile.ViewModels;

[QueryProperty(nameof(TypeQuery), "type")]
[QueryProperty(nameof(BrandIdQuery), "brandId")]
public sealed class ConfigureRemoteViewModel : BaseViewModel
{
    private readonly BrandCatalog _catalog;
    private readonly DeviceRegistry _registry;

    private DeviceType _selectedType = DeviceType.AirConditioner;
    private string _brandId = "carrier";
    private Brand? _brand;

    private string _deviceName = string.Empty;
    private string _selectedRoom = "Living Room";
    private string _model = string.Empty;
    private ConnectionKind _selectedConnection = ConnectionKind.Simulation;
    private string _statusNote = "Simulation Mode — Operates virtually on this phone without external hardware or network access.";
    private string _errorMessage = string.Empty;

    public string TypeQuery
    {
        set
        {
            if (int.TryParse(value, out var t) && Enum.IsDefined((DeviceType)t))
            {
                _selectedType = (DeviceType)t;
                UpdateHeader();
            }
        }
    }

    public string BrandIdQuery
    {
        set
        {
            _brandId = value ?? string.Empty;
            _brand = _catalog.Find(_brandId);
            UpdateHeader();
        }
    }

    public string BrandName => _brand?.Name ?? "Selected Brand";
    public string CategoryName => DeviceTypeInfo.DisplayName(_selectedType);

    public string DeviceName
    {
        get => _deviceName;
        set => SetProperty(ref _deviceName, value);
    }

    public string SelectedRoom
    {
        get => _selectedRoom;
        set => SetProperty(ref _selectedRoom, value);
    }

    public string Model
    {
        get => _model;
        set => SetProperty(ref _model, value);
    }

    public ConnectionKind SelectedConnection
    {
        get => _selectedConnection;
        set
        {
            if (SetProperty(ref _selectedConnection, value))
            {
                StatusNote = _selectedConnection switch
                {
                    ConnectionKind.Simulation => "Simulation Mode — Operates virtually on this phone without external hardware or network access.",
                    ConnectionKind.BluetoothLe => "Bluetooth LE — Requires compatible bonded ORDP peripheral nearby.",
                    ConnectionKind.Wifi => "Wi-Fi — Connects via local private IP or HTTPS endpoint.",
                    ConnectionKind.InfraredGateway => "IR Gateway — Emits one-way commands via external ESP32 gateway.",
                    ConnectionKind.SmartHomeHub => "Smart Home Hub — Relays commands through Home Assistant.",
                    _ => "",
                };
            }
        }
    }

    public bool IsSimulation => SelectedConnection == ConnectionKind.Simulation;

    public string StatusNote
    {
        get => _statusNote;
        set => SetProperty(ref _statusNote, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasErrorMessage));
            }
        }
    }

    public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);

    public ObservableCollection<string> Rooms { get; } =
        ["Living Room", "Bedroom", "Office", "Kitchen", "Dining Room", "Master Bedroom"];

    public IReadOnlyList<ConnectionKind> ConnectionOptions { get; } =
        [ConnectionKind.Simulation, ConnectionKind.BluetoothLe, ConnectionKind.Wifi, ConnectionKind.InfraredGateway, ConnectionKind.SmartHomeHub];

    public ICommand SaveCommand { get; }
    public ICommand BackCommand { get; }

    public ConfigureRemoteViewModel(BrandCatalog catalog, DeviceRegistry registry)
    {
        _catalog = catalog;
        _registry = registry;
        Title = "Configure remote";

        SaveCommand = new Command(async () => await SaveRemoteAsync());
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    private void UpdateHeader()
    {
        OnPropertyChanged(nameof(BrandName));
        OnPropertyChanged(nameof(CategoryName));
        if (string.IsNullOrWhiteSpace(DeviceName))
        {
            DeviceName = $"{BrandName} {CategoryName}";
        }
    }

    private async Task SaveRemoteAsync()
    {
        ErrorMessage = string.Empty;

        var nameVal = InputRules.ValidateDisplayName(DeviceName, InputRules.MaxDeviceNameLength);
        if (!nameVal.IsValid)
        {
            ErrorMessage = nameVal.Error ?? "Invalid remote name.";
            return;
        }

        var device = new RemoteDevice
        {
            Id = Guid.NewGuid(),
            Name = nameVal.Normalized,
            Type = _selectedType,
            BrandId = _brandId,
            BrandName = BrandName,
            Model = string.IsNullOrWhiteSpace(Model) ? null : Model.Trim(),
            Room = SelectedRoom,
            Connection = new ConnectionSettings
            {
                Kind = SelectedConnection,
                Values = new Dictionary<string, string>(),
            },
            Profile = DeviceProfile.DefaultFor(_selectedType),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var errors = await _registry.AddAsync(device);
        if (errors.Count > 0)
        {
            ErrorMessage = errors[0];
            return;
        }

        // Navigate directly to the newly created remote
        await Shell.Current.GoToAsync($"///HomeScreen/remote-control?deviceId={device.Id}");
    }
}
